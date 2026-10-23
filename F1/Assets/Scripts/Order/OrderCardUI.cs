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
    [Tooltip("호버 시 hoverStrip 알파 (job.color에 곱).")]
    [SerializeField] float hoverStripAlpha = 0.6f;
    [Tooltip("선택 시 종이에 job.color 를 섞는 비율(0~1). 잉크 글자가 읽히도록 낮게.")]
    [SerializeField, Range(0f, 0.6f)] float selectedPaperTint = 0.28f;

    // D-121 종이 전표 톤 — 배경색은 토큰에서(프리팹 직렬화 값이 아니라). 옛 어두운 색 필드는 제거.
    static Color BaseBgColor => UIPalette.Paper;
    static Color HoverBgColor => UIPalette.PaperDeep;
    static Color LockedBgColor => Color.Lerp(UIPalette.Paper, UIPalette.Stone, 0.6f);
    bool styled;

    int cardIndex;
    OrderJob job;
    // 채널 5 — 적재함 부족이면 잠김. 클릭 무시 + 회색 표시.
    bool locked;
    // 채널 8 정정 — 여러 주문 픽 (OrderBoard 다중 선택).
    bool selected;
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
        ApplyPaperStyle();

        // 채널 3 — 잡이 속한 zone 라벨을 작게 위에 표시.
        if (titleText != null)
        {
            Zone zone = ZoneUnlockSystem.Instance != null ? ZoneUnlockSystem.Instance.GetZoneOf(job) : null;
            if (zone != null)
            {
                // 종이 위에선 zone.tintColor(옅은 파랑 등)가 안 읽힘 → 보조 잉크색.
                titleText.text = $"<size=60%><color={UIPalette.Hex(UIPalette.InkMuted)}>{zone.displayName}</color></size>\n{job.displayName}";
            }
            else titleText.text = job.displayName;
        }
        if (colorStrip != null) colorStrip.color = job.color;
        if (bg != null) bg.color = BaseBgColor;
        if (hoverStrip != null)
        {
            var c = job.color; c.a = 0f;
            hoverStrip.color = c;
        }

        // 세션 27 — 어디로 배달할지 직접 표시 (단순 N곳 → 실제 라벨).
        string destStr;
        if (job.destinationLabels != null && job.destinationLabels.Count > 0)
        {
            // 라벨은 데이터 키(영문) — 화면엔 한글 표시명.
            destStr = PlaceNames.Join(job.destinationLabels);
        }
        else destStr = $"{job.itemCount}곳";
        SetStatRow(0, "행선지", destStr);
        SetStatRow(1, "시간", Services.PrototypeMode ? "판 제한 시간 안"
            : job.timeLimitSec > 0f
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

    /// <summary>
    /// 채널 5 — 적재함 칸 부족으로 잠금/해제. 잠기면 회색 + 클릭 무시 + 사유 표시.
    /// OrderBoard가 Bind 직후 호출 (job.itemCount &gt; 현재 적재함 칸).
    /// </summary>
    public void SetLocked(bool isLocked, int requiredCapacity, int currentCapacity)
    {
        SetLocked(isLocked, isLocked ? $"잠김 — 적재함 {requiredCapacity}칸 필요 (현재 {currentCapacity}칸)" : null);
    }

    /// <summary>채널 8 — 자유로운 잠금 사유. reason==null이면 본래 tipText 복원(drive tip).</summary>
    public void SetLocked(bool isLocked, string reason)
    {
        locked = isLocked;
        float a = isLocked ? 0.4f : 1f;

        ApplyBgColor();
        if (colorStrip != null && job != null)
            colorStrip.color = isLocked ? Color.Lerp(job.color, UIPalette.Stone, 0.75f) : job.color;

        SetAlpha(titleText, a);
        SetAlpha(tipText, a);
        for (int i = 0; i < statRows.Length; i++)
        {
            if (statRows[i] == null) continue;
            SetAlpha(statRows[i].label, a);
            SetAlpha(statRows[i].value, a);
        }

        if (tipText != null)
        {
            if (isLocked && !string.IsNullOrEmpty(reason)) tipText.text = reason;
            else if (!isLocked && job != null) tipText.text = $"→ {CargoTypePresets.DriveTip(job.cargoType)}";
        }
    }

    /// <summary>채널 8 정정 — 다중 픽 선택 상태. 클릭으로 토글.</summary>
    /// <summary>D-119 — 긴급 전화로 받은 주문 표시(제목 앞 빨간 "긴급").</summary>
    public void SetUrgent(bool urgent)
    {
        if (titleText == null) return;
        string tag = $"<color={UIPalette.Hex(UIPalette.Signal)}><b>긴급</b></color> ";
        bool has = titleText.text.StartsWith(tag);
        if (urgent && !has) titleText.text = tag + titleText.text;
        else if (!urgent && has) titleText.text = titleText.text.Substring(tag.Length);
    }

    public void SetSelected(bool sel)
    {
        selected = sel;
        ApplyBgColor();
        // colorStrip은 선택 시 더 진하게 (job.color 그대로 + alpha 1 유지)
        if (colorStrip != null && job != null && !locked) colorStrip.color = job.color;
    }

    public bool IsSelected => selected;

    void ApplyBgColor()
    {
        if (bg == null) return;
        if (locked) bg.color = LockedBgColor;
        else if (selected) bg.color = SelectedBgColor();
        else bg.color = BaseBgColor;
        // 선택 = 테라코타 굵은 테두리, 평소 = 잉크 얇은 테두리.
        UIPalette.InkBorder(bg, selected && !locked ? 5f : 2f);
        var o = bg.GetComponent<Outline>();
        if (o != null && selected && !locked) o.effectColor = UIPalette.Terracotta;
    }

    /// <summary>선택된 카드 배경 — 종이에 job.color 를 옅게 섞음(어떤 주문 골랐는지 색으로 한눈에).</summary>
    Color SelectedBgColor()
    {
        var c = job != null ? Color.Lerp(UIPalette.Paper, job.color, selectedPaperTint) : UIPalette.PaperDeep;
        c.a = 1f;
        return c;
    }

    /// <summary>
    /// D-121 — 프리팹(어두운 카드·흰 글자·맑은 고딕)을 런타임에 종이 전표 톤으로 덮는다.
    /// 배치(RectTransform)는 건드리지 않는다. 한 번만.
    /// </summary>
    void ApplyPaperStyle()
    {
        if (styled) return;
        styled = true;
        if (bg != null)
        {
            UIPalette.PaperPanel(bg, BaseBgColor, slot: UISlot.Card);
        }
        if (titleText != null)
        {
            UIPalette.Display(titleText, UIPalette.Ink);
            titleText.textWrappingMode = TextWrappingModes.Normal;
        }
        for (int i = 0; i < statRows.Length; i++)
        {
            if (statRows[i] == null) continue;
            UIPalette.Body(statRows[i].label, UIPalette.InkMuted);
            UIPalette.Body(statRows[i].value, UIPalette.Ink);
        }
        if (tipText != null)
        {
            UIPalette.Body(tipText, UIPalette.InkMuted);
            tipText.textWrappingMode = TextWrappingModes.Normal;     // 어절 줄바꿈(TMP Modern Hangul)
        }
    }

    static void SetAlpha(TMP_Text t, float a)
    {
        if (t == null) return;
        var c = t.color; c.a = a; t.color = c;
    }

    public void SetHovering(bool hovering)
    {
        // 잠긴 카드는 호버 배경색 변화 없음. 선택된 카드는 selectedBg 유지.
        if (bg != null)
        {
            if (locked) bg.color = LockedBgColor;
            else if (selected) bg.color = SelectedBgColor();
            else bg.color = hovering ? HoverBgColor : BaseBgColor;
        }
        if (hoverStrip != null && job != null && !locked)
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
        if (locked) return; // 적재함 부족 — 클릭 무시
        onCardClicked?.Invoke(cardIndex);
    }
}

[Serializable]
public class OrderCardStatRow
{
    public TMP_Text label;
    public TMP_Text value;
}
