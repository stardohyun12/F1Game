using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// UI 1판(D-240, 과제 20261101-0515): 화면 위에 매끈하게 얹는 HUD 대신, 저해상도 RT(LowResRenderTest) 안에 같이 그리는 도트 UI.
/// Screen Space - Camera 캔버스를 Main Camera 에 붙인다 → 캔버스 1 단위 = RT 1 픽셀, 세상과 같은 격자·같은 Present(Nearest·노을 역광 테)를 받는다.
/// 그리는 것(전부 간단히):
///   주행 — 위 가운데 목적지 방향 화살표 1개 + 화물 위험 "!" 1개(위험할 때만) + Tab 누르는 동안 전표.
///   걷기 — 가운데 조준점 2×2 + [E] 칩을 그 물건 곁에(OnFootDirector.PromptAnchor).
///   대화 — 말하는 사람 머리 위 말풍선(이름표 + 한 글자씩). 시선 이동은 OnFootDirector 가 한다.
/// 기존 HUD(속도·기어·화물 막대·목적지 등)는 이 컴포넌트가 켜져 있는 동안만 끈다(SetActive false, 지우지 않음). 이 컴포넌트를 끄면 되돌린다.
/// 글꼴: Galmuri11/9(OFL, Assets/UI/Fonts/Galmuri) — 가져오기 설정 Hinted Raster = 안티앨리어싱 없음. 아트 스프라이트 슬롯은 아래 *Sprite 필드(비면 코드 임시 모양).
/// </summary>
[DefaultExecutionOrder(200)]
public class DotScreenUI : MonoBehaviour
{
    [SerializeField] OnFootDirector director;
    [Tooltip("본문 글꼴(Galmuri11, 12px).")]
    [SerializeField] Font font;
    [Tooltip("작은 글꼴(Galmuri9, 10px) — 키 칩·전표 제목.")]
    [SerializeField] Font smallFont;
    [SerializeField] int fontSize = 12;
    [SerializeField] int smallSize = 10;
    [Tooltip("말풍선 한 글자씩 — 초당 글자 수.")]
    [SerializeField] float typeCps = 30f;
    [Tooltip("말풍선 최대 너비(RT 픽셀).")]
    [SerializeField] int bubbleMaxW = 190;
    [Tooltip("기존 HUD(이름이 HUD 로 끝나는 것·화살표·트렁크 캠)를 끈다. 끄면 기존 HUD 가 그대로 나온다.")]
    [SerializeField] bool hideLegacyHud = true;
    [Tooltip("화물 위험 비율(현재 / 경고 임계) 이 이 이상이면 '!' 표시, critical 이상이면 깜박.")]
    [SerializeField] float warnShow = 0.7f, warnCrit = 1.0f;
    [Tooltip("탑승 직후 이 초 동안만 'Tab 전표' 안내를 보인다.")]
    [SerializeField] float slipHintSec = 6f;

    [Header("아트 슬롯(비면 코드 임시 모양)")]
    public Sprite boxSprite, tabSprite, keySprite, tailSprite, warnSprite;

    // 9판 UI 토큰(UIPalette) — 종이 크림·잉크.
    static Color Paper => UIPalette.Paper;
    static Color Ink => UIPalette.Ink;
    static Color Signal => UIPalette.Signal;

    Camera cam;
    Canvas canvas;
    RectTransform root;
    Image dot, arrowImg, warnBox, chipBox, chipKey, bubble, tail, nameTab, slip;
    Text chipLabel, chipKeyText, bubbleText, nameText, warnText, slipTitle, slipBody, hintText;
    Texture2D arrowTex;
    int arrowStep = int.MinValue;
    readonly List<GameObject> hidden = new List<GameObject>();
    OnFootDirector.Mode lastState;
    float cartAt = -99f;
    string bubbleLine;
    TextGenerator gen;

    /// <summary>QA·봇이 읽는 것 — 지금 보이는 요소 이름들.</summary>
    public string Visible { get; private set; } = "";

    void Start()
    {
        if (director == null) director = FindFirstObjectByType<OnFootDirector>();
        cam = Camera.main;
        if (cam == null) { enabled = false; return; }
        lowRes = cam.GetComponent<LowResRenderTest>();
        Build();
    }

