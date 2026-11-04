using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// D-301 포장 3D 작업대: PackBoard(판 좌표 자료)를 기울인 원근 카메라 아래 두께 있는 물건으로 그린다 — 전부 코드로 만든 모양·저해상 절차 질감(AI 그림·3D 없음).
/// 나무 작업대 윗면(널 결) · 가운데 자리는 마스킹 테이프 테두리. 상자 = 바닥·벽 4·안쪽 어두운 면·날개 4(열면 바깥으로 젖혀짐, 테이프를 대면 닫힘) ·
/// 롤 = 원통(남은 장 수만큼 가늘어짐) + 앞으로 풀려 나온 한 장 · 테이프 = 디스펜서 · 알갱이 = 뚜껑 없는 통 · 물건 = 높이 있는 덩어리(달걀판·병·상자).
/// 감싼 뽁뽁이 = 물건 둘레 반투명 겹 · 구긴 종이 = 울퉁불퉁한 공 · 알갱이 = 작은 공(인스턴싱 한 번에 최대 1023개, 그림자 있음).
/// 빛: 작업대 위 따뜻한 전등 + 옆 창빛(둘 다 실시간 그림자) — 보드가 켜진 동안만. 조각 자료는 읽기만 한다(물리·규칙 값 변동 0).
/// </summary>
public class PackBoardView : MonoBehaviour
{
    PackBoard board;
    Transform root;
    TMP_FontAsset font;
    Quaternion flat;
    PackBoardFx fx;
    PackTutorial tut;
    /// <summary>QA: 첫 포장 튜토리얼(D-301).</summary>
    public PackTutorial Tutorial => tut;
    /// <summary>QA: 마지막 프레임에 알갱이를 그리는 데 쓴 CPU 시간(ms).</summary>
    public float DrawMs { get; private set; }

    public static PackBoardView Create(PackBoard b, TMP_FontAsset f)
    {
        var v = b.gameObject.AddComponent<PackBoardView>();
        v.board = b; v.font = f;
        return v;
    }

    // ── 색(SPEC §1: 적은 색, 거친 결) ──
    static readonly Color cardC = new Color(0.74f, 0.55f, 0.34f), cardInC = new Color(0.46f, 0.33f, 0.20f), flapC = new Color(0.80f, 0.61f, 0.39f),
        bubbleC = new Color(0.62f, 0.84f, 1.00f, 0.50f), bubble2C = new Color(0.80f, 0.93f, 1.00f, 0.60f), paperC = new Color(0.94f, 0.90f, 0.79f),
        tapeC = new Color(0.82f, 0.63f, 0.36f), maskC = new Color(0.93f, 0.89f, 0.72f), binC = new Color(0.34f, 0.45f, 0.56f), beadC = new Color(0.97f, 0.97f, 0.94f),
        coreC = new Color(0.42f, 0.31f, 0.19f), dispC = new Color(0.19f, 0.20f, 0.22f), pulpC = new Color(0.64f, 0.60f, 0.53f), eggC = new Color(0.94f, 0.81f, 0.65f),
        glassC = new Color(0.40f, 0.66f, 0.50f, 0.80f), crateC = new Color(0.66f, 0.48f, 0.30f), labelC = new Color(0.16f, 0.12f, 0.10f);

    // ── 높이(m, 판 윗면 기준) ──
    /// <summary>나무 덧판 윗면 · 상자 판지 두께 · 상자 안 완충재 한 장 두께 · 들어 올린 높이(옛 그림과 같음).</summary>
    const float Bench = 0.004f, T = 0.006f, SheetH = 0.01f, Lift = 0.07f, BoxS = 0.6f;
    const float BinH = 0.07f, TapeH = 0.085f, ScoopH = 0.03f;

    readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    readonly Dictionary<string, Texture2D> texs = new Dictionary<string, Texture2D>();

    Material Mat(Color c) => Mat(c, null, 0f);

    Material Mat(Color c, string tex, float smooth)
    {
        string key = c.r.ToString("F3") + c.g.ToString("F3") + c.b.ToString("F3") + c.a.ToString("F3") + "|" + tex + "|" + smooth;
        if (mats.TryGetValue(key, out var m)) return m;
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(sh) { name = "pack3d(runtime)", enableInstancing = true };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (tex != null) { var t = TexOf(tex); if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t); m.mainTexture = t; }
        if (c.a < 0.99f)                                                     // 반투명(뽁뽁이·유리)
        {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        return mats[key] = m;
    }

    static float Hash(int x, int y, int s) { float h = Mathf.Sin(x * 12.9898f + y * 78.233f + s * 37.719f) * 43758.5453f; return h - Mathf.Floor(h); }

