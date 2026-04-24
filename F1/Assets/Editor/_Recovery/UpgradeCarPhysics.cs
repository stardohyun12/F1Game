using UnityEditor;
using UnityEngine;

/// <summary>
/// 세션 14 후반: Race prefab의 Rigidbody + ArcadeCarController 물리 수치를 "무게감" 쪽으로 대폭 조정.
/// 도현 피드백 "운전 더 현실적이게" 반영. D-007/D-019 Tier A 수치 튜닝 연장 (풀 시뮬 금지).
///
/// v1 튠 목표: 마을 배송에 맞는 묵직한 세단/밴 느낌.
/// - mass 1200kg (소형 SUV/세단)
/// - accelTime 4.5s (현재 2.8에서 확장 — 관성감 증가)
/// - brakeDecel 5 (6 → 5, 급정거 완화)
/// - airDrag 0.25 (0.35 → 0.25, 코스팅감)
/// - angularDamping 2.5 (1.5 → 2.5, 회전 관성)
/// - linearDamping 0.5 (수동 0 → 미세 공기저항)
/// </summary>
public static class UpgradeCarPhysics
{
    const string RacePrefabPath = "Assets/Prefabs/Race.prefab";

    [MenuItem("PitCrew/Car/Upgrade Physics v1 (Weighted)")]
    public static void Execute()
    {
        var editable = PrefabUtility.LoadPrefabContents(RacePrefabPath);
        if (editable == null) { Debug.LogError("[CarUpgrade] Race prefab missing: " + RacePrefabPath); return; }

        // 1. Rigidbody
        var rb = editable.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 1200f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 2.5f;
            Debug.Log("[CarUpgrade] Rigidbody: mass=1200, linearDamp=0.5, angularDamp=2.5");
        }
        else
        {
            Debug.LogWarning("[CarUpgrade] Race prefab에 Rigidbody 없음.");
        }

        // 2. ArcadeCarController private serialized fields
        var car = editable.GetComponent<ArcadeCarController>();
        if (car != null)
        {
            var so = new SerializedObject(car);
            TrySet(so, "accelTime", 4.5f);
            TrySet(so, "brakeDecel", 5f);
            TrySet(so, "airDrag", 0.25f);
            TrySet(so, "angularDamping", 2.5f);
            so.ApplyModifiedProperties();
            Debug.Log("[CarUpgrade] ArcadeCar: accelTime=4.5, brakeDecel=5, airDrag=0.25, angularDamping=2.5");
        }
        else
        {
            Debug.LogWarning("[CarUpgrade] ArcadeCarController 없음.");
        }

        PrefabUtility.SaveAsPrefabAsset(editable, RacePrefabPath);
        PrefabUtility.UnloadPrefabContents(editable);
        Debug.Log("[CarUpgrade] v1 Weighted 완료. Play 후 체감 이상하면 값 튜닝.");
    }

    static void TrySet(SerializedObject so, string name, float value)
    {
        var p = so.FindProperty(name);
        if (p != null) p.floatValue = value;
        else Debug.LogWarning("[CarUpgrade] SerializedProperty 미발견: " + name);
    }
}
