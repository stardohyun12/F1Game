using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Day 21 자동화 — 5개 UpgradeDef + UpgradeCatalog 자동 생성.
///
/// 메뉴: PitCrew/Upgrade/Generate 5-Upgrade Catalog
/// 결과:
/// - Assets/Data/Upgrades/Upgrade_*.asset 5개
/// - Assets/Data/Upgrades/UpgradeCatalog.asset
/// </summary>
public static class GenerateUpgrades
{
    const string Dir = "Assets/Data/Upgrades";
    const string CatalogPath = "Assets/Data/Upgrades/UpgradeCatalog.asset";

    [MenuItem("PitCrew/Upgrade/Generate 5-Upgrade Catalog")]
    public static void Generate()
    {
        Directory.CreateDirectory(Dir);

        var defs = new List<UpgradeDef>
        {
            Make("Upgrade_01_Suspension", UpgradeEffect.SuspensionUpgrade,
                "서스펜션 강화", "급가속·급제동 임계값 +0.1g 여유. 공격적 운전 가능.",
                cost: 800, color: new Color(0.6f, 0.85f, 1f)),

            Make("Upgrade_02_ChilledHold", UpgradeEffect.ChilledHold,
                "냉동 칸", "시간 제약 강한 냉동 화물 주문 카탈로그 해제 (Phase 2).",
                cost: 1500, color: new Color(0.5f, 0.95f, 1f)),

            Make("Upgrade_03_ShortcutMap", UpgradeEffect.ShortcutMap,
                "지름길 지도", "맵에 지름길(isShortcut) 엣지 표시. 경로 결정 정보 ↑.",
                cost: 1200, color: new Color(1f, 0.95f, 0.4f)),

            Make("Upgrade_04_SmoothSteering", UpgradeEffect.SmoothSteering,
                "부드러운 조향", "Spin(yaw rate) 임계값 +20°/s. 활어/액체 안전.",
                cost: 1000, color: new Color(0.85f, 0.55f, 1f)),

            Make("Upgrade_05_ReinforcedTrunk", UpgradeEffect.ReinforcedTrunk,
                "튼튼한 적재함", "화물 maxHP +20. 모든 화물 내구도 ↑.",
                cost: 1500, color: new Color(0.95f, 0.6f, 0.3f)),
        };

        var catalog = LoadOrCreate<UpgradeCatalog>(CatalogPath);
        catalog.upgrades = defs;
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GenerateUpgrades] 5개 업그레이드 + 카탈로그 생성 완료 → {CatalogPath}");
        EditorGUIUtility.PingObject(catalog);
        Selection.activeObject = catalog;
    }

    static UpgradeDef Make(string fileName, UpgradeEffect effect, string displayName, string desc, int cost, Color color)
    {
        string path = $"{Dir}/{fileName}.asset";
        var def = LoadOrCreate<UpgradeDef>(path);
        def.effect = effect;
        def.displayName = displayName;
        def.description = desc;
        def.cost = cost;
        def.tintColor = color;
        EditorUtility.SetDirty(def);
        return def;
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
