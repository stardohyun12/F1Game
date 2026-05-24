using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 따뜻한 컬러 그레이딩 프로파일을 글로벌 볼륨에 적용 — "톤 칙칙"(도현 헤드라인) 대응.
/// PrototypeVolume에 프로파일이 없어 색보정 0이던 상태를 해결. 저리스크·되돌리기 쉬움.
/// 메뉴: PitCrew/Map/Apply Warm Grade
/// </summary>
public static class ApplyWarmGrade
{
    const string ProfilePath = "Assets/Rendering/WarmGrade.asset";

    [MenuItem("PitCrew/Map/Apply Warm Grade")]
    public static void Execute()
    {
        var go = GameObject.Find("PrototypeVolume");
        if (go == null) { go = new GameObject("PrototypeVolume"); }
        var vol = go.GetComponent<Volume>();
        if (vol == null) vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            System.IO.Directory.CreateDirectory("Assets/Rendering");
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        // 기존 컴포넌트 비우고 재구성 (멱등)
        for (int i = profile.components.Count - 1; i >= 0; i--)
            Object.DestroyImmediate(profile.components[i], true);
        profile.components.Clear();

        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.12f);
        ca.contrast.Override(14f);
        ca.colorFilter.Override(new Color(1.0f, 0.96f, 0.88f)); // 살짝 따뜻
        ca.saturation.Override(14f);

        var wb = profile.Add<WhiteBalance>(true);
        wb.temperature.Override(14f); // 따뜻한 쪽
        wb.tint.Override(2f);

        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.22f);
        vig.smoothness.Override(0.5f);

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.4f);
        bloom.threshold.Override(1.1f);

        EditorUtility.SetDirty(profile);
        vol.sharedProfile = profile;
        AssetDatabase.SaveAssets();

        // 땅도 칙칙한 올리브 → 좀 더 생기있는 잔디색 (전용 머티리얼, 공유 default 안 건드림)
        var ground = GameObject.Find("Ground");
        if (ground != null)
        {
            var gr = ground.GetComponent<Renderer>();
            if (gr != null)
            {
                const string gmPath = "Assets/Materials/GroundGrass.mat";
                var gm = AssetDatabase.LoadAssetAtPath<Material>(gmPath);
                if (gm == null)
                {
                    gm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    System.IO.Directory.CreateDirectory("Assets/Materials");
                    AssetDatabase.CreateAsset(gm, gmPath);
                }
                gm.color = new Color(0.34f, 0.47f, 0.28f); // 생기있는 잔디
                EditorUtility.SetDirty(gm);
                gr.sharedMaterial = gm;
            }
        }
        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[WarmGrade] ✓ 따뜻한 그레이딩 + 잔디색 적용.");
    }
}
