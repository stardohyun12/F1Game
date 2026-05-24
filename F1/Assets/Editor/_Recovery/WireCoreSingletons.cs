#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UpgradeSystem · TireSystem 싱글톤 GameObject 복원 — 세션31 맵 리빌드(d89a466)로 유실됨.
/// 기존엔 이 둘만 깔끔히 생성하는 단독 메뉴가 없고 Phase1 통합 메뉴에 묶여 있었는데,
/// Phase1은 카탈로그 재생성·차량 yaw 변경·CargoView 중복 등 부작용이 커서 비권장.
/// 이 메뉴는 두 싱글톤만 멱등 생성한다. GameContext.autoFindMissingOnAwake가 타입으로
/// 자동 등록하므로 명시 배선 불필요 (GameContext.cs:101,105).
/// 메뉴: PitCrew/Restore/Wire Core Singletons
/// </summary>
public static class WireCoreSingletons
{
    [MenuItem("PitCrew/Restore/Wire Core Singletons")]
    public static void Run()
    {
        int created = 0;
        created += Ensure<UpgradeSystem>("UpgradeSystem");
        created += Ensure<TireSystem>("TireSystem");

        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[WireCoreSingletons] ✓ UpgradeSystem/TireSystem 복원 (신규 생성 {created}개).");
    }

    static int Ensure<T>(string goName) where T : Component
    {
        if (Object.FindFirstObjectByType<T>() != null) return 0;
        var go = GameObject.Find(goName) ?? new GameObject(goName);
        if (go.GetComponent<T>() == null) go.AddComponent<T>();
        return 1;
    }
}
#endif
