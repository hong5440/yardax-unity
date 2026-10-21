// W-5 움직이는 트랜스포터 — 경로(15번 PE장→주도로→1도크) 주행 + 이동 위험 표시.
// 위험 계산은 TransporterRisk(순수), 이 컴포넌트는 주행·시각화·게이지만.

using UnityEngine;
using YardAX.Performance;   // WorkerPath 재사용
using YardAX.Safety;
using YardAX.Viz;

namespace YardAX.Schedule
{
    [CreateAssetMenu(menuName = "YardAX/Transporter Config", fileName = "TransporterConfig")]
    public class TransporterConfig : ScriptableObject
    {
        public float speedMs = 2.0f;
        public float turnRadiusM = 12f;
        public float lookAheadS = 6f;
        public float cabEyeHeightM = 2.5f;
        public Vector3 blockSize = new Vector3(14f, 6f, 22f); // w,h,l (E3)
        public float slowFactor = 0.5f;    // 감속 권고 시 배율
    }

    public class TransporterController : MonoBehaviour
    {
        public TransporterConfig config;
        public WorkerPath path;            // 웨이포인트 경로 (Create Worker Path 재사용 가능)
        public bool rainLikely, night;     // W-1 환경에서 주입

        public TransporterRisk.Result Risk { get; private set; }
        public float T { get; private set; }

        Transform _body, _block;
        MeshRenderer _band;
        LineRenderer _swing;

        void Awake()
        {
            _body = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            Destroy(_body.GetComponent<Collider>());
            _body.name = "TransporterBody";
            _body.SetParent(transform, false);
            _body.localScale = new Vector3(config.blockSize.x * 0.9f, 1.2f, config.blockSize.z * 0.9f);
            _block = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            Destroy(_block.GetComponent<Collider>());
            _block.name = "TransporterLoad";
            _block.SetParent(transform, false);
            _block.localScale = config.blockSize;
            _block.localPosition = new Vector3(0, 1.2f + config.blockSize.y / 2f, 0);

            var band = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(band.GetComponent<Collider>());
            band.name = "TransporterBand";
            band.layer = ProjectionCamera.OverlayLayer;
            band.transform.SetParent(transform, false);
            band.transform.localRotation = Quaternion.Euler(90, 0, 0);
            _band = band.GetComponent<MeshRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            _band.sharedMaterial = new Material(sh) { color = new Color(0.2f, 0.9f, 0.4f, 0.35f) };
            _band.sharedMaterial.renderQueue = 3008;

            _swing = gameObject.AddComponent<LineRenderer>();
            _swing.positionCount = 0;
            _swing.widthMultiplier = 0.15f;
            _swing.material = _band.sharedMaterial;
        }

        void Update()
        {
            if (path == null || config == null) return;
            float speed = config.speedMs * (Risk.slowdownAdvised ? config.slowFactor : 1f);
            T = Mathf.Repeat(T + speed * Time.deltaTime / Mathf.Max(1f, path.Length), 1f);
            var pos = path.Position(T);
            var dir = path.Direction(T);
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir, Vector3.up));

            // 작업자 → 로컬 좌표
            var locals = new System.Collections.Generic.List<(float, float)>();
            if (WorkerRegistry.Instance != null)
                foreach (var w in WorkerRegistry.Instance.AllXZ())
                {
                    var l = transform.InverseTransformPoint(new Vector3(w.x, 0, w.y));
                    locals.Add((l.x, l.z));
                }

            Risk = TransporterRisk.Compute(new TransporterRisk.Input
            {
                speedMs = speed, lookAheadS = config.lookAheadS,
                blockW = config.blockSize.x, blockL = config.blockSize.z,
                blockH = config.blockSize.y, cabEyeHeight = config.cabEyeHeightM,
                rainLikely = rainLikely, night = night, workersLocal = locals,
            });

            // 진행 방향 위험 띠 (속도 비례)
            _band.transform.localPosition = new Vector3(0, -0.9f, config.blockSize.z / 2f + Risk.bandLenM / 2f);
            _band.transform.localScale = new Vector3(config.blockSize.x + 3f, Risk.bandLenM, 1f);
            _band.sharedMaterial.color = Risk.band switch
            {
                2 => new Color(1f, 0.25f, 0.2f, 0.45f),
                1 => new Color(1f, 0.8f, 0.2f, 0.4f),
                _ => new Color(0.2f, 0.9f, 0.4f, 0.3f),
            };

            // 회전 시 블록 끝단 스윙 궤적 (곡률 추정: 방향 변화율)
            var dirAhead = path.Direction(Mathf.Min(1f, T + 0.02f));
            float turn = Vector3.SignedAngle(dir, dirAhead, Vector3.up);
            if (Mathf.Abs(turn) > 1f)
            {
                float half = new Vector2(config.blockSize.x, config.blockSize.z).magnitude / 2f;
                int seg = 20;
                _swing.positionCount = seg;
                for (int i = 0; i < seg; i++)
                {
                    float a = (i / (seg - 1f) - 0.5f) * Mathf.Deg2Rad * 60f * Mathf.Sign(turn);
                    var p = transform.TransformPoint(
                        new Vector3(Mathf.Sin(a) * half, 0.1f, Mathf.Cos(a) * half));
                    _swing.SetPosition(i, new Vector3(p.x, 0.15f, p.z));
                }
            }
            else _swing.positionCount = 0;
        }

        void OnGUI() // 위험도 게이지 + 이유 1줄 (시연용 규칙 고지)
        {
            var r = new UnityEngine.Rect(Screen.width - 330, Screen.height - 86, 320, 76);
            GUI.Box(r, "");
            GUI.Label(new UnityEngine.Rect(r.x + 10, r.y + 6, 300, 22),
                $"트랜스포터 위험도 {Risk.score}/100" + (Risk.slowdownAdvised ? " · 감속 권고" : ""));
            GUI.color = Risk.band == 2 ? Color.red : Risk.band == 1 ? Color.yellow : Color.green;
            GUI.Box(new UnityEngine.Rect(r.x + 10, r.y + 30, 3f * Risk.score, 10), "");
            GUI.color = Color.white;
            GUI.Label(new UnityEngine.Rect(r.x + 10, r.y + 44, 300, 22),
                Risk.reason + "  (시연용 규칙)");
        }
    }
}
