using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DeliveryManager : MonoBehaviour, IDeliveryManager
{
    [SerializeField] private List<DeliveryPoint> points = new List<DeliveryPoint>();
    [SerializeField] private Transform startReturn; // optional: return zone after last delivery

    public UnityEvent OnAllCompleted;
    public UnityEvent<DeliveryPoint> OnDeliveryCompleted;

    // IDeliveryManager 인터페이스 충족용 — 인터페이스는 property를 요구하지만
    // 기존 호출자(RunController.AddListener 등) 호환 위해 field 그대로 두고 explicit impl로 노출.
    UnityEvent<DeliveryPoint> IDeliveryManager.OnDeliveryCompleted => OnDeliveryCompleted;
    UnityEvent IDeliveryManager.OnAllCompleted => OnAllCompleted;

    // 채널 8 (2026-05-15): "앞에서부터 N개" → 플레이어가 지정한 순서 있는 경로.
    // points는 마스터 리스트(씬 8개). route는 이번 런의 실제 배달 순서.
    readonly List<DeliveryPoint> route = new List<DeliveryPoint>();

    int currentIndex = 0;
    bool returning = false;
    bool finished = false;

    public int CurrentIndex => currentIndex;
    public int TotalCount => route.Count > 0 ? route.Count : points.Count;
    public bool Returning => returning;
    public bool Finished => finished;
    public DeliveryPoint CurrentTarget => returning || finished || currentIndex >= route.Count
        ? null
        : route[currentIndex];
    public Transform ReturnTarget => startReturn;

    /// <summary>true면 RoutePlanScreen이 경로를 미리 세팅함 — RunController는 ActivateSubset 폴백 안 함.</summary>
    public bool HasPlannedRoute => route.Count > 0 && !finished;

    Transform carCache;

    void Awake()
    {
        // Phase 1: Services 경유. GameContext.Awake가 [DefaultExecutionOrder(-1000)]로 먼저.
        if (Services.Car != null) carCache = Services.Car.Transform;
    }

    void Start()
    {
        // RunController가 ActivateSubset/ActivateRoute 안 하면 전체 사용
        if (route.Count == 0) ActivateSubset(points.Count);
    }

    /// <summary>씬 DeliveryPoint를 label로 찾기 (OrderJob.destinationLabels 해석용).</summary>
    public DeliveryPoint FindByLabel(string label)
    {
        if (string.IsNullOrEmpty(label)) return null;
        for (int i = 0; i < points.Count; i++)
            if (points[i] != null && points[i].Label == label) return points[i];
        return null;
    }

    /// <summary>
    /// 채널 8 — 경로 계획 화면이 정한 순서대로 활성화.
    /// route[0]만 active, 나머지는 visible-inactive. 다 끝나면 returning/Finish 흐름은 기존과 동일.
    /// </summary>
    public void ActivateRoute(List<DeliveryPoint> orderedRoute)
    {
        route.Clear();
        if (orderedRoute != null)
            foreach (var p in orderedRoute) if (p != null) route.Add(p);

        currentIndex = 0;
        returning = false;
        finished = false;

        // 모든 points는 일단 비활성, route에 든 것만 visible + 첫 번째만 active(트리거 수락)
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null) continue;
            points[i].Bind(this, points[i].Label);
            bool used = route.Contains(points[i]);
            points[i].gameObject.SetActive(used);
            points[i].SetActive(used && route.Count > 0 && points[i] == route[0]);
        }
    }

    /// <summary>
    /// 폴백: 앞에서부터 N개 points로 route 구성. OrderBoard/RoutePlan 없는 씬용.
    /// </summary>
    public void ActivateSubset(int n)
    {
        int clamped = Mathf.Clamp(n, 0, points.Count);
        var fallback = new List<DeliveryPoint>(clamped);
        for (int i = 0; i < clamped; i++) if (points[i] != null) fallback.Add(points[i]);
        ActivateRoute(fallback);
    }

    public void OnDeliveryReached(DeliveryPoint p)
    {
        if (finished || returning) return;
        if (route.Count == 0 || currentIndex >= route.Count) return;
        if (p != route[currentIndex]) return;

        p.SetCompleted();
        OnDeliveryCompleted?.Invoke(p);
        AudioCueBus.Publish(AudioCueId.DeliveryCompleted);
        currentIndex++;

        if (currentIndex >= route.Count)
        {
            returning = (startReturn != null);
            if (!returning) Finish();
        }
        else
        {
            route[currentIndex].SetActive(true);
        }
    }

    void Update()
    {
        if (!returning || finished || startReturn == null) return;
        // simple distance check to return zone
        if (carCache == null) carCache = FindActiveCar();
        var car = carCache;
        if (car == null) return;
        if (Vector3.Distance(car.position, startReturn.position) < 4f)
        {
            Finish();
        }
    }

    void Finish()
    {
        finished = true;
        returning = false;
        OnAllCompleted?.Invoke();
        route.Clear(); // 다음 런 폴백/route 재설정 위해
        Debug.Log("DeliveryManager: all deliveries complete + returned.");
    }

    Transform FindActiveCar()
    {
        return Services.Car?.Transform;
    }

    // Editor convenience: register points programmatically (used by setup script)
    public void SetPoints(List<DeliveryPoint> list, Transform retn)
    {
        points = list;
        startReturn = retn;
    }
}
