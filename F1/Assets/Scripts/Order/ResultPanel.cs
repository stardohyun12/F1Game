using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 정산 화면 (UGUI/TMP). 세션 16 B2 — IMGUI에서 UGUI로 재작성.
///
/// 변경 (도현 #2 "한글 안보임" + #3-2 정산 UI 확대):
/// - IMGUI(OnGUI) → UGUI 자가구성 (BuildIfNeeded). TMP_Text 사용 → KenneyFuture SDF + 한글 fallback 자동.
/// - 사이즈 600×460(700) → 1200×800.
/// - 신규: 타이어 마모, 포장 효과 피드백, 시간 보너스.
/// - 동일 public API: Show(controller), Hide(), upgradeCatalog SerializeField.
///
/// 호출자 (RunController.EndRun) 변경 없음.
/// </summary>
public class ResultPanel : MonoBehaviour, IResultPanel
{
    [SerializeField] private UpgradeCatalog upgradeCatalog;

    bool isOpen;
    public bool IsOpen => isOpen;
    RunController controller;
    bool built;

    // UGUI 루트
    Canvas canvas;
    GameObject backdrop;
    RectTransform panelRT;
    Image panelBg;
    Image colorStrip;
    TMP_Text titleText;
    TMP_Text rewardLabel, rewardVal;
    TMP_Text timeLabel, timeVal;
    TMP_Text orderLabel, orderVal;
    TMP_Text completedLabel, completedVal;
    TMP_Text cargoLabel, cargoVal;
    TMP_Text storyText;
    TMP_Text tireLabel, tireVal;
    Image tireFillBg, tireFill;
    TMP_Text packingFeedback;
    TMP_Text walletLabel, walletVal;
    Button restartBtn;
    TMP_Text restartBtnText;
    Button _garageBtn;
    TMP_Text _garageBtnTxt;
    readonly List<UpgradeCardUI> cards = new();

    class UpgradeCardUI
    {
        public RectTransform rt;
        public Image bg;
        public Image strip;
        public TMP_Text name;
        public TMP_Text desc;
        public TMP_Text cost;
        public Button buyBtn;
        public TMP_Text buyText;
        public UpgradeDef def;
    }

