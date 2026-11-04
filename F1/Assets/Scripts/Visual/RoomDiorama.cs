using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 과제 20261104-1559(D-298, SPEC §1 「실내는 디오라마」): 플레이어가 2층 방(ROOM_2F) 상자 안에 있으면
/// 카메라가 플레이어 뒤·위로 빠져 방 전체를 상자처럼 내려다본다(기준 화면 world/taste/interior-candidates/07).
/// - 걷기·E 상호작용은 1인칭 그대로: OnFootCamera 가 놓은 눈 자세를 저장해 두고 Gaze 가 그 눈에서 레이를 쏜다(EyeRay). 조준점(dot)은 그 레이가 맞은 곳의 화면 위치로 옮긴다.
/// - 전환은 포장 2D(Stage_Packing.BoardSec)와 같은 0.4초. 문을 지나 상자를 나가면 1인칭으로 돌아온다.
/// - 그리는 동안(저해상 카메라에만): A7 셰이더 전역 _A7Dio 를 켜 방 상자 밖·천장·카메라 쪽 벽을 버린다. 배경 검정, 방과 안 겹치는 렌더러·지형은 끈다
///   (forceRenderingOff — enabled 는 안 건드림). 과제 20261104-2033: 20 의 「창 밖은 바깥 세계」를 되돌림 — 창 유리는 시간대 단색 판(낮 하늘색·노을 주황·밤 남색).
///   빛 = 씬의 해(방향광 — 시간대 dusk 로 색·각도가 바뀜, 그림자 켬, 잘린 지붕은 그림자도 안 드리움) × (1 − 안개) + 시간대 바탕빛 + 책상 전등 한 곳.
///   전등은 밤에만 주역(낮엔 작고 약하게). 해가 꺼지면(밤) 바탕빛 남색 + 전등. 방에 닿는 다른 점·스팟 빛과 전역광은 낮춘다. 나오면 전부 되돌린다.
/// 좌표는 손으로 적지 않는다 — 방 상자(A7ShadeGlobals.TryRoomBox)·창 유리(GLASS_*)·책상(DESK_*) 이름에서 매번 잡는다.
/// 과제 20261105-0359(D-304): 2층 방만이 아니라 건물 안 전부(A7ShadeGlobals.FindInteriors — 집 1층·보관소·우체국. 정비소는 컷 셰이더가 없어 1인칭 — CanCut). 상자 → 상자는 w 를 그대로 두고
/// 상자만 바꿔 끼운다(실내 도트·빛 전역값도 그 상자로). 작업대·칠판·카트·낚시가 카메라를 가져간 동안은 그림까지 비켜 있다.
/// </summary>
[DefaultExecutionOrder(100)]   // OnFootCamera(0) 가 눈 자세를 놓은 뒤에 덮어쓴다
public sealed class RoomDiorama : MonoBehaviour
{
    [Header("카메라 각도(도현 판정용 가설값 — 과제: 피치 45~55°, 거리 4~5 m)")]
    public float pitch = 50f;
    public float distance = 5f;
    [Tooltip("바라보는 점 = 플레이어 가슴 ↔ 방 바닥 가운데 섞기(0 = 플레이어, 1 = 방 가운데)")]
    [Range(0f, 1f)] public float roomPull = 0.5f;
    [Tooltip("1인칭 ↔ 디오라마 전환 시간(초) — Stage_Packing.BoardSec 와 같은 값")]
    public float blendSec = 0.4f;
    [Tooltip("방에 들어올 때 눈이 수평이면 이 각도로 앞 바닥을 본다(조준점이 방 물건에 닿게)")]
    public float aimPitch = 30f;
    [Tooltip("카메라가 고개 돌림(Yaw)을 따라가는 시간(초)")]
    public float yawSmooth = 0.25f;
    [Tooltip("과제 20261105-0359(D-304) 가설값: 이름 없는 건물 안(보관소·정비소처럼 넓은 곳)은 거리 = 상자 가로 반폭(큰 쪽) × 이 값, distance~maxDistance. 2층 방은 distance 그대로")]
    public float distPerHalf = 1.6f;
    public float maxDistance = 12f;

