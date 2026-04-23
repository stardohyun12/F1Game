using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 배달 진행률 HUD. 상단 중앙. "2/5   FactoryEast" 형식.
/// 복귀 중일 때는 "RETURN" 표시, 완료 시 "COMPLETE".
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class DeliveryProgressHUD : MonoBehaviour
{
    [SerializeField] DeliveryManager manager;

    [Header("Layout (상단 중앙 기준)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(0, -40);
    [SerializeField] Vector2 size = new Vector2(360, 72);

    RectTransform rt;
    Image bg;
    TMP_Text countText, labelText;

    void Awake()
    {
        if (manager == null) manager = FindFirstObjectByType<DeliveryManager>();
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        BuildIfNeeded();
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[DeliveryProgressHUD] Canvas 밖. UIRoot 직속으로.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
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

        if (countText == null)
        {
            var g = new GameObject("Count", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            countText = g.AddComponent<TextMeshProUGUI>();
            countText.raycastTarget = false;
            countText.alignment = TextAlignmentOptions.MidlineLeft;
            countText.fontSize = 36;
            countText.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) countText.font = theme.fontPrimary; countText.color = theme.colorAccent; }
            var cRt = countText.rectTransform;
            cRt.anchorMin = new Vector2(0, 0); cRt.anchorMax = new Vector2(0.4f, 1);
            cRt.offsetMin = new Vector2(20, 0); cRt.offsetMax = Vector2.zero;
        }

        if (labelText == null)
        {
            var g = new GameObject("Label", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            labelText = g.AddComponent<TextMeshProUGUI>();
            labelText.raycastTarget = false;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.fontSize = 22;
            if (theme != null) { if (theme.fontCondensed != null) labelText.font = theme.fontCondensed; labelText.color = theme.colorText; }
            var lRt = labelText.rectTransform;
            lRt.anchorMin = new Vector2(0.4f, 0); lRt.anchorMax = new Vector2(1, 1);
            lRt.offsetMin = new Vector2(0, 0); lRt.offsetMax = new Vector2(-20, 0);
        }
    }

    void Update()
    {
        if (manager == null) return;
        var theme = UITheme.Active;

        if (manager.Finished)
        {
            countText.text = "✓";
            countText.color = theme != null ? theme.colorOk : new Color(0.4f, 0.85f, 0.45f);
            labelText.text = "COMPLETE";
        }
        else if (manager.Returning)
        {
            countText.text = $"{manager.TotalCount}/{manager.TotalCount}";
            countText.color = theme != null ? theme.colorWarn : new Color(1f, 0.8f, 0.3f);
            labelText.text = "RETURN TO START";
        }
        else
        {
            countText.text = $"{manager.CurrentIndex + 1}/{manager.TotalCount}";
            countText.color = theme != null ? theme.colorAccent : new Color(1f, 0.85f, 0.28f);
            var target = manager.CurrentTarget;
            labelText.text = target != null ? target.Label : "...";
        }
    }
}
