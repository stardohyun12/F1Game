using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 절차적 보도/연석 띠 — 도로가 잔디에 연석 없이 뚝 끊기는 "모형 티"를 제거(세션 30, 리뷰어 7회 1순위).
/// 모든 도로 셀의 비도로(잔디·건물) 쪽 가장자리에 밝은 콘크리트 띠를 깐다.
/// collider 없음, 결정론적, dialog 없음.
/// 메뉴: PitCrew/Track/Auto-Place Sidewalks
/// </summary>
public static class PlaceSidewalks
{
    const float CellSize = 6f;
    const float StripWidth = 1.3f;   // 띠 폭
    const float StripHeight = 0.12f; // 연석 높이
    const string MatPath = "Assets/Materials/Sidewalk.mat";

    [MenuItem("PitCrew/Track/Auto-Place Sidewalks")]
    public static void Execute()
    {
        var guids = AssetDatabase.FindAssets("t:TrackLayout");
        if (guids == null || guids.Length == 0) { Debug.LogError("[Sidewalk] TrackLayout 없음."); return; }
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (layout == null) { Debug.LogError("[Sidewalk] 로드 실패."); return; }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.72f, 0.71f, 0.67f); // 밝은 콘크리트
            System.IO.Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(mat, MatPath);
        }

        var existing = GameObject.Find("Sidewalks");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("Sidewalks");
        Undo.RegisterCreatedObjectUndo(root, "Place Sidewalks");

        var dirs = new (int dx, int dy)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        int n = 0;
        for (int r = 0; r < layout.Height; r++)
        for (int c = 0; c < layout.Width; c++)
        {
            if (!layout.IsRoad(c, r)) continue;
            foreach (var (dx, dy) in dirs)
            {
                int nc = c + dx, nr = r + dy;
                // 경계 밖 또는 비도로 쪽에만 (도로-도로 경계는 X)
                bool nbrRoad = nc >= 0 && nc < layout.Width && nr >= 0 && nr < layout.Height && layout.IsRoad(nc, nr);
                if (nbrRoad) continue;

                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"SW_{c}_{r}_{dx}_{dy}";
                strip.transform.SetParent(root.transform, false);
                // 가장자리 위치: 셀 중심에서 해당 방향으로 (CellSize/2 - StripWidth/2)
                float edge = CellSize * 0.5f - StripWidth * 0.5f;
                float px = c * CellSize + dx * edge;
                float pz = r * CellSize + dy * edge;
                strip.transform.localPosition = new Vector3(px, StripHeight * 0.5f, pz);
                // 가장자리 따라 길게: 가로 방향이면 Z로 길게, 세로면 X로 길게
                bool horiz = dx != 0;
                strip.transform.localScale = horiz
                    ? new Vector3(StripWidth, StripHeight, CellSize)
                    : new Vector3(CellSize, StripHeight, StripWidth);
                var mr = strip.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                Object.DestroyImmediate(strip.GetComponent<Collider>());
                n++;
            }
        }
        Debug.Log($"[Sidewalk] ✓ {n}개 보도 띠 배치 (collider 없음, mat={MatPath}).");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
}
