using UnityEngine;

/// <summary>
/// 과제 2050 장면 A7(D-185): 빛 없는 albedo + 게임 안 해 1개. 인스펙터에서 바로 반영(에디트·Play).
///  · 해: 자식 "A7_Sun" 방향광. 높이(°)·방위(°)·색. PitCrew/SceneA7/Lit 재질이 이 빛의 방향·색·그림자를 3단으로 읽는다.
///  · 그늘 색 = 안개색과 청록 사이 × 주변광 세기 (전역 _A7Shade). 0.3 밑으로는 안 내려가 검정이 안 됨.
///  · 그림자: 켬 = 단단한 그림자. 거리·해상도는 Play 중에만 URP 에셋에 잠깐 넣고 Play 끝나면 원래 값으로 돌린다(에셋 저장 안 함).
/// </summary>
[ExecuteAlways]
public sealed class A7LightToggles : MonoBehaviour
{
    [Header("해 — 늦가을 오후")]
    [Range(10f, 60f)] public float sunElevation = 30f;
    [Tooltip("132 = A6d 계단 명암(_RampSun)과 같은 쪽에서 비춤")]
    [Range(0f, 360f)] public float sunAzimuth = 132f;
    public Color sunColor = new Color(1f, 0.86f, 0.68f);
    [Header("주변광(그늘) — 하늘·안개색 쪽")]
    [Range(0.3f, 1f)] public float ambientAmount = 0.62f;
    public Color skyFogColor = new Color(0.8f, 0.87f, 0.93f);
    public Color shadeTeal = new Color(0.55f, 0.76f, 0.80f);
    [Range(0f, 1f)] public float tealAmount = 0.5f;
    [Header("3단 문턱 (해 쪽 면 · 옆 면)")]
    [Range(0f, 1f)] public float brightAbove = 0.3f;
    [Range(-0.5f, 0.5f)] public float sideAbove = 0.02f;
    [Range(0f, 1f)] public float sideLevel = 0.6f;
    [Header("그림자 (a 켬 / b 끔)")]
    public bool realtimeShadows = true;
    public float shadowDistance = 40f;
    [Tooltip("256·512·1024·2048. 작을수록 그림자 가장자리가 굵은 픽셀")]
    public int shadowResolution = 512;

    Light sun; string applied;
    // Scripts 어셈블리가 URP 를 참조하지 않아 URP 에셋 값은 이름으로 읽고 쓴다
    Object urp; object oDist, oRes, oCasc; bool saved;
    static readonly string[] UrpProps = { "shadowDistance", "mainLightShadowmapResolution", "shadowCascadeCount" };
    object Get(string n) => urp.GetType().GetProperty(n).GetValue(urp);
    void Set(string n, object v) { var p = urp.GetType().GetProperty(n); if (p != null && p.CanWrite) p.SetValue(urp, v); }

    static readonly int ShadeId = Shader.PropertyToID("_A7Shade"), TId = Shader.PropertyToID("_A7T"), MidId = Shader.PropertyToID("_A7Mid");

    void OnEnable() => applied = null;

    void OnDisable()
    {
        if (saved && urp) { Set(UrpProps[0], oDist); Set(UrpProps[1], oRes); Set(UrpProps[2], oCasc); }
        saved = false;
    }

    void Update()
    {
        string k = sunElevation + "/" + sunAzimuth + "/" + sunColor + "/" + ambientAmount + "/" + skyFogColor + "/" + shadeTeal + "/" + tealAmount + "/"
            + brightAbove + "/" + sideAbove + "/" + sideLevel + "/" + realtimeShadows + "/" + shadowDistance + "/" + shadowResolution + "/" + Application.isPlaying;
        if (k == applied && sun) return;
        applied = k;
        Apply();
    }

    public void Apply()
    {
        if (!sun)
        {
            var t = transform.Find("A7_Sun");
            if (!t) { t = new GameObject("A7_Sun").transform; t.SetParent(transform, false); }
            sun = t.GetComponent<Light>() ? t.GetComponent<Light>() : t.gameObject.AddComponent<Light>();
        }
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(sunElevation, sunAzimuth, 0f);
        sun.color = sunColor; sun.intensity = 1f;
        sun.shadows = realtimeShadows ? LightShadows.Hard : LightShadows.None;
        sun.shadowStrength = 1f; sun.shadowBias = 0.05f; sun.shadowNormalBias = 0.4f;
        RenderSettings.sun = sun;

        var shade = Color.Lerp(skyFogColor, shadeTeal, tealAmount) * Mathf.Max(0.3f, ambientAmount);
        Shader.SetGlobalColor(ShadeId, new Color(shade.r, shade.g, shade.b, 1f));
        Shader.SetGlobalVector(TId, new Vector4(brightAbove, sideAbove, 0f, 0f));
        Shader.SetGlobalFloat(MidId, sideLevel);

        if (!Application.isPlaying) return;
        urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (!urp) return;
        if (!saved) { oDist = Get(UrpProps[0]); oRes = Get(UrpProps[1]); oCasc = Get(UrpProps[2]); saved = true; }
        Set(UrpProps[0], shadowDistance); Set(UrpProps[1], shadowResolution); Set(UrpProps[2], 1);
    }
}
