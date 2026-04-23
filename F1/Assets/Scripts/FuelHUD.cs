using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 연료 게이지 HUD (UGUI). 이전 OnGUI 버전 대체 (세션 12, UI 트랙).
/// - 좌하단 가로 바 + 라벨
/// - 25% warn, 10% crit 블링크
/// - 자가구성: Canvas 하위에 GameObject만 두면 Awake에서 자식 Image/TMP 자동 생성
/// </summary>
[DisallowMultipleComponent]
public class FuelHUD : MonoBehaviour
{
    [SerializeField] FuelSystem fuel;

    [Header("Layout (좌하단 기준)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(36, 80);
    [SerializeField] Vector2 size = new Vector2(420, 56);

    [Header("Thresholds (0~1)")]
    [SerializeField] float warnThreshold = 0.25f;
    [SerializeField] float critThreshold = 0.10f;
    [SerializeField] float critBlinkHz = 4f;

    RectTransform rt;
    Image bg, fillImg;
    RectTransform fillRt;
    TMP_Text label;

    void Awake()
    {
        if (fuel == null) fuel = FindFirstObjectByType<FuelSystem>();
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        BuildIfNeeded();
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() == null)
            transform.SetParent(UIRoot.Ensure(), false);
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;

        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (bg == null)
        {
            var bgGo = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(transform, false);
            bg = bgGo.GetComponent<Image>();
            bg.raycastTarget = false;
            bg.color = theme != null ? theme.colorPanel : new Color(0, 0, 0, 0.55f);
            if (theme != null && theme.panelBackground != null)
            {
                bg.sprite = theme.panelBackground;
                bg.type = Image.Type.Sliced;
            }
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
        }

        if (fillImg == null)
        {
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(transform, false);
            fillImg = fillGo.GetComponent<Image>();
            fillImg.raycastTarget = false;
            if (theme != null && theme.barFill != null)
            {
                fillImg.sprite = theme.barFill;
                fillImg.type = Image.Type.Sliced;
            }
            fillRt = fillImg.rectTransform;
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.offsetMin = new Vector2(4, 4);
            fillRt.offsetMax = new Vector2(4, -4);
        }

        if (label == null)
        {
            var lblGo = new GameObject("Label", typeof(RectTransform));
            lblGo.transform.SetParent(transform, false);
            label = lblGo.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 28;
            label.fontStyle = FontStyles.Bold;
            if (theme != null)
            {
                if (theme.fontPrimary != null) label.font = theme.fontPrimary;
                label.color = theme.colorText;
            }
            var lRt = label.rectTransform;
            lRt.anchorMin = new Vector2(0, 0);
            lRt.anchorMax = new Vector2(1, 1);
            lRt.offsetMin = new Vector2(16, 0);
            lRt.offsetMax = new Vector2(-16, 0);
        }
    }

    void Update()
    {
        if (fuel == null) return;
        var theme = UITheme.Active;

        float level = Mathf.Clamp01(fuel.FuelLevel);

        float innerW = rt.rect.width - 8f;
        fillRt.sizeDelta = new Vector2(innerW * level, 0);

        Color fill;
        if (level <= critThreshold)
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * critBlinkHz * Mathf.PI * 2f);
            var a = theme != null ? theme.colorCrit : new Color(1f, 0.3f, 0.3f, 1f);
            var b = theme != null ? theme.colorCritPulse : new Color(1f, 0.6f, 0.6f, 1f);
            fill = Color.Lerp(a, b, t);
        }
        else if (level <= warnThreshold) fill = theme != null ? theme.colorWarn : new Color(1f, 0.85f, 0.3f, 0.95f);
        else fill = theme != null ? theme.colorOk : new Color(0.4f, 0.9f, 0.4f, 0.9f);
        fillImg.color = fill;

        label.text = fuel.IsDepleted
            ? "FUEL EMPTY  (F12 refill)"
            : $"FUEL  {Mathf.RoundToInt(level * 100)}%";
    }
}
