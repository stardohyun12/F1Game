using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 채널 8 — 경로 계획 화면 (2026-05-15 신규).
/// OrderBoard에서 잡을 고른 직후 뜨고, 플레이어가 맵 위 목적지 마커들을 배달
/// 순서대로 클릭한다. 확정하면 Services.Delivery.ActivateRoute → PackingScreen.
///
/// 자체 ScreenSpaceOverlay 캔버스 (PackingScreen 패턴). 맵은 OverviewCamera 활용
/// (별도 카메라 없음 — OrderBoard에서 이어진 오버뷰 그대로).
///
/// spec: 옵시디언/dev-plan/2026-05-15-order-route-flow-design.md
/// </summary>
public sealed class RoutePlanScreen : MonoBehaviour, IRoutePlanScreen
{
    GameObject panelRoot;
    // 채널 8 정정: 한 런 = 여러 주문. currentJob은 primary(=orders[0]) — title 색상 등 호환.
    OrderJob currentJob;
    readonly List<OrderJob> currentOrders = new();
    Camera mapCamera;
    DeliveryManager dm;
    Transform startPoint;
    GameObject startMarker;
    LineRenderer routeLine;

    // 이 런의 목적지 마커들 (강조 대상). jobMarkers[i] ↔ currentOrders[i] 평행.
    readonly List<DeliveryPointMarker> jobMarkers = new();
    // 마커 → 주문 역매핑 (확정 시 orderedOrders 만들기 위해).
    // 1:N — 여러 주문이 같은 destination을 공유할 수 있음 (Order_C_Risky+Order_02_Glass 둘 다 EastHub 같은 케이스).
    readonly Dictionary<DeliveryPoint, List<OrderJob>> markerToOrder = new();
    // 플레이어가 클릭 순서대로 쌓는 경로
    readonly List<DeliveryPoint> orderedRoute = new();

    // UI 참조
    TMP_Text titleText, listText, distanceText;
    Button confirmBtn, suggestBtn, resetBtn;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    // =========================================================
    //  Public API
    // =========================================================
    public void Show(OrderJob job)
    {
        Show(new List<OrderJob> { job });
    }

    /// <summary>채널 8 정정 — 여러 주문 동시 경로 계획.</summary>
    public void Show(List<OrderJob> orders)
    {
        if (orders == null || orders.Count == 0)
        {
            Debug.LogWarning("[RoutePlanScreen] Show: orders 비어있음"); return;
        }
        currentOrders.Clear();
        currentOrders.AddRange(orders);
        currentJob = currentOrders[0]; // primary (title 색상 등)
        EnsureRefs();
        BuildUI();
        ResolveJobMarkers();
        ShowStartMarker(true);
        ResetOrder();
        if (panelRoot != null) panelRoot.SetActive(true);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void Hide()
    {
        ClearOrderBadges();
        ShowStartMarker(false);
        if (routeLine != null) routeLine.positionCount = 0;
        // 잡 마커들 외부 제어 해제 (자동 상태로 복귀)
        foreach (var m in jobMarkers) if (m != null) m.SetExternalControl(false);
        var all = UnityEngine.Object.FindObjectsByType<DeliveryPointMarker>(FindObjectsSortMode.None);
        foreach (var m in all) if (m != null) m.SetExternalControl(false);
        jobMarkers.Clear();
        orderedRoute.Clear();
        if (panelRoot != null) { Destroy(panelRoot); panelRoot = null; }
        // 참조 초기화
        titleText = listText = distanceText = null;
        confirmBtn = suggestBtn = resetBtn = null;
    }

    // =========================================================
    //  Refs / Markers
    // =========================================================
    void EnsureRefs()
    {
        if (mapCamera == null)
        {
            var ov = Services.Overview as OverviewCamera;
            if (ov != null) mapCamera = ov.GetComponent<Camera>();
            if (mapCamera == null) mapCamera = Camera.main;
        }
        if (dm == null) dm = Services.Delivery as DeliveryManager;
        if (startPoint == null)
        {
            var race = GameObject.Find("Race");
            if (race != null) startPoint = race.transform;
        }
    }

    void ResolveJobMarkers()
    {
        jobMarkers.Clear();
        markerToOrder.Clear();
        if (dm == null || currentOrders.Count == 0) return;

        // 채널 8 정정 — 각 주문이 1개 destinationLabel을 가짐. 주문별로 마커 해석.
        foreach (var order in currentOrders)
        {
            if (order == null || order.destinationLabels == null || order.destinationLabels.Count == 0) continue;
            var label = order.destinationLabels[0];
            var dp = dm.FindByLabel(label);
            if (dp == null) continue;
            var m = dp.GetComponent<DeliveryPointMarker>();
            if (m == null) continue;
            m.SetExternalControl(true);
            m.SetVisible(true);
            m.SetColor(order.color); // 주문별 색상
            m.SetDimmed(false);
            m.ClearRouteOrder();
            if (!jobMarkers.Contains(m)) jobMarkers.Add(m); // 같은 마커 1번만 강조
            if (!markerToOrder.TryGetValue(dp, out var list))
            {
                list = new List<OrderJob>();
                markerToOrder[dp] = list;
            }
            list.Add(order);
        }
        // 이 런 외의 다른 마커들은 dim
        var allMarkers = UnityEngine.Object.FindObjectsByType<DeliveryPointMarker>(FindObjectsSortMode.None);
        foreach (var am in allMarkers)
        {
            if (jobMarkers.Contains(am)) continue;
            am.SetExternalControl(true);
            am.SetDimmed(true);
            am.ClearRouteOrder();
        }
    }

    void ShowStartMarker(bool show)
    {
        if (startPoint == null) return;
        if (startMarker == null)
        {
            startMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            startMarker.name = "RoutePlan_StartMarker";
            DestroyImmediate(startMarker.GetComponent<Collider>());
            startMarker.transform.position = startPoint.position + Vector3.up * 1.5f;
            startMarker.transform.localScale = new Vector3(2f, 3f, 2f);
            var mr = startMarker.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = new Color(1f, 0.9f, 0.2f, 1f);
            mr.sharedMaterial = mat;
        }
        startMarker.SetActive(show);
    }

    void EnsureRouteLine()
    {
        if (routeLine != null) return;
        var lrGO = new GameObject("RoutePlan_RouteLine");
        routeLine = lrGO.AddComponent<LineRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        routeLine.material = mat;
        Color c = currentJob != null ? currentJob.color : Color.yellow;
        routeLine.startColor = c; routeLine.endColor = c;
        routeLine.startWidth = 0.5f; routeLine.endWidth = 0.5f;
        routeLine.positionCount = 0;
        routeLine.useWorldSpace = true;
    }

    // =========================================================
    //  Input
    // =========================================================
    void Update()
    {
        if (!IsVisible || mapCamera == null) return;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        // UI 위 클릭은 무시 (버튼 처리 보호)
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null && es.IsPointerOverGameObject()) return;
        Vector2 sp = Mouse.current.position.ReadValue();
        HandleMapClick(sp);
#endif
    }

