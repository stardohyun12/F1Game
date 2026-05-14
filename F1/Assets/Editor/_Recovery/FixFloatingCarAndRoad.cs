using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 차/도로 공중부양 문제 진단 + 1-클릭 수정.
// Unity 메뉴: PitCrew > Fix > Diagnose Floating (읽기) / Snap Car & Tiles (쓰기).
public static class FixFloatingCarAndRoad
{
    const float SnapEpsilon = 0f; // 차 바닥 ↔ 도로 표면 (정확히 맞춤)
    const float FloatThreshold = 0.25f; // 이 이상 떠 있으면 floating 판정 (평지 도로)
    const float SlopeMaxTop = 3.3f; // SlantFlatHigh top ≈ 3.12m + 여유

    [MenuItem("PitCrew/Fix/Diagnose Floating")]
    public static void Diagnose()
    {
        Run(applyFixes: false);
    }

    // -batchmode -executeMethod FixFloatingCarAndRoad.BatchDiagnose
    public static void BatchDiagnose()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OpenWorld_v2.unity");
        Run(applyFixes: false);
    }

    public static void BatchSnap()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OpenWorld_v2.unity");
        Run(applyFixes: true);
    }

    // 차 snap + centerOfMass offset 안정값(+0.15) 적용 + angularDrag 조정.
    public static void BatchFixPhysics()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OpenWorld_v2.unity");
        Run(applyFixes: true);

        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("[FixPhysics] car missing"); return; }

        // 차 콜라이더 bounds 측정 — pivot 위 box 안쪽으로 COM 옮기기.
        var col = car.GetComponent<Collider>();
        Vector3 boxLocalCenter = Vector3.zero;
        float boxHeight = 0.5f;
        if (col != null)
        {
            // pivot 좌표계 기준 box 중심 local y
            float worldCenterY = col.bounds.center.y;
            boxLocalCenter.y = worldCenterY - car.transform.position.y;
            boxHeight = col.bounds.size.y;
        }
        // COM은 box 중앙 살짝 아래(안정성). box 중앙 y - 25%.
        float comY = boxLocalCenter.y - boxHeight * 0.25f;
        Vector3 newCom = new Vector3(0f, comY, 0f);

        var so = new SerializedObject(car);
        var p = so.FindProperty("centerOfMassOffset");
        if (p != null)
        {
            Vector3 old = p.vector3Value;
            p.vector3Value = newCom;
            so.ApplyModifiedProperties();
            Debug.Log($"[FixPhysics] centerOfMassOffset {old} → {newCom} (box localCenter.y={boxLocalCenter.y:F3}, height={boxHeight:F3})");
        }
        else Debug.LogWarning("[FixPhysics] centerOfMassOffset property not found");

        // angularDamping 살짝 ↑ (1.5 → 2.5) — 기울임 후 자세 복귀.
        var pAng = so.FindProperty("angularDamping");
        if (pAng != null) { float oldA = pAng.floatValue; pAng.floatValue = 2.5f; so.ApplyModifiedProperties(); Debug.Log($"[FixPhysics] angularDamping {oldA} → 2.5"); }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixPhysics] Saved.");
    }

    [MenuItem("PitCrew/Fix/Snap Car And Tiles To Ground")]
    public static void SnapAll()
    {
        Run(applyFixes: true);
    }

    static void Run(bool applyFixes)
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("[FixFloating] ArcadeCarController not found."); return; }

        var track = GameObject.Find("Track");
        if (track == null) { Debug.LogError("[FixFloating] Track GameObject not found."); return; }

        // ----- 1. Track 표면 정보 -----
        var tiles = new List<MeshRenderer>(track.GetComponentsInChildren<MeshRenderer>(false));
        if (tiles.Count == 0) { Debug.LogError("[FixFloating] no track tiles."); return; }

        var topY = tiles.Select(r => r.bounds.max.y).OrderBy(y => y).ToList();
        float medianTop = topY[topY.Count / 2];
        float minTop = topY[0];
        float maxTop = topY[topY.Count - 1];
        Debug.Log($"[FixFloating] Tiles={tiles.Count}, top.y median={medianTop:F3}, min={minTop:F3}, max={maxTop:F3}");

        // ----- 2. 차 진단 -----
        var carPos = car.transform.position;
        var carBox = car.GetComponentInChildren<Collider>();
        float carBottomY = carBox != null ? carBox.bounds.min.y : carPos.y;
        Debug.Log($"[FixFloating] Car pos={carPos}, bottom.y={carBottomY:F3}");

        // 차 아래로 raycast — Track 표면 찾기. 차 자식 콜라이더는 무시.
        Vector3 rayOrigin = carPos + Vector3.up * 5f;
        float carGroundY = float.NaN;
        string carGroundName = "(none)";
        var carColliders = new HashSet<Collider>(car.GetComponentsInChildren<Collider>(true));
        var hits = Physics.RaycastAll(rayOrigin, Vector3.down, 50f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (carColliders.Contains(h.collider)) continue;
            carGroundY = h.point.y;
            carGroundName = h.collider.gameObject.name;
            Debug.Log($"[FixFloating] Car raycast hit '{carGroundName}' y={carGroundY:F3} (gap={carBottomY - carGroundY:F3})");
            break;
        }
        if (float.IsNaN(carGroundY))
            Debug.LogWarning($"[FixFloating] Car raycast: NO HIT below (excluding car colliders).");

        // ----- 3. 도로 floating 진단 -----
        // 평지 도로 expected top ≈ medianTop. 그 이하 또는 SlopeMaxTop 이상은 의도(slope/bridge)로 간주.
        // floating = 인접 cell(같은 medianTop 그룹)인데 GroundField 위로 FloatThreshold 이상 떠 있음.
        var ground = GameObject.Find("GroundField");
        float groundTopY = 0f;
        if (ground != null)
        {
            var gc = ground.GetComponent<Collider>();
            if (gc != null) groundTopY = gc.bounds.max.y;
        }
        Debug.Log($"[FixFloating] GroundField top.y={groundTopY:F3}");

        // 각 tile에 대해, 아래 raycast로 collider 만나는지 검사 (mesh collider 자체 무시 위해 origin 위에서)
        int floatingCount = 0;
        var floatingTiles = new List<(GameObject go, float topY, float gapToGround)>();
        foreach (var rend in tiles)
        {
            var go = rend.gameObject;
            float top = rend.bounds.max.y;
            // slope/bridge는 의도된 높이
            if (top > medianTop + 0.5f && top < SlopeMaxTop) continue;
            // tile이 GroundField top 위로 (FloatThreshold) 이상 + medianTop 보다 위 → 평지 floating
            float gap = top - groundTopY;
            if (Mathf.Abs(top - medianTop) < 0.15f && gap > FloatThreshold && top > groundTopY + 0.5f)
            {
                floatingCount++;
                floatingTiles.Add((go, top, gap));
            }
        }
        if (floatingCount > 0)
        {
            Debug.LogWarning($"[FixFloating] {floatingCount} potentially-floating flat tiles. (showing first 5)");
            foreach (var f in floatingTiles.Take(5))
                Debug.Log($"  - {f.go.name}: top.y={f.topY:F3}, gap above GroundField={f.gapToGround:F3}");
        }
        else
        {
            Debug.Log("[FixFloating] No floating flat tiles detected.");
        }

        // ----- 4. 수정 (applyFixes) -----
        if (!applyFixes) { Debug.Log("[FixFloating] Diagnose only — no changes."); return; }

        // 4a. 차를 Track 표면 + Epsilon으로 snap
        if (!float.IsNaN(carGroundY))
        {
            float boxHalfFromBottom = carPos.y - carBottomY; // pivot ↔ bottom 거리
            float newY = carGroundY + SnapEpsilon + boxHalfFromBottom;
            car.transform.position = new Vector3(carPos.x, newY, carPos.z);
            car.transform.rotation = Quaternion.Euler(0f, car.transform.rotation.eulerAngles.y, 0f);
            var rb = car.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            Debug.Log($"[FixFloating] Car snapped to y={newY:F3} (ground={carGroundY:F3} + {SnapEpsilon} + halfH={boxHalfFromBottom:F3})");
        }

        // 4b. floating tile들 — medianTop 기준으로 y 낮춤 (Track 자식 transform.position 직접 조정)
        int snapped = 0;
        foreach (var f in floatingTiles)
        {
            var t = f.go.transform;
            float delta = f.topY - medianTop; // medianTop 으로 맞추려면 delta 만큼 내림
            t.position = new Vector3(t.position.x, t.position.y - delta, t.position.z);
            snapped++;
        }
        if (snapped > 0) Debug.Log($"[FixFloating] Snapped {snapped} floating tiles to medianTop.");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixFloating] Scene saved.");
    }
}
