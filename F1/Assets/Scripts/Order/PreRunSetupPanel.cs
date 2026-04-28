using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 출발 전 화물·포장 선택 패널 (세션 16 B3 — 1차 단순 모달).
///
/// 흐름 (현재)
///   OrderBoard 카드 클릭 → PreRunSetupPanel.Show(job)
///   → 화물 종류(고정, 표시만) + 포장 4단계 카드(None/Bubble/Foam/Hardcase) 선택
///   → 출발 버튼 → CargoBox.SetPacking(tier) + RunController.BeginRun(job)
///
/// 도현 #1: "물건을 어떻게 포장할지에 대한 계획과 포장지에 따른 손상도감소기능"
/// 데미지 감소 로직은 이미 작동(CargoBox.ApplyPreset의 packMul). UI만 신설.
///
/// 도현 #2 미래 요구 (Phase 2/3 별도 단계로 분리, ideas-parking):
///   "물건을 선택하고 포장하는 단계가 따로 있어서 완충제를 어떻게 싸고 뭘 쓸지
///    물건을 3D로 돌려가면서 포장해야함"
/// → PackingStage scene/panel 신설:
///   - 화물 prefab 3D 표시 (RenderTexture에 isolated camera).
///   - 마우스 드래그 회전, 줌.
///   - 완충제 layer 토글/이동 (Bubble wrap 시각화 — 반투명 mesh overlay).
///   - 적용 후 BeginRun 트리거.
/// 본 클래스는 단순 모달로 유지, 별도 PackingStage 신설 권장.
/// </summary>
public class PreRunSetupPanel : MonoBehaviour
{
    [SerializeField] private RunController runController;

    bool isOpen;
    public bool IsOpen => isOpen;
    OrderJob currentJob;
    PackingTier selectedTier = PackingTier.Bubble;
    bool built;

    Canvas canvas;
    GameObject backdrop;
    RectTransform panelRT;
    TMP_Text titleText, cargoText, cargoTipText, totalCostText;
    readonly List<PackingCardUI> cards = new();
    Button confirmBtn, cancelBtn;
    TMP_Text confirmText, cancelText;

    class PackingCardUI
    {
        public RectTransform rt;
        public Image bg;
        public Image strip;
        public TMP_Text name;
        public TMP_Text desc;
        public TMP_Text cost;
        public TMP_Text mul;
        public Button btn;
        public PackingTier tier;
    }

    void Awake()
    {
        if (runController == null) runController = FindFirstObjectByType<RunController>();
        EnsureEventSystem();
        EnsureBuilt();
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    /// <summary>UGUI 버튼 클릭 디스패치 — EventSystem 자동 보장 (도현 #2-11).</summary>
    static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        var existing = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existing != null) return;
        var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    public void Show(OrderJob job)
    {
        currentJob = job;
        isOpen = true;
        EnsureBuilt();
        if (canvas != null) canvas.gameObject.SetActive(true);
        // 기본 선택: order의 defaultPacking이 있으면 그것, 없으면 Bubble
        selectedTier = job != null && System.Enum.IsDefined(typeof(PackingTier), (int)job.defaultPacking)
            ? job.defaultPacking
            : PackingTier.Bubble;
        Refresh();
    }

