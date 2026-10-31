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

    [Header("검증 봇 입력(마우스 대신)")]
    public bool simActive;

    public bool Seated { get; private set; }
    public bool Landed => Seated && castT >= castTime;
    public Vector3 BobberPosition => bobber != null ? bobber.position : Vector3.zero;

    Camera cam; float savedNear; float yaw, pitch, castT;
    Transform root, rod, bobber; LineRenderer line;
    Vector3 castTo;
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
        savedNear = cam.nearClipPlane; cam.nearClipPlane = 0.05f;
        Build();
        castTo = seat.position + Flat(seat.forward) * castDistance; castTo.y = waterY;
        if (!simActive) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        Place();
    }

    public void Stand()
    {
        Seated = false;
        if (cam != null) cam.nearClipPlane = savedNear;
        if (root != null) Destroy(root.gameObject);
        root = null; rod = null; bobber = null; line = null;
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
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = "Rod"; Destroy(g.GetComponent<Collider>());
            g.GetComponent<Renderer>().sharedMaterial = gray;
            var mesh = g.transform; mesh.localScale = new Vector3(0.022f, 1.0f, 0.022f);   // 원통 높이 2 → 2 m
            rod = new GameObject("RodPivot").transform; rod.SetParent(root, false);
            mesh.SetParent(rod, false); mesh.localPosition = new Vector3(0f, 0f, 1.0f); mesh.localRotation = Quaternion.Euler(90f, 0f, 0f);   // 피벗(손잡이)에서 +z 로 2 m
        }
        if (bobberPrefab != null) bobber = Instantiate(bobberPrefab, root).transform;
        else
        {
            // D-229 찌 = 아래 흰 띠 + 위 빨강(빛 안 받는 단색 — 노을 물 위에서도 12 m 앞 2~3 px 로 읽히게). 높이 0.22 m
            bobber = new GameObject("Bobber").transform; bobber.SetParent(root, false);
            Part(bobber, new Color(0.95f, 0.92f, 0.86f), 0.08f, 0.00f);
            Part(bobber, new Color(0.86f, 0.10f, 0.07f), 0.12f, 0.10f);
        }
        var lg = new GameObject("Line"); lg.transform.SetParent(root, false);
        line = lg.AddComponent<LineRenderer>();
        line.positionCount = pts.Length; line.useWorldSpace = true; line.numCapVertices = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
        var lm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "FishingLine (대역)" };
        lm.SetColor("_BaseColor", lineColor);
        line.sharedMaterial = lm;
    }

    [Tooltip("D-229 줄 색 — 노을빛(복숭아)에 섞이게. 굵기는 매 프레임 화면 1 px(거리×픽셀 크기)로 맞춘다.")]
    public Color lineColor = new Color(0.90f, 0.62f, 0.55f);

    void Part(Transform parent, Color c, float h, float y)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = "BobberPart"; Destroy(g.GetComponent<Collider>());
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Bobber (대역)" }; m.SetColor("_BaseColor", c);
        g.GetComponent<Renderer>().sharedMaterial = m; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        g.transform.SetParent(parent, false); g.transform.localScale = new Vector3(0.07f, h * 0.5f, 0.07f); g.transform.localPosition = new Vector3(0f, y + h * 0.5f - 0.06f, 0f);
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
        cam.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(e.x - pitch, e.y + yaw, 0f));
        Vector3 f = Flat(seat.forward), r = Vector3.Cross(Vector3.up, f);
        float t = Time.time;
        // 손: 눈 오른쪽 아래 — 대는 바다 쪽으로 30° 들어 2 m. 바람에 아주 조금 흔들림
        Vector3 hand = seat.position + r * 0.28f - Vector3.up * 0.42f + f * 0.30f;
        Quaternion rq = Quaternion.LookRotation(Quaternion.AngleAxis(-28f + Mathf.Sin(t * 0.9f) * 0.6f, r) * f, Vector3.up) * Quaternion.Euler(0f, Mathf.Sin(t * 0.6f) * 0.8f, 0f);
        if (castT < 0.35f) rq = Quaternion.AngleAxis(Mathf.Lerp(-45f, 25f, castT / 0.35f), r) * rq;   // 휙 — 뒤로 젖혔다 앞으로
        rod.SetPositionAndRotation(hand, rq);
        Vector3 tip = hand + rq * Vector3.forward * 2.0f;
        Vector3 b;
        float k = Mathf.Clamp01(castT / castTime);
        if (k < 1f)
        {
            b = Vector3.Lerp(tip, castTo, k) + Vector3.up * (castArc * 4f * k * (1f - k));   // 짧은 포물선
        }
        else
        {
            b = castTo + Vector3.up * (0.03f + 0.035f * Mathf.Sin(t * 1.6f) + 0.02f * Mathf.Sin(t * 2.7f + 1.1f));   // 물결 따라 위아래
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
        Vector3 eye = cam.transform.position;
        line.startWidth = 1.15f * PixelAt(Vector3.Distance(eye, tip)); line.endWidth = 1.15f * PixelAt(Vector3.Distance(eye, b));   // 1 px 딱 맞추면 기울기에 따라 픽셀 중심을 놓쳐 끊긴다 → 1.15
    }
}
