using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>D-121 스프라이트 슬롯 종류 — UITheme 의 paperPanel/paperCard/… 에 대응.</summary>
public enum UISlot { Panel, Card, Header, Stamp, Button, Plate, Gauge }

/// <summary>
/// D-121 디자인 토큰 접근점 — "종이 전표·도장·영수증".
/// UITheme.Active(Resources/UITheme.asset) 의 토큰을 읽고, 없으면 같은 기본값으로 폴백한다.
/// 색은 코드에 직접 쓰지 말고 여기서 꺼낸다(계획서 §2.2 — 한 화면 강조색 2개, signal 은 긴급·파손 전용).
/// </summary>
public static class UIPalette
{
    // ── 기본값 (계획서 §2.2) ───────────────────────────────────────
    public static readonly Color DefaultInk        = new Color32(0x23, 0x30, 0x3B, 0xFF);
    public static readonly Color DefaultInkMuted   = new Color32(0x5E, 0x68, 0x72, 0xFF);
    public static readonly Color DefaultPaper      = new Color32(0xF3, 0xE9, 0xD2, 0xFF);
    public static readonly Color DefaultPaperDeep  = new Color32(0xE4, 0xD5, 0xB3, 0xFF);
    public static readonly Color DefaultLemon      = new Color32(0xF2, 0xC2, 0x30, 0xFF);
    public static readonly Color DefaultLemonInk   = new Color32(0x8F, 0x6A, 0x00, 0xFF);
    public static readonly Color DefaultTerracotta = new Color32(0xC8, 0x55, 0x3D, 0xFF);
    public static readonly Color DefaultTyrrhenian = new Color32(0x1F, 0x7A, 0x9C, 0xFF);
    public static readonly Color DefaultSignal     = new Color32(0xD9, 0x3A, 0x2B, 0xFF);
    public static readonly Color DefaultOlive      = new Color32(0x6A, 0x9A, 0x3A, 0xFF);
    public static readonly Color DefaultStone      = new Color32(0xA6, 0x9F, 0x93, 0xFF);
    public static readonly Color DefaultShade      = new Color32(0x23, 0x30, 0x3B, 0x99); // ink 60%

    static UITheme T => UITheme.Active;

    // ── 색 토큰 ──────────────────────────────────────────────────
    public static Color Ink        => T != null ? T.ink        : DefaultInk;
    public static Color InkMuted   => T != null ? T.inkMuted   : DefaultInkMuted;
    public static Color Paper      => T != null ? T.paper      : DefaultPaper;
    public static Color PaperDeep  => T != null ? T.paperDeep  : DefaultPaperDeep;
    public static Color Lemon      => T != null ? T.lemon      : DefaultLemon;
    public static Color LemonInk   => T != null ? T.lemonInk   : DefaultLemonInk;
    public static Color Terracotta => T != null ? T.terracotta : DefaultTerracotta;
    public static Color Tyrrhenian => T != null ? T.tyrrhenian : DefaultTyrrhenian;
    public static Color Signal     => T != null ? T.signal     : DefaultSignal;
    public static Color Olive      => T != null ? T.olive      : DefaultOlive;
    public static Color Stone      => T != null ? T.stone      : DefaultStone;
    public static Color Shade      => T != null ? T.shade      : DefaultShade;

    // ── 글자 크기 ────────────────────────────────────────────────
    public static float SizeTitle   => T != null ? T.sizeTitle   : 60f;
    public static float SizeHeading => T != null ? T.sizeHeading : 36f;
    public static float SizeBody    => T != null ? T.sizeBody    : 26f;
    public static float SizeSmall   => T != null ? T.sizeSmall   : 20f;
    public static float SizeNumber  => T != null ? T.sizeNumber  : 44f;

    // ── 폰트 (null 이면 TMP Settings 기본 폰트가 그대로 쓰인다) ─────
    public static TMP_FontAsset BodyFont => T != null ? T.fontPrimary : null;
    public static TMP_FontAsset DisplayFont
        => T == null ? null : (T.fontDisplay != null ? T.fontDisplay : T.fontPrimary);

    // ── 유틸 ─────────────────────────────────────────────────────
    public static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    /// <summary>TMP 리치 텍스트용 "#RRGGBB".</summary>
    public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    /// <summary>본문 글꼴 + 색. 글꼴 에셋이 없으면 글꼴은 건드리지 않는다.</summary>
    public static void Body(TMP_Text t, Color color)
    {
        if (t == null) return;
        var f = BodyFont; if (f != null) t.font = f;
        t.color = color;
    }

    /// <summary>제목체(Jua) + 색. 굵기는 제목체 자체가 두꺼우므로 Bold 를 끈다(가짜 굵게 방지).</summary>
    public static void Display(TMP_Text t, Color color)
    {
        if (t == null) return;
        var f = DisplayFont; if (f != null) { t.font = f; t.fontStyle &= ~FontStyles.Bold; }
        t.color = color;
    }

