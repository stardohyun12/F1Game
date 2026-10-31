using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// D-227 노을 낚시 자리(회색 대역). 걷다가 자리 반경 안에서 E → 앉기(감독 OnFootDirector 가 부름) → 낚싯대 던지기(짧은 포물선) →
/// 고정 시점(seat = Cam_Sunset)에서 마우스로 조금만 둘러보기(좌우 ±yawRange, 위아래 -pitchDown~+pitchUp). 다시 E → 일어나기.
///  · 낚싯대 = 가는 원통 2 m(손 위치 고정 — 둘러봐도 대는 그 자리) · 줄 = LineRenderer 늘어진 곡선 · 찌 = 작은 구, 물결 따라 위아래.
///  · 아트는 나중에(Codex) — rodPrefab/bobberPrefab 슬롯에 넣으면 원통·구 대신 그걸 쓴다.
/// </summary>
public class FishingSpot : MonoBehaviour
{
    [Tooltip("앉은 눈(≈ 바닥 + 1.0 m)과 기본 방향 — Cam_Sunset.")]
    public Transform seat;
    [Tooltip("이 반경(m, 수평) 안에 발이 들어오면 E 앉기.")]
    public float sitRadius = 2.4f;
    [Tooltip("일어나면 설 자리(없으면 seat 뒤 1.1 m 땅).")]
    public Transform standPoint;
    [Tooltip("둘러보기: 오른쪽(+) yawRange°, 왼쪽(−) yawLeft°. D-229 왼쪽은 마을이라 좁게.")]
    public float yawRange = 35f, pitchUp = 15f, pitchDown = 22f;
    public float yawLeft = 35f;
    [Header("D-236 7판 구도 — 앉은 동안만(걷기는 그대로)")]
    [Tooltip("앉은 시야각(°). 좁히면 해·먼 실루엣이 커지고 엽서처럼 눌린다. 0 = 걷기 시야각 그대로.")]
    public float seatedFov = 46f;
    [Tooltip("seat 기본 각 위에 더 숙이는 각(°) — 바다 면적↑, 수평선이 화면 위 1/3 쪽으로.")]
    public float seatLookDown = 5f;
    [Tooltip("던지는 방향을 seat 정면에서 오른쪽(+)으로 돌리는 각(°) — 찌를 해 빛길 위에 놓는다.")]
    public float castYaw = 0f;
    [Tooltip("마우스 1 픽셀당 도(걷기 0.12 보다 느리게 — 앉아서 천천히 둘러봄).")]
    public float mouseSensitivity = 0.07f;
    [Header("던지기")]
    public float castDistance = 12f;
    public float castTime = 1.1f;
    public float castArc = 3.5f;
    public float waterY = 0f;
    [Header("아트 슬롯(비면 회색 대역)")]
    public GameObject rodPrefab;
    public GameObject bobberPrefab;
    [Header("D-236 8판 대(대역) — 손잡이는 화면 밖 오른쪽 아래, 끝은 해 오른쪽 아래 물 위(해를 안 가리게)")]
    [Tooltip("손(대 뿌리) 자리 — 눈 기준 (오른쪽, 위, 앞) m. 화면 밖이어야 한다.")]
    public Vector3 rodHand = new Vector3(0.38f, -0.62f, 0.35f);
    [Tooltip("대 끝 자리 — 눈 기준 (오른쪽, 위, 앞) m. 길이는 손~끝 거리.")]
    public Vector3 rodTip = new Vector3(1.07f, -0.24f, 2.36f);
    [Tooltip("뿌리·끝 반지름(m) — 끝으로 갈수록 가늘다(화면에서 뿌리 쪽 ~10px → 끝 1px).")]
    public Vector2 rodRadius = new Vector2(0.013f, 0.0025f);
    public Color rodColor = new Color(0.26f, 0.16f, 0.10f);
    [Tooltip("대 끝이 바람에 흔들리는 폭(m)과 찌가 잠길 때 끝이 따라 숙는 비율.")]
    public float rodSway = 0.012f, rodFollow = 0.35f;
    [Header("D-236 8판 찌 반짝임 — 찌가 해 빛길 안(해 방위 ±pathHalfWidth°)이면 찌 둘레에 1px 별")]
    public float pathHalfWidth = 7f;
    public Color glintColor = new Color(1.00f, 0.96f, 0.78f);
    [Header("D-235 물 — 노을 바다(DuskSea)일 때 셰이더와 같은 파에 뜨고 파문을 낸다")]
    [Tooltip("착수 때 한 번(줄·찌가 물에 닿는 작은 물소리, CC0).")]
    public AudioClip splashClip;
    [Range(0f, 1f)] public float splashVolume = 0.55f;
    [Tooltip("찌가 수면 기울기를 따라 기우는 비율(1 = 수면과 똑같이).")]
    public float tiltFollow = 0.7f;
    [Tooltip("착수 때 찌가 잠겼다 튀는 속도(m/s)와 출렁임 용수철(강도, 감쇠비).")]
    public float plunge = 0.55f;
    public Vector2 bobSpring = new Vector2(60f, 0.22f);
    [Tooltip("물결에 출렁일 때 작은 파문 — 찌가 골에서 다시 오르기 시작할 때, 최소 간격(초)·세기.")]
    public float bobRippleGap = 1.6f;
    public float bobRippleStrength = 0.35f;

