#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// W2 채널 8 — 미니맵 씬 와이어링.
/// MiniMapHUD GameObject를 UIRoot 아래에 추가. 멱등 (이미 있으면 skip).
/// MiniMapHUD는 자가구성 — 컴포넌트만 붙으면 Awake에서 UI/카메라/RT 다 만든다.
/// </summary>
public static class WireMiniMap
{
    [MenuItem("PitCrew/W2/Wire MiniMap")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();

        // 이미 있으면 skip
        if (Object.FindFirstObjectByType<MiniMapHUD>(FindObjectsInactive.Include) != null)
        {
            Debug.Log("[WireMiniMap] MiniMapHUD 이미 존재 — skip.");
            return;
        }

        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot == null)
        {
            Debug.LogError("[WireMiniMap] UIRoot 못 찾음 — 씬에 UIRoot 캔버스가 있어야 함.");
            return;
        }

        var go = new GameObject("MiniMapHUD", typeof(RectTransform));
        go.transform.SetParent(uiRoot.transform, false);
        go.AddComponent<MiniMapHUD>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[WireMiniMap] MiniMapHUD를 UIRoot 아래 추가 + 씬 저장 완료.");

        // post-wire 검증 (asset.md 규칙)
        var check = Object.FindFirstObjectByType<MiniMapHUD>(FindObjectsInactive.Include);
        Debug.Log(check != null
            ? "[WireMiniMap][VERIFY] OK — MiniMapHUD 씬에 존재."
            : "[WireMiniMap][VERIFY] 실패 — MiniMapHUD 없음.");
    }
}
#endif