    public void Hide()
    {
        isOpen = false;
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    void EnsureBuilt()
    {
        if (built) return;
        built = true;

        var rootGO = new GameObject("PreRunSetupCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        rootGO.transform.SetParent(transform, false);
        canvas = rootGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;     // OrderBoard(IMGUI ~100) 위, ResultPanel(200) 아래
        var scaler = rootGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var theme = UITheme.Active;

        backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(rootGO.transform, false);
        var bdRT = backdrop.GetComponent<RectTransform>();
        bdRT.anchorMin = Vector2.zero; bdRT.anchorMax = Vector2.one;
        bdRT.offsetMin = Vector2.zero; bdRT.offsetMax = Vector2.zero;
        var bdImg = backdrop.GetComponent<Image>();
        bdImg.color = new Color(0, 0, 0, 0.65f);

        // Panel — 1100×620 중앙
        var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(rootGO.transform, false);
        panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        // 도현 #2-9: 카드 글자 벗어남 → 패널 1100→1400, 카드별 더 넓은 공간.
        panelRT.sizeDelta = new Vector2(1400, 680);
        var panelBg = panelGO.GetComponent<Image>();
        panelBg.color = theme != null ? theme.colorPanel : new Color(0.12f, 0.14f, 0.18f, 0.96f);
        if (theme?.panelDark != null) { panelBg.sprite = theme.panelDark; panelBg.type = Image.Type.Sliced; }

        // Title
        titleText = CreateText(panelGO.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -50), new Vector2(900, 60), 48, FontStyles.Bold, TextAlignmentOptions.Center);
        titleText.text = "출발 준비";

        // Cargo (좌상단 큰 라벨)
        cargoText = CreateText(panelGO.transform, "Cargo", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(60, -130), new Vector2(700, 44), 36, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        // Cargo tip (운전 가이드)
        cargoTipText = CreateText(panelGO.transform, "CargoTip", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(60, -180), new Vector2(900, 36), 24, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
        if (theme != null) cargoTipText.color = theme.colorTextMuted;

        // 4개 포장 카드 (가로)
        BuildPackingCards(panelGO.transform);

        // 합계 비용 (좌하단)
        totalCostText = CreateText(panelGO.transform, "TotalCost", new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(60, 50), new Vector2(700, 60), 32, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        // 취소 버튼 (가운데 좌)
        cancelBtn = CreateButton(panelGO.transform, "CancelBtn", new Vector2(0, 0), new Vector2(240, 80),
            new Vector2(800, 40), out cancelText, "취소", 36, new Color(0.40f, 0.40f, 0.45f));
        cancelBtn.onClick.AddListener(OnCancelClicked);

        // 출발 버튼 (우하단)
        confirmBtn = CreateButton(panelGO.transform, "ConfirmBtn", new Vector2(0, 0), new Vector2(320, 80),
            new Vector2(1060, 40), out confirmText, "출발", 40, theme != null ? theme.colorAccent : new Color(0.95f, 0.65f, 0.15f));
        confirmBtn.onClick.AddListener(OnConfirmClicked);
    }

    void BuildPackingCards(Transform parent)
    {
        var theme = UITheme.Active;
        cards.Clear();
        var tiers = new[] { PackingTier.None, PackingTier.Bubble, PackingTier.Foam, PackingTier.Hardcase };
        // 4 카드 가로. panel 1400, pad 60, spacing 20 → cardW = (1400-120-60)/4 = 305px (한글 충분).
        int n = tiers.Length;
        float pad = 60f, spacing = 20f, cardW = (1400 - pad * 2 - spacing * (n - 1)) / n, cardH = 320f;
        for (int i = 0; i < n; i++)
        {
            var card = new PackingCardUI { tier = tiers[i] };
            var cardGO = new GameObject($"Pack_{tiers[i]}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGO.transform.SetParent(parent, false);
            card.rt = cardGO.GetComponent<RectTransform>();
            card.rt.anchorMin = new Vector2(0, 1); card.rt.anchorMax = new Vector2(0, 1);
            card.rt.pivot = new Vector2(0, 1);
            card.rt.anchoredPosition = new Vector2(pad + i * (cardW + spacing), -240);
            card.rt.sizeDelta = new Vector2(cardW, cardH);
            card.bg = cardGO.GetComponent<Image>();
            if (theme?.panelDark != null) { card.bg.sprite = theme.panelDark; card.bg.type = Image.Type.Sliced; }

            // strip
            var stripGO = new GameObject("Strip", typeof(RectTransform), typeof(Image));
            stripGO.transform.SetParent(cardGO.transform, false);
            var sRT = stripGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.sizeDelta = new Vector2(0, 8);
            card.strip = stripGO.GetComponent<Image>();
            card.strip.color = TierColor(tiers[i]);

            // 이름
            card.name = CreateText(cardGO.transform, "Name", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -22), new Vector2(0, 50), 32, FontStyles.Bold, TextAlignmentOptions.Center);
            // ×배율
            card.mul = CreateText(cardGO.transform, "Mul", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -76), new Vector2(0, 50), 40, FontStyles.Bold, TextAlignmentOptions.Center);
            if (theme != null) card.mul.color = theme.colorAccent;
            // 설명 — 카드 안에서 word wrap. 도현 #2-9: 영역 ↑ 80→120, fontSize 22→20.
            card.desc = CreateText(cardGO.transform, "Desc", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -140), new Vector2(-20, 120), 20, FontStyles.Normal, TextAlignmentOptions.Top);
            card.desc.textWrappingMode = TextWrappingModes.Normal;
            card.desc.overflowMode = TMPro.TextOverflowModes.Truncate;
            // 가격
            card.cost = CreateText(cardGO.transform, "Cost", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 16), new Vector2(0, 36), 26, FontStyles.Bold, TextAlignmentOptions.Center);

            // 클릭 이벤트
            card.btn = cardGO.GetComponent<Button>();
            int captured = i;
            card.btn.onClick.AddListener(() => OnCardClicked(tiers[captured]));

            cards.Add(card);
        }
    }

    void Refresh()
    {
        var theme = UITheme.Active;

        // Cargo 표시
        if (currentJob != null)
        {
            string name = CargoTypePresets.DisplayName(currentJob.cargoType);
            cargoText.text = $"화물: {name}";
            cargoTipText.text = $"→ {CargoTypePresets.DriveTip(currentJob.cargoType)}";
        }
        else
        {
            cargoText.text = "화물: -";
            cargoTipText.text = "";
        }

        // 카드 강조 + 데이터
        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            c.name.text = PackingTierHelper.DisplayName(c.tier);
            float mul = PackingTierHelper.ThresholdMultiplier(c.tier);
            c.mul.text = $"×{mul:F1}";
            c.desc.text = PackingTierGuide(c.tier);
            int costAmt = PackingTierHelper.Cost(c.tier);
            c.cost.text = costAmt == 0 ? "무료" : $"₩{costAmt:N0}";

            bool selected = c.tier == selectedTier;
            c.bg.color = selected
                ? new Color(0.30f, 0.45f, 0.65f, 0.95f)
                : new Color(0.16f, 0.18f, 0.22f, 0.92f);
        }

        // 합계 + 출발 텍스트
        int packCost = PackingTierHelper.Cost(selectedTier);
        int reward = currentJob != null ? currentJob.baseReward : 0;
        totalCostText.text = packCost > 0
            ? $"포장비 ₩{packCost:N0}  /  예상 보상 ₩{reward:N0}"
            : $"무료 포장  /  예상 보상 ₩{reward:N0}";

        confirmText.text = $"출발 ({PackingTierHelper.DisplayName(selectedTier)})";
    }

