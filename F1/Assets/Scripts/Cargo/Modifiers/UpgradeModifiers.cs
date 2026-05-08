using System;
using System.Collections.Generic;

// Phase 3 — 업그레이드별 ICargoModifier + Registry.
// 새 업그레이드 추가 = 새 클래스 + Registry 한 줄. CargoBox.ApplyPreset 변경 0 (OCP).
// 학습: GDD/architecture/phase3-modifier-chain.md

public class SuspensionUpgradeModifier : ICargoModifier
{
    public void Apply(ref CargoStats stats)
    {
        stats.longWarnG += 0.1f;
        stats.latWarnG += 0.1f;
    }
}

public class SmoothSteeringUpgradeModifier : ICargoModifier
{
    public void Apply(ref CargoStats stats)
    {
        stats.spinWarnDegPerSec += 20f;
    }
}

public class ReinforcedTrunkUpgradeModifier : ICargoModifier
{
    public void Apply(ref CargoStats stats)
    {
        stats.maxHP += 20f;
    }
}

/// <summary>UpgradeEffect → modifier factory 매핑. 새 업그레이드 추가 시 여기 한 줄만.</summary>
public static class UpgradeModifierRegistry
{
    static readonly Dictionary<UpgradeEffect, Func<ICargoModifier>> map = new()
    {
        { UpgradeEffect.SuspensionUpgrade,  () => new SuspensionUpgradeModifier() },
        { UpgradeEffect.SmoothSteering,     () => new SmoothSteeringUpgradeModifier() },
        { UpgradeEffect.ReinforcedTrunk,    () => new ReinforcedTrunkUpgradeModifier() },
        // 새 업그레이드 추가 = 여기 한 줄. CargoBox 안 건드림.
    };

    /// <summary>해당 효과의 modifier 인스턴스 생성. CargoBox에 안 적용되는 효과(Phase 2 후보)는 null 반환.</summary>
    public static ICargoModifier Create(UpgradeEffect effect)
        => map.TryGetValue(effect, out var factory) ? factory() : null;
}
