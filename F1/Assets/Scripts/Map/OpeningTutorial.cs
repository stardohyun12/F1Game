using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// D-142 오프닝 시네마틱(46초, 아무 키로 건너뛰기) + 첫 배달 튜토리얼(MART 계란 → 언덕 찻집 → 해안도로 → 정비소 정산).
/// 카메라는 Timeline 대신 코드 경로 — 빌더(BuildBayLoop.Tutorial)가 만든 Shot_* 빈 오브젝트 사이를 움직인다.
/// 기존 흐름(RunController·DeliveryManager·PackingScreen)을 그대로 쓰고, 시계만 끈다(RunController.NoClock).
/// 무드: Morning_Mist → 씬 원래 빛(오후)으로 보간. 월드 머티리얼(공유 에셋)을 건드리므로 끝·OnDisable 에서 원래 값으로 되돌린다.
/// </summary>
[DefaultExecutionOrder(500)]
public class OpeningTutorial : MonoBehaviour
{
    [SerializeField] MoodProfile morning;
    [Tooltip("첫 배달 기본 보상(₩) — 가설")]
    [SerializeField] int baseReward = 1200;
    [Tooltip("레이스 참가비(₩) — 가설")]
    [SerializeField] int raceFee = 30000;
    [Tooltip("끄면 시네마틱 없이 바로 튜토리얼")]
    [SerializeField] bool playCinematic = true;
    [Tooltip("짐 주는 사람 말풍선 이름 — BayLoop=MART 주인, TownV6=택배 보관소 직원(D-187)")]
    [SerializeField] string packerName = "MART 주인";

    public const float CinematicLength = 46f;

    // ── QA 가 읽는 상태 ──
    public string Stage { get; private set; } = "Init";
    public float CineTime { get; private set; }
    public readonly List<string> Log = new List<string>();
    public bool BubbleUphillShown { get; private set; }
    public float CoastSegmentSec { get; private set; } = -1f;

    RunController rc;
    DeliveryManager mgr;
    CargoBox cargo;
    Camera cam;
    FollowCamera follow;
    Transform car;
    Rigidbody carRb;
    readonly Dictionary<string, Transform> pts = new Dictionary<string, Transform>();
    DeliveryPoint tea;
    OrderJob job;
    float t0;
    bool skipRequested;
    List<Canvas> hudCanvases = new List<Canvas>();

    // ── UI ──
    Canvas ui;
    RectTransform barTop, barBottom, slip, bubble, settle;
    TextMeshProUGUI subtitle, hint, bubbleText, coastText, settleText, gaugeText;
    Image gaugeFill;
    Transform bubbleAnchor; float bubbleUntil;
    float coastUntil; bool subOn;

    void Awake()
    {
        var drv = FindFirstObjectByType<BayLoopDriveOnly>();
        if (drv != null) drv.enabled = false;   // 주문판 건너뛰기 임시 장치를 이 감독이 대신한다
    }

    IEnumerator Start()
    {
        yield return null;                       // RunController.Start(주문판 열기) 다음
        rc = Services.Run as RunController ?? FindFirstObjectByType<RunController>();
        mgr = Services.Delivery as DeliveryManager ?? FindFirstObjectByType<DeliveryManager>();
        cargo = Services.Cargo as CargoBox;
        cam = Camera.main; follow = cam != null ? cam.GetComponent<FollowCamera>() : null;
        car = Services.Car?.Transform; carRb = car != null ? car.GetComponent<Rigidbody>() : null;
        foreach (var t in GetComponentInParentOrRoot().GetComponentsInChildren<Transform>(true))
            if ((t.name.StartsWith("Shot_") || t.name.StartsWith("TUT_") || t.name.StartsWith("Audio_") || t.name == "ShutterRoll" || t.name == "ArmPivot" || t.name == "NPC_Head") && !pts.ContainsKey(Key(t))) pts[Key(t)] = t;
        tea = Pt("TUT_TeaDelivery")?.GetComponent<DeliveryPoint>();
        if (rc == null || mgr == null || car == null || cam == null || tea == null)
        {
            Debug.LogWarning($"[OpeningTutorial] 배선 부족 rc={rc != null} mgr={mgr != null} car={car != null} cam={cam != null} tea={tea != null} — 끈다");
            enabled = false; yield break;
        }
        var hud = FindFirstObjectByType<DeliveryHUD>();
        if (hud != null) foreach (var c in hud.GetComponentsInChildren<Canvas>(true)) hudCanvases.Add(c);
        if (hud != null && hud.GetComponentInParent<Canvas>() is Canvas pc && !hudCanvases.Contains(pc)) hudCanvases.Add(pc);
        HideNoArrowUI();
        // D-138: 배달 화살표(DeliveryHUD = 화면 가장자리 화살) 끔. 속도·화물 게이지는 같은 캔버스의 다른 HUD 라 그대로.
        if (hud != null) { hud.enabled = false; foreach (var img in hud.GetComponentsInChildren<Image>(true)) if (img.name == "Arrow") img.enabled = false; }
        BuildUI();
        rc.BeginNoClock();
        Services.Board?.Close();
        UrgentCallPanel.Ensure().Hide(); // 시작 주문판이 40% 로 띄운 긴급 전화 — 튜토리얼엔 없음
        Time.timeScale = 1f;
        if (follow != null) follow.enabled = false;
        rc.OnRunStarted += OnRunStarted;
        rc.OnRunEnded += OnRunEnded;
        mgr.OnDeliveryCompleted.AddListener(OnDelivered);
        if (cargo != null) { cargo.OnImpact += OnImpact; cargo.OnIntegrityChanged += OnIntegrity; }
        if (playCinematic && morning != null) yield return Cinematic();
        EndCinematic();
    }

