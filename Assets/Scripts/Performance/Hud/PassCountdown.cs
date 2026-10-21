// F-4 카운트다운 순수 계산 — EditMode 테스트 대상.
// 예상 경로와 현재 속도로 "하중이 작업자 위치를 통과할 때까지 N초".

using System;

namespace YardAX.Performance.Hud
{
    public static class PassCountdown
    {
        /// <summary>
        /// 1차원(이동 축) 계산: 하중 전단이 작업자 위치에 도달할 때까지 초.
        /// loadPos: 하중 중심, loadHalf: 이동 방향 반길이, workerPos: 작업자,
        /// velocity: 부호 있는 속도. 접근 중이 아니면 -1.
        /// </summary>
        public static float SecondsToReach(float loadPos, float loadHalf,
                                           float workerPos, float velocity)
        {
            if (Math.Abs(velocity) < 1e-4f) return -1f;
            float edge = loadPos + Math.Sign(velocity) * loadHalf; // 진행 방향 전단
            float dist = (workerPos - edge) * Math.Sign(velocity);
            if (dist <= 0f) return 0f;               // 이미 전단이 지나는 중
            return dist / Math.Abs(velocity);
        }
    }
}
