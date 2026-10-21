// 크레인 상태 읽기 전용 뷰 — HUD·검증·Yard Hub 보고가 이것만 본다.

namespace YardAX.Core
{
    public interface ICraneState
    {
        string CraneId { get; }                 // "goliath-1" | "tower-1"
        float GetPosition(CraneAxis axis);      // m (권상은 지면 기준 높이)
        float GetVelocity(CraneAxis axis);      // m/s
        float LoadTon { get; }                  // 현재 하중 (시뮬레이션 값)
        bool Supports(CraneAxis axis);
    }
}