    /// <summary>
    /// 종이/버튼/표지판 바탕을 깐다. UITheme 의 해당 스프라이트 슬롯이 있으면 그 스프라이트(9-slice),
    /// 없으면 임시 모양 = 단색 + 잉크 테두리 + 흐림 없는 오프셋 그림자(계획서 §2.4).
    /// 원색 슬롯(Panel·Card·Plate)은 fill 을 따로 안 주면 흰 틴트(스프라이트 원색), 틴트 슬롯(Button·Header·Stamp·Gauge)은 fill 로 틴트.
    /// 여러 번 불러도 효과 컴포넌트는 하나씩.
    /// </summary>
    public static void PaperPanel(Image img, Color? fill = null, bool border = true, bool shadow = true,
        UISlot slot = UISlot.Panel)
    {
        if (img == null) return;
        var sprite = SlotSprite(slot);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            bool tintable = slot == UISlot.Button || slot == UISlot.Header || slot == UISlot.Stamp || slot == UISlot.Gauge;
            img.color = tintable ? (fill ?? Paper) : (fill ?? Color.white);
            // 스프라이트가 테두리·그림자를 직접 가진다 → 코드 효과 제거
            foreach (var e in img.GetComponents<Shadow>()) e.enabled = false;
            return;
        }
        img.sprite = null;
        img.type = Image.Type.Simple;
        img.color = fill ?? Paper;
        if (shadow) FlatShadow(img);
        if (border) InkBorder(img);
    }

    /// <summary>띠·도장·게이지처럼 효과 없이 색만 칠하는 면: 슬롯 스프라이트가 있으면 꽂고 color 로 틴트.</summary>
    public static void Tint(Image img, UISlot slot, Color color)
    {
        if (img == null) return;
        var sprite = SlotSprite(slot);
        if (sprite != null && img.sprite != sprite)
        {
            img.sprite = sprite;
            img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        }
        img.color = color;
    }

    /// <summary>슬롯 스프라이트(없으면 null → 단색 폴백).</summary>
    public static Sprite SlotSprite(UISlot slot)
    {
        var t = T;
        if (t == null) return null;
        switch (slot)
        {
            case UISlot.Panel:  return t.paperPanel;
            case UISlot.Card:   return t.paperCard != null ? t.paperCard : t.paperPanel;
            case UISlot.Header: return t.slipHeader;
            case UISlot.Stamp:  return t.stampRing;
            case UISlot.Button: return t.buttonPlate;
            case UISlot.Plate:  return t.enamelPlate != null ? t.enamelPlate : t.paperPanel;
            case UISlot.Gauge:  return t.gaugeFill;
            default:            return null;
        }
    }

    /// <summary>잉크 테두리(Outline, 기본 3px).</summary>
    public static void InkBorder(Graphic g, float px = 3f)
    {
        if (g == null) return;
        var o = g.GetComponent<Outline>();
        if (o == null) o = g.gameObject.AddComponent<Outline>();
        o.effectColor = Ink;
        o.effectDistance = new Vector2(px, -px);
        o.useGraphicAlpha = true;
    }

    /// <summary>흐림 없는 평면 오프셋 그림자(4px, ink 40%).</summary>
    public static void FlatShadow(Graphic g, float px = 4f)
    {
        if (g == null) return;
        Shadow s = null;
        foreach (var c in g.GetComponents<Shadow>())
            if (c.GetType() == typeof(Shadow)) { s = c; break; }     // Outline 은 Shadow 의 하위 클래스 — 제외
        if (s == null) s = g.gameObject.AddComponent<Shadow>();
        s.effectColor = WithAlpha(Ink, 0.4f);
        s.effectDistance = new Vector2(px, -px);
        s.useGraphicAlpha = true;
    }

    /// <summary>버튼 색 상태(평면): normal = 지정색, 호버 = 살짝 밝게, 누름 = 어둡게, 비활성 = stone.</summary>
    public static void ButtonColors(Button b, Color normal)
    {
        if (b == null) return;
        // 상태 틴트(ColorBlock)는 Graphic.color 에 곱해진다 → Graphic 은 흰색, 색은 전부 ColorBlock 이 정한다.
        if (b.targetGraphic != null) b.targetGraphic.color = Color.white;
        var cb = b.colors;
        cb.colorMultiplier = 1f;
        cb.normalColor = normal;
        cb.highlightedColor = Color.Lerp(normal, Color.white, 0.15f);
        cb.selectedColor = normal;
        cb.pressedColor = Color.Lerp(normal, Ink, 0.25f);
        cb.disabledColor = Stone;
        b.colors = cb;
    }

    /// <summary>
    /// 한글 어절 단위 줄바꿈(TMP "Modern Hangul Line Breaking Rules"). TMP Settings 에셋에 저장하는 건
    /// 에디터 메뉴(PitCrew/UI/Build Fonts (D-121))의 몫이고, 이건 메뉴를 안 돌린 빌드의 런타임 안전망.
    /// </summary>
    public static void EnsureKoreanWordWrap()
    {
        if (s_wrapChecked) return;
        s_wrapChecked = true;
        var s = TMP_Settings.instance;
        if (s != null && !TMP_Settings.useModernHangulLineBreakingRules)
            TMP_Settings.useModernHangulLineBreakingRules = true;
    }
    static bool s_wrapChecked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitOnLoad() { s_wrapChecked = false; EnsureKoreanWordWrap(); }
}
