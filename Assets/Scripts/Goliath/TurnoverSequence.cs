// 블록 턴오버 시퀀스 (G-4)
// ① 상부 2 + 하부 1 후크 연결 → ② 동기 권상 → ③ 하부를 내리며 90°/180° 회전
// → ④ 하부 해제 → ⑤ 착지. 각 단계는 사람이 Confirm해야 진행 (1/5~5/5 표시).
// 시작 전 회전 포락선을 계산·표시하고, 안에 작업자가 있으면 시작 불가.
// 기울기·하중 분담비는 시뮬레이션 값 — UI에 그렇게 표기할 것.

using System;
using UnityEngine;
using YardAX.Core;
using YardAX.Safety;

namespace YardAX.Goliath
{
    public class TurnoverSequence : MonoBehaviour
    {
        public GoliathController controller;
        public GoliathRig rig;
        [Range(90f, 180f)] public float targetTiltDeg = 90f;
        public float rotateSpeedDegPerSec = 3f;

        public static readonly string[] StepNames =
        {
            "후크 연결 (상부 2 + 하부 1)", "동기 권상", "하부 하강·회전",
            "하부 후크 해제", "착지",
        };

        /// <summary>0 = 대기, 1..5 = 진행 단계</summary>
        public int StepIndex { get; private set; }
        public bool AwaitingConfirm { get; private set; }
        public float TiltDeg { get; private set; }
        public (float upper, float lower) LoadShare => TurnoverMath.LoadShare(TiltDeg);
        public float EnvelopeRadius { get; private set; }
        public Vector2 EnvelopeCenterXZ { get; private set; }

        public event Action<int, string> StepChanged;   // (1..5, 이름)
        public event Action<string> Blocked;

        GameObject _envelopeViz;

        /// <summary>시작 가능 검사: 포락선 계산·표시 + 작업자 침입 검사</summary>
        public bool CanStart(out string reason)
        {
            var size = controller.config.loadBlockSize;
            EnvelopeRadius = TurnoverMath.EnvelopeRadius(size.z, size.y);
            var lb = rig.LoadBlock != null ? rig.LoadBlock.position : rig.Root.position;
            EnvelopeCenterXZ = new Vector2(lb.x, lb.z);
            ShowEnvelope();
            if (WorkerRegistry.Instance != null &&
                WorkerRegistry.Instance.AnyInCircle(EnvelopeCenterXZ, EnvelopeRadius))
            {
                reason = "회전 포락선 안에 작업자 — 턴오버 시작 불가";
                Blocked?.Invoke(reason);
                return false;
            }
            reason = null;
            return true;
        }

        public bool Begin()
        {
            if (StepIndex != 0) return false;
            if (!CanStart(out _)) return false;
            TiltDeg = 0f;
            Advance();
            return true;
        }

        /// <summary>사람 Confirm — 다음 단계로</summary>
        public void Confirm()
        {
            if (StepIndex == 0 || !AwaitingConfirm) return;
            AwaitingConfirm = false;
            if (StepIndex >= StepNames.Length) { Finish(); return; }
            Advance();
        }

        void Advance()
        {
            StepIndex++;
            if (StepIndex > StepNames.Length) { Finish(); return; }
            StepChanged?.Invoke(StepIndex, StepNames[StepIndex - 1]);
            switch (StepIndex)
            {
                case 2: // 동기 권상 +6m
                    controller.ExecuteMove(CraneAxis.HOIST_A, +6f);
                    controller.ExecuteMove(CraneAxis.HOIST_L, +6f);
                    break;
                case 3: break; // Update에서 회전 진행
                case 4: // 하부 해제 — 하부 후크만 하강
                    controller.ExecuteMove(CraneAxis.HOIST_L, -8f);
                    break;
                case 5: // 착지 — 동기 하강
                    controller.ExecuteMove(CraneAxis.HOIST_A, -6f);
                    break;
            }
            if (StepIndex != 3) AwaitingConfirm = true;
        }

        void Update()
        {
            if (StepIndex != 3) return;
            // 회전: 하부 후크를 내리며 블록을 x축 기준 회전
            TiltDeg = Mathf.MoveTowards(TiltDeg, targetTiltDeg, rotateSpeedDegPerSec * Time.deltaTime);
            controller.Motor(CraneAxis.HOIST_L)?.MoveTo(
                controller.config.hoistMinM + 2f +
                (1f - TiltDeg / targetTiltDeg) * 10f);
            if (rig.LoadBlock != null)
                rig.LoadBlock.localRotation = Quaternion.Euler(TiltDeg, 0, 0);
            if (Mathf.Approximately(TiltDeg, targetTiltDeg))
                AwaitingConfirm = true; // 회전 완료 → Confirm 대기
        }

        void Finish()
        {
            StepIndex = 0;
            AwaitingConfirm = false;
            TiltDeg = 0f;
            if (_envelopeViz != null) _envelopeViz.SetActive(false);
        }

        void ShowEnvelope()
        {
            if (_envelopeViz == null)
            {
                _envelopeViz = new GameObject("TurnoverEnvelope")
                { layer = YardAX.Viz.ProjectionCamera.OverlayLayer };
                var mf = _envelopeViz.AddComponent<MeshFilter>();
                mf.sharedMesh = DiscMesh(48);
                var mr = _envelopeViz.AddComponent<MeshRenderer>();
                var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                var m = new Material(sh) { color = new Color(1f, 0.35f, 0.2f, 0.25f) };
                m.renderQueue = 3004;
                mr.sharedMaterial = m;
            }
            _envelopeViz.SetActive(true);
            _envelopeViz.transform.position =
                new Vector3(EnvelopeCenterXZ.x, 0.06f, EnvelopeCenterXZ.y);
            _envelopeViz.transform.localScale = Vector3.one * EnvelopeRadius;
        }

        static Mesh DiscMesh(int seg)
        {
            var mesh = new Mesh { name = "EnvelopeDisc" };
            var v = new Vector3[seg + 1];
            var tris = new int[seg * 3];
            v[0] = Vector3.zero;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                v[i + 1] = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            }
            for (int i = 0; i < seg; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = 1 + (i + 1) % seg;
                tris[i * 3 + 2] = 1 + i;
            }
            mesh.vertices = v; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
