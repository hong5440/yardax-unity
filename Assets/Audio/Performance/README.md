# 퍼포먼스 오디오 파일 규칙 (F-5)

이 폴더에 아래 이름으로 넣고 `PerformanceAudio` 컴포넌트 슬롯에 연결한다.
없는 파일은 조용히 건너뛴다 — 녹음·TTS로 채워 넣으면 된다.

| 파일 | 내용 |
|---|---|
| `crane_motor_loop.wav` | 크레인 모터 루프 (3D, 트롤리 위치) |
| `wire_creak_loop.wav` | 와이어 삐걱 루프 (3D, 후크 위치) |
| `block_metal_01.wav` … | 블록 흔들림 금속음 (랜덤 주기, 약하게) |
| `radio_ko_01.wav` | Before: 빠르게 지나가는 한국어 무전 |
| `warn_vi_stop.wav` | After: 작업자 언어 "멈추세요" (vi 예시 — 언어별로 추가) |
| `warn_vi_back.wav` | After: "뒤로 물러나세요" |
| `beep_warning.wav` | 짧은 경고음 (음성 직전) |

권장: 44.1kHz mono WAV. 음성은 Voice 믹서 그룹(별도 볼륨), 나머지는 Master.
