using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 현재 열린 씬에 UIRoot Canvas를 보장하고 FuelHUD/GearHUD GameObject를 배치.
/// 이미 존재하면 스킵. CountdownHUD는 Awake에서 자동 재부착되므로 그대로 둔다.
/// </summary>
public static class SetupUIScene
{
    [MenuItem("PitCrew/UI/Setup UI Scene (Current)")]
    public static void Execute()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // UIRoot
        var rootT = UIRoot.Ensure();
        Debug.Log("[UIScene] UIRoot ready: " + rootT.name);

        // FuelHUD
        if (Object.FindFirstObjectByType<FuelHUD>() == null)
        {
            var go = new GameObject("FuelHUD", typeof(RectTransform));
            go.transform.SetParent(rootT, false);
            go.AddComponent<FuelHUD>();
            Debug.Log("[UIScene] Added FuelHUD");
        }

        // GearHUD
        if (Object.FindFirstObjectByType<GearHUD>() == null)
        {
            var go = new GameObject("GearHUD", typeof(RectTransform));
            go.transform.SetParent(rootT, false);
            go.AddComponent<GearHUD>();
            Debug.Log("[UIScene] Added GearHUD");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[UIScene] Scene saved: " + scene.path);
    }
}
