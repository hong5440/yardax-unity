// EditMode 테스트 (W-1)
// JSON은 Assets/StreamingAssets/yard_schedule_demo.json 에 두는 것을 기본으로 하고,
// 없으면 저장소 경로(docs/data/)도 시도한다.

using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using YardAX.Data;

public class ScheduleRepositoryTests
{
    static string LoadJson()
    {
        string[] candidates =
        {
            Path.Combine(Application.streamingAssetsPath, "yard_schedule_demo.json"),
            Path.Combine(Application.dataPath, "../docs/data/yard_schedule_demo.json"),
            Path.Combine(Application.dataPath, "../../docs/data/yard_schedule_demo.json"),
        };
        foreach (var p in candidates)
            if (File.Exists(p)) return File.ReadAllText(p);
        Assert.Fail("yard_schedule_demo.json 을 찾지 못했습니다: " + string.Join(" | ", candidates));
        return null;
    }

    [Test]
    public void DemoDay_E3_CurrentStage_Is_Erection()
    {
        var repo = new ScheduleRepository(LoadJson());
        var stage = repo.GetCurrentStage("E3", new DateTime(2026, 11, 5));
        Assert.IsNotNull(stage, "11/5에 E3가 어떤 단계에도 속하지 않음");
        Assert.AreEqual("탑재", stage.stage);
        Assert.AreEqual(1, stage.facilityId, "탑재는 1도크에서");
    }

    [Test]
    public void DemoDay_Has_12_WorkOrders()
    {
        var repo = new ScheduleRepository(LoadJson());
        Assert.AreEqual(12, repo.GetTodayOrders(new DateTime(2026, 11, 5)).Count);
    }

    [Test]
    public void MissingPermits_GroundTruth_Is_2()
    {
        var repo = new ScheduleRepository(LoadJson());
        int missing = 0;
        foreach (var wo in repo.GetTodayOrders())
            if (repo.GetMissingPermits(wo).Count > 0) missing++;
        Assert.AreEqual(2, missing, "시연 시나리오: 허가 누락 2건");
    }

    [Test]
    public void DemoDay_Conflicts_GroundTruth_Is_3()
    {
        var repo = new ScheduleRepository(LoadJson());
        Assert.AreEqual(3, repo.GetConflicts().Count);
    }
}
