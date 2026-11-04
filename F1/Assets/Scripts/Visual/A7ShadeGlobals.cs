using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 과제 0404(맵 코멘트 03:44·03:46): A7 셰이더(PitCrew/SceneA7/Lit · Library/A7LitPoint)는 그늘 색·3단 문턱을 전역값
/// _A7Shade·_A7T·_A7Mid 에서 읽는다. 그 값을 넣는 건 A7LightToggles 뿐인데, 그건 해(A7_Sun)까지 새로 만들어 씬 빛을 바꾼다.
/// 해가 이미 있는 씬(Map_TownV6)에는 이것만 둔다 — 전역값 셋만 넣고 빛은 안 건드린다. 없으면 그늘 면이 검정(0,0,0)이 된다.
/// 기본값 = Scene_A7_Light 의 A7LightToggles 값과 같은 결과.
/// </summary>
[ExecuteAlways]
public sealed class A7ShadeGlobals : MonoBehaviour
{
    [Range(0.3f, 1f)] public float ambientAmount = 0.62f;
    public Color skyFogColor = new Color(0.8f, 0.87f, 0.93f);
    public Color shadeTeal = new Color(0.55f, 0.76f, 0.80f);
    [Range(0f, 1f)] public float tealAmount = 0.5f;
    [Range(0f, 1f)] public float brightAbove = 0.3f;
    [Range(-0.5f, 0.5f)] public float sideAbove = 0.02f;
    [Range(0f, 1f)] public float sideLevel = 0.6f;

    [Header("실내 도트(과제 20261104-0501) — 방 상자 안 A7 면만 바다 화면 문법으로")]
    [Tooltip("켜면 이름이 roomName 인 오브젝트(집 방 소품 묶음)를 둘러싼 벽 상자 안에서 SceneA7/Lit 이 텍스처를 걸러 읽고, 빛 3단·근거리 안개·밝기 단+디더를 쓴다.")]
    public bool indoorDot = true;
    public string roomName = "ROOM_2F";
    [Range(4, 32)] public int indoorLevels = 10;
    [Range(0f, 0.5f)] public float indoorFogMax = 0.15f;
    public float indoorFogEnd = 6f;
    [Tooltip("텍스처 거르는 탭 간격(텍셀). 0 = 안 거름")] [Range(0f, 4f)] public float indoorFilterTexels = 0.5f;
    [Tooltip("국소 평균 쪽으로 대비 줄임")] [Range(0f, 1f)] public float indoorMeanPull = 0.15f;
    [Tooltip("밝기 단 끝 디더 폭(0..1). 0.15 에서 바다 화면 디더 비율과 비슷(실측 방 0.027 vs 바다 0.021)")] [Range(0f, 1f)] public float indoorDitherWidth = 0.15f;
    [Tooltip("단 자를 때 텍스처 결 빼기. 0 = 결째로 잘라 얼룩이 단으로 뭉개짐")] [Range(0f, 1f)] public float indoorGrainKeep = 0f;

    [Header("방 윤곽선 A/B(과제 20261104-1449, D-297) — 끔 = A(윤곽선 없음) · 켬 = B(깊이·법선 1px 어두운 선)")]
    [Tooltip("켜면 저해상 카메라가 roomName 방 상자 안에 있을 때만 그 집에 1px 어두운 윤곽선을 그린다. 방 밖에선 아무 일도 안 한다. Play 중에만 동작.")]
    public bool roomOutline = false;
    [Tooltip("깊이 끊김 문턱(앞뒤 거리 비율)")] [Range(0.005f, 0.3f)] public float outlineDepth = 0.015f;
    [Tooltip("면 꺾임 문턱(두 법선 dot 이 이보다 작으면 선)")] [Range(0f, 1f)] public float outlineCrease = 0.8f;
    [Tooltip("선 색 = 바탕 × 이 값")] [Range(0f, 1f)] public float outlineDark = 0.15f;
    [Tooltip("선 색을 먹색 쪽으로 섞는 양")] [Range(0f, 1f)] public float outlineInk = 0f;
    RoomOutline outline;
    /// <summary>B안 윤곽선이 지금 걸려 있나(검사·캡처용).</summary>
    public RoomOutline Outline => outline;

    static readonly int ShadeId = Shader.PropertyToID("_A7Shade"), TId = Shader.PropertyToID("_A7T"), MidId = Shader.PropertyToID("_A7Mid");
    static readonly int InBoxId = Shader.PropertyToID("_A7InBox"), InPId = Shader.PropertyToID("_A7InP"), InQId = Shader.PropertyToID("_A7InQ");

    /// <summary>지금 넣은 방 상자(월드 → -1..1). 상자가 없으면 zero.</summary>
    public Matrix4x4 IndoorBox { get; private set; }

