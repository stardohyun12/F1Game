using UnityEngine;
using UnityEditor;

/// <summary>
/// Play 중 차량을 지속적으로 전진시키는 테스트용 MonoBehaviour.
/// ArcadeCarController의 FixedUpdate가 drag를 적용해도 이 스크립트가 매 프레임 velocity 유지.
/// </summary>
public class _TestDriveLoop : MonoBehaviour
{
    public float targetKmh = 80f;
    ArcadeCarController car;
    Rigidbody rb;

    void Start()
    {
        car = GetComponent<ArcadeCarController>();
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb == null || car == null) return;
        float mps = targetKmh / 3.6f;
        // 전진 방향 velocity 유지 — drag 상쇄
        rb.linearVelocity = car.transform.forward * mps;
    }
}

public static class TestDriveLoop
{
    [MenuItem("PitCrew/Test/Drive at 80 km/h (loop)")]
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("[Test] Car not found"); return; }
        var existing = car.GetComponent<_TestDriveLoop>();
        if (existing == null)
        {
            var loop = car.gameObject.AddComponent<_TestDriveLoop>();
            loop.targetKmh = 80f;
        }
        else
        {
            existing.targetKmh = 80f;
        }
        Debug.Log($"[Test] Drive loop @80km/h active");
    }

    [MenuItem("PitCrew/Test/Stop Drive Loop")]
    public static void Stop()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) return;
        var loop = car.GetComponent<_TestDriveLoop>();
        if (loop != null) Object.Destroy(loop);
        Debug.Log("[Test] Drive loop stopped");
    }
}
