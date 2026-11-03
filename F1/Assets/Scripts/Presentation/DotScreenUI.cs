using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// UI 1판(D-240, 과제 20261101-0515): 화면 위에 매끈하게 얹는 HUD 대신, 저해상도 RT(LowResRenderTest) 안에 같이 그리는 도트 UI.
/// Screen Space - Camera 캔버스를 Main Camera 에 붙인다 → 캔버스 1 단위 = RT 1 픽셀, 세상과 같은 격자·같은 Present(Nearest·노을 역광 테)를 받는다.
/// 그리는 것(전부 간단히):
///   주행 — 위 가운데 목적지 방향 화살표 1개 + 화물 위험 "!" 1개(위험할 때만) + Tab 누르는 동안 전표.
///   걷기 — 가운데 조준점 2×2 + [E] 칩을 그 물건 곁에(DayView.PromptAnchor).
///   대화 — 말하는 사람 머리 위 말풍선(이름표 + 한 글자씩). 시선 이동은 감독(Stages)이 한다.
/// D-287 5단계: 감독을 직접 보지 않는다 — 감독이 매 프레임 적는 View(DayView)만 읽는다.
/// 기존 HUD(속도·기어·화물 막대·목적지 등)는 이 컴포넌트가 켜져 있는 동안만 끈다(SetActive false, 지우지 않음). 이 컴포넌트를 끄면 되돌린다.
/// 글꼴: Galmuri11/9(OFL, Assets/UI/Fonts/Galmuri) — 가져오기 설정 Hinted Raster = 안티앨리어싱 없음. 아트 스프라이트 슬롯은 아래 *Sprite 필드(비면 코드 임시 모양).
/// </summary>
[DefaultExecutionOrder(200)]
public partial class DotScreenUI : MonoBehaviour, IScreenFonts
{
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
    DayState.Mode lastState;
    float cartAt = -99f;
    string bubbleLine;
    TextGenerator gen;

    /// <summary>본문 글꼴(Galmuri11) — 개발 디버그 창(DebugWindow)이 같은 글꼴을 쓴다.</summary>
    public Font BodyFont => font;
    /// <summary>작은 글꼴(Galmuri9, 10px) — 방 달력 숫자가 같은 글꼴을 쓴다.</summary>
    public Font SmallFont => smallFont;

    /// <summary>QA·봇이 읽는 것 — 지금 보이는 요소 이름들.</summary>
    public string Visible { get; private set; } = "";

    void Start()
    {
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

        BuildDiary();
        HideAll();
        Debug.Log($"[DotScreenUI] 캔버스 {W}×{H} (RT={(cam.targetTexture != null ? cam.targetTexture.width + "×" + cam.targetTexture.height : "없음")}) font={(font != null ? font.name : "없음")}");
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
