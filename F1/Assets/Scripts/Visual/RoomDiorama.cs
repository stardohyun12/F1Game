using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 과제 20261104-1559(D-298, SPEC §1 「실내는 디오라마」): 플레이어가 2층 방(ROOM_2F) 상자 안에 있으면
/// 카메라가 플레이어 뒤·위로 빠져 방 전체를 상자처럼 내려다본다(기준 화면 world/taste/interior-candidates/07).
/// - 걷기·E 상호작용은 1인칭 그대로: OnFootCamera 가 놓은 눈 자세를 저장해 두고 Gaze 가 그 눈에서 레이를 쏜다(EyeRay). 조준점(dot)은 그 레이가 맞은 곳의 화면 위치로 옮긴다.
/// - 전환은 포장 2D(Stage_Packing.BoardSec)와 같은 0.4초. 문을 지나 상자를 나가면 1인칭으로 돌아온다.
/// - 그리는 동안(저해상 카메라에만): A7 셰이더 전역 _A7Dio 를 켜 방 상자 밖·카메라 쪽 벽·천장을 버린다. 배경 검정, 방과 안 겹치는 렌더러·지형은 끈다(forceRenderingOff — enabled 는 안 건드림).
///   빛 = 창 한 곳 + 책상 전등 한 곳(실시간 점광원, 실행 중에만 만든다). 방에 닿는 다른 점·스팟 빛과 전역광은 낮춘다. 나오면 전부 되돌린다.
/// 좌표는 손으로 적지 않는다 — 방 상자(A7ShadeGlobals.TryRoomBox)·창 유리(GLASS_*)·책상(DESK_*) 이름에서 매번 잡는다.
/// </summary>
[DefaultExecutionOrder(100)]   // OnFootCamera(0) 가 눈 자세를 놓은 뒤에 덮어쓴다
public sealed class RoomDiorama : MonoBehaviour
{
    [Header("카메라 각도(도현 판정용 가설값 — 과제: 피치 45~55°, 거리 4~5 m)")]
    public float pitch = 50f;
    public float distance = 4.5f;
    [Tooltip("바라보는 점 = 플레이어 가슴 ↔ 방 바닥 가운데 섞기(0 = 플레이어, 1 = 방 가운데)")]
    [Range(0f, 1f)] public float roomPull = 0.5f;
    [Tooltip("1인칭 ↔ 디오라마 전환 시간(초) — Stage_Packing.BoardSec 와 같은 값")]
    public float blendSec = 0.4f;
    [Tooltip("방에 들어올 때 눈이 수평이면 이 각도로 앞 바닥을 본다(조준점이 방 물건에 닿게)")]
    public float aimPitch = 30f;
    [Tooltip("카메라가 고개 돌림(Yaw)을 따라가는 시간(초)")]
    public float yawSmooth = 0.25f;

    [Header("빛")]
    public Color ambient = new Color(0.10f, 0.11f, 0.14f);
    public Color windowColor = new Color(0.62f, 0.74f, 1.00f);
    public float windowIntensity = 3.2f, windowRange = 4.5f;
    public Color lampColor = new Color(1.00f, 0.78f, 0.48f);
    public float lampIntensity = 2.6f, lampRange = 2.2f;
    [Tooltip("카메라 쪽 벽을 버리는 문턱(상자 좌표, 벽 안쪽 면 ≈ 0.97)")]
    public float wallCut = 0.95f;
    [Tooltip("천장을 버리는 문턱(상자 좌표)")]
    public float ceilCut = 0.9f;

    /// <summary>잔소품 이름 머리(RoomDioramaBuild 가 끄고, WorldInvariants 가 켜진 게 없는지 본다). 남길 것: 침대·책상·의자·창·문·NOTEBOOK(일기장)·선반·러그.</summary>
    public static readonly string[] SmallKeys =
        { "POSTER", "PHOTO", "CERT", "FRAME", "PENCIL", "BOOK", "HANGER", "CLOTHES", "CALENDAR", "MAGAZINE", "MUG", "SHOE", "SLIPPER", "BOX_", "KART", "WALLCLOCK", "MIRROR", "TOWEL", "HOOK" };
    public static bool IsSmallProp(string n) { foreach (var k in SmallKeys) if (n.StartsWith(k)) return true; return false; }

