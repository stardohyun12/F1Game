using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-236 노을 낚시 자리 연출물 — 수평선에 「보는 곳」을 둔다: 먼 곶·섬, 곶 끝 등대, 작은 배. 전부 단색 실루엣 판(DuskSilhouette).
///  · 아트 에셋이 아니다(D-188 대상 아님): 이 자리에서 보이는 외곽선만 코드로 그린 판이고, 노을(SunsetLook.backdrop)일 때만 켜진다.
///  · 판은 자리(origin)를 향해 선다. 생성물은 씬에 저장 안 함(DontSave) — 켤 때마다 값으로 다시 만든다.
///  · 위치는 자리 기준 방위(°, 0 = +z)·거리(m). 해 방위(SunsetLook.sunAzimuth)를 같이 보며 놓는다.
/// </summary>
[ExecuteAlways]
public sealed class DuskBackdrop : MonoBehaviour
{
    public enum Kind { Headland, Island, Lighthouse, Boat }

    [System.Serializable]
    public struct Piece
    {
        public Kind kind;
        [Tooltip("자리 기준 방위(°, 0 = +z, 90 = +x)")] public float azimuth;
        public float distance;
        [Tooltip("판 너비(m) — 등대·배는 배율(1 = 기본 크기)")] public float width;
        public float height;
        [Tooltip("곶: 1 = 끝(절벽)이 오른쪽, -1 = 왼쪽")] public float tipSide;
        [Range(0f, 1f)] public float haze;
        public float baseY;
        public int seed;
    }

    public Transform origin;
    public Material material;
    public List<Piece> pieces = new List<Piece>();

    [Header("D-236 8판 생기 — 등대 불 · 배 · 갈매기(전부 같은 실루엣 소재, 색만 프로퍼티 블록)")]
    public Color lampColor = new Color(1.00f, 0.86f, 0.52f);
    [Tooltip("등대 불: x 주기(s) · y 켜진 시간(s) · z 불 크기(m, 420 m 에서 ≈2~3px)")]
    public Vector3 lampBlink = new Vector3(5f, 1.1f, 4.5f);
    [Tooltip("배가 옆으로 오가는 속도(m/s)와 폭(m) — 170 m 에서 1px ≈ 0.4 m 라 12 s 에 약 5px")]
    public Vector2 boatDrift = new Vector2(0.18f, 40f);
    [Tooltip("갈매기: x 마릿수 · y 거리(m) · z 높이(m, 수면 위) · w 속도(m/s)")]
    public Vector4 gulls = new Vector4(3f, 35f, 8f, 5f);
    [Tooltip("갈매기가 지나는 방위 범위(°, 자리 정면 기준, 오른쪽 +). 기본 = 바다 위 하늘(왼쪽 곶 나무 뒤 피함)")]
    public Vector2 flightArc = new Vector2(-6f, 48f);
    public Color gullColor = new Color(0.16f, 0.12f, 0.22f);
    [Tooltip("갈매기 소리(CoastAmbience) — 울 때 날고 있지 않으면 한 무리를 띄운다(소리와 대략 맞춤). 비면 gullEvery 초마다.")]
    public CoastAmbience ambience;
    public Vector2 gullEvery = new Vector2(22f, 38f);
    [Tooltip("켜진 뒤 첫 무리까지(s)")]
    public float firstGull = 2.5f;

    readonly List<GameObject> built = new List<GameObject>();
    static readonly int HazeId = Shader.PropertyToID("_Haze"), ColorId = Shader.PropertyToID("_Color");
    Renderer lamp; Transform boat; Vector3 boatBase, boatSide;
    readonly List<Transform> gullT = new List<Transform>(); Mesh gullUp, gullFlat;
    float flightT0 = -1f, flightNext; Vector3 flightA, flightB; int lastCalls;
    public int Flights { get; private set; }   // 검증용
    public bool LampOn => lamp != null && lamp.enabled;

