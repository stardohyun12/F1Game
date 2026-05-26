using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 지구별 ground 톤 — 공업/항구/다운타운이 부감서 다 녹색 마당이라 구분 안 되던 것(그래픽게이트
/// 6/10 지적)을 지구별 바닥색으로 해소. "은은하게 톤 차이만"(도현): 자연스러운 흙/모래/머튼그린.
///
/// 각 zone 띠에 단색 평면 쿼드 1개. y=0.004 (잔디 top 0 위 · 도로면 0.01 아래)라 도로는 아스팔트
/// 유지되고 빈 마당·건물 사이만 지구색으로 보임. 멱등(있으면 갱신). 리빌드 후 재실행.
/// 메뉴: PitCrew/Map/Place Zone Ground Tones
/// </summary>
public static class PlaceZoneGround
{
    const float Y = 0.004f;
    const float CELL = 6f;

    // (이름, row시작, row끝, 색) — zoneMask: I rows0-6 / D 7-14 / H 15-21
    static readonly (string name, int r0, int r1, Color col)[] Zones =
    {
        ("ZoneGround_Industrial", 0,  6,  new Color(0.33f, 0.29f, 0.23f)), // 갈색 흙/콘크리트
        ("ZoneGround_Downtown",   7,  14, new Color(0.26f, 0.31f, 0.23f)), // 머튼 그린(도시 잔디)
        ("ZoneGround_Harbor",     15, 19, new Color(0.42f, 0.39f, 0.31f)), // 모래/부두 탄 (rows20-21은 바다)
    };

    [MenuItem("PitCrew/Map/Place Zone Ground Tones")]
    public static void Execute()
    {
        var root = GameObject.Find("ZoneGrounds") ?? new GameObject("ZoneGrounds");
        foreach (var z in Zones)
        {
            var go = GameObject.Find(z.name);
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                go.name = z.name;
                Object.DestroyImmediate(go.GetComponent<Collider>()); // 주행 방해 X
            }
            go.transform.SetParent(root.transform, true);
            // 셀 중심 x=col*6, 폭 col 0~29 → x -3~177 (center 87, width 180). z=row*6 ± 3.
            float zMin = z.r0 * CELL - CELL / 2f;
            float zMax = z.r1 * CELL + CELL / 2f;
            float zc = (zMin + zMax) / 2f;
            float depth = zMax - zMin;
            go.transform.position = new Vector3(87f, Y, zc);
            go.transform.localScale = new Vector3(18f, 1f, depth / 10f); // Plane 10유닛
            go.transform.rotation = Quaternion.identity;

            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", z.col);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
            mr.sharedMaterial = mat;
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[ZoneGround] ✓ 지구별 ground 톤 3개 배치 (공업 갈색/다운타운 그린/항구 모래).");
    }
}
