using UnityEngine;

/// <summary>
/// D-227 노을 = 게임 안 분위기 기준 장면. 씬 하나(Map_TownV6)의 빛·하늘·안개·바다를 "노을 값 묶음" 하나로 바꾼다.
///  · amount 0 = 씬 원래 낮(켜기 직전에 찍어 둔 값으로 그대로 되돌림) · 1 = 노을. 사이 값은 나중에 하루 시간 흐름용(빛·안개·하늘·바다가 같이 섞임).
///  · 공유 머티리얼은 안 건드린다: 땅·집(LofiSurface 등)은 PCLook.hlsl 의 전역 _PCDusk* 로만 안개·그늘을 당기고,
///    하늘·바다는 씬 전용 머티리얼(Map_TownV6_Mats/TV6_SkyDusk·TV6_SeaDusk)로 갈아 끼웠다가 끄면 원래 것으로 돌린다.
///  · 켬/끔: 에디터 메뉴 PitCrew/Look/노을 (PitCrewSunsetMenu). Play 중에도 된다(씬엔 저장 안 됨).
/// </summary>
[ExecuteAlways, DefaultExecutionOrder(950)]
public sealed class SunsetLook : MonoBehaviour
{
    [Range(0f, 1f)] public float amount;

    [Header("해 — 보이는 해(하늘 원반·물 반사)와 빛 방향을 따로(빛까지 3°면 면이 다 꺼멓다)")]
    public float sunAzimuth = 186f;          // 0 = +z(북), 90 = +x
    public float skySunElevation = 3.2f;
    public float lightElevation = 12f;
    public Color sunLight = new Color(1.00f, 0.62f, 0.38f);
    public float sunIntensity = 1.25f;

    [Header("하늘")]
    public Color skyHorizon = new Color(1.00f, 0.55f, 0.26f);   // 해 쪽 지평선
    public Color skyAway = new Color(0.62f, 0.50f, 0.66f);      // 반대쪽 지평선(라벤더)
    public Color skyMid = new Color(0.93f, 0.52f, 0.55f);       // 분홍
    public Color skyTop = new Color(0.26f, 0.27f, 0.52f);       // 보라·청
    public Color sunDisc = new Color(1.00f, 0.93f, 0.72f);
    public Color sunGlow = new Color(1.00f, 0.60f, 0.30f, 1f);  // a = 세기
    public Color cloudLit = new Color(1.00f, 0.56f, 0.50f);
    public Color cloudDark = new Color(0.40f, 0.33f, 0.48f);

    [Header("D-236 7판 도트 하늘 — 색 단 + 단 사이 Bayer 디더(화면 픽셀 고정)")]
    [Tooltip("x 켬 0/1 · y 단 수 · z 단 끝 디더 폭 0..1 · w 디더 칸 크기(렌더 px, 0 = 360 줄 기준 자동)")]
    public Vector4 skyBands = new Vector4(1f, 5f, 0.35f, 0f);
    [Tooltip("x 해 원반 반지름° · y 빛무리 1단 반지름° · z 2단 반지름° · w 빛무리 세기")]
    public Vector4 sunHalo = new Vector4(1.9f, 4.2f, 9.0f, 0.55f);
    [Tooltip("납작한 구름 띠: x 켬 0/1 · y 흐름 °/s · z 덮는 양 0..1 · w 두께 배수")]
    public Vector4 cloudStrips = new Vector4(1f, 0.25f, 0.6f, 1.5f);
    [Tooltip("1px 역광 테(Present): x 켬 0/1 · y 실루엣 밝기 상한 · z 하늘 밝기 하한 · w 섞음")]
    public Vector4 rim = new Vector4(1f, 0.06f, 0.12f, 0.9f);
    public Color rimColor = new Color(1.00f, 0.62f, 0.32f);
    [Tooltip("D-236 노을 때만 켜는 수평선 연출물(먼 곶·섬·등대·배 실루엣, DuskBackdrop). 끄면 숨김.")]
    public GameObject backdrop;
    [Tooltip("D-236 7판 노을일 때만 끄는 렌더러(앉은 시점 섬 자리에 비치는 회색 대역 벽 등). 끄면 다시 켬.")]
    public Renderer[] hideWhileOn;
    [Tooltip("D-236 7판 역광 테 소재(PitCrew/Look/DuskRim). 노을 동안만 LowResRenderTest 화면 올리기에 꽂는다.")]
    public Material rimMaterial;

