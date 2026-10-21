// Yard Hub v2 명령 (G-2) — {"crane":"goliath-1","axis":"GANTRY","dir":"+","amount":20}
// 파싱은 MonoBehaviour 없는 순수 클래스 (EditMode 테스트 대상).

using System;
using UnityEngine;
using YardAX.Core;

namespace YardAX.Safety
{
    public struct CraneCommandV2
    {
        public string craneId;
        public CraneAxis axis;
        public int direction;   // +1 | -1
        public float amountM;

        public override string ToString() =>
            $"{craneId} {axis} {(direction > 0 ? "+" : "-")}{amountM}m";
    }

    public static class CommandParser
    {
        [Serializable]
        class Dto { public string crane; public string axis; public string dir; public float amount; }

        /// <summary>v2 JSON 한 건 파싱. 실패 시 false + error</summary>
        public static bool TryParse(string json, out CraneCommandV2 cmd, out string error)
        {
            cmd = default; error = null;
            Dto d;
            try { d = JsonUtility.FromJson<Dto>(json); }
            catch (Exception e) { error = "JSON 파싱 실패: " + e.Message; return false; }
            if (d == null || string.IsNullOrEmpty(d.axis)) { error = "axis 없음"; return false; }
            if (!CraneAxisUtil.TryParse(d.axis, out var axis)) { error = "알 수 없는 axis: " + d.axis; return false; }
            int dir = d.dir == "-" ? -1 : d.dir == "+" ? 1 : 0;
            if (dir == 0) { error = "dir은 \"+\" 또는 \"-\""; return false; }
            if (d.amount <= 0f || d.amount > 200f) { error = "amount 범위 오류: " + d.amount; return false; }
            cmd = new CraneCommandV2
            {
                craneId = string.IsNullOrEmpty(d.crane) ? "goliath-1" : d.crane,
                axis = axis, direction = dir, amountM = d.amount,
            };
            return true;
        }
    }
}