    void OnEnable() => Apply();
    void OnValidate() { if (isActiveAndEnabled) Apply(); }
    void OnDisable() { Shader.SetGlobalVector(InPId, Vector4.zero); SetOutline(false); }
    void Update() { if (Application.isPlaying && (outline != null) != roomOutline) SetOutline(roomOutline); }

    public void Apply()
    {
        var shade = Color.Lerp(skyFogColor, shadeTeal, tealAmount) * Mathf.Max(0.3f, ambientAmount);
        Shader.SetGlobalColor(ShadeId, new Color(shade.r, shade.g, shade.b, 1f));
        Shader.SetGlobalVector(TId, new Vector4(brightAbove, sideAbove, 0f, 0f));
        Shader.SetGlobalFloat(MidId, sideLevel);
        ApplyIndoor();
        if (Application.isPlaying) SetOutline(roomOutline);
    }

    void SetOutline(bool on)
    {
        if (outline != null && (!on || outline.Dirty(this))) { outline.Disable(); outline = null; }
        if (!on || outline != null) return;
        var room = FindRoom(roomName);
        if (room == null) return;
        outline = new RoomOutline(room) { depthRatio = outlineDepth, creaseDot = outlineCrease, dark = outlineDark, ink = outlineInk };
        outline.Enable();
    }

    void ApplyIndoor()
    {
        IndoorBox = Matrix4x4.zero;
        var room = indoorDot ? FindRoom(roomName) : null;
        if (room == null || !TryRoomBox(room, out var box)) { Shader.SetGlobalVector(InPId, Vector4.zero); return; }
        IndoorBox = box;
        Shader.SetGlobalMatrix(InBoxId, box);
        Shader.SetGlobalVector(InPId, new Vector4(1f, indoorLevels, indoorFogMax, indoorFogEnd));
        Shader.SetGlobalVector(InQId, new Vector4(indoorFilterTexels, indoorMeanPull, indoorDitherWidth, indoorGrainKeep));
    }

    /// <summary>불변 검사용: 실내 도트가 켜져 있으면 방 상자가 잡혔고 전역값이 들어갔는지.</summary>
    public bool IndoorReady => !indoorDot || (IndoorBox != Matrix4x4.zero && Shader.GetGlobalVector(InPId).x > 0.5f);

