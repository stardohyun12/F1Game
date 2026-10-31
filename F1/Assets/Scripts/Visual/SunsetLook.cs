using UnityEngine;

/// <summary>
/// D-227 노을 = 게임 안 분위기 기준 장면. 씬 하나(Map_TownV6)의 빛·하늘·안개·바다를 "노을 값 묶음" 하나로 바꾼다.
///  · amount 0 = 씬 원래 낮(켜기 직전에 찍어 둔 값으로 그대로 되돌림) · 1 = 이 Look 켬. 사이 값은 옛 낮과 섞임.
///  · D-239 하루 빛 묶음: 그리는 방식(도트 하늘·디더·해 원반·역광 테·보색 그늘·DuskSea)은 하나, 시간대별 색만 다르다.
///    아래 맨 위 필드들 = 노을 프리셋(9판 값 그대로), afternoon = 오후 프리셋. dusk 0 = 오후 · 1 = 노을, 사이는 모든 값 보간(D-224 하루 흐름용).
///    바다 색은 머티리얼 에셋을 안 바꾸고 렌더러별 MaterialPropertyBlock(노을 쪽 = 머티리얼 값 그대로). 촘촘한 바다 격자는 카메라를 따라간다.
///  · 공유 머티리얼은 안 건드린다: 땅·집(LofiSurface 등)은 PCLook.hlsl 의 전역 _PCDusk* 로만 안개·그늘을 당기고,
///    하늘·바다는 씬 전용 머티리얼(Map_TownV6_Mats/TV6_SkyDusk·TV6_SeaDusk)로 갈아 끼웠다가 끄면 원래 것으로 돌린다.
///  · 켬/끔: 에디터 메뉴 PitCrew/Look/노을 (PitCrewSunsetMenu). Play 중에도 된다(씬엔 저장 안 됨).
/// </summary>
[ExecuteAlways, DefaultExecutionOrder(950)]
public sealed class SunsetLook : MonoBehaviour
{
    [Range(0f, 1f)] public float amount;
    [Tooltip("D-239 시간대: 0 = 오후 프리셋(afternoon) · 1 = 노을 프리셋(아래 필드들). 사이 = 보간")]
    [Range(0f, 1f)] public float dusk = 1f;
    [Tooltip("D-239 수평선 연출물·노을 전용 숨김은 dusk 가 이 값 이상일 때만")]
    public float duskPropsFrom = 0.5f;

    /// <summary>D-239 시간대 한 벌. 노을 프리셋은 이 컴포넌트 맨 위 필드들(씬 값)에서 만든다(Sunset()).</summary>
    [System.Serializable]
    public class Palette
    {
        public float sunAzimuth, skySunElevation, lightElevation; public Color sunLight; public float sunIntensity;
        public Color skyHorizon, skyAway, skyMid, skyTop, sunDisc, sunGlow, cloudLit, cloudDark;
        public Vector4 skyBands, sunHalo, cloudStrips, rim; public Color rimColor; public Vector4 screenGlow; public Color screenGlowColor;
        public Color fog; public Vector2 fogStartEnd; public Color shade, a7Shade;
        [Header("바다(DuskSea 색) — 노을 프리셋은 seaMaterial 값을 그대로 쓴다")]
        public Color seaShallow, seaDeep, seaCoolCol, seaGlow, seaHot, seaCore, seaFoam, seaCrest, seaRipple;
        public Vector4 seaCool, seaLv, seaSceneRefl;

