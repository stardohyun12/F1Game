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
/// 과제 20261105-0359(D-304): 2층 방만이 아니라 건물 안 전부(A7ShadeGlobals.FindInteriors — 집 1층·보관소·우체국·정비소. 컷 셰이더가 없는 건물은 1인칭 — CanCut). 상자 → 상자는 w 를 그대로 두고
/// 상자만 바꿔 끼운다(실내 도트·빛 전역값도 그 상자로). 작업대·칠판·카트·낚시가 카메라를 가져간 동안은 그림까지 비켜 있다.
/// 과제 20261105-0419(D-306): 시점 고정 — 방마다 한 각도(문 쪽 벽 뒤·위, FixedYaw). 작은 방은 카메라 자리까지, 넓은 곳은 각도 그대로 플레이어를 따라 평행이동.
/// D-307: 카메라 자리 = 바닥 네 모서리 + 천장 컷 높이 벽 위가 화면 안에 다 드는 자리(Fit). 그러면 사람이 화면 높이 minCapsule 보다 작아지는 방만 넓은 곳.
/// 안에선 WASD = 화면 기준(OnFootPlayer.ScreenYaw), 몸은 가는 쪽, 마우스 시선 없음, E = 몸 앞 부채꼴 안 가장 가까운 물건(Gaze.Fan). 정비소도 셰이더에 컷을 넣어 디오라마.
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
    [Header("방이 다 보이게(D-307 ⑧) — 피치 그대로, 바닥 네 모서리 + 천장 컷 높이 벽 위 네 모서리가 화면 안 여백에 들게 카메라 자리를 푼다")]
    [Tooltip("화면 가장자리 여백(한쪽, 화면 크기 비율)")]
    [Range(0f, 0.2f)] public float fitMargin = 0.05f;
    [Tooltip("맞췄을 때 사람 캡슐(방 바닥 가운데에 선 키 capsuleHeight)이 화면 높이의 이 비율보다 작으면 그 방만 넓은 곳 — 각도 고정, 플레이어를 따라 평행이동(옛 거리 규칙)")]
    public float minCapsule = 0.06f;
    public float capsuleHeight = 1.8f;

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
    [Tooltip("천장 컷 상한(바닥에서 m) — 높은 상자(정비소)는 지붕 처마가 ceilCut 아래로 내려와 화면을 덮었다")]
    public float ceilMetres = 2.6f;
    [Header("결 — 디오라마 전용(D-307 ⑦ 도현 「잔결을 줄인 것으로」 = B 기본). 1인칭 실내 도트 값은 안 건드림")]
    [Tooltip("켜면 B(기본). 끄면 A(디더 폭 = A7ShadeGlobals.indoorDitherWidth, 텍스처 안 거름 — 과제 20261105-0453 의 옛 값)")]
    public bool grainB = true;
    [Tooltip("B: 텍스처 거르는 탭 간격(텍셀) — 바닥·벽 텍스처 잔점을 눌러 준다")] [Range(0f, 4f)] public float grainBFilterTexels = 1f;
    [Tooltip("B: 밝기 단 끝 디더 폭(0..1)")] [Range(0f, 1f)] public float grainBDitherWidth = 0.04f;
    float Ceil => CeilOf(box, ceilCut, ceilMetres);
    /// <summary>천장 컷 높이(상자 좌표 y) — 검사도 같은 식을 쓴다.</summary>
    public static float CeilOf(Matrix4x4 b, float cut, float metres) => Mathf.Min(cut, -1f + metres / Mathf.Max(0.1f, b.inverse.MultiplyVector(Vector3.up).magnitude));

    /// <summary>잔소품 이름 머리(RoomDioramaBuild 가 끄고, WorldInvariants 가 켜진 게 없는지 본다). 남길 것: 침대·책상·의자·창·문·NOTEBOOK(일기장)·선반·러그.</summary>
    public static readonly string[] SmallKeys =
        { "POSTER", "PHOTO", "CERT", "FRAME", "PENCIL", "BOOK", "HANGER", "CLOTHES", "CALENDAR", "MAGAZINE", "MUG", "SHOE", "SLIPPER", "BOX_", "KART", "WALLCLOCK", "MIRROR", "TOWEL", "HOOK" };
    public static bool IsSmallProp(string n) { foreach (var k in SmallKeys) if (n.StartsWith(k)) return true; return false; }

    static readonly int DioId = Shader.PropertyToID("_A7Dio"), DioAmbId = Shader.PropertyToID("_A7DioAmb");
    static readonly int InBoxId = Shader.PropertyToID("_A7InBox"), InPId = Shader.PropertyToID("_A7InP"), InQId = Shader.PropertyToID("_A7InQ");

    /// <summary>지금 디오라마 렌더 모드인가(검사·캡처용).</summary>
    public static bool Active { get; private set; }
    /// <summary>D-306: 건물 안 고정 시점 조작 중(화면 기준 WASD·부채꼴 E). Gaze·DotScreenUI 가 본다.</summary>
    public static bool Fixed { get; private set; }
    /// <summary>D-306: 고정 시점의 E 대상 위 표시 자리(대상 윗면 가운데, Gaze 가 매 프레임 넣음) — 없으면 null. 조준점 대신 여기 작은 표시.</summary>
    public static Vector3? Mark { get; set; }
    /// <summary>0 = 1인칭, 1 = 디오라마.</summary>
    public float Weight => w;
    static bool eyeValid; static Vector3 eyeP, eyeF; static Transform eyeCam;

    /// <summary>Gaze 용: 디오라마로 카메라를 덮어쓴 동안엔 1인칭 눈 자세, 아니면 카메라 그대로.</summary>
    public static void EyeRay(Transform camT, out Vector3 pos, out Vector3 fwd)
    {
        if (eyeValid && camT == eyeCam) { pos = eyeP; fwd = eyeF; return; }
        pos = camT.position; fwd = camT.forward;
    }

    Camera cam; OnFootCamera foot; OnFootPlayer body; Transform player; Func<GameObject> dot; Func<bool> camTaken;
    Matrix4x4 box, boxInv; Transform room, building;
    float w, target, yawNow, yawVel; bool inRoom;
    RoomCalendar calendar;
    // 과제 20261105-0359(D-304): 건물 안 전부 — 상자 여럿(A7ShadeGlobals.FindInteriors), 지금 상자 cur
    List<A7ShadeGlobals.Interior> rooms; int cur = -1; float[] roomYaw; Vector4[] roomFaces;
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
        body = playerT != null ? playerT.GetComponent<OnFootPlayer>() : null;
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
        SetRendering(false); RestoreDot(); Release();
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
            // ⚠ 컷(천장·카메라 쪽 벽 버림)은 CutShaders 에만 있다 — 그 재질이 하나도 없는 건물은 지붕이 화면을 덮으므로 1인칭 그대로
            calendar = FindFirstObjectByType<RoomCalendar>();
            var doors = FindObjectsByType<DoorHinge>(FindObjectsSortMode.None);
            roomYaw = new float[rooms.Count]; roomFaces = new Vector4[rooms.Count];
            for (int i = 0; i < rooms.Count; i++) { roomYaw[i] = FixedYaw(rooms[i], rooms, doors, player); roomFaces[i] = A7ShadeGlobals.WallFaces(rooms[i]); }
            A7ShadeGlobals.PatchOneSidedFloors(rooms);   // 과제 20261105-0535 ③: 우체국 2층 한 면 바닥
        }
        return rooms.Count > 0;
    }

    /// <summary>A7DioCut(천장·카메라 쪽 벽 버림)이 든 셰이더. 정비소 Library/A7LitPoint 는 과제 20261105-0419 에 넣었다.</summary>
    public static readonly string[] CutShaders = { "PitCrew/SceneA7/Lit", "PitCrew/Library/A7LitPoint" };
    public static bool IsCutShader(Material m) => m != null && m.shader != null && Array.IndexOf(CutShaders, m.shader.name) >= 0;
    public static bool CanCut(Transform building)
    {
        if (building == null) return false;
        foreach (var r in building.GetComponentsInChildren<Renderer>())
            foreach (var m in r.sharedMaterials)
                if (IsCutShader(m)) return true;
        return false;
    }

    /// <summary>유리 = GLASS_* 이름(집) 또는 재질 이름에 glass(우체국 Q4_*_glass). 컷 셰이더가 아니라 Cut()·시간대 단색 판으로 다룬다.</summary>
    public static bool IsGlass(Renderer r)
    {
        if (r.name.StartsWith("GLASS_")) return true;
        var m = r.sharedMaterial;
        return m != null && m.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static Vector3 SideDir(int s, Vector3 ax, Vector3 az) => s == 0 ? ax : s == 1 ? -ax : s == 2 ? az : -az;

    /// <summary>
    /// D-306 방의 고정 시점 방향(도 — 카메라가 바라보는 쪽, 상자 축에 맞춤). 카메라는 「문 쪽 벽」 뒤에 선다.
    /// 문 = 상자 벽에 붙은 DoorHinge(벽 따라 다른 축 |q| ≤ 1). 여럿이면 문 밖 0.8 m 가 같은 건물 다른 방 상자 안인 것(집 안에서 들어오는 문) 먼저, 그다음 벽면에 가까운 것.
    /// 문짝이 없으면(보관소·우체국·정비소 — 열린 입구) 바닥 + 1 m 에서 네 벽 쪽으로 쏜 레이 7개 중 가장 많이 빠져나가는 벽 = 입구.
    /// </summary>
    public static float FixedYaw(A7ShadeGlobals.Interior it, IList<A7ShadeGlobals.Interior> all, IList<DoorHinge> doors, Transform ignore)
    {
        var inv = it.box.inverse;
        Vector3 ax = inv.MultiplyVector(Vector3.right).normalized, az = inv.MultiplyVector(Vector3.forward).normalized;
        var pts = new List<Vector3>();
        foreach (var d in doors)
        {
            if (d == null) continue;
            // 닫힌 자리 가운데 — 열린 문짝으로 재면 Play 중 문이 열려 있을 때 옆방 문으로 잡혔다(2층 2F#2 가 100°→10°)
            if (d.configured) { pts.Add(d.transform.parent != null ? d.transform.parent.TransformPoint(d.centerLocal) : d.centerLocal); continue; }
            var rs = d.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { pts.Add(d.transform.position); continue; }
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            pts.Add(bb.center);   // 문짝 가운데(Gaze.CenterOf 와 같음)
        }
        int side = DoorSide(it, all, pts, ax, az);
        if (side < 0)
        {
            // 문짝(DoorHinge)이 없는 건물: 이름에 door·shutter 가 든 렌더러(정비소 DOOR_01·SHUTTER_01, 우체국 Q4_door_glass)
            // 바닥까지 내려오는 것만 — 우체국 사서함 문(POBOX_DOOR_*)은 벽에 붙은 작은 칸이라 빠진다
            pts.Clear();
            foreach (var r in it.building.GetComponentsInChildren<Renderer>())
            {
                if (r.name.IndexOf("door", StringComparison.OrdinalIgnoreCase) < 0 && r.name.IndexOf("shutter", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var b = r.bounds;
                if (it.box.MultiplyPoint3x4(new Vector3(b.center.x, b.min.y, b.center.z)).y > -0.7f) continue;
                pts.Add(b.center);
            }
            side = DoorSide(it, all, pts, ax, az);
        }
        if (side < 0) side = OpenSide(it, ax, az, ignore);
        Vector3 f = -SideDir(side, ax, az);
        return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
    }

    static int DoorSide(A7ShadeGlobals.Interior it, IList<A7ShadeGlobals.Interior> all, List<Vector3> pts, Vector3 ax, Vector3 az)
    {
        int side = -1; float best = float.MaxValue;
        foreach (var p in pts)
        {
            Vector3? c = p;
            Vector3 q = it.box.MultiplyPoint3x4(c.Value);
            if (Mathf.Abs(q.y) > 1.2f) continue;
            bool onX = Mathf.Abs(q.x) > Mathf.Abs(q.z);
            float along = onX ? Mathf.Abs(q.x) : Mathf.Abs(q.z), other = onX ? Mathf.Abs(q.z) : Mathf.Abs(q.x);
            if (other > 1f || along < 0.6f || along > 1.6f) continue;
            int s = onX ? (q.x > 0 ? 0 : 1) : (q.z > 0 ? 2 : 3);
            Vector3 outP = c.Value + SideDir(s, ax, az) * 0.8f;
            bool fromInside = false;
            foreach (var o in all) if (o != it && o.ok && o.building == it.building && MaxAbs(o.box.MultiplyPoint3x4(outP)) <= 1.05f) fromInside = true;
            float score = Mathf.Abs(along - 1f) + (fromInside ? 0f : 10f);
            if (score < best) { best = score; side = s; }
        }
        return side;
    }

    static readonly RaycastHit[] sideHits = new RaycastHit[8];
    static int OpenSide(A7ShadeGlobals.Interior it, Vector3 ax, Vector3 az, Transform ignore)
    {
        Vector3 o = it.box.inverse.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)) + Vector3.up;
        int best = 3, bn = -1;
        for (int s = 0; s < 4; s++)
        {
            Vector3 dir = SideDir(s, ax, az), lat = s < 2 ? az : ax;
            float reach = (s < 2 ? it.half.x : it.half.z) + 1f, span = s < 2 ? it.half.z : it.half.x;
            int n = 0;
            for (int k = -3; k <= 3; k++)
            {
                int h = Physics.RaycastNonAlloc(o + lat * (span * 0.85f * k / 3f), dir, sideHits, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int j = 0; j < h; j++)
                {
                    var ht = sideHits[j].transform;
                    if (ignore != null && ht.IsChildOf(ignore)) continue;
                    // 유리문·문틀은 입구다(우체국 Q4_door_glass 가 콜라이더라 입구 쪽을 막힌 벽으로 셌다)
                    var hr = ht.GetComponent<Renderer>();
                    if ((hr != null && IsGlass(hr)) || ht.name.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    blocked = true;
                }
                if (!blocked) n++;
            }
            if (n > bn) { bn = n; best = s; }
        }
        return best;
    }

    /// <summary>
    /// D-307 ⑧: 각도(pitch·yaw) 그대로, 상자 바닥 네 모서리 + 벽 위(천장 컷 ceilY) 네 모서리가 화면 안(한쪽 margin 여백)에 드는 가장 가까운 카메라 자리.
    /// look = 카메라가 보는 점(카메라 = look - 앞 × dist). capFrac = 바닥 가운데에 선 키 capsule 사람이 화면 높이에서 차지하는 비율.
    /// </summary>
    public static void Fit(Matrix4x4 boxInv, float yaw, float pitch, float vfov, float aspect, float margin, float ceilY, float capsule, out Vector3 look, out float dist, out float capFrac)
    {
        var r = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 R = r * Vector3.right, U = r * Vector3.up, F = r * Vector3.forward, o = boxInv.MultiplyPoint3x4(Vector3.zero);
        float tan = Mathf.Tan(vfov * 0.5f * Mathf.Deg2Rad), ty = tan * (1f - 2f * margin), tx = ty * aspect;
        // 점 (x,y,z)(카메라 축 기준, 상자 가운데 원점)가 거리 D 카메라 (px,py) 화면 안: |x - px| <= tx (z + D)
        float ax = float.MinValue, bx = float.MaxValue, ay = float.MinValue, by = float.MaxValue;
        for (int k = 0; k < 8; k++)
        {
            Vector3 q = new Vector3((k & 1) == 0 ? -1f : 1f, (k & 2) == 0 ? -1f : ceilY, (k & 4) == 0 ? -1f : 1f);
            Vector3 d = boxInv.MultiplyPoint3x4(q) - o;
            float x = Vector3.Dot(d, R), y = Vector3.Dot(d, U), z = Vector3.Dot(d, F);
            ax = Mathf.Max(ax, x - tx * z); bx = Mathf.Min(bx, x + tx * z);
            ay = Mathf.Max(ay, y - ty * z); by = Mathf.Min(by, y + ty * z);
        }
        dist = Mathf.Max((ax - bx) / (2f * tx), (ay - by) / (2f * ty));
        look = o + R * ((ax + bx) * 0.5f) + U * ((ay + by) * 0.5f);
        Vector3 cp = look - F * dist, foot = boxInv.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)), head = foot + Vector3.up * capsule;
        float V(Vector3 p) { Vector3 e = p - cp; return Vector3.Dot(e, U) / Mathf.Max(0.01f, Vector3.Dot(e, F)); }
        capFrac = Mathf.Abs(V(head) - V(foot)) / (2f * tan);
    }

    /// <summary>방 i 의 카메라가 보는 점·거리. 맞춘 사람이 너무 작으면(minCapsule) 넓은 곳 = 옛 거리 규칙 + 플레이어 따라 평행이동 → false.</summary>
    bool Aim(int i, out Vector3 look, out float dist)
    {
        var it = rooms[i]; Matrix4x4 inv = it.box.inverse;
        Fit(inv, roomYaw[i], pitch, cam.fieldOfView, cam.aspect, fitMargin, CeilOf(it.box, ceilCut, ceilMetres), capsuleHeight, out look, out dist, out float capF);
        if (capF >= minCapsule) return true;
        look = inv.MultiplyPoint3x4(new Vector3(0f, -0.6f, 0f)); dist = DistFor(i);
        return false;
    }

    void SnapAim() { Aim(cur, out midNow, out distNow); midVel = Vector3.zero; distVel = 0f; }

    // 고정 시점 조작을 놓는다(나감·비킴·꺼짐)
    void Release()
    {
        if (Fixed && foot != null) foot.Pitch = 0f;
        Fixed = false;
        if (body != null) body.ScreenYaw = null;
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
        return Holds(rooms, cur, p) ? cur : -1;
    }

    /// <summary>방 사이 문턱에서 지금 상자를 붙드는 범위(상자 좌표).</summary>
    public const float HoldLim = 1.3f;
    /// <summary>
    /// 지금 상자 cur 를 붙드는가: p 가 cur 에도, 같은 건물 다른 상자에도 HoldLim 안(두 방 사이 문턱).
    /// 과제 20261105-0453 ①: 전엔 「다른 상자 1.3 안」만 봐서 집 1층 벽 밖 1 m(1층 상자 1.16)에 서도 2층 방 디오라마가 남았다.
    /// </summary>
    public static bool Holds(IReadOnlyList<A7ShadeGlobals.Interior> rooms, int cur, Vector3 p)
    {
        if (MaxAbs(rooms[cur].box.MultiplyPoint3x4(p)) > HoldLim) return false;
        for (int i = 0; i < rooms.Count; i++)
            if (i != cur && rooms[i].building == rooms[cur].building && MaxAbs(rooms[i].box.MultiplyPoint3x4(p)) <= HoldLim) return true;
        return false;
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
        if (camTaken != null && camTaken()) { SetRendering(false); RestoreDot(); RestoreHands(); Release(); return; }
        Vector3 probe = player.position + Vector3.up * 1.0f;
        int now = Pick(probe);
        if (now >= 0 && now != cur)
        {
            SetRendering(false);   // 상자 바꿈: 끄고 새 상자로 아래에서 다시 켠다(w 는 그대로 — 상자 → 상자는 안 끊김)
            bool otherHouse = cur < 0 || rooms[now].building != building;
            Select(now);
            // 다른 건물(순간이동)은 마을을 가로질러 미끄러지지 않게 바로 놓는다
            if ((!inRoom && w <= 0f) || otherHouse) SnapAim();
        }
        else if (now >= 0 && !inRoom && w <= 0f) SnapAim();
        if (now >= 0 && !inRoom) { yawNow = roomYaw[cur]; yawVel = 0f; }   // D-306: 들어오는 순간부터 방의 고정 각도
        if (now < 0 && inRoom) Release();   // 나가면 1인칭 — 눈은 몸이 향한 쪽 수평
        inRoom = now >= 0;
        target = inRoom ? 1f : 0f;
        if (!foot.enabled) { ApplyCalendar(); return; }   // 달력·일기·침대 보기 중: 그 장면의 카메라 이동을 건드리지 않는다
        // D-306: 안에선 마우스 시선을 안 쓴다 — 눈은 몸이 향한 쪽 앞 바닥(aimPitch), 걷기는 화면 기준
        Fixed = inRoom;
        if (inRoom) { foot.Yaw = player.eulerAngles.y; foot.Pitch = aimPitch; }

        w = Mathf.MoveTowards(w, target, Time.deltaTime / Mathf.Max(0.01f, blendSec));
        SetRendering(inRoom);   // 나갈 때는 바로 끈다(카메라만 0.4초에 눈으로 돌아온다)
        if (rendering) BodyOn();
        ApplyCalendar();
        if (w <= 0f) { RestoreDot(); RestoreHands(); return; }

        // 1인칭 눈(OnFootCamera 가 이번 프레임에 놓은 자세) 저장 → Gaze 가 여기서 레이를 쏜다
        Transform t = cam.transform;
        Quaternion eyeR = t.rotation;
        eyeP = t.position; eyeF =inRoom ? Quaternion.Euler(aimPitch, player.eulerAngles.y, 0f) * Vector3.forward : t.forward; eyeCam = t; eyeValid = true;

        // D-306: 방의 고정 각도(상자 → 상자로 바뀔 때만 부드럽게 돈다). 몸 방향(foot.Yaw)은 안 따른다
        yawNow = Mathf.SmoothDampAngle(yawNow, roomYaw[cur], ref yawVel, yawSmooth);
        if (inRoom && body != null) body.ScreenYaw = yawNow;
        // 방 바닥 가운데·거리는 상자가 바뀌면 부드럽게 따라간다(한 방 안에선 상수)
        bool fit = Aim(cur, out Vector3 aimLook, out float aimDist);
        midNow = Vector3.SmoothDamp(midNow, aimLook, ref midVel, yawSmooth);
        distNow = Mathf.SmoothDamp(distNow, aimDist, ref distVel, yawSmooth);
        // D-307: 방이 다 보이게 맞춘 자리에 고정. 맞추면 사람이 너무 작은 넓은 곳만 같은 각도로 플레이어 ↔ 방 가운데 사이를 봄(평행이동만)
        Vector3 look = fit ? midNow : Vector3.Lerp(player.position + Vector3.up * 0.9f, midNow, roomPull);
        Quaternion dr = Quaternion.Euler(pitch, yawNow, 0f);
        Vector3 dp = look - dr * Vector3.forward * distNow;
        float s = w * w * (3f - 2f * w);
        t.SetPositionAndRotation(Vector3.Lerp(eyeP, dp, s), Quaternion.Slerp(Quaternion.LookRotation(eyeF), dr, s));
        if (rendering) UpdateGlass();   // 카메라를 디오라마 자리로 옮긴 뒤에 — 눈(방 안) 기준이면 카메라 쪽 벽 유리를 못 자른다
        PlaceHands(eyeR, s);
        MoveDot();
    }

    // 과제 20261105-0453 ②: 달력·침대·일기를 닫으면 EnterFoot → OnFootCamera.OnEnable 이 몸을 그림자만(1인칭)으로 돌린다.
    // SetRendering(true) 은 이미 켜져 있어 다시 안 불려 몸이 안 보였다 → 그리는 동안은 매 프레임 몸을 켠다.
    Renderer[] bodyR;
    void BodyOn()
    {
        if (bodyR == null) bodyR = player.GetComponentsInChildren<Renderer>(true);
        foreach (var r in bodyR) if (r != null && r.shadowCastingMode != ShadowCastingMode.On) r.shadowCastingMode = ShadowCastingMode.On;
    }

    // 과제 20261105-0453 ④: 손에 든 것(카메라 자식 — 문 앞 상자 HeldBox, 보관소 포장대 PackHand)은 1인칭 눈 앞 자리라
    // 디오라마 카메라를 따라가 화면 아래 큰 회색 판이 됐다 → 안에선 몸 앞 가슴 높이로. w 로 눈 자리 ↔ 가슴을 섞고, 나가면 원래 자리로
    static readonly string[] HandNames = { "HeldBox", "PackHand" };
    /// <summary>든 상자 높이(m) — Stage_Door.HeldSize.y 와 같은 값(Visual 은 Stages 를 못 본다).</summary>
    const float HeldBoxHeight = 0.24f;
    readonly Dictionary<Transform, Pose> handLocal = new Dictionary<Transform, Pose>();
    /// <summary>검사용: 지금 가슴 앞으로 옮겨 둔 손 물건 수.</summary>
    public int HandsMoved => handLocal.Count;

    void PlaceHands(Quaternion eyeR, float s)
    {
        var cc = body != null ? body.Controller : null;
        Vector3 chest = player.position + Vector3.up * 1.1f + player.forward * ((cc != null ? cc.radius : 0.3f) + 0.25f);
        // 과제 20261105-0617 ②: 든 상자는 머리 위 살짝 — 가슴 앞이면 카메라를 등지고 걸을 때 몸 캡슐에 가린다(위에서 내려다보는 시점). 포장대 손(PackHand)은 가슴 그대로
        Vector3 over = player.position + Vector3.up * ((cc != null ? cc.height : 1.8f) + HeldBoxHeight * 0.5f + 0.06f) + player.forward * 0.1f;
        foreach (var n in HandNames)
        {
            var h = cam.transform.Find(n);
            if (h == null) continue;
            if (!handLocal.TryGetValue(h, out var lp)) { lp = new Pose(h.localPosition, h.localRotation); handLocal[h] = lp; }
            Vector3 atEye = eyeP + eyeR * lp.position;
            h.SetPositionAndRotation(Vector3.Lerp(atEye, n == "HeldBox" ? over : chest, s),Quaternion.Slerp(eyeR * lp.rotation, player.rotation * lp.rotation, s));
        }
    }

    void RestoreHands()
    {
        if (handLocal.Count == 0) return;
        foreach (var kv in handLocal) if (kv.Key != null) kv.Key.SetLocalPositionAndRotation(kv.Value.position, kv.Value.rotation);
        handLocal.Clear();
    }

    void ApplyCalendar()
    {
        if (calendar != null) calendar.SetDetail(!(rendering && foot.enabled));
    }

    // 조준점 = 눈 레이가 맞은 곳(3 m 안, 없으면 1.5 m 앞)의 화면 위치. D-306 고정 시점: E 대상 위 작은 표시(Gaze.Mark), 없으면 화면 밖
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
        if (Fixed)
        {
            var m = Mark; Vector3 mv = m.HasValue ? cam.WorldToViewportPoint(m.Value) : new Vector3(-1f, -1f, 1f);
            dotRt.anchorMin = dotRt.anchorMax = mv.z > 0f ? new Vector2(mv.x, mv.y) : new Vector2(-1f, -1f); dotRt.anchoredPosition = Vector2.zero;
            return;
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
                if (!IsGlass(r)) continue;   // 과제 20261105-0419: 우체국 유리(Q4_*_glass)도 — 전엔 GLASS_ 만 잡아 하늘색 판이 안 잘렸다
                glass.Add(r); glassSaved.Add(r.sharedMaterial);
                r.sharedMaterial = GlassMat();
            }
            UpdateGlass();
            // 방 밖은 검정: 방 상자와 안 겹치는 렌더러·지형을 끈다. A7 재질은 셰이더가 상자 밖을 버리므로 상자에 걸치면 남긴다. 그 밖(지형 메시·URP Lit …)은 가운데가 상자 안일 때만
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r.forceRenderingOff || r.transform.IsChildOf(player) || glass.Contains(r)) continue;
                bool a7 = IsCutShader(r.sharedMaterial);
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
        float cut = CutLine(box, cam.transform.position, roomFaces[cur], wallCut);
        if (MaxAbs(b) > 1.15f || b.y > Ceil) return true;
        if (Mathf.Abs(c.x) > 1f && b.x * Mathf.Sign(c.x) > cut) return true;
        if (Mathf.Abs(c.z) > 1f && b.z * Mathf.Sign(c.z) > cut) return true;
        return false;
    }

    /// <summary>
    /// 과제 20261105-0535 ②: 카메라 쪽 벽 컷선(상자 좌표) = wallCut, 단 카메라가 넘어선 쪽 벽의 안쪽 면(WallFaces)이 그보다 안이면 그 면 − 0.02.
    /// 다른 방은 안쪽 면 ≥ 0.96 이라 그대로 0.95(바뀐 방 = 집 2층 복도 2F#2 하나, 0.853). 셰이더 _A7Dio.z 와 Cut() 이 같은 값을 쓴다.
    /// </summary>
    public static float CutLine(Matrix4x4 box, Vector3 camPos, Vector4 faces, float wallCut)
    {
        Vector3 c = box.MultiplyPoint3x4(camPos); float t = wallCut;
        if (Mathf.Abs(c.x) > 1f) t = Inside(t, c.x > 0f ? faces.x : faces.y, wallCut);
        if (Mathf.Abs(c.z) > 1f) t = Inside(t, c.z > 0f ? faces.z : faces.w, wallCut);
        return t;
    }

    // 안쪽 면이 컷선 밖(≥ wallCut)이면 이미 잘리니 그대로 — 다른 방 그림이 1 cm 도 안 바뀌게
    static float Inside(float t, float face, float wallCut) => face < wallCut ? Mathf.Min(t, face - 0.02f) : t;

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
        Shader.SetGlobalVector(InQId, grainB ? new Vector4(grainBFilterTexels, 0f, grainBDitherWidth, 0f)
                                             : new Vector4(0f, 0f, g != null ? g.indoorDitherWidth : 0.15f, 0f));
        // y = 해 세기 배수(창빛 = 1 − 안개) · 바탕빛(시간대) w = 그림자에서 지붕을 빼는 집 범위
        ApplyTime();
        Shader.SetGlobalVector(DioId, new Vector4(1f, 1f - Mathf.Clamp01(fog), CutLine(box, cam.transform.position, roomFaces[cur], wallCut), Ceil));
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
