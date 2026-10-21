// 경고 타임라인 (G-5) — 경고 시각·조작 시각·선행 시간 기록.
// 라우터 Routed / 컨트롤러 Warning / 게이트 차단을 한 줄씩 쌓는다.

using System;
using System.Collections.Generic;
using UnityEngine;
using YardAX.Goliath;

namespace YardAX.Safety
{
    public class WarningTimeline : MonoBehaviour
    {
        public OperatorInputRouter router;
        public GoliathController goliath;
        public int capacity = 200;

        public struct Entry
        {
            public float time;          // Time.time
            public string kind;         // warn | block | command | info
            public string message;
            /// <summary>경고→조작 선행 시간(s). 해당 없으면 -1</summary>
            public float leadTimeS;
        }

        readonly List<Entry> _entries = new();
        public IReadOnlyList<Entry> Entries => _entries;
        public event Action<Entry> Added;

        float _lastWarnTime = -1f;

        void OnEnable()
        {
            if (router != null) router.Routed += OnRouted;
            if (goliath != null) goliath.Warning += OnWarning;
        }

        void OnDisable()
        {
            if (router != null) router.Routed -= OnRouted;
            if (goliath != null) goliath.Warning -= OnWarning;
        }

        void OnRouted(CraneCommandV2 cmd, bool allowed, string info)
        {
            float lead = (!allowed && _lastWarnTime >= 0) ? Time.time - _lastWarnTime : -1f;
            Add(allowed ? "command" : "block", $"{cmd} — {info}", lead);
        }

        void OnWarning(string msg)
        {
            _lastWarnTime = Time.time;
            Add("warn", msg, -1f);
        }

        public void Add(string kind, string message, float leadTimeS = -1f)
        {
            var e = new Entry { time = Time.time, kind = kind, message = message, leadTimeS = leadTimeS };
            _entries.Add(e);
            if (_entries.Count > capacity) _entries.RemoveAt(0);
            Added?.Invoke(e);
        }
    }
}
