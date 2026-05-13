using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 주문 카드 prefab의 root 컴포넌트 (세션 16 후속 — IMGUI → Canvas).
///
/// 사용 흐름:
/// 1. Assets/Prefabs/UI/OrderCard.prefab 만들고 자식 elements를 Inspector 와이어링.
/// 2. OrderBoard가 Open() 시 Instantiate → Bind() 호출.
/// 3. Bind는 데이터 + 콜백 등록, Refresh로 시각 갱신.
/// 4. PointerEnter/Exit/Click → OrderBoard 정적 콜백.
///
/// 디자인 의도: prefab에서 도현이 Scene view로 직접 픽셀 위치·폰트·색·앵커 조정.
/// 이 스크립트는 데이터 바인딩만 담당.
/// </summary>
public class OrderCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Background")]
    [Tooltip("카드 전체 배경. 호버 시 색 변경.")]
    public Image bg;
    [Tooltip("좌측 세로 색상 띠 (job.color).")]
    public Image colorStrip;
    [Tooltip("호버 시 우측 강조 띠 (선택, 없어도 됨).")]
    public Image hoverStrip;

    [Header("Title")]
    public TMP_Text titleText;

    [Header("Stat Rows (6) — 순서: 지점/시간/보상/무게/화물/물건")]
    public OrderCardStatRow[] statRows = new OrderCardStatRow[6];

    [Header("Tip — 운전 가이드 + 포장 가이드 (멀티라인)")]
    public TMP_Text tipText;

    [Header("Visuals")]
    [Tooltip("기본 배경 색 (검정 반투명).")]
    [SerializeField] Color baseBgColor = new Color(0.05f, 0.05f, 0.10f, 0.88f);
    [Tooltip("호버 시 배경 색.")]
    [SerializeField] Color hoverBgColor = new Color(0.18f, 0.20f, 0.30f, 0.95f);
    [Tooltip("호버 시 hoverStrip 알파 (job.color에 곱).")]
    [SerializeField] float hoverStripAlpha = 0.6f;

    int cardIndex;
    OrderJob job;
    Action<int> onCardClicked;
    Action<int> onCardHoverEnter;
    Action<int> onCardHoverExit;

    /// <summary>OrderBoard.Open에서 호출. 데이터 + 콜백 4종 바인딩.</summary>
    /// <remarks>
    /// Phase 6(A3): 호버도 클릭과 동일한 콜백 패턴으로 통일. 이전엔 호버만 정적 dispatch
    /// (`OrderBoard.NotifyHoverEnter`)로 흘러 일관성이 깨졌고 멀티 보드를 불가능하게 했음.
    /// </remarks>
    public void Bind(int cardIndex, OrderJob job,
        Action<int> onCardClicked,
        Action<int> onCardHoverEnter = null,
        Action<int> onCardHoverExit = null)
    {
        this.cardIndex = cardIndex;
        this.job = job;
        this.onCardClicked = onCardClicked;
        this.onCardHoverEnter = onCardHoverEnter;
        this.onCardHoverExit = onCardHoverExit;

        if (titleText != null) titleText.text = job.displayName;
        if (colorStrip != null) colorStrip.color = job.color;
        if (bg != null) bg.color = baseBgColor;
        if (hoverStrip != null)
        {
            var c = job.color; c.a = 0f;
            hoverStrip.color = c;
        }

        SetStatRow(0, "지점", $"{job.pointCount}곳");
        SetStatRow(1, "시간", job.timeLimitSec > 0f
            ? $"{Mathf.RoundToInt(job.timeLimitSec / 60f)}분 {Mathf.RoundToInt(job.timeLimitSec % 60f):D2}초"
            : "무제한");
        string rewardStr = job.timeBonus > 0
            ? $"₩{job.baseReward:N0} + ₩{job.timeBonus:N0}"
            : $"₩{job.baseReward:N0}";
        SetStatRow(2, "보상", rewardStr);
        SetStatRow(3, "무게", job.WeightLabel);
        SetStatRow(4, "화물", CargoTypePresets.DisplayName(job.cargoType));
        SetStatRow(5, "물건", $"{job.itemCount}개");

        if (tipText != null) tipText.text = $"→ {CargoTypePresets.DriveTip(job.cargoType)}";
    }

    public void SetHovering(bool hovering)
    {
        if (bg != null)
        {
            bg.color = hovering ? hoverBgColor : baseBgColor;
        }
        if (hoverStrip != null && job != null)
        {
            var c = job.color;
            c.a = hovering ? hoverStripAlpha : 0f;
            hoverStrip.color = c;
        }
    }

    void SetStatRow(int idx, string label, string val)
    {
        if (idx < 0 || idx >= statRows.Length) return;
        var row = statRows[idx];
        if (row == null) return;
        if (row.label != null) row.label.text = label;
        if (row.value != null) row.value.text = val;
    }

    // ---- Pointer events ----
    public void OnPointerEnter(PointerEventData eventData)
    {
        onCardHoverEnter?.Invoke(cardIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onCardHoverExit?.Invoke(cardIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onCardClicked?.Invoke(cardIndex);
    }
}

[Serializable]
public class OrderCardStatRow
{
    public TMP_Text label;
    public TMP_Text value;
}
