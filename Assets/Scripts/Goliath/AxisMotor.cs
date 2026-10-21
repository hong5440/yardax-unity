// 축 1개의 운동 모델 (G-2) — MonoBehaviour 없는 순수 계산 클래스 (EditMode 테스트 대상).
// 속도·가속도·범위 제한, 범위 끝 접근 시 제동거리(v²/2a)에 맞춰 감속 정지.

using System;

namespace YardAX.Goliath
{
    public class AxisMotor
    {
        public float Position { get; private set; }
        public float Velocity { get; private set; }
        public float Min { get; }
        public float Max { get; }
        public float MaxSpeed { get; }
        public float Accel { get; }

        /// <summary>명령 이동의 목표 위치 (조그 중엔 null)</summary>
        public float? Target { get; private set; }
        /// <summary>조그 입력 (-1..+1). 명령 이동이 없을 때만 적용</summary>
        public float Jog { get; set; }

        public AxisMotor(float min, float max, float maxSpeed, float accel, float start = 0f)
        {
            if (max <= min) throw new ArgumentException("max <= min");
            Min = min; Max = max;
            MaxSpeed = Math.Max(0.01f, maxSpeed);
            Accel = Math.Max(0.001f, accel);
            Position = Clamp(start);
        }

        public void MoveBy(float signedAmount) => MoveTo(Position + signedAmount);
        public void MoveTo(float target) => Target = Clamp(target);
        public void Stop() { Target = null; Jog = 0f; }
        /// <summary>즉시 정지 (비상·인터록)</summary>
        public void Halt() { Stop(); Velocity = 0f; }

        public bool IsMoving => Math.Abs(Velocity) > 1e-4f || Target.HasValue;

        float Clamp(float v) => Math.Min(Max, Math.Max(Min, v));

        /// <summary>제동거리 기반 허용 속도: 남은 거리 d에서 v ≤ √(2·a·d)</summary>
        public float AllowedSpeedToward(float boundDistance) =>
            (float)Math.Sqrt(2.0 * Accel * Math.Max(0f, boundDistance));

        public void Step(float dt)
        {
            if (dt <= 0f) return;

            // 1) 원하는 속도
            float desired;
            if (Target.HasValue)
            {
                float d = Target.Value - Position;
                float dir = Math.Sign(d);
                float cap = AllowedSpeedToward(Math.Abs(d));
                desired = dir * Math.Min(MaxSpeed, cap);
                if (Math.Abs(d) < 1e-3f && Math.Abs(Velocity) < Accel * dt)
                {
                    Position = Target.Value;
                    Velocity = 0f;
                    Target = null;
                    return;
                }
            }
            else
            {
                desired = Math.Clamp(Jog, -1f, 1f) * MaxSpeed;
            }

            // 2) 범위 끝 감속: 진행 방향 경계까지의 거리로 속도 상한
            if (desired > 0f) desired = Math.Min(desired, AllowedSpeedToward(Max - Position));
            if (desired < 0f) desired = -Math.Min(-desired, AllowedSpeedToward(Position - Min));

            // 3) 가속도 제한으로 속도 수렴
            float dv = desired - Velocity;
            float maxDv = Accel * dt;
            Velocity += Math.Clamp(dv, -maxDv, maxDv);

            // 4) 적분 + 하드 클램프
            Position += Velocity * dt;
            if (Position >= Max) { Position = Max; if (Velocity > 0) Velocity = 0; }
            if (Position <= Min) { Position = Min; if (Velocity < 0) Velocity = 0; }
        }
    }
}
