using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 과제 2050·0236 장면 A7(D-185): 빛 없는 albedo + 게임 안 해 1개. 인스펙터에서 바로 반영.
///  · 해: 자식 "A7_Sun" 방향광. 높이(°)·방위(°)·색. PitCrew/SceneA7/Lit 재질이 이 빛의 방향·색·그림자를 3단으로 읽는다.
///  · 그늘 색 = 안개색과 청록 사이 × 주변광 세기 (전역 _A7Shade). 0.3 밑으로는 안 내려가 검정이 안 됨.
///  · 그림자: 켬 = 단단한 그림자. 거리·해상도·캐스케이드는 씬 전용 URP 에셋 사본(sceneRP)에 넣고, Play 중에만 그 사본을 쓴다.
///    공용 URP 에셋은 건드리지 않고, Play 가 끝나면 원래 파이프라인으로 돌린다.
///  · 붓질(painterly): 720 RT 를 화면에 올릴 때 Kuwahara(반경 RT 텍셀). LowResRenderTest 의 presentMaterial 자리에 잠깐 꽂는다.
/// </summary>
[ExecuteAlways]
public sealed class A7LightToggles : MonoBehaviour
{
    [Header("해 — 늦가을 오후")]
    [Range(10f, 60f)] public float sunElevation = 30f;
    [Tooltip("해가 가는 쪽(빛 방향)의 방위")]
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
    [Tooltip("씬 전용 URP 에셋 사본(Assets/Art/SceneA7/A7_RPAsset). Play 중에만 쓴다")]
    public Object sceneRP;
    [Header("붓질 필터 (720 RT 위 Kuwahara)")]
    public bool painterly = false;
    [Range(1, 4)] public int painterlyRadius = 2;
    public Shader kuwaharaShader;

    Light sun; string applied;
    bool rpSwapped; UnityEngine.Rendering.RenderPipelineAsset oldRP;
    Material kuwa; readonly Dictionary<LowResRenderTest, (bool smooth, Material mat)> lrOld = new Dictionary<LowResRenderTest, (bool, Material)>();

    static readonly int ShadeId = Shader.PropertyToID("_A7Shade"), TId = Shader.PropertyToID("_A7T"), MidId = Shader.PropertyToID("_A7Mid");

    void OnEnable() => applied = null;

    void OnDisable()
    {
        if (rpSwapped) { QualitySettings.renderPipeline = oldRP; rpSwapped = false; }
        RestorePresent();
    }

    void Update()
    {
        string k = sunElevation + "/" + sunAzimuth + "/" + sunColor + "/" + ambientAmount + "/" + skyFogColor + "/" + shadeTeal + "/" + tealAmount + "/"
            + brightAbove + "/" + sideAbove + "/" + sideLevel + "/" + realtimeShadows + "/" + shadowDistance + "/" + shadowResolution + "/"
            + painterly + "/" + painterlyRadius + "/" + Application.isPlaying;
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
        // 씬 전용 URP 사본: 값은 사본에만 쓴다(Scripts 어셈블리가 URP 를 참조하지 않아 이름으로)
        if (sceneRP is UnityEngine.Rendering.RenderPipelineAsset rp)
        {
            SetProp(rp, "shadowDistance", shadowDistance); SetProp(rp, "mainLightShadowmapResolution", shadowResolution); SetProp(rp, "shadowCascadeCount", 1);
            if (!rpSwapped) { oldRP = QualitySettings.renderPipeline; QualitySettings.renderPipeline = rp; rpSwapped = true; }
        }
        if (painterly) UsePainterly(); else RestorePresent();
    }

    static void SetProp(Object o, string n, object v) { var p = o.GetType().GetProperty(n); if (p != null && p.CanWrite) p.SetValue(o, v); }

    void UsePainterly()
    {
        if (!kuwa && kuwaharaShader) kuwa = new Material(kuwaharaShader) { name = "A7Kuwahara" };
        if (!kuwa) return;
        kuwa.SetFloat("_Radius", painterlyRadius);
        foreach (var lr in FindObjectsByType<LowResRenderTest>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!lrOld.ContainsKey(lr)) lrOld[lr] = (lr.smooth, lr.presentMaterial);
            lr.smooth = true; lr.presentMaterial = kuwa;
        }
    }

    void RestorePresent()
    {
        foreach (var kv in lrOld) if (kv.Key) { kv.Key.smooth = kv.Value.smooth; kv.Key.presentMaterial = kv.Value.mat; }
        lrOld.Clear();
    }
}
