using NUnit.Framework;
using YardAX.Core;
using YardAX.Safety;

public class CommandParserTests
{
    [Test]
    public void Parses_V2_Gantry_Plus20()
    {
        string json = "{\"crane\":\"goliath-1\",\"axis\":\"GANTRY\",\"dir\":\"+\",\"amount\":20}";
        Assert.IsTrue(CommandParser.TryParse(json, out var cmd, out var err), err);
        Assert.AreEqual("goliath-1", cmd.craneId);
        Assert.AreEqual(CraneAxis.GANTRY, cmd.axis);
        Assert.AreEqual(1, cmd.direction);
        Assert.AreEqual(20f, cmd.amountM, 1e-4f);
    }

    [Test]
    public void Parses_TowerAxis_ForSameProtocol()
    {
        string json = "{\"crane\":\"tower-1\",\"axis\":\"SLEW\",\"dir\":\"-\",\"amount\":5}";
        Assert.IsTrue(CommandParser.TryParse(json, out var cmd, out _));
        Assert.AreEqual(CraneAxis.SLEW, cmd.axis);
        Assert.AreEqual(-1, cmd.direction);
    }

    [Test]
    public void Rejects_BadAxis_And_BadDir()
    {
        Assert.IsFalse(CommandParser.TryParse("{\"axis\":\"WARP\",\"dir\":\"+\",\"amount\":1}", out _, out _));
        Assert.IsFalse(CommandParser.TryParse("{\"axis\":\"GANTRY\",\"dir\":\"x\",\"amount\":1}", out _, out _));
        Assert.IsFalse(CommandParser.TryParse("{\"axis\":\"GANTRY\",\"dir\":\"+\",\"amount\":0}", out _, out _));
    }
}
