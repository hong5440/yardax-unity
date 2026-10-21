// W-2 블록 일정 바 + 오늘 작업 보드 (IMGUI — 기능 우선, uGUI 폴리시는 후속)
// - 간트: 블록 18 × 7단계, 오늘 세로선, 완료/진행/예정 색
// - 상단 일정 계층 브레드크럼 (블록 일정 강조)
// - 작업지시 12건 카드: 공정·블록·시간·시설·위험 배지, 간섭·누락 경고 배지
// - 카드/진행 칸 클릭 → W-3 작업 액티브

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YardAX.Data;

namespace YardAX.Schedule
{
    public class ScheduleBoardView : MonoBehaviour
    {
        public WorkActiveScreen workActive;
        public bool Visible = true;

        ScheduleRepository _repo;
        List<WorkOrder> _today;
        HashSet<string> _conflicted = new();
        Dictionary<string, int> _missing = new();
        Vector2 _scroll;

        static readonly Color Done = new(0.35f, 0.5f, 0.42f);
        static readonly Color Now = new(0.24f, 0.78f, 1f);
        static readonly Color Todo = new(0.22f, 0.28f, 0.38f);

        public ScheduleRepository Repo => _repo;

        void Awake()
        {
            string p = Path.Combine(Application.streamingAssetsPath, "yard_schedule_demo.json");
            _repo = new ScheduleRepository(File.ReadAllText(p));
            _today = _repo.GetTodayOrders();
            foreach (var c in _repo.GetConflicts())
                foreach (var id in c.workOrderIds) _conflicted.Add(id);
            foreach (var wo in _today) _missing[wo.id] = _repo.GetMissingPermits(wo).Count;
        }

        void OnGUI()
        {
            if (!Visible) return;
            float W = Mathf.Min(Screen.width - 24, 1180);
            var area = new UnityEngine.Rect((Screen.width - W) / 2, 16, W, Screen.height - 32);
            GUILayout.BeginArea(area, GUI.skin.box);

            // 일정 계층 브레드크럼
            GUILayout.BeginHorizontal();
            foreach (var h in _repo.Data.scheduleHierarchy)
            {
                bool cur = h == "블록 일정";
                var st = new GUIStyle(GUI.skin.label)
                { fontStyle = cur ? FontStyle.Bold : FontStyle.Normal, fontSize = cur ? 15 : 12 };
                st.normal.textColor = cur ? Now : Color.gray;
                GUILayout.Label(h, st, GUILayout.ExpandWidth(false));
                if (h != "블록 일정") GUILayout.Label("›", GUILayout.Width(12));
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"시연일 {_repo.Data.demoDate} · 합성 데이터", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawGantt(W - 30);
            GUILayout.Space(10);
            DrawBoard(W - 30);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawGantt(float width)
        {
            GUILayout.Label("블록 일정 (절단→소조립→조립→선행의장→선행도장→PE→탑재)",
                new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            // 날짜 범위
            DateTime min = DateTime.MaxValue, max = DateTime.MinValue;
            foreach (var b in _repo.Data.blocks)
            {
                var s0 = ScheduleRepository.ParseDate(b.stages[0].start);
                var e1 = ScheduleRepository.ParseDate(b.stages[^1].end);
                if (s0 < min) min = s0;
                if (e1 > max) max = e1;
            }
            float days = (float)(max - min).TotalDays + 1;
            float labelW = 56f, rowH = 16f;
            float px = (width - labelW) / days;

            var r0 = GUILayoutUtility.GetRect(width, _repo.Data.blocks.Count * (rowH + 2) + 8);
            float today = (float)(_repo.DemoDate - min).TotalDays;

            int row = 0;
            foreach (var b in _repo.Data.blocks)
            {
                float y = r0.y + row * (rowH + 2);
                GUI.Label(new UnityEngine.Rect(r0.x, y, labelW, rowH), b.id);
                foreach (var s in b.stages)
                {
                    float x0 = (float)(ScheduleRepository.ParseDate(s.start) - min).TotalDays;
                    float x1 = (float)(ScheduleRepository.ParseDate(s.end) - min).TotalDays + 1;
                    int status = _repo.StageStatus(s);
                    var rect = new UnityEngine.Rect(r0.x + labelW + x0 * px, y,
                        Mathf.Max(2, (x1 - x0) * px - 1), rowH);
                    GUI.color = status < 0 ? Done : status == 0 ? Now : Todo;
                    if (GUI.Button(rect, GUIContent.none, GUI.skin.box) && status == 0)
                        OpenForBlock(b.id);
                    GUI.color = Color.white;
                }
                row++;
            }
            // 오늘 세로선
            GUI.color = new Color(1f, 0.45f, 0.3f);
            GUI.Box(new UnityEngine.Rect(r0.x + labelW + today * px, r0.y - 2, 2,
                _repo.Data.blocks.Count * (rowH + 2) + 4), GUIContent.none);
            GUI.color = Color.white;
        }

        void OpenForBlock(string blockId)
        {
            for (int i = 0; i < _today.Count; i++)
                if (_today[i].blockId == blockId) { workActive?.Show(i); Visible = false; return; }
        }

        void DrawBoard(float width)
        {
            GUILayout.Label($"오늘 작업 보드 — {_today.Count}건",
                new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            int cols = Mathf.Max(2, (int)(width / 280));
            for (int i = 0; i < _today.Count; i += cols)
            {
                GUILayout.BeginHorizontal();
                for (int c = 0; c < cols && i + c < _today.Count; c++)
                {
                    int idx = i + c;
                    var wo = _today[idx];
                    bool warn = _conflicted.Contains(wo.id) || _missing[wo.id] > 0;
                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(width / cols - 8));
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{ProcessIcon(wo.process)} {wo.process}",
                        new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                    if (warn)
                    {
                        GUI.color = new Color(1f, 0.4f, 0.3f);
                        GUILayout.Label(_missing[wo.id] > 0 ? "⚠ 허가 누락" : "⚠ 간섭",
                            GUILayout.ExpandWidth(false));
                        GUI.color = Color.white;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Label($"{(string.IsNullOrEmpty(wo.blockId) ? "-" : wo.blockId)} · " +
                        $"{wo.start}~{wo.end} · 시설 {wo.facilityId}");
                    GUILayout.Label("위험: " + string.Join(", ", wo.expectedRisks),
                        new GUIStyle(GUI.skin.label) { fontSize = 11 });
                    if (GUILayout.Button("작업 액티브 열기 (A)"))
                    { workActive?.Show(idx); Visible = false; }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }

        static string ProcessIcon(string p) =>
            p.Contains("탑재") ? "🏗" : p.Contains("용접") ? "⚡" : p.Contains("도장") ? "🎨"
            : p.Contains("검사") ? "🔍" : p.Contains("운송") || p.Contains("불출") ? "🚚"
            : p.Contains("족장") ? "🪜" : p.Contains("절단") ? "🔥" : "🔧";

        public List<WorkOrder> Today => _today;
        public bool IsConflicted(string id) => _conflicted.Contains(id);
        public int MissingCount(string id) => _missing.TryGetValue(id, out var n) ? n : 0;
    }
}
