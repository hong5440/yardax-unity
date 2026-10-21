// F-4 HUD 문구 테이블 — 전부 여기서 수정 (ko/vi/en).

using System;
using System.Collections.Generic;
using UnityEngine;

namespace YardAX.Performance.Hud
{
    public enum HudLang { ko, vi, en }

    [CreateAssetMenu(menuName = "YardAX/HUD Strings Table", fileName = "HudStringsTable")]
    public class HudStrings : ScriptableObject
    {
        [Serializable]
        public struct Row { public string key; public string ko; public string vi; public string en; }

        public List<Row> rows = new()
        {
            new Row { key = "countdown", ko = "하중 통과까지 {0}초",
                      vi = "Tải đi qua sau {0} giây", en = "Load passes in {0}s" },
            new Row { key = "safe", ko = "안전 — 영역 밖입니다",
                      vi = "An toàn — ngoài khu vực", en = "SAFE — outside the zone" },
            new Row { key = "move_back", ko = "뒤로 물러나세요",
                      vi = "Lùi lại phía sau", en = "Move back" },
            new Row { key = "stop", ko = "멈추세요", vi = "Dừng lại", en = "Stop" },
            new Row { key = "load_offscreen", ko = "하중 방향 →",
                      vi = "Hướng tải →", en = "Load direction →" },
        };

        public string Get(string key, HudLang lang, object arg = null)
        {
            foreach (var r in rows)
            {
                if (r.key != key) continue;
                string s = lang switch { HudLang.vi => r.vi, HudLang.en => r.en, _ => r.ko };
                return arg != null ? string.Format(s, arg) : s;
            }
            return key;
        }
    }
}