    /// <summary>저해상 절차 질감(점 필터 — 화면 360 줄에서 거친 결로 보인다). 회색조 · 재질 색이 곱해진다(나무만 제 색).</summary>
    Texture2D TexOf(string k)
    {
        if (texs.TryGetValue(k, out var t)) return t;
        int w = k == "wood" ? 128 : 16, h = k == "wood" ? 48 : 16;
        t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, name = "pack3d " + k };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = Hash(x, y, k.Length), c = 1f, a = 1f;
                switch (k)
                {
                    case "wood":                                             // 널 4장(가로로 긴 결) · 이음매 어둡게 · 결 3톤
                    {
                        int pl = y / 12;
                        float g = 0.5f + 0.5f * Mathf.Sin(x * 0.19f + pl * 2.3f + 2.6f * Mathf.Sin(x * 0.043f + pl * 1.7f) + (y % 12) * 0.55f);
                        c = g > 0.78f ? 0.80f : g < 0.22f ? 1.05f : 0.93f;
                        c *= 0.92f + 0.10f * Hash(pl, 0, 9);
                        if (y % 12 == 0) c = 0.55f;
                        if (n > 0.93f) c *= 0.9f;
                        px[y * w + x] = new Color(0.62f * c, 0.43f * c, 0.26f * c, 1f);
                        continue;
                    }
                    case "card": c = (x % 4 == 0 ? 0.92f : 1f) * (n > 0.86f ? 0.9f : n < 0.07f ? 1.05f : 1f); break;   // 골판지 골 · 얼룩
                    case "paper": c = n > 0.9f ? 0.9f : 1f; break;
                    case "tape": c = (y % 5 == 0 ? 0.93f : 1f) * (n > 0.92f ? 0.94f : 1f); break;
                    case "bead": c = n > 0.75f ? 0.80f : n > 0.5f ? 0.92f : 1f; break;   // 알갱이 더미 윗면
                    case "bubble":                                           // 4×4 칸마다 볼록 뽁 하나(밝은 꼭지 · 테두리)
                    {
                        float cx = x % 4 - 1.5f, cy = y % 4 - 1.5f, r = Mathf.Sqrt(cx * cx + cy * cy);
                        if (r < 0.8f) { c = 1.15f; a = 1f; }
                        else if (r < 1.6f) { c = 0.82f; a = 0.85f; }
                        else { c = 1f; a = 0.55f; }
                        break;
                    }
                }
                px[y * w + x] = new Color(c, c, c, a);
            }
        t.SetPixels(px); t.Apply(false);
        return texs[k] = t;
    }

    // ── 공 메시(알갱이 · 구긴 종이) ──
    static Mesh Ball(int seg, int rings, int seed, float lump)
    {
        var vs = new List<Vector3>(); var tri = new List<int>();
        for (int j = 0; j <= rings; j++)
        {
            float th = Mathf.PI * j / rings;
            for (int i = 0; i <= seg; i++)
            {
                float ph = Mathf.PI * 2f * i / seg;
                var dir = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                float r = 0.5f;
                if (lump > 0f)                                               // 방향의 연속 함수라 이음매가 벌어지지 않는다
                {
                    float s = seed * 1.7f;
                    float nz = Mathf.Sin(Vector3.Dot(dir, new Vector3(0.8f, 0.3f, -0.5f)) * 5f + s) + 0.6f * Mathf.Sin(Vector3.Dot(dir, new Vector3(-0.2f, 0.9f, 0.4f)) * 7f + 2f * s)
                        + 0.35f * Mathf.Sin(Vector3.Dot(dir, new Vector3(0.5f, -0.4f, 0.75f)) * 11f + 3f * s);
                    r *= 1f + lump * nz / 1.95f;
                }
                vs.Add(dir * r);
            }
        }
        for (int j = 0; j < rings; j++)
            for (int i = 0; i < seg; i++)
            {
                int a = j * (seg + 1) + i, b = a + seg + 1;
                tri.Add(a); tri.Add(a + 1); tri.Add(b);
                tri.Add(b); tri.Add(a + 1); tri.Add(b + 1);
            }
        var m = new Mesh { name = lump > 0f ? "pack3d wad" : "pack3d bead" };
        m.SetVertices(vs); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    Mesh beadMesh; Material beadMat;
    readonly Mesh[] wadMesh = new Mesh[4];

    Transform Prim(PrimitiveType s, string n, Transform parent, Material m) => PackBoardFx.Prim(s, n, parent, m);
    static Transform Node(string n, Transform parent) { var t = new GameObject(n).transform; t.SetParent(parent, false); return t; }
    static void SetAct(Transform t, bool on) { if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on); }

    Transform Wad(string n, Transform parent, int seed)
    {
        var g = new GameObject(n);
        g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = wadMesh[seed & 3];
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = Mat(paperC, "paper", 0f);
        r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
        return g.transform;
    }

    TextMeshPro Text(string n, float size)
    {
        var g = new GameObject(n);
        g.transform.SetParent(root, false);
        var t = g.AddComponent<TextMeshPro>();
        if (font != null) t.font = font;
        t.fontSize = size;                                                // 월드 글자: 0.5 ≈ 글자 높이 4.5 cm
        t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.NoWrap;
        t.color = labelC;
        t.rectTransform.sizeDelta = new Vector2(0.5f, 0.2f);
        return t;
    }

    Vector3 W(float u, float v, float y) => board.Origin + board.Right * u + board.Up * v + Vector3.up * y;

    // ── 판·빛 ──
    void BuildAll()
    {
        root = new GameObject("PackBoard3D_View").transform;                  // 장면 맨 위 — 판을 켤 때 숨기는 포장대 자식들(HideWorld)과 섞이지 않게
        flat = Quaternion.LookRotation(board.Up, Vector3.up);
        beadMesh = Ball(8, 6, 0, 0f);
        for (int i = 0; i < wadMesh.Length; i++) wadMesh[i] = Ball(12, 9, i + 1, 0.55f);
        beadMat = Mat(beadC, null, 0.1f);

        var top = Prim(PrimitiveType.Cube, "작업대 나무 덧판", root, Mat(Color.white, "wood", 0.05f));   // 진짜 판(공유 회색 대역 .mat)은 그대로 두고 그 위에 덮는다
        top.SetPositionAndRotation(W(0f, 0f, Bench - 0.015f), flat); top.localScale = new Vector3(2.12f, 0.03f, 0.94f);
        float sw = PackBoard.SlotW + 0.03f, sd = PackBoard.SlotD + 0.03f, mw = 0.02f;   // 가운데 자리 = 마스킹 테이프 네모
        for (int k = 0; k < 4; k++)
        {
            var s = Prim(PrimitiveType.Cube, "자리 테이프", root, Mat(maskC, "tape", 0.2f));
            s.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            bool hz = k < 2; float sg = k % 2 == 0 ? -1f : 1f;
            s.SetPositionAndRotation(W(PackBoard.SlotU + (hz ? 0f : sg * sw * 0.5f), PackBoard.SlotV + (hz ? sg * sd * 0.5f : 0f), Bench + 0.0006f), flat);
            s.localScale = hz ? new Vector3(sw + mw, 0.0012f, mw) : new Vector3(mw, 0.0012f, sd + mw);
        }

        // 빛: 작업대 위 전등(따뜻) + 옆 창빛(차갑게, 약하게) — 둘 다 실시간 부드러운 그림자
        Lamp("작업대 전등", W(-0.05f, 0.25f, 1.15f), W(-0.05f, 0f, 0f), new Color(1f, 0.86f, 0.66f), 3.2f, 100f, 3.5f);
        Lamp("창빛", W(1.7f, 0.9f, 1.3f), W(0.1f, 0f, 0f), new Color(0.74f, 0.84f, 1f), 2.4f, 55f, 5f);

        fx = new PackBoardFx(board, root, Mat);
        tut = new PackTutorial(board, root, font);
    }

    void Lamp(string n, Vector3 at, Vector3 look, Color c, float intensity, float angle, float range)
    {
        var g = new GameObject(n);
        g.transform.SetParent(root, false);
        g.transform.SetPositionAndRotation(at, Quaternion.LookRotation(look - at));
        var l = g.AddComponent<Light>();
        l.type = LightType.Spot; l.color = c; l.intensity = intensity; l.range = range; l.spotAngle = angle; l.innerSpotAngle = angle * 0.55f;
        l.shadows = LightShadows.Soft; l.shadowStrength = 0.85f; l.shadowBias = 0.01f; l.shadowNormalBias = 0.15f; l.shadowNearPlane = 0.1f;
        l.lightmapBakeType = LightmapBakeType.Realtime;
    }

    void OnDestroy()
    {
        if (fx != null) fx.Dispose();
        if (root != null) Destroy(root.gameObject);
    }

    void OnDisable() { if (root != null) root.gameObject.SetActive(false); }

    // ── 조각 하나의 입체 ──
    sealed class Rig
    {
        public Transform t;                                              // 조각 바닥 가운데(판 틀)
        public TextMeshPro label;
        public float w = -1f, d = -1f, baseY, topY;
        public string key;
        // 상자
        public Transform[] walls, flaps;
        public float[] ang;
        public Transform floor, sheetB, sheetT, trail;
        public readonly List<Transform[]> strips = new List<Transform[]>();
        public readonly List<Transform> wads = new List<Transform>();
        public Vector2 trailA; public bool trailOn;
        // 물건
        public Transform body;
        public readonly List<Transform> sleeves = new List<Transform>();
        public float h;
        // 롤
        public Transform axis, roll, core, tongue;
    }
    readonly Dictionary<PackPiece, Rig> rigs = new Dictionary<PackPiece, Rig>();

    static float BoxH(PackPiece p) => Mathf.Max(0.05f, p.inner.y * BoxS);

    Rig Ensure(PackPiece p)
    {
        if (rigs.TryGetValue(p, out var r)) return r;
        r = new Rig { t = Node(p.name, root) };
        switch (p.kind)
        {
            case PackPieceKind.Box:
                r.floor = Prim(PrimitiveType.Cube, "바닥", r.t, Mat(cardInC, "card", 0f));
                r.walls = new Transform[4]; r.flaps = new Transform[4]; r.ang = new float[] { 25f, 20f, 15f, 15f };
                for (int k = 0; k < 4; k++)
                {
                    r.walls[k] = Prim(PrimitiveType.Cube, "벽", r.t, Mat(cardC, "card", 0f));
                    var lin = Prim(PrimitiveType.Cube, "안쪽", r.walls[k], Mat(cardInC, "card", 0f));   // 벽 안쪽 면은 어두운 판지
                    lin.localScale = new Vector3(k < 2 ? 0.995f : 0.4f, 0.995f, k < 2 ? 0.4f : 0.995f);
                    lin.localPosition = k < 2 ? new Vector3(0f, 0f, k == 0 ? 0.31f : -0.31f) : new Vector3(k == 2 ? 0.31f : -0.31f, 0f, 0f);
                    r.flaps[k] = Node("날개 경첩", r.t);
                    Prim(PrimitiveType.Cube, "날개", r.flaps[k], Mat(flapC, "card", 0f));
                }
                r.sheetB = Prim(PrimitiveType.Cube, "바닥 완충재", r.t, Mat(bubbleC, "bubble", 0.6f));
                r.sheetT = Prim(PrimitiveType.Cube, "덮은 완충재", r.t, Mat(bubbleC, "bubble", 0.6f));
                r.trail = Prim(PrimitiveType.Cube, "붙이는 테이프", r.t, Mat(tapeC, "tape", 0.45f));
                SetAct(r.trail, false);
                r.label = Text(p.name + " 글", 0.42f);
                break;
            case PackPieceKind.Parcel:
                r.body = Node("몸", r.t);
                r.label = Text(p.name + " 글", 0.36f);
                break;
            case PackPieceKind.Roll:
            {
                bool paper = p.fillerKind == PackFiller.Paper;
                r.axis = Node("축", r.t);
                r.roll = Prim(PrimitiveType.Cylinder, "롤", r.axis, paper ? Mat(paperC, "paper", 0f) : Mat(bubble2C, "bubble", 0.6f));
                r.core = Prim(PrimitiveType.Cylinder, "심", r.axis, Mat(coreC, "card", 0f));
                r.tongue = Prim(PrimitiveType.Cube, "풀린 한 장", r.axis, paper ? Mat(paperC, "paper", 0f) : Mat(bubbleC, "bubble", 0.6f));
                r.label = Text(p.name + " 글", 0.34f);
                break;
            }
            case PackPieceKind.Tape:
            {
                var b = Prim(PrimitiveType.Cube, "받침", r.t, Mat(dispC)); b.localScale = new Vector3(0.08f, 0.022f, 0.09f); b.localPosition = new Vector3(0f, 0.011f, 0f);
                for (int k = -1; k <= 1; k += 2) { var c = Prim(PrimitiveType.Cube, "볼", r.t, Mat(dispC)); c.localScale = new Vector3(0.006f, 0.05f, 0.06f); c.localPosition = new Vector3(0.031f * k, 0.042f, 0.005f); }
                var tr = Prim(PrimitiveType.Cylinder, "테이프 롤", r.t, Mat(tapeC, "tape", 0.45f)); tr.localRotation = Quaternion.Euler(0f, 0f, 90f); tr.localScale = new Vector3(0.064f, 0.023f, 0.064f); tr.localPosition = new Vector3(0f, 0.052f, 0.008f);
                var hole = Prim(PrimitiveType.Cylinder, "심", r.t, Mat(coreC, "card", 0f)); hole.localRotation = Quaternion.Euler(0f, 0f, 90f); hole.localScale = new Vector3(0.042f, 0.0235f, 0.042f); hole.localPosition = tr.localPosition;
                var cut = Prim(PrimitiveType.Cube, "칼날", r.t, Mat(dispC)); cut.localScale = new Vector3(0.056f, 0.012f, 0.008f); cut.localPosition = new Vector3(0f, 0.03f, -0.044f);
                var tg = Prim(PrimitiveType.Cube, "나온 테이프", r.t, Mat(tapeC, "tape", 0.45f)); tg.localScale = new Vector3(0.044f, 0.002f, 0.04f); tg.localRotation = Quaternion.Euler(-28f, 0f, 0f); tg.localPosition = new Vector3(0f, 0.037f, -0.028f);
                r.label = Text(p.name + " 글", 0.34f);
                break;
            }
            case PackPieceKind.BeadBin:
            {
                var f = Prim(PrimitiveType.Cube, "통 바닥", r.t, Mat(binC)); f.localScale = new Vector3(p.w, 0.006f, p.d); f.localPosition = new Vector3(0f, 0.003f, 0f);
                for (int k = 0; k < 4; k++)
                {
                    var wl = Prim(PrimitiveType.Cube, "통 벽", r.t, Mat(binC));
                    bool fz = k < 2; float sg = k % 2 == 0 ? -1f : 1f;
                    wl.localScale = fz ? new Vector3(p.w, BinH, 0.008f) : new Vector3(0.008f, BinH, p.d - 0.016f);
                    wl.localPosition = fz ? new Vector3(0f, BinH * 0.5f, sg * (p.d * 0.5f - 0.004f)) : new Vector3(sg * (p.w * 0.5f - 0.004f), BinH * 0.5f, 0f);
                }
                var fill = Prim(PrimitiveType.Cube, "알갱이 더미", r.t, Mat(beadC, "bead", 0.1f)); fill.localScale = new Vector3(p.w - 0.016f, 0.05f, p.d - 0.016f); fill.localPosition = new Vector3(0f, 0.031f, 0f);
                r.label = Text(p.name + " 글", 0.34f);
                break;
            }
            case PackPieceKind.Scoop:
                PackBoardFx.BuildScoop("삽", r.t, Mat(new Color(0.70f, 0.72f, 0.76f)), Mat(new Color(0.36f, 0.37f, 0.40f)));
                break;
            case PackPieceKind.Sheet:
                if (p.crumple || p.fillerKind == PackFiller.Paper) r.body = Wad("구긴 종이", r.t, rigs.Count);
                else r.body = Prim(PrimitiveType.Cube, "뽁뽁이 한 장", r.t, Mat(bubbleC, "bubble", 0.6f));
                break;
        }
        rigs[p] = r;
        return r;
    }

    void Drop(PackPiece p)
    {
        if (!rigs.TryGetValue(p, out var r)) return;
        if (r.t != null) Destroy(r.t.gameObject);
        if (r.label != null) Destroy(r.label.gameObject);
        rigs.Remove(p);
    }

    /// <summary>상자 크기(w·d 는 R 로 돌리면 바뀐다)에 맞춰 바닥·벽·날개를 다시 맞춘다.</summary>
    static void LayoutBox(Rig r, float w, float d, float H)
    {
        r.w = w; r.d = d;
        r.floor.localScale = new Vector3(w, T, d); r.floor.localPosition = new Vector3(0f, T * 0.5f, 0f);
        for (int k = 0; k < 4; k++)
        {
            bool fz = k < 2; float sg = k % 2 == 0 ? -1f : 1f;           // 0 앞(−v) · 1 뒤 · 2 왼(−u) · 3 오른
            r.walls[k].localScale = fz ? new Vector3(w, H, T) : new Vector3(T, H, d - 2f * T);
            r.walls[k].localPosition = fz ? new Vector3(0f, H * 0.5f, sg * (d * 0.5f - T * 0.5f)) : new Vector3(sg * (w * 0.5f - T * 0.5f), H * 0.5f, 0f);
            float L = fz ? d * 0.5f : w * 0.5f, wid = fz ? w - 0.002f : d - 2f * T - 0.002f;
            r.flaps[k].localPosition = fz ? new Vector3(0f, H, sg * d * 0.5f) : new Vector3(sg * w * 0.5f, H, 0f);
            var f = r.flaps[k].GetChild(0);
            f.localScale = new Vector3(wid, T, L - 0.002f);
            f.localPosition = new Vector3(0f, fz ? T * 1.5f : T * 0.5f, L * 0.5f);   // 닫으면 앞뒤 날개가 옆 날개 위에 겹친다
        }
    }

    static readonly float[] yawOf = { 0f, 180f, 90f, -90f };              // 날개 경첩: 자식이 +z(안쪽)를 향하게
    static readonly float[] openAng = { 225f, 100f, 125f, 125f };         // 열린 상자: 앞 날개는 바깥으로 늘어져 안을 가리지 않는다

    /// <summary>바깥에서 본 판 위 높이(m): 각 조각의 바닥과 윗면을 정하고, 몸을 그 자리에 둔다.</summary>
    void LateUpdate()
    {
        bool on = board != null && board.On;
        if (root == null) { if (!on) return; BuildAll(); }
        if (root.gameObject.activeSelf != on) root.gameObject.SetActive(on);
        tut.Tick(on);
        if (!on) return;
        float now = Time.unscaledTime, dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
        fx.Begin();
        var pcs = board.pieces;
        if (rigs.Count != pcs.Count || rigs.Count == 0)                      // 뜯어 쓴 한 장·삽은 생기고 사라진다
        {
            var gone = new List<PackPiece>();
            foreach (var k in rigs.Keys) if (!pcs.Contains(k)) gone.Add(k);
            foreach (var k in gone) Drop(k);
        }
        var held = board.Held; fx.HeldTop = 0.1f;
        var slot = board.InSlot != null && board.InSlot.kind == PackPieceKind.Box ? board.InSlot : null;
        // 두 번: 상자 먼저(안에 든 물건은 상자 바닥 높이를 쓴다)
        for (int pass = 0; pass < 2; pass++)
            foreach (var p in pcs)
            {
                if ((p.kind == PackPieceKind.Box) != (pass == 0)) continue;
                var r = Ensure(p);
                bool show = p.active && !(p.holder != null && (p.holder.closed || !p.holder.active));
                SetAct(r.t, show); if (r.label != null) SetAct(r.label.transform, show);
                if (!show) continue;
                var shakeBox = p.kind == PackPieceKind.Box ? p : p.holder;     // 안의 것이 움직인 만큼 상자 겉(과 든 물건)이 덜컹
                var sk = shakeBox != null && shakeBox.kind == PackPieceKind.Box ? fx.ShakeOf(shakeBox, now) : Vector3.zero;
                float pu = p.u + p.jit.x + sk.x, pv = p.v + p.jit.y + sk.y;
                float z = p.drop.z;
                if (p.perch > 0f) z = Mathf.Max(PackBoard.RimZ, slot != null ? BoxH(slot) + 2f * T : 0f);   // 들어가지 못하고 벽 위에 걸림
                float baseY = Bench + (p.lifted ? Lift : 0f) + z + sk.z;
                if (p.holder != null && rigs.TryGetValue(p.holder, out var hr))
                    baseY = hr.baseY + T + (p.holder.bottom != PackFiller.None ? SheetH : 0f) + p.drop.z;
                r.baseY = baseY;
                r.t.SetPositionAndRotation(W(pu, pv, baseY), flat);
                switch (p.kind)
                {
                    case PackPieceKind.Box: DrawBox(p, r, held, slot, dt, now); break;
                    case PackPieceKind.Parcel: DrawParcel(p, r, now); break;
                    case PackPieceKind.Roll: DrawRoll(p, r); break;
                    case PackPieceKind.Tape: r.topY = TapeH; break;
                    case PackPieceKind.BeadBin: r.topY = BinH; break;
                    case PackPieceKind.Scoop: r.topY = ScoopH; break;
                    case PackPieceKind.Sheet: DrawSheet(p, r); break;
                }
                if (p == held) fx.HeldTop = baseY + r.topY;
                PlaceLabel(p, r, pu, pv);
            }
        fx.End(now);
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        DrawBeads(slot);
        DrawMs = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000f / System.Diagnostics.Stopwatch.Frequency;
    }

    void PlaceLabel(PackPiece p, Rig r, float pu, float pv)
    {
        if (r.label == null) return;
        var t = r.label;
        string s = p.kind == PackPieceKind.Parcel && p.parcel != null ? p.parcel.cargoName + "\n" + p.parcel.recipient + "·" + p.parcel.destLabel : p.name;
        if (t.text != s) t.text = s;
        switch (p.kind)
        {
            case PackPieceKind.Box:                                          // 앞 벽에 찍힌 글(카메라 쪽)
            case PackPieceKind.BeadBin:
            {
                float hh = p.kind == PackPieceKind.Box ? BoxH(p) : BinH;
                t.transform.SetPositionAndRotation(W(pu, pv - p.d * 0.5f - 0.003f, r.baseY + hh * 0.5f), Quaternion.LookRotation(board.Up, Vector3.up));
                break;
            }
            case PackPieceKind.Roll:                                         // 풀려 나온 장 위에
                t.transform.SetPositionAndRotation(W(pu, pv - p.d * 0.5f - 0.012f, r.baseY + 0.004f), Quaternion.LookRotation(Vector3.down, board.Up));
                break;
            case PackPieceKind.Tape:                                         // 받침 앞 작업대 위
                t.transform.SetPositionAndRotation(W(pu, pv - p.d * 0.5f - 0.03f, r.baseY + 0.001f), Quaternion.LookRotation(Vector3.down, board.Up));
                break;
            default:                                                         // 물건: 윗면에 붙은 꼬리표
                t.transform.SetPositionAndRotation(W(pu, pv, r.baseY + r.topY + 0.003f), Quaternion.LookRotation(Vector3.down, board.Up));
                break;
        }
    }

    // ── 상자 ──
    void DrawBox(PackPiece p, Rig r, PackPiece held, PackPiece slot, float dt, float now)
    {
        float H = BoxH(p);
        if (!Mathf.Approximately(r.w, p.w) || !Mathf.Approximately(r.d, p.d)) LayoutBox(r, p.w, p.d, H);
        // 테이프 디스펜서를 가운데 상자 위로 끄는 동안: 날개가 닫히고 지나간 자리에 테이프가 깔린다(자료는 놓을 때 PackBoard.EndTape 가 정한다)
        bool taping = held != null && held.kind == PackPieceKind.Tape && p == slot && !p.closed;
        if (taping)
        {
            float lu = held.u - p.u, lv = held.v - p.v;
            bool over = Mathf.Abs(lu) <= p.w * 0.5f + 0.06f && Mathf.Abs(lv) <= p.d * 0.5f + 0.06f;
            if (over && !r.trailOn) { r.trailOn = true; r.trailA = new Vector2(lu, lv); }
            if (r.trailOn)
            {
                var b = new Vector2(Mathf.Clamp(lu, -p.w * 0.5f - 0.02f, p.w * 0.5f + 0.02f), Mathf.Clamp(lv, -p.d * 0.5f - 0.02f, p.d * 0.5f + 0.02f));
                var a = r.trailA; var dd = b - a;
                bool hz = Mathf.Abs(dd.x) >= Mathf.Abs(dd.y);
                float len = Mathf.Max(0.03f, hz ? Mathf.Abs(dd.x) : Mathf.Abs(dd.y));
                r.trail.localScale = hz ? new Vector3(len, 0.002f, 0.03f) : new Vector3(0.03f, 0.002f, len);
                r.trail.localPosition = hz ? new Vector3((a.x + b.x) * 0.5f, H + 2f * T + 0.001f, a.y) : new Vector3(a.x, H + 2f * T + 0.001f, (a.y + b.y) * 0.5f);
            }
        }
        else r.trailOn = false;
        SetAct(r.trail, r.trailOn);
        bool shut = p.closed || r.trailOn;
        for (int k = 0; k < 4; k++)
        {
            float goal = shut ? 0f : p.open ? openAng[k] : (k == 0 ? 25f : k == 1 ? 20f : 15f);
            r.ang[k] = Mathf.MoveTowards(r.ang[k], goal, 600f * dt);
            r.flaps[k].localRotation = Quaternion.Euler(0f, yawOf[k], 0f) * Quaternion.Euler(-r.ang[k], 0f, 0f);
        }
        // 안: 바닥 완충재 · (물건은 따로) · 덮은 완충재 · 구긴 종이
        float y = T;
        bool hb = p.bottom != PackFiller.None, ht = p.top != PackFiller.None;
        SetAct(r.sheetB, hb);
        if (hb)
        {
            SetFiller(r.sheetB, p.bottom);
            r.sheetB.localScale = new Vector3(p.w - 2f * T - 0.004f, SheetH, p.d - 2f * T - 0.004f); r.sheetB.localPosition = new Vector3(0f, y + p.dropBottom.z + SheetH * 0.5f, 0f); y += SheetH;
        }
        if (p.content != null && rigs.TryGetValue(p.content, out var cr)) y += cr.h + 0.01f * p.content.wrap;
        SetAct(r.sheetT, ht && !p.closed);
        if (ht)
        {
            SetFiller(r.sheetT, p.top);
            r.sheetT.localScale = new Vector3(p.w - 2f * T - 0.004f, SheetH, p.d - 2f * T - 0.004f); r.sheetT.localPosition = new Vector3(0f, Mathf.Min(y, H - SheetH) + p.dropTop.z + SheetH * 0.5f, 0f);
        }
        float rad = PackBoard.PaperRadius * BoxS;
        while (r.wads.Count < p.wads.Count) r.wads.Add(Wad("구긴 종이", r.t, r.wads.Count + p.name.Length));
        for (int k = 0; k < r.wads.Count; k++)
        {
            bool has = k < p.wads.Count && !p.closed;
            SetAct(r.wads[k], has);
            if (!has) continue;
            var wd = p.wads[k];
            r.wads[k].localScale = new Vector3(rad * 2.1f, rad * 1.6f, rad * 2.1f);
            r.wads[k].localPosition = new Vector3(wd.off.x, T + (hb ? SheetH : 0f) + rad * 0.8f + wd.fall.z, wd.off.y);
            r.wads[k].localRotation = Quaternion.Euler(0f, k * 67f, 0f);
        }
        // 닫힌 상자의 테이프 줄: 윗면 + 양쪽 벽으로 꺾여 내려간 끝
        while (r.strips.Count < p.strips.Count)
        {
            var m = Mat(tapeC, "tape", 0.45f);
            r.strips.Add(new[] { Prim(PrimitiveType.Cube, "테이프 줄", r.t, m), Prim(PrimitiveType.Cube, "테이프 끝", r.t, m), Prim(PrimitiveType.Cube, "테이프 끝", r.t, m) });
        }
        float top = H + 2f * T + 0.001f;
        for (int k = 0; k < r.strips.Count; k++)
        {
            bool has = k < p.strips.Count && p.closed;
            foreach (var s in r.strips[k]) SetAct(s, has);
            if (!has) continue;
            var st = p.strips[k];
            var a = r.strips[k];
            if (st.horiz)
            {
                a[0].localScale = new Vector3(p.w + 0.004f, 0.002f, 0.03f); a[0].localPosition = new Vector3(0f, top, st.dv);
                for (int e = 1; e <= 2; e++) { a[e].localScale = new Vector3(0.002f, 0.035f, 0.03f); a[e].localPosition = new Vector3((e == 1 ? -1f : 1f) * (p.w * 0.5f + 0.001f), top - 0.0175f, st.dv); }
            }
            else
            {
                a[0].localScale = new Vector3(0.03f, 0.002f, p.d + 0.004f); a[0].localPosition = new Vector3(st.du, top, 0f);
                for (int e = 1; e <= 2; e++) { a[e].localScale = new Vector3(0.03f, 0.035f, 0.002f); a[e].localPosition = new Vector3(st.du, top - 0.0175f, (e == 1 ? -1f : 1f) * (p.d * 0.5f + 0.001f)); }
            }
        }
        r.topY = H + (shut ? 2f * T : 0f);
    }

    void SetFiller(Transform t, PackFiller f)
    {
        var m = f == PackFiller.Paper ? Mat(paperC, "paper", 0f) : Mat(bubbleC, "bubble", 0.6f);
        var rr = t.GetComponent<Renderer>(); if (rr.sharedMaterial != m) rr.sharedMaterial = m;
    }

    // ── 물건 ──
    Vector3 CargoSize(PackPiece p)
    {
        var s = board.CargoSizeOf(p);
        return p.rot90 ? new Vector3(s.z, s.y, s.x) : s;
    }

    void DrawParcel(PackPiece p, Rig r, float now)
    {
        var sz = CargoSize(p) * BoxS;
        string cargo = p.parcel != null ? p.parcel.cargoName : "";
        string key = cargo + "|" + sz.x.ToString("F3") + "|" + sz.z.ToString("F3");
        if (r.key != key) { r.key = key; BuildCargo(r, cargo, sz); }
        float hb = sz.y;
        while (r.sleeves.Count < p.wrap) r.sleeves.Add(Prim(PrimitiveType.Cube, "감싼 뽁뽁이", r.t, Mat(r.sleeves.Count == 0 ? bubbleC : bubble2C, "bubble", 0.6f)));
        for (int k = 0; k < r.sleeves.Count; k++)
        {
            bool has = k < p.wrap;
            SetAct(r.sleeves[k], has);
            if (!has) continue;
            float g = 0.012f * (k + 1), sp = fx.WrapSqueeze(p, k + 1, now);   // 겹마다 한 뼘씩 두꺼워지고, 방금 감은 겹은 벌어졌다 조여 든다
            float sy = hb + g * 0.8f;
            r.sleeves[k].localScale = new Vector3((sz.x + g) * sp, sy * Mathf.Lerp(1f, sp, 0.4f), (sz.z + g) * sp);
            r.sleeves[k].localPosition = new Vector3(0f, sy * 0.5f, 0f);
        }
        r.topY = hb + (p.wrap > 0 ? 0.012f * p.wrap * 0.8f : 0f);
        // 상자에 든 동안은 겉(감싼 겹 포함)을 상자 안 벽 안쪽에 맞춘다 — 판지 두께만큼 그림이 좁으니 안 그러면 벽을 뚫고 삐져나온다(그림만, 자료 그대로)
        float s = p.holder != null ? FitInside(sz.x + 0.012f * p.wrap, sz.z + 0.012f * p.wrap, p.holder.w, p.holder.d) : 1f;
        r.t.localScale = new Vector3(s, 1f, s);
    }

    /// <summary>상자(w·d, 바깥 치수) 안 벽 사이에 겉 치수 ow·od 를 넣는 가로 배율(≤1). 불변 검사가 같은 식을 쓴다.</summary>
    public static float FitInside(float ow, float od, float boxW, float boxD)
    {
        float iw = boxW - 2f * T - 0.004f, id = boxD - 2f * T - 0.004f;
        return Mathf.Min(1f, iw / Mathf.Max(1e-4f, ow), id / Mathf.Max(1e-4f, od));
    }

    /// <summary>그림 축척(판 m = 실제 m × BoxS) · 감싼 한 겹 그림 두께(양쪽 합) — 불변 검사용.</summary>
    public const float DrawScale = BoxS, WrapDraw = 0.012f;

    /// <summary>화물 이름으로 모양: 달걀 = 펄프 판 + 달걀 줄 · 병 = 유리 원통 · 그 밖 = 판지 덩어리. 크기는 world.yaml cargo_traits × 상자와 같은 축척.</summary>
    void BuildCargo(Rig r, string cargo, Vector3 sz)
    {
        for (int i = r.body.childCount - 1; i >= 0; i--) Destroy(r.body.GetChild(i).gameObject);
        r.h = sz.y;
        if (cargo.Contains("달걀"))
        {
            var b = Prim(PrimitiveType.Cube, "펄프 판", r.body, Mat(pulpC, "paper", 0f)); b.localScale = new Vector3(sz.x, sz.y * 0.45f, sz.z); b.localPosition = new Vector3(0f, sz.y * 0.225f, 0f);
            int nx = Mathf.Clamp(Mathf.RoundToInt(sz.x / 0.035f), 2, 6), nz = Mathf.Clamp(Mathf.RoundToInt(sz.z / 0.035f), 2, 6);
            float cx = sz.x / nx, cz = sz.z / nz;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    var e = Prim(PrimitiveType.Sphere, "달걀", r.body, Mat(eggC));
                    e.localScale = new Vector3(cx * 0.82f, sz.y * 0.7f, cz * 0.82f);
                    e.localPosition = new Vector3(-sz.x * 0.5f + cx * (i + 0.5f), sz.y * 0.62f, -sz.z * 0.5f + cz * (j + 0.5f));
                }
        }
        else if (cargo.Contains("병") || cargo.Contains("유리"))
        {
            int nx = Mathf.Clamp(Mathf.RoundToInt(sz.x / 0.05f), 1, 4), nz = Mathf.Clamp(Mathf.RoundToInt(sz.z / 0.05f), 1, 4);
            float cx = sz.x / nx, cz = sz.z / nz, dm = Mathf.Min(cx, cz) * 0.85f;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    var c = new Vector3(-sz.x * 0.5f + cx * (i + 0.5f), 0f, -sz.z * 0.5f + cz * (j + 0.5f));
                    var bo = Prim(PrimitiveType.Cylinder, "병", r.body, Mat(glassC, null, 0.8f)); bo.localScale = new Vector3(dm, sz.y * 0.36f, dm); bo.localPosition = c + Vector3.up * sz.y * 0.36f;
                    var nk = Prim(PrimitiveType.Cylinder, "병목", r.body, Mat(glassC, null, 0.8f)); nk.localScale = new Vector3(dm * 0.38f, sz.y * 0.14f, dm * 0.38f); nk.localPosition = c + Vector3.up * sz.y * 0.86f;
                }
        }
        else
        {
            var b = Prim(PrimitiveType.Cube, "물건", r.body, Mat(crateC, "card", 0f)); b.localScale = sz; b.localPosition = new Vector3(0f, sz.y * 0.5f, 0f);
        }
    }

    // ── 롤 · 한 장 ──
    void DrawRoll(PackPiece p, Rig r)
    {
        float len = (p.rot90 ? p.d : p.w) * 0.92f;
        float R = 0.05f * Mathf.Lerp(0.45f, 1f, (float)p.left / Mathf.Max(1, p.full));   // 쓸수록 가늘어진다
        r.axis.localRotation = Quaternion.Euler(0f, p.rot90 ? 90f : 0f, 0f);
        r.roll.localRotation = Quaternion.Euler(0f, 0f, 90f); r.roll.localScale = new Vector3(2f * R, len * 0.5f, 2f * R); r.roll.localPosition = new Vector3(0f, R, 0f);
        r.core.localRotation = r.roll.localRotation; r.core.localScale = new Vector3(0.024f, len * 0.5f + 0.003f, 0.024f); r.core.localPosition = r.roll.localPosition;
        SetAct(r.tongue, p.left > 0);
        r.tongue.localScale = new Vector3(len * 0.96f, 0.003f, 0.07f); r.tongue.localPosition = new Vector3(0f, 0.0015f, -0.035f - R * 0.3f);   // 바닥에서 앞으로 풀려 나온 한 장
        r.topY = 2f * R;
    }

    void DrawSheet(PackPiece p, Rig r)
    {
        if (p.crumple || p.fillerKind == PackFiller.Paper)
        {
            float e = p.crumple ? p.crumpleT * p.crumpleT * (3f - 2f * p.crumpleT) : 0f;   // 평평한 장 → 울퉁불퉁 공
            var s = new Vector3(Mathf.Lerp(0.20f, p.w * 1.05f, e), Mathf.Lerp(0.008f, p.w * 0.8f, e), Mathf.Lerp(0.18f, p.d * 1.05f, e));
            r.body.localScale = s; r.body.localPosition = new Vector3(0f, s.y * 0.5f, 0f); r.body.localRotation = Quaternion.Euler(0f, 30f * e, 0f);
            r.topY = s.y;
        }
        else
        {
            r.body.localScale = new Vector3(p.w, 0.006f, p.d); r.body.localPosition = new Vector3(0f, 0.003f, 0f);
            r.topY = 0.006f;
        }
    }

    // ── 알갱이(인스턴싱) ──
    readonly Matrix4x4[] mbuf = new Matrix4x4[1023];
    int mn;

    void Bead(Vector3 c, float r)
    {
        mbuf[mn++] = Matrix4x4.TRS(c, Quaternion.identity, Vector3.one * (2f * r));
        if (mn == mbuf.Length) Flush();
    }

    void Flush()
    {
        if (mn > 0) Graphics.DrawMeshInstanced(beadMesh, 0, beadMat, mbuf, mn, null, ShadowCastingMode.On, true, 0);
        mn = 0;
    }

    /// <summary>통 위 알갱이 · 삽에 뜬 한 줌 · 상자 안에 쌓이는 것(쏟은 자리에서 호를 그리며) · 흩어지는 것.</summary>
    void DrawBeads(PackPiece slot)
    {
        mn = 0;
        float r = PackBoard.BeadRadius * BoxS;
        foreach (var p in board.pieces)
        {
            if (!p.active || !rigs.TryGetValue(p, out var rg) || !rg.t.gameObject.activeSelf) continue;
            if (p.kind == PackPieceKind.BeadBin)                                // 통: 더미 위에 알갱이(자리 고정 무작위)
            {
                const int cols = 16, rows = 6;
                for (int i = 0; i < cols * rows; i++)
                {
                    float jx = Hash(i, 1, 3) - 0.5f, jy = Hash(i, 2, 3) - 0.5f;
                    float du = ((i % cols + 0.5f) / cols - 0.5f + jx * 0.5f / cols) * (p.w - 0.03f), dv = ((i / cols + 0.5f) / rows - 0.5f + jy * 0.5f / rows) * (p.d - 0.03f);
                    Bead(W(p.u + du, p.v + dv, rg.baseY + 0.056f + r * 0.6f), r * 1.2f);
                }
            }
            else if (p.kind == PackPieceKind.Scoop)                             // 삽에 소복한 한 줌
            {
                int n = PackBoard.BeadScoop;
                for (int i = 0; i < n; i++)
                {
                    float a = i * 2.39996f, s = Mathf.Sqrt((i + 0.5f) / n) * 0.026f;
                    Bead(W(p.u + Mathf.Cos(a) * s, p.v + Mathf.Sin(a) * s * 0.9f, rg.baseY + 0.004f + r * 1.2f + (1f - s / 0.026f) * 0.012f), r * 1.2f);
                }
            }
            else if (p.kind == PackPieceKind.Box && !p.closed && p.beads.Count > 0)
            {
                float pu = p.u + p.jit.x, pv = p.v + p.jit.y, floor = rg.baseY + T + (p.bottom != PackFiller.None ? SheetH : 0f);
                float iw = p.w - 2f * T, id = p.d - 2f * T, per = Mathf.Max(1f, iw * id / (4f * r * r * 1.15f));   // 한 층에 깔리는 수
                float startY = rg.baseY + BoxH(p) + 0.08f;
                for (int i = 0; i < p.beads.Count; i++)
                {
                    var bd = p.beads[i];
                    if (bd.delay > 0f) continue;                                 // 아직 안 쏟아짐
                    float k0 = Mathf.Clamp01(bd.t / PackBoard.BeadFlight), k = k0 * k0 * (3f - 2f * k0);
                    var pos = Vector2.Lerp(bd.from, bd.off, k);
                    float rest = floor + r + Mathf.Floor(i / per) * r * 1.7f + Hash(i, 5, 7) * r * 0.5f;
                    float arc = 4f * k0 * (1f - k0) * 0.045f;                    // 호: 날아가는 중간이 가장 높다
                    Bead(W(pu + pos.x, pv + pos.y, Mathf.Lerp(startY, rest, k) + arc + bd.fall.z), r);
                }
            }
        }
        foreach (var s in board.scatter) Bead(W(s.pos.x, s.pos.y, Bench + r + 0.04f * Mathf.Clamp01(s.life)), r);
        Flush();
    }
}
