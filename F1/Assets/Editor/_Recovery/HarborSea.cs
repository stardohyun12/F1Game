using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 항구 가장자리(높은 Z쪽, 항구 zone)에 단색 바다 평면 1개 배치. 멱등(있으면 갱신).
/// 진짜 물 시스템(셰이더·waterMask) 회피 — 데모용 단색 평면. RunFullRebuild 후 재실행.
/// </summary>
public static class HarborSea
{
    const string NAME = "HarborSea";

    [MenuItem("PitCrew/Map/Add Harbor Sea")]
    public static void Add()
    {
        var go = GameObject.Find(NAME);
        if (go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = NAME;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // 주행 방해 X
        }
        // 맵 월드: X=열(~0~177), Z=행(~0~130), 항구=높은 Z 가장자리.
        // 바다는 도시 밖(Z 130 너머)에만 — 건물 위 안 덮이게. 근접 가장자리 ≈ Z130.
        go.transform.position = new Vector3(90f, -0.15f, 205f);
        go.transform.localScale = new Vector3(24f, 1f, 15f); // Plane 10유닛 → X240, Z150 → Z 130~280
        go.transform.rotation = Quaternion.identity;

        var mr = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.07f, 0.32f, 0.42f)); // 짙은 청록
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.65f);
        mr.sharedMaterial = mat;

        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorSceneManager.SaveScene(go.scene);
        Debug.Log($"[HarborSea] 배치 완료 pos={go.transform.position} scale={go.transform.localScale}");
    }
}
