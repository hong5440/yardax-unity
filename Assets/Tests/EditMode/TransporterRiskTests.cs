using NUnit.Framework;
using YardAX.Schedule;

public class TransporterRiskTests
{
    [Test]
    public void BandLength_DoublesWithSpeed()
    {
        float a = TransporterRisk.BandLength(2f, 5f);
        float b = TransporterRisk.BandLength(4f, 5f);
        Assert.AreEqual(10f, a, 1e-3f);
        Assert.AreEqual(2f * a, b, 1e-3f, "속도 2배 → 띠 길이 2배");
    }

    [Test]
    public void BlindZone_DetectsWorkerBehindTallBlock()
    {
        // 블록 14×22, 높이 6 > 운전석 눈높이 2.5 → 블록 뒤 사각
        Assert.IsTrue(TransporterRisk.InBlindZone(0f, -14f, 14f, 22f, 6f, 2.5f));
        Assert.IsFalse(TransporterRisk.InBlindZone(0f, -14f, 14f, 22f, 2f, 2.5f), "낮은 블록은 보임");
        Assert.IsFalse(TransporterRisk.InBlindZone(0f, 5f, 14f, 22f, 6f, 2.5f), "전방은 사각 아님");
    }

    [Test]
    public void CrossingWorker_RedBand_WithEta()
    {
        var r = TransporterRisk.Compute(new TransporterRisk.Input
        {
            speedMs = 2f, lookAheadS = 6f, blockW = 14f, blockL = 22f,
            blockH = 6f, cabEyeHeight = 2.5f,
            workersLocal = new[] { (0f, 6f) },
        });
        Assert.AreEqual(2, r.band);
        Assert.AreEqual(3f, r.crossingEtaS, 0.1f);
        StringAssert.Contains("보행 교차", r.reason);
    }
}
