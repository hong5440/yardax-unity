// W-6 RiskRuleEngine — 규칙 기반 (LLM 아님). 순수 계산, EditMode 테스트 대상.
// 간섭·허가 누락·환경 임계값·신입 비율·언어 불일치·블록 중량/반전으로
// 위험 요인 목록과 0~100 점수. ※ 시연용 규칙 — UI에 상시 고지.

using System;
using System.Collections.Generic;
using YardAX.Data;

namespace YardAX.Schedule
{
    public struct RiskFactor
    {
        public string name;
        public int points;
        public string reason;
    }

    public struct RiskInput
    {
        public WorkOrder order;
        public int conflictCount;           // 이 작업이 낀 간섭 수
        public IReadOnlyList<string> missingPermits;
        public float windMs, tempC;
        public bool rain, night;
        public Block block;                 // 없으면 null
        public ProcessRisk processRisk;     // 없으면 null
        public RiskThresholds thresholds;
    }

    [Serializable]
    public struct RiskThresholds   // ScriptableObject(RiskConfig)가 채움
    {
        public float windWarnMs;   // 기본 15
        public float heavyBlockTon;// 기본 400
        public float newHireRatio; // 기본 0.2
        public static RiskThresholds Default => new()
        { windWarnMs = 15f, heavyBlockTon = 400f, newHireRatio = 0.2f };
    }

    public static class RiskRuleEngine
    {
        /// <summary>2025 조선업 재해자 유형별 수치 (출처: 고용노동부, 이투데이 2026.5.27)</summary>
        public static readonly (string type, int count)[] IndustryFrequency =
        {
            ("넘어짐", 294), ("부딪힘", 239), ("끼임", 221),
            ("불균형·무리한 동작", 187), ("떨어짐", 171), ("맞음", 169),
        };

        public static List<RiskFactor> Evaluate(RiskInput i, out int score)
        {
            var f = new List<RiskFactor>();
            var th = i.thresholds.windWarnMs <= 0 ? RiskThresholds.Default : i.thresholds;

            if (i.conflictCount > 0)
                f.Add(new RiskFactor { name = "작업 간섭",
                    points = Math.Min(40, 25 * i.conflictCount),
                    reason = $"같은 시간·인접 장소 간섭 {i.conflictCount}건" });
            if (i.missingPermits != null && i.missingPermits.Count > 0)
                f.Add(new RiskFactor { name = "허가 누락",
                    points = 20 * i.missingPermits.Count,
                    reason = "누락: " + string.Join(", ", i.missingPermits) });
            if (i.windMs >= th.windWarnMs)
                f.Add(new RiskFactor { name = "강풍", points = 15,
                    reason = $"풍속 {i.windMs:0.0}m/s ≥ {th.windWarnMs}m/s (법령·사내 기준 확인 필요)" });
            if (i.rain)
                f.Add(new RiskFactor { name = "강수", points = 10, reason = "우천 — 미끄럼·시계 저하" });
            if (i.night)
                f.Add(new RiskFactor { name = "야간", points = 8, reason = "야간 작업 — 시인성 저하" });
            var crew = i.order?.crew;
            if (crew != null && crew.count > 0 && (float)crew.newHires / crew.count >= th.newHireRatio)
                f.Add(new RiskFactor { name = "신입 비율", points = 10,
                    reason = $"신입 {crew.newHires}/{crew.count}명" });
            if (crew != null && crew.languages != null && crew.languages.Count > 1)
                f.Add(new RiskFactor { name = "언어 불일치", points = 8,
                    reason = "사용 언어 " + string.Join("/", crew.languages) });
            if (i.block != null && i.block.weightTon >= th.heavyBlockTon)
                f.Add(new RiskFactor { name = "중량 블록", points = 10,
                    reason = $"{i.block.id} {i.block.weightTon}t" });
            if (i.block != null && i.block.needsTurnover)
                f.Add(new RiskFactor { name = "반전(턴오버) 블록", points = 8,
                    reason = "회전 포락선·동시 하중 관리 필요" });
            if (i.processRisk != null)
                f.Add(new RiskFactor { name = "공정 기본 위험",
                    points = i.processRisk.baseLevel * 5,
                    reason = $"{i.order.process} — {string.Join(", ", i.processRisk.risks)}" });

            score = 0;
            foreach (var x in f) score += x.points;
            score = Math.Min(100, score);
            return f;
        }
    }
}
