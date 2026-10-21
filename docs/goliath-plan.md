# G-wave: 1도크 골리앗 크레인 구현·리팩터링 계획

요청 8건(CLAUDE.md, 리그, 컨트롤러, Swept Area, 턴오버, 위험 판단, Yard Hub,
프로젝터·발표 모드)을 단계로 정리한 계획. **코드 변경은 G-0 스캔 결과 확인 후 시작.**

## G-0. 스캔 (선행, 코드 변경 없음)

- `Assets/Scripts` 전체를 훑어 CLAUDE.md의 "기존 코드 구조" 표를 채운다:
  CraneState, 예상 경로, Swept Area, 검증(지시→조작→움직임), 타임라인, 명령 수신.
- 산출물: 클래스→책임 표, 의존 그래프 메모, 인터페이스 추출 지점 표시.
- 기존 asmdef 구성·Input System 사용 여부·테스트 프레임워크 유무 확인.

## G-0.5. 인터페이스 추출 리팩터링

- `Core/`에 `ICraneRig`(축 피벗 핸들), `ICraneState`(읽기 전용 상태),
  `ISweptAreaProvider`(경로·영역 계산/표시) 정의.
- 타워크레인은 **어댑터 클래스**로 인터페이스를 구현 — 기존 클래스 내부 수정 최소화.
- 수용 기준: 타워크레인 기존 시연 플로우가 그대로 동작(수동 회귀 체크리스트 작성).

## G-1. 골리앗 리그 생성기

- `GoliathCraneConfig : ScriptableObject` — 경간, 높이, 주행 범위,
  상부 트롤리 2기·하부 트롤리 1기의 횡행 범위, 권상 범위, 축별 속도·가속도,
  동기 허용 오차, 풍속 경고 임계값(값은 전부 SO).
- `Tools > YardAX > Create Goliath Crane` (Editor 스크립트, 멱등):
  ```
  GoliathRoot(주행)
  ├─ Legs (양쪽 다리·주행 바퀴)
  ├─ Girder (박스 거더, 측면 트러스는 인스턴싱)
  ├─ UpperTrolleyA ─ HookA (와이어 LineRenderer)
  ├─ UpperTrolleyB ─ HookB (와이어 LineRenderer)
  ├─ LowerTrolley ─ HookL (와이어 LineRenderer)
  └─ LoadBlock (크기 설정 가능)
  ```
- 외형은 단순 형상 + 노란 도장. **피벗 이름·위치 고정** → 나중에 Blender 모델로
  메시만 교체 (피벗 규약을 CLAUDE.md에 명문화).
- 1도크 레일 위, 조감도 비율로 도크를 가로지르게 배치 (배치도 Editor 스크립트).

## G-2. GoliathController

- 축: `GANTRY, TROLLEY_A, TROLLEY_B, TROLLEY_L, HOIST_A, HOIST_B, HOIST_L`.
- 축별 속도·가속도·범위 제한, **범위 끝 접근 시 감속 정지** (제동거리 v²/2a 계산 —
  EditMode 테스트).
- 동기 모드: `TROLLEY_A+B` 동기 횡행, `HOIST_A+B` 동기 권상.
  높이 차 > 허용 오차 → 경고 이벤트 (타임라인 기록).
- 입력: Input System (키보드·게임패드·화면 버튼) → 전부 `OperatorInputRouter` 경유.
- 명령 수신: 기존 TCP/JSON에 v2 형식 추가
  `{"crane":"goliath-1","axis":"GANTRY","dir":"+","amount":20}`.
- 상태 공개: `GoliathState : ICraneState` (각 축 위치·속도·하중).

## G-3. 예상 경로 + Swept Area (`ISweptAreaProvider` 구현)

- 경로: 주행=레일 방향 직선, 횡행=거더 방향 직선, 권상=수직.
- 블록 바닥 사각형이 경로를 쓸고 간 영역(사각형 합)을 **Procedural Mesh**로 바닥 표시.
- 기존 표시 체계 그대로: 전체 예측 / 지나온 영역 / 남은 영역 / 현재 footprint.
- 주행 시: 다리·바퀴 전방 레일 구간(설정 길이)을 **주행 위험 구간**으로 별도 표시.
- EditMode 테스트: 주행 20m 명령 → 영역 길이 = 블록 길이+20m, 폭 = 블록 폭.

## G-4. 블록 턴오버 시퀀스

- 5단계: ① 상부 후크 2 + 하부 후크 1 연결 → ② 동기 권상 → ③ 하부 후크 하강하며
  회전 축 기준 90°/180° 회전 → ④ 하부 후크 해제 → ⑤ 착지.
- 시작 전 **회전 포락선**(회전 중 블록 점유 전체 범위) 계산·바닥 표시,
  내부에 작업자 있으면 시작 불가.
