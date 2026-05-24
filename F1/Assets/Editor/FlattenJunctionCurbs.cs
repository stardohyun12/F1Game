using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 교차로 "네모 블럭" 제거 — Kenney 도로 junction 타일(crossroad/intersection)은
/// 4 코너에 6cm 솟은 연석(curb)이 baked 되어 있다. 단일 교차로면 인도 코너로 의도된 것이지만,
/// 2칸폭 간선이 교차해 2x2 클러스터를 이루면 안쪽 코너 4개가 도로 한복판에서 만나
/// 솟은 사각 패드가 된다. 민무늬 아스팔트(PlainAsphaltRoads) 적용 후 텍스처 맥락이 사라져
/// 이 솟은 코너가 어두운 네모로 드러남 (세션 31 도현 발견, 세션 32 진단).
///
/// fix: junction 타일 메시의 솟은 verts(y > 0.010)를 도로면(0.010)으로 클램프한 평탄 메시를
/// 생성·교체. 메시 외곽·UV 불변 → 새 이음매 없음 (이전 road-square swap 실패 원인 회피).
/// 직선/곡선 도로의 측면 연석은 의도된 인도라 건드리지 않음.
///
/// 재빌드 후 재실행 필요 (룩 재적용 메뉴 체인의 일부).
/// 메뉴: PitCrew/Map/Flatten Junction Curbs
/// </summary>
public static class FlattenJunctionCurbs
{
    const float RoadSurfaceY = 0.010f; // 도로면 로컬 높이 (연석은 0.020)
    const string FlatDir = "Assets/Meshes/JunctionFlat";

    [MenuItem("PitCrew/Map/Flatten Junction Curbs")]
    public static void Execute()
    {
        var track = GameObject.Find("Track");
        if (track == null) { Debug.LogError("[FlattenCurbs] Track 없음."); return; }

        System.IO.Directory.CreateDirectory(FlatDir);
        var flatCache = new Dictionary<string, Mesh>();
        int tiles = 0;

        foreach (var mf in track.GetComponentsInChildren<MeshFilter>())
        {
            var name = mf.gameObject.name;
            if (!name.StartsWith("Tile_Crossroad") && !name.StartsWith("Tile_Intersection")) continue;
            var src = mf.sharedMesh;
            if (src == null || src.name.EndsWith("-flat")) continue;

            if (!flatCache.TryGetValue(src.name, out var flat))
            {
                flat = BuildFlatMesh(src);
                flatCache[src.name] = flat;
            }
            mf.sharedMesh = flat;
            tiles++;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[FlattenCurbs] ✓ junction 타일 {tiles}개 평탄화 (메시 변형 {flatCache.Count}종). 네모 블럭 제거.");
    }

    static Mesh BuildFlatMesh(Mesh src)
    {
        var flat = Object.Instantiate(src);
        flat.name = src.name + "-flat";
        var v = flat.vertices;
        for (int i = 0; i < v.Length; i++)
            if (v[i].y > RoadSurfaceY + 0.0001f) v[i].y = RoadSurfaceY;
        flat.vertices = v;
        flat.RecalculateNormals();
        flat.RecalculateBounds();

        string path = $"{FlatDir}/{flat.name}.asset";
        AssetDatabase.DeleteAsset(path); // 멱등: 재실행 시 갱신
        AssetDatabase.CreateAsset(flat, path);
        return flat;
    }
}
