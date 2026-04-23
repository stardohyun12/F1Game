using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// UIRoot와 모든 HUD GameObject를 삭제하고 SetupUITheme → SetupUIScene 재실행.
/// HUD 코드의 BuildIfNeeded 로직이 이미 생성된 자식을 재활용하므로
/// Theme 색상 갱신하려면 전체 재생성 필요.
/// </summary>
public static class RebuildUIFromScratch
{
    [MenuItem("PitCrew/UI/Rebuild UI From Scratch")]
    public static void Execute()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot != null)
        {
            Object.DestroyImmediate(uiRoot);
            Debug.Log("[RebuildUI] Removed UIRoot");
        }

        SetupUITheme.Execute();
        SetupUIScene.Execute();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[RebuildUI] Done");
    }
}