    [Header("안개·그늘")]
    public Color fog = new Color(0.66f, 0.52f, 0.62f);
    public Vector2 fogStartEnd = new Vector2(60f, 700f);
    public Color shade = new Color(0.52f, 0.48f, 0.78f);        // 그늘 = 청보라 하늘빛(LofiSurface _Ambient 자리)
    public Color a7Shade = new Color(0.40f, 0.36f, 0.58f);      // A7 셰이더 그늘(_A7Shade)

    [Header("씬 전용 머티리얼")]
    public Material skyMaterial;   // PitCrew/Look/DuskSky
    public Material seaMaterial;   // PitCrew/Look/DuskSea
    public Renderer sea;
    [Tooltip("D-232 5판 자리 둘레 촘촘한 바다 격자(SeaNear, seaMaterial 고정). 노을일 때만 켜짐 — 낮엔 꺼서 원래 Sea 판만.")]
    public Renderer seaNear;
    public Light sun;
    [Tooltip("D-229 이 장면에서 숲 나무 F1·F2 로 바꾸느라 꺼 둔 옛 대역 나무(메뉴 PitCrew/노을/나무 되돌리기 로 다시 켬).")]
    public GameObject[] hiddenTrees;

    // 켜기 직전 낮 값 — 끌 때 이것으로 되돌린다(씬에 저장돼 도메인 리로드·Play 진입에도 남는다)
    [System.Serializable]
    public struct Day
    {
        public Quaternion sunRot; public Color sunColor; public float sunIntensity; public LightShadows shadows;
        public Color fogColor; public float fogStart, fogEnd; public Color ambSky, ambEq, ambGround, ambLight;
        public Material skybox, seaMat;
    }
    [SerializeField] Day day;
    [SerializeField] bool captured;
    A7ShadeGlobals a7;
    public bool IsOn => captured;

    static readonly int DuskId = Shader.PropertyToID("_PCDusk"), FogId = Shader.PropertyToID("_PCDuskFog"), GlowId = Shader.PropertyToID("_PCDuskGlow"),
        AmbId = Shader.PropertyToID("_PCDuskAmb"), FogPId = Shader.PropertyToID("_PCDuskFogP"), SunDirId = Shader.PropertyToID("_PCDuskSunDir"),
        HorId = Shader.PropertyToID("_PCDuskHor"), AwayId = Shader.PropertyToID("_PCDuskAway"), MidId = Shader.PropertyToID("_PCDuskMid"), TopId = Shader.PropertyToID("_PCDuskTop"),
        DiscId = Shader.PropertyToID("_PCDuskSunCol"), CLitId = Shader.PropertyToID("_PCDuskCloudLit"), CDarkId = Shader.PropertyToID("_PCDuskCloudDark"),
        DayTopId = Shader.PropertyToID("_PCDayTop"), DayHorId = Shader.PropertyToID("_PCDayHor"), DayBotId = Shader.PropertyToID("_PCDayBot"),
        A7ShadeId = Shader.PropertyToID("_A7Shade"),
        BandsId = Shader.PropertyToID("_PCDuskBands"), HaloId = Shader.PropertyToID("_PCDuskHalo"), StripsId = Shader.PropertyToID("_PCDuskStrips"),
        RimId = Shader.PropertyToID("_PCDuskRim"), RimColId = Shader.PropertyToID("_PCDuskRimCol"), RimHzId = Shader.PropertyToID("_PCDuskRimHz");

    public static SunsetLook Find() => FindFirstObjectByType<SunsetLook>();

    public void SetOn(bool on)
    {
        if (on && !captured) Capture();
        amount = on ? 1f : 0f;
        Apply();
        if (!on) Release();
    }

