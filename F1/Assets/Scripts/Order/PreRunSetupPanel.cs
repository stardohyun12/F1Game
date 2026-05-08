using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 출발 전 화물·포장 선택 패널 (세션 16 후속 — procedural → prefab 패턴).
///
/// 흐름:
///   OrderBoard 카드 클릭 → PreRunSetupPanel.Show(job)
///   → 화물 종류(고정, 표시만) + 포장 4단계 카드 (None/Bubble/Foam/Hardcase) 선택
///   → 출발 버튼 → RunController.BeginRun(job, selectedTier)
///
/// 인스펙터 와이어링 (Required):
///   panelRoot           — 패널 root (활성/비활성 토글)
///   titleText           — "출발 준비"
///   cargoText           — "화물: 계란"
///   cargoTipText        — "→ 급가속·급제동 금지"
///   totalCostText       — "포장비 ₩X / 예상 보상 ₩Y"
///   cardsParent         — PackingCard 4개 부모 (Horizontal Layout Group 권장)
///   packingCardPrefab   — PackingCardUI 컴포넌트 가진 prefab
///   confirmBtn          — 출발 버튼
///   confirmText         — 출발 버튼 라벨 (텍스트 갱신용)
///   cancelBtn           — 취소 버튼
///
/// 디자인: 도현이 prefab + scene에서 직접 픽셀·폰트·앵커 조정.
/// </summary>
public class PreRunSetupPanel : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private RunController runController;

    [Header("UI — Canvas 구조 (Scene/Prefab에서 디자인)")]
    [Tooltip("패널 root. Show 시 활성, Hide 시 비활성.")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("타이틀 (\"출발 준비\").")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("화물 표시 (\"화물: 계란\").")]
    [SerializeField] private TMP_Text cargoText;
    [Tooltip("화물 운전 가이드 (\"→ 급가속·급제동 금지\").")]
    [SerializeField] private TMP_Text cargoTipText;
    [Tooltip("합계 표시 (\"포장비 ₩X / 예상 보상 ₩Y\").")]
    [SerializeField] private TMP_Text totalCostText;
    [Tooltip("4개 PackingCard 부모. Horizontal Layout Group 권장.")]
    [SerializeField] private RectTransform cardsParent;
    [Tooltip("포장 카드 prefab — PackingCardUI 컴포넌트 필요.")]
    [SerializeField] private GameObject packingCardPrefab;
    [Tooltip("출발 버튼.")]
    [SerializeField] private Button confirmBtn;
    [Tooltip("출발 버튼 라벨 — 선택 티어에 따라 \"출발 (뽁뽁이)\" 형식으로 갱신.")]
    [SerializeField] private TMP_Text confirmText;
    [Tooltip("취소 버튼.")]
    [SerializeField] private Button cancelBtn;

    bool isOpen;
    public bool IsOpen => isOpen;
    OrderJob currentJob;
    PackingTier selectedTier = PackingTier.Bubble;

    static readonly PackingTier[] kPackingOrder =
    {
        PackingTier.None, PackingTier.Bubble, PackingTier.Foam, PackingTier.Hardcase
    };
    readonly List<PackingCardUI> spawnedCards = new();
    bool cardsSpawned;
    bool buttonsWired;

    void Awake()
    {
        if (runController == null) runController = Services.Run as RunController;
        EnsureEventSystem();
        if (panelRoot != null && panelRoot.activeSelf) panelRoot.SetActive(false);
        WireButtons();
    }

    /// <summary>UGUI 버튼 클릭용 EventSystem 자동 보장.</summary>
    static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        var existing = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existing != null) return;
        var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    void WireButtons()
    {
        if (buttonsWired) return;
        if (confirmBtn != null)
        {
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(OnConfirmClicked);
        }
        if (cancelBtn != null)
        {
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(OnCancelClicked);
        }
        buttonsWired = true;
    }

    public void Show(OrderJob job)
    {
        currentJob = job;
        isOpen = true;
        WireButtons();
        SpawnCardsIfNeeded();

        // 기본 선택: order의 defaultPacking이 유효하면 그것, 없으면 Bubble.
        selectedTier = job != null && System.Enum.IsDefined(typeof(PackingTier), (int)job.defaultPacking)
            ? job.defaultPacking
            : PackingTier.Bubble;

        if (panelRoot != null) panelRoot.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void SpawnCardsIfNeeded()
    {
        if (cardsSpawned) return;
        if (packingCardPrefab == null || cardsParent == null)
        {
            Debug.LogWarning("[PreRunSetupPanel] packingCardPrefab 또는 cardsParent 와이어링 누락.");
            return;
        }

        spawnedCards.Clear();
        for (int i = 0; i < kPackingOrder.Length; i++)
        {
            var go = Instantiate(packingCardPrefab, cardsParent);
            var card = go.GetComponent<PackingCardUI>();
            if (card == null)
            {
                Debug.LogWarning("[PreRunSetupPanel] packingCardPrefab에 PackingCardUI 컴포넌트 없음.");
                Destroy(go);
                continue;
            }
            card.Bind(kPackingOrder[i], OnCardClicked);
            spawnedCards.Add(card);
        }
        cardsSpawned = true;
    }

    void Refresh()
    {
        // 화물 표시
        if (currentJob != null)
        {
            string name = CargoTypePresets.DisplayName(currentJob.cargoType);
            if (cargoText != null) cargoText.text = $"화물: {name}";
            if (cargoTipText != null) cargoTipText.text = $"→ {CargoTypePresets.DriveTip(currentJob.cargoType)}";
        }
        else
        {
            if (cargoText != null) cargoText.text = "화물: -";
            if (cargoTipText != null) cargoTipText.text = "";
        }

        // 카드 강조
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            var card = spawnedCards[i];
            if (card == null) continue;
            card.SetSelected(card.Tier == selectedTier);
        }

        // 합계 + 출발 라벨
        int packCost = PackingTierHelper.Cost(selectedTier);
        int reward = currentJob != null ? currentJob.baseReward : 0;
        if (totalCostText != null)
        {
            totalCostText.text = packCost > 0
                ? $"포장비 ₩{packCost:N0}  /  예상 보상 ₩{reward:N0}"
                : $"무료 포장  /  예상 보상 ₩{reward:N0}";
        }
        if (confirmText != null)
        {
            confirmText.text = $"출발 ({PackingTierHelper.DisplayName(selectedTier)})";
        }
    }

    void OnCardClicked(PackingTier tier)
    {
        selectedTier = tier;
        Refresh();
    }

    void OnConfirmClicked()
    {
        Hide();
        if (runController != null && currentJob != null)
        {
            // 세션 16 후속: BeginRun 오버로드로 selectedTier 전달.
            // 기존 버그(BeginRun 안 cargo.SetPacking이 selectedTier 덮어쓰던 것) 해소.
            runController.BeginRun(currentJob, selectedTier);
        }
    }

    void OnCancelClicked()
    {
        Hide();
        if (Services.Board != null) Services.Board.Open();
    }
}
