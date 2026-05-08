using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 시작 / 재시작 시 뜨는 주문 보드 (세션 16 후속 — IMGUI → Canvas 전환).
///
/// 역할: 우측 세로 카드 N장, 호버 시 해당 주문 배달지 마커만 강조, 클릭 시 PreRunSetupPanel.Show(job).
/// 시각 디자인은 Scene/Prefab에서 직접. 이 스크립트는 데이터 바인딩 + 마커 상태 + 클릭 흐름만.
///
/// 인스펙터 와이어링 (Required):
///   panelRoot       — 보드 패널 root (활성/비활성 토글). Canvas 자식.
///   cardsContainer  — 카드 instantiate 부모 (Vertical Layout Group 권장).
///   cardPrefab      — OrderCardUI 컴포넌트 가진 prefab.
/// 인스펙터 와이어링 (Optional):
///   headerText      — 상단 헤더 TMP_Text.
///   helpText        — 하단 안내 TMP_Text.
/// </summary>
public class OrderBoard : MonoBehaviour, IOrderBoard
{
    [Header("Data")]
    [SerializeField] private OrderCatalog catalog;
    [SerializeField] private RunController runController;

    [Header("UI — Canvas 구조 (Scene/Prefab에서 디자인)")]
    [Tooltip("주문 보드 패널 root. Open 시 활성, Close 시 비활성.")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("Instantiate된 카드들이 담길 부모. Vertical Layout Group 권장.")]
    [SerializeField] private RectTransform cardsContainer;
    [Tooltip("카드 prefab — OrderCardUI 컴포넌트 가짐.")]
    [SerializeField] private GameObject cardPrefab;
    [Tooltip("상단 헤더 텍스트 (선택). 비우면 패널 prefab의 정적 텍스트 사용.")]
    [SerializeField] private TMP_Text headerText;
    [Tooltip("하단 도움말 텍스트 (선택).")]
    [SerializeField] private TMP_Text helpText;

    [Header("Day 26 — Shuffle Pick")]
    [Tooltip("매 보드 오픈마다 catalog.jobs 풀에서 N개 무작위 노출. 0이면 풀 전체.")]
    [SerializeField] private int offerCount = 3;

    // ---- 상태 ----
    bool isOpen;
    int hoveredIndex = -1;
    List<DeliveryPointMarker> allMarkers;
    readonly List<OrderJob> currentOffer = new List<OrderJob>();
    readonly List<OrderCardUI> spawnedCards = new List<OrderCardUI>();
    OrderJob lastSelectedJob;

    // ---- OrderCardUI → OrderBoard 호버 통지 ----
    static OrderBoard _hoverTarget;
    public static void NotifyHoverEnter(int cardIndex)
    {
        if (_hoverTarget != null) _hoverTarget.OnCardHoverEnter(cardIndex);
    }
    public static void NotifyHoverExit(int cardIndex)
    {
        if (_hoverTarget != null) _hoverTarget.OnCardHoverExit(cardIndex);
    }

    void Awake()
    {
        if (runController == null) runController = Services.Run as RunController;
        _hoverTarget = this;
        if (panelRoot != null && panelRoot.activeSelf) panelRoot.SetActive(false);
        if (helpText != null) helpText.text = "카드에 마우스 올리면 맵에 배달지가 표시됨 · 클릭으로 출발";
        if (headerText != null) headerText.text = "주문 선택";
    }

    void OnDestroy()
    {
        if (_hoverTarget == this) _hoverTarget = null;
    }

    public void Open()
    {
        isOpen = true;
        hoveredIndex = -1;
        PickOffer();
        CacheMarkers();
        if (allMarkers != null)
            foreach (var m in allMarkers) if (m != null) m.SetExternalControl(true);

        SpawnCards();

        if (panelRoot != null) panelRoot.SetActive(true);
        ApplyHoverState();
    }

    public void Close()
    {
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);

        // 카드 destroy.
        foreach (var c in spawnedCards) if (c != null) Destroy(c.gameObject);
        spawnedCards.Clear();