    void HandleMapClick(Vector2 screenPos)
    {
        Physics.SyncTransforms();
        var ray = mapCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, 500f, ~0, QueryTriggerInteraction.Collide)) return;
        var dp = hit.collider.GetComponentInParent<DeliveryPoint>();
        if (dp == null) return;
        var marker = dp.GetComponent<DeliveryPointMarker>();
        if (marker == null || !jobMarkers.Contains(marker)) return;
        if (orderedRoute.Contains(dp)) return; // 이미 순서에 있음
        orderedRoute.Add(dp);
        marker.SetRouteOrder(orderedRoute.Count);
        RefreshAfterChange();
    }

    // =========================================================
    //  Suggest / Reset / Confirm
    // =========================================================
    void OnSuggest()
    {
        orderedRoute.Clear();
        ClearOrderBadges();
        var remaining = new List<DeliveryPoint>();
        foreach (var m in jobMarkers)
        {
            if (m == null) continue;
            var d = m.GetComponent<DeliveryPoint>();
            if (d != null) remaining.Add(d);
        }
        Vector3 cur = startPoint != null ? startPoint.position : Vector3.zero;
        while (remaining.Count > 0)
        {
            DeliveryPoint best = null;
            float bestDist = float.MaxValue;
            foreach (var d in remaining)
            {
                float dist = Vector2.Distance(
                    new Vector2(cur.x, cur.z),
                    new Vector2(d.transform.position.x, d.transform.position.z));
                if (dist < bestDist) { bestDist = dist; best = d; }
            }
            if (best == null) break;
            remaining.Remove(best);
            orderedRoute.Add(best);
            var m = best.GetComponent<DeliveryPointMarker>();
            if (m != null) m.SetRouteOrder(orderedRoute.Count);
            cur = best.transform.position;
        }
        RefreshAfterChange();
    }

    void OnReset()
    {
        orderedRoute.Clear();
        ClearOrderBadges();
        RefreshAfterChange();
    }

    void OnConfirm()
    {
        if (orderedRoute.Count != jobMarkers.Count || orderedRoute.Count == 0) return;
        if (dm == null) dm = Services.Delivery as DeliveryManager;
        if (dm != null) dm.ActivateRoute(new List<DeliveryPoint>(orderedRoute));

        // 채널 8 정정 — orderedRoute의 순서에 맞춰 orders도 재정렬해서 핸드오프
        // (PackingScreen이 각 주문 1개씩 큐브를 만들고, 운전에서 1→2→3 순으로 배달됨).
        var orderedOrders = new List<OrderJob>(orderedRoute.Count);
        foreach (var dp in orderedRoute)
        {
            // 1:N — 같은 마커에 묶인 모든 주문을 그 순서 자리에서 펼침
            if (markerToOrder.TryGetValue(dp, out var list) && list != null)
                foreach (var o in list) if (o != null) orderedOrders.Add(o);
        }
        Hide();
        if (Services.Packing != null)
        {
            (Services.Run as RunController)?.EnterPacking();
            Services.Packing.Show(orderedOrders);
        }
        else if (Services.Run != null) Services.Run.BeginRun(orderedOrders, null);
    }

    void ClearOrderBadges()
    {
        foreach (var m in jobMarkers) if (m != null) m.ClearRouteOrder();
    }

    void RefreshAfterChange()
    {
        // 리스트 텍스트
        if (listText != null)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < orderedRoute.Count; i++)
                sb.AppendLine($"{i + 1}. {orderedRoute[i].Label}");
            listText.text = sb.ToString();
        }
        // 직선 거리 합 (XZ)
        float total = 0f;
        Vector3 prev = startPoint != null ? startPoint.position : Vector3.zero;
        foreach (var d in orderedRoute)
        {
            total += Vector2.Distance(
                new Vector2(prev.x, prev.z),
                new Vector2(d.transform.position.x, d.transform.position.z));
            prev = d.transform.position;
        }
        if (distanceText != null) distanceText.text = $"총 거리 약 {total:F0}m";

        // 라인
        EnsureRouteLine();
        if (routeLine != null)
        {
            routeLine.startColor = routeLine.endColor =
                currentJob != null ? currentJob.color : Color.yellow;
            routeLine.positionCount = orderedRoute.Count + (orderedRoute.Count > 0 ? 1 : 0);
            int idx = 0;
            if (orderedRoute.Count > 0)
            {
                Vector3 sp = startPoint != null ? startPoint.position + Vector3.up * 0.5f : Vector3.zero;
                routeLine.SetPosition(idx++, sp);
                foreach (var d in orderedRoute)
                    routeLine.SetPosition(idx++, d.transform.position + Vector3.up * 0.5f);
            }
        }

        if (confirmBtn != null)
            confirmBtn.interactable = (orderedRoute.Count == jobMarkers.Count && orderedRoute.Count > 0);
    }

    void ResetOrder()
    {
        orderedRoute.Clear();
        ClearOrderBadges();
        RefreshAfterChange();
    }

    // =========================================================
    //  Build UI
    // =========================================================
    void BuildUI()
    {
        if (panelRoot != null) return;
        var canvasGO = new GameObject("RoutePlanCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1f;
        panelRoot = canvasGO;
        EnsureEventSystem();
        BuildSidePanel(panelRoot.transform);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
    }

    void BuildSidePanel(Transform canvas)
    {
        var panelGO = new GameObject("RouteSidePanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvas, false);
        var prt = panelGO.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(1, 0); prt.anchorMax = new Vector2(1, 1);
        prt.pivot = new Vector2(1, 0.5f);
        prt.sizeDelta = new Vector2(340, 0); prt.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.92f);

        titleText = MakeLabel(panelGO.transform, "Title",
            currentJob != null ? $"경로 계획 — {currentJob.displayName}" : "경로 계획",
            new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(300, 60),
            24, FontStyles.Bold, TextAlignmentOptions.Center);

        MakeLabel(panelGO.transform, "Hint", "맵의 목적지를 배달 순서대로 클릭",
            new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(300, 40),
            14, FontStyles.Normal, TextAlignmentOptions.Center);

        listText = MakeLabel(panelGO.transform, "List", "",
            new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(300, 200),
            18, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        distanceText = MakeLabel(panelGO.transform, "Distance", "",
            new Vector2(0.5f, 0f), new Vector2(0, 240), new Vector2(300, 30),
            16, FontStyles.Normal, TextAlignmentOptions.Center);

        suggestBtn = MakeButton(panelGO.transform, "Suggest", "추천 순서",
            new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(140, 36));
        suggestBtn.onClick.AddListener(OnSuggest);

        resetBtn = MakeButton(panelGO.transform, "Reset", "재설정",
            new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(140, 36));
        resetBtn.onClick.AddListener(OnReset);

        confirmBtn = MakeButton(panelGO.transform, "Confirm", "확정",
            new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(280, 56));
        confirmBtn.onClick.AddListener(OnConfirm);
        var cbg = confirmBtn.GetComponent<Image>();
        if (cbg != null) cbg.color = new Color(0.88f, 0.04f, 0f, 1f);
        confirmBtn.interactable = false;
    }

    static TMP_Text MakeLabel(Transform parent, string name, string text, Vector2 anchor,
        Vector2 anchoredPos, Vector2 size, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, anchor.y);
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        t.text = text; t.fontSize = fontSize; t.fontStyle = style; t.alignment = align;
        t.color = Color.white;
        return t;
    }

    static Button MakeButton(Transform parent, string name, string label,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = new Color(0.18f, 0.20f, 0.25f, 1f);
        var txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.raycastTarget = false;
        txt.text = label; txt.fontSize = 20; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center; txt.color = Color.white;
        return go.GetComponent<Button>();
    }
}
