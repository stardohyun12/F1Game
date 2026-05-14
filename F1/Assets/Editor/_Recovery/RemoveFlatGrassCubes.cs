using UnityEditor;
using UnityEngine;

// 평지 잔디 큐브(GG_*) 제거 — BaseHorizon plane이 연속 잔디 역할이라 중복.
// 도로 측면 ↔ 잔디 큐브 측면 z-fight 원인. 플래토(GP_*)는 유지.
public static class RemoveFlatGrassCubes
{
    [MenuItem("PitCrew/Fix/Remove Flat Grass Cubes")]
    public static void Execute()
    {
        var ground = GameObject.Find("GroundField");
        if (ground == null) { Debug.LogError("[GG] no GroundField"); return; }

        int removed = 0;
        // 자식 역순으로 (삭제 중 인덱스 안 깨지게)
        for (int i = ground.transform.childCount - 1; i >= 0; i--)
        {
            var child = ground.transform.GetChild(i);
            if (child.name.StartsWith("GG_"))
            {
                Object.DestroyImmediate(child.gameObject);
                removed++;
            }
        }
        Debug.Log($"[GG] removed {removed} flat grass cubes (GG_*). GP_/BaseHorizon 유지.");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[GG] saved.");
    }
}