    [Header("빛 — 시간대(낮 / 노을 = SunsetLook.dusk / 밤 = 해 꺼짐)")]
    [Tooltip("바탕빛: 낮 밝은 회청 · 노을 주황빛 · 밤 남색")]
    public Color ambientDay = new Color(0.62f, 0.66f, 0.72f);
    public Color ambientDusk = new Color(0.62f, 0.42f, 0.28f);
    public Color ambientNight = new Color(0.07f, 0.08f, 0.18f);
    [Tooltip("창 유리 단색 판: 낮 하늘색 · 노을 주황 · 밤 짙은 남색")]
    public Color glassDay = new Color(0.62f, 0.80f, 0.96f);
    public Color glassDusk = new Color(1.00f, 0.60f, 0.30f);
    public Color glassNight = new Color(0.07f, 0.09f, 0.24f);
    [Tooltip("날씨 안개 0..1 — 창빛(해) 세기 = 1 − 안개. 날씨 시스템이 아직 없어 값 하나만 둔다(생기면 여기 묶는다)")]
    [Range(0f, 1f)] public float fog = 0f;
    [Tooltip("그림자에서 지붕을 빼는 집 범위(방 상자 좌표 — 방 가운데에서 이만큼 옆까지, 천장 위만)")]
    public float houseReach = 3f;
    public Color lampColor = new Color(1.00f, 0.78f, 0.48f);
    [Tooltip("책상 전등: 밤 세기·반경 / 낮 세기·반경(낮엔 작고 약하게)")]
    public float lampIntensity = 1.8f, lampRange = 1.6f;
    public float lampDayIntensity = 0.5f, lampDayRange = 1.0f;
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

    Camera cam; OnFootCamera foot; Transform player; Func<GameObject> dot; Func<bool> camTaken;
    Matrix4x4 box, boxInv; Transform room, building;
    float w, target, yawNow, yawVel; bool inRoom;
    RoomCalendar calendar;
    // 과제 20261105-0359(D-304): 건물 안 전부 — 상자 여럿(A7ShadeGlobals.FindInteriors), 지금 상자 cur
    List<A7ShadeGlobals.Interior> rooms; int cur = -1;
    Vector3 midNow, midVel; float distNow, distVel;
    /// <summary>지금(또는 마지막) 상자 이름(검사·캡처용). 없으면 "".</summary>
    public string RoomNow => cur >= 0 ? rooms[cur].name : "";
    /// <summary>찾은 건물 안 목록(검사·캡처용, 첫 LateUpdate 뒤).</summary>
    public IReadOnlyList<A7ShadeGlobals.Interior> Rooms => rooms;

    // 되돌릴 것
    readonly List<Renderer> glass = new List<Renderer>();
    readonly List<Material> glassSaved = new List<Material>();
    readonly List<Light> dimmed = new List<Light>();
    readonly List<Renderer> hidden = new List<Renderer>();
    readonly List<Terrain> hiddenTerrain = new List<Terrain>();
    Light lampLight; Material glassMat; SunsetLook sunset; bool sunsetLooked;
    CameraClearFlags savedClear; Color savedBg; float savedAmb; bool rendering;
    /// <summary>지금 시간대 무게(검사·캡처용): 노을 0..1, 밤 0..1.</summary>
    public float DuskW { get; private set; }
    public float NightW { get; private set; }
    /// <summary>창 고르기 결과(검사·캡처용) — 책상 창 가운데. 못 찾으면 방 가운데.</summary>
    public Vector3 WindowCenter { get; private set; }
    RectTransform dotRt; Vector2 dotMin, dotMax, dotPos;

    /// <param name="taken">참이면 다른 장면(작업대·칠판·카트·낚시)이 카메라를 가져간 동안 — 디오라마는 그림까지 비켜 있다.</param>
    public void Init(OnFootCamera footCam, Transform playerT, Func<GameObject> dotRoot, Func<bool> taken = null)
    {
        foot = footCam; player = playerT; dot = dotRoot; camTaken = taken;
    }

