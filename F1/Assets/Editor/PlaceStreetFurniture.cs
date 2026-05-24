using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 거리 가구(가로등·표지판) 자동 배치 — 리뷰어 "거리 비어있음, 가로등 0" 피드백(세션 30).
/// 도로 인접 빈 셀의 도로 쪽 가장자리에 가로등을 규칙적으로, 교차로 근처엔 표지판.
/// collider 없음, 결정론적 hash, dialog 없음.
/// 메뉴: PitCrew/Track/Auto-Place Street Furniture
/// </summary>
public static class PlaceStreetFurniture
{
    const float CellSize = 6f;
    const float Y = 0f;
    const string RoadFbxRoot = "Assets/Model/kenney_city-kit-roads/Models/FBX format";

    // 가로등은 자주(거리를 따라 늘어서게), 표지판은 가끔.
    const float LampProbability = 0.55f;
    const float SignProbability = 0.12f;

    [MenuItem("PitCrew/Track/Auto-Place Street Furniture")]
    public static void Execute()
    {
        var guids = AssetDatabase.FindAssets("t:TrackLayout");
        if (guids == null || guids.Length == 0) { Debug.LogError("[Furniture] TrackLayout 없음."); return; }
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (layout == null) { Debug.LogError("[Furniture] 로드 실패."); return; }

        var lamps = new List<GameObject>();
        foreach (var fn in new[] { "light-square.fbx", "light-curved.fbx" })
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{RoadFbxRoot}/{fn}");
            if (go != null) lamps.Add(go);
        }
        var signs = new List<GameObject>();
        foreach (var fn in new[] { "sign-highway-basic.fbx", "sign-highway.fbx" })
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{RoadFbxRoot}/{fn}");
            if (go != null) signs.Add(go);
        }
        if (lamps.Count == 0) { Debug.LogError("[Furniture] 가로등 FBX 0개."); return; }
        Debug.Log($"[Furniture] 가로등 {lamps.Count} / 표지판 {signs.Count} 로드.");

        var existing = GameObject.Find("StreetFurniture");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("StreetFurniture");
        Undo.RegisterCreatedObjectUndo(root, "Place Street Furniture");

        var occ = ScanBuildingsOccupancy(layout.Width, layout.Height);

        int lamp = 0, sign = 0;
        for (int r = 0; r < layout.Height; r++)
        for (int c = 0; c < layout.Width; c++)
        {
            if (layout.IsRoad(c, r)) continue;
            if (layout.BuildingChar(c, r) != '.') continue;
            if (occ[c, r]) continue;

            int rdx = 0, rdy = 0;
            if (layout.IsRoad(c + 1, r)) { rdx = 1; }
            else if (layout.IsRoad(c - 1, r)) { rdx = -1; }
            else if (layout.IsRoad(c, r + 1)) { rdy = 1; }
            else if (layout.IsRoad(c, r - 1)) { rdy = -1; }
            else continue;

            // 교차로 인접 판정 (2개 이상 방향에 도로) → 표지판 후보
            int roadSides = (layout.IsRoad(c + 1, r) ? 1 : 0) + (layout.IsRoad(c - 1, r) ? 1 : 0)
                          + (layout.IsRoad(c, r + 1) ? 1 : 0) + (layout.IsRoad(c, r - 1) ? 1 : 0);

            int hash = (c * 40503301) ^ (r * 22801763) ^ 0x1234567;
            float roll = Mathf.Abs(hash % 1000) / 1000f;

            GameObject pick = null; bool isSign = false;
            if (signs.Count > 0 && roadSides >= 2 && roll < SignProbability)
            { pick = signs[Mathf.Abs(hash) % signs.Count]; isSign = true; }
            else if (roll < LampProbability)
            { pick = lamps[Mathf.Abs(hash) % lamps.Count]; }
            if (pick == null) continue;

            float posX = c * CellSize + rdx * 2.2f;   // 연석 쪽
            float posZ = r * CellSize + rdy * 2.2f;
            // 가로등 머리가 도로 위로 가게 도로 방향 바라봄
            float yaw = (rdx > 0) ? 270f : (rdx < 0) ? 90f : (rdy > 0) ? 180f : 0f;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pick);
            if (inst == null) continue;
            inst.name = (isSign ? "Sign_" : "Lamp_") + pick.name + $"_{c}_{r}";
            inst.transform.SetParent(root.transform, false);
            inst.transform.localPosition = new Vector3(posX, Y, posZ);
            inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            inst.transform.localScale = Vector3.one;
            foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);

            if (isSign) sign++; else lamp++;
        }
        Debug.Log($"[Furniture] ✓ 가로등 {lamp} / 표지판 {sign} 배치 (collider 없음).");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    static bool[,] ScanBuildingsOccupancy(int w, int h)
    {
        var occ = new bool[w, h];
        var b = GameObject.Find("Buildings");
        if (b == null) return occ;
        foreach (Transform t in b.transform)
        {
            var parts = t.name.Split('_');
            if (parts.Length < 4) continue;
            if (!int.TryParse(parts[2], out int c)) continue;
            if (!int.TryParse(parts[3], out int r)) continue;
            int ww = 1, dd = 1;
            if (parts.Length >= 5) { var sz = parts[4].Split('x'); if (sz.Length == 2) { int.TryParse(sz[0], out ww); int.TryParse(sz[1], out dd); } }
            for (int dr = 0; dr < dd; dr++) for (int dc = 0; dc < ww; dc++)
            { int cc = c + dc, rr = r + dr; if (cc >= 0 && cc < w && rr >= 0 && rr < h) occ[cc, rr] = true; }
        }
        return occ;
    }
}
