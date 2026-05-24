using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 도로 중앙 차선 — 민무늬 아스팔트가 "일반 도로 같지 않다"(도현)는 문제 해결.
/// 마킹을 전부 지웠더니 차선까지 사라져 회색 띠가 됨 → 직선 도로마다 중앙 점선을
/// 다시 그림(횡단보도 패치는 없이). collider 없음, 결정론적, dialog 없음.
/// 메뉴: PitCrew/Map/Place Road Center Lines
/// </summary>
public static class PlaceRoadLines
{
    const float CellSize = 6f;
    const float LineY = 0.02f;       // 도로 표면(땅=0 정렬 후 ~0.01) 위 1cm. AlignMapToGround와 일치.
                                     // (옛 0.19는 연석 꼭대기 0.17을 잘못 기준 삼아 도로면 위로 떠 있었음)
    const float DashLen = 3.2f;      // 셀당 점선 길이 (나머지는 간격 → 점선)
    const float LineWidth = 0.18f;
    const string MatPath = "Assets/Materials/RoadLine.mat";

    [MenuItem("PitCrew/Map/Place Road Center Lines")]
    public static void Execute()
    {
        var guids = AssetDatabase.FindAssets("t:TrackLayout");
        if (guids == null || guids.Length == 0) { Debug.LogError("[RoadLine] TrackLayout 없음."); return; }
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (layout == null) { Debug.LogError("[RoadLine] 로드 실패."); return; }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            System.IO.Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        var c = new Color(0.88f, 0.83f, 0.55f); // 바랜 노란 차선
        mat.color = c; if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        EditorUtility.SetDirty(mat);

        var existing = GameObject.Find("RoadLines");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("RoadLines");
        Undo.RegisterCreatedObjectUndo(root, "Place Road Lines");

        int n = 0;
        for (int r = 0; r < layout.Height; r++)
        for (int cc = 0; cc < layout.Width; cc++)
        {
            if (!layout.IsRoad(cc, r)) continue;
            bool ew = layout.IsRoad(cc + 1, r) && layout.IsRoad(cc - 1, r);
            bool ns = layout.IsRoad(cc, r + 1) && layout.IsRoad(cc, r - 1);
            // 직선 통과 셀만 (양쪽이 도로). 교차/T/끝은 중앙선 생략(현실 도로처럼).
            if (ew == ns) continue; // 둘 다거나(교차) 둘 다 아님(끝) → skip

            var dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.name = $"Line_{cc}_{r}";
            dash.transform.SetParent(root.transform, false);
            dash.transform.localPosition = new Vector3(cc * CellSize, LineY, r * CellSize);
            dash.transform.localScale = ew
                ? new Vector3(DashLen, 0.02f, LineWidth)
                : new Vector3(LineWidth, 0.02f, DashLen);
            dash.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(dash.GetComponent<Collider>());
            n++;
        }
        Debug.Log($"[RoadLine] ✓ 중앙 점선 {n}개 (직선 통과 셀, 교차/끝 제외).");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
}
