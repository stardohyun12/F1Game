using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Day 26 — 8개 주문 카드 셋 자동 생성 (Phase 1 Week 4).
///
/// 5종 cargoType × 4종 packing × 거리/시간 조합으로 다양성 확보.
/// 매 런 8개 풀에서 3개 무작위 노출 (OrderBoard 셔플 픽).
///
/// 메뉴: PitCrew/Order/Generate 8-Order Catalog
/// 실행 결과:
/// - Assets/Data/Orders/Pool/ 폴더에 8개 OrderJob_*.asset
/// - Assets/Data/Orders/OrderCatalogExpanded.asset (8개 풀)
/// </summary>
public static class GenerateExpandedOrders
{
    const string PoolDir = "Assets/Data/Orders/Pool";
    const string CatalogPath = "Assets/Data/Orders/OrderCatalogExpanded.asset";

    [MenuItem("PitCrew/Order/Generate 8-Order Catalog")]
    public static void Generate()
    {
        Directory.CreateDirectory(PoolDir);

        var jobs = new List<OrderJob>
        {
            // 1. 신선 계란 (Eggs) — 짧은 코스, Bubble, 시간제한 강함
            MakeJob("Order_01_Eggs", "신선 계란", new Color(0.95f, 0.9f, 0.5f),
                pointCount: 3, timeLimit: 240f,    // 4분
                baseReward: 700, timeBonus: 200,
                cargo: CargoType.Eggs, packing: PackingTier.Bubble, weight: 0),

            // 2. 유리 식기 (Glass) — 중거리, Foam, 충돌 절대 금지
            MakeJob("Order_02_Glass", "유리 식기", new Color(0.6f, 0.85f, 1f),
                pointCount: 5, timeLimit: 360f,    // 6분
                baseReward: 1000, timeBonus: 300,
                cargo: CargoType.Glass, packing: PackingTier.Foam, weight: 1),

            // 3. 활어 운송 (Fish) — 중거리, Foam, 코너 부드럽게
            MakeJob("Order_03_Fish", "활어 운송", new Color(0.4f, 0.7f, 0.95f),
                pointCount: 5, timeLimit: 300f,    // 5분
                baseReward: 1100, timeBonus: 400,
                cargo: CargoType.Fish, packing: PackingTier.Foam, weight: 1),

            // 4. 결혼 케이크 (Cake) — 중거리, Hardcase, 경사 절대 금지
            MakeJob("Order_04_WeddingCake", "결혼 케이크", new Color(1f, 0.7f, 0.85f),
                pointCount: 4, timeLimit: 420f,    // 7분
                baseReward: 1500, timeBonus: 500,
                cargo: CargoType.Cake, packing: PackingTier.Hardcase, weight: 2),

            // 5. 택배 박스 (Box) — 장거리, Bubble, 시간 무제한
            MakeJob("Order_05_StandardBox", "택배 박스", new Color(0.7f, 0.5f, 0.3f),
                pointCount: 7, timeLimit: 0f,      // 무제한
                baseReward: 600, timeBonus: 0,
                cargo: CargoType.Box, packing: PackingTier.Bubble, weight: 0),

            // 6. 고급 와인 (Glass) — 장거리, Hardcase, 큰 보상
            MakeJob("Order_06_PremiumWine", "고급 와인", new Color(0.6f, 0.2f, 0.4f),
                pointCount: 6, timeLimit: 480f,    // 8분
                baseReward: 1800, timeBonus: 600,
                cargo: CargoType.Glass, packing: PackingTier.Hardcase, weight: 2),

            // 7. 회식용 횟감 (Fish) — 짧은 코스, Bubble, 빠른 회전
            MakeJob("Order_07_SashimiTray", "회식용 횟감", new Color(0.4f, 0.85f, 0.5f),
                pointCount: 3, timeLimit: 210f,    // 3.5분
                baseReward: 900, timeBonus: 300,
                cargo: CargoType.Fish, packing: PackingTier.Bubble, weight: 0),

            // 8. 생일 케이크 (Cake) — 짧은 코스, Foam, 정시 도착 압박
            MakeJob("Order_08_BirthdayCake", "생일 케이크", new Color(0.95f, 0.6f, 0.85f),
                pointCount: 3, timeLimit: 270f,    // 4.5분
                baseReward: 800, timeBonus: 250,
                cargo: CargoType.Cake, packing: PackingTier.Foam, weight: 1),
        };

        // 카탈로그 (8개 모두 풀로)
        var catalog = LoadOrCreate<OrderCatalog>(CatalogPath);
        catalog.jobs = jobs;
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GenerateExpandedOrders] 8개 주문 + 카탈로그 생성 완료 → {CatalogPath}");
        EditorGUIUtility.PingObject(catalog);
        Selection.activeObject = catalog;
    }

    static OrderJob MakeJob(string fileName, string displayName, Color color,
        int pointCount, float timeLimit, int baseReward, int timeBonus,
        CargoType cargo, PackingTier packing, int weight)
    {
        string path = $"{PoolDir}/{fileName}.asset";
        var job = LoadOrCreate<OrderJob>(path);
        job.displayName = displayName;
        job.color = color;
        job.pointCount = pointCount;
        job.timeLimitSec = timeLimit;
        job.baseReward = baseReward;
        job.timeBonus = timeBonus;
        job.cargoType = cargo;
        // Task 14: defaultPacking 필드 삭제됨 (PackingTier 폐기). packing 파라미터는 Task 14에서 제거.
        job.weightTier = weight;
        EditorUtility.SetDirty(job);
        return job;
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }
}
