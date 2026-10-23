using UnityEngine;
using TMPro;

/// <summary>
/// PIT CREW UI 테마 SO (Resources/UITheme.asset).
/// - D-121: 종이 전표 팔레트 토큰(계획서 §2.2) + 글자 크기 단계 + 제목체(fontDisplay).
///   코드는 이 필드를 직접 읽지 말고 <see cref="UIPalette"/> 를 쓴다 — Active 가 null 이어도 같은 값으로 폴백.
/// - 레거시 "어두운 패널" 색(colorPanel·colorText…)은 Packing/Cargo/GearHUD/IntegrityHUD 가 아직 쓴다.
///   값은 그대로 둔다(종이 톤으로 바꾸면 그 화면들의 어두운 배경 위 글자가 안 보인다). 그 파일들이 토큰으로 옮기면 제거.
/// </summary>
[CreateAssetMenu(fileName = "UITheme", menuName = "PitCrew/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Fonts (D-121)")]
    [Tooltip("본문·숫자 — Pretendard Regular SDF (Bold 는 폰트 에셋 weight table 700 = Pretendard Bold).")]
    public TMP_FontAsset fontPrimary;
    [Tooltip("레거시 보조 폰트(HUD 단위·라벨). D-121 메뉴가 Pretendard Bold 로 바꾼다.")]
    public TMP_FontAsset fontCondensed;
    [Tooltip("제목·도장 — 둥근 제목체(Jua) SDF. 짧은 제목 전용.")]
    public TMP_FontAsset fontDisplay;

    [Header("Paper slip palette (D-121, 계획서 §2.2)")]
    public Color ink = UIPalette.DefaultInk;               // #23303B 글자·테두리·어두운 바탕(순수 검정 금지)
    public Color inkMuted = UIPalette.DefaultInkMuted;     // #5E6872 종이 위 보조 글자
    public Color paper = UIPalette.DefaultPaper;           // #F3E9D2 패널·카드·전표
    public Color paperDeep = UIPalette.DefaultPaperDeep;   // #E4D5B3 호버·보조 칸·트랙
    public Color lemon = UIPalette.DefaultLemon;           // #F2C230 돈·시간·선택 강조(바탕 / 잉크 위 글자)
    public Color lemonInk = UIPalette.DefaultLemonInk;     // #8F6A00 종이 위 강조 글자(레몬은 크림 위에서 안 읽힘)
    public Color terracotta = UIPalette.DefaultTerracotta; // #C8553D 주 버튼·출발
    public Color tyrrhenian = UIPalette.DefaultTyrrhenian; // #1F7A9C 정보·경로·보조 버튼
    public Color signal = UIPalette.DefaultSignal;         // #D93A2B 긴급·파손·시간 초과 전용(도장)
    public Color olive = UIPalette.DefaultOlive;           // #6A9A3A 성공·완벽
    public Color stone = UIPalette.DefaultStone;           // #A69F93 비활성·구분선
    public Color shade = UIPalette.DefaultShade;           // ink 60% — 모달 뒤 어둡게

    [Header("Type scale (D-121, 1920×1080 기준)")]
    public float sizeTitle = 60f;    // 화면 제목(제목체)
    public float sizeHeading = 36f;  // 카드 제목·큰 버튼
    public float sizeBody = 26f;     // 본문
    public float sizeSmall = 20f;    // 보조 설명
    public float sizeNumber = 44f;   // 시간·돈 큰 숫자

    [Header("D-121 스프라이트 슬롯 — null 이면 단색 Image 폴백 (아트는 나중에 꽂기만)")]
    [Tooltip("큰 종이 패널(정산 영수증·전화 쪽지·사이드 패널). 9-slice, 원색 그대로(흰 틴트).")]
    public Sprite paperPanel;
    [Tooltip("주문 전표 카드·업그레이드 카드. 9-slice, 원색 그대로.")]
    public Sprite paperCard;
    [Tooltip("전표 머리 띠(절취선·색 띠). 흰/회색으로 그려 두면 코드가 상태색으로 틴트.")]
    public Sprite slipHeader;
    [Tooltip("도장 테두리(긴급·완료 도장). 흰색으로 그려 두면 코드가 signal/olive 로 틴트.")]
    public Sprite stampRing;
    [Tooltip("버튼 바탕. 흰/회색으로 그려 두면 코드가 테라코타·바다색 등으로 틴트. 9-slice.")]
    public Sprite buttonPlate;
    [Tooltip("주행 HUD 법랑 표지판 판. 9-slice, 원색 그대로.")]
    public Sprite enamelPlate;
    [Tooltip("게이지 채움(평면). 흰색으로 그려 두면 코드가 올리브/레몬/신호색으로 틴트.")]
    public Sprite gaugeFill;

    [Header("Panels (9-slice) — Grey 계열 (레거시)")]
    public Sprite panelDark;       // Grey/button_rectangle_depth_flat — 기본 HUD 패널
    public Sprite panelBright;     // Grey/button_rectangle_flat — 가벼운 panel
    public Sprite panelGloss;      // Grey/button_rectangle_gloss — Pause 메뉴 배경
    public Sprite barTrack;        // slide_horizontal_grey_section_wide

    [Header("Fills — 상태별 Sprite")]
    public Sprite fillOk;          // Green/button_rectangle_flat
    public Sprite fillWarn;        // Yellow/button_rectangle_flat
    public Sprite fillCrit;        // Red/button_rectangle_flat
    public Sprite fillAccent;      // Yellow/button_rectangle_gloss (강조)

    [Header("Buttons")]
    public Sprite buttonNormal;    // Grey/button_rectangle_gloss
    public Sprite buttonHighlight; // Yellow/button_rectangle_gloss

    [Header("Icons / Arrows")]
    public Sprite arrowNorth;      // Yellow/arrow_basic_n (회전 기반 화살표)
    public Sprite iconCheck;       // Green/icon_checkmark

    [Header("레거시 어두운 패널 색 — Packing/Cargo/GearHUD/IntegrityHUD 전용 (값 유지)")]
    public Color colorPanel = new Color(0.12f, 0.14f, 0.18f, 0.92f);      // 어두운 남색 느낌 panel
    public Color colorText = new Color(0.98f, 0.98f, 0.98f, 1f);          // 흰색, 본문
    public Color colorTextMuted = new Color(0.72f, 0.74f, 0.78f, 1f);     // 밝은 회색, 보조
    public Color colorTextFlash = Color.white;
    public Color colorAccent = new Color(1f, 0.82f, 0.25f, 1f);           // 밝은 노랑 (강조)
    public Color colorOkText = new Color(0.55f, 0.95f, 0.60f, 1f);        // 밝은 녹색
    public Color colorWarnText = new Color(1f, 0.82f, 0.30f, 1f);         // 밝은 노랑
    public Color colorCritText = new Color(1f, 0.45f, 0.42f, 1f);         // 밝은 빨강
    public Color colorTextOnFill = Color.white;                            // fill 위 흰 텍스트 유지

    [Header("Sounds")]
    public AudioClip sfxClick;
    public AudioClip sfxBack;
    public AudioClip sfxConfirm;
    public AudioClip sfxShift;

    /// <summary>게이지 레벨(0~1)에 맞는 fill sprite 반환.</summary>
    public Sprite FillForLevel(float level01, float warnThreshold, float critThreshold)
    {
        if (level01 <= critThreshold) return fillCrit;
        if (level01 <= warnThreshold) return fillWarn;
        return fillOk;
    }

    /// <summary>타이머 남은 시간에 맞는 배경 sprite 반환.</summary>
    public Sprite PanelForTimer(float remainingSec)
    {
        if (remainingSec < 30f) return fillCrit;
        if (remainingSec < 60f) return fillWarn;
        return panelDark;
    }

    static UITheme _runtime;
    public static UITheme Active
    {
        get
        {
            if (_runtime == null) _runtime = Resources.Load<UITheme>("UITheme");
            return _runtime;
        }
    }

    /// <summary>Edit-mode에서 asset 변경 후 static cache 무효화.</summary>
    public static void InvalidateCache() { _runtime = null; }
}
