// 골리앗 위험 판단 게이트 (G-5) — OperatorInputRouter에 등록된다.
// - 주행 위험: 진행 방향 레일 구간(주행 위험 구간)에 작업자 → 그 방향 주행 차단
// - 하중 아래: Swept Area(예측)와 작업자 원형 범위가 겹침 → 해당 축·방향 차단
// - 강풍: 풍속 ≥ 임계값 → 주행 "경고" (차단 아님). 임계값 옆 "법령·사내 기준 확인 필요"
// 판단은 순수 계산(SweptGeometry)으로, 게이트는 조회·조립만 한다.

using UnityEngine;
using YardAX.Config;
using YardAX.Core;
using YardAX.Goliath;
using YardAX.Viz;

namespace YardAX.Safety
{
    /// <summary>풍속 공급 (시뮬 슬라이더 또는 실시간 기상)</summary>
    public class WindProvider : MonoBehaviour
    {
        [Range(0f, 40f)] public float windMs = 5f;
        public bool fromLiveWeather; // true면 외부(W-wave 환경 데이터)가 갱신
    }

    public class GantryPathGate : IRiskGate
    {
        readonly GoliathController _c;
        readonly GoliathCraneConfig _cfg;
        readonly Transform _railFrame;

        public GantryPathGate(GoliathController c, Transform railFrame)
        { _c = c; _cfg = c.config; _railFrame = railFrame; }

        public bool Allow(CraneCommandV2 cmd, out string reason)
        {
            reason = null;
            if (cmd.axis != CraneAxis.GANTRY) return true;
            var reg = WorkerRegistry.Instance;
            if (reg == null) return true;
            float z = _c.State.GetPosition(CraneAxis.GANTRY);
            var strip = SweptGeometry.GantryDangerStrip(
                z, cmd.direction, _cfg.gantryDangerStripM, _cfg.spanM, 7f);
            bool hit = reg.Any(p =>
            {
                var local = _railFrame.InverseTransformPoint(new Vector3(p.x, 0, p.y));
                return strip.Contains(local.x, local.z, margin: 1f);
            });
            if (hit)
            {
                reason = $"주행 위험 구간({(cmd.direction > 0 ? "전진" : "후진")} {_cfg.gantryDangerStripM}m)에 작업자·장비 — 주행 차단";
                return false;
            }
            return true;
        }
    }

    public class LoadBelowGate : IRiskGate
    {
        readonly GoliathSweptArea _swept;
        public float workerRadiusM = 3f;

        public LoadBelowGate(GoliathSweptArea swept) => _swept = swept;

        public bool Allow(CraneCommandV2 cmd, out string reason)
        {
            reason = null;
            var reg = WorkerRegistry.Instance;
            if (reg == null || _swept == null) return true;
            // 이 명령의 예상 경로를 먼저 계산해 보고 겹침 검사
            _swept.Recalculate(cmd.axis, cmd.direction * cmd.amountM);
            bool hit = reg.Any(p => _swept.Contains(p));
            if (hit)
            {
                reason = $"하중 이동 영역에 작업자(반경 {workerRadiusM}m) — {cmd.axis} {(cmd.direction > 0 ? "+" : "-")} 차단";
                return false;
            }
            return true;
        }
    }

    /// <summary>강풍은 차단이 아니라 경고 — Allow는 true, 타임라인에 경고만</summary>
    public class WindWarnGate : IRiskGate
    {
        readonly WindProvider _wind;
        readonly GoliathCraneConfig _cfg;
        readonly WarningTimeline _timeline;

        public WindWarnGate(WindProvider wind, GoliathCraneConfig cfg, WarningTimeline tl)
        { _wind = wind; _cfg = cfg; _timeline = tl; }

        public bool Allow(CraneCommandV2 cmd, out string reason)
        {
            reason = null;
            if (cmd.axis == CraneAxis.GANTRY && _wind != null && _wind.windMs >= _cfg.windWarnMs)
                _timeline?.Add("warn",
                    $"강풍 {_wind.windMs:0.0}m/s ≥ 임계 {_cfg.windWarnMs}m/s — 주행 주의 (법령·사내 기준 확인 필요)");
            return true;
        }
    }

    /// <summary>게이트 일괄 설치 — 씬 부트스트랩에서 1회 호출</summary>
    public class GoliathRiskSetup : MonoBehaviour
    {
        public OperatorInputRouter router;
        public GoliathController goliath;
        public GoliathSweptArea swept;
        public WindProvider wind;
        public WarningTimeline timeline;

        void Start()
        {
            if (router == null || goliath == null) return;
            Transform railFrame = swept != null ? swept.transform : goliath.transform;
            router.RegisterGate(new GantryPathGate(goliath, railFrame));
            if (swept != null) router.RegisterGate(new LoadBelowGate(swept));
            if (wind != null) router.RegisterGate(new WindWarnGate(wind, goliath.config, timeline));
        }
    }
}
