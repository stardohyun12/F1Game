using UnityEditor;
using UnityEngine;

public static class AlignGroundToTrack
{
    public static void Execute()
    {
        // Track 표면 y 측정 (높은 곳에서 raycast). GroundField BoxCollider 임시 비활성.
        var ground = GameObject.Find("GroundField");
        BoxCollider gBox = ground != null ? ground.GetComponent<BoxCollider>() : null;
        bool gWasEnabled = gBox != null && gBox.enabled;
        if (gBox != null) gBox.enabled = false;

        // 차 스폰 위치 (48, _, 36) 평지 도로 표면 = 0.115 (처음 진단 raycast 결과).
        // slope tile들은 더 높지만 차 스폰은 평지.
        float trackY = 0.115f;
        Debug.Log($"[Align] Using flat road y = {trackY:F3} at spawn (48,36)");

        if (gBox != null) gBox.enabled = gWasEnabled;

        // GroundField top y를 Track top과 일치
        if (ground != null)
        {
            ground.transform.position = new Vector3(48f, trackY, 36f);
            gBox.center = new Vector3(0f, -0.5f, 0f);
            gBox.size = new Vector3(400f, 1f, 400f);
            Debug.Log($"[Align] GroundField top y = {ground.transform.position.y + gBox.center.y + gBox.size.y * 0.5f:F3}");
        }

        // 차 위치도 그 위로
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car != null)
        {
            car.transform.position = new Vector3(48f, trackY + 0.05f, 36f);
            car.transform.rotation = Quaternion.identity;
            var rb = car.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log($"[Align] Car y = {car.transform.position.y:F3}");
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[Align] Scene saved.");
    }
}
