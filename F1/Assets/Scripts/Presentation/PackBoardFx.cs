using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-300 포장 보이고 들리게 — 전부 코드로 만든 모양과 합성음(외부 소리·AI 이미지 없음). 물리 값은 건드리지 않는다.
/// D-301 3D 작업대: 납작한 다각형 대신 그림자를 드리우는 입체.
/// ① 뽁뽁이: 감싸는 순간 0.2 s 비닐 겹이 크게 벌어졌다 물건에 조여 든다(WrapSqueeze — 겹 모양은 PackBoardView).
/// ② 쏟는 삽: 금속 삽이 기울며 쏟는다(0.3 s) · 알갱이는 View 가 호를 그리며 떨어뜨린다.
/// ③ 상자 덜컹: 안의 것이 움직인 양에 비례해 상자 겉이 흔들린다(ShakeOf).
/// ④ 손: 3D 손(손바닥·손가락 4·엄지·소매) — 마우스 레이 위 판 위 0.17 m 에 떠 있고, 집으면 쥔 물건 위로 내려와 손가락을 오므린다.
/// 모든 자리 계산은 판 좌표(u,v,m) — 월드 좌표는 판 틀에서 바꾼다.
/// </summary>
public sealed class PackBoardFx
{
    readonly PackBoard board;
    readonly Transform root;
    readonly AudioSource src;
    readonly AudioClip cBubble, cCrumple, cThump, cSand, cScoop;
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

    static readonly Color skinC = new Color(0.90f, 0.70f, 0.56f), sleeveC = new Color(0.20f, 0.27f, 0.40f),
        metalC = new Color(0.70f, 0.72f, 0.76f), metalDkC = new Color(0.36f, 0.37f, 0.40f);

    public PackBoardFx(PackBoard b, Transform r, System.Func<Color, Material> mat)
    {
        board = b; root = r;
        src = root.gameObject.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0f;
        cBubble = SynthBubble(); cCrumple = SynthCrumple(); cThump = SynthThump(); cSand = SynthSand(0.5f, 0.55f, 304); cScoop = SynthSand(0.16f, 0.35f, 305);
        board.FxEvent += OnFx;
        BuildHand(mat(skinC), mat(sleeveC));
        mMetal = mat(metalC); mMetalDk = mat(metalDkC);
    }

    public void Dispose() { if (board != null) board.FxEvent -= OnFx; }

