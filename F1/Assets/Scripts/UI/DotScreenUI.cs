using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// UI 1판(D-240, 과제 20261101-0515): 화면 위에 매끈하게 얹는 HUD 대신, 저해상도 RT(LowResRenderTest) 안에 같이 그리는 도트 UI.
/// World Space 캔버스를 Main Camera 바로 앞에 붙인다 → 캔버스 1 단위 = RT 1 픽셀, 세상과 같은 격자·같은 Present(Nearest·노을 역광 테)를 받는다.
/// UI 2판(D-241, 과제 20261101-2330): 색·질감도 세상과 같이.
///   · 모든 UI 를 전용 셰이더 PitCrew/UI/DotPalette 로 — 시간대 팔레트(10색)에서 가장 가까운 색 + 세상과 같은 Bayer 4×4 디더.
///     SunsetLook(오후·노을 프리셋)의 해 빛 색을 팔레트에 살짝 곱한다(노을 땐 UI 도 노을빛). 공유 .mat 없음(실행 중 생성).
///   · 종이(말풍선·전표·키캡)·나무(칩·이름표) 바탕은 크기마다 코드로 굽는다: CC0 사진 결(Resources/dotui_*_grain) ±grainAmp,
///     고르지 않은 1px 테 + 1px 그림자, 전표는 접힌 모서리 + 테이프 한 조각. 글씨는 짙은 남보라(순검정 X).
///   · 방향 표시: 화면 위 나침반 바늘(Compass) 또는 차 앞 길 위 화살표(Road), 19px(360 줄의 5.3%).
/// 그리는 것:
///   주행 — 방향 표시 + 화물 위험 "!"(위험할 때만) + Tab 누르는 동안 전표.
///   걷기 — 가운데 조준점 2×2 + [E] 칩을 그 물건 곁에(OnFootDirector.PromptAnchor).
///   대화 — 말하는 사람 머리 위 말풍선(이름표 + 한 글자씩). 시선 이동은 OnFootDirector 가 한다.
/// 기존 HUD(속도·기어·화물 막대·목적지 등)는 이 컴포넌트가 켜져 있는 동안만 끈다(SetActive false, 지우지 않음). 이 컴포넌트를 끄면 되돌린다.
/// 글꼴: Galmuri11/9(OFL, Assets/UI/Fonts/Galmuri). 아트 스프라이트 슬롯은 아래 *Sprite 필드(비면 코드 임시 모양).
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

    [Header("2판 — 세상과 같은 화면 처리(D-241)")]
    [Tooltip("PitCrew/UI/DotPalette. 비면 Shader.Find.")]
    [SerializeField] Shader uiShader;
    public enum DirMode { Compass, Road }
    [Tooltip("Compass = 화면 위 가운데 나침반 바늘 · Road = 차 앞 길 위에 뜬 화살표")]
    public DirMode dirMode = DirMode.Compass;
    [Tooltip("방향 표시 크기(RT 픽셀). 360 줄의 4~6% = 15~22.")]
    [SerializeField] int dirPx = 19;
    [Tooltip("종이 결 세기(±). D-197 사진 결 8~12%.")]
    [Range(0f, 0.2f)] public float grainAmp = 0.10f;
    [Tooltip("나무 결 세기(±).")]
    [Range(0f, 0.3f)] public float woodAmp = 0.14f;
    [Tooltip("Bayer 흔들기 폭(감마 공간). 클수록 단 사이 디더가 넓다.")]
    [Range(0f, 0.15f)] public float ditherSpread = 0.05f;
    [Tooltip("SunsetLook 해 빛 색을 팔레트에 곱하는 세기(0 = 안 물듦, 1 = 해 빛 그대로).")]
    [Range(0f, 1f)] public float sunTint = 0.35f;
    [Tooltip("Road 모드: 화살표를 차 앞 몇 m 길 위에.")]
    [SerializeField] float roadAhead = 9f;

    [Header("아트 슬롯(비면 코드 임시 모양)")]
    public Sprite boxSprite, tabSprite, keySprite, tailSprite, warnSprite;

    // ── 2판 팔레트(sRGB). 셰이더가 이 10색에서 가장 가까운 색으로 다시 찍는다 — 순서 = _Pal 순서 ──
    static readonly Color[] Base =
    {
        new Color32(0x2A, 0x23, 0x40, 0xFF),   // 0 Ink 짙은 남보라(글씨·테)
        new Color32(0x4B, 0x3D, 0x58, 0xFF),   // 1 InkSoft 그림자·접힌 선
        new Color32(0x62, 0x45, 0x31, 0xFF),   // 2 WoodDark 나무 결 어두운 단
        new Color32(0x7C, 0x5B, 0x3F, 0xFF),   // 3 Wood 칩·이름표 판
        new Color32(0xCB, 0xB6, 0x95, 0xFF),   // 4 PaperShade 종이 결 어두운 단·접힌 뒷면
        new Color32(0xE6, 0xD9, 0xBC, 0xFF),   // 5 Paper
        new Color32(0xF4, 0xEC, 0xD9, 0xFF),   // 6 PaperLight 종이 결 밝은 단·나무 위 글씨
        new Color32(0xD0, 0x48, 0x2E, 0xFF),   // 7 Accent 신호 주홍("!"·바늘 끝)
        new Color32(0xEB, 0xC2, 0x4A, 0xFF),   // 8 Lemon 길 위 화살표
        new Color32(0xC9, 0xB0, 0x6A, 0xFF),   // 9 Tape 종이 테이프
    };
    static Color Ink => Base[0]; static Color InkSoft => Base[1]; static Color WoodDark => Base[2]; static Color Wood => Base[3];
    static Color PaperShade => Base[4]; static Color Paper => Base[5]; static Color PaperLight => Base[6];
    static Color Accent => Base[7]; static Color Lemon => Base[8]; static Color Tape => Base[9];

    Camera cam;
    Canvas canvas;
    RectTransform root;
    Image dot, arrowImg, warnBox, chipBox, chipKey, bubble, tail, nameTab, slip;
    Image warnSkin, chipSkin, keySkin, bubbleSkin, nameSkin, slipSkin;
    Text chipLabel, chipKeyText, bubbleText, nameText, warnText, slipTitle, slipBody, hintText;
    Texture2D arrowTex;
    int arrowStep = int.MinValue; DirMode arrowMode;
    readonly List<GameObject> hidden = new List<GameObject>();
    OnFootDirector.Mode lastState;
    float cartAt = -99f;
    string bubbleLine;
    TextGenerator gen;
    Material mat;
    readonly Vector4[] pal = new Vector4[10];
    SunsetLook sunset; float sunsetLookAt = -99f;
    byte[] paperGrain, woodGrain;
    readonly Dictionary<string, Sprite> skinCache = new Dictionary<string, Sprite>();
    const int M = 3;   // 바탕 스프라이트 둘레 여백(그림자·테이프가 상자 밖으로 나오는 칸)

    /// <summary>QA·봇이 읽는 것 — 지금 보이는 요소 이름들.</summary>
    public string Visible { get; private set; } = "";
    /// <summary>QA: 지금 UI 에 곱하는 해 빛(시간대 물듦).</summary>
    public Color TintNow { get; private set; } = Color.white;

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

    void OnDestroy()
    {
        Font.textureRebuilt -= OnFontRebuilt;
        if (canvas != null) Destroy(canvas.gameObject);
        if (arrowTex != null) Destroy(arrowTex);
        if (mat != null) Destroy(mat);
        foreach (var s in skinCache.Values) if (s != null) { Destroy(s.texture); Destroy(s); }
    }

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
        // Screen Space - Camera 는 화면(1920×1080) 단위로 배치·글자 래스터를 해서 RT 에 1/3 로 눌려 흐려졌다(1판 보고서 §3).
        var go = new GameObject("DotScreenUI_Canvas", typeof(Canvas));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        canvas.scaleFactor = 1f;
        canvas.referencePixelsPerUnit = 100f;
        canvas.sortingOrder = 32000;
        canvas.vertexColorAlwaysGammaSpace = false;   // 정점 색은 선형으로 — 셰이더가 팔레트(선형)와 같은 공간에서 비교
        root = (RectTransform)go.transform;
        root.SetParent(cam.transform, false);
        root.pivot = new Vector2(0.5f, 0.5f);
        gen = new TextGenerator();
        foreach (var f in new[] { font, smallFont })            // 글자 텍스처를 Nearest 로(반 픽셀 어긋나도 번지지 않게)
            if (f != null && f.material != null && f.material.mainTexture != null) f.material.mainTexture.filterMode = FilterMode.Point;
        Font.textureRebuilt += OnFontRebuilt;

        var sh = uiShader != null ? uiShader : Shader.Find("PitCrew/UI/DotPalette");
        if (sh != null) mat = new Material(sh) { name = "DotUI_Palette (runtime)", hideFlags = HideFlags.DontSave };
        else Debug.LogWarning("[DotScreenUI] PitCrew/UI/DotPalette 셰이더 없음 — 1판처럼 기본 UI 셰이더로 그린다");
        paperGrain = LoadGrain("dotui_paper_grain64", 64 * 64);
        woodGrain = LoadGrain("dotui_wood_grain64x32", 64 * 32);

        var tl = tailSprite != null ? tailSprite : MakeTail(Paper, Ink, InkSoft);

        dot = Img("Dot", MakeSolid(PaperLight), Color.white, 2, 2);
        int S = dirPx + 4;   // 테 1 + 그림자 1 + 여유
        arrowTex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Dir" };
        arrowImg = Img("Dir", Sprite.Create(arrowTex, new Rect(0, 0, S, S), Vector2.zero, 100f), Color.white, S, S);

        warnBox = Host("Warn", 13, 17); warnSkin = Skin(warnBox, warnSprite);
        warnText = Txt("Text", warnBox.transform, font, fontSize, PaperLight, TextAnchor.MiddleCenter); warnText.text = "!";
        Fill(warnText.rectTransform, 0, 1, 0, 0);

        chipBox = Host("Chip", 40, 15); chipSkin = Skin(chipBox, tabSprite);
        chipKey = Host("Key", 11, 11, chipBox.transform); keySkin = Skin(chipKey, keySprite);
        chipKey.rectTransform.anchoredPosition = new Vector2(2, 2);
        chipKeyText = Txt("Text", chipKey.transform, smallFont, smallSize, Ink, TextAnchor.MiddleCenter); chipKeyText.text = "E";
        Fill(chipKeyText.rectTransform, 1, 0, 0, 0);
        chipLabel = Txt("Label", chipBox.transform, font, fontSize, PaperLight, TextAnchor.MiddleLeft);
        chipLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

        bubble = Host("Bubble", 60, 20); bubbleSkin = Skin(bubble, boxSprite);
        tail = Img("Tail", tl, Color.white, 7, 5);   // 말풍선 뒤에 만들어 위에 그린다(맨 윗줄이 상자 밑 테를 덮어 입이 열림)
        bubbleText = Txt("Text", bubble.transform, font, fontSize, Ink, TextAnchor.UpperLeft);
        bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap; bubbleText.supportRichText = true; bubbleText.lineSpacing = 1f;
        nameTab = Host("Name", 30, 13); nameSkin = Skin(nameTab, tabSprite);
        nameText = Txt("Text", nameTab.transform, smallFont, smallSize, PaperLight, TextAnchor.MiddleCenter);
        Fill(nameText.rectTransform, 0, 0, 0, 1);

        slip = Host("Slip", 150, 36); slipSkin = Skin(slip, boxSprite);
        slipTitle = Txt("Title", slip.transform, smallFont, smallSize, InkSoft, TextAnchor.UpperLeft); slipTitle.text = "전표";
        slipTitle.rectTransform.anchoredPosition = new Vector2(6, 22); slipTitle.rectTransform.sizeDelta = new Vector2(100, 11);
        slipBody = Txt("Body", slip.transform, font, fontSize, Ink, TextAnchor.UpperLeft);
        slipBody.rectTransform.anchoredPosition = new Vector2(6, 6); slipBody.rectTransform.sizeDelta = new Vector2(400, 14);
        slipBody.horizontalOverflow = HorizontalWrapMode.Overflow;
        hintText = Txt("Hint", root, smallFont, smallSize, PaperLight, TextAnchor.LowerLeft); hintText.text = "Tab 전표";
        hintText.rectTransform.sizeDelta = new Vector2(80, 11);
        var hs = hintText.gameObject.AddComponent<Shadow>(); hs.effectColor = Ink; hs.effectDistance = new Vector2(1, -1);   // 세상 위 글씨 = 1px 그림자로 읽히게

        HideAll();
        Debug.Log($"[DotScreenUI] 캔버스 {W}×{H} (RT={(cam.targetTexture != null ? cam.targetTexture.width + "×" + cam.targetTexture.height : "없음")}) font={(font != null ? font.name : "없음")} shader={(mat != null)} grain={(paperGrain != null)}/{(woodGrain != null)}");
    }

    static byte[] LoadGrain(string name, int len)
    {
        var ta = Resources.Load<TextAsset>(name);
        if (ta == null || ta.bytes.Length < len) { Debug.LogWarning($"[DotScreenUI] 결 {name} 없음 — 민 면으로"); return null; }
        return ta.bytes;
    }

    Image Img(string name, Sprite s, Color c, int w, int h, bool sliced = false, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent != null ? parent : root, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = Vector2.zero; r.sizeDelta = new Vector2(w, h);
        var im = go.GetComponent<Image>(); im.sprite = s; im.color = c; im.raycastTarget = false;
        if (mat != null) im.material = mat;
        if (sliced && s != null) { im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 1f; }
        return im;
    }

    /// <summary>상자 자리(크기·위치·자식 기준)만 잡는 빈 이미지. 그림은 첫 자식 Skin 이 그린다(그림자·테이프가 상자 밖 M 칸까지).</summary>
    Image Host(string name, int w, int h, Transform parent = null)
    {
        var im = Img(name, null, Color.white, w, h, false, parent);
        im.enabled = false;
        return im;
    }

    /// <summary>host 뒤에 깔리는 바탕. 아트 슬롯 스프라이트가 있으면 그것을 9분할로(여백 없이), 없으면 SetSkin 이 크기마다 굽는다.</summary>
    Image Skin(Image host, Sprite art)
    {
        var s = Img("Skin", art, Color.white, 1, 1, art != null, host.transform);
        s.transform.SetAsFirstSibling();
        s.rectTransform.anchoredPosition = art != null ? Vector2.zero : new Vector2(-M, -M);
        return s;
    }

    Text Txt(string name, Transform parent, Font f, int size, Color c, TextAnchor a)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
        var t = go.AddComponent<Text>();
        t.font = f != null ? f : font; t.fontSize = size; t.color = c; t.alignment = a; t.raycastTarget = false;
        t.verticalOverflow = VerticalWrapMode.Overflow; t.supportRichText = false;
        if (mat != null) t.material = mat;
        go.AddComponent<PixelSnapText>().root = root;
        return t;
    }

    static void Fill(RectTransform r, int l, int b, int rr, int tt)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -tt);
    }

    static Sprite MakeSolid(Color c)
    {
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "DotUI_Solid" };
        t.SetPixel(0, 0, c); t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, 1, 1), Vector2.zero, 100f);
    }

    // ── 2판 바탕 굽기 ──
    enum Kind { Paper, Slip, Wood, Key, Warn }

    static float Hash(int a, int b, int c)
    {
        uint h = (uint)(a * 374761393 + b * 668265263 + c * 2147483647);
        h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
        return (h & 0xFFFF) / 65535f;
    }

    /// <summary>손으로 그은 테: 변을 4~7칸 마디로 나눠 마디마다 30% 확률로 1칸 안쪽으로. 모서리 2칸은 그대로(각이 무너지지 않게).</summary>
    static int Wob(int seed, int edge, int i, int len)
    {
        if (i < 2 || i > len - 3 || len < 8) return 0;
        int seg = (i + (int)(Hash(seed, edge, 99) * 5)) / 5;
        return Hash(seed, edge, seg) < 0.3f ? 1 : 0;
    }

    /// <summary>
    /// w×h 상자(+둘레 M 칸)를 픽셀로 굽는다. 칸 색은 팔레트 기준색(sRGB) ± 사진 결 — 시간대 물듦·재양자화는 셰이더가 한다.
    /// 테 = 상자 안쪽 칸 중 바깥과 맞닿은 칸(Ink), 그림자 = 오른쪽 아래로 1칸(InkSoft).
    /// </summary>
    Sprite BakeSkin(int w, int h, Kind kind, int seed)
    {
        int TW = w + 2 * M, TH = h + 2 * M;
        var px = new Color[TW * TH];
        bool fold = kind == Kind.Slip;
        const int F = 5;   // 접힌 모서리 크기
        bool In(int bx, int by)
        {
            if (bx < 0 || by < 0 || bx >= w || by >= h) return false;
            if ((bx == 0 || bx == w - 1) && (by == 0 || by == h - 1)) return false;   // 모서리 1칸 깎기
            if (bx < Wob(seed, 0, by, h) || bx >= w - Wob(seed, 1, by, h)) return false;
            if ((kind != Kind.Paper && by < Wob(seed, 2, bx, w)) || by >= h - Wob(seed, 3, bx, w)) return false;   // 말풍선 밑변은 곧게(꼬리가 붙는 자리)
            if (fold && (w - 1 - bx) + (h - 1 - by) < F) return false;               // 오른쪽 위 접힘: 잘린 삼각형
            return true;
        }
        Color baseC = kind == Kind.Wood ? Wood : kind == Kind.Key ? PaperLight : kind == Kind.Warn ? Accent : Paper;
        for (int y = 0; y < TH; y++)
            for (int x = 0; x < TW; x++)
            {
                int bx = x - M, by = y - M;
                Color c = Color.clear;
                if (In(bx, by))
                {
                    bool edge = !In(bx - 1, by) || !In(bx + 1, by) || !In(bx, by - 1) || !In(bx, by + 1);
                    if (edge) c = Ink;
                    else
                    {
                        c = baseC;
                        float g = 0f;
                        if (kind == Kind.Wood && woodGrain != null) g = (woodGrain[((by + seed) & 31) * 64 + ((bx + seed * 7) & 63)] - 128) / 127f * woodAmp;
                        else if (kind != Kind.Warn && paperGrain != null) g = (paperGrain[((by + seed * 3) & 63) * 64 + ((bx + seed * 5) & 63)] - 128) / 127f * grainAmp;
                        if (kind == Kind.Wood && g < -woodAmp * 0.35f) c = WoodDark;   // 나무 결 어두운 줄(사진 결을 2단으로)
                        else c = new Color(c.r * (1f + g), c.g * (1f + g), c.b * (1f + g), 1f);
                        if (fold && (w - 1 - bx) < F && (h - 1 - by) < F) c = (w - 1 - bx) + (h - 1 - by) == F ? Ink : PaperShade;   // 접혀 넘어온 뒷면 + 접힌 선
                    }
                }
                else if (In(bx - 1, by + 1)) c = InkSoft;   // 1px 그림자(오른쪽 아래)
                px[y * TW + x] = c;
            }
        if (kind == Kind.Slip)   // 테이프 한 조각: 왼쪽 위, 위로 2칸 삐져나오고 가운데서 1칸 내려앉음(손으로 붙인 기울기)
        {
            int tx0 = 7, tw = 13;
            for (int i = 0; i < tw; i++)
            {
                int dy = i >= tw / 2 ? -1 : 0;
                for (int j = -3; j <= 1; j++)
                {
                    bool ragged = (i == 0 || i == tw - 1) && ((j + i) & 1) == 0;   // 찢은 끝
                    if (ragged) continue;
                    int x = M + tx0 + i, y = M + h + j + dy;
                    if (x < 0 || x >= TW || y < 0 || y >= TH) continue;
                    float g = paperGrain != null ? (paperGrain[((y * 3) & 63) * 64 + ((x * 2) & 63)] - 128) / 127f * 0.06f : 0f;
                    px[y * TW + x] = new Color(Tape.r * (1f + g), Tape.g * (1f + g), Tape.b * (1f + g), 1f);
                }
            }
        }
        var t = new Texture2D(TW, TH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Skin_" + kind };
        t.SetPixels(px); t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, TW, TH), Vector2.zero, 100f);
    }

    /// <summary>host 크기를 w×h 로, 바탕을 그 크기에 맞게(같은 크기·종류는 캐시).</summary>
    void SetBox(Image host, Image skin, int w, int h, Kind kind)
    {
        host.rectTransform.sizeDelta = new Vector2(w, h);
        if (skin.type == Image.Type.Sliced) { skin.rectTransform.sizeDelta = new Vector2(w, h); return; }   // 아트 슬롯
        int seed = (int)kind * 131 + w * 7 + h * 3;
        string key = kind + ":" + w + "x" + h;
        if (!skinCache.TryGetValue(key, out var sp)) { sp = BakeSkin(w, h, kind, seed); skinCache[key] = sp; }
        if (skin.sprite != sp) skin.sprite = sp;
        skin.rectTransform.sizeDelta = new Vector2(w + 2 * M, h + 2 * M);
    }

    /// <summary>말풍선 꼬리 7×5 — 맨 윗줄은 상자 밑 테를 덮는다(속은 종이색), 오른쪽 1칸 그림자.</summary>
    static Sprite MakeTail(Color fill, Color edge, Color shadow)
    {
        var t = new Texture2D(7, 5, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Tail" };
        string[] rows = { "...os..", "..ofos.", ".offfos", "offfffo", "offfffo" };   // 아래(y=0) → 위(y=4)
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 7; x++)
            {
                char ch = rows[y][x];
                t.SetPixel(x, y, ch == 'o' ? edge : ch == 'f' ? fill : ch == 's' ? shadow : Color.clear);
            }
        t.SetPixel(0, 4, edge); t.SetPixel(6, 4, edge); t.SetPixel(1, 4, fill); t.SetPixel(5, 4, fill);
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, 7, 5), Vector2.zero, 100f);
    }

    /// <summary>방향 표시를 픽셀 단위로 직접 찍는다(16방향). 회전한 그림을 늘리지 않으니 흐린 픽셀이 없다.
    /// Compass = 종이 원판 + 주홍 바늘(끝이 목적지) · Road = 레몬색 화살표. 둘 다 Ink 테 + 오른쪽 아래 1px 그림자.</summary>
    void DrawDir(int step, DirMode mode)
    {
        int S = arrowTex.width; float c0 = (S - 2) * 0.5f - 0.5f;   // 그림자 1칸 자리를 오른쪽 아래에 남기고 가운데
        float R = dirPx * 0.5f;
        float a = step * (Mathf.PI * 2f / 16f);
        float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        var kind = new byte[S, S];   // 0 빈 · 1 바탕(원판/화살표 몸) · 2 바늘 끝 · 3 바늘 꼬리
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float px = x - c0, py = y - c0 - 1f;
                float ux = px * ca - py * sa, uy = px * sa + py * ca;   // 화살표 공간(+y 가 앞). step 양수 = 오른쪽.
                if (mode == DirMode.Compass)
                {
                    float r = Mathf.Sqrt(px * px + py * py);
                    if (r > R - 0.3f) continue;
                    kind[x, y] = 1;
                    float L = R - 2.2f;
                    if (Mathf.Abs(ux) <= (L - Mathf.Abs(uy)) * 0.32f + 0.35f) kind[x, y] = (byte)(uy >= 0f ? 2 : 3);
                }
                else
                {
                    float L = R - 1.2f;
                    bool head = uy >= -0.5f && uy <= L && Mathf.Abs(ux) <= (L - uy) * 0.95f;
                    bool stem = uy >= -L && uy < -0.5f && Mathf.Abs(ux) <= R * 0.24f;
                    if (head || stem) kind[x, y] = 1;
                }
            }
        bool Filled(int x, int y) => x >= 0 && y >= 0 && x < S && y < S && kind[x, y] != 0;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Color c = Color.clear;
                if (Filled(x, y))
                {
                    bool edge = !Filled(x - 1, y) || !Filled(x + 1, y) || !Filled(x, y - 1) || !Filled(x, y + 1);
                    int k = kind[x, y];
                    c = edge ? Ink : k == 2 ? Accent : k == 3 ? Ink : mode == DirMode.Compass ? Paper : Lemon;
                    if (mode == DirMode.Compass && Mathf.Abs(x - c0) < 0.6f && Mathf.Abs(y - c0 - 1f) < 0.6f) c = PaperLight;   // 가운데 핀
                }
                else if (Filled(x - 1, y + 1)) c = InkSoft;
                arrowTex.SetPixel(x, y, c);
            }
        arrowTex.Apply(false);
    }

    // ── 매 프레임 ──
    void LateUpdate()
    {
        if (canvas == null) return;
        Fit();   // 걷기(near 0.05)·주행 카메라 near·FOV 가 다르다 — 항상 바로 앞, 화면 꽉
        UpdatePalette();
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

    static readonly int PalId = Shader.PropertyToID("_Pal"), PalCountId = Shader.PropertyToID("_PalCount"), SpreadId = Shader.PropertyToID("_Spread");

    /// <summary>시간대 팔레트: 기준 10색 × 해 빛(SunsetLook 지금 프리셋의 sunLight, 가장 밝은 채널 = 1 로 맞춤)을 sunTint 만큼.
    /// Look 이 꺼져 있으면(낮 원본) 물들지 않는다. 오후 프리셋 해 빛 (1,.92,.78) → 살짝 따뜻, 노을 (1,.62,.38) → 주황.</summary>
    void UpdatePalette()
    {
        if (mat == null) return;
        if (sunset == null && Time.unscaledTime - sunsetLookAt > 2f) { sunsetLookAt = Time.unscaledTime; sunset = SunsetLook.Find(); }
        Color tint = Color.white;
        if (sunset != null && sunset.IsOn)
        {
            var sl = sunset.Now().sunLight;
            float mx = Mathf.Max(0.01f, Mathf.Max(sl.r, Mathf.Max(sl.g, sl.b)));
            tint = Color.Lerp(Color.white, new Color(sl.r / mx, sl.g / mx, sl.b / mx), sunTint * Mathf.Clamp01(sunset.amount));
        }
        TintNow = tint;
        for (int i = 0; i < Base.Length; i++)
        {
            var c = new Color(Base[i].r * tint.r, Base[i].g * tint.g, Base[i].b * tint.b, 1f).linear;
            pal[i] = new Vector4(c.r, c.g, c.b, 1f);
        }
        mat.SetVectorArray(PalId, pal);
        mat.SetFloat(PalCountId, Base.Length);
        mat.SetFloat(SpreadId, ditherSpread);
    }

    // 실제 도트 격자 = LowResRenderTest.RenderSize(360 줄). 카메라 RT 는 URP renderScale 0.5 를 감안해 그 2배(1280×720)로
    // 잡혀 있어서 cam.pixelWidth 로 맞추면 UI 가 절반 크기로 그려져 글자 줄이 빠졌다(1판 보고서 §3).
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
        // 목적지 방향: 차 카메라 앞(수평) 기준으로 목적지가 몇 도 옆인지 → 16방향.
        var mgr = Services.Delivery as DeliveryManager;
        var tgt = mgr != null ? mgr.CurrentTarget : null;
        int S = arrowTex.width;
        int warnX = W / 2 + S / 2 + 3, warnY = H - 6 - 17;
        if (tgt != null)
        {
            Vector3 f = cam.transform.forward; f.y = 0f;
            Vector3 d = tgt.transform.position - cam.transform.position; d.y = 0f;
            if (f.sqrMagnitude > 1e-4f && d.sqrMagnitude > 1e-4f)
            {
                float ang = Vector3.SignedAngle(f, d, Vector3.up);
                int step = Mathf.RoundToInt(ang / 22.5f);
                if (step != arrowStep || dirMode != arrowMode) { arrowStep = step; arrowMode = dirMode; DrawDir(step, dirMode); }
                if (dirMode == DirMode.Compass) { Show(arrowImg, W / 2 - S / 2, H - 5 - S); vis.Append("compass "); }
                else
                {
                    // 차 앞 roadAhead m 길 위(아래로 쏴서 땅) + 0.6m. 화면 아래 절반 안으로.
                    Vector3 p = cam.transform.position + f.normalized * roadAhead;
                    if (Physics.Raycast(p + Vector3.up * 6f, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) p = hit.point;
                    else p.y -= 2f;
                    p += Vector3.up * 0.6f;
                    int x = W / 2 - S / 2, y = H / 3;
                    if (ToScreen(p, out var sp)) { x = Mathf.RoundToInt(sp.x) - S / 2; y = Mathf.RoundToInt(sp.y) - S / 2; }
                    x = Mathf.Clamp(x, 2, W - S - 2); y = Mathf.Clamp(y, 40, H / 2 - S);
                    Show(arrowImg, x, y); vis.Append("road ");
                    warnX = W / 2 - 13 / 2;
                }
            }
        }
        // 화물 위험 "!": CargoWarningHUD 와 같은 4 소스(급제동·코너·스핀·경사) 중 큰 것.
        var cargo = Services.Cargo as CargoBox;
        if (cargo != null && tgt != null)
        {
            float r = Mathf.Max(Mathf.Max(cargo.CurrentLongG / 0.9f, cargo.CurrentLatG / 1.0f), Mathf.Max(cargo.CurrentSpinDegPerSec / 100f, cargo.CurrentTiltDeg / 10f));
            bool blinkOff = r >= warnCrit && Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f;
            if ((r >= warnShow || SimWarn) && !blinkOff) { SetBox(warnBox, warnSkin, 13, 17, Kind.Warn); Show(warnBox, warnX, warnY); vis.Append("warn "); }
        }
        // 전표: Tab 누르는 동안. 탑승 직후 잠깐 안내.
        bool tab = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        var rc = Services.Run as RunController;
        var job = rc != null ? rc.CurrentJob : null;
        if (job != null && (tab || SimSlip))
        {
            slipBody.text = job.displayName;
            int bw = Mathf.Clamp(Mathf.CeilToInt(Pref(slipBody, job.displayName, 400)) + 14, 64, 300);
            SetBox(slip, slipSkin, bw, 36, Kind.Slip);
            Show(slip, 8, 8); vis.Append("slip ");
        }
        else if (job != null && Time.unscaledTime - cartAt < slipHintSec) { Show(hintText, 8, 8); vis.Append("hint "); }
    }

    /// <summary>봇·캡처용: Tab 을 누른 것과 같다.</summary>
    public bool SimSlip { get; set; }
    /// <summary>캡처용: 화물 위험 "!" 를 강제로 켠다.</summary>
    public bool SimWarn { get; set; }

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
        SetBox(chipBox, chipSkin, w, h, Kind.Wood);
        if (hasKey) SetBox(chipKey, keySkin, 11, 11, Kind.Key);
        var lr = chipLabel.rectTransform; lr.anchoredPosition = new Vector2(3 + keyW, 1); lr.sizeDelta = new Vector2(lw + 2, 13);
        // 그 물건 곁(오른쪽 위)에. 화면 밖·뒤면 조준점 바로 아래.
        int x, y;
        if (director.PromptAnchor is Vector3 a && ToScreen(a, out var sp) && sp.x > 0 && sp.x < W && sp.y > 0 && sp.y < H)
        { x = Mathf.RoundToInt(sp.x) + 6; y = Mathf.RoundToInt(sp.y) + 2; }
        else { x = W / 2 - w / 2; y = H / 2 - 10 - h; }
        x = Mathf.Clamp(x, 2, W - w - 2 - M); y = Mathf.Clamp(y, 2 + M, H - h - 2);
        Show(chipBox, x, y); vis.Append("chip ");
    }

    void TalkUI(System.Text.StringBuilder vis)
    {
        string full = director.TalkText;
        if (full != bubbleLine)
        {
            bubbleLine = full;
            int oneLine = Mathf.CeilToInt(Pref(bubbleText, full, 2000));
            int tw = Mathf.Min(oneLine, bubbleMaxW - 12);
            bubbleText.rectTransform.sizeDelta = new Vector2(tw + 1, 13);
            bubbleText.text = full;
            int th = Mathf.CeilToInt(bubbleText.preferredHeight);
            SetBox(bubble, bubbleSkin, tw + 12, th + 10, Kind.Paper);
            bubbleText.rectTransform.sizeDelta = new Vector2(tw + 1, th);
            bubbleText.rectTransform.anchoredPosition = new Vector2(6, 5);
            nameText.text = director.TalkWho;
            SetBox(nameTab, nameSkin, Mathf.CeilToInt(Pref(nameText, director.TalkWho, 200)) + 10, 13, Kind.Wood);
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
        int bx = Mathf.Clamp(ax - bw / 2, 2, W - bw - 2 - M);
        int by = Mathf.Clamp(ay + 4, 2 + M, H - bh - 13 - 2);
        int tx = Mathf.Clamp(ax - 3, bx + 3, bx + bw - 10);
        Show(bubble, bx, by);
        Show(tail, tx, by - 4);
        Show(nameTab, bx + 5, by + bh - 2);
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