    static Transform FindRoom(string n)
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.name == n && t.parent != null) return t;
        return null;
    }

    /// <summary>
    /// 방 상자 = 방 소품 묶음 가운데에서 집 축(부모 = 집) ±x·±z·위·아래로 레이를 쏴 처음 닿는 벽·바닥·천장까지(+6 cm).
    /// 좌표를 손으로 적지 않는다 — 집이 옮겨지거나 돌아가도 따라간다. 한 방향이라도 못 닿으면 상자 없음(꺼짐).
    /// </summary>
    public static bool TryRoomBox(Transform room, out Matrix4x4 worldToBox)
    {
        worldToBox = Matrix4x4.zero;
        bool any = false; var b = new Bounds();
        foreach (var r in room.GetComponentsInChildren<Renderer>())
        {
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        if (!any) return false;
        return TryBoxAt(room.parent, b.center, null, null, out worldToBox, out _);
    }

    /// <summary>이름으로 정해진 방(roomName)과 그 상자 — 실내 도트가 켜져 있으면 넣은 상자, 꺼져 있으면 TryRoomBox(RoomDiorama 와 같은 순서).</summary>
    public static bool NamedRoom(out Transform room, out Matrix4x4 box)
    {
        var g = Object.FindFirstObjectByType<A7ShadeGlobals>();
        room = FindRoom(g != null ? g.roomName : "ROOM_2F"); box = Matrix4x4.zero;
        if (room == null) return false;
        if (g != null && g.IndoorBox != Matrix4x4.zero) { box = g.IndoorBox; return true; }
        return TryRoomBox(room, out box);
    }

    /// <summary>
    /// 점 하나에서 집 축 ±x·±z·위·아래로 레이를 쏴 상자를 잡는다(TryRoomBox 의 몸통).
    /// shell = null 이면 12 m 안에 못 닿는 방향이 있을 때 실패(2층 방 그대로). shell 이 있으면 레이를 그 건물 겉 상자에서 자른다
    /// — 열린 셔터로 빠지는 큰 공간(보관소·정비소)도 건물 끝을 벽으로 쓴다. 그때 벽은 그 건물(house) 콜라이더만(선반·작업대·플레이어는 안 맞는 셈).
    /// </summary>
    public static bool TryBoxAt(Transform house, Vector3 worldCenter, Bounds? shell, Transform ignore, out Matrix4x4 worldToBox, out string why)
    {
        worldToBox = Matrix4x4.zero; why = "";
        Vector3 o = house.InverseTransformPoint(worldCenter);
        var dirs = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        var lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); var hi = -lo;
        float far = shell.HasValue ? Mathf.Max(12f, shell.Value.size.magnitude) : 12f;
        foreach (var d in dirs)
        {
            // 옆으로 ±0.6 m 띄운 레이 셋의 가운데 값 — 소품 하나에 막히거나 문틈 하나로 빠져도 벽을 고른다
            var side = Mathf.Abs(d.x) > 0.5f ? Vector3.forward : Vector3.right;
            var hits = new float[3]; int k = 0;
            foreach (float s in new[] { -0.6f, 0f, 0.6f })
            {
                var wo = house.TransformPoint(o + side * s); var wd = house.TransformDirection(d);
                float dist;
                if (!shell.HasValue && ignore == null)
                {
                    if (!Physics.Raycast(wo, wd, out var hit, 12f, ~0, QueryTriggerInteraction.Ignore)) { why = "레이 " + d + " 12 m 안에 벽 없음"; return false; }
                    dist = hit.distance;
                }
                else
                {
                    dist = Cast(wo, wd, far, ignore, shell.HasValue ? house : null, out var hit2) ? hit2.distance : float.MaxValue;
                    if (shell.HasValue && shell.Value.IntersectRay(new Ray(wo + wd * far, -wd), out var back)) dist = Mathf.Min(dist, far - back);
                    if (dist == float.MaxValue) { why = "레이 " + d + " 벽·건물 끝 없음"; return false; }
                }
                hits[k++] = dist / house.lossyScale.x;
            }
            System.Array.Sort(hits);
            float best = hits[1];
            var p = o + d * (best + 0.06f);
            lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p);
        }
        var c = (lo + hi) * 0.5f; var e = (hi - lo) * 0.5f;
        // 집 로컬 → 상자 -1..1
        worldToBox = Matrix4x4.Scale(new Vector3(1f / e.x, 1f / e.y, 1f / e.z)) * Matrix4x4.Translate(-c) * house.worldToLocalMatrix;
        return true;
    }

    static readonly RaycastHit[] castBuf = new RaycastHit[32];

    /// <summary>가장 가까운 맞은 곳(트리거·ignore 아래 제외, only 가 있으면 그 아래만).</summary>
    static bool Cast(Vector3 o, Vector3 d, float far, Transform ignore, Transform only, out RaycastHit best)
    {
        best = default; bool got = false;
        int n = Physics.RaycastNonAlloc(o, d, castBuf, far, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var h = castBuf[i];
            if (ignore != null && h.transform.IsChildOf(ignore)) continue;
            if (only != null && !h.transform.IsChildOf(only)) continue;
            if (!got || h.distance < best.distance) { best = h; got = true; }
        }
        return got;
    }

    // ── 건물 안 전부 디오라마(과제 20261105-0359, D-304) ──

    /// <summary>들어갈 수 있는 건물 안 하나 = 건물(집 축) + 그 안의 상자. room = 이름으로 정해진 방(ROOM_2F)이면 그 소품 묶음, 아니면 null.</summary>
    public sealed class Interior
    {
        public string name; public Transform building, room; public Vector3 seed;
        public Matrix4x4 box; public bool ok; public string why;
        /// <summary>상자 반폭(m, 월드) — x·z 는 집 축.</summary>
        public Vector3 half;
        public int samples;
    }

    /// <summary>건물 폴더 이름 — 이 아래 켜진 자식 하나가 건물 하나다.</summary>
    public const string BuildingsRoot = "D198_Buildings";

    /// <summary>
    /// 찾는 규칙(손 목록 없음): D198_Buildings 아래 켜진 건물마다, 1 m 격자 기둥으로 위에서 아래로 바닥(위를 보는 면)을 훑고
    /// 바닥 위 1 m 점이 「위로 그 건물 천장(0.8~8 m)이 있고 집 축 네 옆이 모두 그 건물 벽에 닿으면」 건물 안 점이다.
    /// 건물 안 점을 층·가운데 가까운 순으로 묶어 TryBoxAt 상자 하나씩 — 상자에 든 점은 그 방. 점 6개 미만·반폭 0.8 m(높이 0.9 m) 미만 상자는 틈으로 보고 버린다.
    /// 이름으로 정해진 방(roomName, 2층 방)은 맨 앞에 그 방 상자 그대로(namedBox) 두고, 그 상자 안 점은 다시 묶지 않는다.
    /// </summary>
    public static List<Interior> FindInteriors(Transform ignore)
    {
        var list = new List<Interior>();
        if (NamedRoom(out var named, out var namedBox))
            list.Add(new Interior { name = named.parent.name + "/" + named.name, building = named.parent, room = named, box = namedBox, ok = true, seed = namedBox.inverse.MultiplyPoint3x4(Vector3.zero), half = HalfOf(namedBox, named.parent), samples = -1 });
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t.name != BuildingsRoot) continue;
            foreach (Transform bld in t)
                if (bld.gameObject.activeInHierarchy) ScanBuilding(bld, list, ignore);
        }
        return list;
    }

    static Vector3 HalfOf(Matrix4x4 box, Transform house)
    {
        var inv = box.inverse;
        return new Vector3(inv.MultiplyVector(Vector3.right).magnitude, inv.MultiplyVector(Vector3.up).magnitude, inv.MultiplyVector(Vector3.forward).magnitude);
    }

    static float MaxAbs(Vector3 q) => Mathf.Max(Mathf.Abs(q.x), Mathf.Max(Mathf.Abs(q.y), Mathf.Abs(q.z)));

    static void ScanBuilding(Transform bld, List<Interior> list, Transform ignore)
    {
        bool any = false; var B = new Bounds();
        foreach (var c in bld.GetComponentsInChildren<Collider>())
        {
            if (!c.enabled || c.isTrigger) continue;
            if (!any) { B = c.bounds; any = true; } else B.Encapsulate(c.bounds);
        }
        if (!any) return;
        bool Own(RaycastHit h) => h.transform.IsChildOf(bld);
        float side = B.size.magnitude;
        var axes = new[] { bld.right, -bld.right, bld.forward, -bld.forward };
        var pts = new List<Vector3>();
        for (float x = B.min.x + 0.5f; x < B.max.x - 0.3f; x += 1f)
        for (float z = B.min.z + 0.5f; z < B.max.z - 0.3f; z += 1f)
        {
            float y = B.max.y + 0.5f;
            for (int step = 0; step < 8; step++)
            {
                if (!Cast(new Vector3(x, y, z), Vector3.down, y - B.min.y + 1f, ignore, null, out var fl)) break;
                y = fl.point.y - 0.05f;
                if (fl.normal.y < 0.7f || !(Own(fl) || fl.collider is TerrainCollider)) continue;   // 선반·작업대 윗면은 바닥이 아니다
                var p = fl.point + Vector3.up;
                if (!Cast(p, Vector3.up, 8f, ignore, bld, out var up) || up.distance < 0.8f) continue;
                bool walls = true;
                foreach (var a in axes) if (!Cast(p, a, side, ignore, bld, out _)) { walls = false; break; }
                if (walls) pts.Add(p);
            }
        }
        var center = B.center;
        pts.Sort((a, b) =>
        {
            int fy = Mathf.RoundToInt(a.y / 1.5f).CompareTo(Mathf.RoundToInt(b.y / 1.5f));
            if (fy != 0) return fy;
            return (new Vector2(a.x - center.x, a.z - center.z)).sqrMagnitude.CompareTo((new Vector2(b.x - center.x, b.z - center.z)).sqrMagnitude);
        });
        var done = new bool[pts.Count];
        foreach (var it in list)
            if (it.building == bld && it.ok)
                for (int i = 0; i < pts.Count; i++) if (MaxAbs(it.box.MultiplyPoint3x4(pts[i])) <= 1.05f) done[i] = true;
        int floor = 0; float lastY = float.NegativeInfinity;
        for (int i = 0; i < pts.Count; i++)
        {
            if (done[i]) continue;
            var it = new Interior { building = bld, seed = pts[i] };
            it.ok = TryBoxAt(bld, pts[i], B, ignore, out it.box, out it.why);
            if (!it.ok)
            {
                for (int j = i; j < pts.Count; j++) if ((pts[j] - pts[i]).sqrMagnitude < 2.25f) done[j] = true;
                it.name = bld.name + "/?" + pts[i].ToString("F0"); list.Add(it);
                continue;
            }
            int n = 0;
            for (int j = i; j < pts.Count; j++) if (!done[j] && MaxAbs(it.box.MultiplyPoint3x4(pts[j])) <= 1.05f) { done[j] = true; n++; }
            done[i] = true;
            it.samples = n; it.half = HalfOf(it.box, bld);
            if (n < 6 || it.half.x < 0.8f || it.half.z < 0.8f || it.half.y < 0.9f) continue;   // 틈·벽장
            if (pts[i].y - lastY > 1.5f) { floor++; lastY = pts[i].y; }
            it.name = bld.name + "/" + floor + "F#" + list.FindAll(o => o.building == bld).Count;
            list.Add(it);
        }
    }
}