    /// <summary>판 틀 기본 모양 하나(충돌체 없음, 그림자 드리움·받음).</summary>
    public static Transform Prim(PrimitiveType shape, string n, Transform parent, Material m)
    {
        var g = GameObject.CreatePrimitive(shape);
        g.name = n;
        Object.Destroy(g.GetComponent<Collider>());
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    static Transform Node(string n, Transform parent)
    {
        var t = new GameObject(n).transform;
        t.SetParent(parent, false);
        return t;
    }

    /// <summary>금속 삽(앞이 트인 얕은 통 + 뒤로 솟은 자루). 원점 = 삽 바닥 가운데, 트인 쪽 = +z(판 위쪽).</summary>
    public static Transform BuildScoop(string n, Transform parent, Material metal, Material dark)
    {
        var s = Node(n, parent);
        var f = Prim(PrimitiveType.Cube, "바닥", s, metal); f.localScale = new Vector3(0.07f, 0.004f, 0.064f); f.localPosition = new Vector3(0f, 0.002f, 0f);
        var bw = Prim(PrimitiveType.Cube, "뒷벽", s, metal); bw.localScale = new Vector3(0.07f, 0.03f, 0.004f); bw.localPosition = new Vector3(0f, 0.015f, -0.03f);
        for (int k = -1; k <= 1; k += 2)
        {
            var sw = Prim(PrimitiveType.Cube, "옆벽", s, metal); sw.localScale = new Vector3(0.004f, 0.026f, 0.064f); sw.localPosition = new Vector3(0.033f * k, 0.013f, 0f);
        }
        var h = Prim(PrimitiveType.Cylinder, "자루", s, dark); h.localScale = new Vector3(0.014f, 0.035f, 0.014f);
        h.localRotation = Quaternion.Euler(-55f, 0f, 0f); h.localPosition = new Vector3(0f, 0.035f, -0.058f);
        return s;
    }

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
    Quaternion Flat => Quaternion.LookRotation(board.Up, Vector3.up);

    /// <summary>View 가 매 프레임 알려 준다: 쥔 물건 윗면 높이(판 위 m) — 쥔 손이 그 위에 얹힌다.</summary>
    public float HeldTop;

    public void Begin()
    {
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
    }

    // ── ③ 상자 덜컹 ──
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

    // ── ① 뽁뽁이 조이기 ──
    /// <summary>겹 layer(1부터)의 크기 배율: 방금 감긴 맨 바깥 겹은 0.2 s 동안 2.1 배에서 1 배로 조여 든다. 그 밖은 1.</summary>
    public float WrapSqueeze(PackPiece p, int layer, float now)
    {
        if (layer != p.wrap || !wrapAt.TryGetValue(p, out var t0)) return 1f;
        float q = Mathf.Clamp01((now - t0) / WrapSec), e = 1f - (1f - q) * (1f - q);
        return Mathf.Lerp(2.1f, 1f, e);
    }

    // ── ② 쏟는 삽 ──
    Material mMetal, mMetalDk;
    readonly List<Transform> pourRigs = new List<Transform>();

    void DrawPours(float now)
    {
        for (int i = pours.Count - 1; i >= 0; i--) if (now - pours[i].t0 >= PourSec) pours.RemoveAt(i);
        while (pourRigs.Count < pours.Count) pourRigs.Add(BuildScoop("쏟는 삽", root, mMetal, mMetalDk));
        for (int i = 0; i < pourRigs.Count; i++)
        {
            var s = pourRigs[i];
            bool on = i < pours.Count;
            if (s.gameObject.activeSelf != on) s.gameObject.SetActive(on);
            if (!on) continue;
            float q = (now - pours[i].t0) / PourSec, tilt = Mathf.Sin(Mathf.Min(1f, q * 1.6f) * Mathf.PI * 0.5f);   // 기울어짐 0 → 1
            var at = pours[i].at;
            s.SetPositionAndRotation(W(at.x, at.y - 0.04f, 0.22f + tilt * 0.02f), Flat * Quaternion.Euler(tilt * 80f, 0f, 0f));   // 트인 앞(+z)이 아래로
        }
    }

    // ── ④ 손 ──
    bool handShown; float grip;
    Transform hand;
    readonly Transform[] fingers = new Transform[4];
    Transform thumb;
    const float HoverY = 0.17f, TipZ = 0.078f;

    void BuildHand(Material skin, Material sleeve)
    {
        hand = Node("손", root);
        var palm = Prim(PrimitiveType.Sphere, "손바닥", hand, skin); palm.localScale = new Vector3(0.072f, 0.026f, 0.078f);
        float[] len = { 0.040f, 0.048f, 0.046f, 0.036f };
        for (int i = 0; i < 4; i++)
        {
            var pv = Node("손가락 " + i, hand); pv.localPosition = new Vector3(-0.024f + i * 0.016f, 0f, 0.030f);
            var f = Prim(PrimitiveType.Capsule, "마디", pv, skin);
            f.localScale = new Vector3(0.015f, len[i] * 0.5f, 0.015f); f.localRotation = Quaternion.Euler(90f, 0f, 0f); f.localPosition = new Vector3(0f, 0f, len[i] * 0.5f);
            fingers[i] = pv;
        }
        thumb = Node("엄지", hand); thumb.localPosition = new Vector3(-0.032f, -0.002f, -0.008f);
        var th = Prim(PrimitiveType.Capsule, "마디", thumb, skin);
        th.localScale = new Vector3(0.017f, 0.019f, 0.017f); th.localRotation = Quaternion.Euler(90f, 0f, 0f); th.localPosition = new Vector3(0f, 0f, 0.019f);
        var wr = Prim(PrimitiveType.Sphere, "손목", hand, skin); wr.localScale = new Vector3(0.05f, 0.03f, 0.05f); wr.localPosition = new Vector3(0f, 0.006f, -0.042f);
        // 소매: 팔은 작업대 앞(카메라 쪽)에서 거의 눕혀 들어온다 — 세우면 원통 뚜껑이 카메라를 보고 손바닥을 덮는다
        var sl = Prim(PrimitiveType.Cylinder, "소매", hand, sleeve); sl.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        sl.localRotation = Quaternion.Euler(-82f, 0f, 0f); sl.localPosition = new Vector3(0f, 0.012f, -0.10f);
        hand.gameObject.SetActive(false);
    }

    void DrawHand(float now)
    {
        handShown = board.On && board.Cam != null && (board.PointerOn || board.Gripping);
        if (hand.gameObject.activeSelf != handShown) hand.gameObject.SetActive(handShown);
        if (!handShown) { grip = 0f; return; }
        float target = board.Gripping ? 1f : 0f;
        grip = Mathf.MoveTowards(grip, target, Mathf.Min(0.05f, Time.unscaledDeltaTime) / 0.07f);   // 쥐는 데 0.07 s
        float g = grip * grip * (3f - 2f * grip);
        var pos = board.HandPos;
        Vector3 at;
        if (board.Gripping) at = W(pos.x, pos.y - 0.012f, HeldTop + 0.014f);       // 쥔 물건 위에 얹힌 손
        else
        {
            // 펼친 손: 가운뎃손가락 끝이 마우스 자리 — 화면에서 커서와 겹치게 카메라 레이를 따라 판 위 HoverY 높이로 올린다
            var tip = W(pos.x, pos.y, PackBoard.PickY);
            var c = board.Cam.transform.position;
            float y = board.TopY + HoverY, k = Mathf.Abs(tip.y - c.y) < 1e-4f ? 1f : (y - c.y) / (tip.y - c.y);
            at = c + (tip - c) * k - board.Up * TipZ;
        }
        hand.SetPositionAndRotation(at, Flat * Quaternion.Euler(Mathf.Lerp(8f, 0f, g), -8f, 0f));
        for (int i = 0; i < 4; i++) fingers[i].localRotation = Quaternion.Euler(Mathf.Lerp(10f, 95f, g), 0f, 0f);
        thumb.localRotation = Quaternion.Euler(Mathf.Lerp(5f, 40f, g), Mathf.Lerp(-55f, -15f, g), 0f);
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
