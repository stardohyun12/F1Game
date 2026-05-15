using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 채널 8 보강 — 다운타운 zone에 잡을 풍부하게.
///
/// 시드 상태: 다운타운 4잡 → 매번 같은 destination 보임 (도현 피드백 2026-05-15).
/// 해법: Order_A/B/C destination 부여, 다운타운 zone.jobs를 11잡으로 채움.
/// EastHub 중복(02 vs C)도 같이 해소.
/// </summary>
public static class Channel8DowntownEnrich
{
    const string CentralCross  = "CentralCross";
    const string EastHub       = "EastHub";
    const string DowntownEntry = "DowntownEntry";
    const string PlateauTop    = "PlateauTop";
    const string FactoryNorth  = "FactoryNorth";
    const string WestHub       = "WestHub";
    const string LowerWest     = "LowerWest";
    const string SuburbanGate  = "SuburbanGate";

    [MenuItem("PitCrew/Channel8/Downtown Enrich — 풀 확장 + EastHub 충돌 해소")]
    public static void Run()
    {
        // Order_A/B에 destination, Order_C는 WestHub로 이동(EastHub 충돌 해소).
        Assign("Assets/Data/Orders/Order_A_Safe.asset",     SuburbanGate);
        Assign("Assets/Data/Orders/Order_B_Balanced.asset", PlateauTop);
        Assign("Assets/Data/Orders/Order_C_Risky.asset",    WestHub);

        // 다운타운 zone.jobs를 11잡으로 채움.
        var zone = FindZoneByName("다운타운");
        if (zone == null) { Debug.LogError("[DowntownEnrich] 다운타운 zone 못 찾음"); return; }

        var newJobs = new List<OrderJob>();
        AddIfExists(newJobs, "Assets/Data/Orders/Order_A_Safe.asset");
        AddIfExists(newJobs, "Assets/Data/Orders/Order_B_Balanced.asset");
        AddIfExists(newJobs, "Assets/Data/Orders/Order_C_Risky.asset");
        AddIfExists(newJobs, "Assets/Data/Orders/Pool/Order_01_Eggs.asset");
        AddIfExists(newJobs, "Assets/Data/Orders/Pool/Order_02_Glass.asset");
        AddIfExists(newJobs, "Assets/Data/Orders/Pool/Order_03_Fish.asset");
        AddIfExists(newJobs, "Assets/Data/Orders/Pool/Order_04_WeddingCake.asset");
        AddIfExists(newJobs, "Assets/Data/Orders/Pool/Order_05_StandardBox.asset");
        AddIfExists(newJobs, "Assets/Data/Orders/Pool/Order_06_PremiumWine.asset");
        AddIfExists(newJobs, "Assets/Data/Orders/Pool/Order_07_SashimiTray.asset");
        AddIfExists(newJobs, "Assets/Data/Orders/Pool/Order_08_BirthdayCake.asset");

        var jobsField = typeof(Zone).GetField("jobs");
        jobsField.SetValue(zone, newJobs);
        EditorUtility.SetDirty(zone);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Channel8DowntownEnrich] 완료 — 다운타운 jobs={newJobs.Count} (Order_A/B/C destination 부여, Order_C EastHub→WestHub).");
    }

    static void Assign(string path, string label)
    {
        var job = AssetDatabase.LoadAssetAtPath<OrderJob>(path);
        if (job == null) { Debug.LogWarning($"[DowntownEnrich] {path} 못 찾음"); return; }
        job.destinationLabels = new List<string> { label };
        EditorUtility.SetDirty(job);
    }

    static void AddIfExists(List<OrderJob> list, string path)
    {
        var job = AssetDatabase.LoadAssetAtPath<OrderJob>(path);
        if (job != null && !list.Contains(job)) list.Add(job);
    }

    static Zone FindZoneByName(string displayName)
    {
        var guids = AssetDatabase.FindAssets("t:Zone");
        foreach (var g in guids)
        {
            var z = AssetDatabase.LoadAssetAtPath<Zone>(AssetDatabase.GUIDToAssetPath(g));
            if (z != null && z.displayName == displayName) return z;
        }
        return null;
    }
}