        public static Palette Lerp(Palette a, Palette b, float t)
        {
            if (t <= 0f) return a; if (t >= 1f) return b;
            return new Palette
            {
                sunAzimuth = Mathf.LerpAngle(a.sunAzimuth, b.sunAzimuth, t), skySunElevation = Mathf.Lerp(a.skySunElevation, b.skySunElevation, t),
                lightElevation = Mathf.Lerp(a.lightElevation, b.lightElevation, t), sunLight = Color.Lerp(a.sunLight, b.sunLight, t), sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
                skyHorizon = Color.Lerp(a.skyHorizon, b.skyHorizon, t), skyAway = Color.Lerp(a.skyAway, b.skyAway, t), skyMid = Color.Lerp(a.skyMid, b.skyMid, t), skyTop = Color.Lerp(a.skyTop, b.skyTop, t),
                sunDisc = Color.Lerp(a.sunDisc, b.sunDisc, t), sunGlow = Color.Lerp(a.sunGlow, b.sunGlow, t), cloudLit = Color.Lerp(a.cloudLit, b.cloudLit, t), cloudDark = Color.Lerp(a.cloudDark, b.cloudDark, t),
                skyBands = new Vector4(b.skyBands.x, Mathf.Round(Mathf.Lerp(a.skyBands.y, b.skyBands.y, t)), Mathf.Lerp(a.skyBands.z, b.skyBands.z, t), Mathf.Round(Mathf.Lerp(a.skyBands.w, b.skyBands.w, t))),
                sunHalo = Vector4.Lerp(a.sunHalo, b.sunHalo, t), cloudStrips = Vector4.Lerp(a.cloudStrips, b.cloudStrips, t), rim = Vector4.Lerp(a.rim, b.rim, t), rimColor = Color.Lerp(a.rimColor, b.rimColor, t),
                screenGlow = Vector4.Lerp(a.screenGlow, b.screenGlow, t), screenGlowColor = Color.Lerp(a.screenGlowColor, b.screenGlowColor, t),
                fog = Color.Lerp(a.fog, b.fog, t), fogStartEnd = Vector2.Lerp(a.fogStartEnd, b.fogStartEnd, t), shade = Color.Lerp(a.shade, b.shade, t), a7Shade = Color.Lerp(a.a7Shade, b.a7Shade, t),
                seaShallow = Color.Lerp(a.seaShallow, b.seaShallow, t), seaDeep = Color.Lerp(a.seaDeep, b.seaDeep, t), seaCoolCol = Color.Lerp(a.seaCoolCol, b.seaCoolCol, t),
                seaGlow = Color.Lerp(a.seaGlow, b.seaGlow, t), seaHot = Color.Lerp(a.seaHot, b.seaHot, t), seaCore = Color.Lerp(a.seaCore, b.seaCore, t), seaFoam = Color.Lerp(a.seaFoam, b.seaFoam, t),
                seaCrest = Color.Lerp(a.seaCrest, b.seaCrest, t), seaRipple = Color.Lerp(a.seaRipple, b.seaRipple, t),
                seaCool = Vector4.Lerp(a.seaCool, b.seaCool, t), seaLv = Vector4.Lerp(a.seaLv, b.seaLv, t), seaSceneRefl = Vector4.Lerp(a.seaSceneRefl, b.seaSceneRefl, t),
            };
        }
    }

    [Tooltip("D-239 오후 프리셋 — 맑은 늦가을 오후(TONE: 맑은 공기, 어둡거나 탁하지 않음). 하늘 6단·디더·해 원반은 노을과 같은 방식, 색만 다름")]
    public Palette afternoon = AfternoonDefaults();

    public static Palette AfternoonDefaults() => new Palette
    {
        sunAzimuth = 215f, skySunElevation = 26f, lightElevation = 30f, sunLight = new Color(1.00f, 0.92f, 0.78f), sunIntensity = 1.35f,
        skyHorizon = new Color(0.96f, 0.88f, 0.74f), skyAway = new Color(0.76f, 0.84f, 0.90f), skyMid = new Color(0.62f, 0.77f, 0.92f), skyTop = new Color(0.36f, 0.56f, 0.86f),
        sunDisc = new Color(1.00f, 0.98f, 0.90f), sunGlow = new Color(1.00f, 0.88f, 0.66f, 0.55f), cloudLit = new Color(0.99f, 0.96f, 0.90f), cloudDark = new Color(0.72f, 0.78f, 0.88f),
        skyBands = new Vector4(1f, 6f, 0.35f, 0f), sunHalo = new Vector4(1.6f, 4.0f, 8.5f, 0.30f), cloudStrips = new Vector4(1f, 0.25f, 0.45f, 1.2f),
        rim = new Vector4(1f, 0.22f, 0.55f, 0.5f), rimColor = new Color(1.00f, 0.92f, 0.74f),
        screenGlow = new Vector4(0.08f, 0.18f, 0.035f, 0.015f), screenGlowColor = new Color(1.00f, 0.95f, 0.82f),
        fog = new Color(0.80f, 0.86f, 0.92f), fogStartEnd = new Vector2(90f, 900f), shade = new Color(0.56f, 0.62f, 0.76f), a7Shade = new Color(0.40f, 0.48f, 0.60f),
        seaShallow = new Color(0.22f, 0.55f, 0.56f), seaDeep = new Color(0.06f, 0.16f, 0.30f), seaCoolCol = new Color(0.14f, 0.40f, 0.56f),
        seaGlow = new Color(0.80f, 0.88f, 0.92f), seaHot = new Color(0.95f, 0.96f, 0.92f), seaCore = new Color(1.00f, 1.00f, 0.96f),
        seaFoam = new Color(0.90f, 0.93f, 0.92f), seaCrest = new Color(0.86f, 0.93f, 0.95f), seaRipple = new Color(0.90f, 0.95f, 0.96f),
        seaCool = new Vector4(0.6f, 1.5f, 0f, 0f), seaLv = new Vector4(0.10f, 0.24f, 0.45f, 0.80f), seaSceneRefl = new Vector4(0.6f, 0.06f, 0f, 0f),
    };

