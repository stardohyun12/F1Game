using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 시간제한 카운트다운 HUD (UGUI). 우상단. 시간제한 없는 작업이면 자동 숨김.
/// 세션 12 UI 트랙 — OnGUI → UGUI 전환.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class CountdownHUD : MonoBehaviour
{
    [SerializeField] RunController controller;

    [Header("Layout (우상단 기준)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(-40, -40);
    [SerializeField] Vector2 size = new Vector2(240, 72);

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
        // Unity의 ?? 연산자 + UnityEngine.Object fake-null 이슈 회피. TryGetComponent는 안전.
        if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
        BuildIfNeeded();
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning($"[CountdownHUD] Canvas 밖에 있음. GameObject를 UIRoot 직속으로 이동하세요.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;

        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (bg == null)
        {
            var bgGo = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(transform, false);
            bg = bgGo.GetComponent<Image>();
            bg.raycastTarget = false;
            bg.color = theme != null ? theme.colorTimerNormal : new Color(0, 0, 0, 0.55f);
            if (theme != null && theme.panelBackground != null)
            {
                bg.sprite = theme.panelBackground;
                bg.type = Image.Type.Sliced;
            }
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        }

        if (timeText == null)
        {
            var t = new GameObject("Time", typeof(RectTransform));
            t.transform.SetParent(transform, false);
            timeText = t.AddComponent<TextMeshProUGUI>();
            timeText.raycastTarget = false;
            timeText.alignment = TextAlignmentOptions.Center;
            timeText.fontSize = 40;
            timeText.fontStyle = FontStyles.Bold;
            if (theme != null)
            {
                if (theme.fontPrimary != null) timeText.font = theme.fontPrimary;
                timeText.color = theme.colorText;
            }
            var tRt = timeText.rectTransform;
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        }
    }

    void Update()
    {
        if (group == null) return; // Awake가 enabled=false로 끝난 이례 상황 방어
        if (controller == null || !controller.RunActive) { group.alpha = 0f; return; }

        float remaining = controller.TimeRemaining;
        if (remaining < 0f) { group.alpha = 0f; return; }

        group.alpha = 1f;
        var theme = UITheme.Active;
        Color bgCol;
        if (remaining < 30f) bgCol = theme != null ? theme.colorTimerLow : new Color(0.7f, 0.15f, 0.15f, 0.85f);
        else if (remaining < 60f) bgCol = theme != null ? theme.colorTimerMid : new Color(0.8f, 0.6f, 0.1f, 0.7f);
        else bgCol = theme != null ? theme.colorTimerNormal : new Color(0, 0, 0, 0.55f);
        bg.color = bgCol;

        int m = Mathf.FloorToInt(remaining / 60f);
        int s = Mathf.FloorToInt(remaining % 60f);
        timeText.text = $"{m}:{s:D2}";
    }
}