    void Capture()
    {
        if (sun == null) sun = RenderSettings.sun;
        day = new Day
        {
            sunRot = sun != null ? sun.transform.rotation : Quaternion.identity, sunColor = sun != null ? sun.color : Color.white,
            sunIntensity = sun != null ? sun.intensity : 1f, shadows = sun != null ? sun.shadows : LightShadows.Soft,
            fogColor = RenderSettings.fogColor, fogStart = RenderSettings.fogStartDistance, fogEnd = RenderSettings.fogEndDistance,
            ambSky = RenderSettings.ambientSkyColor, ambEq = RenderSettings.ambientEquatorColor, ambGround = RenderSettings.ambientGroundColor, ambLight = RenderSettings.ambientLight,
            skybox = RenderSettings.skybox, seaMat = sea != null ? sea.sharedMaterial : null,
        };
        captured = true;
    }

    /// <summary>낮 값으로 되돌리고 전역을 0 으로.</summary>
    void Release()
    {
        if (!captured) return;
        if (sun != null) { sun.transform.rotation = day.sunRot; sun.color = day.sunColor; sun.intensity = day.sunIntensity; sun.shadows = day.shadows; }
        RenderSettings.fogColor = day.fogColor; RenderSettings.fogStartDistance = day.fogStart; RenderSettings.fogEndDistance = day.fogEnd;
        RenderSettings.ambientSkyColor = day.ambSky; RenderSettings.ambientEquatorColor = day.ambEq; RenderSettings.ambientGroundColor = day.ambGround; RenderSettings.ambientLight = day.ambLight;
        if (day.skybox != null) RenderSettings.skybox = day.skybox;
        if (sea != null && day.seaMat != null) sea.sharedMaterial = day.seaMat;
        if (seaNear != null) seaNear.enabled = false;
        if (backdrop != null) backdrop.SetActive(false);
        SetHidden(false);
        SetRimOverlay(false);
        Shader.SetGlobalVector(DuskId, Vector4.zero);
        if (a7 == null) a7 = FindFirstObjectByType<A7ShadeGlobals>(); if (a7 != null) a7.Apply();
        captured = false;
    }

    void OnEnable() { if (captured) Apply(); else Shader.SetGlobalVector(DuskId, Vector4.zero); }
    void OnDisable() { Shader.SetGlobalVector(DuskId, Vector4.zero); }
    void Update() { if (captured) PushGlobals(); }   // 다른 컴포넌트가 전역을 덮어도 매 프레임 다시

