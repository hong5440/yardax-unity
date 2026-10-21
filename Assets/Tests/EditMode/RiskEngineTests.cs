using System.Collections.Generic;
using NUnit.Framework;
using YardAX.Data;
using YardAX.Schedule;

public class RiskEngineTests
{
    static RiskInput Base() => new RiskInput
    {
        order = new WorkOrder { process = "블록 탑재", crew = new Crew { count = 6, newHires = 0, languages = new List<string> { "ko" } } },
        missingPermits = new List<string>(),
        thresholds = RiskThresholds.Default,
    };

    [Test]
    public void Conflicts_And_MissingPermits_RaiseScore()
    {
        var i = Base();
        RiskRuleEngine.Evaluate(i, out int baseScore);
        i.conflictCount = 1;
        i.missingPermits = new List<string> { "화기작업" };
        var f = RiskRuleEngine.Evaluate(i, out int score);
        Assert.Greater(score, baseScore);
        Assert.IsTrue(f.Exists(x => x.name == "작업 간섭"));
        Assert.IsTrue(f.Exists(x => x.name == "허가 누락"));
    }

    [Test]
    public void Wind_AtThreshold_AddsFactor_WithLegalNote()
    {
        var i = Base();
        i.windMs = 15f;
        var f = RiskRuleEngine.Evaluate(i, out _);
        var wind = f.Find(x => x.name == "강풍");
        Assert.IsNotNull(wind.reason);
        StringAssert.Contains("법령·사내 기준 확인 필요", wind.reason);
    }

    [Test]
    public void Score_Capped_At_100()
    {
        var i = Base();
        i.conflictCount = 3;
        i.missingPermits = new List<string> { "a", "b", "c", "d" };
        i.windMs = 20f; i.rain = true; i.night = true;
        i.block = new Block { weightTon = 480, needsTurnover = true, id = "E3" };
        i.processRisk = new ProcessRisk { baseLevel = 3, risks = new List<string> { "낙하" } };
        RiskRuleEngine.Evaluate(i, out int score);
        Assert.AreEqual(100, score);
    }
}
