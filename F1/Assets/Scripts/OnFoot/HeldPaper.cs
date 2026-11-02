using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-269(과제 20261103-0002): 손에 든 종이 — 공용 동작(Firewatch 지도 컷). 칠판 쪽지·전표(나중엔 일기·약도)가 같은 물건을 쓴다.
///   Pinned(칠판에 핀으로) → 집기 → Up(들어 올림 0.3초, 화면 아래 약 23%, 뒤로 세상) → Read(가까이, 화면 세로 약 55%, 1 캔버스 칸 = 화면 도트 1칸) → Down(내려놓기) → Up …
/// 종이 = 세상과 같은 LofiSurface 셰이더(해·그늘·안개), 카메라 자식이라 360 줄 렌더 안에서 그려진다. 바탕 = Resources/LofiUI/paper(CC0 사진 ambientCG CardboardSet001, 결 대비만 키움).
/// 글자 = 종이 위 World Space 캔버스의 Galmuri 텍스트(텍스처에 굽지 않음), 잉크색. 약도 칸 = 대역 선 그림(보관소 → 찻집 점선).
/// 손 모양은 아직 없음 — 아트 납품 때 handSlot 자리에 붙인다.
/// 스위치: HeldPaper.On = false 면 예전 칠판 보기 화면·Tab 전표 화면 상자로 돌아간다(다음 칠판 SetLines 부터).
/// </summary>
public class HeldPaper : MonoBehaviour
{
    /// <summary>D-269 스위치. false = 예전(칠판 보기 UI + Tab 누르는 동안 화면 전표).</summary>
    public static bool On = true;

    public enum Pose { Pinned, Down, Up, Read }

    // 종이 실물 크기(m)와 캔버스 칸 — 읽기 자세에서 1 칸 = 화면 도트 1칸(또는 그 정수배)
    public const float WidthM = 0.12f, HeightM = 0.16f;
    public const int CW = 150, CH = 200;
    const float MPerUnit = HeightM / CH;           // 0.0008 m
    const float AnimSec = 0.3f;
    const float UpCover = 0.23f;                    // 들어 올렸을 때 화면 아래에서 종이 위끝까지(세로 비율)
    const float UpTiltDeg = 34f;                    // 위끝이 뒤로 젖혀짐
    const float Bend = 0.003f;                      // 양옆이 뒤로 살짝 휨(m)
    const float ShadeLift = 1.8f;                   // 그늘 쪽일 때 종이 밝기 배수(가설 — 얼굴 가까이 든 종이는 반사광을 받는다)
    const float AmbientBoost = 1.8f;              // 손에 든 종이의 그늘 빛 배수(가설 — 얼굴 가까이 든 종이는 그늘에서도 읽힌다)

    public Pose State { get; private set; } = Pose.Pinned;
    /// <summary>QA: 지금 화면에서 종이가 덮은 세로 비율(0~1, 화면 안 부분만) · 가로 비율.</summary>
    public float CoverY { get; private set; }
    public float CoverX { get; private set; }
    /// <summary>QA·봇: 종이에 적힌 글 그대로.</summary>
    public string Written { get; private set; } = "";
    public bool Moving => Time.unscaledTime - animAt < AnimSec;
    /// <summary>나중에 손 모델을 붙일 자리(종이 아래 가운데). 지금은 빈 Transform.</summary>
    public Transform HandSlot { get; private set; }

    Camera cam;
    Transform root;              // 크기(k) 를 거는 몸 — 카메라 near 가 멀면 종이 전체를 키워 같은 화면 크기로
    Canvas canvas;
    Text title, body, to, addr, when, mapFrom, mapTo;
    RectTransform mapBox;
    MeshRenderer paperMr;
    Collider col;
    Vector3 fromPos; Quaternion fromRot; float fromScale = 1f;
    float animAt = -99f;
    Font font, small;

    static Texture2D paperTex;
    static Material paperMat, pinMat;

    // ── 만들기 ──
    public static HeldPaper Create(string name, Font body, Font small)
    {
        var go = new GameObject(name);
        var hp = go.AddComponent<HeldPaper>();
        hp.font = body; hp.small = small != null ? small : body;
        hp.Build();
        return hp;
    }

