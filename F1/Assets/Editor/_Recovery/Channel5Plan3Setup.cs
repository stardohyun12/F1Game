using UnityEditor;
using UnityEngine;

/// <summary>
/// 채널 5 Plan 3 — 적재함 크기 업그레이드 데이터 셋업.
/// 코드(UpgradeEffect.TrunkExpand1/2, BoxCapacity)는 별도 커밋. 이 스크립트는 SO 데이터만 만든다.
///
/// 하는 일:
///  1. UpgradeDef 2개 생성/갱신 (적재함 확장 I/II).
///  2. UpgradeCatalog에 추가.
///  3. Order_05_StandardBox itemCount 1→2 (택배 박스 2개).
///  4. Order_C_Risky(itemCount=3)를 다운타운 zone 잡 풀에 추가 + displayName 정리.
///
/// 멱등 — 여러 번 실행해도 중복 추가 없음. asset.md 권장 패턴(메뉴 노출 + 검증 로그).
/// </summary>
public static class Channel5Plan3Setup
{
    const string kUpgradeDir = "Assets/Data/Upgrades/";
    const string kU1Path = kUpgradeDir + "Upgrade_06_TrunkExpand1.asset";
    const string kU2Path = kUpgradeDir + "Upgrade_07_TrunkExpand2.asset";
    const string kCatalogPath = kUpgradeDir + "UpgradeCatalog.asset";
    const string kBoxJobPath = "Assets/Data/Orders/Pool/Order_05_StandardBox.asset";
    const string kRiskyJobPath = "Assets/Data/Orders/Order_C_Risky.asset";
    const string kDowntownZonePath = "Assets/Data/Zones/Zone_Downtown.asset";

    [MenuItem("PitCrew/Channel5/Plan3 Setup — 적재함 업그레이드 + 다물건 잡")]
    public static void Run()
    {
        // 1. UpgradeDef 2개
        var u1 = LoadOrCreateUpgrade(kU1Path, UpgradeEffect.TrunkExpand1,
            "적재함 확장 I", "적재함을 한 칸 늘려 물건 2개까지 싣는다.", 800,
            new Color(0.55f, 0.78f, 0.95f));
        var u2 = LoadOrCreateUpgrade(kU2Path, UpgradeEffect.TrunkExpand2,
            "적재함 확장 II", "적재함을 한 칸 더 늘려 물건 3개까지 싣는다.", 1600,
            new Color(0.35f, 0.62f, 0.92f));

        // 2. 카탈로그에 추가 (멱등)
        var cat = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(kCatalogPath);
        if (cat == null)
        {
            Debug.LogError($"[Channel5Plan3Setup] UpgradeCatalog 못 찾음: {kCatalogPath}");
        }
        else
        {
            if (!cat.upgrades.Contains(u1)) cat.upgrades.Add(u1);
            if (!cat.upgrades.Contains(u2)) cat.upgrades.Add(u2);
            EditorUtility.SetDirty(cat);
        }

        // 3. 채널 8 (2026-05-15): itemCount는 destinationLabels.Count에서 파생됨 → 직접 set 불가.
        //    Order_05_StandardBox=2개, Order_C_Risky=3개 보장은 Channel8RouteSetup이 destinationLabels로 처리.

        // 4. Order_C_Risky → 다운타운 zone + displayName 정리
        var risky = AssetDatabase.LoadAssetAtPath<OrderJob>(kRiskyJobPath);
        var downtown = AssetDatabase.LoadAssetAtPath<Zone>(kDowntownZonePath);
        if (risky != null)
        {
            risky.displayName = "대량 배송";
            EditorUtility.SetDirty(risky);
        }
        if (risky != null && downtown != null)
        {
            if (!downtown.jobs.Contains(risky)) downtown.jobs.Add(risky);
            EditorUtility.SetDirty(downtown);
        }
        else Debug.LogWarning("[Channel5Plan3Setup] Order_C_Risky 또는 Zone_Downtown 못 찾음");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int catCount = cat != null ? cat.upgrades.Count : -1;
        int dtCount = downtown != null ? downtown.jobs.Count : -1;
        Debug.Log($"[Channel5Plan3Setup] 완료 — 카탈로그 {catCount}개, 다운타운 잡 {dtCount}개. " +
                  "(itemCount은 채널 8에서 destinationLabels 파생.)");
    }

    static UpgradeDef LoadOrCreateUpgrade(string path, UpgradeEffect effect,
        string displayName, string description, int cost, Color tint)
    {
        var def = AssetDatabase.LoadAssetAtPath<UpgradeDef>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<UpgradeDef>();
            AssetDatabase.CreateAsset(def, path);
        }
        def.effect = effect;
        def.displayName = displayName;
        def.description = description;
        def.cost = cost;
        def.tintColor = tint;
        EditorUtility.SetDirty(def);
        return def;
    }
}
