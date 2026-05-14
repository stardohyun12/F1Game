#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 채널 3 (W1) 씬 와이어링 — ZoneUnlockSystem GameObject + ZoneUnlockPanel UI를 현 씬에 자동 배치.
/// 메뉴: PitCrew/W1/Wire Scene (Zones)
/// </summary>
public static class WireW1ZoneScene
{
    const string REGISTRY_PATH = "Assets/Data/Zones/ZoneRegistry.asset";

    [MenuItem("PitCrew/W1/Wire Scene (Zones)")]
    public static void Wire()
    {
        var registry = AssetDatabase.LoadAssetAtPath<ZoneRegistry>(REGISTRY_PATH);
        if (registry == null)
        {
            Debug.LogError($"[WireW1] ZoneRegistry 없음 — 먼저 PitCrew/W1/Seed Zones 실행: {REGISTRY_PATH}");
            return;
        }

        var upgrade = Object.FindFirstObjectByType<UpgradeSystem>(FindObjectsInactive.Include);
        if (upgrade == null)
        {
            Debug.LogError("[WireW1] 씬에 UpgradeSystem 없음 — 자금 시스템 필요.");
            return;
        }

        // 1) ZoneUnlockSystem GameObject (UpgradeSystem 옆에).
        var existing = Object.FindFirstObjectByType<ZoneUnlockSystem>(FindObjectsInactive.Include);
        ZoneUnlockSystem zus;
        if (existing != null) zus = existing;
        else
        {
            var go = new GameObject("ZoneUnlockSystem");
            zus = go.AddComponent<ZoneUnlockSystem>();
        }
        SetPrivateField(zus, "registry", registry);
        SetPrivateField(zus, "wallet", upgrade);

        // ZoneUnlockPanel UI는 도현 요청으로 제거됨. 자동 해금 (wallet 임계 도달 시) 사용.
        var existingPanel = Object.FindFirstObjectByType<ZoneUnlockPanel>(FindObjectsInactive.Include);
        if (existingPanel != null) Object.DestroyImmediate(existingPanel.gameObject);

        EditorUtility.SetDirty(zus);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[WireW1] ZoneUnlockSystem 와이어링 완료 (auto-unlock). Panel UI 없음.");
    }

    static void SetPrivateField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (f != null) f.SetValue(target, value);
        else Debug.LogWarning($"[WireW1] 필드 못 찾음: {name}");
    }
}
#endif
