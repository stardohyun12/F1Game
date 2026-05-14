using UnityEditor;
using UnityEngine;

// BaseHorizon plane(지평선)의 visual top을 GroundField top(=0.17)에 맞춤.
// 이전: localPos.y=-0.21 → 차가 horizon 위로 21cm 떠 보임.
// 이후: localPos.y=0 → world y=0.17 = 도로/잔디 cube top과 동일 평면.
public static class AlignBaseHorizon
{
    public static void Execute()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OpenWorld_v2.unity");

        var bh = GameObject.Find("BaseHorizon");
        if (bh == null)
        {
            // GroundField 아래 자식 검색 (Find가 inactive 경로일 때 대비)
            var gf = GameObject.Find("GroundField");
            if (gf != null) bh = gf.transform.Find("BaseHorizon")?.gameObject;
        }
        if (bh == null) { Debug.LogError("[AlignBH] BaseHorizon not found"); return; }

        var t = bh.transform;
        Vector3 oldLocal = t.localPosition;
        Vector3 oldWorld = t.position;
        // GroundField world.y(=0.17)와 같은 평면이 되려면 localPos.y = 0 (Plane mesh y=0 local).
        // 도로 top(0.17)과 z-fighting 안 나게 1cm 아래. world y = 0.16.
        t.localPosition = new Vector3(oldLocal.x, -0.01f, oldLocal.z);
        Debug.Log($"[AlignBH] localPos.y {oldLocal.y:F3} → 0.000 (world.y {oldWorld.y:F3} → {t.position.y:F3})");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[AlignBH] saved.");
    }
}
