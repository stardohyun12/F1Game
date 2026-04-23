using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 속도계 HUD. 세션 13 Kenney panelDark sprite.
/// 속도 구간별 텍스트 색: muted / text / accent.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SpeedometerHUD : MonoBehaviour
{
    [SerializeField] ArcadeCarController car;

    [Header("Layout (좌하단 기준)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(36, 150);
    [SerializeField] Vector2 size = new Vector2(220, 88);

    [Header("Thresholds (0~1 of maxSpeed)")]
    [SerializeField] float midSpeedNorm = 0.5f;
    [SerializeField] float highSpeedNorm = 0.85f;

    RectTransform rt;
    Image bg;
    TMP_Text speedText, unitText;

    void Awake()
    {
        if (car == null) car = FindFirstObjectByType<ArcadeCarController>();
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        BuildIfNeeded();
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[SpeedometerHUD] Canvas 밖.");
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
            bg.color = Color.white;
            if (theme?.panelDark != null) { bg.sprite = theme.panelDark; bg.type = Image.Type.Sliced; }
            var r = bg.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        if (speedText == null)
        {
            var g = new GameObject("Speed", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            speedText = g.AddComponent<TextMeshProUGUI>();
            speedText.raycastTarget = false;
            speedText.alignment = TextAlignmentOptions.Right;
            speedText.fontSize = 60; speedText.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) speedText.font = theme.fontPrimary; speedText.color = theme.colorText; }
            var r = speedText.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(12, 8); r.offsetMax = new Vector2(-60, -8);
        }

        if (unitText == null)
        {
            var g = new GameObject("Unit", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            unitText = g.AddComponent<TextMeshProUGUI>();
            unitText.raycastTarget = false;
            unitText.text = "KM/H";
            unitText.alignment = TextAlignmentOptions.BottomRight;
            unitText.fontSize = 18;
            if (theme != null) { if (theme.fontCondensed != null) unitText.font = theme.fontCondensed; unitText.color = theme.colorTextMuted; }
            var r = unitText.rectTransform;
            r.anchorMin = new Vector2(1, 0); r.anchorMax = new Vector2(1, 0);
            r.pivot = new Vector2(1, 0);
            r.sizeDelta = new Vector2(54, 40);
            r.anchoredPosition = new Vector2(-14, 14);
        }
    }

    void Update()
    {
        if (car == null) return;
        var theme = UITheme.Active;
        float mps = Mathf.Abs(car.CurrentSpeedMps);
        int kmh = Mathf.RoundToInt(mps * 3.6f);
        speedText.text = kmh.ToString();

        float norm = car.MaxSpeedMps > 0 ? mps / car.MaxSpeedMps : 0f;
        Color col;
        if (theme == null) col = Color.white;
        else if (norm >= highSpeedNorm) col = theme.colorAccent;
        else if (norm >= midSpeedNorm) col = theme.colorText;
        else col = theme.colorTextMuted;
        speedText.color = col;
    }
}