    [Header("해 —보이는 해(하늘 원반·물 반사)와 빛 방향을 따로(빛까지 3°면 면이 다 꺼멓다)")]
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
    [Tooltip("D-236 8판 해 둘레 화면 빛무리(테 소재에서): x 1단 반지름 · y 2단 반지름(화면 높이 비) · z 1단 세기 · w 2단 세기. 0 이면 끔")]
    public Vector4 screenGlow = new Vector4(0.10f, 0.22f, 0.05f, 0.022f);
    public Color screenGlowColor = new Color(1.00f, 0.72f, 0.45f);
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
        RimId = Shader.PropertyToID("_PCDuskRim"), RimColId = Shader.PropertyToID("_PCDuskRimCol"), RimHzId = Shader.PropertyToID("_PCDuskRimHz"),
        SunScrId = Shader.PropertyToID("_PCDuskSunScr"), ScrGlowId = Shader.PropertyToID("_PCDuskScrGlow"), ScrGlowColId = Shader.PropertyToID("_PCDuskScrGlowCol");

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
        if (seaNear != null) { seaNear.enabled = false; if (seaNearHomeSet) seaNear.transform.position = seaNearHome; }
        PushSea(null, 0f);
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

    // ── D-239 시간대 묶음 ─────────────────────────────────────────────
    Palette sunsetP, curP;
    static readonly int SeaShallowId = Shader.PropertyToID("_ShallowCol"), SeaDeepId = Shader.PropertyToID("_DeepCol"), SeaCoolColId = Shader.PropertyToID("_CoolCol"),
        SeaGlowId = Shader.PropertyToID("_Glow"), SeaHotId = Shader.PropertyToID("_Hot"), SeaCoreId = Shader.PropertyToID("_Core"), SeaFoamId = Shader.PropertyToID("_Foam"),
        SeaCrestId = Shader.PropertyToID("_CrestCol"), SeaRippleId = Shader.PropertyToID("_RippleCol"), SeaCoolId = Shader.PropertyToID("_Cool"), SeaLvId = Shader.PropertyToID("_Lv"),
        SeaReflId = Shader.PropertyToID("_SceneRefl"), SeaDispId = Shader.PropertyToID("_Disp");
    MaterialPropertyBlock seaMpb;
    [Tooltip("D-239 촘촘한 바다 격자가 카메라를 따라갈 때의 걸음(m). 원점 = 처음 놓인 자리(낚시 자리) → 거기선 9판과 같은 격자")]
    public float seaNearStep = 4f;
    [SerializeField, HideInInspector] Vector3 seaNearHome; [SerializeField, HideInInspector] bool seaNearHomeSet;

    /// <summary>노을 프리셋 = 이 컴포넌트 맨 위 필드 + seaMaterial 의 바다 색(그대로 → dusk 1 이면 9판과 같은 값).</summary>
    Palette Sunset()
    {
        var p = sunsetP ??= new Palette();
        p.sunAzimuth = sunAzimuth; p.skySunElevation = skySunElevation; p.lightElevation = lightElevation; p.sunLight = sunLight; p.sunIntensity = sunIntensity;
        p.skyHorizon = skyHorizon; p.skyAway = skyAway; p.skyMid = skyMid; p.skyTop = skyTop; p.sunDisc = sunDisc; p.sunGlow = sunGlow; p.cloudLit = cloudLit; p.cloudDark = cloudDark;
        p.skyBands = skyBands; p.sunHalo = sunHalo; p.cloudStrips = cloudStrips; p.rim = rim; p.rimColor = rimColor; p.screenGlow = screenGlow; p.screenGlowColor = screenGlowColor;
        p.fog = fog; p.fogStartEnd = fogStartEnd; p.shade = shade; p.a7Shade = a7Shade;
        var m = seaMaterial;
        if (m != null)
        {
            p.seaShallow = m.GetColor(SeaShallowId); p.seaDeep = m.GetColor(SeaDeepId); p.seaCoolCol = m.GetColor(SeaCoolColId); p.seaGlow = m.GetColor(SeaGlowId);
            p.seaHot = m.GetColor(SeaHotId); p.seaCore = m.GetColor(SeaCoreId); p.seaFoam = m.GetColor(SeaFoamId); p.seaCrest = m.GetColor(SeaCrestId); p.seaRipple = m.GetColor(SeaRippleId);
            p.seaCool = m.GetVector(SeaCoolId); p.seaLv = m.GetVector(SeaLvId); p.seaSceneRefl = m.GetVector(SeaReflId);
        }
        return p;
    }

