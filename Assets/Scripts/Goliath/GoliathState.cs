// GoliathState (G-2) — ICraneState 구현. 컨트롤러의 모터를 읽기 전용으로 공개.

using System.Collections.Generic;
using YardAX.Core;

namespace YardAX.Goliath
{
    public class GoliathState : ICraneState
    {
        readonly IReadOnlyDictionary<CraneAxis, AxisMotor> _motors;
        public string CraneId { get; }
        /// <summary>현재 하중 (t) — 시뮬레이션 값. 블록 연결 시 컨트롤러가 설정</summary>
        public float LoadTon { get; internal set; }

        public GoliathState(string craneId, IReadOnlyDictionary<CraneAxis, AxisMotor> motors)
        {
            CraneId = craneId;
            _motors = motors;
        }

        public bool Supports(CraneAxis axis) => _motors.ContainsKey(axis);
        public float GetPosition(CraneAxis axis) =>
            _motors.TryGetValue(axis, out var m) ? m.Position : 0f;
        public float GetVelocity(CraneAxis axis) =>
            _motors.TryGetValue(axis, out var m) ? m.Velocity : 0f;
    }
}
