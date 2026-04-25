using UnityEditor;
using UnityEngine;

/// <summary>
/// 세션 14 후반: UpgradeCarPhysics가 prefab 메모리에 남긴 mass 1200 등 원복.
/// 도현 "운행이 안됨" 피드백 → 원래 값으로 복원.
///
/// 원복 값:
/// - rb.mass: 1200 → 100 (세션 4 D-005 기록값)
/// - rb.linearDamping: 0.5 → 0 (UpgradeCarPhysics가 추가, 원래 0)
/// - ArcadeCar.accelTime: 4.5 → 2.8 (코드 기본값)
/// - ArcadeCar.brakeDecel: 5 → 6 (코드 기본값)
/// - ArcadeCar.airDrag: 0.25 → 0.35 (D-019 기록값)
/// - ArcadeCar.angularDamping: 2.5 유지 (D-019 명시)
/// </summary>
public static class RestoreCarPhysicsDefault
{
    const string RacePrefabPath = "Assets/Prefabs/Race.prefab";

    [MenuItem("PitCrew/Car/Restore Physics Default (mass 100)")]
    public static void Execute()
    {
        var edit = PrefabUtility.LoadPrefabContents(RacePrefabPath);
        if (edit == null) { Debug.LogError("[RestoreDefault] Race prefab missing."); return; }

        var rb = edit.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 100f;
            rb.linearDamping = 0f;
            Debug.Log("[RestoreDefault] Rigidbody mass 1200 → 100, linearDamping → 0");
        }

        var car = edit.GetComponent<ArcadeCarController>();
        if (car != null)
        {
            var so = new SerializedObject(car);
            TrySet(so, "accelTime", 2.8f);
            TrySet(so, "brakeDecel", 6f);
            TrySet(so, "airDrag", 0.35f);
            // angularDamping은 D-019의 2.5 유지 — 의도된 튜닝
            so.ApplyModifiedProperties();
            Debug.Log("[RestoreDefault] ArcadeCar accelTime=2.8 / brakeDecel=6 / airDrag=0.35");
        }

        PrefabUtility.SaveAsPrefabAsset(edit, RacePrefabPath);
        PrefabUtility.UnloadPrefabContents(edit);
        Debug.Log("[RestoreDefault] 완료 — Play로 운전 재확인.");
    }

    static void TrySet(SerializedObject so, string name, float value)
    {
        var p = so.FindProperty(name);
        if (p != null) p.floatValue = value;
    }
}
