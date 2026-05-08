// Phase 3 — Modifier Chain용 stats 캐리어.
// CargoBox의 11개 임계 + 1 HP 필드를 한 struct에 모음.
// ICargoModifier들이 ref로 받아 변경, CargoBox는 마지막에 자기 필드로 복사.
// 학습: GDD/architecture/phase3-modifier-chain.md

public struct CargoStats
{
    public float longWarnG;
    public float longDamageMul;
    public float latWarnG;
    public float latDamageMul;
    public float spinWarnDegPerSec;
    public float spinDamageMul;
    public float tiltWarnDeg;
    public float tiltDamageMul;
    public float impactWarnKmh;
    public float impactDamageMul;
    public float cumulativeMul;
    public float maxHP;
}
