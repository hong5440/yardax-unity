# F-wave: 작업자 1인칭 퍼포먼스 (Explore Mode 확장) 계획

발표용 "작업자 1인칭 퍼포먼스" — 기존 Explore Mode(1·3인칭 탐색, 위험 HUD)를
**수정하지 않고** 별도 Performance Mode로 붙인다. 씬·프리팹 직접 수정 금지,
배치는 전부 `Tools > YardAX` Editor 스크립트 규칙 유지. **코드 변경은 F-0 스캔
결과 확인 후 시작.**

## F-0. 스캔 (선행, 코드 변경 없음)

`Assets/Scripts`에서 아래 책임의 기존 클래스를 찾아 표를 채운다:

| 책임 | 기존 클래스 (TODO) | 퍼포먼스에서 재사용 방식 |
|---|---|---|
| 탐색 이동 (WASD, 1·3인칭) | ? | 측정 모드(F-6)의 참가자 이동으로 재사용 |
| 카메라 전환 | ? | 관제→1인칭 3초 전환 연출의 시작/끝 상태 |
| 위험 HUD | ? | After 상태 HUD의 토대 (끄기/켜기 스위치 지점) |
| 크레인 Swept Area (`ISweptAreaProvider`) | ? | 하중 이동 띠·카운트다운의 데이터 소스 |
| 작업자 위험 감지 | ? | 영역 내 체류 판정·차단 대기(F-3)·측정(F-6) |
| 명령 수신/실행 구조 | ? | 기록 재생(F-3)의 주입 지점 |
| 다국어 명령 UI 번역 체계 | ? | HUD 문구 ko/vi/en 전환(F-4) |

통합 원칙: 기존 컴포넌트는 **구독(이벤트/인터페이스)으로만** 사용하고, Performance
Mode 진입/이탈 시 기존 Explore Mode 상태를 저장·복원한다. 기존 클래스에 손대야
한다면 public 이벤트/프로퍼티 추가까지만.

## F-2. 시네마틱 1인칭 모드

- `FirstPersonPerformanceConfig : ScriptableObject` — 눈높이 1.65m, FOV 70,
  걸음 흔들림 세기(기본 약하게 + 끔 옵션), 이동 속도, 고개 들기 타이밍(경로 구간 t).
- 경로: 1도크 E3 블록 하부로 걸어 들어가는 스플라인.
  **Cinemachine 있으면** Spline Dolly 사용, 없으면 Catmull-Rom 웨이포인트 보간
  (스캔에서 패키지 유무 확인 후 결정 — 둘 다 같은 `IWorkerPath` 뒤에 숨김).
- `Tools > YardAX > Create Worker Path` — 웨이포인트 자식 오브젝트 생성(멱등),
  씬 기즈모로 편집 가능.
- 정해진 지점에서 카메라가 **천천히 고개를 들어** 머리 위 블록 주시
  (회전 속도 상한 — 멀미 방지 규칙과 공유).
- 관제 시점 → 작업자 머리 위로 하강 → 1인칭 전환 연출 **약 3초**
  (위치·FOV 동시 보간, 급회전 금지).
- 화면 위 가장자리 **안전모 챙 그림자** 오버레이 (반투명 그라데이션, 옅게).
- 단축키: `N` 다음 단계, `B` 처음부터. 모든 카메라 회전에 각속도 상한.

## F-3. Before / After 재생

- `PerformanceSequencer` 단계: ① 관제 시점 → ② 1인칭 전환 → ③ **Before**(HUD 끔)
  → ④ 정지·되감기 → ⑤ **After**(HUD 켬) → ⑥ 화면 분할 → ⑦ 숫자 패널.
- 크레인 동작은 **타임스탬프 포함 명령 기록**(`CommandRecording` SO 또는 JSON)을
  기존 명령 수신 구조로 재생 — Before/After가 시간까지 동일.
- After: 작업자가 하중 이동 영역 안에 있는 동안 크레인 입력이 **차단 대기**,
  물러나면 해제 — 기존 `OperatorInputRouter` 차단 판단 재사용(새 규칙 추가 없음).
- 각 단계 `N` 키 진행, 단계 이름은 발표자 메모(`Shift+N`)로 표시.
- 되감기 = 시뮬 상태 스냅숏 복원(크레인 축 위치·작업자 위치·타이머).

## F-4. 작업자 HUD (After 전용)

