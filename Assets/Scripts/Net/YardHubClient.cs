// YardHubClient (G-6) — docs/protocol.md WebSocket JSON.
// 자동 재접속(3s)·heartbeat(1s)·연결 상태. SIM/LIVE 전환, 크레인 선택
// tower-1(실물 연동 대상) / goliath-1(디지털) — 같은 Hub 프로토콜.
// 운전자 입력은 라우터의 차단 판단을 통과한 것만 operator_input으로 전송하고,
// 차단되면 interlock을 보낸다. crane_state 수신 시 실측값 보간 + 예측 차이 제공.
// (상단 SIM/LIVE 버튼은 임시로 OnGUI — 정식 UI는 W-3 작업 액티브에서 교체)

using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using YardAX.Core;
using YardAX.Goliath;
using YardAX.Safety;

namespace YardAX.Net
{
    public enum HubMode { SIM, LIVE }

    public class YardHubClient : MonoBehaviour
    {
        public string url = "ws://localhost:8765";
        public HubMode mode = HubMode.SIM;
        public string selectedCrane = "tower-1";
        public OperatorInputRouter router;
        public GoliathController goliath;
        public Transform craneLocalFrame; // worker x_m/z_m 해석 프레임 (크레인 로컬)

        public bool Connected { get; private set; }
        public event Action<bool> ConnectionChanged;
        /// <summary>LIVE crane_state 최신값 (m): travel, trolley, hook</summary>
        public Vector3 LiveState { get; private set; }
        public float LiveStateAge => Time.time - _liveAt;
        float _liveAt = -999f;

        ClientWebSocket _ws;
        CancellationTokenSource _cts;
        readonly ConcurrentQueue<string> _rx = new();
        readonly ConcurrentQueue<string> _tx = new();
        float _hb, _lerp;
        Vector3 _liveTarget;

        [Serializable] class TypeProbe { public string type; }
        [Serializable] class CraneStateMsg { public string crane; public float travel_m, trolley_m, hook_m; }
        [Serializable] class WorkerMsg { public string id; public float x_m, z_m; }
        [Serializable] class OperatorInputMsg { public string text; public string lang; }

        void OnEnable()
        {
            if (router != null) router.Routed += OnRouted;
            if (mode == HubMode.LIVE) StartLive();
        }

        void OnDisable()
        {
            if (router != null) router.Routed -= OnRouted;
            StopLive();
        }

        public void SetMode(HubMode m)
        {
            if (mode == m) return;
            mode = m;
            if (m == HubMode.LIVE) StartLive(); else StopLive();
        }

        void StartLive()
        {
            StopLive();
            _cts = new CancellationTokenSource();
            _ = ConnectLoop(_cts.Token);
        }

        void StopLive()
        {
            _cts?.Cancel();
            _cts = null;
            try { _ws?.Dispose(); } catch { }
            _ws = null;
            if (Connected) { Connected = false; ConnectionChanged?.Invoke(false); }
        }

