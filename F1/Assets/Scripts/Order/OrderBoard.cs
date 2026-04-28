using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시작 / 재시작 시 뜨는 주문 보드.
/// 우측 세로 카드 3장. 호버 시 해당 주문의 마커만 강조, 나머지 흐림.
/// </summary>
public class OrderBoard : MonoBehaviour
{
    [SerializeField] private OrderCatalog catalog;
    [SerializeField] private RunController runController;

    [Header("Layout")]
    // 도현 #2-16: 카드 글자 패널 벗어남 → 폭 360→480 + 높이 280→320 (한글 줄 충분).
    [SerializeField] private float cardWidth = 480f;
    [SerializeField] private float cardHeight = 320f;
    [SerializeField] private float cardSpacing = 20f;
    [SerializeField] private float rightMargin = 40f;

    bool isOpen;
    int hoveredIndex = -1;
    List<DeliveryPoint> allPoints;
    List<DeliveryPointMarker> allMarkers;

    [Header("Day 26 — Shuffle Pick")]
    [Tooltip("매 보드 오픈마다 catalog.jobs 풀에서 N개 무작위 노출. 0이면 풀 전체.")]
    [SerializeField] private int offerCount = 3;
    readonly List<OrderJob> currentOffer = new List<OrderJob>();

    Texture2D pixel;
    GUIStyle titleStyle, statStyle, statValueStyle, helpStyle;

    void Awake()
    {
        if (runController == null) runController = FindFirstObjectByType<RunController>();
    }

    public void Open()
    {
        isOpen = true;
        hoveredIndex = -1;
        PickOffer();   // Day 26: 풀에서 무작위 N개 픽
        CacheMarkers();
        // 보드 열린 동안엔 외부 제어 (자동 상태 무시)
        if (allMarkers != null)
            foreach (var m in allMarkers) if (m != null) m.SetExternalControl(true);
        ApplyHoverState();
    }

    /// <summary>Day 26 — catalog.jobs 풀에서 offerCount개 무작위 픽 (중복 X).</summary>
    void PickOffer()
    {
        currentOffer.Clear();
        if (catalog == null || catalog.jobs == null || catalog.jobs.Count == 0) return;

        int n = offerCount > 0 ? Mathf.Min(offerCount, catalog.jobs.Count) : catalog.jobs.Count;
        if (catalog.jobs.Count <= n)
        {
            // 풀이 작으면 전체 표시
            foreach (var j in catalog.jobs) if (j != null) currentOffer.Add(j);
            return;
        }

        // Fisher-Yates 부분 셔플 (앞 n개만 셔플)
        var pool = new List<OrderJob>(catalog.jobs);
        for (int i = 0; i < n; i++)
        {
            int idx = Random.Range(i, pool.Count);
            (pool[i], pool[idx]) = (pool[idx], pool[i]);
            currentOffer.Add(pool[i]);
        }
    }

