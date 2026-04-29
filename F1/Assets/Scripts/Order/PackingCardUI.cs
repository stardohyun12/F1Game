using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 포장 카드 prefab의 root 컴포넌트 (세션 16 후속 — procedural → prefab).
///
/// 사용 흐름:
/// 1. Assets/Prefabs/UI/PackingCard.prefab 만들고 자식 elements를 Inspector 와이어링.
/// 2. PreRunSetupPanel이 Show() 시 4개 인스턴스 (None/Bubble/Foam/Hardcase) 만들어 Bind().
/// 3. Bind는 tier 고정, Refresh로 선택 상태 시각 갱신.
/// 4. 카드 클릭 → onClicked 콜백.
///
/// 디자인 의도: prefab에서 직접 픽셀·폰트·색·앵커 조정.
/// </summary>
public class PackingCardUI : MonoBehaviour
{
    [Header("Background")]
    public Image bg;
    [Tooltip("상단 색상 띠 (티어별 고정 색).")]
    public Image colorStrip;

    [Header("Texts")]
    public TMP_Text nameText;
    [Tooltip("배율 표시 (×1.0 / ×1.3 / ×1.6 / ×2.0).")]
    public TMP_Text multiplierText;
    public TMP_Text descText;
    [Tooltip("가격 (무료 또는 ₩X,XXX).")]
    public TMP_Text costText;

    [Header("Click")]
    public Button button;

    [Header("Visuals — 선택 상태 색")]
    [SerializeField] Color selectedColor = new Color(0.30f, 0.45f, 0.65f, 0.95f);
    [SerializeField] Color normalColor = new Color(0.16f, 0.18f, 0.22f, 0.92f);

    PackingTier tier;
    Action<PackingTier> onClicked;
    bool wired;

    /// <summary>고정 1회 — Show()에서 호출. tier 결정 + 텍스트.</summary>
    public void Bind(PackingTier tier, Action<PackingTier> onClicked)
    {
        this.tier = tier;
        this.onClicked = onClicked;

        if (nameText != null) nameText.text = PackingTierHelper.DisplayName(tier);
        if (multiplierText != null) multiplierText.text = $"×{PackingTierHelper.ThresholdMultiplier(tier):F1}";
        if (descText != null) descText.text = PackingTierGuide(tier);
        if (costText != null)
        {
            int cost = PackingTierHelper.Cost(tier);
            costText.text = cost == 0 ? "무료" : $"₩{cost:N0}";
        }
        if (colorStrip != null) colorStrip.color = TierColor(tier);

        if (button != null && !wired)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.onClicked?.Invoke(this.tier));
            wired = true;
        }
    }

    /// <summary>매번 호출 (선택 변경 시) — 강조 색 토글.</summary>
    public void SetSelected(bool selected)
    {
        if (bg != null) bg.color = selected ? selectedColor : normalColor;
    }

    public PackingTier Tier => tier;

    static Color TierColor(PackingTier t) => t switch
    {
        PackingTier.None => new Color(0.50f, 0.50f, 0.55f),
        PackingTier.Bubble => new Color(0.55f, 0.85f, 1f),
        PackingTier.Foam => new Color(0.95f, 0.85f, 0.30f),
        PackingTier.Hardcase => new Color(1f, 0.55f, 0.30f),
        _ => Color.white
    };

    static string PackingTierGuide(PackingTier t) => t switch
    {
        PackingTier.None => "포장 없음. 모든 충격 그대로.",
        PackingTier.Bubble => "일반 주행 안전. 코너·제동 흠집 X.",
        PackingTier.Foam => "스포츠 주행 가능. 약한 충돌 흡수.",
        PackingTier.Hardcase => "최고 보호. 큰 충돌도 견딤.",
        _ => ""
    };
}
