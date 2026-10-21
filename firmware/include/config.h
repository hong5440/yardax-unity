#pragma once
// ─────────────────────────────────────────────────────────
// 미니 골리앗 하드웨어 설정 — 핀 번호와 환산 계수는 전부 여기서 수정
// ─────────────────────────────────────────────────────────

// 스테퍼 (STEP/DIR 드라이버, 예: A4988/DRV8825)
#define PIN_GANTRY_L_STEP 25
#define PIN_GANTRY_L_DIR 26
#define PIN_GANTRY_R_STEP 27
#define PIN_GANTRY_R_DIR 14
#define PIN_TROLLEY_STEP 32
#define PIN_TROLLEY_DIR 33

// 권상 서보 (연속 회전이 아닌 각도 서보 + 윈치 드럼)
#define PIN_HOIST_SERVO 13

// 리밋 스위치 4개 (INPUT_PULLUP, 눌리면 LOW)
#define PIN_LIMIT_GANTRY_MIN 34
#define PIN_LIMIT_GANTRY_MAX 35
#define PIN_LIMIT_TROLLEY_MIN 36
#define PIN_LIMIT_TROLLEY_MAX 39

// 비상정지 — NC 체인 (정상 = GND로 닫힘 = LOW, 열리면 HIGH → 즉시 정지)
#define PIN_ESTOP 15

// 스텝 ↔ 거리 환산 (장치 실측으로 보정)
#define STEPS_PER_CM_GANTRY 80.0f
#define STEPS_PER_CM_TROLLEY 100.0f

// 권상: 서보 각도 ↔ 후크 높이
#define HOIST_DEG_PER_CM 6.0f
#define HOIST_MIN_DEG 10
#define HOIST_MAX_DEG 170
#define HOIST_HOME_DEG 90

// 속도·가속도 (스텝/s, 스텝/s^2)
#define GANTRY_MAX_SPEED 1600.0f
#define GANTRY_ACCEL 800.0f
#define TROLLEY_MAX_SPEED 1200.0f
#define TROLLEY_ACCEL 700.0f

// 소프트 리밋 (cm) — 리밋 스위치 이전의 논리 범위
#define GANTRY_RANGE_CM 120.0f
#define TROLLEY_RANGE_CM 40.0f

// 통신
#define SERIAL_BAUD 115200
#define HEARTBEAT_TIMEOUT_MS 500   // 이 시간 이상 heartbeat 없으면 정지
#define STATE_REPORT_MS 50         // crane_state 보고 주기
#define CRANE_ID "goliath-mini"