    static readonly int DioId = Shader.PropertyToID("_A7Dio"), DioAmbId = Shader.PropertyToID("_A7DioAmb");
    static readonly int InBoxId = Shader.PropertyToID("_A7InBox"), InPId = Shader.PropertyToID("_A7InP"), InQId = Shader.PropertyToID("_A7InQ");

    /// <summary>지금 디오라마 렌더 모드인가(검사·캡처용).</summary>
    public static bool Active { get; private set; }
    /// <summary>0 = 1인칭, 1 = 디오라마.</summary>
    public float Weight => w;
    static bool eyeValid; static Vector3 eyeP, eyeF; static Transform eyeCam;

    /// <summary>Gaze 용: 디오라마로 카메라를 덮어쓴 동안엔 1인칭 눈 자세, 아니면 카메라 그대로.</summary>
    public static void EyeRay(Transform camT, out Vector3 pos, out Vector3 fwd)
    {
        if (eyeValid && camT == eyeCam) { pos = eyeP; fwd = eyeF; return; }
        pos = camT.position; fwd = camT.forward;
    }

    Camera cam; OnFootCamera foot; Transform player; Func<GameObject> dot;
    Matrix4x4 box, boxInv; bool hasBox; Transform room;
    float w, target, yawNow, yawVel; bool inRoom;
    RoomCalendar calendar;

    // 되돌릴 것
    readonly List<Renderer> hidden = new List<Renderer>();
    readonly List<Terrain> hiddenTerrain = new List<Terrain>();
    readonly List<Light> dimmed = new List<Light>();
    Light winLight, lampLight;
    CameraClearFlags savedClear; Color savedBg; float savedAmb; bool rendering;
    RectTransform dotRt; Vector2 dotMin, dotMax, dotPos;

    public void Init(OnFootCamera footCam, Transform playerT, Func<GameObject> dotRoot)
    {
        foot = footCam; player = playerT; dot = dotRoot;
    }

