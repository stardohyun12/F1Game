using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Suburban props 자동 배치 (세션 16, 도현 "맵 단조" 피드백 #3).
///
/// 설계
/// - layout (TrackLayout) 변경 X. 별도 "Props" GameObject 아래 자식으로 배치.
/// - 도로 인접 빈 셀(buildingMask=='.', 건물 미배치, 비도로) 후보화.
/// - 각 후보에 결정론적 hash로 prop 종류·회전·확률 결정 (재실행 시 동일 결과).
/// - Props는 collider 없음 → 차량 통과 자유 (시각적 첨가만).
///
/// 메뉴: PitCrew/Track/Auto-Place Suburban Props
/// </summary>
public static class PlaceSuburbanProps
{
    const float CellSize = 6f;
    const float PropsY = 0f;
    const string SuburbanFbxRoot = "Assets/Model/kenney_city-kit-suburban_20/Models/FBX format";

    // 배치 확률. 도로변을 가로수로 줄지어 채워야(리뷰어 1순위) 도로 끝단의
    // "맨 잔디에 뚝 끊김" 모형 티가 가려진다 → 거의 다 채움.
    const float PlaceProbability = 0.97f;

    [MenuItem("PitCrew/Track/Auto-Place Suburban Props")]
    public static void Execute()
    {
        var layoutGuids = AssetDatabase.FindAssets("t:TrackLayout");
        if (layoutGuids == null || layoutGuids.Length == 0)
        {
            Debug.LogError("[Props] TrackLayout 자산 없음.");
            return;
        }
        var layoutPath = AssetDatabase.GUIDToAssetPath(layoutGuids[0]);
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(layoutPath);
        if (layout == null) { Debug.LogError($"[Props] {layoutPath} 로드 실패."); return; }

        Debug.Log($"[Props] === Suburban Props 배치 시작 (layout={layout.name}) ===");

        // 1. Prop FBX 로드
        // 나무 비중을 높임 (녹지가 거리서 보이게). 중복 추가 = 가중치.
        var propPool = new List<GameObject>();
        AddIfExists(propPool, "tree-large.fbx");
        AddIfExists(propPool, "tree-large.fbx");
        AddIfExists(propPool, "tree-large.fbx");
        AddIfExists(propPool, "tree-small.fbx");
        AddIfExists(propPool, "tree-small.fbx");
        AddIfExists(propPool, "planter.fbx");
        AddIfExists(propPool, "fence.fbx");
        AddIfExists(propPool, "fence-low.fbx");
        AddIfExists(propPool, "path-stones-long.fbx");
        if (propPool.Count == 0)
        {
            Debug.LogError("[Props] FBX prop 없음 — Suburban kit 임포트 확인.");
            return;
        }
        Debug.Log($"[Props] (1/3) {propPool.Count}개 prop FBX 로드.");

        // 2. 기존 Props 루트 제거 (재실행 안전)
        var existing = GameObject.Find("Props");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("Props");
        Undo.RegisterCreatedObjectUndo(root, "Place Props");

        // 기존 Buildings 점유 셀도 회피 (이름 패턴 B_{ch}_{c}_{r}로 추출)
        var buildingsOccupied = ScanBuildingsOccupancy(layout.Width, layout.Height);

        // 3. 후보 셀 순회
        int placed = 0;
        for (int r = 0; r < layout.Height; r++)
        {
            for (int c = 0; c < layout.Width; c++)
            {
                if (layout.IsRoad(c, r)) continue;
                if (layout.BuildingChar(c, r) != '.') continue;       // 건물 마스크 셀 회피
                if (buildingsOccupied[c, r]) continue;                 // 실제 배치된 건물 회피
                if (!IsAdjacentToRoad(layout, c, r)) continue;

                // 결정론적 hash
                int hash = c * 73856093 ^ r * 19349663;
                float roll = Mathf.Abs((hash % 1000)) / 1000f;
                if (roll > PlaceProbability) continue;

                var prop = propPool[Mathf.Abs(hash) % propPool.Count];

                // 인접 도로 쪽으로 밀어 연석에 줄세움(가로수). 도로 평행 방향으로만 약간 흩음.
                int rdx = layout.IsRoad(c + 1, r) ? 1 : layout.IsRoad(c - 1, r) ? -1 : 0;
                int rdy = (rdx == 0 && layout.IsRoad(c, r + 1)) ? 1 : (rdx == 0 && layout.IsRoad(c, r - 1)) ? -1 : 0;
                int subHash = (c * 31 + r) * 17;
                float along = ((subHash % 71) / 71f - 0.5f) * (CellSize * 0.5f); // 도로 평행 흩기
                float curb = 2.1f; // 연석까지 밀기
                float posX = c * CellSize + (rdx != 0 ? rdx * curb : along);
                float posZ = r * CellSize + (rdy != 0 ? rdy * curb : along);

                // 회전: 도로 방향에 등 돌리기 (정원 fence/tree 자연스러움)
                float yaw = ((Mathf.Abs(hash) / 1000) % 4) * 90f;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prop);
                if (inst == null) continue;
                inst.name = $"P_{prop.name}_{c}_{r}";
                inst.transform.SetParent(root.transform, false);
                inst.transform.localPosition = new Vector3(posX, PropsY, posZ);
                inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

                // Kenney prop들은 native ~1m. cell 6m 안에서 자연 크기 유지 (1.0 그대로).
                // tree-large는 살짝 키워줌.
                float scale = prop.name.Contains("tree-large") ? 1.9f
                            : prop.name.Contains("tree-small") ? 1.3f
                            : prop.name.Contains("path-stones-long") ? 0.8f
                            : 1.0f;
                inst.transform.localScale = Vector3.one * scale;

                // collider 제거 (prop은 통과 자유)
                foreach (var col in inst.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);

                placed++;
            }
        }
        Debug.Log($"[Props] (2/3) {placed}개 prop 배치 — 도로 인접 빈 셀 {PlaceProbability * 100:F0}% 확률.");

        // 4. 씬 저장
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[Props] (3/3) 씬 저장 완료. 메뉴 재실행 = 동일 결과 (deterministic hash).");
        // 주의: EditorUtility.DisplayDialog 금지 — unity-cli menu 자동 호출 시 모달이
        // 메인 스레드를 막아 모든 후속 명령이 timeout 됨 (2026-05-24 실제로 막힘).
        Debug.Log($"[Props] ✓ {placed}개 배치 (도로 인접 빈 셀 {PlaceProbability * 100:F0}%, 나무 위주, collider 없음).");
    }

    static void AddIfExists(List<GameObject> pool, string fbxName)
    {
        var path = $"{SuburbanFbxRoot}/{fbxName}";
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go != null) pool.Add(go);
        else Debug.LogWarning($"[Props] {path} 없음 — 풀 제외.");
    }

    /// <summary>4-방위 인접 도로 검사.</summary>
    static bool IsAdjacentToRoad(TrackLayout layout, int c, int r)
    {
        return layout.IsRoad(c + 1, r) || layout.IsRoad(c - 1, r)
            || layout.IsRoad(c, r + 1) || layout.IsRoad(c, r - 1);
    }

    /// <summary>씬의 "Buildings" 자식 이름(B_X_c_r)에서 점유 셀 추출.</summary>
    static bool[,] ScanBuildingsOccupancy(int w, int h)
    {
        var occ = new bool[w, h];
        var buildings = GameObject.Find("Buildings");
        if (buildings == null) return occ;
        foreach (Transform t in buildings.transform)
        {
            // 이름 패턴: "B_{ch}_{c}_{r}" or "B_{ch}_{c}_{r}_{w}x{d}"
            var parts = t.name.Split('_');
            if (parts.Length < 4) continue;
            if (!int.TryParse(parts[2], out int c)) continue;
            if (!int.TryParse(parts[3], out int r)) continue;
            int ww = 1, dd = 1;
            if (parts.Length >= 5)
            {
                var sz = parts[4].Split('x');
                if (sz.Length == 2)
                {
                    int.TryParse(sz[0], out ww);
                    int.TryParse(sz[1], out dd);
                }
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
