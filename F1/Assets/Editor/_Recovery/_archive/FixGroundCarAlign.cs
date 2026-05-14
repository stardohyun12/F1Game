using UnityEditor;
using UnityEngine;

public static class FixGroundCarAlign
{
    public static void Execute()
    {
        // 도로 mesh top y=0.170 (평지 기준). GroundField BoxCollider top을 거기에 맞춤.
        const float roadTopY = 0.170f;

        var ground = GameObject.Find("GroundField");
        if (ground != null)
        {
            var gBox = ground.GetComponent<BoxCollider>();
            // BoxCollider: center.y=-0.5, size.y=1 → top = transform.y + 0
            // → transform.y = roadTopY로 설정하면 top = roadTopY
            var p = ground.transform.position;
            ground.transform.position = new Vector3(p.x, roadTopY, p.z);
            Debug.Log($"[Fix] GroundField pos.y {p.y:F3} → {roadTopY:F3}, collider top = {gBox.bounds.max.y:F3}");
        }

        // 차도 도로 위로 보정 (현 위치 y=0.165 → roadTopY + 약간 띄움)
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car != null)
        {
            var p = car.transform.position;
            car.transform.position = new Vector3(p.x, roadTopY + 0.05f, p.z);
            car.transform.rotation = Quaternion.identity;
            var rb = car.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Debug.Log($"[Fix] Car pos.y {p.y:F3} → {roadTopY + 0.05f:F3}");
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[Fix] Scene saved.");
    }
}
