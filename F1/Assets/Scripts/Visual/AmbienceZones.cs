using UnityEngine;

/// <summary>
/// D-248 ③ 장소별 환경음. 도현 "바람소리가 집 안에서 들리는데 장소에 따라 다르게 들려야 하지 않을까".
/// 듣는 자리(AudioListener)에서 0.1초마다 레이를 쏴서 네 가지를 잰다 — 손으로 구역 상자를 놓지 않는다(새 건물에도 그대로 먹게).
///   지붕: 머리 40 m 위에서 아래로 한 줄 → 머리 위에 뭔가 있으면 지붕.
///   벽:   귀 높이에서 8방향 → wallReach 안에 막힌 방향 수(실내 판정). 문·창이 열린 방향은 빠져서 덜 먹먹해진다.
///   골목: alleyReach 안에 막힌 방향 수. 트인 곳: openReach 까지 안 막힌 방향 수.
///   바다: 시작할 때 지도를 seaGridStep 격자로 내려다봐 바다 칸을 찾고, 칸마다 가장 가까운 바다 칸을 미리 구해 둔다.
/// 결과: 바람(WindAmbience)·파도·갈매기 각각의 크기(dB)와 로우패스(Hz). 안↔밖은 blendTime 동안 부드럽게 바뀐다.
/// 이 컴포넌트가 없는 씬(BayLoop·Proto)은 예전 소리 그대로다.
/// </summary>
[DefaultExecutionOrder(50)]
public class AmbienceZones : MonoBehaviour
{
    public WindAmbience wind;
    public CoastAmbience coast;
    public float seaLevel = 0f;

    [Header("실내 — 지붕 + 사방 벽")]
    public float indoorDb = -15f;
    public float indoorCutoff = 800f;
    [Tooltip("이 거리 안에 벽이 있는 방향이 몇 개인지로 '방 안'을 잰다(m).")]
    public float wallReach = 9f;   // 정비소 가운데서 벽까지 5~7.7 m(측정) → 6 이면 3/8 만 잡혀 '바깥'이 됐다

    [Header("바람 — 높이·골목·트인 곳")]
    [Tooltip("바다 높이에서 이만큼 위(m)면 바람 최대.")]
    public float windLowY = 2f, windHighY = 40f;
    public float lowDb = -6f;
    public float alleyReach = 14f, alleyDb = -4f;
    public float openReach = 30f, openDb = 2f;

    [Header("파도 — 바다와의 거리")]
    public float waveVolume = 0.6f;
    [Tooltip("이 거리 안이면 파도 최대, 그 뒤로 거리에 반비례(6 dB/두 배), waveFarDist 에서 0.")]
    public float waveNearDist = 6f, waveFarDist = 220f;
    public float seaGridStep = 5f;

    [Header("카트 — 속도만큼 바람 더")]
    public float speedWindDb = 3f;

    [Tooltip("안↔밖으로 바뀌는 데 걸리는 시간(s).")]
    public float blendTime = 0.7f;
    public float probeInterval = 0.1f;

    // ── 지금 값(디버그 창·검증 봇이 읽는다) ──
    public float Roof { get; private set; }        // 0/1 원값
    public float Walls { get; private set; }       // wallReach 안 막힌 방향 비율
    public float Alley { get; private set; }       // 부드럽게 한 골목 정도 0~1
    public float Open { get; private set; }
    public float Indoor { get; private set; }      // 부드럽게 한 실내 정도 0~1
    public float SeaDist { get; private set; }
    public float Speed01 { get; private set; }
    public float WindDb { get; private set; }
    public float WaveDb { get; private set; }
    public float GullDb { get; private set; }
    public float CutoffHz { get; private set; } = 22000f;
    public string Label { get; private set; } = "";
    public static AmbienceZones Active { get; private set; }

    AudioSource windSrc, waveSrc, gullSrc;
    AudioLowPassFilter windLp, waveLp, gullLp;
    float gullBaseCutoff = 22000f, gullBaseVol = 1f;
    Transform listener;
    ArcadeCarController car;
    float nextProbe, rawIndoor, rawAlley, rawOpen;
    readonly RaycastHit[] hits = new RaycastHit[16];

    // 바다 격자: 칸마다 가장 가까운 바다 칸(없으면 -1)
    int gw, gh; Vector2 gmin; int[] nearest;

    void OnEnable() { Active = this; }
    void OnDisable() { if (Active == this) Active = null; }