    void OnDisable()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
        RestoreLegacy();
    }

    void OnEnable() { if (canvas != null) canvas.gameObject.SetActive(true); }

    void OnDestroy() { Font.textureRebuilt -= OnFontRebuilt; if (canvas != null) Destroy(canvas.gameObject); if (arrowTex != null) Destroy(arrowTex); }

    void OnFontRebuilt(Font f)
    {
        if ((f == font || f == smallFont) && f.material != null && f.material.mainTexture != null) f.material.mainTexture.filterMode = FilterMode.Point;
    }

    /// <summary>캔버스를 카메라 앞 평면에 RT 크기(W×H 단위)로 — 1 단위 = RT 1 픽셀. 시야각·near·RT 크기가 바뀌어도 매 프레임 맞춘다.</summary>
    void Fit()
    {
        float d = cam.nearClipPlane + 0.02f;
        float fh = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        root.localPosition = new Vector3(0f, 0f, d);
        root.localRotation = Quaternion.identity;
        root.sizeDelta = new Vector2(W, H);
        root.localScale = Vector3.one * (fh / H);
    }

    // ── 만들기 ──
    void Build()
    {
        // World Space 캔버스를 카메라 바로 앞(near + 2cm)에 붙이고, 화면 높이 = RT 세로 픽셀 수가 되게 크기를 맞춘다(Fit).
        // Screen Space - Camera 는 화면(1920×1080) 단위로 배치·글자 래스터를 해서 RT 에 1/3 로 눌려 흐려졌다(1차 시도, 이 판 보고서 §3).
        // World Space 는 글자를 fontSize 픽셀 그대로 굽는다 → Galmuri 12px = RT 12px.
        var go = new GameObject("DotScreenUI_Canvas", typeof(Canvas));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        canvas.scaleFactor = 1f;
        canvas.referencePixelsPerUnit = 100f;
        canvas.sortingOrder = 32000;
        root = (RectTransform)go.transform;
        root.SetParent(cam.transform, false);
        root.pivot = new Vector2(0.5f, 0.5f);
        gen = new TextGenerator();
        foreach (var f in new[] { font, smallFont })            // 글자 텍스처를 Nearest 로(반 픽셀 어긋나도 번지지 않게)
            if (f != null && f.material != null && f.material.mainTexture != null) f.material.mainTexture.filterMode = FilterMode.Point;
        Font.textureRebuilt += OnFontRebuilt;

        var box = boxSprite != null ? boxSprite : MakeBox(Paper, Ink);
        var dark = tabSprite != null ? tabSprite : MakeBox(Ink, Ink);
        var key = keySprite != null ? keySprite : MakeBox(Paper, Ink);
        var tl = tailSprite != null ? tailSprite : MakeTail(Paper, Ink);
        var wb = warnSprite != null ? warnSprite : MakeBox(Signal, Ink);

        dot = Img("Dot", null, Paper, 2, 2);
        arrowTex = new Texture2D(15, 15, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Arrow" };
        arrowImg = Img("Arrow", Sprite.Create(arrowTex, new Rect(0, 0, 15, 15), Vector2.zero, 100f), Color.white, 15, 15);
        warnBox = Img("Warn", wb, Color.white, 11, 14, true);
        warnText = Txt("Text", warnBox.transform, font, fontSize, Paper, TextAnchor.MiddleCenter); warnText.text = "!";
        Fill(warnText.rectTransform, 0, 1, 0, 0);

        chipBox = Img("Chip", dark, Color.white, 40, 15, true);
        chipKey = Img("Key", key, Color.white, 11, 11, true, chipBox.transform);
        chipKey.rectTransform.anchoredPosition = new Vector2(2, 2);
        chipKeyText = Txt("Text", chipKey.transform, smallFont, smallSize, Ink, TextAnchor.MiddleCenter); chipKeyText.text = "E";
        Fill(chipKeyText.rectTransform, 1, 0, 0, 0);
        chipLabel = Txt("Label", chipBox.transform, font, fontSize, Paper, TextAnchor.MiddleLeft);
        chipLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

        tail = Img("Tail", tl, Color.white, 7, 4);
        bubble = Img("Bubble", box, Color.white, 60, 20, true);
        bubbleText = Txt("Text", bubble.transform, font, fontSize, Ink, TextAnchor.UpperLeft);
        bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap; bubbleText.supportRichText = true; bubbleText.lineSpacing = 1f;
        nameTab = Img("Name", dark, Color.white, 30, 13, true);
        nameText = Txt("Text", nameTab.transform, smallFont, smallSize, Paper, TextAnchor.MiddleCenter);
        Fill(nameText.rectTransform, 0, 0, 0, 1);

        slip = Img("Slip", box, Color.white, 150, 34, true);
        slipTitle = Txt("Title", slip.transform, smallFont, smallSize, Ink, TextAnchor.UpperLeft); slipTitle.text = "전표";
        slipTitle.rectTransform.anchoredPosition = new Vector2(5, 22); slipTitle.rectTransform.sizeDelta = new Vector2(100, 11);
        slipBody = Txt("Body", slip.transform, font, fontSize, Ink, TextAnchor.UpperLeft);
        slipBody.rectTransform.anchoredPosition = new Vector2(5, 6); slipBody.rectTransform.sizeDelta = new Vector2(400, 14);
        slipBody.horizontalOverflow = HorizontalWrapMode.Overflow;
        hintText = Txt("Hint", root, smallFont, smallSize, Paper, TextAnchor.LowerLeft); hintText.text = "Tab 전표";
        hintText.rectTransform.sizeDelta = new Vector2(80, 11);

        HideAll();
        Debug.Log($"[DotScreenUI] 캔버스 {W}×{H} (RT={(cam.targetTexture != null ? cam.targetTexture.width + "×" + cam.targetTexture.height : "없음")}) font={(font != null ? font.name : "없음")}");
    }

    Image Img(string name, Sprite s, Color c, int w, int h, bool sliced = false, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent != null ? parent : root, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = Vector2.zero; r.sizeDelta = new Vector2(w, h);
        var im = go.GetComponent<Image>(); im.sprite = s; im.color = c; im.raycastTarget = false;
        if (sliced && s != null) { im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 1f; }
        return im;
    }

    Text Txt(string name, Transform parent, Font f, int size, Color c, TextAnchor a)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
        var t = go.AddComponent<Text>();
        t.font = f != null ? f : font; t.fontSize = size; t.color = c; t.alignment = a; t.raycastTarget = false;
        t.verticalOverflow = VerticalWrapMode.Overflow; t.supportRichText = false;
        go.AddComponent<PixelSnapText>().root = root;
        return t;
    }

    static void Fill(RectTransform r, int l, int b, int rr, int tt)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -tt);
    }

    /// <summary>7×7 상자(모서리 1칸 깎은 계단, 테 1px) — 9분할 테두리 2.</summary>
    static Sprite MakeBox(Color fill, Color edge)
    {
        var t = new Texture2D(7, 7, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Box" };
        for (int y = 0; y < 7; y++)
            for (int x = 0; x < 7; x++)
            {
                bool cx = x == 0 || x == 6, cy = y == 0 || y == 6;
                t.SetPixel(x, y, cx && cy ? Color.clear : (cx || cy) ? edge : fill);
            }
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, 7, 7), Vector2.zero, 100f, 0, SpriteMeshType.FullRect, new Vector4(2, 2, 2, 2));
    }

    /// <summary>말풍선 꼬리 7×4 — 맨 윗줄은 상자 밑 테를 덮는다(속은 종이색).</summary>
    static Sprite MakeTail(Color fill, Color edge)
    {
        var t = new Texture2D(7, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Tail" };
        string[] rows = { "...o...", "..ofo..", ".offfo.", "offfffo" };   // 아래(y=0) → 위(y=3)
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 7; x++)
            {
                char ch = rows[y][x];
                t.SetPixel(x, y, ch == 'o' ? edge : ch == 'f' ? fill : Color.clear);
            }
        t.SetPixel(0, 3, edge); t.SetPixel(6, 3, edge);
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, 7, 4), Vector2.zero, 100f);
    }

    /// <summary>방향 화살표를 픽셀 단위로 직접 찍는다(16방향). 회전한 그림을 늘리지 않으니 흐린 픽셀이 없다.</summary>
    void DrawArrow(int step)
    {
        float a = step * (Mathf.PI * 2f / 16f);
        float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        var inside = new bool[15, 15];
        for (int y = 0; y < 15; y++)
            for (int x = 0; x < 15; x++)
            {
                float px = x - 7f, py = y - 7f;
                // 화면 점을 +a 만큼 돌려 화살표 공간(+y 가 앞)에서 판정 = 화살표는 시계 방향으로 a. step 양수 = 오른쪽.
                float ux = px * ca - py * sa, uy = px * sa + py * ca;
                bool head = uy >= -0.5f && uy <= 5.5f && Mathf.Abs(ux) <= (5.5f - uy) * 0.95f;
                bool stem = uy >= -5.5f && uy < -0.5f && Mathf.Abs(ux) <= 1.6f;
                inside[x, y] = head || stem;
            }
        for (int y = 0; y < 15; y++)
            for (int x = 0; x < 15; x++)
            {
                Color c = Color.clear;
                if (inside[x, y]) c = Paper;
                else if ((x > 0 && inside[x - 1, y]) || (x < 14 && inside[x + 1, y]) || (y > 0 && inside[x, y - 1]) || (y < 14 && inside[x, y + 1])) c = Ink;
                arrowTex.SetPixel(x, y, c);
            }
        arrowTex.Apply(false);
    }

    // ── 매 프레임 ──
    void LateUpdate()
    {
        if (canvas == null) return;
        Fit();   // 걷기(near 0.05)·주행 카메라 near·FOV 가 다르다 — 항상 바로 앞, 화면 꽉
        HideAll();
        var vis = new System.Text.StringBuilder();
        var st = director != null && director.isActiveAndEnabled ? director.State : OnFootDirector.Mode.InCart;
        if (st != lastState) { if (st == OnFootDirector.Mode.InCart) cartAt = Time.unscaledTime; lastState = st; }

        if (st == OnFootDirector.Mode.InCart) { DriveUI(vis); if (hideLegacyHud) HideLegacy(); }
        else RestoreLegacy();

        if (director != null && st == OnFootDirector.Mode.OnFoot)
        {
            if (director.InTalk) TalkUI(vis);
            else
            {
                Show(dot, Mathf.RoundToInt(W * 0.5f) - 1, Mathf.RoundToInt(H * 0.5f) - 1); vis.Append("dot ");
                ChipUI(vis);
            }
        }
        else if (director != null && st == OnFootDirector.Mode.InCart && director.Prompt.Length > 0) ChipUI(vis);
        Visible = vis.ToString().Trim();
    }

    // 실제 도트 격자 = LowResRenderTest.RenderSize(360 줄). 카메라 RT 는 URP renderScale 0.5 를 감안해 그 2배(1280×720)로
    // 잡혀 있어서 cam.pixelWidth 로 맞추면 UI 가 절반 크기로 그려져 글자 줄이 빠졌다(이 판 보고서 §3).
    LowResRenderTest lowRes;
    int W => lowRes != null && lowRes.isActiveAndEnabled ? lowRes.RenderSize.x : cam.pixelWidth;
    int H => lowRes != null && lowRes.isActiveAndEnabled ? lowRes.RenderSize.y : cam.pixelHeight;

    void HideAll()
    {
        foreach (var g in new Graphic[] { dot, arrowImg, warnBox, chipBox, bubble, tail, nameTab, slip, hintText })
            if (g != null && g.gameObject.activeSelf) g.gameObject.SetActive(false);
    }

    static void Show(Graphic g, int x, int y) { g.gameObject.SetActive(true); g.rectTransform.anchoredPosition = new Vector2(x, y); }

    bool ToScreen(Vector3 world, out Vector2 p)
    {
        var s = cam.WorldToScreenPoint(world);
        p = new Vector2(s.x * W / cam.pixelWidth, s.y * H / cam.pixelHeight);
        return s.z > 0.05f;
    }

    void DriveUI(System.Text.StringBuilder vis)
    {
        // 목적지 방향: 차 카메라 앞(수평) 기준으로 목적지가 몇 도 옆인지 → 16방향 화살표.
        var mgr = Services.Delivery as DeliveryManager;
        var tgt = mgr != null ? mgr.CurrentTarget : null;
        if (tgt != null)
        {
            Vector3 f = cam.transform.forward; f.y = 0f;
            Vector3 d = tgt.transform.position - cam.transform.position; d.y = 0f;
            if (f.sqrMagnitude > 1e-4f && d.sqrMagnitude > 1e-4f)
            {
                float ang = Vector3.SignedAngle(f, d, Vector3.up);
                int step = Mathf.RoundToInt(ang / 22.5f);
                if (step != arrowStep) { arrowStep = step; DrawArrow(step); }
                Show(arrowImg, W / 2 - 7, H - 6 - 15); vis.Append("arrow ");
            }
        }
        // 화물 위험 "!": CargoWarningHUD 와 같은 4 소스(급제동·코너·스핀·경사) 중 큰 것.
        var cargo = Services.Cargo as CargoBox;
        if (cargo != null && tgt != null)
        {
            float r = Mathf.Max(Mathf.Max(cargo.CurrentLongG / 0.9f, cargo.CurrentLatG / 1.0f), Mathf.Max(cargo.CurrentSpinDegPerSec / 100f, cargo.CurrentTiltDeg / 10f));
            bool blinkOff = r >= warnCrit && Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f;
            if (r >= warnShow && !blinkOff) { Show(warnBox, W / 2 + 7 + 4, H - 6 - 14); vis.Append("warn "); }
        }
        // 전표: Tab 누르는 동안. 탑승 직후 잠깐 안내.
        bool tab = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        var rc = Services.Run as RunController;
        var job = rc != null ? rc.CurrentJob : null;
        if (job != null && (tab || SimSlip))
        {
            slipBody.text = job.displayName;
            int bw = Mathf.Clamp(Mathf.CeilToInt(Pref(slipBody, job.displayName, 400)) + 11, 60, 300);
            slip.rectTransform.sizeDelta = new Vector2(bw, 34);
            Show(slip, 8, 8); vis.Append("slip ");
        }
        else if (job != null && Time.unscaledTime - cartAt < slipHintSec) { Show(hintText, 8, 8); vis.Append("hint "); }
    }

    /// <summary>봇·캡처용: Tab 을 누른 것과 같다.</summary>
    public bool SimSlip { get; set; }

    void ChipUI(System.Text.StringBuilder vis)
    {
        string p = director.Prompt;
        if (string.IsNullOrEmpty(p)) return;
        bool hasKey = p.StartsWith("E ");
        string label = hasKey ? p.Substring(2) : p;
        chipLabel.text = label;
        int lw = Mathf.CeilToInt(Pref(chipLabel, label, 400));
        int keyW = hasKey ? 13 : 0;
        chipKey.gameObject.SetActive(hasKey);
        int w = 3 + keyW + lw + 4, h = 15;
        chipBox.rectTransform.sizeDelta = new Vector2(w, h);
        var lr = chipLabel.rectTransform; lr.anchoredPosition = new Vector2(3 + keyW, 1); lr.sizeDelta = new Vector2(lw + 2, 13);
        // 그 물건 곁(오른쪽 위)에. 화면 밖·뒤면 조준점 바로 아래.
        int x, y;
        if (director.PromptAnchor is Vector3 a && ToScreen(a, out var sp) && sp.x > 0 && sp.x < W && sp.y > 0 && sp.y < H)
        { x = Mathf.RoundToInt(sp.x) + 6; y = Mathf.RoundToInt(sp.y) + 2; }
        else { x = W / 2 - w / 2; y = H / 2 - 10 - h; }
        x = Mathf.Clamp(x, 2, W - w - 2); y = Mathf.Clamp(y, 2, H - h - 2);
        Show(chipBox, x, y); vis.Append("chip ");
    }

    void TalkUI(System.Text.StringBuilder vis)
    {
        string full = director.TalkText;
        if (full != bubbleLine)
        {
            bubbleLine = full;
            int oneLine = Mathf.CeilToInt(Pref(bubbleText, full, 2000));
            int tw = Mathf.Min(oneLine, bubbleMaxW - 10);
            bubbleText.rectTransform.sizeDelta = new Vector2(tw + 1, 13);
            bubbleText.text = full;
            int th = Mathf.CeilToInt(bubbleText.preferredHeight);
            bubble.rectTransform.sizeDelta = new Vector2(tw + 10, th + 8);
            bubbleText.rectTransform.sizeDelta = new Vector2(tw + 1, th);
            bubbleText.rectTransform.anchoredPosition = new Vector2(5, 4);
            nameText.text = director.TalkWho;
            nameTab.rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Pref(nameText, director.TalkWho, 200)) + 8, 13);
        }
        // 한 글자씩: 안 나온 글자는 투명으로 그려 줄바꿈 자리를 미리 잡는다(글이 나오며 상자가 흔들리지 않게).
        int n = Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - director.TalkLineAt) * typeCps), 0, full.Length);
        bubbleText.supportRichText = true;
        bubbleText.text = n >= full.Length ? full : full.Substring(0, n) + "<color=#00000000>" + full.Substring(n) + "</color>";

        var sz = bubble.rectTransform.sizeDelta;
        int bw = (int)sz.x, bh = (int)sz.y;
        Vector3 head = director.SpeakerTop + Vector3.up * 0.03f;   // 0.12 는 대화 거리 2m 에서 머리 위 ~20px 떠 보였다
        int ax, ay;
        if (ToScreen(head, out var sp)) { ax = Mathf.RoundToInt(sp.x); ay = Mathf.RoundToInt(sp.y); }
        else { ax = W / 2; ay = H / 2; }
        // 꼬리 끝 = 머리 위. 화면 밖이면 가장자리 안으로(시선 이동 0.5초 동안만 생긴다).
        int bx = Mathf.Clamp(ax - bw / 2, 2, W - bw - 2);
        int by = Mathf.Clamp(ay + 3, 2, H - bh - 13 - 2);
        int tx = Mathf.Clamp(ax - 3, bx + 3, bx + bw - 10);
        Show(bubble, bx, by);
        Show(tail, tx, by - 3);
        Show(nameTab, bx + 4, by + bh - 1);
        vis.Append("bubble ");
    }

    float Pref(Text t, string s, float maxW)
    {
        var set = t.GetGenerationSettings(new Vector2(maxW, 0f));
        set.horizontalOverflow = HorizontalWrapMode.Overflow; set.richText = false;
        return gen.GetPreferredWidth(s, set) / Mathf.Max(0.01f, t.pixelsPerUnit);
    }

    // ── 기존 HUD 끄기(되돌릴 수 있게) ──
    float legacyScanAt = -99f;

    void HideLegacy()
    {
        if (Time.unscaledTime - legacyScanAt < 0.25f) return;   // 탑승 뒤 늦게 켜지는 HUD(DeliveryProgressHUD 등)도 잡는다
        legacyScanAt = Time.unscaledTime;
        foreach (var m in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (m == null || m.transform.IsChildOf(root)) continue;
            string n = m.GetType().Name;
            if (!(n.EndsWith("HUD") || LegacyExtra.Contains(n))) continue;
            var g = m.gameObject;
            var cv = g.GetComponent<Canvas>();
            if (!g.activeSelf || (cv != null && cv.isRootCanvas)) continue;   // 루트 캔버스(UIRoot)는 결과창 등도 담고 있어 통째로 끄지 않는다
            g.SetActive(false); hidden.Add(g);
        }
    }

    /// <summary>이름이 HUD 로 안 끝나지만 주행 화면에 뜨던 것.</summary>
    static readonly HashSet<string> LegacyExtra = new HashSet<string> { "DirectionArrow" };

    void RestoreLegacy()
    {
        foreach (var g in hidden) if (g != null) g.SetActive(true);
        hidden.Clear();
    }
}

