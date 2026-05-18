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
    // 채널 8 정정 — 여러 주문 동시 픽 (capacity까지). 카드 클릭 = 토글.
    readonly List<int> selectedIndices = new List<int>();
    // 출발 버튼 (프로그램으로 생성, 패널 하단).
    UnityEngine.UI.Button boardGoButton;
    TMPro.TMP_Text boardGoButtonLabel;

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
            spawnedCards.Add(card);
        }
        selectedIndices.Clear();
        UpdateCardStates();
        EnsureBoardGoButton();
        // 두 번째 런: OnBoardGoClicked가 SetActive(false) 해둔 버튼을 다시 켬.
        if (boardGoButton != null) boardGoButton.gameObject.SetActive(true);
        UpdateGoButton();
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

        // 채널 8 정정 — 카드 클릭 = 선택 토글. 출발은 별도 버튼.
        if (selectedIndices.Contains(cardIndex))
        {
            selectedIndices.Remove(cardIndex);
        }
        else
        {
            // 적재함 가득이면 추가 X (UpdateCardStates가 lock 처리하지만 방어)
            if (selectedIndices.Count >= BoxCapacity) return;
            selectedIndices.Add(cardIndex);
        }
        UpdateCardStates();
        UpdateGoButton();
        AudioCueBus.Publish(AudioCueId.OrderBoardSelect);

        // 세션 27 — 클릭한 카드 destination preview 즉시 표시 (호버 안 해도)
        if (selectedIndices.Count > 0)
        {
            int lastIdx = selectedIndices[selectedIndices.Count - 1];
            if (lastIdx >= 0 && lastIdx < currentOffer.Count)
                DrawPreviewLineFor(currentOffer[lastIdx]);
        }
        else
        {
            HideStartMarkerAndLine();
        }
        return;
    }

    /// <summary>채널 8 — 우측 패널 하단 "출발" 버튼 (프로그램 생성, 멱등).</summary>
    void EnsureBoardGoButton()
    {
        if (boardGoButton != null) return;
        if (panelRoot == null) return;

        // 카드 컨테이너 하단을 출발 버튼 영역만큼 띄움.
        // 버튼 영역 = anchoredPosition.y(24) + size.y(72) + 여유(20) = 116.
        // + VLG가 카드 높이를 컨테이너에 맞추도록 강제 — Unity UI는 컨테이너 rect를 clipping mask로
        //   안 써서 카드 누적 높이 > 컨테이너 높이면 overflow → 출발 버튼 침범. (2026-05-15 subagent 진단)
        if (cardsContainer != null)
        {
            var cc = cardsContainer;
            if (Mathf.Approximately(cc.anchorMin.y, 0f) && Mathf.Approximately(cc.anchorMax.y, 1f))
            {
                var min = cc.offsetMin;
                if (min.y < 116f) { min.y = 116f; cc.offsetMin = min; }
            }
            var vlg = cc.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlHeight = true;
                vlg.childForceExpandHeight = true;
            }
        }

        var goGO = new GameObject("BoardGoButton",
            typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        goGO.transform.SetParent(panelRoot.transform, false);
        var rt = goGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 24);
        rt.sizeDelta = new Vector2(-24, 72);
        var img = goGO.GetComponent<UnityEngine.UI.Image>();
        img.color = goBtnReadyColor;
        boardGoButton = goGO.GetComponent<UnityEngine.UI.Button>();
        // Button.targetGraphic + colors transition으로 interactable=false 시 회색 dim 자동
        boardGoButton.targetGraphic = img;
        var colors = boardGoButton.colors;
        colors.normalColor = goBtnReadyColor;
        colors.highlightedColor = new Color(1.00f, 0.20f, 0.10f, 1f);
        colors.pressedColor = new Color(0.70f, 0.02f, 0.00f, 1f);
        colors.disabledColor = new Color(0.30f, 0.30f, 0.34f, 1f);
        boardGoButton.colors = colors;
        boardGoButton.onClick.AddListener(OnBoardGoClicked);

        var txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(goGO.transform, false);
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        boardGoButtonLabel = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
        boardGoButtonLabel.raycastTarget = false;
        // 자동 크기: 좁아질 때 라벨 잘림/오버플로 방지
        boardGoButtonLabel.enableAutoSizing = true;
        boardGoButtonLabel.fontSizeMin = 14;
        boardGoButtonLabel.fontSizeMax = 28;
        boardGoButtonLabel.fontSize = 28;
        boardGoButtonLabel.fontStyle = TMPro.FontStyles.Bold;
        boardGoButtonLabel.alignment = TMPro.TextAlignmentOptions.Center;
        boardGoButtonLabel.color = Color.white;
        boardGoButtonLabel.textWrappingMode = TMPro.TextWrappingModes.NoWrap; // 한 줄 유지 + autoSize로 줄임
        boardGoButtonLabel.overflowMode = TMPro.TextOverflowModes.Ellipsis;
        boardGoButtonLabel.margin = new Vector4(12, 4, 12, 4); // 좌우 패딩
        boardGoButtonLabel.text = "주문을 골라주세요";
    }

    static readonly Color goBtnReadyColor = new Color(0.88f, 0.04f, 0f, 1f);

    void UpdateGoButton()
    {
        if (boardGoButton == null) return;
        int n = selectedIndices.Count;
        int cap = BoxCapacity;
        boardGoButton.interactable = n >= 1;
        if (boardGoButtonLabel != null)
        {
            if (n == 0) boardGoButtonLabel.text = $"주문을 골라주세요 (최대 {cap}개)";
            else if (n < cap) boardGoButtonLabel.text = $"출발 ({n}/{cap})";
            else boardGoButtonLabel.text = $"출발 ({n}/{cap}) — 적재함 가득";
        }
    }

    void UpdateCardStates()
    {
        int cap = BoxCapacity;
        bool full = selectedIndices.Count >= cap;
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            var card = spawnedCards[i]; if (card == null) continue;
            bool sel = selectedIndices.Contains(i);
            card.SetSelected(sel);
            // 잠금: 적재함 가득 + 이 카드 미선택 → 더 못 고름. 선택된 카드는 잠그지 않음(해제 가능).
            if (!sel && full) card.SetLocked(true, $"적재함 가득 — 다른 주문 빼기 ({cap}/{cap})");
            else card.SetLocked(false, null);
        }
    }

    void OnBoardGoClicked()
    {
        if (!isOpen) return;
        if (selectedIndices.Count == 0) return;

        // 채널 8 정정 — 픽한 주문들 → RoutePlan 또는 Packing 또는 BeginRun
        var pickedOrders = new List<OrderJob>(selectedIndices.Count);
        foreach (var idx in selectedIndices)
            if (idx >= 0 && idx < currentOffer.Count && currentOffer[idx] != null)
                pickedOrders.Add(currentOffer[idx]);
        if (pickedOrders.Count == 0) return;
        lastSelectedJob = pickedOrders[0]; // primary (Close 마커 복원에 사용)

        // 보드 닫고 다음 화면 띄움.
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);
        HideStartMarkerAndLine();
        if (boardGoButton != null) boardGoButton.gameObject.SetActive(false);

        if (Services.RoutePlan != null)
        {
            (Services.Run as RunController)?.EnterRouting();
            Services.RoutePlan.Show(pickedOrders);
        }
        else if (Services.Packing != null)
        {
            (Services.Run as RunController)?.EnterPacking();
            if (allMarkers != null)
                for (int i = 0; i < allMarkers.Count; i++)
                    if (allMarkers[i] != null) allMarkers[i].SetExternalControl(false);
            Services.Packing.Show(pickedOrders);
        }
        else if (runController != null)
        {
            if (allMarkers != null)
                for (int i = 0; i < allMarkers.Count; i++)
                    if (allMarkers[i] != null) allMarkers[i].SetExternalControl(false);
            runController.BeginRun(pickedOrders, null);
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
            // 세션 27 R1 — 카메라 180m에서도 잘 보이도록 두께 ↑ (0.4 → 3.0)
            previewLine.startWidth = 3.0f; previewLine.endWidth = 3.0f;
            previewLine.positionCount = 0;
            previewLine.useWorldSpace = true;
        }
    }

    // 세션 27 — 호버 시 도착지 위에 큰 라벨 띄움
    GameObject destLabelGO;
    TMPro.TextMeshPro destLabelText;

    void ShowDestLabel(Vector3 worldPos, string label, Color color)
    {
        if (destLabelGO == null)
        {
            destLabelGO = new GameObject("OrderBoard_DestLabel");
            destLabelText = destLabelGO.AddComponent<TMPro.TextMeshPro>();
            destLabelText.fontSize = 14f;
            destLabelText.alignment = TMPro.TextAlignmentOptions.Center;
            destLabelText.fontStyle = TMPro.FontStyles.Bold;
            destLabelGO.transform.localScale = Vector3.one * 0.8f;
        }
        destLabelGO.SetActive(true);
        destLabelGO.transform.position = worldPos + Vector3.up * 8f;
        // 카메라 향해 — 매 프레임 LookAt도 좋지만 일단 한 번
        if (Camera.main != null)
            destLabelGO.transform.rotation = Quaternion.LookRotation(destLabelGO.transform.position - Camera.main.transform.position);
        destLabelText.text = label;
        destLabelText.color = color;
    }

    void HideDestLabel()
    {
        if (destLabelGO != null) destLabelGO.SetActive(false);
    }

    void HideStartMarkerAndLine()
    {
        if (startMarker != null) startMarker.SetActive(false);
        if (previewLine != null) previewLine.positionCount = 0;
        HideDestLabel();
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
        DeliveryPoint lastDest = null;
        string firstLabel = null;
        foreach (var lbl in job.destinationLabels)
        {
            var p = dm.FindByLabel(lbl);
            if (p != null) { pts.Add(p.transform.position + Vector3.up * 0.5f); lastDest = p; if (firstLabel == null) firstLabel = lbl; }
        }
        previewLine.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++) previewLine.SetPosition(i, pts[i]);

        // 세션 27 — 도착지 위 큰 라벨
        if (lastDest != null && firstLabel != null)
            ShowDestLabel(lastDest.transform.position, firstLabel, job.color);
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
