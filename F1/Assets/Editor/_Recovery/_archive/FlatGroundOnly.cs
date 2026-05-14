using UnityEditor;
using UnityEngine;

public static class FlatGroundOnly
{
    public static void Execute()
    {
        // 1. Track 자식의 모든 MeshCollider 비활성 (시각만 유지)
        var track = GameObject.Find("Track");
        int disabled = 0;
        if (track != null)
        {
            var mcs = track.GetComponentsInChildren<MeshCollider>();
            foreach (var mc in mcs)
            {
                if (mc.enabled) { mc.enabled = false; disabled++; }
            }
            Debug.Log($"[Flat] Disabled {disabled} Track MeshColliders.");
        }
        else Debug.LogWarning("[Flat] Track GO not found.");

        // 2. GroundField BoxCollider 보장 + 충분히 큰 평면
        var ground = GameObject.Find("GroundField");
        if (ground == null)
        {
            ground = new GameObject("GroundField");
            Debug.Log("[Flat] Created GroundField.");
        }
        var box = ground.GetComponent<BoxCollider>();
        if (box == null) box = ground.AddComponent<BoxCollider>();
        ground.transform.position = new Vector3(48f, 0f, 36f);
        box.center = new Vector3(0f, -0.5f, 0f);
        box.size = new Vector3(400f, 1f, 400f);
        box.isTrigger = false;
        Debug.Log($"[Flat] GroundField BoxCollider: center={box.center}, size={box.size}, world top y={ground.transform.position.y + box.center.y + box.size.y * 0.5f}");

        // 3. 차 위치 표면 위 안전 마진
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car != null)
        {
            car.transform.position = new Vector3(48f, 0.1f, 36f);
            car.transform.rotation = Quaternion.identity;
            var rb = car.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log($"[Flat] Car reset to {car.transform.position}");
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[Flat] Scene saved.");
    }
}
