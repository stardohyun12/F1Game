using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 배달 진행률 HUD. 세션 13 Kenney panelDark sprite.
/// "N/M + 목적지명" 형식. Returning/Finished 상태 표시.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class DeliveryProgressHUD : MonoBehaviour
{
    [SerializeField] DeliveryManager manager;

    [Header("Layout (상단 중앙)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(0, -40);
    [SerializeField] Vector2 size = new Vector2(400, 80);

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
        Debug.LogWarning("[DeliveryProgressHUD] Canvas 밖.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;
        rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1); rt.pivot = new Vector2(0.5f, 1);
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

        if (countText == null)
        {
            var g = new GameObject("Count", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            countText = g.AddComponent<TextMeshProUGUI>();
            countText.raycastTarget = false;
            countText.alignment = TextAlignmentOptions.MidlineLeft;
            countText.fontSize = 40; countText.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) countText.font = theme.fontPrimary; countText.color = theme.colorAccent; }
            var r = countText.rectTransform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(0.4f, 1);
            r.offsetMin = new Vector2(24, 0); r.offsetMax = Vector2.zero;
        }

        if (labelText == null)
        {
            var g = new GameObject("Label", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            labelText = g.AddComponent<TextMeshProUGUI>();
            labelText.raycastTarget = false;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.fontSize = 26;
            if (theme != null) { if (theme.fontCondensed != null) labelText.font = theme.fontCondensed; labelText.color = theme.colorText; }
            var r = labelText.rectTransform;
            r.anchorMin = new Vector2(0.4f, 0); r.anchorMax = new Vector2(1, 1);
            r.offsetMin = new Vector2(0, 0); r.offsetMax = new Vector2(-20, 0);
        }
    }

    void Update()
    {
        if (manager == null) return;
        var theme = UITheme.Active;

        if (manager.Finished)
        {
            countText.text = "✓";
            countText.color = theme != null ? theme.colorOkText : Color.green;
            labelText.text = "COMPLETE";
        }
        else if (manager.Returning)
        {
            countText.text = $"{manager.TotalCount}/{manager.TotalCount}";
            countText.color = theme != null ? theme.colorWarnText : Color.yellow;
            labelText.text = "RETURN TO START";
        }
        else
        {
            countText.text = $"{manager.CurrentIndex + 1}/{manager.TotalCount}";
            countText.color = theme != null ? theme.colorAccent : Color.yellow;
            var target = manager.CurrentTarget;
            labelText.text = target != null ? target.Label : "...";
        }
    }
}
