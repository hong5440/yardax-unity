using NUnit.Framework;
using YardAX.Performance;
using YardAX.Performance.Hud;

public class PerformanceTests
{
    [Test]
    public void Countdown_ApproachingLoad_PositiveSeconds()
    {
        // 하중 중심 0, 반길이 7m, 작업자 +27m, 속도 +2m/s → 전단 +7 → 20m/2 = 10s
        float s = PassCountdown.SecondsToReach(0f, 7f, 27f, 2f);
        Assert.AreEqual(10f, s, 1e-3f);
    }

    [Test]
    public void Countdown_MovingAway_IsMinusOneOrZero()
    {
        Assert.AreEqual(-1f, PassCountdown.SecondsToReach(0f, 7f, 27f, 0f), 1e-3f);
        // 멀어지는 방향: dist<0 → 0 (이미 지남 취급 아님 — 전단 반대편)
        Assert.AreEqual(0f, PassCountdown.SecondsToReach(0f, 7f, -27f, 2f) < 1e-3f ? 0f : 1f);
    }

    [Test]
    public void DwellTimer_AccumulatesInZone_And_EvadeAfterWarn()
    {
        var t = new DwellTimer();
        float now = 0f, dt = 0.1f;
        for (int i = 0; i < 20; i++) { now += dt; t.Tick(true, warned: i >= 5, now, dt); }  // 2s 체류, 0.6s에 경고
        for (int i = 0; i < 5; i++) { now += dt; t.Tick(false, false, now, dt); }           // 이탈
        Assert.AreEqual(2.0f, t.DwellS, 1e-2f);
        Assert.Greater(t.EvadeS, 0f);
        Assert.AreEqual(2.1f - 0.6f, t.EvadeS, 0.15f); // 경고(0.6s)→이탈(2.1s) ≈ 1.5s
    }
}
