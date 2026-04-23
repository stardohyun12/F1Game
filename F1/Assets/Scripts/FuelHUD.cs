using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 연료 게이지 HUD (UGUI). 세션 13 Kenney sprite 스왑 방식.
/// - BG: panelDark, Fill: Green/Yellow/Red Kenney sprite 상태별 교체
/// - Image.color = white (tint 없음, sprite 원색 사용)
/// - 10% crit는 깜박 blink
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
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[FuelHUD] Canvas 밖에 있음.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (bg == null)
        {
            var go = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            bg = go.GetComponent<Image>();
            bg.raycastTarget = false;
            bg.color = theme != null ? theme.colorPanel : new Color(0.12f, 0.14f, 0.18f, 0.92f);
            if (theme?.panelDark != null) { bg.sprite = theme.panelDark; bg.type = Image.Type.Sliced; }
            var r = bg.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        if (fillImg == null)
        {
            var go = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            fillImg = go.GetComponent<Image>();
            fillImg.raycastTarget = false;
            fillImg.color = Color.white;
            if (theme?.fillOk != null) { fillImg.sprite = theme.fillOk; fillImg.type = Image.Type.Sliced; }
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
            label.fontSize = 28; label.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) label.font = theme.fontPrimary; label.color = theme.colorText; }
            var r = label.rectTransform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 1);
            r.offsetMin = new Vector2(20, 0); r.offsetMax = new Vector2(-20, 0);
        }
    }

    void Update()
    {
        if (fuel == null) return;
        var theme = UITheme.Active;

        // Theme 변경 즉시 반영
        if (theme != null)
        {
            if (bg != null) bg.color = theme.colorPanel;
            if (label != null) label.color = theme.colorTextOnFill;
        }

        float level = Mathf.Clamp01(fuel.FuelLevel);
        float innerW = rt.rect.width - 16f;
        fillRt.sizeDelta = new Vector2(innerW * level, 0);

        // Sprite 스왑 방식
        if (theme != null)
        {
            var s = theme.FillForLevel(level, warnThreshold, critThreshold);
            if (s != null && fillImg.sprite != s) fillImg.sprite = s;

            // crit 깜박: alpha로만 조절 (sprite는 Red 고정)
            if (level <= critThreshold)
            {
                float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * critBlinkHz * Mathf.PI * 2f);
                fillImg.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * t);
            }
            else
            {
                fillImg.color = Color.white;
            }
        }

        label.text = fuel.IsDepleted ? "FUEL EMPTY  (F12)" : $"FUEL  {Mathf.RoundToInt(level * 100)}%";
    }
}