    public void Show(RunController ctrl)
    {
        controller = ctrl;
        isOpen = true;
        EnsureBuilt();
        if (canvas != null) canvas.gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        isOpen = false;
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    void Awake()
    {
        EnsureEventSystem();
        EnsureBuilt();
        if (canvas != null) canvas.gameObject.SetActive(false);     // 시작 시 숨김
    }

    /// <summary>UGUI 버튼 클릭 디스패치를 위한 EventSystem 자동 보장 (도현 #2-11).</summary>
    static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        var existing = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existing != null) return;
        var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    void EnsureBuilt()
    {
        if (built) return;
        built = true;

        // 별도 루트 Canvas (Screen Space Overlay, 가장 앞)
        var rootGO = new GameObject("ResultPanelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        rootGO.transform.SetParent(transform, false);
        canvas = rootGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;     // OrderBoard(100) 위
        var scaler = rootGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var theme = UITheme.Active;

        // Backdrop (전체 어둡게)
        backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(rootGO.transform, false);
        var bdRT = backdrop.GetComponent<RectTransform>();
        bdRT.anchorMin = Vector2.zero; bdRT.anchorMax = Vector2.one;
        bdRT.offsetMin = Vector2.zero; bdRT.offsetMax = Vector2.zero;
        var bdImg = backdrop.GetComponent<Image>();
        bdImg.color = new Color(0, 0, 0, 0.65f);
        bdImg.raycastTarget = true;     // 배경 클릭 차단

        // Panel (1200×800, 중앙)
        var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(rootGO.transform, false);
        panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        // 도현 #2-4 "아직도 작음": 1200×800 → 1600×1000 (1920×1080 reference 기준 83%×92%).
        panelRT.sizeDelta = new Vector2(1600, 1000);
        panelBg = panelGO.GetComponent<Image>();
        panelBg.color = theme != null ? theme.colorPanel : new Color(0.12f, 0.14f, 0.18f, 0.96f);
        if (theme?.panelDark != null) { panelBg.sprite = theme.panelDark; panelBg.type = Image.Type.Sliced; }

        // 색상 띠 (top 12px)
        var stripGO = new GameObject("ColorStrip", typeof(RectTransform), typeof(Image));
        stripGO.transform.SetParent(panelGO.transform, false);
        var stripRT = stripGO.GetComponent<RectTransform>();
        stripRT.anchorMin = new Vector2(0, 1);
        stripRT.anchorMax = new Vector2(1, 1);
        stripRT.pivot = new Vector2(0.5f, 1);
        stripRT.sizeDelta = new Vector2(0, 12);
        colorStrip = stripGO.GetComponent<Image>();
        colorStrip.color = new Color(0.4f, 0.95f, 0.4f);

        // Title
        titleText = CreateText(panelGO.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -75), new Vector2(800, 80), 64, FontStyles.Bold, TextAlignmentOptions.Center);

        // Stats — 좌측 컬럼 (라벨 + 값) 5개. 도현 #2-8 fix: + 대신 - (UI Y는 상→하 음수, 행 추가는 더 음수).
        // #2-10: 이야기 공간 확보 위해 statY -180→-160.
        float statY = -160f;
        float rowH = 56f;
        (orderLabel, orderVal) = CreateStatRow(panelGO.transform, "Order", statY - 0 * rowH, "주문");
        (completedLabel, completedVal) = CreateStatRow(panelGO.transform, "Completed", statY - 1 * rowH, "완료");
        (timeLabel, timeVal) = CreateStatRow(panelGO.transform, "Time", statY - 2 * rowH, "소요 시간");
        (cargoLabel, cargoVal) = CreateStatRow(panelGO.transform, "Cargo", statY - 3 * rowH, "화물");
        (rewardLabel, rewardVal) = CreateStatRow(panelGO.transform, "Reward", statY - 4 * rowH, "보상");

        // 화물 이야기 — 도현 #2-12: -460→-450 (보상-story 30px, story-tire 30px 균등).
        storyText = CreateText(panelGO.transform, "Story", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -450), new Vector2(1500, 50), 32, FontStyles.Italic, TextAlignmentOptions.Center);
        if (UITheme.Active != null) storyText.color = UITheme.Active.colorTextMuted;

