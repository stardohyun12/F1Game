using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-300 포장 2D 보이고 들리게 — 전부 코드로 그린 절차 모양과 합성음(외부 소리·AI 이미지 없음). 물리 값은 건드리지 않는다.
/// ① 뽁뽁이 띠: 물건 둘레에 올록볼록 원 무늬 띠(겹마다 하나 더), 감싸는 순간 0.2 s 띠가 조여 들며 덮는다.
/// ② 종이: 네모 장 → 울퉁불퉁한 구긴 덩어리 + 주름선 2~3개(PackBoard.crumpleT, 0.15 s).
/// ③ 알갱이: 삽이 기울며 쏟는 모양(0.3 s) · 알갱이는 View 가 호를 그리며 떨어뜨린다.
/// ④ 상자 덜컹: 안의 것이 움직인 양에 비례해 상자 겉이 흔들린다(ShakeOf).
/// ⑤ 손: 판 위 마우스 자리에 2D 손, 집으면 쥔 모양.
/// 모양은 위에서 본 납작한 다각형을 색마다 메시 하나(Layer)에 모아 그린다. 모든 모양 계산은 판 좌표(u,v,m) — 월드 좌표는 판 틀에서 바꾼다.
/// </summary>
public sealed class PackBoardFx
{
    /// <summary>한 색 = 메시 하나. 꼭짓점은 월드 좌표 그대로. 안 바뀌면 다시 올리지 않는다.</summary>
    public sealed class Layer
    {
        readonly Mesh mesh; readonly List<Vector3> v = new List<Vector3>(512), n = new List<Vector3>(512); readonly List<int> t = new List<int>(1024);
        int sig;
        public int Count => v.Count;
        public Layer(string name, Transform parent, Material m)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false); g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            mesh = new Mesh { name = name + "(runtime)", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.MarkDynamic();
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = g.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }
        public void Clear() { v.Clear(); n.Clear(); t.Clear(); }
        /// <summary>중심 + 가장자리 점들(위에서 보아 반시계) 부채꼴 — 앞뒤 둘 다 그려 감는 방향 실수가 없게.</summary>
        public void Fan(Vector3 c, List<Vector3> ring, bool twoSided)
        {
            int i = v.Count, k = ring.Count;
            v.Add(c); n.Add(Vector3.up);
            for (int j = 0; j < k; j++) { v.Add(ring[j]); n.Add(Vector3.up); }
            for (int j = 0; j < k; j++)
            {
                int a = i + 1 + j, b = i + 1 + (j + 1) % k;
                t.Add(i); t.Add(b); t.Add(a);
                if (twoSided) { t.Add(i); t.Add(a); t.Add(b); }
            }
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int j = 0; j < 4; j++) n.Add(Vector3.up);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
            t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2);
        }
        public bool FlushIfChanged()
        {
            int s = v.Count, step = v.Count > 700 ? 7 : 1;
            for (int i = 0; i < v.Count; i += step) s = s * 31 + v[i].GetHashCode();
            if (s == sig) return false;
            sig = s;
            mesh.Clear();
            if (v.Count == 0) return true;
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return true;
        }
    }

    readonly PackBoard board;
    readonly Layer wrapD1, wrapL1, wrapD2, wrapL2, paperD, paperL, crease, scoopL, scoopD, handD, handS;
    readonly AudioSource src;
    readonly AudioClip cBubble, cCrumple, cThump, cSand, cScoop;
    readonly List<Vector3> ring = new List<Vector3>(32);
    readonly System.Random srng = new System.Random(300);

    // 합성음의 출처 — 외부 클립 없음, 아래 Synth* 코드가 전부(report 의 소리 출처 표)
    public static readonly string[] SoundSources =
    {
        "뽁뽁이 부스럭 = PackBoardFx.SynthBubble(고역 잡음 × 불규칙 포락선 + 무작위 뽁 10개, 시드 301)",
        "종이 구김 = PackBoardFx.SynthCrumple(대역 잡음 × 5~12 ms 마다 바뀌는 알갱이 세기, 시드 302)",
        "툭 = PackBoardFx.SynthThump(150→70 Hz 사인 감쇠 + 짧은 잡음, 시드 303)",
        "알갱이 사락 = PackBoardFx.SynthSand(밝은 잡음 × 2 ms 알갱이 흔들림 × 부드러운 포락선, 시드 304)",
        "덜그럭 = 기존 PackBench.PlayRattle(Kenney CC0 drop_002 등) 그대로 — 세기만 부딪힘 세기에 비례",
    };

    static readonly Color wrapDk1 = new Color(0.52f, 0.66f, 0.75f), wrapLt1 = new Color(0.90f, 0.96f, 1.00f),
        wrapDk2 = new Color(0.66f, 0.78f, 0.90f), wrapLt2 = new Color(1.00f, 1.00f, 1.00f),
        paperDk = new Color(0.66f, 0.63f, 0.56f), paperLt = new Color(0.90f, 0.88f, 0.82f), creaseC = new Color(0.38f, 0.35f, 0.30f),
        scoopDk = new Color(0.45f, 0.45f, 0.50f), scoopLt = new Color(0.78f, 0.78f, 0.82f),
        handOut = new Color(0.20f, 0.13f, 0.10f), handSkin = new Color(0.96f, 0.76f, 0.62f);

    public PackBoardFx(PackBoard b, Transform root, System.Func<Color, Material> mat)
    {
        board = b;
        wrapD1 = new Layer("띠 어두운 1", root, mat(wrapDk1)); wrapL1 = new Layer("띠 밝은 1", root, mat(wrapLt1));
        wrapD2 = new Layer("띠 어두운 2", root, mat(wrapDk2)); wrapL2 = new Layer("띠 밝은 2", root, mat(wrapLt2));
        paperD = new Layer("구김 바탕", root, mat(paperDk)); paperL = new Layer("구김 위", root, mat(paperLt)); crease = new Layer("구김 주름", root, mat(creaseC));
        scoopD = new Layer("삽 어두운", root, mat(scoopDk)); scoopL = new Layer("삽", root, mat(scoopLt));
        handD = new Layer("손 테두리", root, mat(handOut)); handS = new Layer("손", root, mat(handSkin));
        src = root.gameObject.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0f;
        cBubble = SynthBubble(); cCrumple = SynthCrumple(); cThump = SynthThump(); cSand = SynthSand(0.5f, 0.55f, 304); cScoop = SynthSand(0.16f, 0.35f, 305);
        board.FxEvent += OnFx;
    }

    public void Dispose() { if (board != null) board.FxEvent -= OnFx; }

    // ── 신호 받기 ──
    readonly Dictionary<PackPiece, float> wrapAt = new Dictionary<PackPiece, float>();
    readonly Dictionary<PackPiece, float> kick = new Dictionary<PackPiece, float>();
    struct Pour { public Vector2 at; public float t0; public bool inBox; }
    readonly List<Pour> pours = new List<Pour>();
    /// <summary>QA: 이 그림이 낸 소리 수(종류별) — 신호가 소리까지 갔나.</summary>
    public int SoundsPlayed, WrapStarted, PoursStarted;
    public string LastSound = "";
    const float WrapSec = 0.2f, PourSec = 0.3f;

    void Play(AudioClip c, float vol, string n)
    {
        if (c == null) return;
        src.pitch = Mathf.Lerp(0.94f, 1.06f, (float)srng.NextDouble());
        src.PlayOneShot(c, vol); SoundsPlayed++; LastSound = n;
    }

    void OnFx(PackBoard.PackFx k, PackPiece p, float amount)
    {
        switch (k)
        {
            case PackBoard.PackFx.WrapOn: wrapAt[p] = Time.unscaledTime; WrapStarted++; Play(cBubble, 0.6f, "뽁뽁이 부스럭"); break;
            case PackBoard.PackFx.Crumple: Play(cCrumple, 0.55f, "종이 구김"); break;
            case PackBoard.PackFx.WadSet: Play(cThump, 0.55f, "툭"); break;
            case PackBoard.PackFx.Scoop: Play(cScoop, 0.4f, "삽 사락"); break;
            case PackBoard.PackFx.Pour:
            case PackBoard.PackFx.PourOut:
                pours.Add(new Pour { at = new Vector2(p.u, p.v), t0 = Time.unscaledTime, inBox = k == PackBoard.PackFx.Pour }); PoursStarted++;
                Play(cSand, k == PackBoard.PackFx.Pour ? 0.65f : 0.45f, "알갱이 사락"); break;
            case PackBoard.PackFx.Rattle: if (p != null) kick[p] = Mathf.Max(kick.TryGetValue(p, out var o) ? o : 0f, amount); break;
        }
    }

    // ── 한 프레임 ──
    Vector3 W(float u, float v, float y) => board.Origin + board.Right * u + board.Up * v + Vector3.up * y;

    public void Begin()
    {
        wrapD1.Clear(); wrapL1.Clear(); wrapD2.Clear(); wrapL2.Clear(); paperD.Clear(); paperL.Clear(); crease.Clear();
        scoopD.Clear(); scoopL.Clear(); handD.Clear(); handS.Clear();
        if (kick.Count > 0)                                                   // 덜컹은 프레임당 한 번만 가라앉는다
        {
            float f = Mathf.Exp(-9f * Mathf.Min(0.05f, Time.unscaledDeltaTime));
            kickKeys.Clear(); kickKeys.AddRange(kick.Keys);
            foreach (var key in kickKeys) { float kv = kick[key] * f; if (kv < 0.01f) kick.Remove(key); else kick[key] = kv; }
        }
    }
    readonly List<PackPiece> kickKeys = new List<PackPiece>();

    public void End(float now)
    {
        DrawPours(now);
        DrawHand(now);
        board.HandDrawn = handShown;
        wrapD1.FlushIfChanged(); wrapL1.FlushIfChanged(); wrapD2.FlushIfChanged(); wrapL2.FlushIfChanged();
        paperD.FlushIfChanged(); paperL.FlushIfChanged(); crease.FlushIfChanged(); scoopD.FlushIfChanged(); scoopL.FlushIfChanged();
        handD.FlushIfChanged(); handS.FlushIfChanged();
    }

    // ── ④ 상자 덜컹 ──
    /// <summary>상자(box)가 안의 것이 움직인 양에 비례해 흔들리는 자리 어긋남(u, v, 위로). 부딪힘(Rattle)은 한 번 크게 튀고 가라앉는다.</summary>
    public Vector3 ShakeOf(PackPiece box, float now)
    {
        if (box == null || box.phys == null || box.content == null) return Vector3.zero;
        float k = kick.TryGetValue(box, out var kv) ? kv : 0f;
        float a = Mathf.Clamp01(box.phys.vel.magnitude / 0.9f) * 0.6f + k;
        a = Mathf.Min(1f, a);
        if (a < 0.02f) return Vector3.zero;
        return new Vector3(Mathf.Sin(now * 61f + 1.3f) * a * 0.018f, Mathf.Sin(now * 47f + 0.4f) * a * 0.012f, Mathf.Abs(Mathf.Sin(now * 38f)) * a * 0.012f);
    }

    // ── ① 뽁뽁이 띠 ──
    /// <summary>
    /// 물건(발자국 w×d, 가운데 (u,v), 윗면 y) 둘레에 감긴 겹 수(layers)만큼 올록볼록 원 띠. 겹 k 의 띠는 물건 가장자리에서 (k−0.5)×rim 바깥.
    /// 방금 감긴 겹(age &lt; 0.2 s)은 크게 벌어져 있다가 조여 든다.
    /// </summary>
    public void WrapBands(PackPiece p, float u, float v, float y, float w, float d, float rim, float now)
    {
        float age = wrapAt.TryGetValue(p, out var t0) ? now - t0 : 9f;
        for (int k = 1; k <= p.wrap; k++)
        {
            float e = 1f, sp = 1f;
            if (k == p.wrap && age < WrapSec) { float q = Mathf.Clamp01(age / WrapSec); e = 1f - (1f - q) * (1f - q); sp = Mathf.Lerp(2.1f, 1f, e); }
            float off = rim * 0.5f * (k - 0.5f);
            float hx = (w * 0.5f + off) * sp, hz = (d * 0.5f + off) * sp;
            float r = rim * 0.24f * Mathf.Lerp(0.35f, 1f, e);
            var dk = k == 1 ? wrapD1 : wrapD2; var lt = k == 1 ? wrapL1 : wrapL2;
            float per = 4f * (hx + hz), step = Mathf.Max(0.001f, rim * 0.5f);
            int n = Mathf.Max(8, Mathf.RoundToInt(per / step));
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / n * per, du, dv;                         // 둘레를 따라 한 바퀴(아래 변 → 오른쪽 → 위 → 왼쪽)
                if (s < 2f * hx) { du = -hx + s; dv = -hz; }
                else if (s < 2f * hx + 2f * hz) { du = hx; dv = -hz + (s - 2f * hx); }
                else if (s < 4f * hx + 2f * hz) { du = hx - (s - 2f * hx - 2f * hz); dv = hz; }
                else { du = -hx; dv = hz - (s - 4f * hx - 2f * hz); }
                Disc(dk, u + du, v + dv, y, r, 10);
                Disc(lt, u + du - r * 0.22f, v + dv + r * 0.22f, y + 0.0014f, r * 0.58f, 8);
            }
        }
    }

    void Disc(Layer l, float u, float v, float y, float r, int sides)
    {
        ring.Clear();
        for (int i = 0; i < sides; i++) { float a = i * Mathf.PI * 2f / sides; ring.Add(W(u + Mathf.Cos(a) * r, v + Mathf.Sin(a) * r, y)); }
        l.Fan(W(u, v, y), ring, true);
    }

    void Ellipse(Layer l, float u, float v, float y, float rx, float rz, float deg, int sides = 12)
    {
        ring.Clear();
        float c = Mathf.Cos(deg * Mathf.Deg2Rad), s = Mathf.Sin(deg * Mathf.Deg2Rad);
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides, x = Mathf.Cos(a) * rx, z = Mathf.Sin(a) * rz;
            ring.Add(W(u + x * c - z * s, v + x * s + z * c, y));
        }
        l.Fan(W(u, v, y), ring, true);
    }

    // ── ② 종이: 네모 → 울퉁불퉁 덩어리 ──
    static float Hash(int seed, int i) { float x = Mathf.Sin(seed * 12.9898f + i * 78.233f) * 43758.5453f; return x - Mathf.Floor(x); }

    /// <summary>
    /// 구긴 종이 하나: t 0 = 평평한 네모 장(halfU×halfV) → 1 = 울퉁불퉁한 덩어리(반지름 R, 꼭짓점마다 0.72~1.28 배). 주름선 3개는 t 가 0.3 을 넘으면 서서히.
    /// 아래 깔린 어두운 면 + 살짝 비킨 밝은 면으로 두 톤.
    /// </summary>
    public void Paper(float u, float v, float y, float R, float t, int seed, float halfU = 0.10f, float halfV = 0.09f)
    {
        const int N = 11;
        float e = t * t * (3f - 2f * t);
        var pts = new Vector2[N];
        for (int i = 0; i < N; i++)
        {
            float a = i * Mathf.PI * 2f / N + 0.2f;
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float m = 1f / Mathf.Max(Mathf.Abs(ca) / halfU, Mathf.Abs(sa) / halfV);        // 같은 방향으로 쏜 네모 둘레 점
            var sq = new Vector2(ca * m, sa * m);
            var lump = new Vector2(ca, sa) * (R * (0.72f + 0.56f * Hash(seed, i)));
            pts[i] = Vector2.Lerp(sq, lump, e);
        }
        FanPoly(paperD, u, v, y, pts, 1f, Vector2.zero);
        FanPoly(paperL, u, v, y + 0.0014f, pts, Mathf.Lerp(0.92f, 0.74f, e), new Vector2(-R * 0.18f, R * 0.18f) * e);
        float cw = Mathf.Lerp(0.0012f, 0.0026f, e);
        float f = Mathf.Clamp01((t - 0.3f) / 0.7f);
        if (f <= 0f) return;
        for (int c = 0; c < 3; c++)
        {
            int a0 = (c * 4 + (int)(Hash(seed, 40 + c) * 3f)) % N, b0 = (a0 + 4 + (int)(Hash(seed, 50 + c) * 3f)) % N;
            var pa = pts[a0] * 0.7f * f; var pb = pts[b0] * 0.7f * f;
            Seg(crease, u + pa.x, v + pa.y, u + pb.x, v + pb.y, y + 0.0028f, cw);
        }
    }

    void FanPoly(Layer l, float u, float v, float y, Vector2[] pts, float scale, Vector2 shift)
    {
        ring.Clear();
        foreach (var q in pts) ring.Add(W(u + shift.x + q.x * scale, v + shift.y + q.y * scale, y));
        l.Fan(W(u + shift.x, v + shift.y, y), ring, true);
    }

    void Seg(Layer l, float u0, float v0, float u1, float v1, float y, float w)
    {
        var d = new Vector2(u1 - u0, v1 - v0);
        if (d.sqrMagnitude < 1e-8f) return;
        d.Normalize(); var nrm = new Vector2(-d.y, d.x) * (w * 0.5f);
        l.Quad(W(u0 - nrm.x, v0 - nrm.y, y), W(u0 + nrm.x, v0 + nrm.y, y), W(u1 + nrm.x, v1 + nrm.y, y), W(u1 - nrm.x, v1 - nrm.y, y));
    }

    // ── ③ 쏟는 삽 ──
    void DrawPours(float now)
    {
        for (int i = pours.Count - 1; i >= 0; i--)
        {
            float age = now - pours[i].t0;
            if (age >= PourSec) { pours.RemoveAt(i); continue; }
            float q = age / PourSec, tilt = Mathf.Sin(Mathf.Min(1f, q * 1.6f) * Mathf.PI * 0.5f);   // 기울어짐 0 → 1 (끝에서 사라짐)
            float shrink = q > 0.75f ? 1f - (q - 0.75f) / 0.25f : 1f;
            float hw = 0.055f * shrink, hd = 0.032f * shrink * Mathf.Lerp(1f, 0.45f, tilt);
            var at = pours[i].at + new Vector2(0f, tilt * 0.02f);
            float y = 0.14f + tilt * 0.03f;
            Rect4(scoopD, at.x, at.y, y, hw + 0.004f, hd + 0.004f);
            Rect4(scoopL, at.x, at.y, y + 0.0014f, hw, hd);
        }
    }

    void Rect4(Layer l, float u, float v, float y, float hu, float hv)
    {
        l.Quad(W(u - hu, v - hv, y), W(u - hu, v + hv, y), W(u + hu, v + hv, y), W(u + hu, v - hv, y));
    }

    // ── ⑤ 손 ──
    bool handShown; float grip;
    const float HandS = 1.35f;

    void DrawHand(float now)
    {
        handShown = board.On && board.Cam != null && (board.PointerOn || board.Gripping);
        if (!handShown) { grip = 0f; return; }
        float target = board.Gripping ? 1f : 0f;
        grip = Mathf.MoveTowards(grip, target, Mathf.Min(0.05f, Time.unscaledDeltaTime) / 0.07f);   // 쥐는 데 0.07 s
        float g = grip * grip * (3f - 2f * grip);
        var pos = board.HandPos;
        // 가리키는 끝(가운뎃손가락 끝)이 마우스 자리. 쥐면 주먹이 마우스 자리 오른쪽 아래에 와 쥔 물건이 보이게.
        float S = HandS;
        float ou = pos.x + Mathf.Lerp(0f, 0.034f, g), ov = pos.y - Mathf.Lerp(0.040f * S, 0.020f * S, g) - Mathf.Lerp(0f, 0.012f, g);
        HandShape(handD, ou, ov, 0.170f, S, g, 0.0032f);
        HandShape(handS, ou, ov, 0.1716f, S, g, 0f);
    }

    /// <summary>손 하나: 손바닥 + 손가락 4 + 엄지. g 0 = 편 손, 1 = 쥔 주먹. grow 만큼 모두 부풀려 테두리 층으로 쓴다.</summary>
    void HandShape(Layer l, float u, float v, float y, float S, float g, float grow)
    {
        Ellipse(l, u, v - 0.010f * S, y, 0.0195f * S + grow, 0.0200f * S + grow, 0f);
        float[] len = { 0.032f, 0.038f, 0.036f, 0.030f };
        for (int i = 0; i < 4; i++)
        {
            float fx = (-0.0145f + i * 0.0097f) * S;
            float L = Mathf.Lerp(len[i], 0.013f, g) * S;
            float cz = 0.006f * S + L * 0.5f;
            Ellipse(l, u + fx, v + cz, y, Mathf.Lerp(0.0054f, 0.0064f, g) * S + grow, L * 0.5f + grow, 0f, 10);
        }
        float tx = Mathf.Lerp(-0.0285f, -0.008f, g) * S, tz = Mathf.Lerp(0.000f, -0.002f, g) * S, ta = Mathf.Lerp(35f, 85f, g);   // 엄지: 위-왼쪽을 향함 → 손바닥을 가로지름
        Ellipse(l, u + tx, v + tz, y, 0.0058f * S + grow, Mathf.Lerp(0.0145f, 0.0115f, g) * S + grow, ta, 10);
    }

    // ── 합성음(출처: 이 코드뿐 — 외부 파일 없음) ──
    const int SR = 44100;

    static AudioClip Make(string name, float[] d, float gain)
    {
        float peak = 1e-6f;
        for (int i = 0; i < d.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
        for (int i = 0; i < d.Length; i++) d[i] = d[i] / peak * gain;
        var c = AudioClip.Create(name, d.Length, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    static float Lp(float fc) => 1f - Mathf.Exp(-2f * Mathf.PI * fc / SR);

    static AudioClip SynthBubble()
    {
        var r = new System.Random(301);
        int n = (int)(0.45f * SR); var d = new float[n];
        float lp = 0f, a = Lp(1800f), slow = 0f, slowT = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, x = (float)(r.NextDouble() * 2 - 1);
            lp += a * (x - lp); float hp = x - lp;                                       // 고역 잡음
            if (t >= slowT) { slow = 0.35f + 0.65f * (float)r.NextDouble(); slowT = t + 0.012f + 0.02f * (float)r.NextDouble(); }   // 부스럭 불규칙
            float env = Mathf.Clamp01(t / 0.02f) * Mathf.Exp(-t * 5.5f);
            d[i] = hp * env * slow * 0.7f;
        }
        for (int k = 0; k < 10; k++)                                                      // 뽁 — 짧은 공명 펄스
        {
            float t0 = 0.02f + (float)r.NextDouble() * 0.32f, f = 1200f + (float)r.NextDouble() * 1400f, amp = 0.4f + 0.5f * (float)r.NextDouble();
            int s0 = (int)(t0 * SR), len = (int)(0.018f * SR);
            for (int i = 0; i < len && s0 + i < n; i++) { float t = i / (float)SR; d[s0 + i] += amp * Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 380f); }
        }
        return Make("pack_bubble(synth)", d, 0.8f);
    }

    static AudioClip SynthCrumple()
    {
        var r = new System.Random(302);
        int n = (int)(0.2f * SR); var d = new float[n];
        float lp1 = 0f, lp2 = 0f, a1 = Lp(1500f), a2 = Lp(6500f), g = 1f, gT = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, x = (float)(r.NextDouble() * 2 - 1);
            lp1 += a1 * (x - lp1); float hp = x - lp1; lp2 += a2 * (hp - lp2);            // 1.5~6.5 kHz 대역
            if (t >= gT) { g = 0.15f + 0.85f * (float)r.NextDouble(); g *= g; gT = t + 0.005f + 0.007f * (float)r.NextDouble(); }
            d[i] = lp2 * g * Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t * 9f);
        }
        return Make("pack_crumple(synth)", d, 0.85f);
    }

    static AudioClip SynthThump()
    {
        var r = new System.Random(303);
        int n = (int)(0.14f * SR); var d = new float[n];
        float ph = 0f, lp = 0f, a = Lp(2500f);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, f = Mathf.Lerp(150f, 70f, Mathf.Min(1f, t / 0.1f));
            ph += 2f * Mathf.PI * f / SR;
            float x = (float)(r.NextDouble() * 2 - 1); lp += a * (x - lp);
            d[i] = Mathf.Sin(ph) * Mathf.Exp(-t * 30f) + lp * 0.35f * Mathf.Exp(-t * 70f);
        }
        return Make("pack_thump(synth)", d, 0.9f);
    }

    static AudioClip SynthSand(float secs, float gain, int seed)
    {
        var r = new System.Random(seed);
        int n = (int)(secs * SR); var d = new float[n];
        float lp1 = 0f, lp2 = 0f, a1 = Lp(2800f), a2 = Lp(9000f), g = 1f, gT = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, q = t / secs, x = (float)(r.NextDouble() * 2 - 1);
            lp1 += a1 * (x - lp1); float hp = x - lp1; lp2 += a2 * (hp - lp2);            // 2.8~9 kHz 밝은 잡음 = 사락
            if (t >= gT) { g = 0.3f + 0.7f * (float)r.NextDouble(); gT = t + 0.0015f + 0.002f * (float)r.NextDouble(); }
            float env = Mathf.SmoothStep(0f, 1f, q / 0.15f) * (1f - Mathf.SmoothStep(0f, 1f, (q - 0.55f) / 0.45f));
            d[i] = lp2 * g * env;
        }
        return Make("pack_sand(synth)", d, gain);
    }
}
