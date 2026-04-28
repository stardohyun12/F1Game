using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 가로등 자동 배치 (세션 16 B4 — 도현 #3 맵 단조 시각 처치 보강).
///
/// 동작
/// - TrackLayout roadMask 스캔.
/// - 각 도로 셀의 4 모서리 중, 비도로 셀과 인접한 모서리에 가로등 배치.
/// - 결정론적 hash로 30% 확률만 배치 (과밀 회피).
/// - Pool: light-curved.fbx, light-square.fbx (Kenney 도로 키트).
/// - 별도 "StreetLights" GameObject root, collider 없음.
///
/// 메뉴: PitCrew/Track/Place Street Lights
/// </summary>
public static class PlaceStreetLights
{
    const float CellSize = 6f;
    const float CornerInset = 0.7f;     // 셀 모서리에서 안쪽으로 (도로 안 침범 X)
    const float LightY = 0f;
    const float PlaceProb = 0.30f;
    const string KenneyRoadFbxRoot = "Assets/Model/kenney_city-kit-roads/Models/FBX format";

    [MenuItem("PitCrew/Track/Place Street Lights")]
    public static void Execute()
    {
        var guids = AssetDatabase.FindAssets("t:TrackLayout");
        if (guids.Length == 0) { Debug.LogError("[Lights] TrackLayout 없음."); return; }
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (layout == null) { Debug.LogError("[Lights] layout 로드 실패."); return; }

        Debug.Log($"[Lights] === 가로등 배치 시작 (layout={layout.name}) ===");

        // 가로등 FBX 로드
        var lightCurved = AssetDatabase.LoadAssetAtPath<GameObject>($"{KenneyRoadFbxRoot}/light-curved.fbx");
        var lightSquare = AssetDatabase.LoadAssetAtPath<GameObject>($"{KenneyRoadFbxRoot}/light-square.fbx");
        if (lightCurved == null && lightSquare == null)
        {
            Debug.LogError("[Lights] light-curved/square.fbx 둘 다 없음. Kenney road kit 확인.");
            return;
        }
        Debug.Log($"[Lights] (1/3) 가로등 FBX 로드 (curved: {lightCurved != null}, square: {lightSquare != null}).");

        // 기존 root 제거
        var existing = GameObject.Find("StreetLights");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("StreetLights");
        Undo.RegisterCreatedObjectUndo(root, "Place StreetLights");

        int placed = 0;
        for (int r = 0; r < layout.Height; r++)
        {
            for (int c = 0; c < layout.Width; c++)
            {
                if (!layout.IsRoad(c, r)) continue;

                // 4 모서리 (NE, NW, SE, SW). 모서리 인접한 두 셀이 모두 비도로면 가로등 후보.
                TryCorner(layout, root.transform, c, r, +0.5f, +0.5f, "NE", lightCurved, lightSquare, ref placed);
                TryCorner(layout, root.transform, c, r, -0.5f, +0.5f, "NW", lightCurved, lightSquare, ref placed);
                TryCorner(layout, root.transform, c, r, +0.5f, -0.5f, "SE", lightCurved, lightSquare, ref placed);
                TryCorner(layout, root.transform, c, r, -0.5f, -0.5f, "SW", lightCurved, lightSquare, ref placed);
            }
        }
        Debug.Log($"[Lights] (2/3) {placed}개 가로등 배치 — 도로 모서리 {PlaceProb * 100:F0}% 확률.");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[Lights] (3/3) 씬 저장 완료.");

        EditorUtility.DisplayDialog("Street Lights",
            $"✓ {placed}개 가로등 배치.\n" +
            "위치: 도로 셀 모서리 중 비도로 인접 + 30% 확률.\n" +
            "Pool: light-curved + light-square (Kenney).",
            "확인");
    }

    static void TryCorner(TrackLayout layout, Transform parent, int c, int r,
        float dx, float dz, string suffix, GameObject lightCurved, GameObject lightSquare, ref int placed)
    {
        // 모서리 인접 두 셀이 모두 비도로여야 보도 모서리
        int cx = dx > 0 ? c + 1 : c - 1;
        int cz = dz > 0 ? r + 1 : r - 1;
        bool xRoad = layout.IsRoad(cx, r);
        bool zRoad = layout.IsRoad(c, cz);
        if (xRoad || zRoad) return;     // 인접 셀 중 하나라도 도로면 모서리 X (직각 코너 X)

        // 결정론적 hash
        int hash = c * 73856093 ^ r * 19349663 ^ suffix[0] * 12345;
        float roll = Mathf.Abs((hash % 1000)) / 1000f;
        if (roll > PlaceProb) return;

        // FBX 선택 (curved 70%, square 30%)
        var src = lightCurved != null && (Mathf.Abs(hash) % 10) < 7 ? lightCurved
                : lightSquare != null ? lightSquare : lightCurved;
        if (src == null) return;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        if (go == null) return;
        go.name = $"Light_{c}_{r}_{suffix}";
        go.transform.SetParent(parent, false);

        // 셀 모서리 위치 (도로 안쪽 살짝)
        float worldX = c * CellSize + dx * CellSize - Mathf.Sign(dx) * CornerInset;
        float worldZ = r * CellSize + dz * CellSize - Mathf.Sign(dz) * CornerInset;
        go.transform.localPosition = new Vector3(worldX, LightY, worldZ);

        // 회전: 모서리에서 도로 안쪽 향함 (yaw 방향)
        // dx = +0.5 / dz = +0.5 → 모서리 NE → 도로는 SW → 빛 SW 향함 = yaw 225°
        float yaw = (dx > 0 && dz > 0) ? 225f
                  : (dx < 0 && dz > 0) ? 135f
                  : (dx > 0 && dz < 0) ? 315f
                  :                       45f;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        go.transform.localScale = Vector3.one * 6f;     // 다른 도로 타일과 동일 스케일

        // collider 제거 (차량 통과 자유)
        foreach (var col in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col);

        placed++;
    }
}
