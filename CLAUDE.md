# YardAX Unity — 크레인 디지털 트윈

> 기존 타워크레인 데모 코드(별도 PC)는 **아직 이 저장소로 이관되지 않았다.**
> 이관되면 어댑터로 아래 인터페이스에 연결한다(절대 규칙 2). 그 전까지
> 타워(tower-1)는 Yard Hub LIVE 연동 대상 식별자로만 존재한다.

한화오션 거제사업장 **1도크 골리앗 크레인(디지털) + 타워크레인(실물 모형 연동)**
Unity 시뮬레이터. 웹 관제(아차사고 레이더)와 **Yard Hub(WebSocket, docs/protocol.md)**
프로토콜을 공유한다.

## 절대 규칙

1. **씬·프리팹 파일을 직접 수정하지 않는다.** 배치·계층 생성이 필요하면
   `Tools > YardAX > …` 메뉴의 **Editor 스크립트**로 작성한다 (재실행 가능·멱등).
2. **기존 타워크레인 기능을 깨지 않는다.** 공통 동작은 인터페이스로 추출해 공유:
   - `ICraneRig` — 축 트랜스폼 핸들(피벗) 제공
   - `ICraneState` — 축 위치·속도·하중 읽기 전용 상태
   - `ISweptAreaProvider` — 예상 경로·Swept Area 계산/표시
   기존 타워크레인 클래스는 **어댑터로 감싸서** 인터페이스를 구현하게 하고,
   내부 구현은 가능한 한 손대지 않는다.
3. **AI는 크레인을 직접 움직이지 않는다.** 흐름은 항상
   `사람 조작 → (예측·차단 판단) → 허용된 입력만 실행`.
   모든 입력(키보드·게임패드·화면 버튼·TCP 명령)은 `OperatorInputRouter` 한 곳을 거친다.
4. **설정값은 ScriptableObject로 분리한다.** 속도·가속도·범위·동기 허용 오차·
   풍속 임계값 등 숫자를 코드에 하드코딩하지 않는다.
5. **계산 로직에는 EditMode 테스트를 작성한다.** (Swept Area 넓이, 범위 감속,
   동기 오차, 포락선 등 — MonoBehaviour 없는 순수 계산 클래스로 분리해 테스트)
6. 변경 후에는 **컴파일 확인**: Unity 에디터 재포커스 후 Console, 또는 Editor.log.
   - Windows: `%LOCALAPPDATA%\Unity\Editor\Editor.log`
   - macOS: `~/Library/Logs/Unity/Editor.log`
   - Linux: `~/.config/unity3d/Editor.log`
   `CompilationPipeline` 오류가 없는지, 신규 asmdef가 올바로 묶였는지 확인.

## 코드 구조 (현재 구현)

| 책임 | 클래스 | 위치 |
|---|---|---|
| 축 정의·리그·상태·Swept 인터페이스 | `CraneAxis`, `ICraneRig`, `ICraneState`, `ISweptAreaProvider` | `Core/` |
| 설정 | `GoliathCraneConfig`, `TransporterConfig`, SO들 | `Config/`, `Schedule/` |
| 골리앗 리그·구동 | `GoliathRig`, `AxisMotor`(순수), `GoliathController`, `GoliathState` | `Goliath/` |
| 턴오버 | `TurnoverSequence`, `TurnoverMath`(순수) | `Goliath/` |
| Swept Area | `SweptGeometry`(순수), `GoliathSweptArea` | `Goliath/`, `Viz/` |
| 입력 라우팅·차단 | `OperatorInputRouter`, `IRiskGate`, `GoliathRiskGates`, `CommandParser` | `Safety/` |
| 작업자·타임라인 | `WorkerRegistry`, `WarningTimeline` | `Safety/` |
| Yard Hub | `YardHubClient` (SIM/LIVE·재접속·heartbeat·interlock) | `Net/` |
| 투영·발표 | `ProjectionCamera`(레이어 30), `PresentationMode`+`ScenePreset` | `Viz/` |
| 1인칭 퍼포먼스 (F) | `FirstPersonRig`, `WorkerPath`, `PerformanceSequencer`, `CommandRecording`, `WorkerHud`, `PassCountdown`(순수), `PerformanceAudio`, `MeasurementMode`+`DwellTimer`(순수), `SplitScreen` | `Performance/` |
| 일정·작업 액티브 (W) | `ScheduleRepository`, `ScheduleBoardView`, `WorkActiveScreen`, `RiskRuleEngine`(순수), `GuidelineGenerator`, `TransporterController`+`TransporterRisk`(순수), `ProcessSceneManager`, `E3StoryDirector` | `Data/`, `Schedule/` |
| 씬 생성 메뉴 | Create Goliath Crane / Worker Path / Process Scenes | `Assets/Editor/YardAX/` |

## 폴더 구조 (목표)

```
Assets/Scripts/
  Core/            # 인터페이스(ICraneRig, ICraneState, ISweptAreaProvider), 공용 타입
  Config/          # ScriptableObject (GoliathCraneConfig, TowerCraneConfig, RiskConfig)
  Goliath/         # GoliathController, GoliathState, 턴오버 시퀀스
  Tower/           # 기존 타워크레인 (+ 인터페이스 어댑터)
  Safety/          # OperatorInputRouter, 위험 판단, 경고 타임라인
  Net/             # YardHubClient (WebSocket, docs/protocol.md)
  Viz/             # Swept Area 메시, 정사영 카메라, 발표 모드
Assets/Editor/YardAX/   # Tools > YardAX 메뉴 (씬 배치 생성기)
Assets/Tests/EditMode/  # 계산 로직 테스트
```

## 명령 프로토콜

- 전송·수신 틀은 웹 쪽 `docs/protocol.md`(Yard Hub)와 동일한 WebSocket JSON.
- 명령에 **크레인 식별자**를 추가한 v2 형식을 쓴다:
  `{"crane":"goliath-1","axis":"GANTRY","dir":"+","amount":20}`
- 축 이름: 타워 `SLEW|TROLLEY|HOIST` / 골리앗
  `GANTRY|TROLLEY_A|TROLLEY_B|TROLLEY_L|HOIST_A|HOIST_B|HOIST_L`
- `tower-1`(실물 연동 대상)과 `goliath-1`(디지털)은 같은 Hub 프로토콜을 사용한다.
- 차단된 입력은 **전송하지 않고** 경고 타임라인에만 기록한다.

## 시연 원칙

- SIM/LIVE 전환은 상단 버튼. LIVE 끊김 시 자동 재접속 + "SIM 전환 안내" 표시.
- 발표 모드(단축키 P): 3D ≥ 70%, 우측 패널은 현재 장면에 필요한 것 하나만.
- 모든 위험 수치·하중 비율은 **시뮬레이션 값임을 화면에 표시**한다.
  풍속 임계값 옆에는 "법령·사내 기준 확인 필요"를 상시 표기.