- 단계 진행 표시 1/5~5/5, **각 단계 사람 Confirm 후 진행**.
- 회전 중 블록 기울기·후크 하중 비율 패널 표시 (시뮬레이션 값 고지).
- EditMode 테스트: 포락선 반경 = 블록 대각 반경 + 여유.

## G-5. 위험 판단 → OperatorInputRouter 연결

- 주행 위험: 진행 방향 레일 구간에 작업자·장비 → 해당 방향 주행 차단.
- 하중 아래: Swept Area ∩ 작업자 원형 범위 → 해당 축·방향 차단.
- 턴오버: 회전 포락선 내 작업자 → 시작 차단.
- 강풍: 풍속(시뮬 슬라이더 또는 실시간 값) ≥ 임계값 → 주행 경고
  (+ "법령·사내 기준 확인 필요" 표기).
- 차단 시 화면 경고 + **경고 타임라인**(경고 시각, 조작 시각, 선행 시간) 기록.
- 기존 검증(지시→조작→움직임)에 골리앗 축 추가.

## G-6. Yard Hub 연결 (docs/protocol.md)

- `YardHubClient`: WebSocket, 자동 재접속, 연결 상태 표시.
- 운전자 입력은 **차단 판단 통과 후에만** `operator_input` 전송.
- LIVE: `crane_state` 수신 → GoliathState/타워 상태를 실측값으로 **보간 갱신**,
  예측 대비 실측 차이 표시. SIM: 기존처럼 가상 동작.
- 상단 SIM/LIVE 토글, LIVE 끊김 시 SIM 전환 안내.
- 크레인 선택: `tower-1`(실물 연동) / `goliath-1`(디지털), 동일 Hub 프로토콜.
- **protocol.md v2 확장 필요**(웹 저장소와 동시 반영): `command`·`crane_state`·
  `operator_input`에 `crane` 필드, 골리앗 축 7종, `dir:"+"|"-"` 표기 명시.

## G-7. 프로젝터 정사영 + 발표 모드

- 두 번째 디스플레이용 정사영 카메라: 1도크 상공 수직 하방,
  Swept Area·주행 위험 구간·회전 포락선·작업자**만** 렌더(레이어 마스크).
- 발표 모드(단축키 P): 3D ≥ 70%, 우측 패널은 장면당 1개.
- 장면 프리셋: ① 1도크 전경 ② 골리앗 주행+주행 위험 구간 ③ 동기 권상
  ④ 턴오버 포락선+작업자 차단 ⑤ 타워크레인 실물 연동.

## G-8. 테스트·운영 문서

- EditMode 테스트 목록: 축 범위 제한(감속 정지), 동기 오차 경고, Swept Area 크기
  (주행 20m = 블록+20m), 턴오버 포락선, 차단 판단(주행/하중 아래/포락선/강풍).
- **배치 모드 테스트 실행** 명령을 Unity 프로젝트 README에 정리
  (Unity 실행 파일 경로는 팀원 PC에 맞게 변수로):
  ```bash
  "$UNITY_PATH" -batchmode -projectPath . -runTests -testPlatform EditMode \
    -testResults results/editmode.xml -logFile logs/test.log -quit
  ```
- **발표 모드 장면 ①~⑤ 체크리스트** (P 키 → 프리셋 순서 실행):
  1. ① 1도크 전경 — 패널 없음, 카메라 프리셋 확인
  2. ② 골리앗 주행 — 주행 위험 구간 표시, 구간 내 작업자 배치 시 차단 확인
  3. ③ 동기 권상 — HOIST_A+B 동기, 오차 주입 시 경고 이벤트 확인
  4. ④ 턴오버 — 포락선 표시 → 작업자 침입 시 시작 불가 → Confirm 5단계 진행
  5. ⑤ 타워크레인 LIVE — tower-1 선택, 실물 crane_state 보간·예측 차이 표시

## 공통 수용 기준

- 각 단계 후 Unity 컴파일 클린(Editor.log 확인), 타워크레인 회귀 체크리스트 통과.
- 숫자 설정은 전부 ScriptableObject, EditMode 테스트 green.
- 씬 변경은 모두 `Tools > YardAX` 메뉴 재실행으로 재현 가능.

## 전제 조건 (차단 사항)

**이 세션에는 Unity 프로젝트가 없다.** `hong5440/maketing` 전 브랜치와 접근 가능한
GitHub 저장소를 확인했으나 `Assets/`가 없음. 진행하려면:
1. Unity 프로젝트를 GitHub에 푸시 — 새 저장소 또는 `maketing`의 별도 브랜치
   (`Assets/`, `Packages/`, `ProjectSettings/` 포함, `Library/`·`Temp/` 제외), 그리고
2. 저장소 이름(또는 브랜치)을 알려주면 세션에 연결해 G-0 스캔부터 시작한다.