        // 런 중에도 마커는 보임. 자동 상태로.
        if (allMarkers != null && lastSelectedJob != null)
        {
            for (int i = 0; i < allMarkers.Count; i++)
            {
                if (allMarkers[i] == null) continue;
                bool inSet = i < lastSelectedJob.pointCount;
                allMarkers[i].SetVisible(inSet);
                if (inSet)
                {
                    allMarkers[i].SetColor(lastSelectedJob.color);
                    allMarkers[i].SetExternalControl(false);
                }
            }
        }
    }

    // ---- 내부 ----

    /// <summary>Day 26 — catalog.jobs 풀에서 offerCount개 무작위 픽 (중복 X).</summary>
    void PickOffer()
    {
        currentOffer.Clear();
        if (catalog == null || catalog.jobs == null || catalog.jobs.Count == 0) return;

        int n = offerCount > 0 ? Mathf.Min(offerCount, catalog.jobs.Count) : catalog.jobs.Count;
        if (catalog.jobs.Count <= n)
        {
            foreach (var j in catalog.jobs) if (j != null) currentOffer.Add(j);
            return;
        }

        var pool = new List<OrderJob>(catalog.jobs);
        for (int i = 0; i < n; i++)
        {
            int idx = Random.Range(i, pool.Count);
            (pool[i], pool[idx]) = (pool[idx], pool[i]);
            currentOffer.Add(pool[i]);
        }
    }

    void SpawnCards()
    {
        // 기존 카드 정리.
        foreach (var c in spawnedCards) if (c != null) Destroy(c.gameObject);
        spawnedCards.Clear();

        if (cardPrefab == null || cardsContainer == null)
        {
            Debug.LogWarning("[OrderBoard] cardPrefab 또는 cardsContainer 와이어링 누락. 인스펙터 확인.");
            return;
        }

        for (int i = 0; i < currentOffer.Count; i++)
        {
            var job = currentOffer[i];
            if (job == null) continue;

            var go = Instantiate(cardPrefab, cardsContainer);
            var card = go.GetComponent<OrderCardUI>();
            if (card == null)
            {
                Debug.LogWarning("[OrderBoard] cardPrefab에 OrderCardUI 컴포넌트 없음.");
                Destroy(go);
                continue;
            }
            card.Bind(i, job, OnCardClicked);
            spawnedCards.Add(card);
        }
    }

    // ---- OrderCardUI 콜백 ----

    void OnCardHoverEnter(int cardIndex)
    {
        if (!isOpen) return;
        hoveredIndex = cardIndex;
        for (int i = 0; i < spawnedCards.Count; i++)
            if (spawnedCards[i] != null) spawnedCards[i].SetHovering(i == cardIndex);
        ApplyHoverState();
    }

    void OnCardHoverExit(int cardIndex)
    {
        if (!isOpen) return;
        if (hoveredIndex == cardIndex)
        {
            hoveredIndex = -1;
            for (int i = 0; i < spawnedCards.Count; i++)
                if (spawnedCards[i] != null) spawnedCards[i].SetHovering(false);
            ApplyHoverState();
        }
    }

    void OnCardClicked(int cardIndex)
    {
        if (!isOpen) return;
        if (cardIndex < 0 || cardIndex >= currentOffer.Count) return;
        lastSelectedJob = currentOffer[cardIndex];

        // 보드 닫고 setup 패널 띄움 (timeScale=0 유지).
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);
        if (allMarkers != null && lastSelectedJob != null)
        {
            for (int i = 0; i < allMarkers.Count; i++)
            {
                if (allMarkers[i] != null) allMarkers[i].SetExternalControl(false);
            }
        }

        var setup = FindFirstObjectByType<PreRunSetupPanel>(); // Phase 1: PreRunSetupPanel 미등록 — 인터페이스 추가는 후속
        if (setup != null)
        {
            setup.Show(lastSelectedJob);
        }
        else if (runController != null)
        {
            // 폴백: setup 패널 없으면 직접 BeginRun.
            runController.BeginRun(lastSelectedJob);
        }
    }

    // ---- 마커 ----

    void CacheMarkers()
    {
        var mgr = Services.Delivery as DeliveryManager;
        allMarkers = new List<DeliveryPointMarker>();
        if (mgr == null) return;

        var points = FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None);
        foreach (var p in points)
        {
            var marker = p.GetComponent<DeliveryPointMarker>();
            if (marker == null) marker = p.gameObject.AddComponent<DeliveryPointMarker>();
            marker.SetVisible(true);
            allMarkers.Add(marker);
        }
    }

    void ApplyHoverState()
    {
        if (allMarkers == null) return;
        for (int i = 0; i < allMarkers.Count; i++)
        {
            if (allMarkers[i] == null) continue;
            if (hoveredIndex < 0 || hoveredIndex >= currentOffer.Count)
            {
                var fallbackJob = GetLargestJob();
                allMarkers[i].SetColor(fallbackJob != null ? fallbackJob.color : Color.white);
                allMarkers[i].SetDimmed(false);
            }
            else
            {
                var job = currentOffer[hoveredIndex];
                bool inSet = i < job.pointCount;
                if (inSet)
                {
                    allMarkers[i].SetColor(job.color);
                    allMarkers[i].SetDimmed(false);
                }
                else
                {
                    allMarkers[i].SetColor(Color.gray);
                    allMarkers[i].SetDimmed(true);
                }
            }
        }
    }

    OrderJob GetLargestJob()
    {
        if (currentOffer.Count > 0)
        {
            OrderJob largest = currentOffer[0];
            foreach (var j in currentOffer) if (j != null && j.pointCount > largest.pointCount) largest = j;
            return largest;
        }
        if (catalog == null || catalog.jobs == null || catalog.jobs.Count == 0) return null;
        OrderJob largestPool = catalog.jobs[0];
        foreach (var j in catalog.jobs) if (j != null && j.pointCount > largestPool.pointCount) largestPool = j;
        return largestPool;
    }
}
