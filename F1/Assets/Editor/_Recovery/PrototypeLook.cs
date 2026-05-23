using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;

/// <summary>
/// "묵직한 양식화" 룩 프로토타입 — 라이팅·재질·포스트를 풀로 밀어 "실제 느낌" 나는지 확인용.
/// 멱등. 모델 안 바꾸고 연출만. 도현 판단용. (RunFullRebuild 후 재실행)
/// </summary>
public static class PrototypeLook
{
    [MenuItem("PitCrew/Map/Prototype Look (Heavy)")]
    public static void Apply()
    {
        // 1) 태양: 따뜻하고 강하게 + 소프트 그림자
        var sun = Object.FindFirstObjectByType<Light>();
        if (sun != null)
        {
            sun.type = LightType.Directional;
            sun.intensity = 1.35f;
            sun.color = new Color(1f, 0.95f, 0.84f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.transform.rotation = Quaternion.Euler(46f, 35f, 0f);
        }
        // 2) 앰비언트 대비(평평함 제거) + 따뜻한 fog
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.21f, 0.18f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.74f, 0.77f, 0.80f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0016f;

        // 3) PBR 약간의 광택 (Track/Buildings/Ground)
        foreach (var n in new[] { "Track", "Buildings", "Ground" })
        {
            var go = GameObject.Find(n); if (go == null) continue;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                var m = mr.sharedMaterial;
                if (m != null && m.HasProperty("_Smoothness"))
                    m.SetFloat("_Smoothness", n == "Ground" ? 0.12f : 0.28f);
            }
        }

        // 4) 포스트프로세싱 글로벌 볼륨 (그레이딩·블룸·비네팅·ACES)
        var vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/PrototypeLook.asset");
        if (vp == null)
        {
            vp = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(vp, "Assets/Settings/PrototypeLook.asset");
        }
        SetTone(vp);
        var volGO = GameObject.Find("PrototypeVolume") ?? new GameObject("PrototypeVolume");
        volGO.name = "PrototypeVolume";
        var vol = volGO.GetComponent<Volume>() ?? volGO.AddComponent<Volume>();
        vol.isGlobal = true; vol.priority = 10; vol.profile = vp;

        var cam = Camera.main;
        if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        EditorUtility.SetDirty(vp); AssetDatabase.SaveAssets();
        if (sun != null) { EditorSceneManager.MarkSceneDirty(sun.gameObject.scene); EditorSceneManager.SaveScene(sun.gameObject.scene); }
        Debug.Log("[PrototypeLook] 라이팅+PBR+포스트 적용 완료.");
    }

    static void SetTone(VolumeProfile vp)
    {
        var tm = Get<Tonemapping>(vp); tm.mode.overrideState = true; tm.mode.value = TonemappingMode.ACES;
        var ca = Get<ColorAdjustments>(vp);
        ca.postExposure.overrideState = true; ca.postExposure.value = 0.15f;
        ca.contrast.overrideState = true; ca.contrast.value = 18f;
        ca.saturation.overrideState = true; ca.saturation.value = 14f;
        var wb = Get<WhiteBalance>(vp); wb.temperature.overrideState = true; wb.temperature.value = 14f;
        var bl = Get<Bloom>(vp); bl.intensity.overrideState = true; bl.intensity.value = 0.45f; bl.threshold.overrideState = true; bl.threshold.value = 1.1f;
        var vg = Get<Vignette>(vp); vg.intensity.overrideState = true; vg.intensity.value = 0.26f; vg.smoothness.overrideState = true; vg.smoothness.value = 0.5f;
    }
    static T Get<T>(VolumeProfile vp) where T : VolumeComponent
    {
        if (vp.TryGet<T>(out var c)) return c;
        return vp.Add<T>(true);
    }
}
