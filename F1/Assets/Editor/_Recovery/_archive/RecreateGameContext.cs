using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 진짜 진단: GameContext MonoBehaviour의 m_Script가 internal stub 참조 (외부 GUID 안 가리킴).
/// AddComponent로 정상 GUID 참조 새로 만들기 위해 GameObject 통째 재생성.
/// AutoFindMissing이 Awake에서 시스템들 자동 연결하므로 SerializeField는 비어둠.
/// </summary>
public static class RecreateGameContext
{
    public static void Execute()
    {
        var old = GameObject.Find("GameContext");
        if (old != null)
        {
            Debug.Log("[Recreate] Destroying old GameContext GameObject (broken script ref).");
            Object.DestroyImmediate(old);
        }

        var go = new GameObject("GameContext");
        go.AddComponent<GameContext>();
        Debug.Log("[Recreate] New GameContext created with fresh GUID script reference.");

        EditorUtility.SetDirty(go);
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Recreate] Scene saved.");
    }
}
