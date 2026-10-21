// 공용 축 정의 — 타워(SLEW/TROLLEY/HOIST)와 골리앗 7축을 하나의 열거로.
// Yard Hub v2 프로토콜의 axis 문자열과 1:1 (docs/protocol.md).

namespace YardAX.Core
{
    public enum CraneAxis
    {
        // 타워
        SLEW, TROLLEY, HOIST,
        // 골리앗
        GANTRY, TROLLEY_A, TROLLEY_B, TROLLEY_L, HOIST_A, HOIST_B, HOIST_L,
    }

    public static class CraneAxisUtil
    {
        public static bool TryParse(string s, out CraneAxis axis) =>
            System.Enum.TryParse(s, ignoreCase: true, out axis);
    }
}