    void Awake() { cam = GetComponent<Camera>(); }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBegin;
        RenderPipelineManager.endCameraRendering += OnEnd;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBegin;
        RenderPipelineManager.endCameraRendering -= OnEnd;
        SetRendering(false); RestoreDot();
        w = target = 0f; inRoom = false; eyeValid = false;
        Shader.SetGlobalVector(DioId, Vector4.zero);
    }

    bool FindBox()
    {
        if (hasBox) return true;
        var g = FindFirstObjectByType<A7ShadeGlobals>();
        string n = g != null ? g.roomName : "ROOM_2F";
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.name == n && t.parent != null) { room = t; break; }
        if (room == null) return false;
        if (g != null && g.IndoorBox != Matrix4x4.zero) box = g.IndoorBox;
        else if (!A7ShadeGlobals.TryRoomBox(room, out box)) return false;
        boxInv = box.inverse; hasBox = true;
        calendar = FindFirstObjectByType<RoomCalendar>();
        return true;
    }

    static float MaxAbs(Vector3 q) => Mathf.Max(Mathf.Abs(q.x), Mathf.Max(Mathf.Abs(q.y), Mathf.Abs(q.z)));

    void LateUpdate()
    {
        eyeValid = false;
        if (foot == null || player == null || cam == null || !FindBox()) return;
        // 판정: 발 위 1 m 점이 방 상자 안(들어갈 땐 0.98, 나올 땐 1.02 — 문턱에서 깜빡이지 않게)
        float m = MaxAbs(box.MultiplyPoint3x4(player.position + Vector3.up * 1.0f));
        bool now = inRoom ? m <= 1.02f : m <= 0.98f;
        if (now && !inRoom)
        {
            yawNow = foot.Yaw; yawVel = 0f;
            if (foot.enabled && Mathf.Abs(foot.Pitch) < 5f) foot.Pitch = aimPitch;
        }
        inRoom = now;
        target = inRoom ? 1f : 0f;
        if (!foot.enabled) { ApplyCalendar(); return; }   // 달력·일기·침대 보기 중: 그 장면의 카메라 이동을 건드리지 않는다

        w = Mathf.MoveTowards(w, target, Time.deltaTime / Mathf.Max(0.01f, blendSec));
        SetRendering(inRoom);   // 나갈 때는 바로 끈다(카메라만 0.4초에 눈으로 돌아온다)
        ApplyCalendar();
        if (w <= 0f) { RestoreDot(); return; }

        // 1인칭 눈(OnFootCamera 가 이번 프레임에 놓은 자세) 저장 → Gaze 가 여기서 레이를 쏜다
        Transform t = cam.transform;
        eyeP = t.position; eyeF = t.forward; eyeCam = t; eyeValid = true;

        yawNow = Mathf.SmoothDampAngle(yawNow, foot.Yaw, ref yawVel, yawSmooth);
        Vector3 floorMid = boxInv.MultiplyPoint3x4(new Vector3(0f, -0.6f, 0f));
        Vector3 look = Vector3.Lerp(player.position + Vector3.up * 0.9f, floorMid, roomPull);
        Quaternion dr = Quaternion.Euler(pitch, yawNow, 0f);
        Vector3 dp = look - dr * Vector3.forward * distance;
        float s = w * w * (3f - 2f * w);
        t.SetPositionAndRotation(Vector3.Lerp(eyeP, dp, s), Quaternion.Slerp(Quaternion.LookRotation(eyeF), dr, s));
        MoveDot();
    }

    void ApplyCalendar()
    {
        if (calendar != null) calendar.SetDetail(!(rendering && foot.enabled));
    }

    // 조준점 = 눈 레이가 맞은 곳(3 m 안, 없으면 1.5 m 앞)의 화면 위치
    void MoveDot()
    {
        var go = dot != null ? dot() : null;
        if (go == null) return;
        if (dotRt == null)
        {
            dotRt = go.GetComponent<RectTransform>();
            if (dotRt == null) return;
            dotMin = dotRt.anchorMin; dotMax = dotRt.anchorMax; dotPos = dotRt.anchoredPosition;
        }
        Vector3 p = eyeP + eyeF * 1.5f;
        if (Physics.Raycast(eyeP, eyeF, out var hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(player)) p = hit.point;
        Vector3 v = cam.WorldToViewportPoint(p);
        var a = new Vector2(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y));
        dotRt.anchorMin = dotRt.anchorMax = a; dotRt.anchoredPosition = Vector2.zero;
    }

    void RestoreDot()
    {
        if (dotRt == null) return;
        dotRt.anchorMin = dotMin; dotRt.anchorMax = dotMax; dotRt.anchoredPosition = dotPos;
        dotRt = null;
    }

    void SetRendering(bool on)
    {
        if (on == rendering) return;
        rendering = on; Active = on;
        if (on)
        {
            savedClear = cam.clearFlags; savedBg = cam.backgroundColor; savedAmb = RenderSettings.ambientIntensity;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            RenderSettings.ambientIntensity = savedAmb * 0.25f;
            var wb = WorldBounds();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r.forceRenderingOff || r.transform.IsChildOf(player)) continue;
                // A7 재질은 셰이더가 상자 밖을 버린다 — 상자에 걸치면 남긴다. 그 밖(지형 메시·옹벽·URP Lit …)은 가운데가 상자 안일 때만
                bool glass = r.name.StartsWith("GLASS_");
                bool a7 = r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name == "PitCrew/SceneA7/Lit";
                bool keep = a7 ? r.bounds.Intersects(wb) : MaxAbs(box.MultiplyPoint3x4(r.bounds.center)) <= 1.05f;
                if (glass || !keep) { r.forceRenderingOff = true; hidden.Add(r); }
            }
            foreach (var tr in Terrain.activeTerrains) if (tr.drawHeightmap) { tr.drawHeightmap = false; tr.drawTreesAndFoliage = false; hiddenTerrain.Add(tr); }
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (!l.enabled || l.type == LightType.Directional) continue;
                if (wb.SqrDistance(l.transform.position) < l.range * l.range) { l.enabled = false; dimmed.Add(l); }
            }
            winLight = MakeLight("DIO_Window", WindowPoint(), windowColor, windowIntensity, windowRange);
            lampLight = MakeLight("DIO_DeskLamp", LampPoint(), lampColor, lampIntensity, lampRange);
            foreach (var r in player.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
        }
        else
        {
            if (cam != null) { cam.clearFlags = savedClear; cam.backgroundColor = savedBg; }
            RenderSettings.ambientIntensity = savedAmb;
            foreach (var r in hidden) if (r != null) r.forceRenderingOff = false;
            foreach (var tr in hiddenTerrain) if (tr != null) { tr.drawHeightmap = true; tr.drawTreesAndFoliage = true; }
            foreach (var l in dimmed) if (l != null) l.enabled = true;
            hidden.Clear(); hiddenTerrain.Clear(); dimmed.Clear();
            if (winLight != null) Destroy(winLight.gameObject);
            if (lampLight != null) Destroy(lampLight.gameObject);
            if (player != null && foot != null && foot.enabled)
                foreach (var r in player.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
        ApplyCalendar();
    }

    Bounds WorldBounds()
    {
        var b = new Bounds(boxInv.MultiplyPoint3x4(Vector3.zero), Vector3.zero);
        for (int i = 0; i < 8; i++)
            b.Encapsulate(boxInv.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
        return b;
    }

    // 창 = 방 상자 벽에 붙은 GLASS_* 를 벽(±x·±z)별로 묶어 넓이가 가장 큰 무리. 그 가운데에서 방 안쪽으로 0.5 m.
    Vector3 WindowPoint()
    {
        var sum = new Dictionary<int, Vector3>(); var area = new Dictionary<int, float>();
        foreach (var r in room.parent.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.name.StartsWith("GLASS_")) continue;
            Vector3 q = box.MultiplyPoint3x4(r.bounds.center);
            if (MaxAbs(q) > 1.15f || Mathf.Abs(q.y) > 0.9f) continue;
            int side = Mathf.Abs(q.x) > Mathf.Abs(q.z) ? (q.x > 0 ? 0 : 1) : (q.z > 0 ? 2 : 3);
            var sz = r.bounds.size; float a = sz.x * sz.y + sz.z * sz.y;
            sum[side] = (sum.TryGetValue(side, out var v) ? v : Vector3.zero) + r.bounds.center * a;
            area[side] = (area.TryGetValue(side, out var f) ? f : 0f) + a;
        }
        int best = -1; float ba = 0f;
        foreach (var kv in area) if (kv.Value > ba) { ba = kv.Value; best = kv.Key; }
        Vector3 center = boxInv.MultiplyPoint3x4(Vector3.zero);
        if (best < 0) return center + Vector3.up * 0.8f;
        Vector3 c = sum[best] / ba;
        Vector3 inward = center - c; inward.y = 0f;
        return c + inward.normalized * 0.5f;
    }

    // 책상 전등 = 방 안 DESK_* 윗면 + 0.45 m
    Vector3 LampPoint()
    {
        foreach (var r in room.GetComponentsInChildren<Renderer>(true))
            if (r.name.StartsWith("DESK_")) return new Vector3(r.bounds.center.x, r.bounds.max.y + 0.45f, r.bounds.center.z);
        return boxInv.MultiplyPoint3x4(Vector3.zero);
    }

    Light MakeLight(string n, Vector3 p, Color c, float i, float range)
    {
        var go = new GameObject(n);
        go.transform.position = p;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = c; l.intensity = i; l.range = range; l.shadows = LightShadows.None;
        l.renderMode = LightRenderMode.ForcePixel;
        return l;
    }

    // 씬의 실내 도트(A7ShadeGlobals.indoorDot)가 꺼져 있어도 방 상자 전역값은 필요하다 — 이 카메라를 그리는 동안만 넣고 끝나면 원래 값으로
    Matrix4x4 prevInBox; Vector4 prevInP, prevInQ; bool swapped;

    void OnBegin(ScriptableRenderContext ctx, Camera c)
    {
        if (!rendering || c != cam) return;
        prevInBox = Shader.GetGlobalMatrix(InBoxId); prevInP = Shader.GetGlobalVector(InPId); prevInQ = Shader.GetGlobalVector(InQId); swapped = true;
        var g = FindFirstObjectByType<A7ShadeGlobals>();
        Shader.SetGlobalMatrix(InBoxId, box);
        Shader.SetGlobalVector(InPId, new Vector4(1f, g != null ? g.indoorLevels : 10, 0f, 6f));
        Shader.SetGlobalVector(InQId, new Vector4(0f, 0f, g != null ? g.indoorDitherWidth : 0.15f, 0f));
        Shader.SetGlobalVector(DioId, new Vector4(1f, 0f, wallCut, ceilCut));
        Shader.SetGlobalColor(DioAmbId, ambient);
    }

    void OnEnd(ScriptableRenderContext ctx, Camera c)
    {
        if (c != cam) return;
        Shader.SetGlobalVector(DioId, Vector4.zero);
        if (!swapped) return;
        Shader.SetGlobalMatrix(InBoxId, prevInBox); Shader.SetGlobalVector(InPId, prevInP); Shader.SetGlobalVector(InQId, prevInQ);
        swapped = false;
    }
}
