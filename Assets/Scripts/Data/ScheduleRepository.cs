// ScheduleRepository (W-1) — 야드 스케줄 조회 계층
// JSON 텍스트를 받아 파싱한다 (TextAsset.text 또는 File.ReadAllText).
// 날짜는 "yyyy-MM-dd" 문자열 비교로 처리해 타임존 이슈를 피한다.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YardAX.Data
{
    public class ScheduleRepository
    {
        public YardScheduleData Data { get; }
        public DateTime DemoDate { get; }

        public ScheduleRepository(string json)
        {
            Data = JsonUtility.FromJson<YardScheduleData>(json);
            if (Data == null || Data.blocks == null)
                throw new ArgumentException("yard_schedule_demo.json 파싱 실패");
            DemoDate = ParseDate(Data.demoDate);
        }

        public static ScheduleRepository FromTextAsset(TextAsset asset) =>
            new ScheduleRepository(asset.text);

        public static DateTime ParseDate(string yyyyMMdd) =>
            DateTime.ParseExact(yyyyMMdd, "yyyy-MM-dd", null);

        static string Iso(DateTime d) => d.ToString("yyyy-MM-dd");

        /// <summary>해당 날짜의 작업지시 (기본: 시연일 "오늘 작업")</summary>
        public List<WorkOrder> GetTodayOrders(DateTime? date = null)
        {
            string d = Iso(date ?? DemoDate);
            return Data.workOrders.Where(w => w.date == d).ToList();
        }

        public Block GetBlock(string blockId) =>
            Data.blocks.FirstOrDefault(b => b.id == blockId);

        /// <summary>해당 날짜에 블록이 속한 단계. 단계 사이 간격이면 null</summary>
        public BlockStage GetCurrentStage(string blockId, DateTime? date = null)
        {
            var block = GetBlock(blockId);
            if (block == null) return null;
            string d = Iso(date ?? DemoDate);
            return block.stages.FirstOrDefault(
                s => string.Compare(s.start, d, StringComparison.Ordinal) <= 0
                  && string.Compare(d, s.end, StringComparison.Ordinal) <= 0);
        }

        /// <summary>단계 상태: 완료(-1) / 진행(0) / 예정(+1) — 일정 바 색 구분용</summary>
        public int StageStatus(BlockStage s, DateTime? date = null)
        {
            string d = Iso(date ?? DemoDate);
            if (string.Compare(s.end, d, StringComparison.Ordinal) < 0) return -1;
            if (string.Compare(d, s.start, StringComparison.Ordinal) < 0) return +1;
            return 0;
        }

        public EnvironmentHour GetEnvironment(int hour, DateTime? date = null)
        {
            string d = Iso(date ?? DemoDate);
            return Data.environment.FirstOrDefault(e => e.date == d && e.hour == hour);
        }

        public List<ConflictTruth> GetConflicts(DateTime? date = null)
        {
            string d = Iso(date ?? DemoDate);
            return Data.conflicts.Where(c => c.date == d).ToList();
        }

        /// <summary>공정에 필요한 허가 중 작업지시에 빠진 것 (RiskRuleEngine 입력)</summary>
        public List<string> GetMissingPermits(WorkOrder order)
        {
            var req = Data.requiredPermits.FirstOrDefault(r => r.process == order.process);
            if (req == null) return new List<string>();
            var have = order.permits ?? new List<string>();
            return req.permits.Where(p => !have.Contains(p)).ToList();
        }

        public ProcessRisk GetProcessRisk(string process) =>
            Data.processRisks.FirstOrDefault(p => p.process == process);
    }
}
