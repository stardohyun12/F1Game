using UnityEditor;
using UnityEngine;

public static class FixDrivingPolish
{
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }

        // 1. 차 위치: y=0.15 (BoxCollider local bottom 0.005 + Track 0.11 + 마진)
        car.transform.position = new Vector3(48f, 0.15f, 36f);
        car.transform.rotation = Quaternion.identity;
        var rb = car.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 2. Continuous Dynamic 콜리전 — 빠른 차량 + thin ground 침투 방지
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Debug.Log($"[Polish] Rb: pos={car.transform.position}, collisionMode={rb.collisionDetectionMode}");

        // 3. PhysicMaterial 마찰 0.3 → 0.15 (forward force 살리기)
        const string path = "Assets/Materials/CarLowFriction.physicMaterial";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (mat != null)
        {
            mat.dynamicFriction = 0.15f;
            mat.staticFriction = 0.15f;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Polish] PhysicMaterial friction → 0.15");
        }

        // 4. CargoBox는 코드에서 smoothing/lowSpeedRelief 조정됨 — 인스펙터 threshold 한 번 더 안전 마진
        var cargo = car.GetComponent<CargoBox>();
        if (cargo != null)
        {
            var so = new SerializedObject(cargo);
            so.FindProperty("longWarnG").floatValue = 1.5f;
            so.FindProperty("latWarnG").floatValue = 1.8f;
            so.FindProperty("longDamageMul").floatValue = 5f;  // 10 → 5 (덜 가혹)
            so.FindProperty("latDamageMul").floatValue = 4f;   // 8 → 4
            so.ApplyModifiedProperties();
            Debug.Log($"[Polish] CargoBox thresholds: long 1.5/5, lat 1.8/4");
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[Polish] Scene saved. Enter Play and try W.");
    }
}
