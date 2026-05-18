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
    [SerializeField] Vector2 size = new Vector2(520, 80);

    RectTransform rt;
    Image bg;
    TMP_Text countText, labelText;

    // Phase 6 perf(B3): 매 프레임 string interpolation 회피. 상태/인덱스/타겟 변경 시만 갱신.
    enum DisplayState { None, Finished, Returning, Normal }
    DisplayState lastState = DisplayState.None;
    int lastIdx = -1, lastTotal = -1;
    string lastTargetLabel = null;

    CanvasGroup group;

    void Awake()
    {
        if (manager == null) manager = Services.Delivery as DeliveryManager;
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
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
            bg.color = theme != null ? theme.colorPanel : new Color(0.12f, 0.14f, 0.18f, 0.92f);
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
            labelText.fontSize = 24;
            labelText.textWrappingMode = TextWrappingModes.NoWrap; // 한 줄 강제
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            if (theme != null) { if (theme.fontCondensed != null) labelText.font = theme.fontCondensed; labelText.color = theme.colorText; }
            var r = labelText.rectTransform;
            r.anchorMin = new Vector2(0.3f, 0); r.anchorMax = new Vector2(1, 1);
            r.offsetMin = new Vector2(0, 0); r.offsetMax = new Vector2(-20, 0);
        }
    }

    void Update()
    {
        if (manager == null) return;
        // 운전 단계에만 표시.
        var rc = Services.Run as RunController;
        bool driving = rc != null && rc.CurrentStage == RunStage.Driving;
        if (group != null) { group.alpha = driving ? 1f : 0f; group.blocksRaycasts = driving; }
        if (!driving) return;
        var theme = UITheme.Active;
        if (theme != null)
        {
            if (bg != null) bg.color = theme.colorPanel;
            if (labelText != null) labelText.color = theme.colorText;
        }

        if (manager.Finished)
        {
            if (lastState != DisplayState.Finished)
            {
                lastState = DisplayState.Finished;
                countText.text = "✓";
                labelText.text = "COMPLETE";
            }
            countText.color = theme != null ? theme.colorOkText : Color.green;
        }
        else if (manager.Returning)
        {
            int total = manager.TotalCount;
            if (lastState != DisplayState.Returning || lastTotal != total)
            {
                lastState = DisplayState.Returning;
                lastTotal = total;
                countText.text = $"{total}/{total}";
                labelText.text = "RETURN TO START";
            }
            countText.color = theme != null ? theme.colorWarnText : Color.yellow;
        }
        else
        {
            int idx = manager.CurrentIndex;
            int total = manager.TotalCount;
            var target = manager.CurrentTarget;
            string targetLabel = target != null ? target.Label : "...";
            if (lastState != DisplayState.Normal || lastIdx != idx || lastTotal != total)
            {
                lastState = DisplayState.Normal;
                lastIdx = idx; lastTotal = total;
                countText.text = $"{idx + 1}/{total}";
            }
            if (!ReferenceEquals(lastTargetLabel, targetLabel))
            {
                lastTargetLabel = targetLabel;
                labelText.text = targetLabel;
            }
            countText.color = theme != null ? theme.colorAccent : Color.yellow;
        }
    }
}
