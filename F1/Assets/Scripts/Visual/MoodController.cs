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
    }

    Light FindDirectionalLight()
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
