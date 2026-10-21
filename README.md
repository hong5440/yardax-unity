# YardAX — 크레인 안전 디지털 트윈 (Unity)

한화오션 거제사업장 데모의 Unity 시뮬레이터. 타워크레인 실물 모형 연동 +
1도크 골리앗 크레인(디지털). 웹 관제(아차사고 레이더, `hong5440/maketing`)와
Yard Hub(WebSocket, `docs/protocol.md`) 프로토콜을 공유한다.

## 열기

Unity Hub → Open → 이 폴더. 기준 버전 2022.3 LTS(`ProjectSettings/ProjectVersion.txt`,
다른 2022.3.x로 열어도 됨). 첫 열기에서 .meta 파일이 생성되면 함께 커밋할 것.

## 지금 들어 있는 것

- `Assets/Scripts/Data` — W-1 야드 스케줄 데이터 계층
  (모델·ScheduleRepository·FacilityMapTable SO)
- `Assets/StreamingAssets/yard_schedule_demo.json` — 합성 데이터
  (블록 18×7단계, 시연일 2026-11-05, 작업지시 12건·간섭 3·허가 누락 2)
- `Assets/Tests/EditMode` — 데이터 계층 테스트 4종
- `docs/` — 구현 계획: `goliath-plan.md`(G-wave), `performance-plan.md`(F-wave),
  `schedule-plan.md`(W-wave), `protocol.md`(Yard Hub)
- `blender/` — 골리앗 모델 생성 스크립트 + FBX 내보내기 규약
- `firmware/` — 미니 골리앗 ESP32 펌웨어 (PlatformIO)

규칙은 `CLAUDE.md` 참고 (씬·프리팹 직접 수정 금지 — Tools > YardAX 메뉴,
인터페이스 공유, AI 비조작 원칙, ScriptableObject 설정, EditMode 테스트).

## 테스트 (배치 모드)

```bash
"$UNITY_PATH" -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testResults results/editmode.xml -logFile logs/test.log -quit
```

## 아직 없는 것

기존 타워크레인 데모 코드(Explore Mode, Swept Area, 검증·타임라인, TCP 명령 수신)는
이 저장소에 **아직 이관되지 않았다**. 보유 중이면 `Assets/` 에 넣어 커밋하면
G-0 스캔 → 인터페이스 추출부터 이어진다. 없다면 G-wave 계획대로 신규 구현한다.
