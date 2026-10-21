using NUnit.Framework;
using YardAX.Goliath;

public class TurnoverAndGateTests
{
    [Test]
    public void EnvelopeRadius_Is_HalfDiagonal_PlusMargin()
    {
        // 블록 단면 z=14, h=6 → 반대각 √(14²+6²)/2 ≈ 7.616 + 여유 2
        float r = TurnoverMath.EnvelopeRadius(14f, 6f, 2f);
        Assert.AreEqual(System.Math.Sqrt(14 * 14 + 6 * 6) / 2.0 + 2.0, r, 1e-3);
    }

    [Test]
    public void LoadShare_At0_UpperFull_At90_Half()
    {
        var at0 = TurnoverMath.LoadShare(0f);
        Assert.AreEqual(1f, at0.upper, 1e-3f);
        Assert.AreEqual(0f, at0.lower, 1e-3f);
        var at90 = TurnoverMath.LoadShare(90f);
        Assert.AreEqual(0.5f, at90.upper, 1e-3f);
        Assert.AreEqual(0.5f, at90.lower, 1e-3f);
    }

    [Test]
    public void DangerStrip_Backward_UsesNegativeDirection()
    {
        var strip = SweptGeometry.GantryDangerStrip(50f, -1, 12f, 120f, 7f);
        Assert.AreEqual(31f, strip.MinZ, 1e-3f);
        Assert.AreEqual(43f, strip.MaxZ, 1e-3f);
    }
}