    public void Close()
    {
        isOpen = false;
        // 런 중에도 마커는 보임. 자동 상태 모드로 (DeliveryPointMarker.Update가 IsActive/IsCompleted 따라 색/펄스 처리).
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
                    allMarkers[i].SetExternalControl(false); // 이제 자동 상태 (current target 펄스)
                }
            }
        }
    }

    OrderJob lastSelectedJob;

    void CacheMarkers()
    {
        // DeliveryManager의 points 리스트 가져오기
        var mgr = FindFirstObjectByType<DeliveryManager>();
        allPoints = new List<DeliveryPoint>();
        allMarkers = new List<DeliveryPointMarker>();
        if (mgr == null) return;

        // DeliveryManager에서 points 직접 접근 못 하니 씬에서 모두 찾음
        var points = FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None);
        foreach (var p in points)
        {
            allPoints.Add(p);
            var marker = p.GetComponent<DeliveryPointMarker>();
            if (marker == null) marker = p.gameObject.AddComponent<DeliveryPointMarker>();
            marker.SetVisible(true);
            allMarkers.Add(marker);
        }
    }

    void ApplyHoverState()
    {
        if (allMarkers == null) return;
        // Day 26: currentOffer 기반 (3개)으로 변경
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
        // currentOffer 우선, fallback catalog.jobs 전체
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

    void EnsureStyles()
    {
        if (pixel == null)
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }
        if (titleStyle == null)
        {
            // 도현 #2-16: 글자가 패널과 색 비슷해서 안 보임 → 강한 대비 + fontSize ↑.
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(1f, 0.97f, 0.85f) } };
            statStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.95f, 0.95f, 0.95f) } };
            statValueStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(1f, 0.92f, 0.40f) } };
            helpStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.95f, 0.6f) } };
        }
    }

    void OnGUI()
    {
        if (!isOpen) return;
        // Day 26: currentOffer 기반
        if (currentOffer.Count == 0)
        {
            // fallback — currentOffer 비어있으면 catalog.jobs 직접 사용
            if (catalog == null || catalog.jobs == null || catalog.jobs.Count == 0) return;
            currentOffer.AddRange(catalog.jobs);
        }
        EnsureStyles();

        // 상단 헤더
        var headerRect = new Rect(Screen.width / 2f - 280, 30, 560, 40);
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(headerRect, pixel);
        GUI.color = Color.white;
        GUI.Label(headerRect, "주문 선택", new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } });

        // 우측 카드 N장 (Day 26: currentOffer 기반)
        float startX = Screen.width - rightMargin - cardWidth;
        int count = currentOffer.Count;
        float totalH = count * cardHeight + (count - 1) * cardSpacing;
        float startY = (Screen.height - totalH) / 2f;

        int prevHover = hoveredIndex;
        hoveredIndex = -1;

        for (int i = 0; i < count; i++)
        {
            var job = currentOffer[i];
            if (job == null) continue;

            var cardRect = new Rect(startX, startY + i * (cardHeight + cardSpacing), cardWidth, cardHeight);
            bool hovering = cardRect.Contains(Event.current.mousePosition);
            if (hovering) hoveredIndex = i;

            DrawCard(cardRect, job, hovering);
        }

        if (prevHover != hoveredIndex) ApplyHoverState();

        // 클릭 처리 — 세션 16 B3: BeginRun 직접 X. PreRunSetupPanel(화물·포장 선택) 거침.
        if (Event.current.type == EventType.MouseDown && hoveredIndex >= 0)
        {
            lastSelectedJob = currentOffer[hoveredIndex];
            // 보드 닫고 setup 패널 띄움 (timeScale=0 유지)
            isOpen = false;
            if (allMarkers != null && lastSelectedJob != null)
            {
                for (int i = 0; i < allMarkers.Count; i++)
                {
                    if (allMarkers[i] != null) allMarkers[i].SetExternalControl(false);
                }
            }
            var setup = FindFirstObjectByType<PreRunSetupPanel>();
            if (setup != null)
            {
                setup.Show(lastSelectedJob);
            }
            else if (runController != null)
            {
                // 폴백: setup 패널 없으면 기존 흐름.
                runController.BeginRun(lastSelectedJob);
            }
        }

        // 좌하단 안내
        var helpRect = new Rect(40, Screen.height - 60, 520, 30);
        GUI.Label(helpRect, "카드에 마우스 올리면 맵에 배달지가 표시됨 · 클릭으로 출발", helpStyle);
    }

    void DrawCard(Rect r, OrderJob job, bool hovering)
    {
        // 도현 #2-16: 배경 어둡게 강화 (0.55→0.88) + hover시 띠 색 stronger.
        GUI.color = hovering
            ? new Color(0.18f, 0.20f, 0.30f, 0.95f)
            : new Color(0.05f, 0.05f, 0.10f, 0.88f);
        GUI.DrawTexture(r, pixel);

        // 색상 띠 (좌측, 두껍게 12px)
        GUI.color = job.color;
        GUI.DrawTexture(new Rect(r.x, r.y, 12f, r.height), pixel);

        // 호버 시 우측 강조 띠
        if (hovering)
        {
            GUI.color = new Color(job.color.r, job.color.g, job.color.b, 0.6f);
            GUI.DrawTexture(new Rect(r.x + r.width - 6f, r.y, 6f, r.height), pixel);
        }

        GUI.color = Color.white;

        // 제목
        var titleRect = new Rect(r.x + 30, r.y + 14, r.width - 60, 40);
        GUI.Label(titleRect, job.displayName, titleStyle);

        // 스탯 6행 (Day 11: 화물 행 추가, Day 15: 포장 행 추가)
        float rowH = 32f;
        float rowY = r.y + 64;
        DrawRow(r, rowY + 0 * rowH, "지점", $"{job.pointCount}곳");
        DrawRow(r, rowY + 1 * rowH, "시간", job.timeLimitSec > 0f ? $"{Mathf.RoundToInt(job.timeLimitSec / 60f)}분 {Mathf.RoundToInt(job.timeLimitSec % 60f):D2}초" : "무제한");
        string rewardStr = job.timeBonus > 0
            ? $"₩{job.baseReward:N0} + ₩{job.timeBonus:N0}"
            : $"₩{job.baseReward:N0}";
        DrawRow(r, rowY + 2 * rowH, "보상", rewardStr);
        DrawRow(r, rowY + 3 * rowH, "무게", job.WeightLabel);
        DrawRow(r, rowY + 4 * rowH, "화물", CargoTypePresets.DisplayName(job.cargoType));
        DrawRow(r, rowY + 5 * rowH, "포장", PackingTierHelper.DisplayName(job.defaultPacking));

        // 화물 운전 가이드 + 포장 가이드 (별도 줄, 폰트 작게, 색 노란)
        var tipStyle = new GUIStyle(statStyle) {
            fontSize = 16, normal = { textColor = new Color(1f, 0.85f, 0.5f) },
            wordWrap = true, alignment = TextAnchor.UpperLeft
        };
        var tipRect = new Rect(r.x + 30, rowY + 6 * rowH + 8, r.width - 60, 50);
        GUI.Label(tipRect, $"→ {CargoTypePresets.DriveTip(job.cargoType)}\n→ {PackingTierHelper.DriveTip(job.defaultPacking)}", tipStyle);
    }

    void DrawRow(Rect card, float y, string label, string val)
    {
        // #2-16: 라벨 폭 80→100, 값 폭 200→320 (긴 보상 문자열 수용).
        GUI.Label(new Rect(card.x + 30, y, 100, 28), label, statStyle);
        GUI.Label(new Rect(card.x + card.width - 30 - 320, y, 320, 28), val, statValueStyle);
    }
}
