using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixGroundColliders
{
    public static void Execute()
    {
        // 1) Track 타일 각자에 MeshCollider 자동 부착 (mesh가 있는 자식 모두).
        int added = 0, skipped = 0;
        var trackRoot = GameObject.Find("Track");
        if (trackRoot != null)
        {
            foreach (Transform child in trackRoot.transform)
            {
                var mfs = child.GetComponentsInChildren<MeshFilter>();
                foreach (var mf in mfs)
                {
                    if (mf.sharedMesh == null) continue;
                    if (mf.GetComponent<Collider>() != null) { skipped++; continue; }
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    added++;
                }
            }
        }
        Debug.Log($"[FixGround] Track MeshColliders added: {added}, skipped(existing): {skipped}");

        // 2) GroundField에 안전망 BoxCollider — 트랙 영역 전체 cover.
        // OpenWorld_v2는 ~17x13 cell, cell=6m. 중심 (48, 0, 36) 추정. 큼직하게 200x200.
        var ground = GameObject.Find("GroundField");
        if (ground != null)
        {
            if (ground.GetComponent<Collider>() == null)
            {
                var bc = ground.AddComponent<BoxCollider>();
                bc.center = new Vector3(48, -0.5f, 36);
                bc.size = new Vector3(200, 1, 200);
                Debug.Log("[FixGround] GroundField BoxCollider added (200x1x200 at 48,-0.5,36).");
            }
            else Debug.Log("[FixGround] GroundField already has collider.");
        }
        else Debug.LogWarning("[FixGround] GroundField not found — skipping safety net.");

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixGround] Scene saved.");
    }
}