    /// <summary>지금 시간대 값(dusk 로 오후 ↔ 노을 보간).</summary>
    public Palette Now() { if (afternoon == null) afternoon = AfternoonDefaults(); return curP = Palette.Lerp(afternoon, Sunset(), Mathf.Clamp01(dusk)); }
    /// <summary>지금 해 방위(°) — 낚시 반짝임 등이 쓴다.</summary>
    public float SunAzimuthNow => (curP ?? Now()).sunAzimuth;
    bool DuskProps(float k) => k > 0f && dusk >= duskPropsFrom;

    /// <summary>amount 만큼 낮 → 이 Look(dusk 로 정한 시간대). 빛·안개·하늘·바다 한꺼번에.</summary>
    public void Apply()
    {
        if (!captured) { Shader.SetGlobalVector(DuskId, Vector4.zero); return; }
        float k = Mathf.Clamp01(amount);
        var P = Now();
        if (sun != null)
        {
            var rot = Quaternion.LookRotation(-Dir(P.sunAzimuth, P.lightElevation));
            sun.transform.rotation = Quaternion.Slerp(day.sunRot, rot, k);
            sun.color = Color.Lerp(day.sunColor, P.sunLight, k);
            sun.intensity = Mathf.Lerp(day.sunIntensity, P.sunIntensity, k);
        }
        RenderSettings.fogColor = Color.Lerp(day.fogColor, P.fog, k);
        RenderSettings.fogStartDistance = Mathf.Lerp(day.fogStart, P.fogStartEnd.x, k);
        RenderSettings.fogEndDistance = Mathf.Lerp(day.fogEnd, P.fogStartEnd.y, k);
        RenderSettings.ambientLight = Color.Lerp(day.ambLight, P.shade, k);
        RenderSettings.ambientSkyColor = Color.Lerp(day.ambSky, P.shade, k);
        if (k > 0f && skyMaterial != null) RenderSettings.skybox = skyMaterial; else if (day.skybox != null) RenderSettings.skybox = day.skybox;
        if (sea != null) sea.sharedMaterial = k > 0f && seaMaterial != null ? seaMaterial : day.seaMat;
        if (seaNear != null) seaNear.enabled = k > 0f && seaMaterial != null && sea != null;
        // 연출물 묶음은 Look 이 켜져 있으면 켬(앞쪽 부두 판자·양동이 포함), 수평선 실루엣·갈매기만 노을 쪽에서
        if (backdrop != null && backdrop.activeSelf != k > 0f) backdrop.SetActive(k > 0f);
        SetHorizon(DuskProps(k));
        SetHidden(k > 0f);
        PushGlobals();
    }

    /// <summary>바다 두 판(Sea·SeaNear)에 시간대 색 + 촘촘한 격자 중심을 렌더러별 블록으로(머티리얼 에셋은 안 바꿈).</summary>
    void PushSea(Palette P, float k)
    {
        if (sea == null || seaMaterial == null) return;
        seaMpb ??= new MaterialPropertyBlock();
        bool on = k > 0f;
        var disp = seaMaterial.GetVector(SeaDispId);
        if (on && seaNear != null)
        {
            if (!seaNearHomeSet) { seaNearHome = seaNear.transform.position; seaNearHomeSet = true; }
            var cm = Camera.main; var c = seaNearHome;
            if (cm != null && seaNearStep > 0f)
            {
                var d = cm.transform.position - seaNearHome;
                c.x += Mathf.Round(d.x / seaNearStep) * seaNearStep; c.z += Mathf.Round(d.z / seaNearStep) * seaNearStep;
            }
            if (seaNear.transform.position != c) seaNear.transform.position = c;
            disp = new Vector4(disp.x, c.x, c.z, disp.w);
        }
        foreach (var r in new[] { sea, seaNear })
        {
            if (r == null) continue;
            if (!on) { r.SetPropertyBlock(null); continue; }
            r.GetPropertyBlock(seaMpb);
            seaMpb.SetColor(SeaShallowId, P.seaShallow); seaMpb.SetColor(SeaDeepId, P.seaDeep); seaMpb.SetColor(SeaCoolColId, P.seaCoolCol);
            seaMpb.SetColor(SeaGlowId, P.seaGlow); seaMpb.SetColor(SeaHotId, P.seaHot); seaMpb.SetColor(SeaCoreId, P.seaCore);
            seaMpb.SetColor(SeaFoamId, P.seaFoam); seaMpb.SetColor(SeaCrestId, P.seaCrest); seaMpb.SetColor(SeaRippleId, P.seaRipple);
            seaMpb.SetVector(SeaCoolId, P.seaCool); seaMpb.SetVector(SeaLvId, P.seaLv); seaMpb.SetVector(SeaReflId, P.seaSceneRefl);
            seaMpb.SetVector(SeaDispId, disp);
            r.SetPropertyBlock(seaMpb);
        }
    }

