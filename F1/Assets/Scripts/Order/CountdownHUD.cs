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
    int lastWholeSecond = int.MinValue; // Phase 6 perf(B2): 초가 바뀔 때만 string alloc.

    void Awake()
    {
        if (controller == null) controller = Services.Run as RunController;
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
            bg.color = theme != null ? theme.colorPanel : new Color(0.12f, 0.14f, 0.18f, 0.92f);
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
        // 운전 단계에만 표시 (시간 제한 있는 경우).
        // D-118: 긴급 전화 수락 뒤엔 포장 단계에서도 보인다.
        bool show = controller != null && (controller.CurrentStage == RunStage.Driving
            || (controller.ClockRunning && (controller.CurrentStage == RunStage.Board
                || controller.CurrentStage == RunStage.Routing || controller.CurrentStage == RunStage.Packing)));
        if (!show) { group.alpha = 0f; return; }
        float remaining = controller.TimeRemaining;
        if (remaining < 0f) { group.alpha = 0f; return; }
        group.alpha = 1f;

        var theme = UITheme.Active;
        if (theme != null)
        {
            var panelSprite = theme.PanelForTimer(remaining);
            if (panelSprite != null && bg.sprite != panelSprite) bg.sprite = panelSprite;
            // panelDark 시 어두운 tint, fill sprite(Red/Yellow) 시 원색
            bg.color = (panelSprite == theme.panelDark) ? theme.colorPanel : Color.white;
            if (timeText != null) timeText.color = theme.colorText;
        }

        int totalSec = Mathf.Max(0, Mathf.FloorToInt(remaining));
        if (totalSec != lastWholeSecond)
        {
            lastWholeSecond = totalSec;
            timeText.text = $"{totalSec / 60}:{totalSec % 60:D2}";
        }
    }
}
