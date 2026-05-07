using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Project가 InputSystem 패키지로 설정 → StandaloneInputModule(legacy) 제거 + InputSystemUIInputModule로 교체.
// 세션 17 OpenWorld_v2: Play 진입 시 StandaloneInputModule.ShouldActivateModule이 UnityEngine.Input.GetButtonDown 호출해 InvalidOperationException 폭발.
public static class FixEventSystemInputModule
{
    [MenuItem("PitCrew/Layout/Fix EventSystem Input Module")]
    public static void Execute()
    {
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
            Debug.Log("[FixEventSystem] EventSystem 신규 생성.");
        }
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null)
        {
            Object.DestroyImmediate(legacy);
            Debug.Log("[FixEventSystem] StandaloneInputModule 제거.");
        }
        var newModule = es.GetComponent<InputSystemUIInputModule>();
        if (newModule == null)
        {
            es.gameObject.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[FixEventSystem] InputSystemUIInputModule 추가.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[FixEventSystem] 완료.");
    }
}
