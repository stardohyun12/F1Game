using UnityEditor;
using UnityEngine;

public static class FixCarSpawnAndPhysics
{
    public static void Execute()
    {
        // 1. 차 위치 리셋 — Track 표면 + BoxCollider half + 5cm 안전마진
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }
        var box = car.GetComponent<BoxCollider>();

        // 단순 강제: Track 표면 y≈0.11 + BoxCollider local bottom ≈0.005 + 30cm 안전마진.
        // Play 진입 시 gravity가 안착시킴.
        float spawnY = 0.5f;
        car.transform.position = new Vector3(car.transform.position.x, spawnY, car.transform.position.z);
        // 회전 리셋 (이전 lift나 spin 잔재 제거)
        car.transform.rotation = Quaternion.identity;
        var rb = car.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Debug.Log($"[FixSpawn] Car reset to (48, {spawnY}, 36), rotation=identity, velocity=0");

        // 2. PhysicMaterial 마찰 조정 — 0.05 너무 미끌 → 0.3 (자연스러운 마찰)
        const string path = "Assets/Materials/CarLowFriction.physicMaterial";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (mat != null)
        {
            mat.dynamicFriction = 0.3f;
            mat.staticFriction = 0.3f;
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FixSpawn] PhysicMaterial friction 0.05 → 0.3");
        }

        // 3. CargoBox 임계값 상향 — SerializedObject로 인스펙터 값 조정
        var cargo = car.GetComponent<CargoBox>();
        if (cargo != null)
        {
            var so = new SerializedObject(cargo);
            var longWarn = so.FindProperty("longWarnG");
            var latWarn = so.FindProperty("latWarnG");
            if (longWarn != null) longWarn.floatValue = 1.3f;
            if (latWarn != null) latWarn.floatValue = 1.5f;
            so.ApplyModifiedProperties();
            Debug.Log($"[FixSpawn] CargoBox longWarnG → 1.3, latWarnG → 1.5");
        }
        else Debug.LogWarning("[FixSpawn] CargoBox not found on car");

        // 씬 저장
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixSpawn] Scene saved.");
    }
}
