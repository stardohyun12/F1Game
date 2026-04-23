using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 기어 HUD (UGUI). 우하단 큰 박스. 시프트 순간 0.15s 화이트 플래시 (GDD gears.md K5).
/// 세션 12 UI 트랙에서 OnGUI → UGUI 전환.
/// </summary>
[DisallowMultipleComponent]
public class GearHUD : MonoBehaviour
{
    [SerializeField] GearSystem gears;

    [Header("Layout (우하단 기준)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(-36, 80);
    [SerializeField] Vector2 size = new Vector2(140, 140);

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

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning($"[GearHUD] Canvas 밖에 있음. GameObject를 UIRoot 직속으로 이동하거나 Canvas 하위에 두세요.");
        enabled = false;
    }

    void OnEnable()
    {
        if (gears != null) gears.OnShift += OnShift;
    }

    void OnDisable()
    {
        if (gears != null) gears.OnShift -= OnShift;
    }

    void OnShift(int prev, int cur)
    {
        flashT = flashDurationSec;
        UISounds.Shift();
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;

        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (bg == null)
        {
            var bgGo = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(transform, false);
            bg = bgGo.GetComponent<Image>();
            bg.raycastTarget = false;
            bg.color = theme != null ? theme.colorPanel : new Color(0, 0, 0, 0.55f);
            if (theme != null && theme.panelBackground != null)
            {
                bg.sprite = theme.panelBackground;
                bg.type = Image.Type.Sliced;
            }
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        }

        if (title == null)
        {
            var t = new GameObject("Title", typeof(RectTransform));
            t.transform.SetParent(transform, false);
            title = t.AddComponent<TextMeshProUGUI>();
            title.raycastTarget = false;
            title.text = "GEAR";
            title.alignment = TextAlignmentOptions.Top;
            title.fontSize = 18;
            title.fontStyle = FontStyles.UpperCase;
            if (theme != null)
            {
                if (theme.fontCondensed != null) title.font = theme.fontCondensed;
                title.color = theme.colorTextMuted;
            }
            var tRt = title.rectTransform;
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.offsetMin = new Vector2(0, -32);
            tRt.offsetMax = new Vector2(0, -8);
        }

        if (gearText == null)
        {
            var g = new GameObject("Gear", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            gearText = g.AddComponent<TextMeshProUGUI>();
            gearText.raycastTarget = false;
            gearText.alignment = TextAlignmentOptions.Center;
            gearText.fontSize = 96;
            gearText.fontStyle = FontStyles.Bold;
            if (theme != null)
            {
                if (theme.fontPrimary != null) gearText.font = theme.fontPrimary;
                gearText.color = theme.colorText;
            }
            var gRt = gearText.rectTransform;
            gRt.anchorMin = Vector2.zero; gRt.anchorMax = Vector2.one;
            gRt.offsetMin = new Vector2(0, 0);
            gRt.offsetMax = new Vector2(0, -24);
        }
    }

    void Update()
    {
        if (gears == null) return;
        if (flashT > 0f) flashT -= Time.unscaledDeltaTime;

        var theme = UITheme.Active;
        float flashAmt = flashDurationSec > 0 ? Mathf.Clamp01(flashT / flashDurationSec) : 0f;
        Color baseCol = theme != null ? theme.colorText : new Color(0.95f, 0.95f, 0.95f);
        Color flashCol = theme != null ? theme.colorTextFlash : Color.white;
        gearText.color = Color.Lerp(baseCol, flashCol, flashAmt);

        gearText.text = gears.GearLabel;
    }
}
