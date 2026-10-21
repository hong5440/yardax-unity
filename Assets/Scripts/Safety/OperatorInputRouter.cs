// OperatorInputRouter (G-2, 차단 판단은 G-5가 게이트로 등록)
// 모든 입력(키보드·게임패드·화면 버튼·TCP 명령)이 이 한 곳을 거친다.
// AI는 크레인을 직접 움직이지 않는다 — 라우터는 사람 입력과 Confirm된 명령만 받는다.

using System;
using System.Collections.Generic;
using UnityEngine;
using YardAX.Core;
using YardAX.Goliath;

namespace YardAX.Safety
{
    /// <summary>차단 판단 게이트 — G-5 위험 판단들이 구현해 등록</summary>
    public interface IRiskGate
    {
        /// <summary>true면 허용. 차단이면 reason 기입</summary>
        bool Allow(CraneCommandV2 cmd, out string reason);
    }

    public class OperatorInputRouter : MonoBehaviour
    {
        public GoliathController goliath;

        readonly List<IRiskGate> _gates = new();
        /// <summary>(명령, 허용 여부, 사유) — 경고 타임라인·Hub 전송이 구독</summary>
        public event Action<CraneCommandV2, bool, string> Routed;

        public void RegisterGate(IRiskGate gate) { if (!_gates.Contains(gate)) _gates.Add(gate); }
        public void UnregisterGate(IRiskGate gate) => _gates.Remove(gate);

        /// <summary>명령 실행 시도. 차단 시 false — 차단된 입력은 전송하지 않고 기록만</summary>
        public bool TryExecute(CraneCommandV2 cmd, string source = "ui")
        {
            foreach (var g in _gates)
            {
                if (!g.Allow(cmd, out var reason))
                {
                    Routed?.Invoke(cmd, false, $"{reason} ({source})");
                    return false;
                }
            }
            if (goliath != null && cmd.craneId == goliath.craneId)
                goliath.ExecuteMove(cmd.axis, cmd.direction * cmd.amountM);
            Routed?.Invoke(cmd, true, source);
            return true;
        }

        /// <summary>조그(누르는 동안). 차단 중이면 0으로 강제</summary>
        public void TryJog(CraneAxis axis, float dir, string source = "keyboard")
        {
            var probe = new CraneCommandV2
            {
                craneId = goliath != null ? goliath.craneId : "goliath-1",
                axis = axis, direction = dir >= 0 ? 1 : -1,
                amountM = 0.5f,
            };
            foreach (var g in _gates)
            {
                if (dir != 0f && !g.Allow(probe, out var reason))
                {
                    Routed?.Invoke(probe, false, $"{reason} ({source})");
                    dir = 0f;
                    break;
                }
            }
            goliath?.ExecuteJog(axis, dir);
        }

        public void HaltAll(string reason) => goliath?.HaltAll(reason);
    }
}
