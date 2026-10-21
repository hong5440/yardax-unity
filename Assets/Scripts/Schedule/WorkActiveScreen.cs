// W-3 작업 액티브 — 선택한 작업지시를 크게. 3D ≈70% + 우측 AI 지침 카드
// + 상단 얇은 띠(작업 정보·날씨·공정 단계 위치) + 하단 얇은 띠(단계 진행·경고 타임라인).
// 단축키: A 전체화면 토글 · ←/→ 이전·다음 · Esc 보드 복귀.
// 시간 슬라이더(06~20시)로 환경이 바뀌면 점수·지침 재계산 (예: 16시 풍속 상승).
// 탑재 LIVE: Yard Hub 실물 연동 — "실물 검증은 타워크레인으로 수행" 안내 (W-7).
// 탑재 장면에서 하부 작업자 클릭 → 1인칭 퍼포먼스(F) 전환.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using YardAX.Data;
using YardAX.Net;
using YardAX.Performance;
using YardAX.Safety;

namespace YardAX.Schedule
{
    public class WorkActiveScreen : MonoBehaviour
    {
        public ScheduleBoardView board;
        public FacilityMapTable facilityMap;
        public Camera mainCamera;
        public WarningTimeline timeline;
        public YardHubClient hub;
        public PerformanceSequencer performance;   // 1인칭 전환 (W-7)
        public ProcessSceneManager scenes;

        public bool Visible { get; private set; }
        public bool Fullscreen3D { get; private set; }
        public int Hour = 14;
        public GuideLang lang = GuideLang.ko;

        int _index;
        WorkOrder _order;
        List<RiskFactor> _factors = new();
        int _score;
        GuidelineCard _card;
        readonly List<string> _confirmLog = new();
        LineRenderer _conflictLink;

        public void Show(int index)
        {
            var today = board.Today;
            _index = Mathf.Clamp(index, 0, today.Count - 1);
            _order = today[_index];
            Visible = true;
            Hour = int.TryParse(_order.start.Split(':')[0], out var h) ? h : 14;
            FlyCamera();
            scenes?.ActivateFor(_order);
            Recompute();
            DrawConflictLink();
        }

        public void Close()
        {
            Visible = false;
            if (board != null) board.Visible = true;
            if (_conflictLink != null) _conflictLink.positionCount = 0;
        }

        void FlyCamera()
        {
            if (mainCamera == null || facilityMap == null) return;
            var e = facilityMap.Find(_order.facilityId);
            var anchor = facilityMap.ResolveSceneObject(_order.facilityId);
            Vector3 basePos = anchor != null ? anchor.position : Vector3.zero;
            var pos = basePos + (e != null ? e.cameraOffset : new Vector3(0, 35, -55));
            var look = basePos + (e != null ? e.cameraLookOffset : Vector3.zero);
            mainCamera.transform.position = pos;
            mainCamera.transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
        }

        void Recompute()
        {
            var repo = board.Repo;
            var env = repo.GetEnvironment(Hour);
            int conflicts = 0;
            foreach (var c in repo.GetConflicts())
                if (c.workOrderIds.Contains(_order.id) && Hour >= c.startH && Hour < c.endH) conflicts++;
            _factors = RiskRuleEngine.Evaluate(new RiskInput
            {
                order = _order,
                conflictCount = conflicts,
                missingPermits = repo.GetMissingPermits(_order),
                windMs = env?.windMs ?? 0f,
                tempC = env?.tempC ?? 20f,
                rain = env?.rain ?? false,
                night = Hour >= 18 || Hour < 6,
                block = string.IsNullOrEmpty(_order.blockId) ? null : repo.GetBlock(_order.blockId),
                processRisk = repo.GetProcessRisk(_order.process),
                thresholds = RiskThresholds.Default,
            }, out _score);
            _card = GuidelineGenerator.FromTemplate(_order, _factors, _score, lang);
            // LLM 경로(Yard Hub 경유)는 LIVE 연결 시 guideline_request 전송 — 실패/미응답이면 이 템플릿 유지
        }

        void DrawConflictLink()
        {
            if (_conflictLink == null)
            {
                _conflictLink = gameObject.AddComponent<LineRenderer>();
                _conflictLink.widthMultiplier = 0.3f;
                var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                _conflictLink.material = new Material(sh) { color = new Color(1f, 0.3f, 0.25f, 0.9f) };
            }
            _conflictLink.positionCount = 0;
            var repo = board.Repo;
            foreach (var c in repo.GetConflicts())
            {
                if (!c.workOrderIds.Contains(_order.id)) continue;
                var a = facilityMap != null ? facilityMap.ResolveSceneObject(c.facilityId) : null;
                if (a == null) continue;
                // 같은 시설 내 간섭: 지점 두 개를 수직 아크로 강조
                var p = a.position;
                _conflictLink.positionCount = 3;
                _conflictLink.SetPositions(new[]
                { p + new Vector3(-6, 0.5f, 0), p + new Vector3(0, 12f, 0), p + new Vector3(6, 0.5f, 0) });
                break;
            }
        }

        void Update()
        {
            if (!Visible) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame) Close();
            if (kb.aKey.wasPressedThisFrame) Fullscreen3D = !Fullscreen3D;
            if (kb.rightArrowKey.wasPressedThisFrame) Show(_index + 1);
            if (kb.leftArrowKey.wasPressedThisFrame) Show(_index - 1);