/// <summary>
/// 글자 사각형을 RT 픽셀 격자에 맞춘다. Text 는 가운데 정렬·줄 간격 때문에 글자 사각형이 반 픽셀에 놓이곤 하고,
/// Nearest 텍스처를 반 픽셀 어긋나게 찍으면 Galmuri 획이 한 줄씩 빠진다(Play 실측: 크기 9~18 중 대부분 깨짐).
/// 글자마다 왼쪽 아래 꼭짓점을 캔버스 픽셀 정수로 반올림하고 같은 만큼 네 꼭짓점을 함께 민다(글자 크기는 그대로).
/// </summary>
[RequireComponent(typeof(Text))]
public class PixelSnapText : BaseMeshEffect
{
    public RectTransform root;      // 1 단위 = RT 1 픽셀인 캔버스 루트(가운데 원점, 640×360 → 원점이 픽셀 경계)

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || root == null) return;
        var toRoot = root.worldToLocalMatrix * transform.localToWorldMatrix;
        var fromRoot = toRoot.inverse;
        var v = new UIVertex();
        int n = vh.currentVertCount;
        for (int q = 0; q + 3 < n; q += 4)
        {
            vh.PopulateUIVertex(ref v, q);
            Vector3 p = toRoot.MultiplyPoint3x4(v.position);
            Vector3 d = new Vector3(Mathf.Round(p.x) - p.x, Mathf.Round(p.y) - p.y, 0f);
            Vector3 dl = fromRoot.MultiplyVector(d);
            for (int i = q; i < q + 4; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.position += dl;
                vh.SetUIVertex(v, i);
            }
        }
    }
}
