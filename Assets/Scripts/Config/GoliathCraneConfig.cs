// 골리앗 크레인 설정 (G-1) — 모든 수치는 여기서만.

using UnityEngine;

namespace YardAX.Config
{
    [CreateAssetMenu(menuName = "YardAX/Goliath Crane Config", fileName = "GoliathCraneConfig")]
    public class GoliathCraneConfig : ScriptableObject
    {
        [Header("기하 (m)")]
        public float spanM = 120f;          // 경간 (다리 사이)
        public float heightM = 70f;         // 거더 밑면 높이
        public float girderDepthM = 8f;     // 거더 폭(레일 방향)
        public float girderHeightM = 9f;

        [Header("주행 (GANTRY)")]
        public float gantryRangeM = 160f;   // 레일 길이 (0 ~ range)
        public float gantrySpeed = 1.2f;    // m/s
        public float gantryAccel = 0.4f;    // m/s^2

        [Header("상부 트롤리 A·B (거더 위)")]
        public float upperTrolleyMinM = -52f;
        public float upperTrolleyMaxM = 52f;
        public float upperTrolleySpeed = 1.0f;
        public float upperTrolleyAccel = 0.5f;

        [Header("하부 트롤리 L (거더 아래)")]
        public float lowerTrolleyMinM = -45f;
        public float lowerTrolleyMaxM = 45f;
        public float lowerTrolleySpeed = 0.8f;
        public float lowerTrolleyAccel = 0.4f;

        [Header("권상 (지면 기준 후크 높이)")]
        public float hoistMinM = 1.5f;
        public float hoistMaxM = 62f;
        public float hoistSpeed = 0.6f;
        public float hoistAccel = 0.3f;

        [Header("동기 모드")]
        public float syncToleranceM = 0.25f;   // A·B 높이/위치 차 허용 오차

        [Header("주행 위험 구간 (다리·바퀴 전방 레일, m)")]
        public float gantryDangerStripM = 12f;

        [Header("환경 경고 (값은 시연용 — 법령·사내 기준 확인 필요)")]
        public float windWarnMs = 15f;         // 주행 경고 풍속

        [Header("적재 블록 기본 크기 (m)")]
        public Vector3 loadBlockSize = new Vector3(22f, 6f, 14f);

        [Header("씬 배치 (1도크 레일 원점·방향)")]
        public Vector3 railOriginWorld = Vector3.zero;
        public float railYawDeg = 0f;
    }
}
