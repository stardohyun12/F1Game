// Phase 3 — Strategy 패턴 인터페이스.
// 각 modifier는 한 가지 변환만 책임. CargoBox.ApplyPreset이 chain으로 순차 적용.
// 학습: GDD/architecture/phase3-modifier-chain.md

public interface ICargoModifier
{
    /// <summary>이 modifier의 변환을 stats에 적용. 순서 의존 — 호출자가 보장.</summary>
    void Apply(ref CargoStats stats);
}
