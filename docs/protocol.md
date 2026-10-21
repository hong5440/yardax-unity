# 크레인 LIVE 브리지 프로토콜 (B-5)

웹 관제(아차사고 레이더)와 실물 크레인 모형 브리지(Python) 사이의
WebSocket JSON 프로토콜. 기본 주소는 `ws://localhost:8765`
(`NEXT_PUBLIC_CRANE_WS` 환경변수로 변경).

모든 메시지는 한 줄 JSON 객체이며 `type` 필드로 구분한다.
시각은 Unix epoch 밀리초(`ts`).

## 브리지 → 웹 (수신)

### `crane_state` — 크레인 축 상태 (10Hz 권장)
```json
{ "type": "crane_state", "travel_m": 12.5, "trolley_m": -3.0, "hook_m": 18.2, "ts": 0 }
```
- `travel_m`: 레일 방향 주행 위치 (기준점 0, 전진 +)
- `trolley_m`: 거더 방향 트롤리 위치 (거더 중앙 0, 우측 +)
- `hook_m`: 후크 높이 (지면 기준)

### `worker` — 작업자 위치 (태그/비전 추적)
```json
{ "type": "worker", "id": "w-01", "x_m": 20.0, "z_m": 15.5, "ts": 0 }
```
- 좌표계는 크레인 로컬: x = 거더 방향(트롤리와 동일 축), z = 레일 방향(주행과 동일 축)

### `operator_input` — 조작반/마이크 원문 (선택)
```json
{ "type": "operator_input", "text": "후크 2미터 올려", "lang": "ko", "ts": 0 }
```
- 웹이 같은 AI 해석 엔진으로 파싱해 타임라인에 기록한다. 실행은 웹 Confirm 이후.

## 웹 → 브리지 (송신)

### `command` — Confirm된 이동 명령
```json
{ "type": "command", "axis": "HOIST", "direction": 1, "amount_m": 2.0, "ts": 0 }
```
- `axis`: `TRAVEL` | `TROLLEY` | `HOIST`
- `direction`: `1`(전진/우측/상승) | `-1`(후진/좌측/하강)
- **웹 UI에서 사용자가 Confirm을 누른 명령만 송신된다.**

### `interlock` — 안전 인터록
```json
{ "type": "interlock", "active": true, "reason": "worker_under_load", "ts": 0 }
```
- `active: true` 수신 즉시 브리지는 모든 축을 정지해야 한다.
- 해제는 `active: false`.

### `heartbeat` — 생존 신호 (1초 간격)
```json
{ "type": "heartbeat", "ts": 0 }
```
- 브리지는 heartbeat가 **3초** 이상 끊기면 자체적으로 모든 축을 정지해야 한다
  (fail-safe). 웹도 수신 소켓이 닫히면 "연결 끊김"을 표시하고 3초 간격 재접속한다.

## 좌표·단위 규약

- 거리·높이는 모두 **미터**, 실물 모형은 브리지에서 스케일 환산해 적용한다.
- 3D 화면 좌표는 1유닛 = 25m (`lib`의 `UNIT_METERS`)로 환산된다.

## 참고 구현 (Python)

```python
import asyncio, json, time, websockets

async def handler(ws):
    async def push_state():
        while True:
            await ws.send(json.dumps({
                "type": "crane_state", "travel_m": 0.0,
                "trolley_m": 0.0, "hook_m": 20.0,
                "ts": int(time.time() * 1000)}))
            await asyncio.sleep(0.1)
    task = asyncio.create_task(push_state())
    try:
        async for raw in ws:
            msg = json.loads(raw)
            if msg["type"] == "command":
                ...  # 모터 제어
            elif msg["type"] == "interlock" and msg["active"]:
                ...  # 전축 정지
    finally:
        task.cancel()

asyncio.run(websockets.serve(handler, "0.0.0.0", 8765))
```
