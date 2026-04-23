using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 카운트다운 HUD (UGUI). 세션 13 Kenney sprite 방식.
/// 30s 미만: fillCrit(빨강) BG, 60s 미만: fillWarn(노랑), 그 외: panelDark.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class CountdownHUD : MonoBehaviour
{
    [SerializeField] RunController controller;

    [Header("Layout (우상단 기준)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(-40, -40);
    [SerializeField] Vector2 size = new Vector2(240, 80);

    RectTransform rt;
    Image bg;
    TMP_Text timeText;
    CanvasGroup group;

    void Awake()
    {
        if (controller == null) controller = FindFirstObjectByType<RunController>();
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
        BuildIfNeeded();
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[CountdownHUD] Canvas 밖.");
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
            bg.color = Color.white;
            if (theme?.panelDark != null) { bg.sprite = theme.panelDark; bg.type = Image.Type.Sliced; }
            var r = bg.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        if (timeText == null)
        {
            var go = new GameObject("Time", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            timeText = go.AddComponent<TextMeshProUGUI>();
            timeText.raycastTarget = false;
            timeText.alignment = TextAlignmentOptions.Center;
            timeText.fontSize = 44; timeText.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) timeText.font = theme.fontPrimary; timeText.color = theme.colorText; }
            var r = timeText.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }
    }

    void Update()
    {
        if (group == null) return;
        if (controller == null || !controller.RunActive) { group.alpha = 0f; return; }
        float remaining = controller.TimeRemaining;
        if (remaining < 0f) { group.alpha = 0f; return; }
        group.alpha = 1f;

        var theme = UITheme.Active;
        if (theme != null)
        {
            var panelSprite = theme.PanelForTimer(remaining);
            if (panelSprite != null && bg.sprite != panelSprite) bg.sprite = panelSprite;
            // 30s 미만 빨강/60s 미만 노랑 fill 위는 흰 텍스트, 그 외 Grey bg 위는 검정
            if (timeText != null)
                timeText.color = remaining < 60f ? theme.colorTextOnFill : theme.colorText;
        }

        int m = Mathf.FloorToInt(remaining / 60f);
        int s = Mathf.FloorToInt(remaining % 60f);
        timeText.text = $"{m}:{s:D2}";
    }
}
