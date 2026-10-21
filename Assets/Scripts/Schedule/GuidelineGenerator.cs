// W-6 AI 작업안전지침 — 규칙 결과를 카드로 문장화.
// LLM 경로: Yard Hub 경유 guideline_request 송신 → 응답 대기(3s) → 실패 시 템플릿.
// 카드: 한 줄 판단 / 작업 전 확인 3 / 작업 중 주의 3 / 즉시 중지 기준 2 / 근거.
// 언어: ko·vi·en 템플릿 보유, ne·uz는 LLM 응답 시 전체 번역(템플릿 폴백은 en).

using System;
using System.Collections.Generic;
using YardAX.Data;

namespace YardAX.Schedule
{
    public enum GuideLang { ko, vi, en, ne, uz }

    public struct GuidelineCard
    {
        public string verdict;          // 한 줄 판단
        public string[] beforeWork;     // 작업 전 확인 3
        public string[] duringWork;     // 작업 중 주의 3
        public string[] stopCriteria;   // 즉시 중지 기준 2
        public string evidence;         // 근거 (요인 요약)
        public bool fromLlm;            // false = 템플릿 대체
    }

    public static class GuidelineGenerator
    {
        static readonly Dictionary<GuideLang, (string high, string mid, string low)> Verdict = new()
        {
            [GuideLang.ko] = ("위험도가 높습니다 — 보강 조치 후 작업하세요.",
                              "주의가 필요한 작업입니다.", "표준 수칙으로 진행 가능합니다."),
            [GuideLang.vi] = ("Mức rủi ro cao — chỉ làm việc sau khi bổ sung biện pháp.",
                              "Công việc cần chú ý.", "Có thể tiến hành theo quy tắc chuẩn."),
            [GuideLang.en] = ("High risk — reinforce controls before starting.",
                              "Caution required for this task.", "Proceed under standard rules."),
        };

        static readonly Dictionary<GuideLang, string[]> BaseBefore = new()
        {
            [GuideLang.ko] = new[] { "작업허가서와 서명 확인", "보호구(안전모·안전대) 착용 확인", "작업 구역 통제선·신호수 배치 확인" },
            [GuideLang.vi] = new[] { "Kiểm tra giấy phép và chữ ký", "Kiểm tra PPE (mũ, dây an toàn)", "Kiểm tra rào chắn và người ra hiệu" },
            [GuideLang.en] = new[] { "Verify permit and signatures", "Check PPE (helmet, harness)", "Confirm barricades and signaler" },
        };
        static readonly Dictionary<GuideLang, string[]> BaseDuring = new()
        {
            [GuideLang.ko] = new[] { "하중·장비 아래 통행 금지", "지시는 복명복창으로 확인", "이상 발견 시 즉시 보고" },
            [GuideLang.vi] = new[] { "Không đi dưới tải/thiết bị", "Nhắc lại khẩu lệnh để xác nhận", "Báo ngay khi có bất thường" },
            [GuideLang.en] = new[] { "Never walk under the load", "Read back all commands", "Report anomalies immediately" },
        };
        static readonly Dictionary<GuideLang, string[]> BaseStop = new()
        {
            [GuideLang.ko] = new[] { "작업자가 위험 영역에 진입하면 즉시 중지", "강풍·기상 급변 시 즉시 중지" },
            [GuideLang.vi] = new[] { "Dừng ngay nếu có người vào vùng nguy hiểm", "Dừng ngay khi gió mạnh/thời tiết xấu" },
            [GuideLang.en] = new[] { "Stop if anyone enters the danger zone", "Stop on strong wind or sudden weather" },
        };

        /// <summary>템플릿 지침 (LLM 실패 시 대체 경로)</summary>
        public static GuidelineCard FromTemplate(WorkOrder order, IReadOnlyList<RiskFactor> factors,
                                                 int score, GuideLang lang)
        {
            var l = Verdict.ContainsKey(lang) ? lang : GuideLang.en;
            var v = Verdict[l];
            var before = (string[])BaseBefore[l].Clone();
            var during = (string[])BaseDuring[l].Clone();
            var stop = (string[])BaseStop[l].Clone();

            // 요인 맞춤 한 줄씩 교체 (ko 기준 문구, 타 언어는 공통 문구 유지)
            foreach (var f in factors)
            {
                if (l != GuideLang.ko) break;
                if (f.name == "허가 누락") before[0] = "누락된 허가 발급 전 작업 금지: " + f.reason;
                if (f.name == "작업 간섭") during[0] = "간섭 작업과 무전 교신 유지 — " + f.reason;
                if (f.name == "강풍") stop[1] = "풍속 임계 초과 시 즉시 중지 (법령·사내 기준 확인 필요)";
                if (f.name == "언어 불일치") during[1] = "다국어 조: 지시는 그림·손신호 병행, 복명복창 확인";
            }

            var ev = new List<string>();
            foreach (var f in factors) ev.Add($"{f.name}+{f.points}");
            return new GuidelineCard
            {
                verdict = $"[{score}/100] " + (score >= 60 ? v.high : score >= 35 ? v.mid : v.low),
                beforeWork = before, duringWork = during, stopCriteria = stop,
                evidence = $"{order.process} · " + string.Join(", ", ev) + " (시연용 규칙)",
                fromLlm = false,
            };
        }
    }
}
