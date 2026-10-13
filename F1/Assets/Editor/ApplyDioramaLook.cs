using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 아트 스타일 실험 (2026-10-12, 도현 "A+C 로 Unity 실제 화면 한번 찍어봐").
/// A(플랫 로우폴리, 지금 Kenney) 위에 C(미니어처 디오라마) 한 겹을 글로벌 볼륨으로 얹는다:
///   따뜻한 그레이딩 + 가장자리/원경만 흐리는 틸트시프트 근사(DoF Gaussian) + 비네트 + ACES.
/// 메시·머티리얼은 손대지 않는다 — 되돌리기 = Revert 메뉴 한 번.
/// 메뉴: PitCrew/Art/Apply Diorama Look  ·  PitCrew/Art/Revert Diorama Look
/// </summary>
public static class ApplyDioramaLook
{
    const string ProfilePath = "Assets/Rendering/DioramaLook.asset";
    const string VolumeName = "DioramaVolume";

    const string MoodPath = "Assets/Settings/Mood_Daytime.asset";

    /// <summary>세션 28 낮 무드(그림자·따뜻한 태양·Flat 앰비언트·fog·그라데이션 하늘)를 이 씬에 보장.</summary>
    static void EnsureMood()
    {
        var mood = AssetDatabase.LoadAssetAtPath<MoodProfile>(MoodPath);
        if (mood == null) { Debug.LogWarning("[DioramaLook] Mood_Daytime 없음 — 라이팅은 그대로."); return; }
        var mc = Object.FindFirstObjectByType<MoodController>();
        if (mc == null)
        {
            var go = GameObject.Find("MoodController") ?? new GameObject("MoodController");
            mc = go.AddComponent<MoodController>();
        }
        var so = new SerializedObject(mc);
        so.FindProperty("profile").objectReferenceValue = mood;
        so.ApplyModifiedPropertiesWithoutUndo();
        mc.Apply();
    }

    [MenuItem("PitCrew/Art/Apply Diorama Look")]
    public static void Execute()
    {
        EnsureMood();
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            System.IO.Directory.CreateDirectory("Assets/Rendering");
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        for (int i = profile.components.Count - 1; i >= 0; i--)
            Object.DestroyImmediate(profile.components[i], true);
        profile.components.Clear();

        // 1) 색 — 따뜻하고 진하게 (장난감 색)
        var ca = profile.Add<ColorAdjustments>(true);
        // 1차 캡처에서 ACES+낮은 노출이 하늘을 어둡게 눌러서 Neutral + 노출 ↑ 로 조정
        ca.postExposure.Override(0.35f);
        ca.contrast.Override(18f);
        ca.colorFilter.Override(new Color(1.0f, 0.95f, 0.86f));
        ca.saturation.Override(22f);

        var wb = profile.Add<WhiteBalance>(true);
        wb.temperature.Override(12f);
        wb.tint.Override(2f);

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.Neutral);

        // 2) 미니어처 느낌 — 원경만 부드럽게 (주행 가독성 위해 근경은 선명)
        var dof = profile.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(22f);
        dof.gaussianEnd.Override(70f);
        dof.gaussianMaxRadius.Override(1.2f);
        dof.highQualitySampling.Override(true);

        // 3) 가장자리 어둡게 — 탁자 위 조명 느낌
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.34f);
        vig.smoothness.Override(0.65f);
        vig.rounded.Override(false);

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.35f);
        bloom.threshold.Override(1.0f);
        bloom.scatter.Override(0.6f);

        EditorUtility.SetDirty(profile);

        // Mood 가 PrototypeVolume 의 프로파일을 바꿔도 우리 볼륨이 우선(priority 10)
        var go = GameObject.Find(VolumeName);
        if (go == null) go = new GameObject(VolumeName);
        var vol = go.GetComponent<Volume>();
        if (vol == null) vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f; // 기존 PrototypeVolume(1) 위에 덮어쓴다
        vol.weight = 1f;
        vol.sharedProfile = profile;

        // 카메라 후처리 켜져 있는지 보장
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[DioramaLook] ✓ 디오라마 한 겹 적용 (Volume priority 10). 되돌리기: PitCrew/Art/Revert Diorama Look");
    }

    [MenuItem("PitCrew/Art/Revert Diorama Look")]
    public static void Revert()
    {
        var go = GameObject.Find(VolumeName);
        if (go != null) Object.DestroyImmediate(go);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[DioramaLook] 되돌림 — DioramaVolume 제거 (프로파일 에셋은 남김).");
    }
}
