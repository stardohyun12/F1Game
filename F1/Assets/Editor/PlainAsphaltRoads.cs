using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 도로 타일을 민무늬 아스팔트 머티리얼로 — colormap 텍스처에 그려진 횡단보도/마킹 패치 제거.
/// 마킹은 메시가 아니라 텍스처라, 도로 렌더러에만 단색 회색 머티리얼을 입히면 전부 사라짐.
/// 건물(colormap)은 안 건드림. UV 수술 없이 동일 결과. 재빌드 후 재실행 필요.
/// 메뉴: PitCrew/Map/Plain Asphalt Roads
/// </summary>
public static class PlainAsphaltRoads
{
    const string MatPath = "Assets/Materials/Asphalt.mat";

    [MenuItem("PitCrew/Map/Plain Asphalt Roads")]
    public static void Execute()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            System.IO.Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        mat.color = new Color(0.27f, 0.27f, 0.29f); // 아스팔트 회색
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.27f, 0.27f, 0.29f));
        EditorUtility.SetDirty(mat);

        var track = GameObject.Find("Track");
        if (track == null) { Debug.LogError("[Asphalt] Track 없음."); return; }
        int n = 0;
        foreach (var mr in track.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = mr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            mr.sharedMaterials = mats;
            n++;
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[Asphalt] ✓ 도로 타일 {n}개 민무늬 아스팔트 적용 (마킹 제거).");
    }
}
