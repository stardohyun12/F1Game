using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// MoodProfile을 씬의 라이팅·RenderSettings·전역 Volume에 적용.
/// 런타임 Awake 적용 + 에디터 [ContextMenu]로 미리보기. 무드 교체 = profile 필드만 바꿈.
/// </summary>
[ExecuteAlways]
public sealed class MoodController : MonoBehaviour
{
    [SerializeField] private MoodProfile profile;
    [Tooltip("비우면 씬에서 첫 Directional Light 자동 탐색")]
    [SerializeField] private Light sun;
    [Tooltip("비우면 씬에서 첫 전역 Volume 자동 탐색")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private bool applyOnAwake = true;

    void Awake()
    {
        if (Application.isPlaying && applyOnAwake) Apply();
    }

    [ContextMenu("Apply Mood")]
    public void Apply()
    {
        if (profile == null) { Debug.LogWarning("[MoodController] profile 미지정."); return; }
        ApplyProfile(profile, sun, globalVolume);
    }

    public MoodProfile Profile => profile;

    /// <summary>런타임 시간대 전환(D-137 하루 빛). 판 순서에 따른 자동 전환은 호출하는 쪽이 정한다.</summary>
    public void SetProfile(MoodProfile next)
    {
        profile = next;
        Apply();
    }

    /// <summary>씬에 MoodController 없이도 적용 (에디터 메뉴 PitCrew/Art/Mood 가 쓴다).</summary>
    public static void ApplyProfile(MoodProfile profile, Light sun = null, Volume globalVolume = null)
    {
        // 태양
        var light = sun != null ? sun : FindDirectionalLight();
        if (light != null)
        {
            light.transform.rotation = Quaternion.Euler(profile.sunRotationEuler);
            light.color = profile.sunColor;
            light.intensity = profile.sunIntensity;
            light.shadows = profile.castShadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = profile.shadowStrength;
        }

        // 앰비언트
        RenderSettings.ambientMode = profile.ambientMode;
        RenderSettings.ambientLight = profile.ambientColor;
        RenderSettings.ambientIntensity = profile.ambientIntensity;

        // Fog
        RenderSettings.fog = profile.fog;
        RenderSettings.fogMode = profile.fogMode;
        RenderSettings.fogColor = profile.fogColor;
        RenderSettings.fogDensity = profile.fogDensity;

        // 선택: 스카이박스 / 포스트
        if (profile.skybox != null) RenderSettings.skybox = profile.skybox;
        var vol = globalVolume != null ? globalVolume : FindFirstObjectByType<Volume>();
        if (vol != null && profile.postProfile != null) vol.sharedProfile = profile.postProfile;

        if (profile.syncWorld)
        {
            RenderSettings.fogStartDistance = profile.fogStart;
            RenderSettings.fogEndDistance = profile.fogEnd;
            SyncWorldMaterials(profile);
        }

        SetWindowsLit(profile.windowsLit);
    }

    /// <summary>창 발광 묶음(이름 "WindowGlow", 빌더가 만든다) 켜고 끄기. 꺼진 오브젝트도 찾는다.</summary>
    public static void SetWindowsLit(bool on)
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "WindowGlow" && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
    }

    static readonly int AmbientId = Shader.PropertyToID("_Ambient"), FogColorId = Shader.PropertyToID("_FogColor"),
        FogStartId = Shader.PropertyToID("_FogStart"), FogEndId = Shader.PropertyToID("_FogEnd"), TintId = Shader.PropertyToID("_Tint");

    /// <summary>씬 렌더러가 쓰는 월드 셰이더 머티리얼(공유 에셋)에 안개·주변광 한 색을 쓴다. 에디터에선 에셋이 바뀐다 → 메뉴가 스냅샷으로 되돌린다.</summary>
    public static void SyncWorldMaterials(MoodProfile profile)
    {
        var amb = profile.fogColor * profile.worldAmbientScale; amb.a = 1f;
        foreach (var m in WorldMaterials())
        {
            m.SetColor(AmbientId, amb);
            m.SetColor(FogColorId, profile.fogColor);
            m.SetFloat(FogStartId, profile.fogStart);
            m.SetFloat(FogEndId, profile.fogEnd);
            if (profile.railTint.a > 0f && m.name == "BoundaryRail" && m.HasProperty(TintId)) m.SetColor(TintId, profile.railTint);
            if (profile.seaTint.a > 0f && m.name == "Sea" && m.HasProperty(TintId)) m.SetColor(TintId, profile.seaTint);
        }
    }

    public static System.Collections.Generic.List<Material> WorldMaterials()
    {
        var set = new System.Collections.Generic.HashSet<Material>();
        if (PCLookScene.OwnsWorld) return new System.Collections.Generic.List<Material>();   // 2024: Look 5 는 안개·빛을 셰이더 전역으로 받는다 → 공유 머티리얼 에셋에 쓰지 않는다
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var m in r.sharedMaterials)
                if (m != null && m.HasProperty(FogColorId) && m.HasProperty(AmbientId) && m.HasProperty(FogStartId) && m.HasProperty(FogEndId)) set.Add(m);
        return new System.Collections.Generic.List<Material>(set);
    }

    static Light FindDirectionalLight()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) return l;
        return null;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying && profile != null) Apply();
    }
#endif
}