        // 타이어 마모 — 도현 #2-8: y=460 (카드 top 340 + 여유)
        tireLabel = CreateText(panelGO.transform, "TireLabel", new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(60, 460), new Vector2(220, 36), 28, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        tireLabel.text = "타이어";
        var tireBgGO = new GameObject("TireFillBG", typeof(RectTransform), typeof(Image));
        tireBgGO.transform.SetParent(panelGO.transform, false);
        var tireBgRT = tireBgGO.GetComponent<RectTransform>();
        tireBgRT.anchorMin = new Vector2(0, 0); tireBgRT.anchorMax = new Vector2(0, 0);
        tireBgRT.pivot = new Vector2(0, 0.5f);
        tireBgRT.anchoredPosition = new Vector2(285, 478);
        tireBgRT.sizeDelta = new Vector2(280, 24);
        tireFillBg = tireBgGO.GetComponent<Image>();
        tireFillBg.color = new Color(0.10f, 0.10f, 0.12f, 1f);

        var tireFillGO = new GameObject("TireFill", typeof(RectTransform), typeof(Image));
        tireFillGO.transform.SetParent(tireBgGO.transform, false);
        var tireFillRT = tireFillGO.GetComponent<RectTransform>();
        tireFillRT.anchorMin = new Vector2(0, 0); tireFillRT.anchorMax = new Vector2(0, 1);
        tireFillRT.pivot = new Vector2(0, 0.5f);
        tireFillRT.offsetMin = new Vector2(2, 2); tireFillRT.offsetMax = new Vector2(2, -2);
        tireFill = tireFillGO.GetComponent<Image>();
        tireFill.color = new Color(0.5f, 0.85f, 1f);

        tireVal = CreateText(panelGO.transform, "TireVal", new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(580, 460), new Vector2(300, 36), 24, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

        // 포장 피드백 (우 영역)
        packingFeedback = CreateText(panelGO.transform, "PackingFeedback", new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-60, 460), new Vector2(500, 60), 26, FontStyles.Normal, TextAlignmentOptions.MidlineRight);

        // Wallet 표시 — 도현 #2-8: 카드 위 (타이어보다 약간 아래 행)
        walletLabel = CreateText(panelGO.transform, "WalletLabel", new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(60, 400), new Vector2(220, 36), 26, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        walletLabel.text = "보유 자금";
        walletVal = CreateText(panelGO.transform, "WalletVal", new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(285, 400), new Vector2(300, 36), 30, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        if (UITheme.Active != null) walletVal.color = UITheme.Active.colorAccent;

        // 업그레이드 카드 5개 (가로)
        BuildUpgradeCards(panelGO.transform);

        // 다시 버튼 (좌측으로 이동 — 차고로 버튼과 나란히)
        var btnGO = new GameObject("RestartBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(panelGO.transform, false);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0); btnRT.anchorMax = new Vector2(0.5f, 0);
        btnRT.pivot = new Vector2(0.5f, 0);
        btnRT.anchoredPosition = new Vector2(-160f, 30);
        btnRT.sizeDelta = new Vector2(280, 70);
        var btnImg = btnGO.GetComponent<Image>();
        btnImg.color = theme != null ? theme.colorAccent : new Color(0.20f, 0.55f, 0.85f);
        if (theme?.panelBright != null) { btnImg.sprite = theme.panelBright; btnImg.type = Image.Type.Sliced; }
        restartBtn = btnGO.GetComponent<Button>();
        restartBtn.onClick.AddListener(OnRestartClicked);

        restartBtnText = CreateText(btnGO.transform, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(280, 70), 38, FontStyles.Bold, TextAlignmentOptions.Center);
        restartBtnText.text = "다시";
        restartBtnText.color = Color.white;

        // "차고로" 버튼 (D-085 — 다시 버튼 우측)
        var garageBtnGo = new GameObject("GarageBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        garageBtnGo.transform.SetParent(panelGO.transform, false);
        var gbRT = garageBtnGo.GetComponent<RectTransform>();
        gbRT.anchorMin = new Vector2(0.5f, 0); gbRT.anchorMax = new Vector2(0.5f, 0);
        gbRT.pivot = new Vector2(0.5f, 0);
        gbRT.anchoredPosition = new Vector2(160f, 30);
        gbRT.sizeDelta = new Vector2(280, 70);
        var gbImg = garageBtnGo.GetComponent<Image>();
        gbImg.color = new Color(0.35f, 0.40f, 0.65f);
        _garageBtn = garageBtnGo.GetComponent<Button>();
        _garageBtn.onClick.AddListener(OnGarageClicked);

        var gbTxtGo = new GameObject("Txt", typeof(RectTransform));
        gbTxtGo.transform.SetParent(garageBtnGo.transform, false);
        _garageBtnTxt = gbTxtGo.AddComponent<TextMeshProUGUI>();
        _garageBtnTxt.text = "차고로";
        _garageBtnTxt.alignment = TextAlignmentOptions.Center;
        _garageBtnTxt.color = Color.white;
        _garageBtnTxt.fontSize = 38;
        _garageBtnTxt.fontStyle = FontStyles.Bold;
        if (theme?.fontPrimary != null) _garageBtnTxt.font = theme.fontPrimary;
        var gbtRT = gbTxtGo.GetComponent<RectTransform>();
        gbtRT.anchorMin = Vector2.zero; gbtRT.anchorMax = Vector2.one;
        gbtRT.offsetMin = Vector2.zero; gbtRT.offsetMax = Vector2.zero;
    }

    void BuildUpgradeCards(Transform parent)
    {
        var theme = UITheme.Active;
        cards.Clear();
        // 카탈로그 개수만큼 카드 생성 (채널 5: 적재함 업그레이드 2개 추가로 5→7).
        int n = (upgradeCatalog != null && upgradeCatalog.upgrades != null && upgradeCatalog.upgrades.Count > 0)
            ? upgradeCatalog.upgrades.Count
            : 5;
        float panelW = 1600f;     // 도현 #2-4 panel size 동기화
        float pad = 60f;
        float spacing = 16f;
        float available = panelW - pad * 2 - spacing * (n - 1);
        float cardW = available / n;
        float cardH = 220f;
        float cardY = 110f;     // bottom 기준 30(btn) + 70(btn) + 30(margin) ≈ 130
        cardY = 120f;
        for (int i = 0; i < n; i++)
        {
            var card = new UpgradeCardUI();
            var cardGO = new GameObject($"Upgrade_{i}", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(parent, false);
            card.rt = cardGO.GetComponent<RectTransform>();
            card.rt.anchorMin = new Vector2(0, 0); card.rt.anchorMax = new Vector2(0, 0);
            card.rt.pivot = new Vector2(0, 0);
            card.rt.anchoredPosition = new Vector2(pad + i * (cardW + spacing), cardY);
            card.rt.sizeDelta = new Vector2(cardW, cardH);
            card.bg = cardGO.GetComponent<Image>();
            if (theme?.panelDark != null) { card.bg.sprite = theme.panelDark; card.bg.type = Image.Type.Sliced; }
            card.bg.color = new Color(0.16f, 0.18f, 0.22f, 0.95f);

            // strip
            var stripGO = new GameObject("Strip", typeof(RectTransform), typeof(Image));
            stripGO.transform.SetParent(cardGO.transform, false);
            var sRT = stripGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.sizeDelta = new Vector2(0, 6);
            card.strip = stripGO.GetComponent<Image>();
            card.strip.color = Color.white;

            // name
            card.name = CreateText(cardGO.transform, "Name", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -36), new Vector2(0, 40), 22, FontStyles.Bold, TextAlignmentOptions.Center);
            card.name.textWrappingMode = TextWrappingModes.Normal;
            // desc
            card.desc = CreateText(cardGO.transform, "Desc", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -90), new Vector2(0, 70), 18, FontStyles.Normal, TextAlignmentOptions.Top);
            card.desc.textWrappingMode = TextWrappingModes.Normal;
            // cost — 도현 #2-9: 카드 안 cost 라인이 buy 버튼과 겹침. -160→-140.
            card.cost = CreateText(cardGO.transform, "Cost", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -140), new Vector2(0, 30), 22, FontStyles.Bold, TextAlignmentOptions.Center);
            if (theme != null) card.cost.color = theme.colorAccent;
            // buy btn
            var btnGO = new GameObject("BuyBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(cardGO.transform, false);
            var bRT = btnGO.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 0);
            bRT.pivot = new Vector2(0.5f, 0);
            bRT.anchoredPosition = new Vector2(0, 8);
            bRT.sizeDelta = new Vector2(-16, 38);
            card.buyBtn = btnGO.GetComponent<Button>();
            var btnImg = btnGO.GetComponent<Image>();
            btnImg.color = new Color(0.20f, 0.55f, 0.85f);
            if (theme?.panelBright != null) { btnImg.sprite = theme.panelBright; btnImg.type = Image.Type.Sliced; }
            card.buyText = CreateText(btnGO.transform, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(-16, 38), 22, FontStyles.Bold, TextAlignmentOptions.Center);
            card.buyText.text = "구매";
            card.buyText.color = Color.white;
            int captured = i;
            card.buyBtn.onClick.AddListener(() => OnUpgradeClick(captured));

            cards.Add(card);
        }
    }

    void Refresh()
    {
        if (controller == null) return;
        var theme = UITheme.Active;
        var job = controller.CurrentJob;
        int orderCount = controller.CurrentOrders != null ? controller.CurrentOrders.Count : 0;
        bool perfect = controller.LastIntegrityPercent >= 0.8f;

        // Title — 다물건이면 N건 부기
        string baseTitle = controller.LastRunSuccess ? "배달 완료!" : "시간 초과";
        titleText.text = orderCount > 1 ? $"{baseTitle}  ({orderCount}건)" : baseTitle;
        Color stripCol = controller.LastRunSuccess && perfect ? new Color(0.4f, 0.95f, 0.4f)
                       : controller.LastRunSuccess              ? new Color(0.95f, 0.85f, 0.3f)
                                                                : new Color(1f, 0.5f, 0.4f);
        colorStrip.color = stripCol;

        // 스탯 — 다물건이면 주문 이름들 콤마 결합 (긴 경우 ellipsis로 잘림)
        if (orderCount > 1)
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (var o in controller.CurrentOrders) if (o != null) names.Add(o.displayName);
            orderVal.text = string.Join(", ", names);
        }
        else orderVal.text = job != null ? job.displayName : "-";
        // 완료 — 다물건이면 분모 = 주문 수 (각 주문 1곳).
        int totalSpots = orderCount > 0 ? orderCount : (job != null ? job.itemCount : 0);
        completedVal.text = $"{controller.CompletedCount}/{totalSpots}곳";
        timeVal.text = FormatTimeWithBonus(controller.LastElapsed, job);
        string cargoName = job != null ? CargoTypePresets.DisplayName(job.cargoType) : "-";
        int integrityInt = Mathf.RoundToInt(controller.LastIntegrityPercent * 100f);
        cargoLabel.text = $"화물 ({cargoName})";
        cargoVal.text = $"{integrityInt}% · {RunController.IntegrityGrade(controller.LastIntegrityPercent)}";
        rewardVal.text = $"₩{controller.LastReward:N0}";

        // 이야기
        storyText.text = BuildStory(job, controller.LastIntegrityPercent);

        // 타이어 마모
        var tire = Services.Tires as TireSystem;
        if (tire != null)
        {
            float wear = tire.Wear;
            tireFill.fillAmount = 1f;
            tireFill.rectTransform.sizeDelta = new Vector2(0, 0);
            // ProgressBar — left anchor 0~1 fillAmount via anchorMax.x
            var rt = tireFill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(1f - wear), 1);     // 남은 수명 표시 (1-wear)
            tireVal.text = $"{Mathf.RoundToInt((1f - wear) * 100f)}% ({TireSystem.DisplayName(tire.Current)})";
            tireFill.color = wear > 0.7f ? new Color(1f, 0.5f, 0.4f)
                          : wear > 0.4f ? new Color(0.95f, 0.85f, 0.3f)
                                         : new Color(0.4f, 0.95f, 0.7f);
            tireLabel.gameObject.SetActive(true);
            tireFillBg.gameObject.SetActive(true);
            tireVal.gameObject.SetActive(true);
        }
        else
        {
            tireLabel.gameObject.SetActive(false);
            tireFillBg.gameObject.SetActive(false);
            tireVal.gameObject.SetActive(false);
        }

        // 포장 피드백 — Task 14: PackingTier 폐기. WrapState 기반으로 대체 예정 (Task 17).
        if (packingFeedback != null)
            packingFeedback.text = "";

        // 업그레이드 영역
        var ups = UpgradeSystem.Instance;
        bool hasUpgrades = upgradeCatalog != null && upgradeCatalog.upgrades != null && upgradeCatalog.upgrades.Count > 0 && ups != null;
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (!hasUpgrades || i >= upgradeCatalog.upgrades.Count)
            {
                card.rt.gameObject.SetActive(false);
                continue;
            }
            card.def = upgradeCatalog.upgrades[i];
            if (card.def == null) { card.rt.gameObject.SetActive(false); continue; }
            card.rt.gameObject.SetActive(true);
            card.name.text = card.def.displayName;
            card.desc.text = card.def.description;
            card.cost.text = $"₩{card.def.cost:N0}";
            card.strip.color = card.def.tintColor;

            bool owned = ups.IsOwned(card.def.effect);
            bool affordable = ups.Wallet >= card.def.cost;
            if (owned)
            {
                card.bg.color = new Color(0.20f, 0.45f, 0.25f, 0.92f);
                card.buyBtn.interactable = false;
                card.buyText.text = "✓ 보유";
            }
            else if (!affordable)
            {
                card.bg.color = new Color(0.10f, 0.10f, 0.12f, 0.92f);
                card.buyBtn.interactable = false;
                card.buyText.text = "자금 부족";
            }
            else
            {
                card.bg.color = new Color(0.18f, 0.20f, 0.25f, 0.95f);
                card.buyBtn.interactable = true;
                card.buyText.text = "구매";
            }
        }

