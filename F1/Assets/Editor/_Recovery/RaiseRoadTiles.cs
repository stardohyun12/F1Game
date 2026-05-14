using UnityEditor;
using UnityEngine;

// 모든 Track 타일을 일정량 위로 — grass와 시각적 분리 (curb 노출).
public static class RaiseRoadTiles
{
    [MenuItem("PitCrew/Fix/Raise Road Tiles 5cm")]
    public static void Execute()
    {
        const float dy = 0.05f;
        var track = GameObject.Find("Track");
        if (track == null) { Debug.LogError("[Raise] no Track"); return; }
        int n = 0;
        foreach (Transform t in track.transform)
        {
            t.position = new Vector3(t.position.x, t.position.y + dy, t.position.z);
            n++;
        }
        Debug.Log($"[Raise] raised {n} tiles by {dy}m");

        // 차도 함께 — box 바닥이 새 road top과 맞도록
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car != null)
        {
            car.transform.position = new Vector3(car.transform.position.x, car.transform.position.y + dy, car.transform.position.z);
            var rb = car.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            Debug.Log($"[Raise] car raised to y={car.transform.position.y:F3}");
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[Raise] saved.");
    }
}