    void Build()
    {
        root = transform;
        var paper = new GameObject("Paper", typeof(MeshFilter), typeof(MeshRenderer));
        paper.transform.SetParent(root, false);
        paper.GetComponent<MeshFilter>().sharedMesh = PaperMesh();
        paperMr = paper.GetComponent<MeshRenderer>();
        paperMr.sharedMaterial = PaperMat();
        paperMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        paperMr.receiveShadows = true;
        var bc = paper.AddComponent<BoxCollider>(); bc.size = new Vector3(WidthM, HeightM, 0.01f); col = bc;

        HandSlot = new GameObject("HandSlot").transform;
        HandSlot.SetParent(root, false); HandSlot.localPosition = new Vector3(0f, -HeightM * 0.5f, 0f);

        // 글자 캔버스: 종이 앞(-z = 보는 쪽) 0.6 mm. 캔버스 앞면은 -z 쪽에서 읽힌다.
        var cg = new GameObject("Ink", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        cg.transform.SetParent(root, false);
        cg.transform.localPosition = new Vector3(0f, 0f, -0.0006f);
        cg.transform.localScale = Vector3.one * MPerUnit;
        canvas = cg.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        cg.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 1f;   // 글자를 fontSize 도트 그대로(칠판과 같은 방식)
        ((RectTransform)cg.transform).sizeDelta = new Vector2(CW, CH);

        title = Txt("Title", small, 10, 8, 10);
        body = Txt("Item", font, 12, 8, 26);
        body.horizontalOverflow = HorizontalWrapMode.Wrap; body.rectTransform.sizeDelta = new Vector2(CW - 16, 30);   // 긴 주문은 두 줄
        to = Txt("To", font, 12, 8, 58);
        addr = Txt("Addr", font, 12, 8, 74);
        Rule("Rule0", 22); Rule("Rule1", 56);
        // 약도 칸(손그림 대역): 테 + 보관소 □ → 찻집 ⌂ 점선
        const int mh = 72;
        mapBox = new GameObject("Map", typeof(RectTransform)).GetComponent<RectTransform>();
        mapBox.SetParent(cg.transform, false); TopLeft(mapBox, 8, 92, CW - 16, mh);
        Frame(mapBox, CW - 16, mh);
        DashPath(mapBox, new[] { new Vector2(14, 14), new Vector2(46, 20), new Vector2(64, 38), new Vector2(100, 44), new Vector2(118, 56) });
        Square(mapBox, new Vector2(10, 10), 8, 1f);               // 보관소
        House(mapBox, new Vector2(118, 58));                        // 찻집
        mapFrom = Txt("MapFrom", small, 10, 0, 0, mapBox); TopLeft(mapFrom.rectTransform, 20, mh - 22, 60, 11);
        mapTo = Txt("MapTo", small, 10, 0, 0, mapBox); TopLeft(mapTo.rectTransform, 48, 8, 60, 11); mapTo.alignment = TextAnchor.UpperRight;
        mapFrom.text = "보관소"; mapTo.text = "찻집";
        when = Txt("When", font, 12, 8, 172);
        Rule("Rule2", 168);
    }

    Text Txt(string n, Font f, int size, int x, int yFromTop, Transform parent = null)
    {
        var g = new GameObject(n, typeof(RectTransform));
        g.transform.SetParent(parent != null ? parent : canvas.transform, false);
        var t = g.AddComponent<Text>();
        t.font = f; t.fontSize = size; t.color = Ink; t.alignment = TextAnchor.UpperLeft; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        TopLeft(t.rectTransform, x, yFromTop, CW - 2 * x, size + 4);
        return t;
    }

    static void TopLeft(RectTransform r, float x, float yFromTop, float w, float h)
    {
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(Mathf.Round(x), -Mathf.Round(yFromTop)); r.sizeDelta = new Vector2(w, h);
    }

    Image Dot(Transform parent, float x, float y, float w, float h, float alpha)
    {
        var g = new GameObject("i", typeof(RectTransform));
        g.transform.SetParent(parent, false);
        var im = g.AddComponent<Image>(); im.raycastTarget = false; im.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
        var r = im.rectTransform; r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
        r.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y)); r.sizeDelta = new Vector2(w, h);
        return im;
    }

    /// <summary>손으로 그은 가로줄 — 1칸 굵기, 군데군데 끊김(볼펜 자국).</summary>
    void Rule(string n, int yFromTop)
    {
        var r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false); TopLeft(r, 8, yFromTop, CW - 16, 1);
        int x = 0, k = 0;
        while (x < CW - 16) { int len = 9 + (k * 7) % 13; Dot(r, x, 0, Mathf.Min(len, CW - 16 - x), 1, 0.55f); x += len + 1 + (k % 3 == 0 ? 2 : 0); k++; }
    }

    void Frame(RectTransform box, float w, float h)
    {
        Dot(box, 0, 0, w, 1, 0.7f); Dot(box, 0, h - 1, w, 1, 0.7f); Dot(box, 0, 0, 1, h, 0.7f); Dot(box, w - 1, 0, 1, h, 0.7f);
    }

    void Square(RectTransform box, Vector2 at, int s, float a)
    {
        Dot(box, at.x, at.y, s, 1, a); Dot(box, at.x, at.y + s - 1, s, 1, a); Dot(box, at.x, at.y, 1, s, a); Dot(box, at.x + s - 1, at.y, 1, s, a);
    }

    void House(RectTransform box, Vector2 c)
    {
        Square(box, c + new Vector2(-4, -8), 8, 1f);
        for (int i = 0; i < 5; i++) { Dot(box, c.x - 5 + i, c.y + i - 1, 1, 1, 1f); Dot(box, c.x + 4 - i, c.y + i - 1, 1, 1, 1f); }
    }

    /// <summary>점선: 2칸 긋고 2칸 비움, 꺾인 길 따라.</summary>
    void DashPath(RectTransform box, Vector2[] pts)
    {
        float carry = 0f;
        for (int s = 0; s + 1 < pts.Length; s++)
        {
            Vector2 a = pts[s], b = pts[s + 1]; float len = Vector2.Distance(a, b);
            for (float t = carry; t < len; t += 1f)
            {
                float ph = Mathf.Repeat(t - carry, 4f);
                if (ph < 2f) { var p = Vector2.Lerp(a, b, t / len); Dot(box, p.x, p.y, 1, 1, 0.9f); }
            }
            carry = 0f;
        }
    }

    // 볼펜 남색 잉크. 선형 밝기 0.074 — 노을 역광 테(DuskRim: 밝기 0.06 아래 + 위·옆이 밝으면 주황 1px)가 글자에 안 걸리는 가장 진한 쪽.
    static Color Ink => new Color(0.30f, 0.29f, 0.40f, 1f);

    // ── 내용 ──
    /// <summary>전표 내용: 받는 사람·주소·약도·약속 시각. 글은 도현이 정한 것(줄리아·찻집) + 자리 표시.</summary>
    public void SetSlip(OrderJob job)
    {
        title.text = "전표";
        body.text = job != null ? job.displayName : "";
        string who = job != null && !string.IsNullOrEmpty(job.recipientName) ? job.recipientName : "(받는 사람)";
        to.text = "받는 사람  " + who;
        addr.text = "주소  " + (job != null && !string.IsNullOrEmpty(job.address) ? job.address : "○○길(이름 미정)");   // 화면 전표와 같은 자리 표시
        when.text = "약속  (시각 자리)";
        Written = string.Join("\n", title.text, body.text, to.text, addr.text, "[약도] " + mapFrom.text + " → " + mapTo.text, when.text);
    }

    /// <summary>칠판에 꽂힌 채 보이는 글(집기 전) — 주문 한 줄만.</summary>
    public void SetPreview(string line)
    {
        title.text = "주문"; body.text = line; to.text = addr.text = when.text = "";
        Written = title.text + "\n" + line;
    }

    // ── 자세 ──
    /// <summary>칠판 등 세상 물건에 핀으로 꽂는다(실물 크기, 보는 쪽 = parent 의 +forward).</summary>
    public void PinTo(Transform parent, Vector3 localPos)
    {
        State = Pose.Pinned;
        root.SetParent(parent, false);
        root.localPosition = localPos;
        root.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);   // 종이 +z = 판 안쪽, 보는 사람 = 종이 -z
        root.localScale = Vector3.one;
        if (col != null) col.enabled = true;
        // 핀
        var pin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pin.name = "Pin"; Destroy(pin.GetComponent<Collider>());
        pin.transform.SetParent(root, false);
        pin.transform.localPosition = new Vector3(0f, HeightM * 0.5f - 0.012f, -0.004f);
        pin.transform.localScale = Vector3.one * 0.011f;
        var pr = pin.GetComponent<MeshRenderer>(); pr.sharedMaterial = PinMat(); pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>세상(핀)에서 떼어 손으로 — 지금 자리에서 to 자세로 0.3초.</summary>
    public void TakeToHand(Camera c, Pose to)
    {
        cam = c;
        var pin = root.Find("Pin"); if (pin != null) Destroy(pin.gameObject);
        if (col != null) col.enabled = false;
        root.SetParent(cam.transform, true);
        fromPos = root.localPosition; fromRot = root.localRotation; fromScale = root.localScale.x;
        State = to; animAt = Time.unscaledTime;
    }

    public void SetPose(Pose p)
    {
        if (cam == null || p == State || p == Pose.Pinned) return;
        fromPos = root.localPosition; fromRot = root.localRotation; fromScale = root.localScale.x;
        State = p; animAt = Time.unscaledTime;
    }

    /// <summary>Tab: 내려놓음 → 들기 → 읽기 → 내려놓음.</summary>
    public void Cycle() => SetPose(State == Pose.Down ? Pose.Up : State == Pose.Up ? Pose.Read : Pose.Down);

    /// <summary>카메라 기준 자세(위치·회전·크기). 시야각·화면 줄 수·near 로 매 프레임 다시 계산 — 주행 시야각이 바뀌어도 화면 크기가 같다.</summary>
    void Target(Pose p, out Vector3 pos, out Quaternion rot, out float k)
    {
        float t = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        int rows = RenderRows();
        int m = Mathf.Max(1, Mathf.RoundToInt(rows * 0.55f / CH));     // 1 캔버스 칸 = 화면 m 칸
        float dRead = HeightM * rows / (CH * m * 2f * t);
        k = Mathf.Max(1f, cam.nearClipPlane * 2.2f / dRead);           // near 가 먼 주행 카메라 — 통째로 키우고 그만큼 멀리
        dRead *= k;
        if (p == Pose.Read)
        {
            pos = new Vector3(0f, 0f, dRead);
            rot = Quaternion.Euler(0f, 0f, 0f);
            return;
        }
        // Up: 위끝이 뒤로 젖혀진 채 화면 아래. 위끝(가운데)이 화면 아래에서 UpCover 높이에 오게 가운데 y 를 푼다.
        float d = dRead * 1.25f, h2 = HeightM * 0.5f * k, th = UpTiltDeg * Mathf.Deg2Rad;
        float topZ = d + h2 * Mathf.Sin(th);
        float ndc = -1f + 2f * UpCover;
        float cy = ndc * topZ * t - h2 * Mathf.Cos(th);
        pos = new Vector3(0.012f * k, cy, d);
        rot = Quaternion.Euler(UpTiltDeg, -3f, -3f);
        if (p == Pose.Down) pos.y -= 2.4f * h2 + 0.02f * k;              // 화면 아래 밖
    }

    int RenderRows()
    {
        var lr = cam != null ? cam.GetComponent<LowResRenderTest>() : null;
        return lr != null && lr.isActiveAndEnabled && lr.RenderSize.y > 0 ? lr.RenderSize.y : Mathf.Max(1, cam.pixelHeight);
    }

    void LateUpdate()
    {
        if (cam == null || State == Pose.Pinned) return;
        Target(State, out var p, out var r, out var k);
        float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.unscaledTime - animAt) / AnimSec));
        root.localPosition = Vector3.Lerp(fromPos, p, a);
        root.localRotation = Quaternion.Slerp(fromRot, r, a);
        root.localScale = Vector3.one * Mathf.Lerp(fromScale, k, a);
        bool show = !(State == Pose.Down && a >= 1f);
        if (paperMr.enabled != show) { paperMr.enabled = show; canvas.enabled = show; }
        SyncLight();
        Measure();
    }

    /// <summary>세상과 같은 해·그늘·안개 값을 장면의 LofiSurface 머티리얼 하나에서 따라간다(시간대가 바뀌면 같이).</summary>
    static Material lightRef;
    void SyncLight()
    {
        if (lightRef == null)
            foreach (var rr in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var m = rr.sharedMaterial;
                if (m == null || m == paperMat || m == pinMat || m.shader == null || m.shader.name != "PitCrew/LofiPilot/Surface") continue;
                lightRef = m; if (rr.name.ToLower().Contains("depot")) break;
            }
        if (lightRef == null || paperMat == null) return;
        // 그늘 빛만 AmbientBoost 배: 그대로면 그늘(항구 노을 캡처 09·10)에서 종이가 잉크(밝기 0.3 고정 — DuskRim 하한)와 같은 회색이 돼 못 읽음. 해 쪽 명암은 세상 그대로.
        paperMat.SetColor("_Ambient", lightRef.GetColor("_Ambient") * AmbientBoost);
        paperMat.SetColor("_FogColor", lightRef.GetColor("_FogColor"));
        paperMat.SetFloat("_FogStart", lightRef.GetFloat("_FogStart"));
        paperMat.SetFloat("_FogEnd", lightRef.GetFloat("_FogEnd"));
        paperMat.SetFloat("_FaceShade", lightRef.GetFloat("_FaceShade"));

        // 손에 든 종이가 해 반대쪽이거나 건물 그늘이면 종이만 ShadeLift 배 밝힘(캡처 12_read_shade: 그대로면 잉크와 같은 회색). 0.33초에 걸쳐 바뀌어 깜빡이지 않음.
        if (sun == null)
        {
            sun = RenderSettings.sun;
            if (sun == null) foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
        }
        float want = 1f;
        if (sun != null && cam != null && State != Pose.Pinned)
        {
            Vector3 toSun = -sun.transform.forward;
            float ndl = Vector3.Dot((cam.transform.position - transform.position).normalized, toSun);
            bool blocked = Physics.Raycast(transform.position + toSun * 0.3f, toSun, 300f, ~0, QueryTriggerInteraction.Ignore);
            want = blocked ? ShadeLift : Mathf.Lerp(ShadeLift, 1f, Mathf.Clamp01(ndl / 0.3f));
        }
        shadeLift = Mathf.MoveTowards(shadeLift, want, Time.unscaledDeltaTime * 3f);
        paperMat.SetColor("_Tint", PaperTint * shadeLift);
    }
    static Light sun;
    float shadeLift = 1f;
    static readonly Color PaperTint = new Color(1.15f, 1.13f, 1.08f, 1f);   // 실내 조명에서도 종이가 밝게

    void Measure()
    {
        if (!paperMr.enabled) { CoverY = CoverX = 0f; return; }
        float y0 = 1f, y1 = 0f, x0 = 1f, x1 = 0f;
        foreach (var c in new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(-1, 1, 0), new Vector3(1, 1, 0) })
        {
            var v = cam.WorldToViewportPoint(root.TransformPoint(new Vector3(c.x * WidthM * 0.5f, c.y * HeightM * 0.5f, 0f)));
            y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y); x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x);
        }
        CoverY = Mathf.Max(0f, Mathf.Min(1f, y1) - Mathf.Max(0f, y0));
        CoverX = Mathf.Max(0f, Mathf.Min(1f, x1) - Mathf.Max(0f, x0));
    }

    // ── 재료 ──
    /// <summary>12×16 칸 판, 양옆이 Bend 만큼 뒤로 휨. 앞면 = -z(보는 쪽). uv 는 종이 사진 1 텍셀 ≈ 캔버스 1 칸.</summary>
    static Mesh PaperMesh()
    {
        const int nx = 8, ny = 4;
        var m = new Mesh { name = "HeldPaper" };
        var v = new Vector3[(nx + 1) * (ny + 1)]; var uv = new Vector2[v.Length];
        for (int y = 0; y <= ny; y++)
            for (int x = 0; x <= nx; x++)
            {
                float u = x / (float)nx, w = y / (float)ny, xn = u * 2f - 1f;
                v[y * (nx + 1) + x] = new Vector3((u - 0.5f) * WidthM, (w - 0.5f) * HeightM, Bend * xn * xn);
                uv[y * (nx + 1) + x] = new Vector2(u * CW / 128f, w * CH / 128f);   // 텍셀 1 = 캔버스 1칸(읽기 자세에서 화면 1칸) — 2칸이면 얼룩이 굵어짐
            }
        var tri = new int[nx * ny * 6]; int k = 0;
        for (int y = 0; y < ny; y++)
            for (int x = 0; x < nx; x++)
            {
                int i0 = y * (nx + 1) + x, i1 = i0 + 1, i2 = i0 + nx + 1, i3 = i2 + 1;
                tri[k++] = i0; tri[k++] = i2; tri[k++] = i1; tri[k++] = i1; tri[k++] = i2; tri[k++] = i3;   // -z 에서 시계방향 = 앞면
            }
        m.vertices = v; m.uv = uv; m.triangles = tri;
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Material PaperMat()
    {
        if (paperMat != null) return paperMat;
        var sh = Shader.Find("PitCrew/LofiPilot/Surface");
        paperMat = new Material(sh != null ? sh : Shader.Find("Universal Render Pipeline/Unlit")) { name = "HeldPaper_Paper" };
        paperMat.mainTexture = PaperTex();
        paperMat.SetColor("_Tint", PaperTint);
        return paperMat;
    }

    static Material PinMat()
    {
        if (pinMat != null) return pinMat;
        pinMat = new Material(PaperMat()) { name = "HeldPaper_Pin" };
        pinMat.mainTexture = Texture2D.whiteTexture;
        pinMat.SetColor("_Tint", new Color(0.62f, 0.2f, 0.16f, 1f));
        return pinMat;
    }

    /// <summary>paper 사진(CC0)을 읽을 수 있게 복사해 미색 종이로(결은 사진 밝기 그대로 1.2배).</summary>
    static Texture2D PaperTex()
    {
        if (paperTex != null) return paperTex;
        // cream.png 는 결이 0(표준편차 0 — 단색)이라 못 씀. paper.png = ambientCG CardboardSet001(CC0, PROVENANCE.md) 표준편차 8.5.
        var src = Resources.Load<Texture2D>("LofiUI/paper");
        if (src == null) { Debug.LogWarning("[HeldPaper] Resources/LofiUI/paper 없음"); return Texture2D.whiteTexture; }
        var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prev = RenderTexture.active;
        Graphics.Blit(src, rt); RenderTexture.active = rt;
        var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        t.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
        var px = t.GetPixels();
        // 골판지 색은 버리고 밝기 결만 남겨 미색 종이에 싣는다: 종이색 × (1 + 결 × 1.2). 2.4배·2칸 텍셀은 골판지 얼룩(점무늬)으로 보였다(시험 캡처 x_try2·3).
        float mean = 0f; foreach (var c in px) mean += c.grayscale; mean /= px.Length;
        var paper = new Color(0.93f, 0.9f, 0.81f);
        for (int i = 0; i < px.Length; i++)
        {
            float g = 1f + (px[i].grayscale - mean) / Mathf.Max(1e-3f, mean) * 1.2f;
            px[i] = new Color(Mathf.Clamp01(paper.r * g), Mathf.Clamp01(paper.g * g), Mathf.Clamp01(paper.b * g), 1f);
        }
        t.SetPixels(px); t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Repeat; t.name = "HeldPaper_Paper"; t.Apply(false);
        paperTex = t;
        return t;
    }
}
