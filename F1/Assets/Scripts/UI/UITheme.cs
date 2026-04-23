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

    [Header("Panel Tint — Kenney sprite 위에 덮어 어둡게")]
    public Color colorPanel = new Color(0.12f, 0.14f, 0.18f, 0.92f);      // 어두운 남색 느낌 panel

    [Header("Text Colors — 어두운 패널 위 흰 텍스트 체계")]
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
