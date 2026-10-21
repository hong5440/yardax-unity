// 턴오버 순수 계산 (G-4) — EditMode 테스트 대상.

using System;

namespace YardAX.Goliath
{
    public static class TurnoverMath
    {
        /// <summary>회전 포락선 반경: 회전 평면(블록 z×h 단면) 반대각 + 여유</summary>
        public static float EnvelopeRadius(float sizeZ, float sizeH, float marginM = 2f) =>
            (float)Math.Sqrt(sizeZ * sizeZ + sizeH * sizeH) / 2f + marginM;

        /// <summary>회전 중 상부(2후크)·하부(1후크) 하중 분담비 (시뮬레이션 근사).
        /// 0°: 상부 1.0 / 하부 0.0 → 90°: 상부 0.5 / 하부 0.5 → 180°: 상부 1.0 / 0.0</summary>
        public static (float upper, float lower) LoadShare(float tiltDeg)
        {
            double rad = Math.PI * tiltDeg / 180.0;
            float lower = (float)(0.5 * Math.Sin(Math.Min(rad, Math.PI)));
            lower = Math.Clamp(lower, 0f, 0.5f);
            return (1f - lower, lower);
        }
    }
}
