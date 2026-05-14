#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 세션 24 점검 수정 — 씬 데이터 2건.
/// 1. 차(Race) 스폰이 CentralCross 배달지점 위(겹침) → 6m 기둥이 차 관통. 옆 도로칸으로 이동.
/// 2. PreRunSetupPanel — 세션 21에 스크립트 삭제됐는데 씬에 빈 GameObject(missing script) 잔존 → 삭제.
///
/// 코드 수정(BarHUDBase/IntegrityHUD/CargoView/RunController)은 별도. 이 스크립트는 씬 데이터만.
/// 멱등 — 이미 옮겨졌으면 skip. post-fix 검증 포함 (asset.md 규칙).
/// </summary>
public static class FixSpawnAndCleanup
{
    // 새 스폰 위치 — Tile_Straight_7_6_r0 도로칸. CentralCross(48,36)에서 X로 6m.
    static readonly Vector3 NewSpawnXZ = new Vector3(42f, 0f, 36f);
    const float ClearRadius = 3f; // 배달지점과 이만큼은 떨어져야 함.

    [MenuItem("PitCrew/Fix/Spawn And Cleanup")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();
        bool changed = false;

        // --- 1. 차 이동 ---
        var race = GameObject.Find("Race");
        if (race == null)
        {
            Debug.LogWarning("[FixSpawnAndCleanup] Race 못 찾음 — 차 이동 skip.");
        }
        else
        {
            float nearest = NearestDeliveryDist(race.transform.position);
            if (nearest < ClearRadius)
            {
                var old = race.transform.position;
                race.transform.position = new Vector3(NewSpawnXZ.x, old.y, NewSpawnXZ.z);
                Debug.Log($"[FixSpawnAndCleanup] Race {old} → {race.transform.position} (배달지점과 {nearest:F1}m → 겹침 해소)");
                changed = true;
            }
            else
            {
                Debug.Log($"[FixSpawnAndCleanup] Race 이미 배달지점에서 {nearest:F1}m 떨어짐 — 이동 skip.");
            }
        }

        // --- 2. PreRunSetupPanel 삭제 ---
        GameObject prs = null;
        foreach (var g in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (g.name == "PreRunSetupPanel" && g.scene == scene) { prs = g; break; }
        }
        if (prs != null)
        {
            Object.DestroyImmediate(prs);
            Debug.Log("[FixSpawnAndCleanup] PreRunSetupPanel (missing-script 빈 오브젝트) 삭제.");
            changed = true;
        }
        else
        {
            Debug.Log("[FixSpawnAndCleanup] PreRunSetupPanel 없음 — 삭제 skip.");
        }

        // --- 저장 ---
        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[FixSpawnAndCleanup] 씬 저장 완료.");
        }

        Verify();
    }

    static float NearestDeliveryDist(Vector3 pos)
    {
        float best = float.MaxValue;
        foreach (var dp in Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var a = new Vector2(pos.x, pos.z);
            var b = new Vector2(dp.transform.position.x, dp.transform.position.z);
            best = Mathf.Min(best, Vector2.Distance(a, b));
        }
        return best;
    }

    /// <summary>post-fix 검증 — 침묵 실패 방지 (asset.md 규칙).</summary>
    static void Verify()
    {
        var race = GameObject.Find("Race");
        if (race != null)
        {
            float d = NearestDeliveryDist(race.transform.position);
            if (d < ClearRadius)
                Debug.LogError($"[FixSpawnAndCleanup][VERIFY] 실패 — Race가 여전히 배달지점 {d:F1}m 이내.");
            else
                Debug.Log($"[FixSpawnAndCleanup][VERIFY] OK — Race가 배달지점에서 {d:F1}m.");
        }

        bool prsGone = true;
        foreach (var g in Resources.FindObjectsOfTypeAll<GameObject>())
            if (g.name == "PreRunSetupPanel" && g.scene == SceneManager.GetActiveScene()) prsGone = false;
        Debug.Log(prsGone
            ? "[FixSpawnAndCleanup][VERIFY] OK — PreRunSetupPanel 없음."
            : "[FixSpawnAndCleanup][VERIFY] 실패 — PreRunSetupPanel 아직 존재.");
    }
}
#endif
