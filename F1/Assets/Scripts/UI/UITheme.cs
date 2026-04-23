using UnityEngine;
using TMPro;

/// <summary>
/// PIT CREW UI 테마 SO.
/// - Kenney UI pack(Grey+Yellow, Warm 아트 프리셋 매칭) + Kenney Future TMP 폰트 참조.
/// - 모든 HUD는 이 SO를 참조해서 색/폰트/스프라이트 일관 유지.
/// - 설계 원칙: HUD는 OnGUI를 버리고 Canvas 기반. 색/폰트를 인스펙터 노출하지 않고 이 SO로 통일.
/// </summary>
[CreateAssetMenu(fileName = "UITheme", menuName = "PitCrew/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Fonts")]
    public TMP_FontAsset fontPrimary;   // KenneyFuture SDF (본문/숫자)
    public TMP_FontAsset fontCondensed; // KenneyFutureNarrow SDF (라벨)

    [Header("Panels (9-slice)")]
    public Sprite panelBackground;      // 어두운 반투명 패널 (button_rectangle_flat Grey)
    public Sprite barTrack;             // 게이지 홈 (slide_horizontal_grey_section_wide)
    public Sprite barFill;              // 게이지 채움 (solid white, tint으로 색상)

    [Header("Colors — 게이지 상태")]
    public Color colorOk = new Color(0.4f, 0.85f, 0.45f, 0.95f);
    public Color colorWarn = new Color(1f, 0.80f, 0.30f, 0.98f);
    public Color colorCrit = new Color(0.95f, 0.32f, 0.32f, 1f);
    public Color colorCritPulse = new Color(1f, 0.6f, 0.6f, 1f);

    [Header("Colors — 패널/텍스트")]
    public Color colorPanel = new Color(0f, 0f, 0f, 0.55f);
    public Color colorText = new Color(0.96f, 0.96f, 0.95f, 1f);
    public Color colorTextMuted = new Color(0.70f, 0.70f, 0.68f, 1f);
    public Color colorTextFlash = Color.white;
    public Color colorAccent = new Color(1f, 0.85f, 0.28f, 1f); // Kenney Yellow

    [Header("Countdown — 시간대별")]
    public Color colorTimerNormal = new Color(0f, 0f, 0f, 0.55f);
    public Color colorTimerMid = new Color(0.85f, 0.58f, 0.10f, 0.85f);
    public Color colorTimerLow = new Color(0.75f, 0.15f, 0.15f, 0.92f);

    [Header("Sounds")]
    public AudioClip sfxClick;
    public AudioClip sfxBack;
    public AudioClip sfxConfirm;
    public AudioClip sfxShift; // 기어 시프트용 (bong 계열)

    static UITheme _runtime;

    /// <summary>Resources/UITheme에서 로드 (싱글톤 접근).</summary>
    public static UITheme Active
    {
        get
        {
            if (_runtime == null)
                _runtime = Resources.Load<UITheme>("UITheme");
            return _runtime;
        }
    }
}