            // 탑재 장면: 하부 작업자 클릭 → 1인칭 퍼포먼스 (W-7)
            if (_order.process.Contains("탑재") && Mouse.current.leftButton.wasPressedThisFrame
                && performance != null && mainCamera != null)
            {
                var ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out var hit, 500f) &&
                    hit.transform.name.Contains("Worker"))
                    performance.Go(2); // ② 1인칭 전환부터
            }
        }

        void OnGUI()
        {
            if (!Visible || _order == null) return;
            float w = Screen.width, h = Screen.height;

            if (!Fullscreen3D)
            {
                // ── 상단 얇은 띠 ──
                GUILayout.BeginArea(new UnityEngine.Rect(0, 0, w, 34), GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"[{_order.id}] {_order.process} · {_order.blockId} · 시설 {_order.facilityId} · {_order.start}~{_order.end}",
                    new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                var env = board.Repo.GetEnvironment(Hour);
                if (env != null)
                    GUILayout.Label($"   {Hour:00}시 {env.tempC:0.0}°C · 풍속 {env.windMs:0.0}m/s{(env.rain ? " · 비" : "")}");
                GUILayout.Label("  시간:", GUILayout.ExpandWidth(false));
                int newHour = (int)GUILayout.HorizontalSlider(Hour, 6, 20, GUILayout.Width(140));
                if (newHour != Hour) { Hour = newHour; Recompute(); }
                GUILayout.FlexibleSpace();
                if (_order.process.Contains("탑재") && hub != null && hub.mode == HubMode.LIVE)
                    GUILayout.Label("LIVE — 실물 검증은 타워크레인으로 수행",
                        new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                GUILayout.Label("A 전체화면 · ←→ 작업 이동 · Esc 보드", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                GUILayout.EndArea();

                // ── 우측 AI 작업안전지침 카드 (3D ≈70% 유지: 폭 28%) ──
                float pw = w * 0.28f;
                GUILayout.BeginArea(new UnityEngine.Rect(w - pw, 40, pw - 8, h - 120), GUI.skin.box);
                GUILayout.Label("AI 작업안전지침" + (_card.fromLlm ? "" : " (템플릿 대체)"),
                    new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
                GUILayout.BeginHorizontal();
                foreach (GuideLang l in System.Enum.GetValues(typeof(GuideLang)))
                    if (GUILayout.Toggle(lang == l, l.ToString(), GUI.skin.button) && lang != l)
                    { lang = l; Recompute(); }
                GUILayout.EndHorizontal();
                GUILayout.Label(_card.verdict, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                GUILayout.Label("— 작업 전 확인");
                foreach (var s in _card.beforeWork) GUILayout.Label(" □ " + s);
                GUILayout.Label("— 작업 중 주의");
                foreach (var s in _card.duringWork) GUILayout.Label(" • " + s);
                GUILayout.Label("— 즉시 중지 기준");
                foreach (var s in _card.stopCriteria) GUILayout.Label(" ■ " + s);
                GUILayout.Label("근거: " + _card.evidence, new GUIStyle(GUI.skin.label) { fontSize = 11 });
                GUILayout.Space(6);
                GUILayout.Label("위험 요인 기여도", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                foreach (var f in _factors)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{f.name} +{f.points}", GUILayout.Width(120));
                    GUI.color = new Color(1f, 0.6f, 0.2f);
                    GUILayout.Box("", GUILayout.Width(Mathf.Max(4, f.points * 3f)), GUILayout.Height(10));
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();
                }
                GUILayout.Label("업계 빈도(2025 조선업 재해자, 고용노동부):",
                    new GUIStyle(GUI.skin.label) { fontSize = 11 });
                foreach (var (type, count) in RiskRuleEngine.IndustryFrequency)
                    GUILayout.Label($"  {type} {count}명", new GUIStyle(GUI.skin.label) { fontSize = 11 });
                GUILayout.Space(6);
                if (GUILayout.Button("Confirm — 지침 확인"))
                    _confirmLog.Add($"{System.DateTime.Now:HH:mm:ss} {_order.id} {lang} 점수 {_score}");
                foreach (var c in _confirmLog)
                    GUILayout.Label("✓ " + c, new GUIStyle(GUI.skin.label) { fontSize = 11 });
                GUILayout.EndArea();

                // ── 하단 얇은 띠: 블록 단계 위치 + 경고 타임라인 ──
                GUILayout.BeginArea(new UnityEngine.Rect(0, h - 70, w, 70), GUI.skin.box);
                GUILayout.BeginHorizontal();
                var block = string.IsNullOrEmpty(_order.blockId) ? null : board.Repo.GetBlock(_order.blockId);
                if (block != null)
                    foreach (var s in block.stages)
                    {
                        int st = board.Repo.StageStatus(s);
                        GUI.color = st == 0 ? new Color(0.24f, 0.78f, 1f) : st < 0 ? Color.gray : new Color(0.4f, 0.45f, 0.55f);
                        GUILayout.Label(s.stage, GUI.skin.box, GUILayout.ExpandWidth(false));
                        GUI.color = Color.white;
                    }
                GUILayout.FlexibleSpace();
                if (timeline != null)
                {
                    int n = timeline.Entries.Count;
                    for (int i = Mathf.Max(0, n - 3); i < n; i++)
                        GUILayout.Label($"[{timeline.Entries[i].kind}] {timeline.Entries[i].message}",
                            new GUIStyle(GUI.skin.label) { fontSize = 11 }, GUILayout.Width(300));
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
            else
            {
                GUI.Label(new UnityEngine.Rect(10, 6, 600, 22),
                    $"[{_order.id}] {_order.process} — A 복귀 · ←→ 이동 · Esc 보드");
            }
        }
    }
}