    static Vector3 Dir(float az, float el)
    {
        float a = az * Mathf.Deg2Rad, e = el * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Cos(a) * Mathf.Cos(e));
    }

    /// <summary>amount 만큼 낮 → 노을. 빛·안개·하늘·바다 한꺼번에.</summary>
    public void Apply()
    {
        if (!captured) { Shader.SetGlobalVector(DuskId, Vector4.zero); return; }
        float k = Mathf.Clamp01(amount);
        if (sun != null)
        {
            var rot = Quaternion.LookRotation(-Dir(sunAzimuth, lightElevation));
            sun.transform.rotation = Quaternion.Slerp(day.sunRot, rot, k);
            sun.color = Color.Lerp(day.sunColor, sunLight, k);
            sun.intensity = Mathf.Lerp(day.sunIntensity, sunIntensity, k);
        }
        RenderSettings.fogColor = Color.Lerp(day.fogColor, fog, k);
        RenderSettings.fogStartDistance = Mathf.Lerp(day.fogStart, fogStartEnd.x, k);
        RenderSettings.fogEndDistance = Mathf.Lerp(day.fogEnd, fogStartEnd.y, k);
        RenderSettings.ambientLight = Color.Lerp(day.ambLight, shade, k);
        RenderSettings.ambientSkyColor = Color.Lerp(day.ambSky, shade, k);
        if (k > 0f && skyMaterial != null) RenderSettings.skybox = skyMaterial; else if (day.skybox != null) RenderSettings.skybox = day.skybox;
        if (sea != null) sea.sharedMaterial = k > 0f && seaMaterial != null ? seaMaterial : day.seaMat;
        if (seaNear != null) seaNear.enabled = k > 0f && seaMaterial != null && sea != null;
        if (backdrop != null && backdrop.activeSelf != k > 0f) backdrop.SetActive(k > 0f);
        SetHidden(k > 0f);
        PushGlobals();
    }

    void SetHidden(bool hide)
    {
        if (hideWhileOn == null) return;
        foreach (var r in hideWhileOn) if (r != null) r.enabled = !hide;
    }

    void SetRimOverlay(bool on)
    {
        var cm = Camera.main; var lr = cm != null ? cm.GetComponent<LowResRenderTest>() : null;
        if (lr != null) lr.overlay = on ? rimMaterial : null;
    }

    void PushGlobals()
    {
        float k = Mathf.Clamp01(amount);
        Shader.SetGlobalVector(DuskId, new Vector4(k, 0, 0, 0));
        Shader.SetGlobalColor(FogId, fog.linear);
        Shader.SetGlobalColor(GlowId, new Color(sunGlow.linear.r, sunGlow.linear.g, sunGlow.linear.b, sunGlow.a));
        Shader.SetGlobalColor(AmbId, shade.linear);
        Shader.SetGlobalVector(FogPId, new Vector4(fogStartEnd.x, fogStartEnd.y, 0, 0));
        Shader.SetGlobalVector(SunDirId, Dir(sunAzimuth, skySunElevation));
        Shader.SetGlobalColor(HorId, skyHorizon.linear); Shader.SetGlobalColor(AwayId, skyAway.linear);
        Shader.SetGlobalColor(MidId, skyMid.linear); Shader.SetGlobalColor(TopId, skyTop.linear);
        Shader.SetGlobalColor(DiscId, sunDisc.linear); Shader.SetGlobalColor(CLitId, cloudLit.linear); Shader.SetGlobalColor(CDarkId, cloudDark.linear);
        Shader.SetGlobalVector(BandsId, skyBands); Shader.SetGlobalVector(HaloId, sunHalo); Shader.SetGlobalVector(StripsId, cloudStrips);
        Shader.SetGlobalVector(RimId, k > 0f ? rim : Vector4.zero); Shader.SetGlobalColor(RimColId, rimColor.linear);
        SetRimOverlay(k > 0f && rim.x > .5f);
        var cm = Camera.main;   // 테는 수평선 위만(물속 어두운 단 + 빛길 밝은 단 경계에 테가 생기지 않게)
        if (cm != null)
        {
            var f = cm.transform.forward; f.y = 0f; float hz = f.sqrMagnitude > 1e-6f ? cm.WorldToViewportPoint(cm.transform.position + f.normalized * 2000f).y : 0f;
            var sv = cm.WorldToViewportPoint(cm.transform.position + Dir(sunAzimuth, skySunElevation) * 2000f);   // y = 해 화면 x(테는 해 쪽 가장자리에만)
            var lr = cm.GetComponent<LowResRenderTest>(); var rs = lr != null ? lr.RenderSize : Vector2Int.zero;   // zw = 실제 렌더 격자(RT 는 renderScale 로 더 클 수 있음)
            Shader.SetGlobalVector(RimHzId, new Vector4(hz - 0.004f, sv.z > 0f ? sv.x : 0.5f, rs.x > 0 ? rs.x : 0, rs.y > 0 ? rs.y : 0));
        }
        // 낮 하늘(사이 값용) = 원래 GradientSky 색
        var ds = day.skybox;
        if (ds != null && ds.HasProperty("_TopColor"))
        {
            Shader.SetGlobalColor(DayTopId, ds.GetColor("_TopColor").linear); Shader.SetGlobalColor(DayHorId, ds.GetColor("_HorizonColor").linear);
            Shader.SetGlobalColor(DayBotId, ds.GetColor("_BottomColor").linear);
        }
        if (a7 == null) a7 = FindFirstObjectByType<A7ShadeGlobals>();
        if (a7 != null && k > 0f)
        {
            var baseShade = Color.Lerp(a7.skyFogColor, a7.shadeTeal, a7.tealAmount) * Mathf.Max(0.3f, a7.ambientAmount);
            var c = Color.Lerp(baseShade, a7Shade, k); c.a = 1f; Shader.SetGlobalColor(A7ShadeId, c);
        }
    }
}
