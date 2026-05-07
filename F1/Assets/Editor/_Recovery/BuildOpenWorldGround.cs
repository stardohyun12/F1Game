using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ground + Plateau 시각화. heightMask 기반 cube를 비도로 셀에 배치.
// - 도로 cell은 비움 (도로 타일 자체가 시각화)
// - 비도로 cell: 6×(0.2 + h)×6 cube, y center = (0.2 + h)/2 - 0.2
//   → tier 0 (평지) cell도 얇은 (0.2m) 잔디층으로 채움 → "길 외 바닥 없음" 해소
//   → tier 1 (1.5m) → 1.7m cube → plateau visual
//   → tier 2 (3.0m) → 3.2m cube → 더 높은 plateau
// 하나의 'GroundField' GameObject 아래에 자식으로.
public static class BuildOpenWorldGround
{
    const string LayoutPath = "Assets/Data/OpenWorldLayout.asset";
    const float CellSize = 6f;
    const float BaseFloor = 0.2f;   // 평지 cell 잔디 두께

    [MenuItem("PitCrew/Layout/Build Ground + Plateau Cubes")]
    public static void Execute()
    {
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(LayoutPath);
        if (layout == null) { Debug.LogError("Layout missing"); return; }

        var existing = GameObject.Find("GroundField");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("GroundField");
        Undo.RegisterCreatedObjectUndo(root, "Build Ground");

        // 잔디(평지) / 흙(plateau) 머티리얼 — URP Lit 기반 색 분리
        var grass = MakeMat(new Color(0.42f, 0.56f, 0.32f), "GrassMat"); // 잔디 녹색
        var dirt1 = MakeMat(new Color(0.58f, 0.48f, 0.36f), "Plateau1Mat"); // 흙
        var dirt2 = MakeMat(new Color(0.50f, 0.40f, 0.30f), "Plateau2Mat"); // 더 짙은 흙

        int placed = 0;
        for (int r = 0; r < layout.Height; r++)
        {
            for (int c = 0; c < layout.Width; c++)
            {
                if (layout.IsRoad(c, r)) continue;
                float h = layout.GetHeight(c, r);
                float total = BaseFloor + h;
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(cube.GetComponent<BoxCollider>()); // 빌딩 collider만 사용
                cube.name = h > 0 ? $"GP_{c}_{r}_h{h:0.0}" : $"GG_{c}_{r}";
                cube.transform.SetParent(root.transform, false);
                cube.transform.localPosition = new Vector3(c * CellSize, total * 0.5f - BaseFloor, r * CellSize);
                cube.transform.localScale = new Vector3(CellSize, total, CellSize);
                var mr = cube.GetComponent<MeshRenderer>();
                Material chosen = h > 1.5f ? dirt2 : (h > 0f ? dirt1 : grass);
                mr.sharedMaterial = chosen;
                placed++;
            }
        }

        // 외곽 큰 base plane (지평선 채움)
        var basePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        basePlane.name = "BaseHorizon";
        basePlane.transform.SetParent(root.transform, false);
        basePlane.transform.localPosition = new Vector3(layout.Width * CellSize * 0.5f, -0.21f, layout.Height * CellSize * 0.5f);
        basePlane.transform.localScale = new Vector3(40f, 1f, 40f);  // Plane 기본 10m → 400m 확장
        basePlane.GetComponent<MeshRenderer>().sharedMaterial = grass;
        Object.DestroyImmediate(basePlane.GetComponent<MeshCollider>());

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"BuildOpenWorldGround: {placed} ground cells + horizon plane.");
    }

    static Material MakeMat(Color color, string name)
    {
        // URP Lit
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        return mat;
    }
}