원칙: **최소한으로, 작업자 언어로, 공간(월드)에 붙인다.**

- 하중 이동 띠: 기존 Swept Area 데이터를 1인칭 가독성용으로 바닥 부착 반투명 띠로
  재표현 + 진행 방향으로 흐르는 화살표(셰이더 UV 스크롤 또는 텍스처 오프셋).
- 카운트다운: 띠 위 월드 공간 텍스트 "하중 통과까지 N초" — 예상 경로와 현재 속도로
  계산하는 **순수 계산 클래스**(`PassCountdown`)로 분리 → EditMode 테스트.
- 화면 밖 방향 표시: 하중이 시야 밖이면 화면 가장자리 화살표(뷰포트 클램프).
- 영역 이탈 시: 띠 초록 전환 + "안전" 표시.
- 언어 ko/vi/en 전환 — 기존 다국어 명령 UI의 번역 체계 재사용.
- 문구는 전부 테이블(`HudStringsTable` SO: key × {ko, vi, en})로 분리.

## F-5. 사운드

- 크레인 모터·와이어: 크레인 트롤리/후크 위치에 3D AudioSource, 거리 감쇠(Log).
- 블록 흔들림 금속음: 약하게, 재생 주기 랜덤(코루틴 + 랜덤 지연).
- Before: 빠르게 지나가는 한국어 무전 클립. After: 작업자 언어 음성 경고
  ("멈추세요", "뒤로 물러나세요") + 짧은 경고음.
- 파일 위치 `Assets/Audio/Performance/`, 파일명 규칙(README):
  `radio_ko_01.wav`, `warn_vi_stop.wav`, `warn_vi_back.wav`, `beep_warning.wav`,
  `crane_motor_loop.wav`, `wire_creak_loop.wav`, `block_metal_01..n.wav`
  — 팀이 녹음·TTS로 채워 넣기만 하면 된다 (없는 파일은 조용히 건너뜀).
- AudioMixer 그룹 2개: Master / Voice — 볼륨 별도 조절(설정 SO에 기본값).

## F-6. 화면 분할 + 체험·측정 모드

- 화면 분할: 1인칭 메인 + 관제 보조 — 좌우 반반 ↔ 우상단 PiP 전환
  (카메라 2대, 같은 시뮬레이션 상태; Viewport Rect로 구현해 URP/빌트인 공통).
- 체험·측정 모드: 참가자가 WASD(기존 탐색 이동 재사용)로 지정 위치까지 이동.
  기록 지표: **영역 내 체류 시간, 경고 후 회피까지 시간, 성공 여부**.
- 과제 순서: HUD 없음 → HUD 있음 (동일 크레인 기록 재생).
- CSV 저장: `Application.persistentDataPath/yardax_measure.csv`
  — 열: 참가자 번호, 조건(HUD on/off), 체류 s, 회피 s, 성공, 타임스탬프.
- 요약 패널: 조건별 평균·차이 표시(저장된 CSV 재로드).
- EditMode 테스트: ① 카운트다운 계산(경로·속도 → N초) ② 영역 내 체류 시간 계산
  (겹침 구간 적산) — 둘 다 MonoBehaviour 없는 순수 클래스.

## 파일 배치 (목표)

```
Assets/Scripts/Performance/   # Sequencer, WorkerPath, 전환 연출, 측정 기록
Assets/Scripts/Performance/Hud/   # 띠·카운트다운·방향 화살표·문자열 테이블
Assets/Config/                # FirstPersonPerformanceConfig, HudStringsTable, 녹화본
Assets/Editor/YardAX/         # Create Worker Path 메뉴
Assets/Audio/Performance/     # 오디오 (파일명 규칙 README 포함)
Assets/Tests/EditMode/        # PassCountdown·체류 시간 테스트
```

## 수용 기준

- Explore Mode 단독 실행이 이전과 동일하게 동작(회귀 체크리스트).
- 퍼포먼스 전 과정이 `N`/`B` 두 키로 진행 가능, 네트워크·외부 의존 없음.
- 모든 수치는 SO, 문구는 테이블, EditMode 테스트 green, 컴파일 클린(Editor.log).

## 전제 조건 (차단 — G-wave와 동일)

이 세션에는 Unity 프로젝트가 없다 (`Assets/` 미존재, 전 브랜치 재확인 완료).
프로젝트를 GitHub에 푸시하고 위치를 알려주면 F-0 스캔부터 시작한다.
