using NUnit.Framework;
using YardAX.Goliath;

public class SweptGeometryTests
{
    [Test]
    public void Gantry20m_AreaLength_Is_BlockPlus20_And_WidthIsBlock()
    {
        // 블록 22×14 (x=거더 방향 22, z=레일 방향 14), 주행 +20m
        var full = SweptGeometry.FullSweep(0, 0, sizeX: 22f, sizeZ: 14f,
            SweptGeometry.SweepAxis.AlongZ, 20f);
        Assert.AreEqual(14f + 20f, full.LenZ, 1e-3f, "영역 길이 = 블록 + 20m");
        Assert.AreEqual(22f, full.LenX, 1e-3f, "영역 폭 = 블록 폭");
    }

    [Test]
    public void Split_PassedPlusRemaining_CoverFull()
    {
        SweptGeometry.Split(0, 0, 10f, 8f, SweptGeometry.SweepAxis.AlongZ, 20f, 0.5f,
            out var passed, out var remaining, out var current);
        Assert.AreEqual(8f + 10f, passed.LenZ, 1e-3f);     // 시작~현재 (블록 포함)
        Assert.AreEqual(8f + 10f, remaining.LenZ, 1e-3f);  // 현재~끝
        Assert.AreEqual(8f, current.LenZ, 1e-3f);
        var union = Rect2.Union(passed, remaining);
        Assert.AreEqual(8f + 20f, union.LenZ, 1e-3f, "지나온+남은 = 전체");
    }

    [Test]
    public void DangerStrip_InFrontOfBogie_WithConfigLength()
    {
        var strip = SweptGeometry.GantryDangerStrip(gantryZ: 30f, dir: +1,
            stripLen: 12f, spanX: 120f, bogieHalfLen: 7f);
        Assert.AreEqual(37f, strip.MinZ, 1e-3f);
        Assert.AreEqual(49f, strip.MaxZ, 1e-3f);
        Assert.AreEqual(12f, strip.LenZ, 1e-3f);
    }
}