        if (ups != null) walletVal.text = $"₩{ups.Wallet:N0}";
        else walletVal.text = "—";

        // 차고로 버튼 활성 조건 (D-085): 카탈로그 있으면 항상 진입 가능
        // (자금 부족하면 차고 안에서 회색+부족 태그로 표시 — D-087)
        if (_garageBtn != null)
        {
            _garageBtn.interactable = Services.Restore != null
                && Services.Restore.Catalog != null;
        }
    }

    void OnUpgradeClick(int idx)
    {
        var ups = UpgradeSystem.Instance;
        if (ups == null) return;
        if (idx < 0 || idx >= cards.Count) return;
        var def = cards[idx].def;
        if (def == null) return;
        if (ups.TryPurchase(def))
        {
            Refresh();     // 보유 자금 + 카드 상태 갱신
        }
    }

    void OnRestartClicked()
    {
        if (controller != null) controller.RestartFromBoard();
    }

    void OnGarageClicked()
    {
        Hide();
        var garage = UnityEngine.Object.FindFirstObjectByType<PitCrew.Garage.GarageScreen>();
        if (garage != null) garage.Show();
        else Debug.LogWarning("[ResultPanel] GarageScreen 없음 — Task 11 wiring 필요");
    }

    static string FormatTimeWithBonus(float sec, OrderJob job)
    {
        int m = Mathf.FloorToInt(sec / 60f);
        int s = Mathf.FloorToInt(sec % 60f);
        string baseStr = $"{m}분 {s:D2}초";
        if (job == null || job.timeLimitSec <= 0f) return baseStr;
        int limM = Mathf.FloorToInt(job.timeLimitSec / 60f);
        int limS = Mathf.FloorToInt(job.timeLimitSec % 60f);
        float bonus = job.timeLimitSec - sec;
        if (bonus > 0f)
            return $"{baseStr} / {limM}분 {limS:D2}초 (+{bonus:F0}s 여유)";
        return $"{baseStr} / {limM}분 {limS:D2}초 (초과)";
    }

    static string BuildStory(OrderJob job, float integrityPercent)
    {
        if (job == null) return string.Empty;
        var name = CargoTypePresets.DisplayName(job.cargoType);
        float i = integrityPercent * 100f;
        if (i >= 95f) return $"\"{name}, 한 점 흠 없이 도착했음.\"";
        if (i >= 80f) return $"\"{name}, 잘 가져왔음.\"";
        if (i >= 50f) return $"\"{name}, 조금 흠집 났음.\"";
        if (i >= 20f) return $"\"{name}, 많이 망가졌음. 컴플레인 들어옴.\"";
        if (i > 0f)   return $"\"{name}, 거의 다 부서짐.\"";
        return $"\"{name}, 완전히 박살남. 환불 처리.\"";
    }

    /// <summary>
    /// 라벨+값 한 행 생성. 패널 좌상단 기준 anchoredPos = (0, statY).
    /// 라벨 좌측, 값 우측 정렬.
    /// </summary>
    (TMP_Text label, TMP_Text val) CreateStatRow(Transform parent, string id, float y, string labelText)
    {
        // 2026-05-16: 라벨↔값 간격이 너무 큰 도현 피드백 — val 영역 700→520으로 좁힘.
        // 라벨은 좌측에 280 폭(60~340), val은 우측에 520 폭으로 우측 정렬.
        var label = CreateText(parent, $"{id}_Label", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(60, y), new Vector2(280, 50), 30, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        label.text = labelText;
        if (UITheme.Active != null) label.color = UITheme.Active.colorTextMuted;

        var val = CreateText(parent, $"{id}_Val", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-60, y), new Vector2(520, 50), 32, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        // 다물건 주문명 결합 시 길어지면 자동 축소 (잘림 대신 폰트 줄임).
        val.enableAutoSizing = true;
        val.fontSizeMin = 18;
        val.fontSizeMax = 32;
        val.overflowMode = TMPro.TextOverflowModes.Ellipsis;
        if (UITheme.Active != null) val.color = UITheme.Active.colorText;

        return (label, val);
    }

    /// <summary>panel 자식 TMP_Text 생성 helper.</summary>
    static TMP_Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;     // anchor 기준 pivot 동일 (편의)
        if (anchorMin == anchorMax) rt.pivot = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        var theme = UITheme.Active;
        if (theme != null)
        {
            if (theme.fontPrimary != null) t.font = theme.fontPrimary;
            t.color = theme.colorText;
        }
        return t;
    }
}
