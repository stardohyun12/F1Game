using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 트렁크 CCTV HUD. SimCam의 RenderTexture를 RawImage로 표시.
/// 자가구성 (FuelHUD 패턴). UIRoot 아래 자동 재부착.
/// 우측 상단 DeliveryProgressHUD 아래 stack.
/// </summary>
[DisallowMultipleComponent]
public class CargoView : MonoBehaviour
{
    [SerializeField] RenderTexture renderTexture;

    [Header("Layout (우측 상단 앵커)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(-36, -220);
    [SerializeField] Vector2 size = new Vector2(280, 200);

    RectTransform rt;
    Image bg;
    RawImage view;
    TMP_Text label;
    CanvasGroup group;

    void Awake()
    {
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
        TryLoadRT();
        BuildIfNeeded();
    }

    void TryLoadRT()
    {
        if (renderTexture == null)
            renderTexture = Resources.Load<RenderTexture>("CargoRT");
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[CargoView] Canvas 밖에 있음.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;
        rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1);
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

        if (view == null)
        {
            var go = new GameObject("View", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(transform, false);
            view = go.GetComponent<RawImage>();
            view.raycastTarget = false;
            if (renderTexture != null) view.texture = renderTexture;
            var r = view.rectTransform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 1);
            r.offsetMin = new Vector2(10, 10); r.offsetMax = new Vector2(-10, -36);
        }

        if (label == null)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            label = go.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Top;
            label.fontSize = 20; label.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) label.font = theme.fontPrimary; label.color = theme.colorTextMuted; }
            label.text = "TRUNK CAM";
            var r = label.rectTransform;
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0.5f, 1);
            r.sizeDelta = new Vector2(0, 28);
            r.anchoredPosition = new Vector2(0, -6);
        }
    }

    void Update()
    {
        if (view == null) return;
        if (view.texture == null)
        {
            TryLoadRT();
            if (renderTexture != null) view.texture = renderTexture;
        }
        // 트렁크 CCTV는 런 중에만 표시 (주문/포장/정산 화면에선 숨김).
        if (group != null)
        {
            var run = Services.Run as RunController;
            group.alpha = (run != null && run.RunActive) ? 1f : 0f;
        }
    }
}
