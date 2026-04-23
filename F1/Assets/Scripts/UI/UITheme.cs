using UnityEngine;
using TMPro;

/// <summary>
/// PIT CREW UI 테마 SO.
/// - Kenney UI pack 5색(Grey/Blue/Green/Yellow/Red) + Kenney Future TMP 폰트 참조.
/// - 세션 13 개조: Image.color tint 방식 → Sprite 스왑 방식. Kenney 원색 그대로 사용.
/// </summary>
[CreateAssetMenu(fileName = "UITheme", menuName = "PitCrew/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Fonts")]
    public TMP_FontAsset fontPrimary;
    public TMP_FontAsset fontCondensed;

    [Header("Panels (9-slice) — Grey 계열")]
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

    [Header("Text Colors — Kenney 밝은 회색 패널 대비용 검정 계열")]
    public Color colorText = new Color(0.18f, 0.19f, 0.22f, 1f);        // near-black, 본문
    public Color colorTextMuted = new Color(0.38f, 0.40f, 0.44f, 1f);   // 중간 회색, 보조
    public Color colorTextFlash = Color.white;                            // flash 순간만 흰색
    public Color colorAccent = new Color(0.92f, 0.58f, 0.10f, 1f);        // 진한 주황 (노란 fill과 구별)
    public Color colorOkText = new Color(0.20f, 0.55f, 0.25f, 1f);         // 진녹색 텍스트
    public Color colorWarnText = new Color(0.75f, 0.50f, 0.05f, 1f);       // 진노랑
    public Color colorCritText = new Color(0.78f, 0.18f, 0.18f, 1f);       // 진빨강
    public Color colorTextOnFill = Color.white;                           // Yellow/Red fill 위 흰 텍스트

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
