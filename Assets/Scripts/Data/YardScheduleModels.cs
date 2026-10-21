// 야드 스케줄 데이터 모델 (W-1)
// docs/data/yard_schedule_demo.json 과 1:1 — JsonUtility 호환(딕셔너리·DateTime 없음).
// 합성 데이터 전용: 실제 일정·허가가 아니다.

using System;
using System.Collections.Generic;

namespace YardAX.Data
{
    [Serializable] public class Vec3M { public float x, y, z; }
    [Serializable] public class SizeM { public float l, w, h; }

    [Serializable]
    public class Lug
    {
        public string id;
        public float xM, zM;
        public int swlTon;
    }

    [Serializable]
    public class BlockStage
    {
        public string stage;      // 절단→소조립→조립→선행의장→선행도장→PE→탑재
        public string start, end; // "yyyy-MM-dd"
        public int facilityId;    // 1~22
    }

    [Serializable]
    public class Block
    {
        public string id, name;
        public float weightTon;
        public SizeM sizeM;
        public Vec3M cogM;        // 무게중심
        public List<Lug> lugs;
        public bool needsTurnover;
        public List<BlockStage> stages;
    }

    [Serializable]
    public class Crew
    {
        public int count;
        public List<string> languages;
        public int newHires;
    }

    [Serializable]
    public class WorkOrder
    {
        public string id, date, process;
        public int facilityId;
        public string blockId;    // 없으면 ""
        public string start, end; // "HH:mm"
        public Crew crew;
        public List<string> equipment;
        public List<string> permits;
        public List<string> envFactors;
        public List<string> expectedRisks;
        public string notes;
    }

    [Serializable]
    public class EnvironmentHour
    {
        public string date;
        public int hour;
        public float tempC, humidity, windMs;
        public bool rain;
    }

    [Serializable]
    public class ProcessRisk
    {
        public string process;
        public int baseLevel;     // 1~3 (시연용 규칙)
        public List<string> risks;
    }

    [Serializable]
    public class ConflictTruth
    {
        public string id, date;
        public int facilityId;
        public List<string> workOrderIds;
        public int startH, endH;
        public string reason;
    }

    [Serializable]
    public class RequiredPermit
    {
        public string process;
        public List<string> permits;
    }

    [Serializable]
    public class YardScheduleData
    {
        public string note;
        public string demoDate;   // "2026-11-05"
        public List<string> stageOrder;
        public List<string> scheduleHierarchy; // 선표→대일정→중일정→소일정→블록 일정
        public List<RequiredPermit> requiredPermits;
        public List<Block> blocks;
        public List<WorkOrder> workOrders;
        public List<EnvironmentHour> environment;
        public List<ProcessRisk> processRisks;
        public List<ConflictTruth> conflicts;
    }
}
