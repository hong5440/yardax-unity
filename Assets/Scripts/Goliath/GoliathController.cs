// GoliathController (G-2) — 7축 구동 + 동기 모드 + 리그 피벗 반영.
// 입력은 반드시 OperatorInputRouter를 거친다 (직접 호출은 라우터만 한다).

using System;
using System.Collections.Generic;
using UnityEngine;
using YardAX.Config;
using YardAX.Core;

namespace YardAX.Goliath
{
    [RequireComponent(typeof(GoliathRig))]
    public class GoliathController : MonoBehaviour
    {
        public GoliathCraneConfig config;
        public string craneId = "goliath-1";

        [Header("동기 모드")]
        public bool syncTrolleyAB = true;
        public bool syncHoistAB = true;

        /// <summary>동기 오차 초과 등 경고 (메시지) — 타임라인이 구독</summary>
        public event Action<string> Warning;

        GoliathRig _rig;
        readonly Dictionary<CraneAxis, AxisMotor> _motors = new();
        public GoliathState State { get; private set; }
        bool _syncWarned;

        void Awake()
        {
            _rig = GetComponent<GoliathRig>();
            if (config == null) config = _rig.config;
            var c = config;
            _motors[CraneAxis.GANTRY] = new AxisMotor(0, c.gantryRangeM, c.gantrySpeed, c.gantryAccel);
            _motors[CraneAxis.TROLLEY_A] = new AxisMotor(c.upperTrolleyMinM, c.upperTrolleyMaxM, c.upperTrolleySpeed, c.upperTrolleyAccel, -c.spanM * 0.18f);
            _motors[CraneAxis.TROLLEY_B] = new AxisMotor(c.upperTrolleyMinM, c.upperTrolleyMaxM, c.upperTrolleySpeed, c.upperTrolleyAccel, c.spanM * 0.18f);
            _motors[CraneAxis.TROLLEY_L] = new AxisMotor(c.lowerTrolleyMinM, c.lowerTrolleyMaxM, c.lowerTrolleySpeed, c.lowerTrolleyAccel, 0f);
            _motors[CraneAxis.HOIST_A] = new AxisMotor(c.hoistMinM, c.hoistMaxM, c.hoistSpeed, c.hoistAccel, c.heightM * 0.5f);
            _motors[CraneAxis.HOIST_B] = new AxisMotor(c.hoistMinM, c.hoistMaxM, c.hoistSpeed, c.hoistAccel, c.heightM * 0.5f);
            _motors[CraneAxis.HOIST_L] = new AxisMotor(c.hoistMinM, c.hoistMaxM, c.hoistSpeed, c.hoistAccel, c.heightM * 0.6f);
            State = new GoliathState(craneId, _motors);
        }

        public AxisMotor Motor(CraneAxis axis) =>
            _motors.TryGetValue(axis, out var m) ? m : null;

        /// <summary>라우터 전용: 상대 이동 명령. 동기 모드면 짝 축도 함께</summary>
        internal void ExecuteMove(CraneAxis axis, float signedAmountM)
        {
            foreach (var a in WithSyncPartner(axis))
                Motor(a)?.MoveBy(signedAmountM);
        }

        /// <summary>라우터 전용: 조그 (-1..+1)</summary>
        internal void ExecuteJog(CraneAxis axis, float dir)
        {
            foreach (var a in WithSyncPartner(axis))
            {
                var m = Motor(a);
                if (m != null) { m.Stop(); m.Jog = dir; }
            }
        }

        internal void HaltAll(string reason)
        {
            foreach (var m in _motors.Values) m.Halt();
            Warning?.Invoke($"전축 정지 — {reason}");
        }

        IEnumerable<CraneAxis> WithSyncPartner(CraneAxis axis)
        {
            yield return axis;
            if (syncTrolleyAB && axis == CraneAxis.TROLLEY_A) yield return CraneAxis.TROLLEY_B;
            else if (syncTrolleyAB && axis == CraneAxis.TROLLEY_B) yield return CraneAxis.TROLLEY_A;
            else if (syncHoistAB && axis == CraneAxis.HOIST_A) yield return CraneAxis.HOIST_B;
            else if (syncHoistAB && axis == CraneAxis.HOIST_B) yield return CraneAxis.HOIST_A;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var m in _motors.Values) m.Step(dt);
            CheckSync();
            ApplyToRig();
        }

        /// <summary>동기 오차 검사 — 허용 오차 초과 시 1회 경고 이벤트</summary>
        public bool CheckSync()
        {
            float hoistDiff = Mathf.Abs(State.GetPosition(CraneAxis.HOIST_A) - State.GetPosition(CraneAxis.HOIST_B));
            float trolleyDiff = Mathf.Abs(State.GetPosition(CraneAxis.TROLLEY_A) - State.GetPosition(CraneAxis.TROLLEY_B)
                                          - (config.spanM * 0.36f)); // 기본 간격 유지 기준
            bool bad = (syncHoistAB && hoistDiff > config.syncToleranceM);
            if (bad && !_syncWarned)
            {
                _syncWarned = true;
                Warning?.Invoke($"동기 권상 오차 {hoistDiff:0.00}m > 허용 {config.syncToleranceM}m");
            }
            else if (!bad) _syncWarned = false;
            return !bad;
        }

        void ApplyToRig()
        {
            var root = _rig.Root;
            var p = root.localPosition;
            root.localPosition = new Vector3(p.x, p.y, State.GetPosition(CraneAxis.GANTRY));
            SetTrolley(CraneAxis.TROLLEY_A, CraneAxis.HOIST_A, config.heightM + config.girderHeightM);
            SetTrolley(CraneAxis.TROLLEY_B, CraneAxis.HOIST_B, config.heightM + config.girderHeightM);
            SetTrolley(CraneAxis.TROLLEY_L, CraneAxis.HOIST_L, config.heightM);
        }

        void SetTrolley(CraneAxis trolleyAxis, CraneAxis hoistAxis, float trolleyY)
        {
            var t = _rig.GetPivot(trolleyAxis);
            if (t == null) return;
            var lp = t.localPosition;
            t.localPosition = new Vector3(State.GetPosition(trolleyAxis), lp.y, lp.z);
            var hook = _rig.GetPivot(hoistAxis);
            if (hook == null) return;
            float drop = Mathf.Max(0.3f, trolleyY - State.GetPosition(hoistAxis));
            hook.localPosition = new Vector3(0, -drop, 0);
            var lr = hook.GetComponent<LineRenderer>();
            if (lr != null && lr.positionCount == 2)
                lr.SetPosition(1, new Vector3(0, drop, 0));
        }
    }
}
