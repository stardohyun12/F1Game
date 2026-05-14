using UnityEngine;

public static class DiagnoseCarRb
{
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }
        var rb = car.GetComponent<Rigidbody>();

        Debug.Log($"[Rb] mass={rb.mass}, drag(linear)={rb.linearDamping}, angularDamping={rb.angularDamping}");
        Debug.Log($"[Rb] isKinematic={rb.isKinematic}, useGravity={rb.useGravity}, constraints={rb.constraints}");
        Debug.Log($"[Rb] interpolation={rb.interpolation}, collisionDetection={rb.collisionDetectionMode}");
        Debug.Log($"[Rb] vel={rb.linearVelocity}, angVel={rb.angularVelocity}");

        // 차 GO의 모든 컴포넌트
        var comps = car.GetComponents<Component>();
        foreach (var c in comps) Debug.Log($"[Rb] Component: {c.GetType().Name} enabled={(c is Behaviour b ? b.enabled.ToString() : "N/A")}");

        // 즉시 AddForce 테스트
        rb.AddForce(car.transform.forward * 5000f, ForceMode.Impulse);
        Debug.Log($"[Rb] AddForce(5000 impulse) called. vel after = {rb.linearVelocity}");
    }
}