    void OnCardClicked(PackingTier tier)
    {
        selectedTier = tier;
        Refresh();
    }

    void OnConfirmClicked()
    {
        // CargoBox 찾아 포장 적용
        var cargo = FindFirstObjectByType<CargoBox>();
        if (cargo != null) cargo.SetPacking(selectedTier);

        // 패널 닫기 + RunController.BeginRun
        Hide();
        if (runController != null && currentJob != null)
            runController.BeginRun(currentJob);
    }

    void OnCancelClicked()
    {
        // OrderBoard로 다시 (런 시작 X)
        Hide();
        var board = FindFirstObjectByType<OrderBoard>();
        if (board != null) board.Open();
    }

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

    static TMP_Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        if (anchorMin != anchorMax) rt.pivot = (anchorMin + anchorMax) * 0.5f;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        var theme = UITheme.Active;
        if (theme != null) { if (theme.fontPrimary != null) t.font = theme.fontPrimary; t.color = theme.colorText; }
        return t;
    }

    static Button CreateButton(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 anchoredPos,
        out TMP_Text label, string text, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        var theme = UITheme.Active;
        if (theme?.panelBright != null) { img.sprite = theme.panelBright; img.type = Image.Type.Sliced; }
        var btn = go.GetComponent<Button>();
        label = CreateText(go.transform, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, size, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        label.text = text;
        label.color = Color.white;
        return btn;
    }
}
