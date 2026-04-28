using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 기어 HUD (UGUI). 세션 13 Kenney panelDark sprite 적용.
/// 시프트 순간 화이트 플래시(textColor), bong SFX.
/// </summary>
[DisallowMultipleComponent]
public class GearHUD : MonoBehaviour
{
    [SerializeField] GearSystem gears;

    [Header("Layout (우하단 기준)")]
    // 도현 #2-2: UI 너무 작음. 140x140 → 200x200.
    [SerializeField] Vector2 anchoredPos = new Vector2(-40, 100);
    [SerializeField] Vector2 size = new Vector2(200, 200);

    [Header("Flash")]
    [SerializeField] float flashDurationSec = 0.15f;

    RectTransform rt;
    Image bg;
    TMP_Text title, gearText;
    float flashT;

    void Awake()
    {
        if (gears == null) gears = FindFirstObjectByType<GearSystem>();
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        BuildIfNeeded();
    }

    void OnEnable() { if (gears != null) gears.OnShift += OnShift; }
    void OnDisable() { if (gears != null) gears.OnShift -= OnShift; }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[GearHUD] Canvas 밖.");
        enabled = false;
    }

    void OnShift(int prev, int cur) { flashT = flashDurationSec; UISounds.Shift(); }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;
        rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(1, 0);
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

        if (title == null)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            title = go.AddComponent<TextMeshProUGUI>();
            title.raycastTarget = false;
            title.text = "GEAR";
            title.alignment = TextAlignmentOptions.Top;
            title.fontSize = 26; title.fontStyle = FontStyles.UpperCase;
            if (theme != null) { if (theme.fontCondensed != null) title.font = theme.fontCondensed; title.color = theme.colorTextMuted; }
            var r = title.rectTransform;
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0.5f, 1f);
            r.offsetMin = new Vector2(0, -36); r.offsetMax = new Vector2(0, -12);
        }

        if (gearText == null)
        {
            var go = new GameObject("Gear", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            gearText = go.AddComponent<TextMeshProUGUI>();
            gearText.raycastTarget = false;
            gearText.alignment = TextAlignmentOptions.Center;
            gearText.fontSize = 130; gearText.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) gearText.font = theme.fontPrimary; gearText.color = theme.colorText; }
            var r = gearText.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(0, 0); r.offsetMax = new Vector2(0, -28);
        }
    }

    void Update()
    {
        if (gears == null) return;
        if (flashT > 0f) flashT -= Time.unscaledDeltaTime;

        var theme = UITheme.Active;
        if (theme != null)
        {
            if (bg != null) bg.color = theme.colorPanel;
            if (title != null) title.color = theme.colorTextMuted;
        }
        float f = flashDurationSec > 0 ? Mathf.Clamp01(flashT / flashDurationSec) : 0f;
        Color baseCol = theme != null ? theme.colorText : Color.black;
        Color flashCol = theme != null ? theme.colorAccent : Color.yellow;
        gearText.color = Color.Lerp(baseCol, flashCol, f);
        gearText.text = gears.GearLabel;
    }
}