    void Awake() { cam = GetComponent<Camera>(); }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBegin;
        RenderPipelineManager.endCameraRendering += OnEnd;
    }

    void OnDestroy() { if (glassMat != null) Destroy(glassMat); }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBegin;
        RenderPipelineManager.endCameraRendering -= OnEnd;
        SetRendering(false); RestoreDot();
        w = target = 0f; inRoom = false; eyeValid = false;
        Shader.SetGlobalVector(DioId, Vector4.zero);
    }

    // 건물 안 목록은 처음 한 번만 찾는다(플레이어 콜라이더는 안 맞는 셈) — 첫째는 2층 방(이름 방) 상자 그대로
    bool FindBox()
    {
        if (rooms == null)
        {
            rooms = A7ShadeGlobals.FindInteriors(player);
            rooms.RemoveAll(r => !r.ok || !CanCut(r.building));
            // ⚠ 컷(천장·카메라 쪽 벽 버림)은 SceneA7/Lit 셰이더에만 있다 — 그 재질이 하나도 없는 건물(정비소 = Library/A7LitPoint)은 지붕이 화면을 덮으므로 1인칭 그대로
            calendar = FindFirstObjectByType<RoomCalendar>();
        }
        return rooms.Count > 0;
    }

    public const string CutShader = "PitCrew/SceneA7/Lit";
    public static bool CanCut(Transform building)
    {
        if (building == null) return false;
        foreach (var r in building.GetComponentsInChildren<Renderer>())
            foreach (var m in r.sharedMaterials)
                if (m != null && m.shader != null && m.shader.name == CutShader) return true;
        return false;
    }

    void Select(int i)
    {
        cur = i; var it = rooms[i];
        box = it.box; boxInv = box.inverse; room = it.room; building = it.building;
    }

    static float MaxAbs(Vector3 q) => Mathf.Max(Mathf.Abs(q.x), Mathf.Max(Mathf.Abs(q.y), Mathf.Abs(q.z)));
    float Mx(int i, Vector3 p) => MaxAbs(rooms[i].box.MultiplyPoint3x4(p));
    static float Vol(A7ShadeGlobals.Interior it) => it.room != null ? 0f : it.half.x * it.half.y * it.half.z;

    /// <summary>
    /// 발 위 1 m 점이 든 상자(들어갈 땐 0.98, 지금 상자에서 나갈 땐 1.02 — 문턱에서 깜빡이지 않게). 여럿이면 작은 쪽(2층 방이 맨 먼저).
    /// 없어도 같은 건물 다른 상자 1.3 안(방 사이 문턱·틈)이면 지금 상자를 붙든다 — 방 → 방이 끊기지 않게.
    /// </summary>
    int Pick(Vector3 p)
    {
        int best = -1; float bv = float.MaxValue;
        for (int i = 0; i < rooms.Count; i++)
        {
            float lim = inRoom && i == cur ? 1.02f : 0.98f;
            float v = Vol(rooms[i]);
            if (Mx(i, p) <= lim && v < bv) { best = i; bv = v; }
        }
        if (best >= 0 || !inRoom || cur < 0) return best;
        for (int i = 0; i < rooms.Count; i++)
            if (i != cur && rooms[i].building == rooms[cur].building && Mx(i, p) <= 1.3f) return cur;
        return -1;
    }

    float DistFor(int i)
    {
        var it = rooms[i];
        return it.room != null ? distance : Mathf.Clamp(distPerHalf * Mathf.Max(it.half.x, it.half.z), distance, maxDistance);
    }

    void LateUpdate()
    {
        eyeValid = false;
        if (foot == null || player == null || cam == null || !FindBox()) return;
        // 작업대·칠판·카트·낚시가 카메라를 가져간 동안: 그림도 비켜 있다(w 는 그대로 두어 돌아오면 이어진다)
        if (camTaken != null && camTaken()) { SetRendering(false); RestoreDot(); return; }
        Vector3 probe = player.position + Vector3.up * 1.0f;
        int now = Pick(probe);
        if (now >= 0 && now != cur)
        {
            SetRendering(false);   // 상자 바꿈: 끄고 새 상자로 아래에서 다시 켠다(w 는 그대로 — 상자 → 상자는 안 끊김)
            bool otherHouse = cur < 0 || rooms[now].building != building;
            Select(now);
            // 다른 건물(순간이동)은 마을을 가로질러 미끄러지지 않게 바로 놓는다
            if ((!inRoom && w <= 0f) || otherHouse) { midNow = FloorMid(); distNow = DistFor(cur); midVel = Vector3.zero; distVel = 0f; }
        }
        else if (now >= 0 && !inRoom && w <= 0f) { midNow = FloorMid(); distNow = DistFor(cur); midVel = Vector3.zero; distVel = 0f; }
        if (now >= 0 && !inRoom)
        {
            yawNow = foot.Yaw; yawVel = 0f;
            if (foot.enabled && Mathf.Abs(foot.Pitch) < 5f) foot.Pitch = aimPitch;
        }
        inRoom = now >= 0;
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
        // 방 바닥 가운데·거리는 상자가 바뀌면 부드럽게 따라간다(한 방 안에선 상수 — 2층 방은 전과 같다)
        midNow = Vector3.SmoothDamp(midNow, FloorMid(), ref midVel, yawSmooth);
        distNow = Mathf.SmoothDamp(distNow, DistFor(cur), ref distVel, yawSmooth);
        Vector3 look = Vector3.Lerp(player.position + Vector3.up * 0.9f, midNow, roomPull);
        Quaternion dr = Quaternion.Euler(pitch, yawNow, 0f);
        Vector3 dp = look - dr * Vector3.forward * distNow;
        float s = w * w * (3f - 2f * w);
        t.SetPositionAndRotation(Vector3.Lerp(eyeP, dp, s), Quaternion.Slerp(Quaternion.LookRotation(eyeF), dr, s));
        if (rendering) UpdateGlass();   // 카메라를 디오라마 자리로 옮긴 뒤에 — 눈(방 안) 기준이면 카메라 쪽 벽 유리를 못 자른다
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
            // 창 유리: 시간대 단색 판(실행 중 사본 — 공유 .mat 은 안 건드림). 카메라 쪽 벽 유리는 UpdateGlass 가 매 프레임 숨긴다
            foreach (var r in building.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.name.StartsWith("GLASS_")) continue;
                glass.Add(r); glassSaved.Add(r.sharedMaterial);
                r.sharedMaterial = GlassMat();
            }
            UpdateGlass();
            // 방 밖은 검정: 방 상자와 안 겹치는 렌더러·지형을 끈다. A7 재질은 셰이더가 상자 밖을 버리므로 상자에 걸치면 남긴다. 그 밖(지형 메시·URP Lit …)은 가운데가 상자 안일 때만
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r.forceRenderingOff || r.transform.IsChildOf(player) || r.name.StartsWith("GLASS_")) continue;
                bool a7 = r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name == "PitCrew/SceneA7/Lit";
                bool keep = a7 ? r.bounds.Intersects(wb) : MaxAbs(box.MultiplyPoint3x4(r.bounds.center)) <= 1.05f;
                if (!keep) { r.forceRenderingOff = true; hidden.Add(r); }
            }
            foreach (var tr in Terrain.activeTerrains) if (tr.drawHeightmap) { tr.drawHeightmap = false; tr.drawTreesAndFoliage = false; hiddenTerrain.Add(tr); }
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (!l.enabled || l.type == LightType.Directional) continue;
                if (wb.SqrDistance(l.transform.position) < l.range * l.range) { l.enabled = false; dimmed.Add(l); }
            }
            PickWindowIn(building, box, out var wc, out _); WindowCenter = wc;
            var lp = LampPoint();
            if (lp.HasValue) lampLight = MakeLight("DIO_DeskLamp", lp.Value, lampColor, lampIntensity, lampRange);
            foreach (var r in player.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
        }
        else
        {
            RenderSettings.ambientIntensity = savedAmb;
            if (cam != null) { cam.clearFlags = savedClear; cam.backgroundColor = savedBg; }
            for (int i = 0; i < glass.Count; i++) if (glass[i] != null) { glass[i].sharedMaterial = glassSaved[i]; glass[i].forceRenderingOff = false; }
            foreach (var l in dimmed) if (l != null) l.enabled = true;
            foreach (var r in hidden) if (r != null) r.forceRenderingOff = false;
            foreach (var tr in hiddenTerrain) if (tr != null) { tr.drawHeightmap = true; tr.drawTreesAndFoliage = true; }
            glass.Clear(); glassSaved.Clear(); dimmed.Clear(); hidden.Clear(); hiddenTerrain.Clear();
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

    /// <summary>바닥에서 이 높이(m) 안까지 내려오는 유리 = 유리문 — 창 고르기에서 뺀다(19 는 문 유리를 창으로 골랐다).</summary>
    public const float DoorGlassFloor = 0.5f;

    /// <summary>
    /// 창 = 방 상자 벽에 붙은 GLASS_* 중 유리문(아래 끝이 바닥 + DoorGlassFloor 안)을 뺀 것을 벽(±x·±z)별로 묶어 넓이가 가장 큰 무리 → 책상 창.
    /// 돌려주는 것: 그 무리 가운데, 아래 끝의 바닥 위 높이(m). 없으면 false·방 가운데.
    /// </summary>
    public static bool PickWindow(Transform room, Matrix4x4 box, out Vector3 center, out float bottomAboveFloor)
        => PickWindowIn(room.parent, box, out center, out bottomAboveFloor);

    /// <summary>PickWindow 의 몸통 — 유리를 찾는 곳이 건물(house) 전체.</summary>
    public static bool PickWindowIn(Transform house, Matrix4x4 box, out Vector3 center, out float bottomAboveFloor)
    {
        var inv = box.inverse;
        float floorY = inv.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)).y;
        var sum = new Dictionary<int, Vector3>(); var area = new Dictionary<int, float>(); var low = new Dictionary<int, float>();
        foreach (var r in house.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.name.StartsWith("GLASS_")) continue;
            Vector3 q = box.MultiplyPoint3x4(r.bounds.center);
            if (MaxAbs(q) > 1.15f || Mathf.Abs(q.y) > 0.9f) continue;
            if (r.bounds.min.y - floorY < DoorGlassFloor) continue;
            int side = Mathf.Abs(q.x) > Mathf.Abs(q.z) ? (q.x > 0 ? 0 : 1) : (q.z > 0 ? 2 : 3);
            var sz = r.bounds.size; float a = sz.x * sz.y + sz.z * sz.y;
            sum[side] = (sum.TryGetValue(side, out var v) ? v : Vector3.zero) + r.bounds.center * a;
            area[side] = (area.TryGetValue(side, out var f) ? f : 0f) + a;
            low[side] = Mathf.Min(low.TryGetValue(side, out var m) ? m : float.MaxValue, r.bounds.min.y - floorY);
        }
        int best = -1; float ba = 0f;
        foreach (var kv in area) if (kv.Value > ba) { ba = kv.Value; best = kv.Key; }
        center = inv.MultiplyPoint3x4(Vector3.zero); bottomAboveFloor = -1f;
        if (best < 0) return false;
        center = sum[best] / ba; bottomAboveFloor = low[best];
        return true;
    }

    // 셰이더 A7DioCut 과 같은 규칙(유리처럼 A7 이 아닌 것용): 방 상자(벽 두께 여유 1.15) 밖·천장·카메라 쪽 벽이면 숨김
    bool Cut(Vector3 wp)
    {
        Vector3 b = box.MultiplyPoint3x4(wp), c = box.MultiplyPoint3x4(cam.transform.position);
        if (MaxAbs(b) > 1.15f || b.y > ceilCut) return true;
        if (Mathf.Abs(c.x) > 1f && b.x * Mathf.Sign(c.x) > wallCut) return true;
        if (Mathf.Abs(c.z) > 1f && b.z * Mathf.Sign(c.z) > wallCut) return true;
        return false;
    }

    void UpdateGlass()
    {
        foreach (var r in glass) if (r != null) r.forceRenderingOff = Cut(r.bounds.center);
    }

    // 창 유리 = 불투명 단색 판(URP Unlit 사본, 실행 중에만). 색은 OnBegin 이 시간대마다 넣는다
    Material GlassMat()
    {
        if (glassMat == null)
        {
            glassMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "DIO_Glass(runtime)" };
            glassMat.SetFloat("_Cull", 0f);   // 양면 — 유리 판 앞면이 바깥을 봐서 방 안에서 보면 뒷면이다
        }
        return glassMat;
    }

    /// <summary>
    /// 시간대 무게: 노을 = SunsetLook.dusk(켜져 있을 때), 밤 = 해(RenderSettings.sun)가 꺼졌거나 세기 0.5 아래로 내려간 만큼.
    /// 바탕빛·유리 색·전등 세기를 이 둘로 섞는다(낮 → 노을 → 밤).
    /// </summary>
    public static void TimeWeights(SunsetLook sl, Light sun, out float dusk, out float night)
    {
        dusk = sl != null && sl.IsOn ? Mathf.Clamp01(sl.dusk) : 0f;
        night = sun == null || !sun.isActiveAndEnabled ? 1f : 1f - Mathf.Clamp01(sun.intensity / 0.5f);
    }

    public static Color Mix(Color day, Color dusk, Color night, float dw, float nw) => Color.Lerp(Color.Lerp(day, dusk, dw), night, nw);

    void ApplyTime()
    {
        if (!sunsetLooked) { sunset = SunsetLook.Find(); sunsetLooked = true; }
        TimeWeights(sunset, RenderSettings.sun, out var dw, out var nw);
        DuskW = dw; NightW = nw;
        if (glassMat != null) glassMat.SetColor("_BaseColor", Mix(glassDay, glassDusk, glassNight, dw, nw));
        if (lampLight != null)
        {
            lampLight.intensity = Mathf.Lerp(lampDayIntensity, lampIntensity, nw);
            lampLight.range = Mathf.Lerp(lampDayRange, lampRange, nw);
        }
    }

    // 책상 전등 = 방 안 DESK_* 윗면 + 0.45 m. 이름 없는 건물 안은 그 건물의 상자 안 DESK_ 만, 없으면 전등 없음
    Vector3? LampPoint()
    {
        var src = room != null ? room : building;
        foreach (var r in src.GetComponentsInChildren<Renderer>(true))
            if (r.name.StartsWith("DESK_") && (room != null || MaxAbs(box.MultiplyPoint3x4(r.bounds.center)) <= 1f))
                return new Vector3(r.bounds.center.x, r.bounds.max.y + 0.45f, r.bounds.center.z);
        if (room != null) return boxInv.MultiplyPoint3x4(Vector3.zero);
        return null;
    }

    Vector3 FloorMid() => boxInv.MultiplyPoint3x4(new Vector3(0f, -0.6f, 0f));

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
        // y = 해 세기 배수(창빛 = 1 − 안개) · 바탕빛(시간대) w = 그림자에서 지붕을 빼는 집 범위
        ApplyTime();
        Shader.SetGlobalVector(DioId, new Vector4(1f, 1f - Mathf.Clamp01(fog), wallCut, ceilCut));
        Color amb = Mix(ambientDay, ambientDusk, ambientNight, DuskW, NightW);
        Color a = QualitySettings.activeColorSpace == ColorSpace.Linear ? amb.linear : amb;
        Shader.SetGlobalVector(DioAmbId, new Vector4(a.r, a.g, a.b, houseReach));
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
