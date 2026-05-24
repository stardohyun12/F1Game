using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 거리 주차 차량 자동 배치 (세션 30, 리뷰어 "차·사람 전무" 4/10 피드백).
///
/// 설계
/// - layout 변경 X. "StreetVehicles" GameObject 자식으로 배치.
/// - 도로 인접 빈 셀(buildingMask=='.', 건물 미배치, 비도로)에 낮은 확률로.
/// - 인접 도로 방향에 **평행**하게(주차된 느낌) + 도로 쪽 가장자리로 offset.
/// - collider 없음 → 주행 안 막음 (시각 생활감만).
/// - 결정론적 hash → 재실행 동일 결과.
///
/// 주의: EditorUtility.DisplayDialog 금지 (unity-cli menu 모달 block 함정, 2026-05-24).
/// 메뉴: PitCrew/Track/Auto-Place Street Vehicles
/// </summary>
public static class PlaceStreetVehicles
{
    const float CellSize = 6f;
    // Kenney car-kit 모델 원점 = 바퀴 바닥(boundsMinY==posY). Y=0이어야 접지.
    // 0.05면 5cm 떠서 그림자 분리 버그 (리뷰어 2026-05-24 발견).
    const float VehicleY = 0f;
    const string CarFbxRoot = "Assets/Model/kenney_car-kit/Models/FBX format";

    // 도로 인접 빈 셀 중 차를 둘 확률. 낮게 — 띄엄띄엄 주차.
    const float PlaceProbability = 0.28f;

    // 일반 차량 풀 (응급차는 가끔만 — race/kart는 제외, 플레이어 차와 겹침).
    static readonly string[] CarPool =
    {
        "sedan.fbx", "sedan-sports.fbx", "suv.fbx", "suv-luxury.fbx",
        "taxi.fbx", "hatchback-sports.fbx", "delivery.fbx", "garbage-truck.fbx",
    };

    [MenuItem("PitCrew/Track/Auto-Place Street Vehicles")]
    public static void Execute()
    {
        var layoutGuids = AssetDatabase.FindAssets("t:TrackLayout");
        if (layoutGuids == null || layoutGuids.Length == 0)
        {
            Debug.LogError("[Vehicles] TrackLayout 자산 없음.");
            return;
        }
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(layoutGuids[0]));
        if (layout == null) { Debug.LogError("[Vehicles] TrackLayout 로드 실패."); return; }

        Debug.Log($"[Vehicles] === 거리 차량 배치 시작 (layout={layout.name}) ===");

        // 1. 차량 FBX 로드
        var pool = new List<GameObject>();
        foreach (var fn in CarPool)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{CarFbxRoot}/{fn}");
            if (go != null) pool.Add(go);
            else Debug.LogWarning($"[Vehicles] {fn} 없음 — 풀 제외.");
        }
        if (pool.Count == 0) { Debug.LogError("[Vehicles] 차량 FBX 0개 — car-kit 임포트 확인."); return; }
        Debug.Log($"[Vehicles] (1/3) {pool.Count}개 차량 FBX 로드.");

        // 2. 기존 루트 제거 (재실행 안전)
        var existing = GameObject.Find("StreetVehicles");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("StreetVehicles");
        Undo.RegisterCreatedObjectUndo(root, "Place Street Vehicles");

        var buildingsOccupied = ScanBuildingsOccupancy(layout.Width, layout.Height);

        int placed = 0;
        for (int r = 0; r < layout.Height; r++)
        {
            for (int c = 0; c < layout.Width; c++)
            {
                if (layout.IsRoad(c, r)) continue;
                if (layout.BuildingChar(c, r) != '.') continue;
                if (buildingsOccupied[c, r]) continue;

                // 인접 도로 방향 찾기 (가장 가까운 도로 변)
                int rdx = 0, rdy = 0;
                if (layout.IsRoad(c + 1, r)) { rdx = 1; rdy = 0; }
                else if (layout.IsRoad(c - 1, r)) { rdx = -1; rdy = 0; }
                else if (layout.IsRoad(c, r + 1)) { rdx = 0; rdy = 1; }
                else if (layout.IsRoad(c, r - 1)) { rdx = 0; rdy = -1; }
                else continue; // 도로 인접 아님

                // 차량 전용 hash (props와 다른 시드 — 같은 셀 겹침 회피)
                int hash = (c * 92837111) ^ (r * 689287499) ^ 0x5bd1e995;
                float roll = Mathf.Abs(hash % 1000) / 1000f;
                if (roll > PlaceProbability) continue;
                if (layout.BuildingChar(c, r) != '.') continue;

                var car = pool[Mathf.Abs(hash) % pool.Count];

                // 도로 쪽 가장자리로 밀기 (curb 주차 느낌) — cell 중앙에서 도로 방향 +1.7m
                float posX = c * CellSize + rdx * 1.7f;
                float posZ = r * CellSize + rdy * 1.7f;

                // 도로와 평행: 도로가 동/서(rdx!=0)면 도로는 남북으로 흐름 → 차 yaw 0/180.
                //            도로가 남/북(rdy!=0)면 도로는 동서로 흐름 → 차 yaw 90/270.
                float baseYaw = (rdx != 0) ? 0f : 90f;
                if (((hash >> 5) & 1) == 1) baseYaw += 180f; // 앞뒤 랜덤
                // 차 길이 따라 살짝 살짝 흩기 (완벽 정렬 회피)
                float jitterYaw = (((hash >> 7) % 7) - 3) * 1.5f;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(car);
                if (inst == null) continue;
                inst.name = $"V_{car.name}_{c}_{r}";
                inst.transform.SetParent(root.transform, false);
                inst.transform.localPosition = new Vector3(posX, VehicleY, posZ);
                inst.transform.localRotation = Quaternion.Euler(0f, baseYaw + jitterYaw, 0f);
                inst.transform.localScale = Vector3.one; // Kenney car ~native

                foreach (var col in inst.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);

                placed++;
            }
        }
        Debug.Log($"[Vehicles] (2/3) {placed}대 배치 — 도로 인접 빈 셀 {PlaceProbability * 100:F0}%, 도로 평행 주차, collider 없음.");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[Vehicles] (3/3) 씬 저장 완료. ✓ {placed}대.");
    }

    /// <summary>씬의 "Buildings" 자식 이름(B_X_c_r[_wxd])에서 점유 셀 추출.</summary>
    static bool[,] ScanBuildingsOccupancy(int w, int h)
    {
        var occ = new bool[w, h];
        var buildings = GameObject.Find("Buildings");
        if (buildings == null) return occ;
        foreach (Transform t in buildings.transform)
        {
            var parts = t.name.Split('_');
            if (parts.Length < 4) continue;
            if (!int.TryParse(parts[2], out int c)) continue;
            if (!int.TryParse(parts[3], out int r)) continue;
            int ww = 1, dd = 1;
            if (parts.Length >= 5)
            {
                var sz = parts[4].Split('x');
                if (sz.Length == 2) { int.TryParse(sz[0], out ww); int.TryParse(sz[1], out dd); }
            }
            for (int dr = 0; dr < dd; dr++)
                for (int dc = 0; dc < ww; dc++)
                {
                    int cc = c + dc, rr = r + dr;
                    if (cc >= 0 && cc < w && rr >= 0 && rr < h) occ[cc, rr] = true;
                }
        }
        return occ;
    }
}
