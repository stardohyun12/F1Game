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
        // 맵 월드: X=열(~0~177), Z=행(~0~126), 항구=높은 Z 가장자리(rows15~21).
        // rows20~21(z~120+)은 도로·건물 없는 빈 행(바다 의도) → 거기서부터 물.
        // 잔디 평면(z~173까지)이 덮지 않도록 바다를 잔디 위(y=0.03)로 올림. 넓게 깔아 수변 강조.
        go.transform.position = new Vector3(88f, 0.03f, 215f);
        go.transform.localScale = new Vector3(30f, 1f, 20f); // Plane 10유닛 → X300,Z200 → 근접 z115, x -62~238
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
