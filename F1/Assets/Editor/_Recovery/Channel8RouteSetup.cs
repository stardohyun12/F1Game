using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 채널 8 — 잡별 destinationLabels 저작. 물건 1개 = 목적지 1곳 (spec).
/// 9개 잡(Pool 8 + Order_C)에 씬 DeliveryPoint label 배분. 멱등.
///
/// 채널 5에서 정한 itemCount 유지하도록 그 수만큼 label 부여:
/// - Order_05_StandardBox: 2개 → 서로 다른 2곳
/// - Order_C_Risky: 3개 → 흩어진 3곳
/// - 나머지 Pool 7개: 각 1개 → 서로 다른 단일 목적지
/// </summary>
public static class Channel8RouteSetup
{
    // 씬 DeliveryPoint 이름 8개
    const string DowntownEntry = "DowntownEntry";
    const string FactoryNorth  = "FactoryNorth";
    const string EastHub       = "EastHub";
    const string LowerWest     = "LowerWest";
    const string SuburbanGate  = "SuburbanGate";
    const string CentralCross  = "CentralCross";
    const string WestHub       = "WestHub";
    const string PlateauTop    = "PlateauTop";

    [MenuItem("PitCrew/Channel8/Route Setup — 잡별 destinationLabels")]
    public static void Run()
    {
        // 1물건 잡 7개 — 서로 다른 단일 목적지 (도시 전역 분산)
        AssignLabels("Assets/Data/Orders/Pool/Order_01_Eggs.asset",         new[] { CentralCross });
        AssignLabels("Assets/Data/Orders/Pool/Order_02_Glass.asset",        new[] { EastHub });
        AssignLabels("Assets/Data/Orders/Pool/Order_03_Fish.asset",         new[] { DowntownEntry });
        AssignLabels("Assets/Data/Orders/Pool/Order_04_WeddingCake.asset",  new[] { PlateauTop });
        AssignLabels("Assets/Data/Orders/Pool/Order_06_PremiumWine.asset",  new[] { WestHub });
        AssignLabels("Assets/Data/Orders/Pool/Order_07_SashimiTray.asset",  new[] { LowerWest });
        AssignLabels("Assets/Data/Orders/Pool/Order_08_BirthdayCake.asset", new[] { SuburbanGate });

        // 2물건 잡 (택배 박스) — 떨어진 2곳
        AssignLabels("Assets/Data/Orders/Pool/Order_05_StandardBox.asset",
            new[] { DowntownEntry, FactoryNorth });

        // 3물건 잡 (대량 배송) — 흩어진 3곳
        AssignLabels("Assets/Data/Orders/Order_C_Risky.asset",
            new[] { WestHub, CentralCross, PlateauTop });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Channel8RouteSetup] 완료 — 9개 잡 destinationLabels 저작.");
    }

    static void AssignLabels(string path, string[] labels)
    {
        var job = AssetDatabase.LoadAssetAtPath<OrderJob>(path);
        if (job == null) { Debug.LogWarning($"[Channel8RouteSetup] {path} 못 찾음"); return; }
        job.destinationLabels = new List<string>(labels);
        EditorUtility.SetDirty(job);
    }
}