    void Start()
    {
        if (wind != null)
        {
            windSrc = wind.GetComponent<AudioSource>();
            if (windSrc != null) windSrc.spatialBlend = 0f;   // 바람은 어디에나 — 낚시 자리 한 점에서 멀어진다고 작아지지 않게
            windLp = Lp(wind.gameObject);
        }
        if (coast != null)
        {
            waveSrc = coast.waves; gullSrc = coast.gulls;
            if (waveSrc != null)
            {
                // 거리 감쇠는 여기서 직접(바다 거리) — 소스는 가장 가까운 바다로 옮겨 방향만 맡긴다
                waveSrc.rolloffMode = AudioRolloffMode.Custom;
                waveSrc.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 1f, 1f));
                waveSrc.maxDistance = 1000f; waveSrc.dopplerLevel = 0f;
                waveLp = Lp(waveSrc.gameObject);
            }
            if (gullSrc != null)
            {
                gullLp = gullSrc.GetComponent<AudioLowPassFilter>();
                if (gullLp != null) gullBaseCutoff = gullLp.cutoffFrequency; else gullLp = Lp(gullSrc.gameObject);
                gullBaseVol = gullSrc.volume;
            }
        }
        BuildSeaGrid();
    }

    static AudioLowPassFilter Lp(GameObject go)
    {
        if (!go.TryGetComponent(out AudioLowPassFilter f)) { f = go.AddComponent<AudioLowPassFilter>(); f.cutoffFrequency = 22000f; f.lowpassResonanceQ = 1f; }
        return f;
    }

    void BuildSeaGrid()
    {
        float half = 400f; gmin = new Vector2(-half, -half);
        var t = FindFirstObjectByType<Terrain>();
        var terrainGo = GameObject.Find("TownV6_Terrain");
        if (terrainGo != null && terrainGo.TryGetComponent(out Collider tc)) { var b = tc.bounds; gmin = new Vector2(b.min.x, b.min.z); half = Mathf.Max(b.size.x, b.size.z) * 0.5f; }
        else if (t != null) { gmin = new Vector2(t.transform.position.x, t.transform.position.z); half = Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) * 0.5f; }
        gw = gh = Mathf.CeilToInt(half * 2f / seaGridStep) + 1;
        var sea = new bool[gw * gh];
        for (int z = 0; z < gh; z++)
            for (int x = 0; x < gw; x++)
            {
                var p = new Vector3(gmin.x + x * seaGridStep, seaLevel + 200f, gmin.y + z * seaGridStep);
                sea[z * gw + x] = !Physics.Raycast(p, Vector3.down, out var h, 400f, ~0, QueryTriggerInteraction.Ignore) || h.point.y < seaLevel + 0.1f;
            }
        // 가장 가까운 바다 칸: 앞→뒤, 뒤→앞 두 번 훑어 이웃의 '가장 가까운 칸'을 물려받는다(거리 변환)
        nearest = new int[gw * gh];
        for (int i = 0; i < nearest.Length; i++) nearest[i] = sea[i] ? i : -1;
        for (int pass = 0; pass < 2; pass++)
        {
            int s = pass == 0 ? 1 : -1;
            for (int zi = 0; zi < gh; zi++)
            {
                int z = pass == 0 ? zi : gh - 1 - zi;
                for (int xi = 0; xi < gw; xi++)
                {
                    int x = pass == 0 ? xi : gw - 1 - xi, i = z * gw + x;
                    Take(i, x - s, z); Take(i, x, z - s); Take(i, x - s, z - s); Take(i, x + s, z - s);
                }
            }
        }
    }

    void Take(int i, int nx, int nz)
    {
        if (nx < 0 || nz < 0 || nx >= gw || nz >= gh) return;
        int c = nearest[nz * gw + nx]; if (c < 0) return;
        if (nearest[i] < 0 || D2(i, c) < D2(i, nearest[i])) nearest[i] = c;
    }
    int D2(int a, int b) { int dx = a % gw - b % gw, dz = a / gw - b / gw; return dx * dx + dz * dz; }

    /// <summary>그 자리에서 가장 가까운 바다 점과 거리(m). 바다가 없는 지도면 거리 = 무한.</summary>
    public float NearestSea(Vector3 at, out Vector3 point)
    {
        point = at;
        if (nearest == null) return float.PositiveInfinity;
        int x = Mathf.Clamp(Mathf.RoundToInt((at.x - gmin.x) / seaGridStep), 0, gw - 1), z = Mathf.Clamp(Mathf.RoundToInt((at.z - gmin.y) / seaGridStep), 0, gh - 1);
        int c = nearest[z * gw + x]; if (c < 0) return float.PositiveInfinity;
        point = new Vector3(gmin.x + (c % gw) * seaGridStep, seaLevel, gmin.y + (c / gw) * seaGridStep);
        return Mathf.Max(0f, Vector2.Distance(new Vector2(at.x, at.z), new Vector2(point.x, point.z)) - seaGridStep * 0.5f);
    }

    void Update()
    {
        if (listener == null || !listener.gameObject.activeInHierarchy)
        {
            var l = FindFirstObjectByType<AudioListener>(); if (l == null) return; listener = l.transform;
        }
        Vector3 ear = listener.position;
        if (Time.unscaledTime >= nextProbe) { nextProbe = Time.unscaledTime + probeInterval; Probe(ear); }

        // 부드럽게: blendTime 동안 0→1 (dB 와 Hz 가 아니라 판정값을 섞어서 둘이 함께 움직인다)
        float k = Time.unscaledDeltaTime / Mathf.Max(0.05f, blendTime);
        Indoor = Mathf.MoveTowards(Indoor, rawIndoor, k);
        Alley = Mathf.MoveTowards(Alley, rawAlley, k);
        Open = Mathf.MoveTowards(Open, rawOpen, k);

        // 카트: 런 주행 중(엔진이 울리는 때)만 속도 바람
        float sp = 0f;
        if (!EngineAudio.OnFootMute && Services.Run != null && Services.Run.RunActive)
        {
            if (car == null) car = FindFirstObjectByType<ArcadeCarController>();
            if (car != null) sp = Mathf.Clamp01(Mathf.Abs(car.CurrentSpeedMps) / Mathf.Max(0.1f, car.MaxSpeedMps));
        }
        Speed01 = Mathf.MoveTowards(Speed01, sp, Time.unscaledDeltaTime * 2f);

        float h = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(seaLevel + windLowY, seaLevel + windHighY, ear.y));
        float outdoor = 1f - Indoor;
        WindDb = lowDb * (1f - h) + (alleyDb * Alley + openDb * Open + speedWindDb * Speed01) * outdoor + indoorDb * Indoor;

        SeaDist = NearestSea(ear, out var seaPt);
        float wg = 0f;
        if (!float.IsInfinity(SeaDist) && SeaDist < waveFarDist)
            wg = Mathf.Clamp01(waveNearDist / Mathf.Max(SeaDist, waveNearDist)) * Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(waveFarDist * 0.6f, waveFarDist, SeaDist));
        WaveDb = (wg > 0.0001f ? 20f * Mathf.Log10(wg) : -80f) + indoorDb * Indoor;
        GullDb = indoorDb * Indoor;
        // 로그 눈금으로: 실내 0.5 ≈ 4.2 kHz, 1.0 = 800 Hz
        CutoffHz = Mathf.Exp(Mathf.Lerp(Mathf.Log(22000f), Mathf.Log(indoorCutoff), Indoor));

        if (wind != null) wind.zoneGain = Db(WindDb);
        if (windLp != null) windLp.cutoffFrequency = CutoffHz;
        if (waveSrc != null)
        {
            waveSrc.volume = waveVolume * wg * Db(indoorDb * Indoor);
            seaPt.y = seaLevel + 0.3f;
            waveSrc.transform.position = Vector3.MoveTowards(waveSrc.transform.position, seaPt, 40f * Time.unscaledDeltaTime);
            if (waveLp != null) waveLp.cutoffFrequency = CutoffHz;
        }
        if (gullSrc != null)
        {
            gullSrc.volume = gullBaseVol * Db(GullDb);
            if (gullLp != null) gullLp.cutoffFrequency = Mathf.Min(gullBaseCutoff, CutoffHz);
        }
        Label = Indoor > 0.5f ? "실내" : SeaDist < 15f ? "바닷가" : Alley > 0.5f ? "골목" : Open > 0.6f ? "트인 곳" : "바깥";
    }

    static float Db(float db) => Mathf.Pow(10f, db / 20f);

    void Probe(Vector3 ear)
    {
        bool bf = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;   // 벽·지붕이 한쪽 면만 있는 메시라도 안쪽에서 맞게(이 줄들 동안만)
        float roofTop = 40f;
        Roof = Cast(ear + Vector3.up * roofTop, Vector3.down, roofTop - 0.3f, out _, out _) ? 1f : 0f;
        int wall = 0, alley = 0, open = 0;
        for (int i = 0; i < 8; i++)
        {
            var d = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
            // 골목·벽은 선 면만 센다 — 비탈·길(윗면, 법선이 위)은 골목이 아니다(언덕 길이 골목으로 잡혀 바람이 줄던 것)
            if (Cast(ear, d, alleyReach, out float dist, out bool ground) && !ground) { if (dist <= wallReach) wall++; alley++; }
            // 트임은 8° 위로 — 완만한 언덕 꼭대기에서 수평선이 바로 앞 비탈에 걸려 '막힘'이 되지 않게
            if (!Cast(ear, Quaternion.Euler(-8f, i * 45f, 0f) * Vector3.forward, openReach, out _, out _)) open++;
        }
        Physics.queriesHitBackfaces = bf;
        Walls = wall / 8f;
        // 지붕 + 6/8 방향 넘게 막힘 = 방 안. 문 하나 열림(7/8) ≈ 0.6, 반쯤 트인 차고 ≈ 0.2
        rawIndoor = Roof * Mathf.InverseLerp(0.5f, 1f, Walls);
        rawAlley = Mathf.InverseLerp(0.25f, 0.75f, alley / 8f);
        rawOpen = Mathf.InverseLerp(0.5f, 1f, open / 8f);
    }

    /// <summary>가장 가까운 '벽' — 사람(CharacterController)·카트(움직이는 Rigidbody)·트리거는 뺀다.</summary>
    bool Cast(Vector3 from, Vector3 dir, float len, out float dist, out bool ground)
    {
        dist = len; ground = false;
        int n = Physics.RaycastNonAlloc(from, dir, hits, len, ~0, QueryTriggerInteraction.Ignore);
        bool any = false;
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c is CharacterController) continue;
            var rb = hits[i].rigidbody; if (rb != null && !rb.isKinematic) continue;
            if (hits[i].distance < dist) { dist = hits[i].distance; any = true; ground = Mathf.Abs(hits[i].normal.y) > 0.6f; }
        }
        return any;
    }
}
