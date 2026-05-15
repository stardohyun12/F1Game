using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 시작 / 재시작 시 뜨는 주문 보드 (세션 16 후속 — IMGUI → Canvas 전환).
///
/// 역할: 우측 세로 카드 N장, 호버 시 해당 주문 배달지 마커만 강조, 클릭 시 PackingScreen.Show(job).
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

    // Phase 6(A3): 정적 _hoverTarget 라우터 제거. 호버 콜백은 클릭과 동일하게 Bind 시 주입.

    void Awake()
    {
        if (runController == null) runController = Services.Run as RunController;
        if (panelRoot != null && panelRoot.activeSelf) panelRoot.SetActive(false);
        if (helpText != null) helpText.text = "카드에 마우스 올리면 맵에 배달지가 표시됨 · 클릭으로 출발";
        if (headerText != null) headerText.text = "주문 선택";
    }

    // CCTV HUD 토글 (주문 보드 열려 있는 동안 트렁크 캠 안 보이게)
    GameObject cachedCargoViewHUD;
    bool cargoViewWasActive;

    // 채널 8 — 차고(시작점) 마커 + 호버 시 경로선 미리보기
    GameObject startMarker;
    LineRenderer previewLine;

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
        EnsureStartMarkerAndLine();
        ApplyHoverState();

        // 채널 3 — 보드 열 때마다 잠금 패널도 갱신 (자금 변동으로 affordable 바뀌었을 수 있음).
        var zup = FindFirstObjectByType<ZoneUnlockPanel>(FindObjectsInactive.Include);
        if (zup != null) zup.RefreshExternal();

        // 트렁크 CCTV 잠시 숨김 (도현 요청 — 주문 화면에는 필요 없음)
        if (cachedCargoViewHUD == null)
        {
            var cv = Object.FindFirstObjectByType<CargoView>(FindObjectsInactive.Include);
            if (cv != null) cachedCargoViewHUD = cv.gameObject;
        }
        if (cachedCargoViewHUD != null)
        {
            cargoViewWasActive = cachedCargoViewHUD.activeSelf;
            cachedCargoViewHUD.SetActive(false);
        }
    }

    public void Close()
    {
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);
        HideStartMarkerAndLine();

        // CCTV 복원 (런 시작과 함께 다시 보여야 함)
        if (cachedCargoViewHUD != null && cargoViewWasActive) cachedCargoViewHUD.SetActive(true);

        // 카드 destroy.
        foreach (var c in spawnedCards) if (c != null) Destroy(c.gameObject);
        spawnedCards.Clear();

        // 채널 8: 런 중 마커는 lastSelectedJob.destinationLabels 기반으로 표시.
        if (allMarkers != null && lastSelectedJob != null && lastSelectedJob.destinationLabels != null)
        {
            for (int i = 0; i < allMarkers.Count; i++)
            {
                var marker = allMarkers[i]; if (marker == null) continue;
                bool inSet = lastSelectedJob.destinationLabels.Contains(marker.Label);
                marker.SetVisible(inSet);
                if (inSet)
                {
                    marker.SetColor(lastSelectedJob.color);
                    marker.SetExternalControl(false);
                }
            }
        }
    }

    // ---- 내부 ----

    /// <summary>Day 26 — 풀에서 offerCount개 무작위 픽 (중복 X). W1: ZoneUnlockSystem 있으면 해금 zone 풀, 없으면 catalog 폴백.</summary>
    void PickOffer()
    {
        currentOffer.Clear();

        List<OrderJob> source;
        if (ZoneUnlockSystem.Instance != null)
        {
            source = ZoneUnlockSystem.Instance.GetAvailableJobs();
        }
        else if (catalog != null && catalog.jobs != null)
        {
            source = catalog.jobs;
        }
        else return;

        if (source.Count == 0) return;

        int n = offerCount > 0 ? Mathf.Min(offerCount, source.Count) : source.Count;
        if (source.Count <= n)
        {
            foreach (var j in source) if (j != null) currentOffer.Add(j);
            return;
        }

        var pool = new List<OrderJob>(source);
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
            card.Bind(i, job, OnCardClicked, OnCardHoverEnter, OnCardHoverExit);
            // 채널 5 — 적재함 칸보다 물건이 많으면 카드 잠금 (회색 + 클릭 무시).
            int cap = BoxCapacity;
            card.SetLocked(job.itemCount > cap, job.itemCount, cap);
            spawnedCards.Add(card);
        }
    }

    /// <summary>현재 적재함 칸 수 (채널 5). UpgradeSystem 미등록이면 1.</summary>
    int BoxCapacity => Services.Upgrade != null ? Services.Upgrade.BoxCapacity : 1;

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
        // 채널 5 — 적재함 부족 잡은 무시 (OrderCardUI도 클릭 막지만 방어).
        if (currentOffer[cardIndex].itemCount > BoxCapacity) return;
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

        if (Services.Packing != null)
        {
            Services.Packing.Show(lastSelectedJob);
        }
        else if (runController != null)
        {
            // 폴백: PackingScreen 없으면 직접 BeginRun.
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

        bool hovering = hoveredIndex >= 0 && hoveredIndex < currentOffer.Count;
        var hoverJob = hovering ? currentOffer[hoveredIndex] : null;

        // 채널 8 — 호버한 잡의 경로선 미리보기 (호버 안 하면 클리어)
        if (hoverJob != null) DrawPreviewLineFor(hoverJob);
        else if (previewLine != null) previewLine.positionCount = 0;

        for (int i = 0; i < allMarkers.Count; i++)
        {
            var marker = allMarkers[i]; if (marker == null) continue;
            string lbl = marker.Label;

            if (hovering && hoverJob != null
                && hoverJob.destinationLabels != null
                && hoverJob.destinationLabels.Contains(lbl))
            {
                // 호버한 잡의 목적지 — 그 잡 색으로 선명하게
                marker.SetVisible(true);
                marker.SetColor(hoverJob.color);
                marker.SetDimmed(false);
            }
            else
            {
                // 다른 잡들의 목적지가 있나? — 있으면 그 잡 색으로 dim 표시 (동시 비교)
                var owner = FirstOfferJobUsingLabel(lbl);
                if (owner != null)
                {
                    marker.SetVisible(true);
                    marker.SetColor(owner.color);
                    marker.SetDimmed(true);
                }
                else
                {
                    marker.SetVisible(false);
                }
            }
        }
    }

    /// <summary>현재 offered 잡 중 이 label을 destinationLabels에 가진 첫 잡 (없으면 null).</summary>
    OrderJob FirstOfferJobUsingLabel(string lbl)
    {
        if (string.IsNullOrEmpty(lbl)) return null;
        for (int i = 0; i < currentOffer.Count; i++)
        {
            var j = currentOffer[i];
            if (j != null && j.destinationLabels != null && j.destinationLabels.Contains(lbl))
                return j;
        }
        return null;
    }

    // ── 채널 8 — 차고 마커 + 경로선 미리보기 ─────────────────────────

    void EnsureStartMarkerAndLine()
    {
        if (startMarker == null)
        {
            var race = GameObject.Find("Race");
            if (race != null)
            {
                startMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                startMarker.name = "OrderBoard_StartMarker";
                DestroyImmediate(startMarker.GetComponent<Collider>());
                startMarker.transform.position = race.transform.position + Vector3.up * 1.5f;
                startMarker.transform.localScale = new Vector3(2f, 3f, 2f);
                var mr = startMarker.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = new Color(1f, 0.9f, 0.2f, 1f);
                mr.sharedMaterial = mat;
            }
        }
        if (startMarker != null) startMarker.SetActive(true);

        if (previewLine == null)
        {
            var lrGO = new GameObject("OrderBoard_PreviewLine");
            previewLine = lrGO.AddComponent<LineRenderer>();
            previewLine.material = new Material(Shader.Find("Sprites/Default"));
            previewLine.startWidth = 0.4f; previewLine.endWidth = 0.4f;
            previewLine.positionCount = 0;
            previewLine.useWorldSpace = true;
        }
    }

    void HideStartMarkerAndLine()
    {
        if (startMarker != null) startMarker.SetActive(false);
        if (previewLine != null) previewLine.positionCount = 0;
    }

    void DrawPreviewLineFor(OrderJob job)
    {
        if (previewLine == null) EnsureStartMarkerAndLine();
        if (previewLine == null || job == null) return;
        if (job.destinationLabels == null || job.destinationLabels.Count == 0)
        {
            previewLine.positionCount = 0;
            return;
        }
        var race = GameObject.Find("Race");
        if (race == null) { previewLine.positionCount = 0; return; }
        var dm = Services.Delivery as DeliveryManager;
        if (dm == null) { previewLine.positionCount = 0; return; }

        previewLine.startColor = job.color; previewLine.endColor = job.color;
        var pts = new List<Vector3> { race.transform.position + Vector3.up * 0.5f };
        foreach (var lbl in job.destinationLabels)
        {
            var p = dm.FindByLabel(lbl);
            if (p != null) pts.Add(p.transform.position + Vector3.up * 0.5f);
        }
        previewLine.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++) previewLine.SetPosition(i, pts[i]);
    }

    OrderJob GetLargestJob()
    {
        if (currentOffer.Count > 0)
        {
            OrderJob largest = currentOffer[0];
            foreach (var j in currentOffer) if (j != null && j.itemCount > largest.itemCount) largest = j;
            return largest;
        }
        if (catalog == null || catalog.jobs == null || catalog.jobs.Count == 0) return null;
        OrderJob largestPool = catalog.jobs[0];
        foreach (var j in catalog.jobs) if (j != null && j.itemCount > largestPool.itemCount) largestPool = j;
        return largestPool;
    }
}