    void OnEnable() { Rebuild(); flightT0 = -1f; flightNext = Now + firstGull; lastCalls = ambience != null ? ambience.GullCalls : 0; }
    void OnDisable() => Clear();
    static float Now => Application.isPlaying ? Time.time : (float)UnityEditor_Time();
    static double UnityEditor_Time()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorApplication.timeSinceStartup;
#else
        return Time.realtimeSinceStartup;
#endif
    }
    void OnValidate() { if (isActiveAndEnabled) Rebuild(); }

    void Clear()
    {
        foreach (var g in built) if (g != null) { if (Application.isPlaying) Destroy(g); else DestroyImmediate(g); }
        built.Clear(); gullT.Clear(); lamp = null; boat = null;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i).gameObject;
            if ((c.hideFlags & HideFlags.DontSave) != 0) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
        }
    }

    public void Rebuild()
    {
        Clear();
        if (material == null) return;
        Vector3 o = origin != null ? origin.position : transform.position; o.y = 0f;
        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            float a = p.azimuth * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            var g = new GameObject("Dusk_" + p.kind + "_" + i) { hideFlags = HideFlags.DontSave };
            g.transform.SetParent(transform, false);
            g.transform.SetPositionAndRotation(o + dir * p.distance + Vector3.up * p.baseY, Quaternion.LookRotation(dir, Vector3.up));
            var mesh = new Mesh { name = g.name, hideFlags = HideFlags.DontSave };
            var v = new List<Vector3>(); var t = new List<int>();
            switch (p.kind)
            {
                case Kind.Headland: Ridge(v, t, p, true); break;
                case Kind.Island: Ridge(v, t, p, false); break;
                case Kind.Lighthouse: Lighthouse(v, t, Mathf.Max(0.1f, p.width)); break;
                case Kind.Boat: Boat(v, t, Mathf.Max(0.1f, p.width)); break;
            }
            mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            var mpb = new MaterialPropertyBlock(); mpb.SetFloat(HazeId, p.haze); r.SetPropertyBlock(mpb);
            built.Add(g);
            if (p.kind == Kind.Lighthouse && lamp == null)
            {
                // 등불 칸(12.4~14.4 m) 앞 0.6 m 에 작은 판 — 탑보다 조금 넓게
                float s = Mathf.Max(0.1f, p.width), w = lampBlink.z * 0.5f;
                var lg = Flat(g.transform, "Lamp", new[] { new Vector3(-w, 12.3f * s, 0), new Vector3(w, 12.3f * s, 0), new Vector3(w, 12.3f * s + lampBlink.z * 0.75f, 0), new Vector3(-w, 12.3f * s + lampBlink.z * 0.75f, 0) },
                    new[] { 0, 2, 1, 0, 3, 2 }, lampColor, 0.05f);
                lg.transform.localPosition = new Vector3(0f, 0f, -0.6f);
                lamp = lg.GetComponent<Renderer>();
            }
            if (p.kind == Kind.Boat && boat == null) { boat = g.transform; boatBase = boat.position; boatSide = boat.right; }
        }
        BuildGulls(o);
    }

    GameObject Flat(Transform parent, string name, Vector3[] v, int[] t, Color c, float haze)
    {
        var g = new GameObject(name) { hideFlags = HideFlags.DontSave };
        g.transform.SetParent(parent, false);
        var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave }; mesh.vertices = v; mesh.triangles = t; mesh.RecalculateBounds();
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        var mpb = new MaterialPropertyBlock(); mpb.SetFloat(HazeId, haze); mpb.SetColor(ColorId, c); r.SetPropertyBlock(mpb);
        return g;
    }

    // 갈매기 = 날개 편 폭 1.3 m 의 얇은 실루엣. 두 꼴(날개 위 V / 평평한 활)을 번갈아 = 날갯짓 2 프레임. 80 m 에서 ≈7 px
    void BuildGulls(Vector3 o)
    {
        gullUp = GullMesh(true); gullFlat = GullMesh(false);
        for (int i = 0; i < Mathf.RoundToInt(gulls.x); i++)
        {
            var g = Flat(transform, "Dusk_Gull_" + i, new Vector3[0], new int[0], gullColor, 0.12f);
            g.GetComponent<MeshFilter>().sharedMesh = gullUp;
            g.SetActive(false);
            built.Add(g); gullT.Add(g.transform);
        }
    }

    static Mesh GullMesh(bool up)
    {
        // 몸 가운데(0,0), 날개 끝 ±0.65. up = 어깨가 올라간 V(끝이 위), flat = 어깨 위·끝이 살짝 아래(활)
        float sh = up ? 0.10f : 0.06f, tip = up ? 0.32f : -0.08f, th = 0.06f;
        var v = new[]
        {
            new Vector3(-0.65f, tip, 0), new Vector3(-0.28f, sh, 0), new Vector3(-0.28f, sh - th, 0),
            new Vector3(0f, 0.02f, 0), new Vector3(0f, -0.06f, 0),
            new Vector3(0.28f, sh, 0), new Vector3(0.28f, sh - th, 0), new Vector3(0.65f, tip, 0),
        };
        var t = new[] { 0, 1, 2, 1, 3, 4, 1, 4, 2, 3, 5, 6, 3, 6, 4, 5, 7, 6 };
        var m = new Mesh { name = up ? "GullUp" : "GullFlat", hideFlags = HideFlags.DontSave }; m.vertices = v; m.triangles = t; m.RecalculateBounds();
        return m;
    }

    void Update()
    {
        float now = Now;
        // 등대: lampBlink.x 초마다 lampBlink.y 초 켬(계단 — 서서히 밝아지지 않음)
        if (lamp != null) { bool on = (now % lampBlink.x) < lampBlink.y; if (lamp.enabled != on) lamp.enabled = on; }
        // 배: 옆으로 아주 천천히 오감
        if (boat != null && boatDrift.x > 0f) boat.position = boatBase + boatSide * (Mathf.PingPong(now * boatDrift.x, boatDrift.y) - boatDrift.y * 0.5f);
        Gulls(now);
    }

    void Gulls(float now)
    {
        if (gullT.Count == 0) return;
        bool flying = flightT0 >= 0f;
        int calls = ambience != null ? ambience.GullCalls : 0;
        bool called = calls != lastCalls; lastCalls = calls;
        if (!flying && (called || now >= flightNext)) StartFlight(now);
        if (flightT0 < 0f) return;
        float dur = Vector3.Distance(flightA, flightB) / Mathf.Max(0.5f, gulls.w);
        float u = (now - flightT0) / dur;
        if (u > 1f) { flightT0 = -1f; flightNext = now + Random.Range(gullEvery.x, gullEvery.y); foreach (var g in gullT) g.gameObject.SetActive(false); return; }
        var cam = Camera.main; Vector3 ce = cam != null ? cam.transform.position : flightA;
        Vector3 dir = (flightB - flightA).normalized;
        for (int i = 0; i < gullT.Count; i++)
        {
            var g = gullT[i];
            // 무리: 뒤로 4~9 m · 위아래 1~2 m 어긋남 · 각자 날갯짓 박자(펄럭 1.4 s ↔ 활공 1.8 s, 펄럭 중엔 0.18 s 마다 꼴 바꿈)
            float h = DuskHash(i, 7);
            Vector3 p = Vector3.Lerp(flightA, flightB, u) - dir * (i * (4f + 5f * h)) + Vector3.up * ((i % 2 == 0 ? 1f : -1f) * (0.8f + 1.4f * h) + 0.6f * Mathf.Sin(now * 0.7f + i * 2f));
            g.position = p;
            g.rotation = Quaternion.LookRotation(p - ce, Vector3.up);
            float ph = (now + h * 3f) % 3.2f;
            var mesh = ph < 1.4f ? (Mathf.FloorToInt(ph / 0.18f) % 2 == 0 ? gullUp : gullFlat) : gullFlat;
            var mf = g.GetComponent<MeshFilter>(); if (mf.sharedMesh != mesh) mf.sharedMesh = mesh;
            if (!g.gameObject.activeSelf) g.gameObject.SetActive(true);
        }
    }

    static float DuskHash(int s, int i) => Hash(s + 11, i);

    void StartFlight(float now)
    {
        // 자리(origin) 정면 기준 방위 flightArc.x° → flightArc.y°(오른쪽 +) 를 거리 gulls.y 에서 가로지른다 — 나무(왼쪽 곶) 뒤가 아니라 바다 위 하늘로.
        // 번갈아 반대 방향. 높이 gulls.z(± 20%)
        Vector3 o = origin != null ? origin.position : transform.position;
        Vector3 f = origin != null ? origin.forward : transform.forward; f.y = 0f; f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
        float d = gulls.y * Random.Range(0.9f, 1.1f), y = gulls.z * Random.Range(0.8f, 1.2f);
        Vector3 At(float az) { var v = Quaternion.AngleAxis(az, Vector3.up) * f; return new Vector3(o.x, y, o.z) + v * d; }
        bool rev = Flights % 2 == 1;
        flightA = At(rev ? flightArc.x : flightArc.y); flightB = At(rev ? flightArc.y : flightArc.x) + Vector3.up * Random.Range(-1.5f, 1.5f);
        flightT0 = now; Flights++;
    }

    // 사각(아래 두 점 x0b·x1b @ y0, 위 두 점 x0t·x1t @ y1)
    static void Quad(List<Vector3> v, List<int> t, float x0b, float x1b, float y0, float x0t, float x1t, float y1)
    {
        int b = v.Count;
        v.Add(new Vector3(x0b, y0, 0)); v.Add(new Vector3(x1b, y0, 0)); v.Add(new Vector3(x1t, y1, 0)); v.Add(new Vector3(x0t, y1, 0));
        t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b); t.Add(b + 3); t.Add(b + 2);
    }

    static float Hash(int s, int i) { uint h = (uint)(s * 73856093) ^ (uint)(i * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (h & 0xffff) / 65535f; }

    // 곶 = 한쪽에서 완만히 올라 봉우리 두어 개, 끝은 짧은 절벽 / 섬 = 양쪽 완만한 혹
    static void Ridge(List<Vector3> v, List<int> t, Piece p, bool headland)
    {
        const int n = 28;
        float w = p.width, h = p.height, side = p.tipSide >= 0 ? 1f : -1f;
        float prevX = 0, prevY = 0;
        for (int i = 0; i <= n; i++)
        {
            float u = i / (float)n;   // 0 = 뭍 쪽, 1 = 끝
            float env;
            if (headland) env = Mathf.SmoothStep(0.55f, 1f, Mathf.Min(1f, u * 3f)) * (u < 0.86f ? 1f - 0.35f * u : Mathf.Lerp(0.70f, 0.0f, (u - 0.86f) / 0.14f));
            else env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u)) * (0.85f + 0.15f * Mathf.Sin(u * 9f));
            float bump = 0.82f + 0.18f * Hash(p.seed, i) + 0.12f * Mathf.Sin(u * 11f + p.seed);
            float y = Mathf.Max(0f, h * env * bump);
            if (i == n) y = 0f;
            float x = (u - 0.5f) * w * side;
            if (i > 0)
            {
                // 세로 띠 하나: 아래 -3 m(물 밑) ~ 위는 두 점을 잇는 비탈
                int b = v.Count;
                v.Add(new Vector3(prevX, -3f, 0)); v.Add(new Vector3(x, -3f, 0)); v.Add(new Vector3(x, y, 0)); v.Add(new Vector3(prevX, prevY, 0));
                t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b); t.Add(b + 3); t.Add(b + 2);
            }
            prevX = x; prevY = y;
        }
    }

    // 등대(기본 높이 ≈ 16 m): 좁아지는 탑 · 난간 · 등불 칸 · 지붕
    static void Lighthouse(List<Vector3> v, List<int> t, float s)
    {
        Quad(v, t, -1.7f * s, 1.7f * s, -1f, -1.15f * s, 1.15f * s, 11.5f * s);
        Quad(v, t, -1.75f * s, 1.75f * s, 11.5f * s, -1.75f * s, 1.75f * s, 12.4f * s);
        Quad(v, t, -0.85f * s, 0.85f * s, 12.4f * s, -0.85f * s, 0.85f * s, 14.4f * s);
        Quad(v, t, -1.25f * s, 1.25f * s, 14.4f * s, 0f, 0f, 16.0f * s);
    }

    // 작은 고깃배(기본 길이 ≈ 7 m): 선체 · 조타실 · 돛대
    static void Boat(List<Vector3> v, List<int> t, float s)
    {
        Quad(v, t, -2.6f * s, 2.9f * s, -0.4f, -3.4f * s, 3.6f * s, 1.1f * s);
        Quad(v, t, -2.2f * s, -0.4f * s, 1.1f * s, -2.1f * s, -0.5f * s, 2.9f * s);
        Quad(v, t, 0.9f * s, 1.3f * s, 1.1f * s, 0.95f * s, 1.25f * s, 5.6f * s);
    }
}
