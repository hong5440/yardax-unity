// W-5 트랜스포터 이동 위험 — 순수 계산 (EditMode 테스트 대상).
// 진행 방향 위험 띠(속도 비례)·시야 가림 사각·보행 교차·환경 감속 권고·0~100 점수.
// ※ 시연용 규칙.

using System;
using System.Collections.Generic;

namespace YardAX.Schedule
{
    public static class TransporterRisk
    {
        /// <summary>진행 방향 위험 띠 길이 = 속도 × N초 (최소 3m)</summary>
        public static float BandLength(float speedMs, float lookAheadS) =>
            Math.Max(3f, speedMs * lookAheadS);

        /// <summary>
        /// 운전석 사각(블록 뒤쪽): 트랜스포터 로컬에서 블록 뒤 blindDepth 만큼.
        /// 로컬 z+ = 진행 방향. 블록 높이가 cabEyeH 이상이면 뒤가 안 보인다.
        /// </summary>
        public static bool InBlindZone(float lx, float lz, float blockW, float blockL,
                                       float blockH, float cabEyeHeight, float blindDepth = 15f)
        {
            if (blockH < cabEyeHeight) return false;      // 블록 너머로 보임
            return Math.Abs(lx) <= blockW / 2f + 1f && lz < -blockL / 2f && lz > -blockL / 2f - blindDepth;
        }

        public struct Input
        {
            public float speedMs, lookAheadS;
            public float blockW, blockL, blockH, cabEyeHeight;
            public bool rainLikely, night;
            /// <summary>트랜스포터 로컬 작업자 좌표 (x=횡, z=진행)</summary>
            public IReadOnlyList<(float x, float z)> workersLocal;
        }

        public struct Result
        {
            public int score;            // 0~100
            public string reason;        // 이유 1줄
            public float bandLenM;
            public int band;             // 0 초록 / 1 노랑 / 2 빨강
            public bool slowdownAdvised; // 강수·야간 감속 권고
            public float crossingEtaS;   // 띠 안 작업자 도달 예상 (없으면 -1)
        }

        public static Result Compute(Input i)
        {
            float band = BandLength(i.speedMs, i.lookAheadS);
            int score = 10;
            string reason = "정상 주행";
            float eta = -1f;

            foreach (var w in i.workersLocal ?? Array.Empty<(float, float)>())
            {
                bool inBand = Math.Abs(w.x) <= i.blockW / 2f + 1.5f && w.z > 0 && w.z <= band;
                if (inBand)
                {
                    float d = w.z;
                    float t = i.speedMs > 0.1f ? d / i.speedMs : 99f;
                    if (eta < 0 || t < eta) eta = t;
                    score += 45;
                    reason = $"보행 교차 — 전방 {d:0.0}m, 도달 {t:0.0}s";
                }
                else if (InBlindZone(w.x, w.z, i.blockW, i.blockL, i.blockH, i.cabEyeHeight))
                {
                    score += 25;
                    if (score < 60) reason = "시야 가림 사각에 작업자";
                }
            }
            bool slow = i.rainLikely || i.night;
            if (slow)
            {
                score += 10;
                if (score < 40) reason = i.rainLikely ? "강수 — 감속 권고" : "야간 — 감속 권고";
            }
            score = Math.Min(100, score);
            return new Result
            {
                score = score, reason = reason, bandLenM = band,
                band = score >= 60 ? 2 : score >= 35 ? 1 : 0,
                slowdownAdvised = slow, crossingEtaS = eta,
            };
        }
    }
}