    [Header("검증 봇 입력(마우스 대신)")]
    public bool simActive;

    public bool Seated { get; private set; }
    public bool Landed => Seated && castT >= castTime;
    public Vector3 BobberPosition => bobber != null ? bobber.position : Vector3.zero;
    /// <summary>D-235 검산용 — 이번 프레임 찌 자리의 셰이더 수면 높이(노을 바다가 아니면 NaN).</summary>
    public float SurfaceY { get; private set; } = float.NaN;

    Camera cam; float savedNear, savedFov; float yaw, pitch, castT;
    Transform root, rod, bobber, glint; LineRenderer line;
    public bool BobberInPath { get; private set; }   // 검증용
    Vector3 castTo;
    bool landed; float bobOff, bobVel, lastSurf, lastSurfVel, lastRippleT;
    AudioSource splash;
    static readonly Vector3[] pts = new Vector3[20];

    Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward; }

    public bool CanSit(Vector3 feet)
    {
        if (Seated || seat == null) return false;
        Vector3 d = feet - seat.position; d.y = 0f;
        return d.magnitude < sitRadius;
    }

    public Vector3 StandPoint()
    {
        if (standPoint != null) return standPoint.position;
        Vector3 p = seat.position - Flat(seat.forward) * 1.1f;
        if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) p = hit.point + Vector3.up * 0.05f;
        return p;
    }

    public float SeatYaw => seat != null ? seat.eulerAngles.y : 0f;

    public void Sit(Camera c)
    {
        cam = c; Seated = true; yaw = 0f; pitch = 0f; castT = 0f;
        landed = false; bobOff = bobVel = 0f; lastSurfVel = 0f; lastSurf = float.NaN; SeaWaves.ClearRipples();
        savedNear = cam.nearClipPlane; cam.nearClipPlane = 0.05f;
        savedFov = cam.fieldOfView; if (seatedFov > 1f) cam.fieldOfView = seatedFov;
        Build();
        castTo = seat.position + (Quaternion.Euler(0f, castYaw, 0f) * Flat(seat.forward)) * castDistance; castTo.y = waterY;
        if (!simActive) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        Place();
    }

    public void Stand()
    {
        Seated = false;
        if (cam != null) { cam.nearClipPlane = savedNear; if (savedFov > 1f) cam.fieldOfView = savedFov; }
        if (root != null) Destroy(root.gameObject);
        root = null; rod = null; bobber = null; glint = null; line = null; splash = null;
        SeaWaves.ClearRipples(); SurfaceY = float.NaN;
    }

    /// <summary>노을 바다(DuskSea 격자)가 켜져 있으면 그 머티리얼과 바다 판 y. 아니면 false(낮 바다 = 예전 흉내 출렁임).</summary>
    bool DuskSea(out Material m, out float seaY)
    {
        m = null; seaY = waterY;
        var look = SunsetLook.Find();
        if (look == null || !look.IsOn || look.seaNear == null || !look.seaNear.enabled) return false;
        m = look.seaNear.sharedMaterial; seaY = look.seaNear.transform.position.y;
        return m != null && m.HasProperty("_W0");
    }

    public void AddLook(float dYaw, float dPitch) { yaw = Mathf.Clamp(yaw + dYaw, -yawLeft, yawRange); pitch = Mathf.Clamp(pitch + dPitch, -pitchDown, pitchUp); }

    void Build()
    {
        root = new GameObject("FishingRig").transform; root.SetParent(transform, false);
        var gray = new Material(Shader.Find("PitCrew/LofiPilot/Surface")) { name = "FishingGray (대역)" };
        gray.SetColor("_Tint", new Color(0.62f, 0.60f, 0.57f)); gray.SetColor("_Ambient", new Color(0.70f, 0.72f, 0.76f));
        gray.SetColor("_FogColor", RenderSettings.fogColor); gray.SetFloat("_FogStart", 200f); gray.SetFloat("_FogEnd", 900f); gray.SetFloat("_FaceShade", 0.5f);
        if (rodPrefab != null) rod = Instantiate(rodPrefab, root).transform;
        else
        {
            // D-236 8판: 짙은 나무색, 끝으로 가늘어지는 6각 대(저폴리 대역). 피벗 = 손, +z 로 길이 1(매 프레임 z 배율 = 실제 길이)
            var wood = new Material(gray) { name = "FishingRod (대역)" };
            wood.SetColor("_Tint", rodColor); wood.SetFloat("_FaceShade", 0.35f);
            rod = new GameObject("RodPivot").transform; rod.SetParent(root, false);
            var g = new GameObject("Rod"); g.transform.SetParent(rod, false);
            g.AddComponent<MeshFilter>().sharedMesh = TaperMesh(rodRadius.x, rodRadius.y);
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = wood; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        if (bobberPrefab != null) bobber = Instantiate(bobberPrefab, root).transform;
        else
        {
            // D-229 찌 = 아래 흰 띠 + 위 빨강(빛 안 받는 단색). 8판: 지름 0.07→0.11, 빨강을 한 단 어둡게 — 금빛 빛길 위에서 12 m 앞 4 px 짙은 점으로 읽히게
            bobber = new GameObject("Bobber").transform; bobber.SetParent(root, false);
            Part(bobber, new Color(0.95f, 0.92f, 0.86f), 0.08f, 0.00f, 0.11f);
            Part(bobber, new Color(0.62f, 0.08f, 0.06f), 0.13f, 0.10f, 0.11f);
        }
        // D-236 8판 찌 반짝임: 화면 1px 굵기 십자(카메라를 보는 판 두 장). 빛길 안일 때만 깜빡
        glint = new GameObject("Glint").transform; glint.SetParent(root, false);
        var gm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Glint (대역)" }; gm.SetColor("_BaseColor", glintColor);
        for (int i = 0; i < 2; i++)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "GlintBar"; Destroy(q.GetComponent<Collider>());
            var qr = q.GetComponent<Renderer>(); qr.sharedMaterial = gm; qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            q.transform.SetParent(glint, false); q.transform.localScale = i == 0 ? new Vector3(5f, 1f, 1f) : new Vector3(1f, 5f, 1f);   // 단위 = 1 px(glint 배율로 맞춤)
        }
        glint.gameObject.SetActive(false);
        var lg = new GameObject("Line"); lg.transform.SetParent(root, false);
        line = lg.AddComponent<LineRenderer>();
        line.positionCount = pts.Length; line.useWorldSpace = true; line.numCapVertices = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
        var lm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "FishingLine (대역)" };
        lm.SetColor("_BaseColor", lineColor);
        line.sharedMaterial = lm;
        splash = root.gameObject.AddComponent<AudioSource>();
        splash.playOnAwake = false; splash.spatialBlend = 0.85f; splash.minDistance = 4f; splash.maxDistance = 40f; splash.rolloffMode = AudioRolloffMode.Linear;
    }

    [Tooltip("D-229 줄 색 — 노을빛(복숭아)에 섞이게. 굵기는 매 프레임 화면 1 px(거리×픽셀 크기)로 맞춘다.")]
    public Color lineColor = new Color(0.95f, 0.74f, 0.60f);   // 8판: 해 앞 역광 1px 선(옛 0.90,0.62,0.55 는 빛길에 묻혔고 1,0.84,0.66 은 해보다 눈을 끌었다)

    void Part(Transform parent, Color c, float h, float y, float dia)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = "BobberPart"; Destroy(g.GetComponent<Collider>());
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Bobber (대역)" }; m.SetColor("_BaseColor", c);
        g.GetComponent<Renderer>().sharedMaterial = m; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        g.transform.SetParent(parent, false); g.transform.localScale = new Vector3(dia, h * 0.5f, dia); g.transform.localPosition = new Vector3(0f, y + h * 0.5f - 0.06f, 0f);
    }

    // 6각 대: z 0(뿌리, 반지름 r0) → 1(끝, r1). 끝은 막음
    static Mesh TaperMesh(float r0, float r1)
    {
        const int n = 6; var v = new Vector3[n * 2 + 1]; var t = new int[n * 6 + n * 3];
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            v[i] = d * r0; v[n + i] = d * r1 + Vector3.forward;
        }
        v[n * 2] = Vector3.forward;
        int k = 0;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            t[k++] = i; t[k++] = j; t[k++] = n + i; t[k++] = j; t[k++] = n + j; t[k++] = n + i;   // 바깥에서 시계 방향
            t[k++] = n + i; t[k++] = n + j; t[k++] = n * 2;
        }
        var m = new Mesh { name = "RodTaper" }; m.vertices = v; m.triangles = t; m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // 지금 렌더 해상도(360 줄)에서 거리 dist(m)의 1 px 이 몇 m 인가
    float PixelAt(float dist)
    {
        int h = 360; var lr = cam.GetComponent<LowResRenderTest>();
        if (lr != null && lr.isActiveAndEnabled && lr.RenderSize.y > 0) h = lr.RenderSize.y; else if (cam.pixelHeight > 0) h = cam.pixelHeight;
        return 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / h;
    }

    void LateUpdate()
    {
        if (!Seated || cam == null) return;
        if (!simActive && Mouse.current != null)
        {
            if (Cursor.lockState == CursorLockMode.Locked) { Vector2 d = Mouse.current.delta.ReadValue() * mouseSensitivity; AddLook(d.x, -d.y); }
            if (Mouse.current.leftButton.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
        castT += Time.deltaTime;
        Place();
    }

    void Place()
    {
        var e = seat.eulerAngles;
        cam.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(e.x + seatLookDown - pitch, e.y + yaw, 0f));
        Vector3 f = Flat(seat.forward), r = Vector3.Cross(Vector3.up, f);
        float t = Time.time;
        // D-236 8판 손: 화면 밖 오른쪽 아래(rodHand) → 끝(rodTip)은 해 오른쪽 아래 물 위. 끝이 바람에 아주 조금 흔들리고, 찌가 잠기면 따라 숙는다
        Vector3 eye = seat.position;
        Vector3 hand = eye + r * rodHand.x + Vector3.up * rodHand.y + f * rodHand.z;
        Vector3 aim = eye + r * rodTip.x + Vector3.up * rodTip.y + f * rodTip.z;
        aim += r * (rodSway * (Mathf.Sin(t * 0.63f) + 0.4f * Mathf.Sin(t * 1.7f + 0.8f))) + Vector3.up * (rodSway * 0.7f * Mathf.Sin(t * 0.91f + 2f) + Mathf.Min(0f, bobOff) * rodFollow);
        Quaternion rq = Quaternion.LookRotation(aim - hand, Vector3.up);
        if (castT < 0.35f) rq = Quaternion.AngleAxis(Mathf.Lerp(-45f, 25f, castT / 0.35f), r) * rq;   // 휙 — 뒤로 젖혔다 앞으로
        float rodLen = Vector3.Distance(hand, aim);
        rod.SetPositionAndRotation(hand, rq);
        if (rodPrefab == null) rod.localScale = new Vector3(1f, 1f, rodLen);
        Vector3 tip = hand + rq * Vector3.forward * rodLen;
        Vector3 b;
        float k = Mathf.Clamp01(castT / castTime);
        if (k < 1f)
        {
            b = Vector3.Lerp(tip, castTo, k) + Vector3.up * (castArc * 4f * k * (1f - k));   // 짧은 포물선
        }
        else if (DuskSea(out var sm, out float seaY))
        {
            // D-235 셰이더와 같은 Gerstner 파를 탐: 찌 = 착수 자리의 물 알갱이(옆·위로 같이 돎) + 착수 출렁임 용수철. 수면 기울기만큼 기움
            b = SeaWaves.Particle(sm, seaY, castTo.x, castTo.z, out Vector3 n);
            SurfaceY = b.y;
            float dt = Time.deltaTime;
            if (!landed)
            {
                landed = true; bobOff = 0f; bobVel = -plunge; lastRippleT = Time.time;
                SeaWaves.AddRipple(b, 1f);
                if (splash != null && splashClip != null) { splash.transform.position = b; splash.PlayOneShot(splashClip, splashVolume); }
            }
            else if (dt > 0f)
            {
                // 1/120 s 로 쪼개서 — 프레임이 한 번 길게 튀면(0.2 s+) 용수철이 발산했다(첫 실측 +40 cm)
                float w = Mathf.Sqrt(bobSpring.x); int steps = Mathf.Min(40, Mathf.CeilToInt(dt * 120f)); float h = dt / steps;
                for (int s = 0; s < steps; s++) { bobVel += (-bobSpring.x * bobOff - 2f * bobSpring.y * w * bobVel) * h; bobOff += bobVel * h; }
            }
            // 출렁 파문: 수면이 내려가다 다시 오르기 시작하는 순간(B 는 12 fps 박자로 계단처럼 바뀌니 바뀐 프레임끼리만 비교)
            if (!float.IsNaN(lastSurf) && Mathf.Abs(b.y - lastSurf) > 1e-5f)
            {
                float v = b.y - lastSurf;
                if (lastSurfVel < 0f && v > 0f && Time.time - lastRippleT > bobRippleGap) { SeaWaves.AddRipple(b, bobRippleStrength); lastRippleT = Time.time; }
                lastSurfVel = v;
            }
            lastSurf = b.y;
            b.y += bobOff;
            bobber.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, n, tiltFollow));
        }
        else
        {
            b = castTo + Vector3.up * (0.03f + 0.035f * Mathf.Sin(t * 1.6f) + 0.02f * Mathf.Sin(t * 2.7f + 1.1f));   // 물결 따라 위아래(낮 바다 — 셰이더 파 없음)
            b += f * (0.05f * Mathf.Sin(t * 0.4f));
        }
        bobber.position = b;
        // 줄: 날아가는 동안은 거의 곧게, 내려앉으면 가운데가 늘어짐. 처짐은 대 끝~찌 높이차의 1/5 까지만 — 넘으면 찌 앞에서 줄이 물 아래로 꺾였다 올라온다(갈고리)
        float sag = k < 1f ? 0.05f : Mathf.Min(Mathf.Max(0f, tip.y - b.y) * 0.2f, Vector3.Distance(tip, b) * 0.05f);
        for (int i = 0; i < pts.Length; i++)
        {
            float u = i / (pts.Length - 1f);
            pts[i] = Vector3.Lerp(tip, b, u) + Vector3.down * (4f * u * (1f - u) * sag);
        }
        line.SetPositions(pts);
        Vector3 ce = cam.transform.position;
        line.startWidth = 1.15f * PixelAt(Vector3.Distance(ce, tip)); line.endWidth = 1.15f * PixelAt(Vector3.Distance(ce, b));   // 1 px 딱 맞추면 기울기에 따라 픽셀 중심을 놓쳐 끊긴다 → 1.15
        Glint(b, ce, k >= 1f, t);
    }

    // D-236 8판: 찌가 해 빛길 안이면 찌 둘레(수면) 몇 자리에서 돌아가며 1px 십자가 반짝(0.22 s 켬 · 0.5 s 쉼). 화면 픽셀 크기로 맞춤
    void Glint(Vector3 b, Vector3 ce, bool landedNow, float t)
    {
        if (glint == null) return;
        var look = SunsetLook.Find();
        bool on = false;
        if (landedNow && look != null && look.IsOn)
        {
            Vector3 d = b - ce; d.y = 0f;
            float az = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            BobberInPath = Mathf.Abs(Mathf.DeltaAngle(az, look.sunAzimuth)) < pathHalfWidth;
            const float period = 0.72f;
            int slot = Mathf.FloorToInt(t / period);
            on = BobberInPath && (t - slot * period) < 0.22f;
            if (on)
            {
                float px = PixelAt(Vector3.Distance(ce, b));
                float h = (slot * 0.618034f) % 1f;   // 자리 돌림(찌 왼·오른쪽 0.2~0.45 m, 조금 앞뒤)
                Vector3 side = Vector3.Cross(Vector3.up, (b - ce).normalized).normalized;
                Vector3 p = new Vector3(b.x, b.y - bobOff, b.z) + side * ((h < 0.5f ? -1f : 1f) * (0.2f + 0.5f * (h % 0.5f))) + Vector3.up * 0.02f;
                glint.SetPositionAndRotation(p, Quaternion.LookRotation(p - ce, cam.transform.up));
                float s = (slot % 3 == 0 ? 1f : 0.6f) * px;   // 세 번에 한 번 큰 별(5px), 나머지 3px
                glint.localScale = new Vector3(s, s, s);
            }
        }
        else BobberInPath = false;
        if (glint.gameObject.activeSelf != on) glint.gameObject.SetActive(on);
    }
}
