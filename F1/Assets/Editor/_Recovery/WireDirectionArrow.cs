#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// W2 채널 8 — 월드 방향 화살표 씬 와이어링.
/// DirectionArrow GameObject를 씬 루트에 추가. 멱등 (이미 있으면 skip).
/// DirectionArrow는 자가구성 — 컴포넌트만 붙으면 Awake에서 스프라이트 자식을 만든다.
/// </summary>
public static class WireDirectionArrow
{
    [MenuItem("PitCrew/W2/Wire Direction Arrow")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();

        if (Object.FindFirstObjectByType<DirectionArrow>(FindObjectsInactive.Include) != null)
        {
            Debug.Log("[WireDirectionArrow] DirectionArrow 이미 존재 — skip.");
            return;
        }

        var go = new GameObject("DirectionArrow");
        go.AddComponent<DirectionArrow>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[WireDirectionArrow] DirectionArrow 씬에 추가 + 저장 완료.");

        var check = Object.FindFirstObjectByType<DirectionArrow>(FindObjectsInactive.Include);
        Debug.Log(check != null
            ? "[WireDirectionArrow][VERIFY] OK — DirectionArrow 씬에 존재."
            : "[WireDirectionArrow][VERIFY] 실패 — DirectionArrow 없음.");
    }
}
#endif
