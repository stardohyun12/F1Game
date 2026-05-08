// Phase 3 — packing tier multiplier. 5개 임계 필드 곱.
// 도현 세션 15: 일반 주행 좌절 해소 (기본 Bubble = ×1.3).
// 학습: GDD/architecture/phase3-modifier-chain.md

public class PackingModifier : ICargoModifier
{
    readonly PackingTier tier;

    public PackingModifier(PackingTier tier) { this.tier = tier; }

    public void Apply(ref CargoStats stats)
    {
        float m = PackingTierHelper.ThresholdMultiplier(tier);
        stats.longWarnG *= m;
        stats.latWarnG *= m;
        stats.spinWarnDegPerSec *= m;
        stats.tiltWarnDeg *= m;
        stats.impactWarnKmh *= m;
    }
}
