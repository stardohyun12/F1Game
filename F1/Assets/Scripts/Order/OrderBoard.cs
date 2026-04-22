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
    [SerializeField] private float cardWidth = 320f;
    [SerializeField] private float cardHeight = 200f;
    [SerializeField] private float cardSpacing = 20f;
    [SerializeField] private float rightMargin = 40f;

    bool isOpen;
    int hoveredIndex = -1;
    List<DeliveryPoint> allPoints;
    List<DeliveryPointMarker> allMarkers;

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
        CacheMarkers();
        // 보드 열린 동안엔 외부 제어 (자동 상태 무시)
        if (allMarkers != null)
            foreach (var m in allMarkers) if (m != null) m.SetExternalControl(true);
        ApplyHoverState();
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
        if (catalog == null || allMarkers == null) return;

        // 호버 인덱스에 해당하는 주문의 pointCount만큼 (씬 점 앞에서부터 N개)만 강조
        for (int i = 0; i < allMarkers.Count; i++)
        {
            if (allMarkers[i] == null) continue;
            // 호버 안 됐을 때: 모든 카드 색상 합쳐 표시 (각 카드 색상 합집합)
            // 호버 됐을 때: 해당 카드 색상만, 나머지는 dimmed
            if (hoveredIndex < 0 || hoveredIndex >= catalog.jobs.Count)
            {
                // 모든 점은 가장 큰 주문(C) 색상으로
                var fallbackJob = GetLargestJob();
                allMarkers[i].SetColor(fallbackJob != null ? fallbackJob.color : Color.white);
                allMarkers[i].SetDimmed(false);
            }
            else
            {
                var job = catalog.jobs[hoveredIndex];
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
        if (catalog == null || catalog.jobs.Count == 0) return null;
        OrderJob largest = catalog.jobs[0];
        foreach (var j in catalog.jobs) if (j != null && j.pointCount > largest.pointCount) largest = j;
        return largest;
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
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            statStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };
            statValueStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = Color.white } };
            helpStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.95f, 0.6f) } };
        }
    }

    void OnGUI()
    {
        if (!isOpen) return;
        if (catalog == null || catalog.jobs.Count == 0) return;
        EnsureStyles();

        // 상단 헤더
        var headerRect = new Rect(Screen.width / 2f - 280, 30, 560, 40);
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(headerRect, pixel);
        GUI.color = Color.white;
        GUI.Label(headerRect, "주문 선택", new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } });

        // 우측 카드 3장
        float startX = Screen.width - rightMargin - cardWidth;
        float totalH = catalog.jobs.Count * cardHeight + (catalog.jobs.Count - 1) * cardSpacing;
        float startY = (Screen.height - totalH) / 2f;

        int prevHover = hoveredIndex;
        hoveredIndex = -1;

        for (int i = 0; i < catalog.jobs.Count; i++)
        {
            var job = catalog.jobs[i];
            if (job == null) continue;

            var cardRect = new Rect(startX, startY + i * (cardHeight + cardSpacing), cardWidth, cardHeight);
            bool hovering = cardRect.Contains(Event.current.mousePosition);
            if (hovering) hoveredIndex = i;

            DrawCard(cardRect, job, hovering);
        }

        if (prevHover != hoveredIndex) ApplyHoverState();

        // 클릭 처리
        if (Event.current.type == EventType.MouseDown && hoveredIndex >= 0)
        {
            lastSelectedJob = catalog.jobs[hoveredIndex];
            if (runController != null) runController.BeginRun(lastSelectedJob);
        }

        // 좌하단 안내
        var helpRect = new Rect(40, Screen.height - 60, 520, 30);
        GUI.Label(helpRect, "카드에 마우스 올리면 맵에 배달지가 표시됨 · 클릭으로 출발", helpStyle);
    }

    void DrawCard(Rect r, OrderJob job, bool hovering)
    {
        // 배경
        GUI.color = hovering ? new Color(job.color.r, job.color.g, job.color.b, 0.25f) : new Color(0, 0, 0, 0.55f);
        GUI.DrawTexture(r, pixel);

        // 색상 띠 (좌측)
        GUI.color = job.color;
        GUI.DrawTexture(new Rect(r.x, r.y, 8f, r.height), pixel);

        GUI.color = Color.white;

        // 제목
        var titleRect = new Rect(r.x + 24, r.y + 12, r.width - 48, 36);
        GUI.Label(titleRect, job.displayName, titleStyle);

        // 스탯 4행
        float rowH = 28f;
        float rowY = r.y + 56;
        DrawRow(r, rowY + 0 * rowH, "지점", $"{job.pointCount}곳");
        DrawRow(r, rowY + 1 * rowH, "시간", job.timeLimitSec > 0f ? $"{Mathf.RoundToInt(job.timeLimitSec / 60f)}분 {Mathf.RoundToInt(job.timeLimitSec % 60f):D2}초" : "무제한");
        string rewardStr = job.timeBonus > 0
            ? $"₩{job.baseReward:N0} + 보너스 ₩{job.timeBonus:N0}"
            : $"₩{job.baseReward:N0}";
        DrawRow(r, rowY + 2 * rowH, "보상", rewardStr);
        DrawRow(r, rowY + 3 * rowH, "무게", job.WeightLabel);
    }

    void DrawRow(Rect card, float y, string label, string val)
    {
        GUI.Label(new Rect(card.x + 24, y, 100, 24), label, statStyle);
        GUI.Label(new Rect(card.x + card.width - 24 - 200, y, 200, 24), val, statValueStyle);
    }
}
