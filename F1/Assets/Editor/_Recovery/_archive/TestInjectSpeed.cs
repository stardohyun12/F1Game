using UnityEngine;
using UnityEditor;

public static class TestInjectSpeed
{
    [MenuItem("PitCrew/Test/Inject 50 km/h")]
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("[Test] Car not found"); return; }
        var rb = car.GetComponent<Rigidbody>();
        if (rb == null) { Debug.LogError("[Test] Rigidbody not found"); return; }
        float mps = 50f / 3.6f; // 50 km/h
        rb.linearVelocity = car.transform.forward * mps;
        Debug.Log($"[Test] Injected velocity: {rb.linearVelocity}, mag={rb.linearVelocity.magnitude:F1} m/s ({rb.linearVelocity.magnitude * 3.6f:F0} km/h)");
    }

    [MenuItem("PitCrew/Test/Inject 85 km/h (high speed)")]
    public static void ExecuteHigh()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) return;
        var rb = car.GetComponent<Rigidbody>();
        if (rb == null) return;
        float mps = 85f / 3.6f;
        rb.linearVelocity = car.transform.forward * mps;
        Debug.Log($"[Test] Injected HIGH velocity: {rb.linearVelocity.magnitude * 3.6f:F0} km/h");
    }

    [MenuItem("PitCrew/Test/Drain Fuel to 8%")]
    public static void DrainFuel()
    {
        var fs = Object.FindFirstObjectByType<FuelSystem>();
        if (fs == null) return;
        var field = typeof(FuelSystem).GetField("fuelLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(fs, 0.08f);
        Debug.Log($"[Test] Fuel drained to 8% (crit)");
    }

    [MenuItem("PitCrew/Test/Drain Fuel to 20%")]
    public static void DrainFuelWarn()
    {
        var fs = Object.FindFirstObjectByType<FuelSystem>();
        if (fs == null) return;
        var field = typeof(FuelSystem).GetField("fuelLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(fs, 0.20f);
        Debug.Log($"[Test] Fuel drained to 20% (warn)");
    }
}
