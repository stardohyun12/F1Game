using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화물 위험 경고 HUD (Day 24, 4주차 잔여 "코너 경고 UI").
///
/// 설계 — 반응형 경고 (예측 X):
/// - 4가지 화물 데미지 소스(Long G, Lat G, Spin, Tilt) 중 가장 위험한 것 하나를 표시.
/// - 위험도 = current / warnThreshold. 0.7+ 이면 화면에 표시. 1.0+ 빨강 깜박.
/// - 위치: 화면 상단 중앙(속도계 아래). 짧고 즉각적인 시그널.
///
/// 텍스트 매핑
/// - Long G → "급제동" / "급가속" (signed로 구분)
/// - Lat G  → "코너 ◀" / "코너 ▶"
/// - Spin   → "스핀"
/// - Tilt   → "경사"
///
/// 데미지 임계 7배경 정보:
/// - CargoBox.longWarnG / latWarnG / spinWarnDegPerSec / tiltWarnDeg
/// - 패킹·업그레이드·타입 보정 후 값 기준 (CargoBox.ApplyPreset에서 결정).
/// </summary>
[DisallowMultipleComponent]
public class CargoWarningHUD : MonoBehaviour
{
    [SerializeField] CargoBox cargo;

    [Header("Layout (상단 중앙)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(0, -100);
    [SerializeField] Vector2 size = new Vector2(280, 64);

    [Header("Thresholds (current / warnThreshold)")]
    [Tooltip("이 비율 미만이면 HUD 숨김.")]
    [SerializeField] float showThreshold = 0.7f;
    [Tooltip("이 비율 이상이면 빨강 + 깜박.")]
    [SerializeField] float critThreshold = 1.0f;
    [SerializeField] float critBlinkHz = 5f;

    [Header("Damage Source Threshold Floor")]
    [Tooltip("CargoBox 임계값을 GetField로 못 읽으면 폴백값 사용.")]
    [SerializeField] float fallbackLongWarnG = 0.9f;
    [SerializeField] float fallbackLatWarnG = 1.0f;
    [SerializeField] float fallbackSpinWarnDegPerSec = 100f;
    [SerializeField] float fallbackTiltWarnDeg = 10f;

    RectTransform rt;
    Image bg;
    TMP_Text label;

    enum Source { None, BrakeAccel, Corner, Spin, Slope }

    void Awake()
    {
        if (cargo == null) cargo = FindFirstObjectByType<CargoBox>();
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        BuildIfNeeded();
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[CargoWarningHUD] Canvas 밖에 있음.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (bg == null)
        {
            var go = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            bg = go.GetComponent<Image>();
            bg.raycastTarget = false;
            bg.color = new Color(0.18f, 0.10f, 0.10f, 0.85f);
            if (theme?.panelDark != null) { bg.sprite = theme.panelDark; bg.type = Image.Type.Sliced; }
            var r = bg.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        if (label == null)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            label = go.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 32;
            label.fontStyle = FontStyles.Bold;
            if (theme?.fontPrimary != null) label.font = theme.fontPrimary;
            var r = label.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(12, 6); r.offsetMax = new Vector2(-12, -6);
        }
    }

    void Update()
    {
        if (cargo == null)
        {
            cargo = FindFirstObjectByType<CargoBox>();
            if (cargo == null) { SetVisible(false); return; }
        }

        // 4 소스 중 가장 위험도 높은 것 선택.
        float longRatio = cargo.CurrentLongG / Mathf.Max(0.0001f, fallbackLongWarnG);
        float latRatio = cargo.CurrentLatG / Mathf.Max(0.0001f, fallbackLatWarnG);
        float spinRatio = cargo.CurrentSpinDegPerSec / Mathf.Max(0.0001f, fallbackSpinWarnDegPerSec);
        float tiltRatio = cargo.CurrentTiltDeg / Mathf.Max(0.0001f, fallbackTiltWarnDeg);

        Source src = Source.None;
        float maxRatio = 0f;
        if (longRatio > maxRatio) { maxRatio = longRatio; src = Source.BrakeAccel; }
        if (latRatio > maxRatio) { maxRatio = latRatio; src = Source.Corner; }
        if (spinRatio > maxRatio) { maxRatio = spinRatio; src = Source.Spin; }
        if (tiltRatio > maxRatio) { maxRatio = tiltRatio; src = Source.Slope; }

        if (maxRatio < showThreshold) { SetVisible(false); return; }
        SetVisible(true);

        // 텍스트 (signed로 방향 표시)
        string txt = src switch
        {
            Source.BrakeAccel => cargo.SignedLongG < 0f ? "급제동" : "급가속",
            Source.Corner => cargo.SignedLatG > 0f ? "◀ 코너" : "코너 ▶",
            Source.Spin => "스핀",
            Source.Slope => "경사",
            _ => ""
        };
        label.text = txt;

        // 색상: warn(노랑) → crit(빨강 깜박)
        Color warnYellow = new Color(1f, 0.85f, 0.20f, 1f);
        Color critRed = new Color(1f, 0.25f, 0.25f, 1f);
        Color labelColor;
        Color bgColor;
        if (maxRatio >= critThreshold)
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * critBlinkHz * Mathf.PI * 2f);
            labelColor = Color.white;
            bgColor = Color.Lerp(critRed * 0.6f, critRed, t);
        }
        else
        {
            float warnT = Mathf.InverseLerp(showThreshold, critThreshold, maxRatio);
            labelColor = Color.Lerp(Color.white, Color.white, warnT);
            bgColor = Color.Lerp(new Color(0.18f, 0.14f, 0.05f, 0.85f), warnYellow * 0.7f, warnT);
            bgColor.a = 0.85f;
        }
        if (label != null) label.color = labelColor;
        if (bg != null) bg.color = bgColor;
    }

    void SetVisible(bool on)
    {
        if (bg != null && bg.enabled != on) bg.enabled = on;
        if (label != null && label.enabled != on) label.enabled = on;
    }
}
