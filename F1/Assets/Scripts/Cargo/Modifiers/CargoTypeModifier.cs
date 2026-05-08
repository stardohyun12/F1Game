// Phase 3 — baseline modifier. CargoTypePresets에서 임계값 11개 + maxHP 가져옴.
// Chain의 첫 단계. 다른 modifier들은 이 위에 곱·덧셈으로 변형.
// 학습: GDD/architecture/phase3-modifier-chain.md

public class CargoTypeModifier : ICargoModifier
{
    readonly CargoType type;
    readonly float baselineMaxHP;

    public CargoTypeModifier(CargoType type, float baselineMaxHP)
    {
        this.type = type;
        this.baselineMaxHP = baselineMaxHP;
    }

    public void Apply(ref CargoStats stats)
    {
        var p = CargoTypePresets.Get(type);
        stats.longWarnG = p.longWarnG;
        stats.longDamageMul = p.longDamageMul;
        stats.latWarnG = p.latWarnG;
        stats.latDamageMul = p.latDamageMul;
        stats.spinWarnDegPerSec = p.spinWarnDegPerSec;
        stats.spinDamageMul = p.spinDamageMul;
        stats.tiltWarnDeg = p.tiltWarnDeg;
        stats.tiltDamageMul = p.tiltDamageMul;
        stats.impactWarnKmh = p.impactWarnKmh;
        stats.impactDamageMul = p.impactDamageMul;
        stats.cumulativeMul = p.cumulativeMul;
        stats.maxHP = baselineMaxHP;   // 재호출 시 항상 baseline으로 리셋 (D-021 cumulative 버그 방지)
    }
}
