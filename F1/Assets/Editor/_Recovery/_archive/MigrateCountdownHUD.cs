using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 기존 씬에서 RunRoot에 붙어있던 CountdownHUD 컴포넌트를 제거하고
/// UIRoot 하위 별도 GameObject로 재생성. 세션 12 D-035 수정의 씬 마이그레이션.
/// </summary>
public static class MigrateCountdownHUD
{
    [MenuItem("PitCrew/UI/Migrate CountdownHUD to UIRoot")]
    public static void Execute()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // 1. RunRoot에 붙은 CountdownHUD 컴포넌트 제거
        var runRoot = GameObject.Find("RunRoot");
        if (runRoot != null)
        {
            var countdown = runRoot.GetComponent<CountdownHUD>();
            if (countdown != null)
            {
                Object.DestroyImmediate(countdown);
                Debug.Log("[Migrate] Removed CountdownHUD from RunRoot");
            }
        }

        // 2. UIRoot 하위에 이미 있으면 스킵, 없으면 생성. 어느 경우든 CanvasGroup 강제 확보.
        var uiRootT = UIRoot.Ensure();
        var existing = Object.FindFirstObjectByType<CountdownHUD>();
        if (existing == null)
        {
            var go = new GameObject("CountdownHUD", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(uiRootT, false);
            go.AddComponent<CountdownHUD>();
            Debug.Log("[Migrate] Created UIRoot/CountdownHUD with CanvasGroup");
        }
        else
        {
            if (existing.GetComponent<CanvasGroup>() == null)
            {
                existing.gameObject.AddComponent<CanvasGroup>();
                Debug.Log("[Migrate] Added missing CanvasGroup to existing " + existing.name);
            }
            else
            {
                Debug.Log("[Migrate] CountdownHUD + CanvasGroup OK: " + existing.name);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Migrate] Scene saved.");
    }
}
