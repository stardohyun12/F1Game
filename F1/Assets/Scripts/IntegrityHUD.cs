using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화물 Integrity HUD (세션 14, D-040). FuelHUD 패턴 복제.
/// 좌하단 FuelHUD 위에 stack. "Resource" zone (리서치 B 섹션 3).
/// - 80/50/20 단계 색 변화 (Green → Yellow → Red).
/// - 20% 이하 critical blink.
/// - 0% (전파) 상태는 텍스트 "CARGO TOTALED".
/// </summary>
[DisallowMultipleComponent]
public class IntegrityHUD : MonoBehaviour
{
    [SerializeField] CargoBox cargo;

    [Header("Layout (좌하단 기준, Fuel 위)")]
    // 도현 #2-2 (세션 16): UI 너무 작음. 스택 재배치.
    [SerializeField] Vector2 anchoredPos = new Vector2(40, 200);
    [SerializeField] Vector2 size = new Vector2(560, 80);

    [Header("Thresholds (0~1)")]
    [Tooltip("이 이하부터 노랑 (80% 구간).")]
    [SerializeField] float warnThreshold = 0.5f;
    [Tooltip("이 이하부터 빨강 + 깜박 (20% 구간).")]
    [SerializeField] float critThreshold = 0.2f;
    [SerializeField] float critBlinkHz = 4f;

    [Header("Impact Flash (D-043, 도현 세션 15)")]
    [Tooltip("충돌 시 HUD 빨강 flash 지속 (s).")]
    [SerializeField] float flashDuration = 0.18f;

    RectTransform rt;
    Image bg, fillImg;
    RectTransform fillRt;
    TMP_Text label;
    float flashEndTime;
    Color baseBgColor;

    void Awake()
    {
        if (cargo == null) cargo = FindFirstObjectByType<CargoBox>();
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        BuildIfNeeded();
        HookCargoEvents();
    }

    void HookCargoEvents()
    {
        if (cargo == null) return;
        cargo.OnImpact -= OnCargoImpact;
        cargo.OnImpact += OnCargoImpact;
    }

    void OnCargoImpact(float speedKmh)
    {
        // 충돌 강도에 비례한 flash 길이 (최소 0.1s).
        float t = Mathf.Clamp01(speedKmh / 40f);
        flashEndTime = Time.unscaledTime + flashDuration * (0.5f + 0.5f * t);
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[IntegrityHUD] Canvas 밖에 있음.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0, 0);
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

        if (fillImg == null)
        {
            var go = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            fillImg = go.GetComponent<Image>();
            fillImg.raycastTarget = false;
            fillImg.color = Color.white;
            if (theme?.fillOk != null) { fillImg.sprite = theme.fillOk; fillImg.type = Image.Type.Sliced; }
            fillRt = fillImg.rectTransform;
            fillRt.anchorMin = new Vector2(0, 0); fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.offsetMin = new Vector2(8, 8); fillRt.offsetMax = new Vector2(8, -8);
        }

        if (label == null)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            label = go.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 40; label.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) label.font = theme.fontPrimary; label.color = theme.colorText; }
            var r = label.rectTransform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 1);
            r.offsetMin = new Vector2(20, 0); r.offsetMax = new Vector2(-20, 0);
        }
    }

    void Update()
    {
        if (cargo == null)
        {
            cargo = FindFirstObjectByType<CargoBox>();
            if (cargo == null) return;
            HookCargoEvents();
        }
        var theme = UITheme.Active;

        if (theme != null)
        {
            if (bg != null) baseBgColor = theme.colorPanel;
            if (label != null) label.color = theme.colorTextOnFill;
        }

        // Impact flash (D-043) — bg 색상 빨강 lerp.
        if (bg != null)
        {
            float remaining = flashEndTime - Time.unscaledTime;
            if (remaining > 0f)
            {
                float t = Mathf.Clamp01(remaining / flashDuration);
                bg.color = Color.Lerp(baseBgColor, new Color(1f, 0.25f, 0.25f, 1f), t);
            }
            else
            {
                bg.color = baseBgColor;
            }
        }

        float level = cargo.IntegrityPercent;
        float innerW = rt.rect.width - 16f;
        fillRt.sizeDelta = new Vector2(innerW * level, 0);

        if (theme != null)
        {
            var s = theme.FillForLevel(level, warnThreshold, critThreshold);
            if (s != null && fillImg.sprite != s) fillImg.sprite = s;

            if (level <= critThreshold && level > 0f)
            {
                float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * critBlinkHz * Mathf.PI * 2f);
                fillImg.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * t);
            }
            else
            {
                fillImg.color = Color.white;
            }
        }

        // D-043: 화물 타입 표시 — "계란 87%", "유리 65%" 등 즉시 인식.
        string cargoName = CargoTypePresets.DisplayName(cargo.Type);
        label.text = cargo.IsTotaled
            ? $"{cargoName} 전파"
            : $"{cargoName}  {Mathf.RoundToInt(level * 100)}%";
    }
}