    // 이름 → 자리. 같은 이름(ArmPivot·NPC_Head)은 부모 이름을 붙여 구분한다.
    static string Key(Transform t) => (t.name == "ArmPivot" || t.name == "NPC_Head") && t.parent != null ? t.parent.name + "/" + t.name : t.name;
    Transform Pt(string k) => pts.TryGetValue(k, out var t) ? t : null;
    Transform GetComponentInParentOrRoot() => transform.parent != null ? transform.parent : transform;

    void Mark(string s)
    {
        Stage = s;
        float integ = cargo != null ? cargo.IntegrityPercent : -1f;
        string line = $"[{Time.unscaledTime - t0:F1}s] {s} 화물={integ * 100f:F0}% 차=({car.position.x:F0},{car.position.y:F1},{car.position.z:F0})";
        Log.Add(line); Debug.Log("[OpeningTutorial] " + line);
    }

    // =====================================================================
    //  시네마틱
    // =====================================================================
    struct Seg { public float a, b; public string from, to; public float drift; }
    static readonly Seg[] Segs =
    {
        new Seg { a = 0f,  b = 4f,  from = "Shot_1a", to = "Shot_1a", drift = 0.35f },
        new Seg { a = 4f,  b = 7.5f, from = "Shot_1b", to = "Shot_1b", drift = 0.25f },
        new Seg { a = 7.5f, b = 11f, from = "Shot_1c", to = "Shot_1c", drift = 1.0f },
        new Seg { a = 11f, b = 17f, from = "Shot_2a", to = "Shot_2a", drift = 2.0f },
        new Seg { a = 17f, b = 23f, from = "Shot_2b", to = "Shot_2b", drift = 0.5f },
        new Seg { a = 23f, b = 28f, from = "Shot_3a", to = "Shot_3a", drift = 1.2f },
        new Seg { a = 28f, b = 34f, from = "Shot_3b", to = "Shot_3b", drift = 0.4f },
        new Seg { a = 34f, b = 41f, from = "Shot_4a", to = "Shot_4a", drift = 6f },
        new Seg { a = 41f, b = 46f, from = "Shot_4a", to = "@follow", drift = 0f },
    };

    // 무드 원본(씬 = 오후 쪽) — 보간의 끝값이자 되돌릴 값
    struct MatSnap { public Material m; public Color amb, fogC, tint; public float fs, fe; public bool hasTint; }
    readonly List<MatSnap> mats = new List<MatSnap>();
    Light sun; Quaternion sunRot; Color sunCol; float sunInt, sunShadow;
    AmbientMode ambMode; Color ambCol; float ambInt;
    bool fogOn; FogMode fogMode; Color fogCol; float fogS, fogE, fogD;
    Material sky; Volume vol; VolumeProfile post;
    bool moodDirty;

