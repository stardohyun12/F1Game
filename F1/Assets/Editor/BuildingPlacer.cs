using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scans TrackLayout.buildingMask and places BuildingDef instances under a "Buildings" root.
// For each non-empty cell:
//   - skip if cell is road (mask conflict)
//   - find adjacent road direction(s); entry rotates to face it
//   - pick prefab deterministically from catalog via (col,row) hash
//   - scale uniformly (same factor as road tiles) so Kenny's native per-prefab size variance is preserved
public static class BuildingPlacer
{
    const float CellSize = 6f;
    // Buildings fit their cellsWide x cellsDeep footprint with a small margin so they don't touch the road.
    // A 1x1 building occupies ~3.6m of its 4m cell; a 2x2 occupies ~7.2m of its 8m footprint.
    const float FootprintMargin = 0.9f;
    const float BuildingY = 0f;

    public static GameObject Build(TrackLayout layout)
    {
        if (layout == null) { Debug.LogError("BuildingPlacer: layout null"); return null; }
        if (layout.catalog == null) { Debug.LogWarning("BuildingPlacer: catalog not assigned — skipping buildings"); return null; }
        if (layout.buildingMask == null || layout.buildingMask.Length == 0) return null;

        var existing = GameObject.Find("Buildings");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("Buildings");
        Undo.RegisterCreatedObjectUndo(root, "Build Buildings");

        int width = layout.Width;
        int height = layout.Height;
        int placed = 0;
        // Occupancy grid: a multi-cell building marks all cells in its footprint so later iterations skip them.
        var occupied = new bool[width, height];

        for (int r = 0; r < height; r++)
        {
            for (int c = 0; c < width; c++)
            {
                if (occupied[c, r]) continue;
                char ch = layout.BuildingChar(c, r);
                if (ch == '.' || ch == ' ' || ch == '\0') continue;

                if (layout.IsRoad(c, r))
                {
                    Debug.LogWarning($"BuildingPlacer: cell ({c},{r}) is road but buildingMask has '{ch}' — skipping");
                    continue;
                }

                int hash = c * 73856093 ^ r * 19349663;
                var def = layout.catalog.Pick(ch, hash);
                if (def == null || def.prefab == null)
                {
                    Debug.LogWarning($"BuildingPlacer: no def/prefab for '{ch}' at ({c},{r})");
                    continue;
                }

                int w = Mathf.Max(1, def.cellsWide);
                int d = Mathf.Max(1, def.cellsDeep);

                // Validate footprint: all cells must be in-bounds, not road, not already occupied.
                bool footprintOK = true;
                for (int dr = 0; dr < d && footprintOK; dr++)
                    for (int dc = 0; dc < w && footprintOK; dc++)
                    {
                        int cc = c + dc, rr = r + dr;
                        if (cc >= width || rr >= height) { footprintOK = false; break; }
                        if (layout.IsRoad(cc, rr)) { footprintOK = false; break; }
                        if (occupied[cc, rr]) { footprintOK = false; break; }
                    }
                if (!footprintOK)
                {
                    Debug.LogWarning($"BuildingPlacer: {def.name} ({w}x{d}) at ({c},{r}) footprint conflicts with bounds/road/other building — skipping");
                    continue;
                }

                // Mark occupied cells.
                for (int dr = 0; dr < d; dr++)
                    for (int dc = 0; dc < w; dc++)
                        occupied[c + dc, r + dr] = true;

                // Facing: scan the footprint perimeter for adjacent road (N,E,S,W priority).
                Vector3 faceDir = Vector3.forward;
                bool faceFound = false;
                for (int dc = 0; dc < w && !faceFound; dc++)
                    if (layout.IsRoad(c + dc, r + d)) { faceDir = Vector3.forward; faceFound = true; }   // +Z (north of footprint)
                for (int dr = 0; dr < d && !faceFound; dr++)
                    if (layout.IsRoad(c + w, r + dr)) { faceDir = Vector3.right; faceFound = true; }    // +X (east)
                for (int dc = 0; dc < w && !faceFound; dc++)
                    if (layout.IsRoad(c + dc, r - 1)) { faceDir = Vector3.back; faceFound = true; }     // -Z (south)
                for (int dr = 0; dr < d && !faceFound; dr++)
                    if (layout.IsRoad(c - 1, r + dr)) { faceDir = Vector3.left; faceFound = true; }     // -X (west)

                // Rotate so that the prefab's entryLocalDir aligns with faceDir.
                // Compute Y-axis yaw only — FromToRotation can pick X/Z axes and flip the building upside-down.
                Vector3 localEntry = def.entryLocalDir.sqrMagnitude > 0.001f ? def.entryLocalDir.normalized : Vector3.forward;
                float localYaw = Mathf.Atan2(localEntry.x, localEntry.z) * Mathf.Rad2Deg;
                float targetYaw = Mathf.Atan2(faceDir.x, faceDir.z) * Mathf.Rad2Deg;
                Quaternion rotation = Quaternion.Euler(0f, targetYaw - localYaw, 0f);

                // Position at center of footprint. Scale fits the smaller axis so the building never overflows
                // its cellsWide x cellsDeep footprint. nativeSize is taken as the prefab's max(x,z) at scale 1.
                float posX = (c + (w - 1) * 0.5f) * CellSize;
                float posZ = (r + (d - 1) * 0.5f) * CellSize;
                float nativeSize = def.nativeSize > 0.001f ? def.nativeSize : 1f;
                float scale = (CellSize * Mathf.Min(w, d) * FootprintMargin) / nativeSize;
                // 세션 17 D-050: heightMask 통합. footprint 평균 높이 사용.
                float yOffset = 0f;
                if (layout.heightMask != null)
                {
                    float sumH = 0f;
                    int n = 0;
                    for (int dr = 0; dr < d; dr++)
                        for (int dc = 0; dc < w; dc++) { sumH += layout.GetHeight(c + dc, r + dr); n++; }
                    if (n > 0) yOffset = sumH / n;
                }

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(def.prefab);
                inst.name = w == 1 && d == 1 ? $"B_{ch}_{c}_{r}" : $"B_{ch}_{c}_{r}_{w}x{d}";
                inst.transform.SetParent(root.transform, false);
                inst.transform.localPosition = new Vector3(posX, BuildingY + yOffset, posZ);
                inst.transform.localRotation = rotation;
                inst.transform.localScale = Vector3.one * scale;
                if (def.addCollider) AddColliders(inst);
                placed++;
            }
        }

        int problems = VerifyPlacements(root);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (problems > 0) Debug.LogError($"BuildingPlacer: placed {placed} buildings, {problems} FAILED verification — see warnings above.");
        else Debug.Log($"BuildingPlacer: placed {placed} buildings, all verified.");
        return root;
    }

