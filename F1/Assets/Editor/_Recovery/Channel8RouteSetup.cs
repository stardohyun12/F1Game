using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 채널 8 — 주문별 destinationLabels 저작. 정정된 모델: **주문 1장 = 목적지 1곳**.
/// 9개 주문(Pool 8 + Order_C)에 씬 DeliveryPoint label 1개씩 배분. 멱등.
///
/// 한 런의 다중 목적지는 OrderBoard에서 **여러 주문을 픽**해서 얻음(채널 5 capacity).
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

    [MenuItem("PitCrew/Channel8/Route Setup — 주문별 destinationLabels")]
    public static void Run()
    {
        // 9개 주문 — 각자 단일 목적지. 도시 전역 분산. 일부 중복 허용(8 지점 < 9 주문).
        AssignLabels("Assets/Data/Orders/Pool/Order_01_Eggs.asset",         new[] { CentralCross });
        AssignLabels("Assets/Data/Orders/Pool/Order_02_Glass.asset",        new[] { EastHub });
        AssignLabels("Assets/Data/Orders/Pool/Order_03_Fish.asset",         new[] { DowntownEntry });
        AssignLabels("Assets/Data/Orders/Pool/Order_04_WeddingCake.asset",  new[] { PlateauTop });
        AssignLabels("Assets/Data/Orders/Pool/Order_05_StandardBox.asset",  new[] { FactoryNorth });
        AssignLabels("Assets/Data/Orders/Pool/Order_06_PremiumWine.asset",  new[] { WestHub });
        AssignLabels("Assets/Data/Orders/Pool/Order_07_SashimiTray.asset",  new[] { LowerWest });
        AssignLabels("Assets/Data/Orders/Pool/Order_08_BirthdayCake.asset", new[] { SuburbanGate });
        // Order_C_Risky: 1차 구현에서 "대량 배송" 3곳이었지만 모델 정정 → 단일 목적지.
        // displayName도 단일 주문 의미로 다듬는다.
        AssignLabels("Assets/Data/Orders/Order_C_Risky.asset", new[] { EastHub });
        var risky = AssetDatabase.LoadAssetAtPath<OrderJob>("Assets/Data/Orders/Order_C_Risky.asset");
        if (risky != null && risky.displayName == "대량 배송")
        {
            risky.displayName = "급송 의뢰";
            EditorUtility.SetDirty(risky);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Channel8RouteSetup] 완료 — 9개 주문 destinationLabels 저작 (각 1곳).");
    }

    static void AssignLabels(string path, string[] labels)
    {
        var job = AssetDatabase.LoadAssetAtPath<OrderJob>(path);
        if (job == null) { Debug.LogWarning($"[Channel8RouteSetup] {path} 못 찾음"); return; }
        job.destinationLabels = new List<string>(labels);
        EditorUtility.SetDirty(job);
    }
}
