// F-3 Before/After 시퀀서 — 단계:
// ① 관제 시점 → ② 1인칭 전환 → ③ Before(HUD 끔) → ④ 정지·되감기
// → ⑤ After(HUD 켬) → ⑥ 화면 분할 → ⑦ 숫자 패널
// N 다음 단계 · B 처음부터. 단계 이름은 발표자 메모(Shift+N 토글)로 표시.
// 크레인 동작은 CommandRecording 재생이라 Before/After가 시간까지 동일.
// After: 작업자가 영역 안이면 크레인 입력 차단 대기(LoadBelowGate), 물러나면 해제.

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YardAX.Performance.Hud;

namespace YardAX.Performance
{
    public class PerformanceSequencer : MonoBehaviour
    {
        public FirstPersonRig rig;
        public CommandPlayback playback;
        public WorkerHud hud;
        public SplitScreen split;
        public PerformanceAudio audioFx;

        public static readonly string[] StepNames =
        {
            "① 관제 시점", "② 1인칭 전환", "③ Before — HUD 없음",
            "④ 정지·되감기", "⑤ After — HUD 켬", "⑥ 화면 분할", "⑦ 숫자 패널",
        };

        public int Step { get; private set; } // 0 = 꺼짐, 1..7
        public bool PresenterNotes { get; private set; } = true;
        public event Action<int, string> StepChanged;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.nKey.wasPressedThisFrame)
            {
                if (kb.shiftKey.isPressed) PresenterNotes = !PresenterNotes;
                else Go(Step + 1);
            }
            if (kb.bKey.wasPressedThisFrame) Go(1);
        }

        public void Go(int step)
        {
            Step = Mathf.Clamp(step, 1, StepNames.Length);
            StepChanged?.Invoke(Step, StepNames[Step - 1]);
            switch (Step)
            {
                case 1: // 관제 시점
                    rig?.ReturnToControl();
                    playback?.StopAndRewind();
                    if (hud != null) hud.HudOn = false;
                    split?.Apply(SplitScreen.Layout.FirstPersonOnly);
                    break;
                case 2: rig?.BeginDescend(); break;
                case 3: // Before
                    if (hud != null) hud.HudOn = false;
                    playback?.Play();
                    audioFx?.PlayRadioBefore();
                    break;
                case 4: // 정지·되감기 (시뮬 상태 복원)
                    playback?.StopAndRewind();
                    rig?.ResetToStart();
                    rig?.BeginDescend();
                    break;
                case 5: // After
                    if (hud != null) hud.HudOn = true;
                    playback?.Play();
                    audioFx?.PlayWarnStop();
                    break;
                case 6: split?.Apply(SplitScreen.Layout.SideBySide); break;
                case 7: split?.Apply(SplitScreen.Layout.Pip); break;
            }
        }

        void OnGUI()
        {
            if (Step == 0) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            GUI.Label(new UnityEngine.Rect(12, Screen.height - 58, 700, 24),
                $"퍼포먼스 {Step}/7 — {StepNames[Step - 1]}   (N 다음 · B 처음 · Shift+N 메모)", style);
            if (PresenterNotes)
                GUI.Label(new UnityEngine.Rect(12, Screen.height - 32, 900, 24),
                    "시뮬레이션 값 · 합성 시나리오 — 같은 명령 기록을 두 번 재생해 시간까지 동일");
        }
    }
}
