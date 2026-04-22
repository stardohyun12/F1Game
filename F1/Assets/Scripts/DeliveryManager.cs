using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DeliveryManager : MonoBehaviour
{
    [SerializeField] private List<DeliveryPoint> points = new List<DeliveryPoint>();
    [SerializeField] private Transform startReturn; // optional: return zone after last delivery

    public UnityEvent OnAllCompleted;
    public UnityEvent<DeliveryPoint> OnDeliveryCompleted;

    int currentIndex = 0;
    bool returning = false;
    bool finished = false;

    public int CurrentIndex => currentIndex;
    public int TotalCount => activeCount > 0 ? activeCount : points.Count;
    public bool Returning => returning;
    public bool Finished => finished;
    public DeliveryPoint CurrentTarget => returning || finished || currentIndex >= points.Count ? null : points[currentIndex];
    public Transform ReturnTarget => startReturn;

    int activeCount = -1; // -1 = 전체 (기본). RunController가 세팅.

    void Start()
    {
        // RunController가 ActivateSubset 안 하면 전체 사용
        if (activeCount < 0) ActivateSubset(points.Count);
    }

    /// <summary>
    /// 앞에서부터 N개 지점만 활성화. 나머지는 숨김(비활성).
    /// RunController가 OrderJob.pointCount로 호출.
    /// </summary>
    public void ActivateSubset(int n)
    {
        activeCount = Mathf.Clamp(n, 0, points.Count);
        currentIndex = 0;
        returning = false;
        finished = false;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null) continue;
            points[i].Bind(this, points[i].Label);
            // 사용할 지점 = 처음 N개. 그 안에서 첫 번째만 활성, 나머지는 비활성 (Completed는 리셋 못 — Bind에서 새로 시작)
            bool used = i < activeCount;
            points[i].gameObject.SetActive(used);
            points[i].SetActive(used && i == 0);
        }
    }

    public void OnDeliveryReached(DeliveryPoint p)
    {
        if (finished || returning) return;
        if (p != points[currentIndex]) return;

        p.SetCompleted();
        OnDeliveryCompleted?.Invoke(p);
        currentIndex++;

        int limit = activeCount > 0 ? activeCount : points.Count;
        if (currentIndex >= limit)
        {
            returning = (startReturn != null);
            if (!returning) Finish();
        }
        else
        {
            points[currentIndex].SetActive(true);
        }
    }

    void Update()
    {
        if (!returning || finished || startReturn == null) return;
        // simple distance check to return zone
        var car = FindActiveCar();
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
        Debug.Log("DeliveryManager: all deliveries complete + returned.");
    }

    Transform FindActiveCar()
    {
        var car = GameObject.FindFirstObjectByType<ArcadeCarController>();
        return car != null ? car.transform : null;
    }

    // Editor convenience: register points programmatically (used by setup script)
    public void SetPoints(List<DeliveryPoint> list, Transform retn)
    {
        points = list;
        startReturn = retn;
    }
}
