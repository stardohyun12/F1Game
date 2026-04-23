using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 속도계 HUD. 좌하단 연료 게이지 위. km/h 큰 숫자 + 작은 단위 라벨.
/// 속도 구간별 색: 저속(text) / 중속(text) / 고속(accent).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SpeedometerHUD : MonoBehaviour
{
    [SerializeField] ArcadeCarController car;

    [Header("Layout (좌하단 기준)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(36, 150);
    [SerializeField] Vector2 size = new Vector2(200, 80);

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
        Debug.LogWarning("[SpeedometerHUD] Canvas 밖에 있음. UIRoot 직속으로 이동하세요.");
        enabled = false;
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
            if (theme != null && theme.panelBackground != null) { bg.sprite = theme.panelBackground; bg.type = Image.Type.Sliced; }
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        }

        if (speedText == null)
        {
            var g = new GameObject("Speed", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            speedText = g.AddComponent<TextMeshProUGUI>();
            speedText.raycastTarget = false;
            speedText.alignment = TextAlignmentOptions.Right;
            speedText.fontSize = 56;
            speedText.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) speedText.font = theme.fontPrimary; speedText.color = theme.colorText; }
            var sRt = speedText.rectTransform;
            sRt.anchorMin = Vector2.zero; sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(8, 4); sRt.offsetMax = new Vector2(-52, -4);
        }

        if (unitText == null)
        {
            var g = new GameObject("Unit", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            unitText = g.AddComponent<TextMeshProUGUI>();
            unitText.raycastTarget = false;
            unitText.text = "KM/H";
            unitText.alignment = TextAlignmentOptions.BottomLeft;
            unitText.fontSize = 16;
            if (theme != null) { if (theme.fontCondensed != null) unitText.font = theme.fontCondensed; unitText.color = theme.colorTextMuted; }
            var uRt = unitText.rectTransform;
            uRt.anchorMin = new Vector2(1, 0); uRt.anchorMax = new Vector2(1, 0);
            uRt.pivot = new Vector2(1, 0);
            uRt.sizeDelta = new Vector2(48, 30);
            uRt.anchoredPosition = new Vector2(-8, 10);
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
        if (norm >= highSpeedNorm) col = theme != null ? theme.colorAccent : new Color(1f, 0.85f, 0.28f);
        else if (norm >= midSpeedNorm) col = theme != null ? theme.colorText : Color.white;
        else col = theme != null ? theme.colorTextMuted : new Color(0.7f, 0.7f, 0.7f);
        speedText.color = col;
    }
}