    // Adds a MeshCollider to every child MeshFilter so the car can't drive through buildings.
    // Kenny FBX prefabs ship without colliders, so we attach them at placement time.
    static void AddColliders(GameObject inst)
    {
        var filters = inst.GetComponentsInChildren<MeshFilter>();
        foreach (var mf in filters)
        {
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponent<Collider>() != null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
        }
    }

    // Post-build sanity check. Flags buildings that are upside-down, tilted, or embedded below ground.
    // Returns the number of problem instances found.
    static int VerifyPlacements(GameObject root)
    {
        int problems = 0;
        foreach (Transform child in root.transform)
        {
            var euler = child.localRotation.eulerAngles;
            float tiltX = Mathf.DeltaAngle(0f, euler.x);
            float tiltZ = Mathf.DeltaAngle(0f, euler.z);
            if (Mathf.Abs(tiltX) > 1f || Mathf.Abs(tiltZ) > 1f)
            {
                Debug.LogWarning($"BuildingPlacer VERIFY: {child.name} has non-Y rotation (eulerX={tiltX:0.0}, eulerZ={tiltZ:0.0}) — may be flipped/tilted.");
                problems++;
                continue;
            }

            var renderers = child.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) continue;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            // 세션 17: heightMask와 호환 — anchor의 yOffset만큼 임계 완화.
            float threshold = -0.05f + child.localPosition.y;
            if (b.min.y < threshold)
            {
                Debug.LogWarning($"BuildingPlacer VERIFY: {child.name} bounds.min.y={b.min.y:0.00} below local ground (threshold={threshold:0.00}) — building may be sunk.");
                problems++;
            }
        }
        return problems;
    }

    [MenuItem("PitCrew/Rebuild Buildings From Layout")]
    public static void RebuildFromAssignedLayout()
    {
        var layouts = AssetDatabase.FindAssets("t:TrackLayout");
        if (layouts.Length == 0) { Debug.LogError("No TrackLayout SO found in project."); return; }
        var path = AssetDatabase.GUIDToAssetPath(layouts[0]);
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(path);
        Debug.Log($"BuildingPlacer: using layout {path}");
        Build(layout);
    }
}
