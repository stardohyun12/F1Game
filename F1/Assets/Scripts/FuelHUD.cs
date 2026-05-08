using UnityEngine;

// Phase 2 — BarHUDBase Template Method 적용. 132줄 → 19줄.
// 학습: GDD/architecture/phase2-template-method.md

public class FuelHUD : BarHUDBase
{
    [SerializeField] FuelSystem fuel;

    protected override void OnAcquireDependency()
    {
        if (fuel == null) fuel = Services.Fuel as FuelSystem;
    }

    protected override bool HasDependency() => fuel != null;

    protected override float OnLevel() => fuel.FuelLevel;

    protected override string OnLabel(float level) =>
        fuel.IsDepleted ? "FUEL EMPTY  (F12)" : $"FUEL  {Mathf.RoundToInt(level * 100)}%";
}
