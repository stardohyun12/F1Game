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
    public int TotalCount => points.Count;
    public bool Returning => returning;
    public bool Finished => finished;
    public DeliveryPoint CurrentTarget => returning || finished || currentIndex >= points.Count ? null : points[currentIndex];
    public Transform ReturnTarget => startReturn;

    void Start()
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null) continue;
            points[i].Bind(this, points[i].Label);
            points[i].SetActive(i == 0);
        }
    }

    public void OnDeliveryReached(DeliveryPoint p)
    {
        if (finished || returning) return;
        if (p != points[currentIndex]) return;

        p.SetCompleted();
        OnDeliveryCompleted?.Invoke(p);
        currentIndex++;

        if (currentIndex >= points.Count)
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
