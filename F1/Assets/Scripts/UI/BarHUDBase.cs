using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Phase 2 — Template Method 패턴.
// 학습: GDD/architecture/phase2-template-method.md
//
// 가로 막대 HUD (BG + Fill + Label) 공통 골격.
// 자식은 OnAcquire / OnLevel / OnLabel만 override.
// FuelHUD/IntegrityHUD가 사용. 다른 텍스트 전용 HUD는 적용 X.

[DisallowMultipleComponent]
public abstract class BarHUDBase : MonoBehaviour
{
    [Header("Layout (좌하단 기준)")]
    [SerializeField] protected Vector2 anchoredPos = new Vector2(40, 100);
    [SerializeField] protected Vector2 size = new Vector2(560, 80);

    [Header("Thresholds (0~1)")]
    [SerializeField] protected float warnThreshold = 0.25f;
    [SerializeField] protected float critThreshold = 0.10f;
    [SerializeField] protected float critBlinkHz = 4f;

    protected RectTransform rt;
    protected Image bg, fillImg;
    protected RectTransform fillRt;
    protected TMP_Text label;
    protected CanvasGroup group;

    // Template Method — 골격 (수정 금지). 변하는 부분만 자식이 hook으로 끼움.
    void Awake()
    {
        OnAcquireDependency();
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
        BuildIfNeeded();
        OnPostAwake();
    }

    void Update()
    {
        if (!HasDependency())
        {
            OnAcquireDependency();
            if (!HasDependency()) return;
        }
        // 표시 조건 (자식이 override) — 런 중에만 보이는 HUD 등.
        bool show = ShouldShow();
        if (group != null) group.alpha = show ? 1f : 0f;
        if (!show) return;
        // D-121: 법랑 표지판 — 종이 판 + 잉크 글자. (IntegrityHUD 의 충격 플래시는 OnApplyEffects 가 덮어씀)
        if (bg != null) bg.color = UIPalette.Paper;
        if (label != null) label.color = UIPalette.Ink;
        OnApplyEffects();   // BG color override 가능 (impact flash 등)

        float level = Mathf.Clamp01(OnLevel());
        float innerW = rt.rect.width - 16f;
        if (fillRt != null) fillRt.sizeDelta = new Vector2(innerW * level, 0);

        if (fillImg != null)
        {
            // 평면 토큰색: 정상 = 올리브, 경고 = 레몬, 위험 = 신호 빨강(깜박).
            Color fc = level <= critThreshold ? UIPalette.Signal
                     : level <= warnThreshold ? UIPalette.Lemon
                                              : UIPalette.Olive;
            if (level <= critThreshold && level > 0f)
            {
                float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * critBlinkHz * Mathf.PI * 2f);
                fc.a = 0.55f + 0.45f * t;
            }
            fillImg.color = fc;
        }

        if (label != null) label.text = OnLabel(level);
    }

    // ===== 자식이 override =====

    /// <summary>Services에서 의존 주입.</summary>
    protected abstract void OnAcquireDependency();

    /// <summary>의존 객체가 살아있나?</summary>
    protected abstract bool HasDependency();

    /// <summary>0~1 사이 채움 비율.</summary>
    protected abstract float OnLevel();

    /// <summary>라벨 문자열.</summary>
    protected abstract string OnLabel(float level);

    /// <summary>Awake 마지막 hook (이벤트 구독 등). 기본 no-op.</summary>
    protected virtual void OnPostAwake() { }

    /// <summary>HUD 표시 조건. false면 CanvasGroup alpha 0 + Update 본문 skip. 기본: Driving 단계일 때만.</summary>
    protected virtual bool ShouldShow()
    {
        var rc = Services.Run as RunController;
        return rc != null && rc.CurrentStage == RunStage.Driving;
    }

    /// <summary>Update에서 BG 색상 등 추가 효과. 기본 no-op.</summary>
    protected virtual void OnApplyEffects() { }

    // ===== 공통 골격 (자식 수정 X) =====

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning($"[{GetType().Name}] Canvas 밖.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (bg == null)
        {
            var go = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            bg = go.GetComponent<Image>();
            bg.raycastTarget = false;
            UIPalette.PaperPanel(bg, slot: UISlot.Plate);
            var r = bg.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        if (fillImg == null)
        {
            var go = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            fillImg = go.GetComponent<Image>();
            fillImg.raycastTarget = false;
            fillImg.sprite = null;                 // D-121: Kenney 글로스 대신 평면 토큰색(슬롯 있으면 스프라이트)
            UIPalette.Tint(fillImg, UISlot.Gauge, UIPalette.Olive);
            fillRt = fillImg.rectTransform;
            fillRt.anchorMin = new Vector2(0, 0); fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.offsetMin = new Vector2(8, 8); fillRt.offsetMax = new Vector2(8, -8);
        }

        if (label == null)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            label = go.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 40; label.fontStyle = FontStyles.Bold;
            UIPalette.Body(label, UIPalette.Ink);
            var r = label.rectTransform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 1);
            r.offsetMin = new Vector2(20, 0); r.offsetMax = new Vector2(-20, 0);
        }
    }
}