        async Task ConnectLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _ws = new ClientWebSocket();
                    await _ws.ConnectAsync(new Uri(url), ct);
                    _rx.Enqueue("__connected__");
                    var buf = new byte[8192];
                    var sb = new StringBuilder();
                    while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                    {
                        // 송신 큐 비우기
                        while (_tx.TryDequeue(out var outMsg))
                            await _ws.SendAsync(Encoding.UTF8.GetBytes(outMsg),
                                WebSocketMessageType.Text, true, ct);
                        var res = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), ct);
                        if (res.MessageType == WebSocketMessageType.Close) break;
                        sb.Append(Encoding.UTF8.GetString(buf, 0, res.Count));
                        if (res.EndOfMessage) { _rx.Enqueue(sb.ToString()); sb.Clear(); }
                    }
                }
                catch { /* 재접속 */ }
                _rx.Enqueue("__disconnected__");
                try { await Task.Delay(3000, ct); } catch { break; }
            }
        }

        void Update()
        {
            while (_rx.TryDequeue(out var msg)) Handle(msg);
            if (mode == HubMode.LIVE && Connected)
            {
                _hb += Time.deltaTime;
                if (_hb >= 1f)
                {
                    _hb = 0f;
                    Send($"{{\"type\":\"heartbeat\",\"ts\":{(long)(Time.realtimeSinceStartup * 1000)}}}");
                }
                // 실측값 보간
                LiveState = Vector3.Lerp(LiveState, _liveTarget, Time.deltaTime * 8f);
            }
        }

        void Handle(string msg)
        {
            if (msg == "__connected__") { Connected = true; ConnectionChanged?.Invoke(true); return; }
            if (msg == "__disconnected__")
            {
                if (Connected) { Connected = false; ConnectionChanged?.Invoke(false); }
                return;
            }
            TypeProbe probe;
            try { probe = JsonUtility.FromJson<TypeProbe>(msg); } catch { return; }
            switch (probe?.type)
            {
                case "crane_state":
                    var cs = JsonUtility.FromJson<CraneStateMsg>(msg);
                    if (string.IsNullOrEmpty(cs.crane) || cs.crane == selectedCrane)
                    { _liveTarget = new Vector3(cs.travel_m, cs.trolley_m, cs.hook_m); _liveAt = Time.time; }
                    break;
                case "worker":
                    var w = JsonUtility.FromJson<WorkerMsg>(msg);
                    if (WorkerRegistry.Instance != null)
                    {
                        var frame = craneLocalFrame != null ? craneLocalFrame : transform;
                        var world = frame.TransformPoint(new Vector3(w.x_m, 0, w.z_m));
                        WorkerRegistry.Instance.SetRemote(w.id, new Vector2(world.x, world.z));
                    }
                    break;
                case "operator_input":
                    var oi = JsonUtility.FromJson<OperatorInputMsg>(msg);
                    Debug.Log($"[YardHub] 조작반 입력: {oi.text} ({oi.lang})");
                    break;
            }
        }

        /// <summary>예측(디지털) 대비 실측 차이 (m) — goliath 디지털엔 해당 없음</summary>
        public float PredictionErrorM(CraneAxis axis)
        {
            if (LiveStateAge > 2f) return -1f;
            float live = axis switch
            {
                CraneAxis.GANTRY or CraneAxis.TROLLEY => LiveState.x,
                CraneAxis.TROLLEY_A => LiveState.y,
                CraneAxis.HOIST or CraneAxis.HOIST_A => LiveState.z,
                _ => float.NaN,
            };
            if (float.IsNaN(live) || goliath == null) return -1f;
            return Mathf.Abs(goliath.State.GetPosition(axis) - live);
        }

        void OnRouted(CraneCommandV2 cmd, bool allowed, string info)
        {
            if (mode != HubMode.LIVE || !Connected) return;
            if (allowed)
                Send($"{{\"type\":\"operator_input\",\"crane\":\"{cmd.craneId}\",\"axis\":\"{cmd.axis}\"," +
                     $"\"dir\":\"{(cmd.direction > 0 ? "+" : "-")}\",\"amount\":{cmd.amountM}}}");
            else
                SendInterlock(true, info);
        }

        public void SendInterlock(bool active, string reason) =>
            Send($"{{\"type\":\"interlock\",\"active\":{(active ? "true" : "false")}," +
                 $"\"reason\":\"{reason?.Replace("\"", "'")}\",\"ts\":0}}");

        void Send(string json) { if (Connected) _tx.Enqueue(json); }

        // ── 임시 상단 UI (정식 UI는 W-3에서 교체) ──
        void OnGUI()
        {
            GUILayout.BeginArea(new UnityEngine.Rect(10, 10, 420, 60));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(mode == HubMode.SIM ? "SIM ▶ LIVE 전환" : "LIVE ▶ SIM 전환", GUILayout.Height(28)))
                SetMode(mode == HubMode.SIM ? HubMode.LIVE : HubMode.SIM);
            GUILayout.Label(mode == HubMode.LIVE
                ? (Connected ? $"● LIVE 연결됨 ({selectedCrane})" : "○ LIVE 연결 끊김 — 재접속 중 (SIM 전환 가능)")
                : "SIM 모드");
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
