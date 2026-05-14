using UnityEditor;
using UnityEngine;

public static class DiagnoseCarInput
{
    public static void Execute()
    {
        Debug.Log($"[Car] Time.timeScale = {Time.timeScale}");

        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("[Car] no controller"); return; }

        Debug.Log($"[Car] controller.enabled={car.enabled}, activeInHierarchy={car.gameObject.activeInHierarchy}");
        Debug.Log($"[Car] CurrentSpeedMps={car.CurrentSpeedMps:F2}, MaxSpeedMps={car.MaxSpeedMps:F2}");

        var rb = car.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"[Car] RB mass={rb.mass}, drag={rb.linearDamping}, angularDamping={rb.angularDamping}, kinematic={rb.isKinematic}, useGravity={rb.useGravity}");
            Debug.Log($"[Car] RB constraints={rb.constraints}, vel={rb.linearVelocity}, angVel={rb.angularVelocity}");
        }

        // Force 강제 적용 — 1초간 forward 가속
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(car.transform.forward * 5000f, ForceMode.Impulse);
            Debug.Log("[Car] Applied 5000N forward impulse — should accelerate if physics OK.");
        }

        // Input system 활성?
        Debug.Log($"[Car] InputSystem enabled={UnityEngine.InputSystem.InputSystem.settings != null}");
    }
}
