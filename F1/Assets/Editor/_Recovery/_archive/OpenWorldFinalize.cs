using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// OpenWorld_v2 최종 마무리 — Verify FAIL 잔여 보강.
// 1) IntegrityHUD GameObject 추가 (자가구성 — Awake에서 Canvas/Fill/Label 빌드)
// 2) ResultPanel.upgradeCatalog 연결 (Phase 1 step 6 우회 보강)
public static class OpenWorldFinalize
{
    const string UpgradeCatalogPath = "Assets/Data/Upgrades/UpgradeCatalog.asset";

    [MenuItem("PitCrew/Layout/OpenWorld Finalize")]
    public static void Execute()
    {
        // 1) IntegrityHUD
        if (Object.FindFirstObjectByType<IntegrityHUD>() == null)
        {
            var go = new GameObject("IntegrityHUD");
            go.AddComponent<IntegrityHUD>();
            Debug.Log("[OpenWorldFinalize] IntegrityHUD GO 추가 (자가구성).");
        }

        // 2) ResultPanel.upgradeCatalog 연결
        var resultPanel = Object.FindFirstObjectByType<ResultPanel>();
        var upgradeCatalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradeCatalogPath);
        if (resultPanel != null && upgradeCatalog != null)
        {
            var so = new SerializedObject(resultPanel);
            var prop = so.FindProperty("upgradeCatalog");
            if (prop != null)
            {
                prop.objectReferenceValue = upgradeCatalog;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(resultPanel);
                Debug.Log("[OpenWorldFinalize] ResultPanel.upgradeCatalog → UpgradeCatalog 연결.");
            }
        }
        else
        {
            Debug.LogWarning($"[OpenWorldFinalize] ResultPanel({resultPanel}) or UpgradeCatalog({upgradeCatalog}) null — wiring skip.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[OpenWorldFinalize] 완료.");
    }
}
