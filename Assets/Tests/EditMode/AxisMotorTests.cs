using NUnit.Framework;
using YardAX.Goliath;

public class AxisMotorTests
{
    static void Run(AxisMotor m, float seconds, float dt = 0.02f)
    {
        for (float t = 0; t < seconds; t += dt) m.Step(dt);
    }

    [Test]
    public void RangeEnd_DeceleratesAndStopsAtMax()
    {
        var m = new AxisMotor(0, 10, maxSpeed: 2f, accel: 1f, start: 0f);
        m.MoveBy(50f); // 범위 밖 명령 → Max에서 정지해야
        Run(m, 20f);
        Assert.AreEqual(10f, m.Position, 0.01f);
        Assert.AreEqual(0f, m.Velocity, 0.05f);
    }

    [Test]
    public void Speed_NeverExceedsLimit()
    {
        var m = new AxisMotor(0, 100, maxSpeed: 1.5f, accel: 0.5f);
        m.MoveTo(100);
        float peak = 0;
        for (float t = 0; t < 30f; t += 0.02f) { m.Step(0.02f); peak = System.Math.Max(peak, System.Math.Abs(m.Velocity)); }
        Assert.LessOrEqual(peak, 1.5f + 1e-3f);
    }

    [Test]
    public void BrakingNearBound_SpeedWithinBrakingCurve()
    {
        var m = new AxisMotor(0, 10, maxSpeed: 3f, accel: 1f);
        m.MoveTo(10);
        for (float t = 0; t < 15f; t += 0.02f)
        {
            m.Step(0.02f);
            float remain = 10f - m.Position;
            // v ≤ √(2·a·d) + 수치 여유
            Assert.LessOrEqual(m.Velocity, System.Math.Sqrt(2.0 * 1.0 * System.Math.Max(0, remain)) + 0.1);
        }
    }

    [Test]
    public void Jog_StopsWhenReleased()
    {
        var m = new AxisMotor(0, 100, 2f, 2f);
        m.Jog = 1f; Run(m, 2f);
        Assert.Greater(m.Position, 1f);
        m.Jog = 0f; Run(m, 3f);
        Assert.AreEqual(0f, m.Velocity, 0.02f);
    }
}
