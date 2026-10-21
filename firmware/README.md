# 미니 골리앗 크레인 펌웨어 (ESP32 · PlatformIO)

실물 미니 골리앗 모형의 제어 펌웨어. Yard Hub(Python 브리지)가 시리얼로
`docs/protocol.md` 형식의 JSON을 중계한다.

## 하드웨어

| 축 | 구동 | 비고 |
|---|---|---|
| 주행 (GANTRY) | 스테퍼 ×2 (좌/우) | 같은 목표로 **동기** 구동, 동기 오차 보고 |
| 횡행 (TROLLEY) | 스테퍼 ×1 | |
| 권상 (HOIST) | 각도 서보 + 윈치 드럼 | cm → 각도 환산(`HOIST_DEG_PER_CM`) |

- 리밋 스위치 4개: 주행 min/max, 횡행 min/max (`INPUT_PULLUP`, 눌리면 LOW)
- **비상정지 NC 체인**: 정상 시 GND로 닫힘(LOW). 하나라도 열리면 즉시 전축 정지
- 핀 번호·스텝당 거리·속도·범위는 전부 `include/config.h` 에서 수정

## 안전 규칙 (펌웨어 자체 보장)

1. 비상정지 열림 → 즉시 정지 + `fault` 보고
2. `heartbeat` 가 **500ms** 이상 끊기면 정지 (`HEARTBEAT_TIMEOUT_MS`)
3. 리밋 스위치 방향으로 진행 중이면 해당 정지
4. `interlock {active:true}` 수신 → 전축 정지, 해제 전 신규 이동 거부
5. heartbeat를 한 번도 받지 못한 상태에서는 어떤 이동 명령도 실행하지 않음

## 시리얼 프로토콜 (115200bps, 한 줄 = JSON 1개)

수신 (Hub → 펌웨어):
```json
{"type":"heartbeat","ts":0}
{"type":"operator_input","axis":"GANTRY","dir":"+","amount":20}
{"type":"interlock","active":true,"reason":"worker_under_load","ts":0}
{"type":"fault","reason":"..."}
```
`axis`: `GANTRY` | `TROLLEY` | `HOIST`, `amount` 단위는 cm.

송신 (펌웨어 → Hub, 50ms 주기):
```json
{"type":"crane_state","crane":"goliath-mini","travel_cm":12.5,"trolley_cm":3.0,
 "hook_cm":8.2,"sync_err_steps":0,"estop":false,"interlock":false,"ts":12345}
```
정지 사유 발생 시 `{"type":"fault","reason":"estop_open"}` 등을 즉시 보고.

## 빌드·업로드

```bash
cd firmware
pio run                 # 빌드
pio run -t upload       # 업로드 (포트 자동 감지)
pio device monitor -b 115200
```

## 보정 순서

1. `STEPS_PER_CM_GANTRY/TROLLEY`: 10cm 이동 명령 후 실측해 보정
2. `HOIST_DEG_PER_CM`: 후크 10cm 승강 기준으로 보정
3. 리밋 스위치 위치에서 `GANTRY_RANGE_CM`/`TROLLEY_RANGE_CM` 소프트 리밋 확정
