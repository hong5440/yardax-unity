// F-2 설정 — 모든 수치는 SO.

using UnityEngine;

namespace YardAX.Performance
{
    [CreateAssetMenu(menuName = "YardAX/First Person Performance Config",
        fileName = "FirstPersonPerformanceConfig")]
    public class FirstPersonPerformanceConfig : ScriptableObject
    {
        [Header("카메라")]
        public float eyeHeightM = 1.65f;
        public float fov = 70f;
        [Tooltip("관제 → 1인칭 전환 시간 (s)")]
        public float transitionS = 3f;
        [Tooltip("멀미 방지 — 카메라 각속도 상한 (deg/s)")]
        public float maxTurnDegPerSec = 60f;

        [Header("걷기")]
        public float walkSpeed = 1.3f;
        [Range(0f, 1f)] public float bobStrength = 0.35f; // 0 = 끔
        public float bobFrequency = 1.8f;

        [Header("고개 들기")]
        [Range(0f, 1f)] public float lookUpAtPathT = 0.75f; // 경로 진행도
        public float lookUpDurationS = 2.5f;
        public float lookUpPitchDeg = -55f; // 위를 본다 (음수 pitch)

        [Header("안전모 챙 그림자")]
        [Range(0f, 0.4f)] public float capShadowStrength = 0.12f;
    }
}
