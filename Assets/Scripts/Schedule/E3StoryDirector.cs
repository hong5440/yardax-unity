// W-8 시연 모드 "E3 블록 이야기" — data/scenes/e3-story.json(StreamingAssets) 정의,
// 0(영) 키 토글, N 다음 단계. 각 단계: 카메라·활성 장면·우측 카드.

using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using YardAX.Net;
using YardAX.Performance;

namespace YardAX.Schedule
{
    public class E3StoryDirector : MonoBehaviour
    {
        public Camera mainCamera;
        public ScheduleBoardView board;
        public WorkActiveScreen workActive;
        public TransporterController transporter;
        public ProcessSceneManager scenes;
        public PerformanceSequencer performance;
        public YardHubClient hub;

        public static bool Active { get; private set; }
        public static bool InFpv { get; private set; }
        public int Step { get; private set; }

        [Serializable] class StoryStep
        { public string title; public string scene; public float[] cameraPos; public float[] cameraEuler; public string card; }
        [Serializable] class Story { public StoryStep[] steps; }

        Story _story;

        void Awake()
        {
            string p = Path.Combine(Application.streamingAssetsPath, "scenes/e3-story.json");
            if (File.Exists(p)) _story = JsonUtility.FromJson<Story>(File.ReadAllText(p));
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _story == null) return;
            if (kb.digit0Key.wasPressedThisFrame)
            {
                Active = !Active;
                if (Active) Go(0); else End();
            }
            if (Active && !InFpv && kb.nKey.wasPressedThisFrame && !kb.shiftKey.isPressed)
                Go(Step + 1);
            // fpv 단계: 퍼포먼스 7단계가 끝나면(N으로 ⑦까지) 스토리의 다음으로
            if (Active && InFpv && performance != null && performance.Step >= 7
                && kb.nKey.wasPressedThisFrame && !kb.shiftKey.isPressed)
            { InFpv = false; Go(Step + 1); }
        }

        void End()
        {
            InFpv = false;
            scenes?.DeactivateAll();
            workActive?.Close();
            if (transporter != null) transporter.gameObject.SetActive(false);
        }

        public void Go(int step)
        {
            if (_story == null || _story.steps.Length == 0) return;
            Step = Mathf.Clamp(step, 0, _story.steps.Length - 1);
            var s = _story.steps[Step];

            if (mainCamera != null && s.cameraPos != null && s.cameraPos.Length == 3 && s.scene != "fpv")
            {
                mainCamera.transform.position = new Vector3(s.cameraPos[0], s.cameraPos[1], s.cameraPos[2]);
                mainCamera.transform.rotation = Quaternion.Euler(s.cameraEuler[0], s.cameraEuler[1], s.cameraEuler[2]);
            }

            // 단계별 활성화
            if (board != null) board.Visible = s.scene == "gantt" || s.scene == "board";
            if (transporter != null) transporter.gameObject.SetActive(s.scene == "transporter");
            switch (s.scene)
            {
                case "active_e3":
                    OpenE3();
                    break;
                case "live":
                    OpenE3();
                    hub?.SetMode(HubMode.LIVE);
                    break;
                case "fpv":
                    InFpv = true;
                    performance?.Go(2);
                    break;
                case "processes":
                    workActive?.Close();
                    if (board != null) board.Visible = false;
                    scenes?.ActivateKey("cut");
                    break;
                case "finale":
                    scenes?.DeactivateAll();
                    workActive?.Close();
                    if (board != null) board.Visible = false;
                    break;
            }
        }

        void OpenE3()
        {
            if (board == null || workActive == null) return;
            var today = board.Today;
            for (int i = 0; i < today.Count; i++)
                if (today[i].blockId == "E3" && today[i].process.Contains("탑재"))
                { workActive.Show(i); break; }
            board.Visible = false;
        }

        void OnGUI()
        {
            if (!Active || _story == null) return;
            var s = _story.steps[Step];
            GUI.Box(new UnityEngine.Rect(Screen.width / 2f - 360, Screen.height - 96, 720, 56), "");
            GUI.Label(new UnityEngine.Rect(Screen.width / 2f - 348, Screen.height - 90, 700, 22),
                $"E3 블록 이야기 {Step + 1}/{_story.steps.Length} — {s.title}   (N 다음 · 0 종료)",
                new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            GUI.Label(new UnityEngine.Rect(Screen.width / 2f - 348, Screen.height - 66, 700, 24), s.card);
        }
    }
}
