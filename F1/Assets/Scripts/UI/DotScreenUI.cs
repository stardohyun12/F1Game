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

    public enum DestStyle { Dots, Beam, Both }
    [Header("목적지 표식(D-252)")]
    [Tooltip("깃발 아래 땅까지: Dots = 도트 점선(UI), Beam = 세상 속 은은한 빛기둥(건물에 가려짐), Both = 둘 다.")]
    [SerializeField] DestStyle destStyle = DestStyle.Dots;
    [Tooltip("이 거리(m, 수평) 안이면 깃발 대신 '여기' 칩.")]
    [SerializeField] float destNearM = 15f;
    /// <summary>깃발 크기 2단(4cccf317 후속): 이 거리(m, 수평)보다 멀면 큰 깃발(17×24), 가까우면 원래 13×18(15 m 안은 '여기' 칩). 되돌아올 땐 FlagHysteresisM 만큼 더 가까워야 한다. 상수라 씬 직렬화에 안 남는다.</summary>
    public const float FlagFarM = 25f;
    [Tooltip("화살표 왼쪽에 목적지까지 거리(m).")]
    [SerializeField] bool showDistance = true;
    [Tooltip("빛기둥 높이(m)·너비(m)·불투명도.")]
    [SerializeField] float beamHeight = 40f, beamWidth = 1.4f, beamAlpha = 0.45f;

    [Header("아트 슬롯(비면 코드 임시 모양)")]
    public Sprite boxSprite, tabSprite, keySprite, tailSprite, warnSprite, flagSprite;

    // 9판 UI 토큰(UIPalette) — 종이 크림·잉크.
    static Color Paper => UIPalette.Paper;
    static Color Ink => UIPalette.Ink;
    static Color Signal => UIPalette.Signal;

    Camera cam;
    Canvas canvas;
    RectTransform root;
    Image dot, arrowImg, warnBox, chipBox, chipKey, bubble, tail, nameTab, slip;
    Text chipLabel, chipKeyText, bubbleText, nameText, warnText, slipTitle, slipBody, hintText;
    // D-252 목적지: 깃발(문 위) · 아래로 점선 · 화면 밖이면 가장자리 화살 · 15 m 안 '여기' 칩 · 거리 숫자 · 전표 받는 곳/주소/대역 약도.
    Image destFlag, destDots, destEdge, destEdgeBox, destChip, distBox, slipMap, moneyBox;
    Text moneyText;
    /// <summary>QA: 돈 글자가 지금 그려진 RT 픽셀(왼쪽 아래)과 글.</summary>
    public Vector2 MoneyPx { get; private set; }
    public string MoneyShown { get; private set; } = "";
    Text destChipText, distText, slipTo, slipAddr;
    Texture2D edgeTex, mapTex;
    int edgeStep = int.MinValue;
    const int EdgeN = 11, MapN = 52;
    /// <summary>깃발 2단 상수(RT 픽셀): 가까운 쪽 13×18(1판 그대로), 먼 쪽 17×24 — 높이 최소 20 이상.</summary>
    public const int FlagNearW = 13, FlagNearH = 18, FlagFarW = 17, FlagFarH = 24;
    // 3 m: 차가 서서 흔들리는 거리 잡음(<0.5 m)보다 넉넉하고, 다가갈 때 22~15 m 구간에서 원래 깃발이 실제로 보인다(8 m 면 17~15 m 뿐이라 2단이 사실상 1단).
    public const float FlagHysteresisM = 3f;
    Sprite flagNearSp, flagFarSp;
    bool flagFar;
    /// <summary>QA: true 면 거리와 상관없이 원래(가까운) 깃발 — 같은 카메라 전/후 비교용.</summary>
    public bool SimFlagNear;
    /// <summary>QA: 지금 그려진 깃발 단계("near"/"far")와 크기.</summary>
    public string FlagTier => flagFar && !SimFlagNear ? "far" : "near";
    public Vector2 FlagSize => destFlag != null ? destFlag.rectTransform.sizeDelta : Vector2.zero;
    GameObject beam;
    Material beamMat;
    DeliveryPoint groundFor; Vector3 groundAt;
    /// <summary>표식 깃발 아래 끝(문 위) = 배달점 + 이만큼 위(m). 배달점은 길 위 트리거(땅 + 0.6 m) — 문 꼭대기쯤.</summary>
    public const float DestAnchorUp = 3f;
    public static Vector3 DestAnchorOf(DeliveryPoint dp) => dp.transform.position + Vector3.up * DestAnchorUp;
    Texture2D arrowTex;
    int arrowStep = int.MinValue;
    // 방향 표시 크기: 1판은 15칸 격자·모양 1배. 화면 높이(360줄)의 약 5% = 화살표 전체 길이 ~19px 가 되게 모양째 1.5배, 격자 23칸(D-243).
    const int ArrowN = 23;
    const float ArrowScale = 1.5f;
    bool arrowOld;
    readonly List<GameObject> hidden = new List<GameObject>();
    OnFootDirector.Mode lastState;
    float cartAt = -99f;
    string bubbleLine;
    TextGenerator gen;

    /// <summary>본문 글꼴(Galmuri11) — 개발 디버그 창(DebugWindow)이 같은 글꼴을 쓴다.</summary>
    public Font BodyFont => font;

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
        if (beam != null) beam.SetActive(false);
        RestoreLegacy();
    }

    void OnEnable() { if (canvas != null) canvas.gameObject.SetActive(true); }

    void OnDestroy()
    {
        Font.textureRebuilt -= OnFontRebuilt;
        if (canvas != null) Destroy(canvas.gameObject);
        foreach (var t in new Object[] { arrowTex, edgeTex, mapTex, beam, beamMat }) if (t != null) Destroy(t);
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
        arrowTex = new Texture2D(ArrowN, ArrowN, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Arrow" };
        arrowImg = Img("Arrow", Sprite.Create(arrowTex, new Rect(0, 0, ArrowN, ArrowN), Vector2.zero, 100f), Color.white, ArrowN, ArrowN);
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

        // D-252 목적지 표식 — 그리는 순서상 화살표·칩보다 먼저(뒤에) 만들면 겹칠 때 안내가 위로 온다 → 맨 앞으로 옮긴다.
        destDots = Img("DestDots", MakeDots(), Color.white, 3, 4);
        destDots.type = Image.Type.Tiled; destDots.pixelsPerUnitMultiplier = 1f;
        flagNearSp = flagSprite != null ? flagSprite : MakeFlag(FlagNearW, FlagNearH, 9, 15, 11, 10, 12);
        flagFarSp = flagSprite != null ? flagSprite : MakeFlag(FlagFarW, FlagFarH, 12, 20, 15, 13, 16);
        destFlag = Img("DestFlag", flagNearSp, Color.white, (int)flagNearSp.rect.width, (int)flagNearSp.rect.height);
        edgeTex = new Texture2D(EdgeN, EdgeN, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Edge" };
        destEdge = Img("DestEdge", Sprite.Create(edgeTex, new Rect(0, 0, EdgeN, EdgeN), Vector2.zero, 100f), Color.white, EdgeN, EdgeN);
        destChip = Img("DestChip", dark, Color.white, 40, 15, true);
        destChipText = Txt("Label", destChip.transform, font, fontSize, Paper, TextAnchor.MiddleLeft);
        destChipText.horizontalOverflow = HorizontalWrapMode.Overflow;
        distBox = Img("Dist", dark, Color.white, 30, 13, true);
        distText = Txt("Text", distBox.transform, smallFont, smallSize, Paper, TextAnchor.MiddleCenter);
        Fill(distText.rectTransform, 0, 0, 0, 1);
        // 화면 밖 표식 받침(1409 캡처: 가장자리 깃발 13×18 이 가로등·잔디에 묻혀 안 읽힘) — 잉크 상자 위에 깃발+화살.
        // 받침은 잉크 상자 + 종이색 1px 테 — 어두운 땅·노을 그늘에서 잉크 상자가 배경과 섞여(대비 1.3) 테로 가장자리를 세운다.
        destEdgeBox = Img("DestEdgeBox", tabSprite != null ? tabSprite : MakeBox(Ink, Paper), Color.white, 10, 10, true);
        foreach (var g in new Graphic[] { destDots, destFlag, destEdge, destChip, destEdgeBox }) g.transform.SetAsFirstSibling();
        // D-250 ③ 문 앞 돈 글자: 잉크 상자 + 종이색 글자, 받는 사람 손 옆에서 몇 초 떠오른다.
        moneyBox = Img("Money", dark, Color.white, 40, 15, true);
        moneyText = Txt("Text", moneyBox.transform, font, fontSize, Paper, TextAnchor.MiddleCenter);
        Fill(moneyText.rectTransform, 0, 1, 0, 0);
        // 전표 2·3줄 + 대역 약도(북쪽 위, 나 = 잉크 점, 받는 곳 = 신호색 점) — 진짜 약도는 다음 판.
        slipTo = Txt("To", slip.transform, font, fontSize, Ink, TextAnchor.UpperLeft); slipTo.horizontalOverflow = HorizontalWrapMode.Overflow;
        slipAddr = Txt("Addr", slip.transform, font, fontSize, Ink, TextAnchor.UpperLeft); slipAddr.horizontalOverflow = HorizontalWrapMode.Overflow;
        mapTex = new Texture2D(MapN, MapN, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Map" };
        slipMap = Img("Map", Sprite.Create(mapTex, new Rect(0, 0, MapN, MapN), Vector2.zero, 100f), Color.white, MapN, MapN, false, slip.transform);

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

    /// <summary>
    /// 목적지 깃발(테 1px 포함): 종이색 깃대 + 신호색 깃발(오른쪽 끝 제비꼬리). 깃대 아래 끝 = 그림 왼쪽 아래(x 2) — 점선이 여기서 이어진다.
    /// 깃발은 x 3..fx1, y fy0..fy1, 제비꼬리 홈은 x ≥ notchX 부터 yc 를 가운데로 한 칸씩 벌어진다. 가까운 쪽 13×18 · 먼 쪽 17×24 가 같은 모양의 큰 판이다.
    /// </summary>
    static Sprite MakeFlag(int w, int h, int fy0, int fy1, int fx1, int notchX, int yc)
    {
        var kind = new int[w, h];   // 0 빈칸, 1 깃대(종이), 2 깃발(신호)
        for (int y = 1; y < h - 1; y++) kind[2, y] = 1;
        for (int y = fy0; y <= fy1; y++)
            for (int x = 3; x <= fx1; x++)
                if (!(x >= notchX && Mathf.Abs(y - yc) <= x - notchX)) kind[x, y] = 2;   // 제비꼬리 홈
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Flag" + w };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                if (kind[x, y] == 1) c = Paper;
                else if (kind[x, y] == 2) c = Signal;
                else
                {
                    for (int dy = -1; dy <= 1 && c.a == 0f; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1) continue;
                            int xx = x + dx, yy = y + dy;
                            if (xx >= 0 && xx < w && yy >= 0 && yy < h && kind[xx, yy] != 0) { c = Ink; break; }
                        }
                }
                t.SetPixel(x, y, c);
            }
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, w, h), Vector2.zero, 100f);
    }

    /// <summary>점선 한 칸 3×4: 종이 점 2줄(양옆 잉크 테) + 빈 2줄. 세로로 바둑처럼 이어 붙인다(Tiled).</summary>
    static Sprite MakeDots()
    {
        var t = new Texture2D(3, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, name = "DotUI_Dots" };
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 3; x++)
                t.SetPixel(x, y, y >= 2 ? (x == 1 ? Paper : Ink) : Color.clear);
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, 3, 4), Vector2.zero, 100f, 0, SpriteMeshType.FullRect);
    }

    /// <summary>방향 화살표를 픽셀 단위로 직접 찍는다(16방향). 회전한 그림을 늘리지 않으니 흐린 픽셀이 없다. 모양은 1판 그대로, arrowScale 배.</summary>
    void DrawArrow(int step) => DrawArrowInto(arrowTex, ArrowN, arrowOld ? 1f : ArrowScale, step);

    static void DrawArrowInto(Texture2D arrowTex, int n, float k, int step)
    {
        float c0 = (n - 1) * 0.5f;
        float a = step * (Mathf.PI * 2f / 16f);
        float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        var inside = new bool[n, n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float px = x - c0, py = y - c0;
                // 화면 점을 +a 만큼 돌려 화살표 공간(+y 가 앞)에서 판정 = 화살표는 시계 방향으로 a. step 양수 = 오른쪽. 1/k 로 줄여 판정 = 모양이 k 배.
                float ux = (px * ca - py * sa) / k, uy = (px * sa + py * ca) / k;
                bool head = uy >= -0.5f && uy <= 5.5f && Mathf.Abs(ux) <= (5.5f - uy) * 0.95f;
                bool stem = uy >= -5.5f && uy < -0.5f && Mathf.Abs(ux) <= 1.6f;
                inside[x, y] = head || stem;
            }
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Color c = Color.clear;
                if (inside[x, y]) c = Paper;
                else if ((x > 0 && inside[x - 1, y]) || (x < n - 1 && inside[x + 1, y]) || (y > 0 && inside[x, y - 1]) || (y < n - 1 && inside[x, y + 1])) c = Ink;
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
                if (director.PickedJob != null && !director.OrderStarted)            // D-250: 칠판에서 맡은 주문 = 걷는 중에도 Tab 전표
                    SlipUI(director.PickedJob, director.FirstDrop, director.Player != null ? director.Player.transform.position : cam.transform.position, vis);
            }
        }
        else if (director != null && st == OnFootDirector.Mode.InCart && director.Prompt.Length > 0) ChipUI(vis);
        DestUI(DestTarget(st), vis);
        if (director != null && st == OnFootDirector.Mode.OnFoot) MoneyUI(vis);
        Visible = vis.ToString().Trim();
    }

    /// <summary>D-250 ③: 상자를 건넨 순간부터(반응 말풍선이 떠 있는 동안 + 끝난 뒤 1.2초, 최소 MoneySec 초) — 받는 사람 손 오른쪽에서 위로 14px 떠오르고 마지막 0.5초에 옅어진다.</summary>
    void MoneyUI(System.Text.StringBuilder vis)
    {
        string s = director.MoneyText;
        if (string.IsNullOrEmpty(s)) { MoneyShown = ""; return; }
        if (moneyText.text != s)
        {
            moneyText.text = s;
            moneyBox.rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Pref(moneyText, s, 300)) + 8, 15);
        }
        float age = Time.unscaledTime - director.MoneyAt, dur = Mathf.Max(0.5f, director.MoneySec);
        var sz = moneyBox.rectTransform.sizeDelta;
        int x, y;
        if (ToScreen(director.MoneyAnchor, out var sp)) { x = Mathf.RoundToInt(sp.x) + 8; y = Mathf.RoundToInt(sp.y); }
        else { x = W / 2 + 20; y = H / 2 - 20; }
        y += Mathf.RoundToInt(14f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / (dur * 0.6f))));
        x = Mathf.Clamp(x, 2, W - (int)sz.x - 2); y = Mathf.Clamp(y, 2, H - (int)sz.y - 2);
        float a = Mathf.Clamp01((director.MoneyEnd - Time.unscaledTime) / 0.5f);
        moneyBox.color = new Color(1f, 1f, 1f, a); moneyText.color = new Color(Paper.r, Paper.g, Paper.b, a);
        Show(moneyBox, x, y);
        MoneyPx = new Vector2(x, y); MoneyShown = s;
        vis.Append("money ");
    }

    // 실제 도트 격자 = LowResRenderTest.RenderSize(360 줄). 카메라 RT 는 URP renderScale 0.5 를 감안해 그 2배(1280×720)로
    // 잡혀 있어서 cam.pixelWidth 로 맞추면 UI 가 절반 크기로 그려져 글자 줄이 빠졌다(이 판 보고서 §3).
    LowResRenderTest lowRes;
    int W => lowRes != null && lowRes.isActiveAndEnabled ? lowRes.RenderSize.x : cam.pixelWidth;
    int H => lowRes != null && lowRes.isActiveAndEnabled ? lowRes.RenderSize.y : cam.pixelHeight;

    void HideAll()
    {
        foreach (var g in new Graphic[] { dot, arrowImg, warnBox, chipBox, bubble, tail, nameTab, slip, hintText, destFlag, destDots, destEdge, destEdgeBox, destChip, distBox, moneyBox })
            if (g != null && g.gameObject.activeSelf) g.gameObject.SetActive(false);
    }

    // ── D-252 목적지 표식 ──
    /// <summary>QA·봇이 읽는 것: 표식 상태(none / on = 문 위 / edge = 화면 가장자리 / near = '여기' 칩), 깃발 아래 끝 픽셀(RT), 거리(m).</summary>
    public string DestMode { get; private set; } = "none";
    public Vector2 DestPx { get; private set; }
    public float DestDistM { get; private set; } = -1f;
    /// <summary>비교 캡처용: 이 판 이전(표식·거리·전표 추가 없음) 화면.</summary>
    public bool SimDestOff { get; set; }
    /// <summary>비교 캡처용: 점선/빛기둥 바꾸기.</summary>
    public DestStyle Style { get => destStyle; set => destStyle = value; }

    /// <summary>지금 그릴 목적지: 운전 중 = DeliveryManager.CurrentTarget, 걷는 중 = 사장에게 들은 첫 배달지(탑승 전).</summary>
    DeliveryPoint DestTarget(OnFootDirector.Mode st)
    {
        if (SimDestOff) return null;
        if (st == OnFootDirector.Mode.InCart) { var mgr = Services.Delivery as DeliveryManager; return mgr != null && mgr.CurrentTarget != null ? mgr.CurrentTarget : PreOrderDest; }
        if (st == OnFootDirector.Mode.OnFoot && director != null && !director.InTalk) return PreOrderDest;
        return null;
    }

    /// <summary>주문 출발 전 갈 곳(D-250): 보관소 흐름이면 사장 말 뒤 = 택배 보관소, 칠판에서 맡은 뒤 = 찻집. 옛 흐름이면 찻집.</summary>
    DeliveryPoint PreOrderDest => director != null && director.isActiveAndEnabled && !director.OrderStarted ? director.WalkDest : null;

    Vector3 Ground(DeliveryPoint dp)
    {
        if (groundFor != dp)
        {
            groundFor = dp;
            Vector3 p = dp.transform.position;
            groundAt = Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out var h, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? h.point : p;
        }
        return groundAt;
    }

    void DestUI(DeliveryPoint tgt, System.Text.StringBuilder vis)
    {
        DestMode = "none"; DestDistM = -1f;
        bool beamOn = false;
        if (tgt != null)
        {
            Vector3 me = director != null && director.State == OnFootDirector.Mode.InCart && director.Car != null ? director.Car.transform.position
                       : director != null && director.Player != null ? director.Player.transform.position : cam.transform.position;
            Vector3 gnd = Ground(tgt), top = DestAnchorOf(tgt);
            float dist = Vector2.Distance(new Vector2(me.x, me.z), new Vector2(gnd.x, gnd.z));
            DestDistM = dist;
            ApplyFlagTier(dist);
            beamOn = destStyle != DestStyle.Dots && dist > destNearM * 0.5f;

            var s = cam.WorldToScreenPoint(top);
            Vector2 p = new Vector2(s.x * W / cam.pixelWidth, s.y * H / cam.pixelHeight);
            bool front = s.z > 0.05f;
            var fs = destFlag.rectTransform.sizeDelta;
            int fw = (int)fs.x, fh = (int)fs.y;
            // 위쪽 띠(화살표·거리·위험 '!')는 비워 둔다.
            float xMin = 4, xMax = W - 4 - fw, yMin = 4, yMax = H - 4 - ArrowN - 4 - fh;
            bool inside = front && p.x - 2 >= xMin && p.x - 2 <= xMax && p.y >= yMin && p.y <= yMax;

            if (inside && dist <= destNearM)
            {
                string who = NearLabel();
                destChipText.text = who;
                int lw = Mathf.CeilToInt(Pref(destChipText, who, 400));
                int w = 4 + lw + 4;
                destChip.rectTransform.sizeDelta = new Vector2(w, 15);
                var lr = destChipText.rectTransform; lr.anchoredPosition = new Vector2(4, 1); lr.sizeDelta = new Vector2(lw + 2, 13);
                int cx = Mathf.Clamp(Mathf.RoundToInt(p.x) - w / 2, 2, W - w - 2), cy = Mathf.Clamp(Mathf.RoundToInt(p.y), 2, H - 15 - 2);
                Show(destChip, cx, cy); vis.Append("destchip ");
                DotsDown(p, gnd, cx + w / 2, cy, vis);
                DestMode = "near"; DestPx = new Vector2(cx + w / 2, cy);
            }
            else if (inside)
            {
                int fx = Mathf.RoundToInt(p.x) - 2, fy = Mathf.RoundToInt(p.y);
                Show(destFlag, fx, fy); vis.Append("dest ");
                if (destStyle != DestStyle.Beam) DotsDown(p, gnd, fx + 2, fy, vis);
                DestMode = "on"; DestPx = new Vector2(fx + 2, fy);
            }
            else
            {
                // 화면 밖·카메라 뒤: 가운데에서 그 방향으로 뻗은 선이 테두리에 닿는 곳에 깃발 + 바깥쪽 화살.
                Vector2 c = new Vector2(W * 0.5f, H * 0.5f), d;
                if (front) d = p - c;
                else
                {
                    Vector3 f = cam.transform.forward; f.y = 0f; Vector3 to = top - cam.transform.position; to.y = 0f;
                    d = new Vector2(Mathf.Sign(Vector3.SignedAngle(f, to, Vector3.up)), -0.35f);
                }
                if (d.sqrMagnitude < 1e-4f) d = Vector2.down;
                float ex = (d.x > 0 ? xMax - EdgeN - 1 - c.x : c.x - xMin - EdgeN - 1) / Mathf.Max(1e-3f, Mathf.Abs(d.x));
                float ey = (d.y > 0 ? yMax - c.y : c.y - yMin) / Mathf.Max(1e-3f, Mathf.Abs(d.y));
                Vector2 q = c + d * Mathf.Min(ex, ey);
                int fx = Mathf.RoundToInt(Mathf.Clamp(q.x, xMin + EdgeN + 1, xMax - EdgeN - 1)), fy = Mathf.RoundToInt(Mathf.Clamp(q.y, yMin, yMax));
                Show(destFlag, fx, fy);
                int step = Mathf.RoundToInt(Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg / 22.5f);   // 위 = 0, 오른쪽 = +4
                if (step != edgeStep) { edgeStep = step; DrawArrowInto(edgeTex, EdgeN, 0.8f, step); }
                Vector2 dn = d.normalized;
                int axp = Mathf.RoundToInt(fx + fw * 0.5f + dn.x * (fw * 0.5f + EdgeN * 0.5f + 1) - EdgeN * 0.5f);
                int ayp = Mathf.RoundToInt(fy + fh * 0.5f + dn.y * (fh * 0.5f + EdgeN * 0.5f + 1) - EdgeN * 0.5f);
                axp = Mathf.Clamp(axp, 1, W - EdgeN - 1); ayp = Mathf.Clamp(ayp, 1, H - EdgeN - 1);
                Show(destEdge, axp, ayp);
                int bx0 = Mathf.Min(fx, axp) - 2, by0 = Mathf.Min(fy, ayp) - 2;
                int bx1 = Mathf.Max(fx + fw, axp + EdgeN) + 2, by1 = Mathf.Max(fy + fh, ayp + EdgeN) + 2;
                destEdgeBox.rectTransform.sizeDelta = new Vector2(bx1 - bx0, by1 - by0);
                Show(destEdgeBox, bx0, by0);
                vis.Append("destedge ");
                DestMode = "edge"; DestPx = new Vector2(fx + 2, fy);
            }
        }
        Beam(tgt, beamOn);
        if (beamOn) vis.Append("beam ");
        int area = 0;
        foreach (var g in new Graphic[] { destFlag, destDots, destEdge, destEdgeBox, destChip, distBox })
            if (g.gameObject.activeSelf) area += Mathf.RoundToInt(g.rectTransform.sizeDelta.x * g.rectTransform.sizeDelta.y);
        DestAreaPct = area * 100f / (W * H);
    }

    /// <summary>먼 곳은 큰 깃발, 가까우면 원래 크기. 경계(FlagFarM)에서 FlagHysteresisM 만큼 여유를 둬 깜빡이지 않게 한다. 슬롯 스프라이트를 쓰면 정수 2배.</summary>
    void ApplyFlagTier(float dist)
    {
        if (!flagFar && dist > FlagFarM) flagFar = true;
        else if (flagFar && dist < FlagFarM - FlagHysteresisM) flagFar = false;
        bool far = flagFar && !SimFlagNear;
        var sp = far ? flagFarSp : flagNearSp;
        if (destFlag.sprite != sp) destFlag.sprite = sp;
        int k = far && flagSprite != null ? 2 : 1;
        destFlag.rectTransform.sizeDelta = new Vector2(sp.rect.width * k, sp.rect.height * k);
    }

    /// <summary>표식(깃발·점선·가장자리·칩·거리) 화면 면적 합(%, 겹침 포함 위쪽 어림) — 과제 기준 4% 이내.</summary>
    public float DestAreaPct { get; private set; }

    string NearLabel()
    {
        var rc = Services.Run as RunController;
        var job = rc != null ? rc.CurrentJob : null;
        string to = job != null && !string.IsNullOrEmpty(job.callerName) ? job.callerName : "받는 곳";
        return "여기 · " + to;
    }

    /// <summary>깃발 아래(fx,fy)에서 배달점 땅까지 도트 점선. 땅이 화면 밖·뒤면 화면 아래 끝까지.</summary>
    void DotsDown(Vector2 topPx, Vector3 gnd, int x, int y, System.Text.StringBuilder vis)
    {
        float gy = ToScreen(gnd, out var gp) ? gp.y : 0f;
        int len = Mathf.Clamp(y - Mathf.RoundToInt(gy), 0, y - 2);
        len -= len % 4;
        if (len < 4) return;
        destDots.rectTransform.sizeDelta = new Vector2(3, len);
        Show(destDots, x - 1, y - len); vis.Append("dots ");
    }

    /// <summary>은은한 빛기둥(세상 속, 건물에 가려진다): 배달점 땅에서 위로 beamHeight, 카메라 쪽으로 도는 판 하나. 위로 갈수록 옅다.</summary>
    void Beam(DeliveryPoint tgt, bool on)
    {
        if (!on || tgt == null) { if (beam != null && beam.activeSelf) beam.SetActive(false); return; }
        if (beam == null)
        {
            beam = GameObject.CreatePrimitive(PrimitiveType.Quad);
            beam.name = "DotUI_DestBeam";
            Destroy(beam.GetComponent<Collider>());
            var tex = new Texture2D(1, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_BeamFade" };
            for (int y = 0; y < 32; y++) tex.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Pow(1f - y / 31f, 1.6f)));
            tex.Apply(false, true);
            beamMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "DotUI_DestBeam" };
            beamMat.SetTexture("_BaseMap", tex);
            beamMat.SetFloat("_Surface", 1f); beamMat.SetFloat("_Blend", 0f);
            beamMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            beamMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            beamMat.SetInt("_ZWrite", 0);
            beamMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            beamMat.renderQueue = 3000;
            var mr = beam.GetComponent<MeshRenderer>();
            mr.sharedMaterial = beamMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }
        if (!beam.activeSelf) beam.SetActive(true);
        var l = UIPalette.Lemon;
        beamMat.SetColor("_BaseColor", new Color(l.r, l.g, l.b, beamAlpha));
        Vector3 g = Ground(tgt);
        beam.transform.position = g + Vector3.up * (beamHeight * 0.5f);
        Vector3 to = cam.transform.position - g; to.y = 0f;
        if (to.sqrMagnitude > 1e-4f) beam.transform.rotation = Quaternion.LookRotation(-to.normalized, Vector3.up);
        beam.transform.localScale = new Vector3(beamWidth, beamHeight, 1f);
    }

    /// <summary>대역 약도: 북쪽 위. 나(잉크 3×3)와 받는 곳(신호색 5×5)이 다 들어가게 줄이고, 사이를 점선으로.</summary>
    void DrawMap(Vector3 me, Vector3 dst)
    {
        var px = new Color32[MapN * MapN];
        Color32 bg = UIPalette.PaperDeep, ink = Ink, sig = Signal, pap = Paper;
        for (int i = 0; i < px.Length; i++) px[i] = bg;
        for (int i = 0; i < MapN; i++) { px[i] = ink; px[(MapN - 1) * MapN + i] = ink; px[i * MapN] = ink; px[i * MapN + MapN - 1] = ink; }
        Vector2 a = new Vector2(me.x, me.z), b = new Vector2(dst.x, dst.z), mid = (a + b) * 0.5f;
        float span = Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), 20f) * 0.5f;
        float k = (MapN * 0.5f - 7f) / span;
        Vector2Int P(Vector2 w) => new Vector2Int(Mathf.RoundToInt(MapN * 0.5f + (w.x - mid.x) * k), Mathf.RoundToInt(MapN * 0.5f + (w.y - mid.y) * k));
        void Put(int x, int y, Color32 c) { if (x > 0 && y > 0 && x < MapN - 1 && y < MapN - 1) px[y * MapN + x] = c; }
        var pa = P(a); var pb = P(b);
        int steps = Mathf.Max(1, Mathf.RoundToInt(Vector2Int.Distance(pa, pb)));
        for (int s = 0; s <= steps; s++) if (s % 3 == 0) { var q = Vector2.Lerp(pa, pb, s / (float)steps); Put(Mathf.RoundToInt(q.x), Mathf.RoundToInt(q.y), ink); }
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++) Put(pb.x + x, pb.y + y, (Mathf.Abs(x) == 2 || Mathf.Abs(y) == 2) ? ink : sig);
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) Put(pa.x + x, pa.y + y, ink);
        Put(pa.x, pa.y, pap);
        // 북쪽 표시: 위 가운데 잉크 삼각 3줄
        for (int r = 0; r < 3; r++) for (int x = -r; x <= r; x++) Put(MapN / 2 + x, MapN - 3 - r, ink);
        mapTex.SetPixels32(px); mapTex.Apply(false);
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
        var tgt = mgr != null && mgr.CurrentTarget != null ? mgr.CurrentTarget : PreOrderDest;   // D-250: 정비소 → 보관소 자유 주행 중엔 보관소
        if (tgt != null)
        {
            Vector3 f = cam.transform.forward; f.y = 0f;
            Vector3 d = tgt.transform.position - cam.transform.position; d.y = 0f;
            if (f.sqrMagnitude > 1e-4f && d.sqrMagnitude > 1e-4f)
            {
                float ang = Vector3.SignedAngle(f, d, Vector3.up);
                int step = Mathf.RoundToInt(ang / 22.5f);
                if (step != arrowStep) { arrowStep = step; DrawArrow(step); }
                Show(arrowImg, W / 2 - ArrowN / 2, arrowOld ? H - 6 - 15 - 4 : H - 4 - ArrowN); vis.Append("arrow ");   // 1판 중심(위에서 14줄) → 새 중심(16줄): 화살 끝이 화면 위에서 ~7줄로 같다
            }
            // D-252 거리: 화살표 왼쪽(오른쪽은 위험 '!' 자리). 100 m 넘으면 10 m, 그 안은 5 m 단위 — 숫자가 매 프레임 떨지 않게.
            if (showDistance && !SimDestOff)
            {
                Vector3 me = director != null && director.Car != null ? director.Car.transform.position : cam.transform.position;
                Vector3 g = Ground(tgt);
                float dm = Vector2.Distance(new Vector2(me.x, me.z), new Vector2(g.x, g.z));
                int unit = dm >= 100f ? 10 : 5;
                string s = (Mathf.RoundToInt(dm / unit) * unit) + "m";
                distText.text = s;
                int bw = Mathf.CeilToInt(Pref(distText, s, 100)) + 7;
                distBox.rectTransform.sizeDelta = new Vector2(bw, 13);
                Show(distBox, W / 2 - ArrowN / 2 - 2 - bw, H - 4 - ArrowN / 2 - 7); vis.Append("dist ");
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
        var rc = Services.Run as RunController;
        var job = rc != null ? rc.CurrentJob : null;
        Vector3 at = director != null && director.Car != null ? director.Car.transform.position : cam.transform.position;
        if (!SlipUI(job, mgr != null ? mgr.CurrentTarget : null, at, vis) && job != null && Time.unscaledTime - cartAt < slipHintSec) { Show(hintText, 8, 8); vis.Append("hint "); }
    }

    /// <summary>
    /// 전표(Tab 누르는 동안). 운전 중 = 지금 주문, 걷는 중(D-250) = 칠판에서 맡은 주문(출발 전). 그렸으면 true.
    /// </summary>
    bool SlipUI(OrderJob job, DeliveryPoint tgt, Vector3 me, System.Text.StringBuilder vis)
    {
        bool tab = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        if (job != null && (tab || SimSlip))
        {
            slipBody.text = job.displayName;
            if (SimDestOff || tgt == null)
            {
                foreach (var g in new Graphic[] { slipTo, slipAddr, slipMap }) g.gameObject.SetActive(false);
                slipTitle.rectTransform.anchoredPosition = new Vector2(5, 22); slipBody.rectTransform.anchoredPosition = new Vector2(5, 6);
                int bw = Mathf.Clamp(Mathf.CeilToInt(Pref(slipBody, job.displayName, 400)) + 11, 60, 300);
                slip.rectTransform.sizeDelta = new Vector2(bw, 34);
            }
            else
            {
                // D-252: 받는 곳·받는 사람 / 주소 / 대역 약도(오른쪽). 높이 = 위 5 + 제목 11 + 줄 3×13 + 아래 5 = 60 → 약도 52 와 맞춘다.
                string to = "받는 곳  " + (string.IsNullOrEmpty(job.callerName) ? tgt.Label : job.callerName) + (string.IsNullOrEmpty(job.recipientName) ? "" : " · " + job.recipientName);
                string addr = "주소  " + (string.IsNullOrEmpty(job.address) ? "○○길(이름 미정)" : job.address);
                slipTo.text = to; slipAddr.text = addr;
                int tw = Mathf.CeilToInt(Mathf.Max(Pref(slipBody, job.displayName, 400), Mathf.Max(Pref(slipTo, to, 400), Pref(slipAddr, addr, 400))));
                int h = 62;
                slipTitle.rectTransform.anchoredPosition = new Vector2(5, h - 16);
                slipBody.rectTransform.anchoredPosition = new Vector2(5, h - 31);
                slipTo.rectTransform.anchoredPosition = new Vector2(5, h - 45); slipTo.rectTransform.sizeDelta = new Vector2(400, 14);
                slipAddr.rectTransform.anchoredPosition = new Vector2(5, h - 59); slipAddr.rectTransform.sizeDelta = new Vector2(400, 14);
                int bw = Mathf.Clamp(tw + 11 + 4 + MapN + 5, 120, 420);
                slip.rectTransform.sizeDelta = new Vector2(bw, h);
                slipMap.rectTransform.anchoredPosition = new Vector2(bw - 5 - MapN, 5);
                DrawMap(me, Ground(tgt));
                foreach (var g in new Graphic[] { slipTo, slipAddr, slipMap }) g.gameObject.SetActive(true);
                vis.Append("map ");
            }
            Show(slip, 8, 8); vis.Append("slip ");
            return true;
        }
        return false;
    }

    /// <summary>봇·캡처용: Tab 을 누른 것과 같다.</summary>
    public bool SimSlip { get; set; }
    /// <summary>비교 캡처용: 1판 크기(모양 1배)로 같은 화면에 그린다.</summary>
    public bool SimArrowV1 { get => arrowOld; set { arrowOld = value; arrowStep = int.MinValue; } }

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
