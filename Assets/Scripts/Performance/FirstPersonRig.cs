// F-2 시네마틱 1인칭 리그 — 경로 따라 걷기, 걸음 흔들림(끔 가능),
// 정해진 지점에서 천천히 고개 들기, 관제→1인칭 약 3초 전환, 안전모 챙 그림자.
// 멀미 방지: 모든 회전은 config.maxTurnDegPerSec 상한.

using UnityEngine;
using YardAX.Safety;

namespace YardAX.Performance
{
    public class FirstPersonRig : MonoBehaviour
    {
        public FirstPersonPerformanceConfig config;
        public WorkerPath path;
        public Camera cam;                 // 메인 카메라를 빌려 쓴다

        public enum Phase { Idle, Descend, Walking, LookUp, Hold }
        public Phase CurrentPhase { get; private set; } = Phase.Idle;
        public float PathT { get; private set; }

        Vector3 _ctrlPos; Quaternion _ctrlRot; float _ctrlFov;
        float _phaseT, _bobT;
        Quaternion _targetRot;
        Texture2D _capTex;
        Transform _body;                   // 작업자 몸 (WorkerRegistry 등록)

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            _body = new GameObject("PerformanceWorkerBody").transform;
            _capTex = new Texture2D(1, 2);
            _capTex.SetPixels(new[] { new Color(0, 0, 0, 1f), new Color(0, 0, 0, 0f) });
            _capTex.Apply();
        }

        void OnEnable() { if (WorkerRegistry.Instance != null) WorkerRegistry.Instance.Register(_body); }
        void OnDisable() { if (WorkerRegistry.Instance != null) WorkerRegistry.Instance.Unregister(_body); }

        /// <summary>관제 시점에서 전환 시작 (F-3 ②)</summary>
        public void BeginDescend()
        {
            if (cam == null || path == null) return;
            _ctrlPos = cam.transform.position;
            _ctrlRot = cam.transform.rotation;
            _ctrlFov = cam.fieldOfView;
            PathT = 0f; _phaseT = 0f;
            CurrentPhase = Phase.Descend;
        }

        public void ResetToStart() { CurrentPhase = Phase.Idle; PathT = 0f; }

        /// <summary>관제 카메라로 복귀</summary>
        public void ReturnToControl()
        {
            CurrentPhase = Phase.Idle;
            if (cam != null)
            {
                cam.transform.SetPositionAndRotation(_ctrlPos, _ctrlRot);
                cam.fieldOfView = _ctrlFov;
            }
        }

        void Update()
        {
            if (CurrentPhase == Phase.Idle || cam == null || path == null || config == null) return;
            float dt = Time.deltaTime;

            Vector3 eye = path.Position(PathT) + Vector3.up * config.eyeHeightM;
            Vector3 fwd = path.Direction(PathT);
            _body.position = path.Position(PathT);

            switch (CurrentPhase)
            {
                case Phase.Descend: // 머리 위로 내려와 1인칭으로 (약 3초)
                {
                    _phaseT += dt / Mathf.Max(0.5f, config.transitionS);
                    float s = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_phaseT));
                    cam.transform.position = Vector3.Lerp(_ctrlPos, eye, s);
                    var want = Quaternion.LookRotation(fwd, Vector3.up);
                    cam.transform.rotation = RotateLimited(cam.transform.rotation,
                        Quaternion.Slerp(_ctrlRot, want, s), dt);
                    cam.fieldOfView = Mathf.Lerp(_ctrlFov, config.fov, s);
                    if (_phaseT >= 1f) { CurrentPhase = Phase.Walking; _phaseT = 0f; }
                    break;
                }
                case Phase.Walking:
                {
                    PathT += config.walkSpeed * dt / Mathf.Max(1f, path.Length);
                    _bobT += dt * config.bobFrequency * Mathf.PI * 2f;
                    float bob = config.bobStrength > 0f
                        ? Mathf.Sin(_bobT) * 0.035f * config.bobStrength : 0f;
                    cam.transform.position = eye + Vector3.up * bob;
                    cam.transform.rotation = RotateLimited(cam.transform.rotation,
                        Quaternion.LookRotation(fwd, Vector3.up), dt);
                    if (PathT >= config.lookUpAtPathT) { CurrentPhase = Phase.LookUp; _phaseT = 0f; }
                    break;
                }
                case Phase.LookUp: // 천천히 고개 들어 머리 위 블록 주시
                {
                    _phaseT += dt / Mathf.Max(0.5f, config.lookUpDurationS);
                    float s = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_phaseT));
                    cam.transform.position = eye;
                    var level = Quaternion.LookRotation(fwd, Vector3.up);
                    var up = Quaternion.Euler(config.lookUpPitchDeg, level.eulerAngles.y, 0);
                    cam.transform.rotation = RotateLimited(cam.transform.rotation,
                        Quaternion.Slerp(level, up, s), dt);
                    if (_phaseT >= 1f) CurrentPhase = Phase.Hold;
                    break;
                }
                case Phase.Hold:
                    cam.transform.position = eye;
                    break;
            }
        }

        Quaternion RotateLimited(Quaternion from, Quaternion to, float dt) =>
            Quaternion.RotateTowards(from, to, config.maxTurnDegPerSec * dt);

        void OnGUI() // 안전모 챙 그림자 (화면 위 가장자리, 아주 옅게)
        {
            if (CurrentPhase == Phase.Idle || config.capShadowStrength <= 0f) return;
            var c = GUI.color;
            GUI.color = new Color(0, 0, 0, config.capShadowStrength);
            GUI.DrawTexture(new UnityEngine.Rect(0, 0, Screen.width, Screen.height * 0.12f), _capTex,
                ScaleMode.StretchToFill);
            GUI.color = c;
        }
    }
}