    void SnapMood()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
        if (sun != null) { sunRot = sun.transform.rotation; sunCol = sun.color; sunInt = sun.intensity; sunShadow = sun.shadowStrength; }
        ambMode = RenderSettings.ambientMode; ambCol = RenderSettings.ambientLight; ambInt = RenderSettings.ambientIntensity;
        fogOn = true; fogMode = RenderSettings.fogMode; fogCol = RenderSettings.fogColor;
        fogS = RenderSettings.fogStartDistance; fogE = RenderSettings.fogEndDistance; fogD = RenderSettings.fogDensity;
        sky = RenderSettings.skybox; vol = FindFirstObjectByType<Volume>(); post = vol != null ? vol.sharedProfile : null;
        int tintId = Shader.PropertyToID("_Tint");
        foreach (var m in MoodController.WorldMaterials())
            mats.Add(new MatSnap { m = m, amb = m.GetColor("_Ambient"), fogC = m.GetColor("_FogColor"), fs = m.GetFloat("_FogStart"), fe = m.GetFloat("_FogEnd"),
                hasTint = m.HasProperty(tintId), tint = m.HasProperty(tintId) ? m.GetColor(tintId) : Color.clear });
    }

    /// <summary>u=0 아침 안개, u=1 원래(오후).</summary>
    void BlendMood(float u)
    {
        moodDirty = true;
        var p = morning;
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Slerp(Quaternion.Euler(p.sunRotationEuler), sunRot, u);
            sun.color = Color.Lerp(p.sunColor, sunCol, u); sun.intensity = Mathf.Lerp(p.sunIntensity, sunInt, u);
            sun.shadowStrength = Mathf.Lerp(p.shadowStrength, sunShadow, u);
        }
        RenderSettings.ambientMode = ambMode;
        RenderSettings.ambientLight = Color.Lerp(p.ambientColor, ambCol, u);
        RenderSettings.fog = true; RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = Color.Lerp(p.fogColor, fogCol, u);
        float ms = p.syncWorld ? p.fogStart : fogS * 0.35f, me = p.syncWorld ? p.fogEnd : fogE * 0.35f;
        RenderSettings.fogStartDistance = Mathf.Lerp(ms, fogS, u); RenderSettings.fogEndDistance = Mathf.Lerp(me, fogE, u);
        RenderSettings.skybox = u < 0.5f && p.skybox != null ? p.skybox : sky;
        if (vol != null) vol.sharedProfile = u < 0.5f && p.postProfile != null ? p.postProfile : post;
        var mAmb = p.fogColor * p.worldAmbientScale; mAmb.a = 1f;
        foreach (var s in mats)
        {
            if (s.m == null) continue;
            s.m.SetColor("_Ambient", Color.Lerp(mAmb, s.amb, u)); s.m.SetColor("_FogColor", Color.Lerp(p.fogColor, s.fogC, u));
            s.m.SetFloat("_FogStart", Mathf.Lerp(ms, s.fs, u)); s.m.SetFloat("_FogEnd", Mathf.Lerp(me, s.fe, u));
            if (s.hasTint && s.m.name == "Sea" && p.seaTint.a > 0f) s.m.SetColor("_Tint", Color.Lerp(p.seaTint, s.tint, u));
            if (s.hasTint && s.m.name == "BoundaryRail" && p.railTint.a > 0f) s.m.SetColor("_Tint", Color.Lerp(p.railTint, s.tint, u));
        }
    }

    void RestoreMood()
    {
        if (!moodDirty) return;
        moodDirty = false;
        if (sun != null) { sun.transform.rotation = sunRot; sun.color = sunCol; sun.intensity = sunInt; sun.shadowStrength = sunShadow; }
        RenderSettings.ambientMode = ambMode; RenderSettings.ambientLight = ambCol; RenderSettings.ambientIntensity = ambInt;
        RenderSettings.fog = fogOn; RenderSettings.fogMode = fogMode; RenderSettings.fogColor = fogCol;
        RenderSettings.fogStartDistance = fogS; RenderSettings.fogEndDistance = fogE; RenderSettings.fogDensity = fogD;
        RenderSettings.skybox = sky; if (vol != null) vol.sharedProfile = post;
        foreach (var s in mats)
        {
            if (s.m == null) continue;
            s.m.SetColor("_Ambient", s.amb); s.m.SetColor("_FogColor", s.fogC); s.m.SetFloat("_FogStart", s.fs); s.m.SetFloat("_FogEnd", s.fe);
            // _Tint 는 BlendMood 가 바꾼 Sea·BoundaryRail 만 되돌린다 — 전부 다시 쓰면 공유 .mat 에 소수점 잡음(0.47→0.4699998)이 남는다
            if (s.hasTint && (s.m.name == "Sea" || s.m.name == "BoundaryRail")) s.m.SetColor("_Tint", s.tint);
        }
    }

    void OnDisable() { RestoreMood(); }

    /// <summary>QA·버튼용 건너뛰기.</summary>
    public void Skip() => skipRequested = true;

    IEnumerator Cinematic()
    {
        t0 = Time.unscaledTime;
        Mark("Cinematic");
        SnapMood();
        SetHud(false); SetBars(true);
        hint.gameObject.SetActive(true);
        var shutter = Pt("ShutterRoll"); var startScale = shutter != null ? shutter.localScale : Vector3.one;
        int lastScene = 0;
        Play("Audio_Radio"); Play("Audio_Gulls");
        while (true)
        {
            float t = Time.unscaledTime - t0; CineTime = t;
            if (t > 0.5f && (skipRequested || AnyKey())) { Mark("CinematicSkipped@" + t.ToString("F1")); break; }
            if (t >= CinematicLength) break;
            int scene = t < 11f ? 1 : t < 23f ? 2 : t < 34f ? 3 : 4;
            if (scene != lastScene)
            {
                lastScene = scene;
                Mark("Scene" + scene);
                if (scene == 2) { PlaceCar("TUT_GarageCarSpot"); Play("Audio_Tools"); }
                if (scene == 3) Play("Audio_ShopBell");
                if (scene == 4) { PlaceCar("TUT_CarStart"); Play("Audio_EngineStart"); }
            }
            // 안개: 18초부터 걷히기 시작해 44초에 원래 빛
            BlendMood(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(18f, 44f, t)));
            // 셔터 12~16초 말려 올라감
            if (shutter != null) shutter.localScale = new Vector3(startScale.x, Mathf.Lerp(startScale.y, startScale.y * 0.04f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(12f, 16f, t))), startScale.z);
            // 손짓: 정비소 사람 17~23, MART 주인 23~28
            Wave("TUT_Mechanic/ArmPivot", t, 17.5f, 22.5f, -60f, 18f);
            Wave("TUT_MartOwner/ArmPivot", t, 23f, 28.5f, -115f, 28f);
            // 자막 3개
            subOn = false;
            Sub(t, 5f, 10.5f, "라디오: …선두, 해안 직선으로 들어섭니다!");
            Sub(t, 17.5f, 22.5f, "정비소: 또 그걸로 배달 가?");
            Sub(t, 24f, 28.5f, packerName + ": 첫 주문이야. 계란 한 판, 언덕 위 찻집!");
            if (subtitle.transform.parent.gameObject.activeSelf != subOn) subtitle.transform.parent.gameObject.SetActive(subOn);
            slip.gameObject.SetActive(t >= 28.3f && t < 34f);
            CamPose(t);
            yield return null;
        }
        if (shutter != null) shutter.localScale = new Vector3(startScale.x, startScale.y * 0.04f, startScale.z);
        foreach (var k in new[] { "TUT_Mechanic/ArmPivot", "TUT_MartOwner/ArmPivot" }) { var a = Pt(k); if (a != null) a.localRotation = Quaternion.identity; }
        PlaceCar("TUT_CarStart");
        CineTime = Time.unscaledTime - t0;
    }

    void EndCinematic()
    {
        if (t0 == 0f) t0 = Time.unscaledTime;
        RestoreMood();
        SetBars(false); hint.gameObject.SetActive(false); slip.gameObject.SetActive(false); subtitle.transform.parent.gameObject.SetActive(false);
        SetHud(true);
        Mark("CinematicEnd len=" + CineTime.ToString("F1"));
        rc.EnterFreeDrive();
        Stop("Audio_Radio"); Stop("Audio_Gulls");
        StartCoroutine(Tutorial());
    }

    void CamPose(float t)
    {
        foreach (var s in Segs)
        {
            if (t < s.a || t >= s.b) continue;
            var a = Pt(s.from); if (a == null) return;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(s.a, s.b, t));
            if (s.to == "@follow")
            {
                Vector3 f = car.forward; f.y = 0; f.Normalize();
                var pos = car.position - f * 7f + Vector3.up * 2.6f;
                var rot = Quaternion.LookRotation(car.position + f * 6f + Vector3.up * 0.6f - pos);
                cam.transform.SetPositionAndRotation(Vector3.Lerp(a.position + a.forward * 6f, pos, u), Quaternion.Slerp(a.rotation, rot, u));
            }
            else cam.transform.SetPositionAndRotation(a.position + a.forward * (s.drift * u), a.rotation);
            cam.fieldOfView = s.from.StartsWith("Shot_1") ? 55f : 50f;
            return;
        }
    }

    void Wave(string k, float t, float a, float b, float baseDeg, float amp)
    {
        var p = Pt(k); if (p == null) return;
        if (t < a || t > b) { p.localRotation = Quaternion.identity; return; }
        float w = Mathf.Sin((t - a) * 7f);
        p.localRotation = Quaternion.Euler(baseDeg + amp * w, 0f, 0f);
    }

    void Sub(float t, float a, float b, string text)
    {
        if (t < a || t >= b) return;
        if (subtitle.text != text) subtitle.text = text;
        subOn = true;
    }

    void PlaceCar(string k)
    {
        var p = Pt(k); if (p == null) return;
        Vector3 pos = p.position;
        if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(car))
            pos.y = hit.point.y + 0.55f;
        if (carRb != null) { carRb.linearVelocity = Vector3.zero; carRb.angularVelocity = Vector3.zero; carRb.position = pos; carRb.rotation = p.rotation; }
        car.SetPositionAndRotation(pos, p.rotation);
        Physics.SyncTransforms();
    }

    void Play(string k) { var a = Pt(k)?.GetComponent<AudioSource>(); if (a != null && a.clip != null) a.Play(); }
    void Stop(string k) { var a = Pt(k)?.GetComponent<AudioSource>(); if (a != null) a.Stop(); }

    static bool AnyKey()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) return true;
        var g = Gamepad.current;
        return g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame);
    }

    // =====================================================================
    //  튜토리얼
    // =====================================================================
    bool delivered, runEnded, settleClosed;
    float lastInteg = 1f;
    IEnumerator Tutorial()
    {
        Mark("T0_ToMart");
        // 과제 0131: 주문을 받기 전(MART 로 가는 길)엔 진행 배너를 숨긴다 — 씬 기본 배달지(예 "1/4 골목 끝 집")가 보이던 버그.
        var progress = FindFirstObjectByType<DeliveryProgressHUD>(FindObjectsInactive.Include);
        if (progress != null) progress.gameObject.SetActive(false);
        var owner = Pt("TUT_MartOwner"); var trig = Pt("TUT_PackTrigger");
        bool called = false;
        while (Flat(car.position - trig.position) > 4.5f)
        {
            if (!called && owner != null && Flat(car.position - owner.position) < 40f) { called = true; Bubble(owner, "여기야, 여기!", 3f); }
            yield return null;
        }
        // 1 포장 — MART 주인 말풍선 1줄
        foreach (var dp in FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None)) if (dp != tea) dp.gameObject.SetActive(false);
        mgr.SetPoints(new List<DeliveryPoint> { tea }, Pt("TUT_ReturnGarage"));
        job = ScriptableObject.CreateInstance<OrderJob>();
        job.name = "TUT_FirstEggs"; job.displayName = "계란 한 판 → 언덕 위 찻집"; job.cargoType = CargoType.Eggs;
        job.destinationLabels = new List<string> { "TeaHouse" }; job.timeLimitSec = 0f; job.baseReward = baseReward; job.timeBonus = 0; job.callerName = "언덕 위 찻집";
        rc.EnterPacking();
        Mark("T1_Packing");
        ShowTopLine(packerName + ": 계란이야. 빈 데 없이 꼭꼭 싸 줘.", 6f);
        Services.Packing.Show(job);
        while (!rc.RunActive) yield return null;
        if (progress != null) progress.gameObject.SetActive(true);
        // 2 출발 — 화살표 없음. 찻집 간판·표지판만.
        Mark("T2_Depart");
        float martY = trig.position.y;
        while (!delivered && car.position.y < martY + 3f) yield return null;
        // 3 오르막 상점가 — 첫 큰 흔들림에 말풍선 1번(OnImpact/OnIntegrity 가 띄운다)
        if (!delivered) Mark("T3_Uphill");
        while (!delivered) yield return null;
        // 4 도착 — 화물 상태별 한 줄(OnDelivered 가 띄움). 5 귀환 — 시계 없음, 해안 직선 구간 기록.
        Mark("T5_Return");
        var gs = Pt("TUT_CoastGateStart"); var ge = Pt("TUT_CoastGateEnd");
        float segStart = -1f;
        while (!runEnded)
        {
            if (gs != null && ge != null)
            {
                if (segStart < 0f && Flat(car.position - gs.position) < 9f) { segStart = Time.time; Mark("T5_CoastStraightIn"); }
                if (segStart >= 0f && CoastSegmentSec < 0f && Flat(car.position - ge.position) < 9f)
                {
                    CoastSegmentSec = Time.time - segStart;
                    coastText.text = $"해안 직선  {CoastSegmentSec:F1}초"; coastText.gameObject.SetActive(true); coastUntil = Time.unscaledTime + 5f;
                    Mark($"T5_CoastStraightOut {CoastSegmentSec:F2}s");
                }
            }
            yield return null;
        }
        // 6 정산 — 정비소 앞. 결과판 대신 저금통 게이지 카드.
        yield return null;
        Services.Result?.Hide();
        Services.Overview?.GoToFollow(); if (follow != null) follow.enabled = false;
        var shot = Pt("Shot_2a"); if (shot != null) { cam.transform.SetPositionAndRotation(shot.position, shot.rotation); cam.fieldOfView = 50f; }
        SetHud(false);
        int wallet = Services.Upgrade != null ? Services.Upgrade.Wallet : 0;
        int left = Mathf.Max(0, raceFee - wallet);
        settleText.text = $"<b>첫 배달 정산</b>\n\n배달비  ₩{rc.LastReward:N0}\n포장비  −₩{rc.LastPackCost:N0}\n이번 수입  <b>₩{rc.LastNet:N0}</b>\n\n저금통  ₩{wallet:N0}";
        gaugeFill.fillAmount = raceFee > 0 ? Mathf.Clamp01((float)wallet / raceFee) : 1f;
        gaugeText.text = left > 0 ? $"레이스 참가비까지 ₩{left:N0} 남음" : "레이스 참가비 모였다!";
        settle.gameObject.SetActive(true);
        Mark($"T6_Settle 보상={rc.LastReward} 포장비={rc.LastPackCost} 순={rc.LastNet} 지갑={wallet} 남은참가비={left} 화물={rc.LastIntegrityPercent * 100f:F0}% 해안직선={CoastSegmentSec:F1}s");
        float shownAt = Time.unscaledTime;
        while (!settleClosed && !(Time.unscaledTime - shownAt > 0.8f && AnyKey())) yield return null;
        settle.gameObject.SetActive(false);
        SetHud(true);
        rc.EnterFreeDrive();   // 튜토리얼 끝 — 자유 주행(다음 주문 배선은 다음 과제)
        Mark("Done");
    }

    /// <summary>QA 용: 정산 카드 닫기.</summary>
    public void CloseSettle() => settleClosed = true;

    void OnRunStarted(RunController r) { lastInteg = cargo != null ? cargo.IntegrityPercent : 1f; }

    void OnImpact(float kmh) { if (Stage == "T3_Uphill" || Stage == "T2_Depart") UphillBubble("impact " + kmh.ToString("F0") + "km/h"); }
    void OnIntegrity(float v)
    {
        if ((Stage == "T3_Uphill" || Stage == "T2_Depart") && lastInteg - v >= 0.02f) UphillBubble($"integrity {lastInteg * 100f:F0}→{v * 100f:F0}%");
        lastInteg = v;
    }

    void Update()
    {
        if (Stage != "Done") { var uc = UrgentCallPanel.Ensure(); if (uc.IsVisible) uc.Hide(); }
        // 큰 흔들림 = 충격 또는 손상 외에 세로·가로 G 0.55 이상(오르막 턱)
        if (cargo != null && Stage == "T3_Uphill" && !BubbleUphillShown && (cargo.CurrentLongG > 0.55f || cargo.CurrentLatG > 0.55f))
            UphillBubble($"G long={cargo.CurrentLongG:F2} lat={cargo.CurrentLatG:F2}");
        if (bubble != null)
        {
            bool on = bubbleAnchor != null && Time.unscaledTime < bubbleUntil;
            if (on)
            {
                var sp = cam.WorldToScreenPoint(bubbleAnchor.position + Vector3.up * 2.6f);
                on = sp.z > 0f;
                if (on) bubble.position = new Vector3(sp.x, sp.y, 0f);
            }
            if (bubble.gameObject.activeSelf != on) bubble.gameObject.SetActive(on);
        }
        if (coastText != null && coastText.gameObject.activeSelf && Time.unscaledTime > coastUntil) coastText.gameObject.SetActive(false);
        if (topLine != null && topLine.transform.parent.gameObject.activeSelf && Time.unscaledTime > topLineUntil) topLine.transform.parent.gameObject.SetActive(false);
        AnimateTeaNpc();
    }

    void UphillBubble(string why)
    {
        if (BubbleUphillShown || delivered) return;
        BubbleUphillShown = true;
        Bubble(car, "살살! 계란이 흔들려.", 3f);
        Mark("T3_UphillBubble " + why);
    }

    float teaAnimUntil; bool teaHappy;
    void OnDelivered(DeliveryPoint p)
    {
        if (p != tea || delivered) return;
        delivered = true;
        float integ = cargo != null ? cargo.IntegrityPercent : 1f;
        teaHappy = integ >= 0.8f; teaAnimUntil = Time.unscaledTime + 3f;
        var npc = Pt("TUT_TeaHouse_NPC");
        Bubble(npc != null ? npc : tea.transform, teaHappy ? "어머, 하나도 안 깨졌네! 고마워요." : "아이고… 몇 개 깨졌네. 다음엔 살살 와요.", 4.5f);
        Mark($"T4_Arrive 화물={integ * 100f:F0}% → {(teaHappy ? "기쁨" : "아쉬움")}");
    }

    Vector3 teaNpcBase; bool teaBaseSet;
    void AnimateTeaNpc()
    {
        var npc = Pt("TUT_TeaHouse_NPC"); if (npc == null) return;
        if (!teaBaseSet) { teaNpcBase = npc.position; teaBaseSet = true; }
        if (Time.unscaledTime >= teaAnimUntil) { npc.position = teaNpcBase; npc.rotation = Quaternion.identity; return; }
        float k = teaAnimUntil - Time.unscaledTime;
        if (teaHappy) npc.position = teaNpcBase + Vector3.up * Mathf.Abs(Mathf.Sin(k * 9f)) * 0.25f;   // 깡충
        else npc.rotation = Quaternion.Euler(0f, Mathf.Sin(k * 10f) * 18f, 0f);                        // 고개 젓기(몸째)
    }

    void OnRunEnded(RunController r) { runEnded = true; }

    void OnDestroy()
    {
        if (rc != null) { rc.OnRunStarted -= OnRunStarted; rc.OnRunEnded -= OnRunEnded; }
        if (mgr != null) mgr.OnDeliveryCompleted.RemoveListener(OnDelivered);
        if (cargo != null) { cargo.OnImpact -= OnImpact; cargo.OnIntegrityChanged -= OnIntegrity; }
        RestoreMood();
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

    void SetHud(bool on) { foreach (var c in hudCanvases) if (c != null) c.enabled = on; }

    /// <summary>D-138: 화살표·경로선·미니맵 없음.</summary>
    static void HideNoArrowUI()
    {
        foreach (var m in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (m == null) continue;
            string n = m.GetType().Name;
            if (n.Contains("DirectionArrow") || n.Contains("MiniMap") || n.Contains("Minimap") || n.Contains("RouteLine")) m.gameObject.SetActive(false);
        }
    }

    // =====================================================================
    //  UI (런타임 생성 — 아트는 나중에 스프라이트 슬롯에)
    // =====================================================================
    TextMeshProUGUI topLine; float topLineUntil;
    TMP_FontAsset font;

    void BuildUI()
    {
        var anyTmp = FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        font = anyTmp != null && anyTmp.font != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
        var go = new GameObject("OpeningTutorialUI", typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        ui = go.GetComponent<Canvas>(); ui.renderMode = RenderMode.ScreenSpaceOverlay; ui.sortingOrder = 100;
        var sc = go.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        var root = (RectTransform)go.transform;

        barTop = Panel(root, "LetterboxTop", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -120), Vector2.zero, Color.black);
        barBottom = Panel(root, "LetterboxBottom", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 120), Color.black);

        var subBg = Panel(root, "SubtitleBg", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-620, 140), new Vector2(620, 210), new Color(0, 0, 0, 0.45f));
        subtitle = Text(subBg, "Subtitle", 38, TextAlignmentOptions.Center, Color.white); subBg.gameObject.SetActive(false);

        var tl = Panel(root, "TopLineBg", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-560, -150), new Vector2(560, -90), new Color(0, 0, 0, 0.5f));
        topLine = Text(tl, "TopLine", 32, TextAlignmentOptions.Center, Color.white); tl.gameObject.SetActive(false);

        var h = Panel(root, "SkipHint", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-470, 30), new Vector2(-30, 80), Color.clear);
        hint = Text(h, "Hint", 24, TextAlignmentOptions.Right, new Color(1, 1, 1, 0.6f)); hint.text = "아무 키나 누르면 건너뛰기"; hint.gameObject.SetActive(false);

        slip = Panel(root, "OrderSlip", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180, -160), new Vector2(620, 200), new Color(0.98f, 0.96f, 0.88f, 0.97f));
        var sl = Text(slip, "SlipText", 34, TextAlignmentOptions.TopLeft, new Color(0.15f, 0.12f, 0.1f));
        sl.text = "<size=26>MART 주문 전표</size>\n\n계란 한 판\n→ 언덕 위 찻집\n\n<size=24>깨지면 돈을 못 받아요</size>"; sl.margin = new Vector4(28, 24, 28, 24);
        Slot(slip, "ArtSlot_OrderSlip");
        slip.gameObject.SetActive(false);

        bubble = Panel(root, "Bubble", new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.95f));
        bubble.sizeDelta = new Vector2(560, 84); bubble.pivot = new Vector2(0.5f, 0f);
        bubbleText = Text(bubble, "BubbleText", 30, TextAlignmentOptions.Center, new Color(0.1f, 0.1f, 0.1f));
        bubble.gameObject.SetActive(false);

        var ct = Panel(root, "CoastRecord", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-260, 36), new Vector2(260, 86), Color.clear);
        coastText = Text(ct, "CoastText", 30, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.8f)); coastText.gameObject.SetActive(false);

        settle = Panel(root, "SettleCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160, -300), new Vector2(820, 300), new Color(0.08f, 0.09f, 0.11f, 0.92f));
        settleText = Text(settle, "SettleText", 34, TextAlignmentOptions.TopLeft, Color.white); settleText.margin = new Vector4(40, 30, 240, 120);
        var piggy = Panel(settle, "ArtSlot_PiggyBankImage", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-230, -230), new Vector2(-30, -30), new Color(0.92f, 0.62f, 0.66f));
        var gBg = Panel(settle, "GaugeBg", new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 70), new Vector2(-40, 100), new Color(1, 1, 1, 0.15f));
        var gf = Panel(gBg, "GaugeFill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.98f, 0.75f, 0.25f));
        gaugeFill = gf.GetComponent<Image>(); gaugeFill.sprite = WhiteSprite(); gaugeFill.type = Image.Type.Filled; gaugeFill.fillMethod = Image.FillMethod.Horizontal;
        var gtR = Panel(settle, "GaugeLabel", new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 20), new Vector2(-40, 64), Color.clear);
        gaugeText = Text(gtR, "GaugeText", 26, TextAlignmentOptions.Left, new Color(1, 1, 1, 0.85f));
        var foot = Panel(settle, "Foot", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-420, -290), new Vector2(-30, -250), Color.clear);
        var ft = Text(foot, "FootText", 22, TextAlignmentOptions.Right, new Color(1, 1, 1, 0.5f)); ft.text = "아무 키나 누르면 계속";
        settle.gameObject.SetActive(false);
        SetBars(false);
    }

    static Sprite white;
    static Sprite WhiteSprite()
    {
        if (white == null) white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return white;
    }

    RectTransform Panel(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color c)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(Image));
        var r = (RectTransform)g.transform; r.SetParent(parent, false);
        r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = oMin; r.offsetMax = oMax;
        var img = g.GetComponent<Image>(); img.color = c; img.raycastTarget = false;
        return r;
    }

    /// <summary>스프라이트 슬롯 — 아트가 오면 이 Image 에 꽂는다(지금은 투명).</summary>
    void Slot(RectTransform parent, string name) { Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear); }

    TextMeshProUGUI Text(RectTransform parent, string name, float size, TextAlignmentOptions al, Color c)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var r = (RectTransform)g.transform; r.SetParent(parent, false);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(16, 6); r.offsetMax = new Vector2(-16, -6);
        var t = g.GetComponent<TextMeshProUGUI>(); t.font = font; t.fontSize = size; t.alignment = al; t.color = c; t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    void SetBars(bool on) { if (barTop != null) { barTop.gameObject.SetActive(on); barBottom.gameObject.SetActive(on); } }

    void Bubble(Transform anchor, string text, float sec)
    {
        bubbleAnchor = anchor; bubbleText.text = text; bubbleUntil = Time.unscaledTime + sec;
    }

    void ShowTopLine(string text, float sec)
    {
        topLine.text = text; topLine.transform.parent.gameObject.SetActive(true); topLineUntil = Time.unscaledTime + sec;
    }
}
