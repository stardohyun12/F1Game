using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// 세션 18 (d) — Slope tile 시각 보강.
// road-slant.fbx는 좁은 wedge라 도로 surface 부족. 그 아래/위에 cube 받침을 추가.
// - Slant (램프): 6×1.62×6 cube, 정확히 wedge 각도로 기울임. 차량이 driveable.
// - SlantFlat: road-bridge로 변경됨 (TileMapAuto). 추가 보강 불요.
//
// 실행: TileMapAuto.BuildFromLayout 후 (또는 BuildOpenWorldV2 후).
public static class EnhanceSlopes
{
    [MenuItem("PitCrew/Layout/Enhance Slope Tiles")]
    public static void Execute()
    {
        var track = GameObject.Find("Track");
        if (track == null) { Debug.LogError("Track GameObject 없음 — TileMapAuto 먼저 실행"); return; }

        var concreteMat = MakeMat(new Color(0.55f, 0.55f, 0.58f), "SlopeSupportMat"); // 콘크리트 회색

        int count = 0;
        foreach (Transform t in track.transform)
        {
            if (t == null || !t.name.StartsWith("Tile_Slant_") || t.name.StartsWith("Tile_SlantFlat")) continue;

            // 기존 보강 cube 제거
            var existing = t.Find("SlopeSupport");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // wedge 각도로 cube 추가. road-slant는 native +Z로 상승. rot에 따라 회전.
            float rotY = t.localRotation.eulerAngles.y; // 0/90/180/270
            bool isHigh = t.name.Contains("SlantHigh");
            float riseM = isHigh ? 3.12f : 1.62f;       // 3m 또는 1.6m
            float riseAngle = Mathf.Atan2(riseM, 6f) * Mathf.Rad2Deg; // 보통 ~15° or ~27°

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "SlopeSupport";
            cube.transform.SetParent(t, false);
            cube.transform.localScale = new Vector3(6f, 0.5f, 6f);
            // 박스 중심을 셀 중앙, 살짝 아래 (도로 surface 바로 아래)
            cube.transform.localPosition = new Vector3(0f, riseM * 0.5f - 0.25f, 0f);
            // X축 회전으로 기울임. native +Z 상승이므로 -X 축 기준 (양수 각).
            cube.transform.localRotation = Quaternion.Euler(-riseAngle, 0f, 0f);
            // 머티리얼
            var mr = cube.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = concreteMat;
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[EnhanceSlopes] {count} 슬로프 cube 받침 추가.");
    }

    static Material MakeMat(Color color, string name)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        return mat;
    }
}