    DuskBackdrop backdropC;
    void SetHorizon(bool on)
    {
        if (backdrop == null) return;
        if (backdropC == null) backdropC = backdrop.GetComponent<DuskBackdrop>();
        if (backdropC != null) backdropC.Horizon = on;
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
        var P = Now();
        Shader.SetGlobalVector(DuskId, new Vector4(k, 0, 0, 0));
        Shader.SetGlobalColor(FogId, P.fog.linear);
        Shader.SetGlobalColor(GlowId, new Color(P.sunGlow.linear.r, P.sunGlow.linear.g, P.sunGlow.linear.b, P.sunGlow.a));
        Shader.SetGlobalColor(AmbId, P.shade.linear);
        Shader.SetGlobalVector(FogPId, new Vector4(P.fogStartEnd.x, P.fogStartEnd.y, 0, 0));
        Shader.SetGlobalVector(SunDirId, Dir(P.sunAzimuth, P.skySunElevation));
        Shader.SetGlobalColor(HorId, P.skyHorizon.linear); Shader.SetGlobalColor(AwayId, P.skyAway.linear);
        Shader.SetGlobalColor(MidId, P.skyMid.linear); Shader.SetGlobalColor(TopId, P.skyTop.linear);
        Shader.SetGlobalColor(DiscId, P.sunDisc.linear); Shader.SetGlobalColor(CLitId, P.cloudLit.linear); Shader.SetGlobalColor(CDarkId, P.cloudDark.linear);
        Shader.SetGlobalVector(BandsId, P.skyBands); Shader.SetGlobalVector(HaloId, P.sunHalo); Shader.SetGlobalVector(StripsId, P.cloudStrips);
        Shader.SetGlobalVector(RimId, k > 0f ? P.rim : Vector4.zero); Shader.SetGlobalColor(RimColId, P.rimColor.linear);
        SetRimOverlay(k > 0f && P.rim.x > .5f);
        PushSea(P, k);
        var cm = Camera.main;   // 테는 수평선 위만(물속 어두운 단 + 빛길 밝은 단 경계에 테가 생기지 않게)
        if (cm != null)
        {
            var f = cm.transform.forward; f.y = 0f; float hz = f.sqrMagnitude > 1e-6f ? cm.WorldToViewportPoint(cm.transform.position + f.normalized * 2000f).y : 0f;
            var sv = cm.WorldToViewportPoint(cm.transform.position + Dir(P.sunAzimuth, P.skySunElevation) * 2000f);   // y = 해 화면 x(테는 해 쪽 가장자리에만)
            var lr = cm.GetComponent<LowResRenderTest>(); var rs = lr != null ? lr.RenderSize : Vector2Int.zero;   // zw = 실제 렌더 격자(RT 는 renderScale 로 더 클 수 있음)
            Shader.SetGlobalVector(RimHzId, new Vector4(hz - 0.004f, sv.z > 0f ? sv.x : 0.5f, rs.x > 0 ? rs.x : 0, rs.y > 0 ? rs.y : 0));
            Shader.SetGlobalVector(SunScrId, new Vector4(sv.x, sv.y, k > 0f && sv.z > 0f && P.screenGlow.z > 0f ? 1f : 0f, 0f));
            Shader.SetGlobalVector(ScrGlowId, P.screenGlow); Shader.SetGlobalColor(ScrGlowColId, P.screenGlowColor.linear);
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
            var c = Color.Lerp(baseShade, P.a7Shade, k); c.a = 1f; Shader.SetGlobalColor(A7ShadeId, c);
        }
    }
}
