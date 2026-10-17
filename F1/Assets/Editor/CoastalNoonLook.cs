using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// D-101 (2026-10-13) — 색감 기준 "아말피 한낮". 도현: "지금은 너무 어둡고 음산".
/// 진단: 회청색 주변광(Skybox 모드의 회색 기본 하늘) + 회청색 안개 + 베이지 벽 → 흐린 오후.
/// 손잡이: 하늘(그라데이션, 위 진한 파랑) / 해(높이 62°, 세기 1.45, 거의 흰색, 그림자 진하게) / 주변광(하늘빛 흰색, Trilight) /
/// 안개(더 멀리·밝은 하늘색) / 로파이 재질의 _Ambient·_FogColor·_FaceShade / 바다 채도 / 후처리 노출·채도 살짝.
/// 벽 칸(plaster → 크림)은 lofi_pilot.py 팔레트에서. 멱등 — 설치기(Install)도 이걸 부른다.
/// 메뉴: PitCrew/Art/Coastal Town/Apply Noon Light
/// </summary>
public static class CoastalNoonLook
{
    public static readonly Color FogColor = new Color(.76f, .86f, .93f);
    public const float FogStart = 75f, FogEnd = 240f;

    [MenuItem("PitCrew/Art/Coastal Town/Apply Noon Light")]
    public static void ApplyMenu() { Apply(); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); }

    public static void Apply()
    {
        // 하늘: 세션 28 의 그라데이션 셰이더. 위 진한 파랑 → 수평선 거의 흰색.
        var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/GradientSky.mat");
        if (sky)
        {
            sky.SetColor("_TopColor", new Color(.14f, .38f, .80f)); sky.SetColor("_HorizonColor", new Color(.74f, .86f, .96f)); sky.SetColor("_BottomColor", new Color(.66f, .76f, .84f));   // v4: 위 더 진한 파랑
            sky.SetFloat("_Exponent", 1.15f);   // v4: 파랑이 더 낮게 내려오게 EditorUtility.SetDirty(sky); RenderSettings.skybox = sky;
        }
        // 해: 높이 62° → 짧고 진한 그림자. 거의 흰 빛.
        var light = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>()).FirstOrDefault(l => l.type == LightType.Directional);
        if (light)
        {
            light.transform.rotation = Quaternion.Euler(52f, -35f, 0f); light.color = new Color(1f, .95f, .86f); light.intensity = 1.25f;   // v3: 셰이더가 해 세기를 쓰게 됨 → 1.3 (1.7 은 흰색 포화), 52° 로 그림자가 벽·도로에 떨어지게
            light.shadows = LightShadows.Soft; light.shadowStrength = 1f; EditorUtility.SetDirty(light);
        }
        // 주변광: 하늘빛 흰색 (Skybox 모드는 기본 하늘이 회색이라 어두웠음)
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.60f, .70f, .82f); RenderSettings.ambientEquatorColor = new Color(.68f, .66f, .60f); RenderSettings.ambientGroundColor = new Color(.42f, .39f, .33f);   // v2: 주변광 낮춰 그림자 안이 차지 않게
        // 안개: 더 멀리, 밝은 하늘색
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = FogColor; RenderSettings.fogStartDistance = FogStart; RenderSettings.fogEndDistance = FogEnd;
        // 로파이 재질 전부: 주변 톤 흰색, 안개 동기화, 면 명암 ↑ (빛이 세지면 질감이 날아가므로 명암으로 깊이 유지)
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art/CoastalTown", "Assets/Art/LofiPilot", "Assets/Art/CoastalFormula", "Assets/Art/StreetFurniture" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (!m || !m.HasProperty("_FogColor")) continue;
            m.SetColor("_Ambient", new Color(.72f, .82f, 1f));   // v3: 그늘 = 하늘빛(살짝 파랑), 양지 = 따뜻한 해 → 맑은 날 대비 m.SetColor("_FogColor", FogColor); m.SetFloat("_FogStart", FogStart); m.SetFloat("_FogEnd", FogEnd);
            if (m.HasProperty("_FaceShade") && m.shader.name.Contains("WorldSurface")) m.SetFloat("_FaceShade", .9f);   // v3: 바닥도 거의 전부 조명 (그늘이 보이게). 건물(LofiShared)은 1.0
            if (m.name == "Sea") m.SetColor("_Tint", new Color(.30f, .64f, .74f));          // 바다 채도 ↑
            if (m.name == "Asphalt" && m.shader.name.Contains("WorldSurface")) m.SetColor("_Tint", new Color(1f, .97f, .92f));        // v2: 옛 아스팔트만. 승인 골재 도로는 자기 틴트
            if (m.name == "DistantHills") m.SetColor("_Tint", new Color(.56f, .70f, .78f));  // 원경 언덕은 하늘에 녹게
            EditorUtility.SetDirty(m);
        }
        // 후처리: 노출 살짝, 채도 ↑, 대비 조금 (WarmGrade 프로필의 다른 항목은 그대로)
        // 메인 카메라 후처리 켜기 — 꺼져 있어 대비·채도·블룸이 전혀 안 그려졌다 (2026-10-13 발견)
        var cam = Camera.main; if (cam) { var cd = cam.GetUniversalAdditionalCameraData(); if (cd && !cd.renderPostProcessing) { cd.renderPostProcessing = true; EditorUtility.SetDirty(cam); } }
        var vol = Object.FindFirstObjectByType<Volume>();
        if (vol && vol.sharedProfile)
        {
            var profile = vol.sharedProfile;
            profile.components.RemoveAll(c => c == null);   // 이전에 AddObjectToAsset 없이 넣어 fileID 0 으로 남은 빈 칸 정리 (verifier 21:50 발견)
            if (!profile.TryGet<ColorAdjustments>(out var ca)) { ca = profile.Add<ColorAdjustments>(true); ca.name = "ColorAdjustments"; AssetDatabase.AddObjectToAsset(ca, profile); }
            if (!profile.TryGet<Bloom>(out var bloom)) { bloom = profile.Add<Bloom>(true); bloom.name = "Bloom"; AssetDatabase.AddObjectToAsset(bloom, profile); }
            ca.active = true; bloom.active = true;
            bloom.threshold.Override(.95f); bloom.intensity.Override(.35f); bloom.scatter.Override(.6f);   // v4: 흰 벽·바다 반짝임
            ca.postExposure.Override(.15f); ca.saturation.Override(18f); ca.contrast.Override(22f);   // v4: 대비 ↑
            EditorUtility.SetDirty(profile); EditorUtility.SetDirty(ca); EditorUtility.SetDirty(bloom);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[NoonLook] 한낮 조명 적용: 해 62°/1.45, Trilight 주변광, 안개 75~240, 재질 _Ambient 흰색, 바다 채도, 후처리 노출+0.25/채도+12/대비+8");
    }
}
