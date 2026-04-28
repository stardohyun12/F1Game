using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 속도계 HUD. 세션 13 재튜닝 — 숫자 세로 튕김 방지 + 검정 텍스트.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SpeedometerHUD : MonoBehaviour
{
    [SerializeField] ArcadeCarController car;

    [Header("Layout (좌하단 기준)")]
    // 도현 #2-2 (세션 16): UI 너무 작음. 스택 위치도 비례 상향.
    [SerializeField] Vector2 anchoredPos = new Vector2(40, 280);
    [SerializeField] Vector2 size = new Vector2(320, 110);

    [Header("Thresholds (0~1 of maxSpeed)")]
    // midSpeedNorm 제거 (CS0414 미사용). highSpeedNorm은 색상 변화 임계로 사용 중.
    [SerializeField] float highSpeedNorm = 0.85f;

    RectTransform rt;
    Image bg;
    TMP_Text speedText, unitText;

    void Awake()
    {
        if (car == null) car = FindFirstObjectByType<ArcadeCarController>();
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        BuildIfNeeded();
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[SpeedometerHUD] Canvas 밖.");
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

        // 숫자 박스: 세로 중앙 + 우측 정렬, 한 줄, 큰 숫자
        if (speedText == null)
        {
            var g = new GameObject("Speed", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            speedText = g.AddComponent<TextMeshProUGUI>();
            speedText.raycastTarget = false;
            speedText.alignment = TextAlignmentOptions.MidlineRight;
            speedText.fontSize = 76; speedText.fontStyle = FontStyles.Bold;
            speedText.textWrappingMode = TextWrappingModes.NoWrap;
            speedText.overflowMode = TextOverflowModes.Overflow;
            if (theme != null) { if (theme.fontPrimary != null) speedText.font = theme.fontPrimary; speedText.color = theme.colorText; }
            var r = speedText.rectTransform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0.5f, 0.5f);
            // 도현 #2-7: fontSize 76 + 한글 SDF 문자 폭 ↑ → unit 영역 침범 (─76 → ─110).
            r.offsetMin = new Vector2(12, 0); r.offsetMax = new Vector2(-110, 0);
        }

        if (unitText == null)
        {
            var g = new GameObject("Unit", typeof(RectTransform));
            g.transform.SetParent(transform, false);
            unitText = g.AddComponent<TextMeshProUGUI>();
            unitText.raycastTarget = false;
            unitText.text = "km/h";
            unitText.alignment = TextAlignmentOptions.MidlineLeft;
            unitText.fontSize = 24;
            unitText.textWrappingMode = TextWrappingModes.NoWrap;
            if (theme != null) { if (theme.fontCondensed != null) unitText.font = theme.fontCondensed; unitText.color = theme.colorTextMuted; }
            var r = unitText.rectTransform;
            r.anchorMin = new Vector2(1, 0); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(1, 0.5f);
            r.sizeDelta = new Vector2(96, 0);
            r.anchoredPosition = new Vector2(-8, 0);
        }
    }

    void Update()
    {
        if (car == null) return;
        var theme = UITheme.Active;
        if (theme != null)
        {
            if (bg != null) bg.color = theme.colorPanel;
            if (unitText != null) unitText.color = theme.colorTextMuted;
        }

        float mps = Mathf.Abs(car.CurrentSpeedMps);
        int kmh = Mathf.RoundToInt(mps * 3.6f);
        speedText.text = kmh.ToString();

        // 저속/정지도 가독성 유지 — colorText(검정) 기본, 고속만 accent
        float norm = car.MaxSpeedMps > 0 ? mps / car.MaxSpeedMps : 0f;
        Color col;
        if (theme == null) col = Color.black;
        else if (norm >= highSpeedNorm) col = theme.colorAccent;
        else col = theme.colorText;
        speedText.color = col;
    }
}
