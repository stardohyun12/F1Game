using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 맵 환경: 큰 땅(도시가 허공에 안 뜨게) + 큰 바다(도시보다 낮아 해안선처럼).
/// 멱등. RunFullRebuild 후 재실행(Ground는 매 빌드 재생성되므로 크기/머티리얼 재적용).
/// 색은 무난한 무채+톤(도현이 추후 조정 가능).
/// </summary>
public static class MapEnvironment
{
    [MenuItem("PitCrew/Map/Apply Environment")]
    public static void Apply()
    {
        // 땅: 도시(Z ~0~130) + 주변을 덮되 바다쪽(높은 Z)으로 안 넘침. y -0.05.
        var ground = GameObject.Find("Ground") ?? GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(90f, -0.05f, 12f);
        ground.transform.localScale = new Vector3(48f, 1f, 22f); // X ~-150~330, Z ~-98~122
        SetMat(ground, new Color(0.40f, 0.44f, 0.34f), 0.05f); // 무딘 세이지

        // 바다: 도시 너머(높은 Z)부터 크게, 땅보다 낮게(y -0.6) → 해안선 단차. 가장자리 화면 밖.
        var sea = GameObject.Find("HarborSea") ?? GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "HarborSea";
        var col = sea.GetComponent<Collider>(); if (col != null) Object.DestroyImmediate(col);
        sea.transform.position = new Vector3(90f, -0.6f, 270f);
        sea.transform.localScale = new Vector3(48f, 1f, 30f); // Z ~120~420 (도시 가장자리부터 멀리)
        SetMat(sea, new Color(0.05f, 0.28f, 0.34f), 0.7f); // 짙은 청록, 약간 반짝

        EditorSceneManager.MarkSceneDirty(ground.scene);
        EditorSceneManager.SaveScene(ground.scene);
        Debug.Log("[MapEnvironment] 땅+바다 적용 완료.");
    }

    static void SetMat(GameObject go, Color c, float smooth)
    {
        var mr = go.GetComponent<MeshRenderer>();
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        mr.sharedMaterial = m;
    }
}
