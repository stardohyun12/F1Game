/// <summary>
/// 화물 타입 — 어떤 운전 패턴에 약한가.
/// Day 9 (Phase 1) 도입. Day 15에 Tilt 축 + Cake 추가로 3축 민감도 완성.
///
/// 3축 민감도 (GDD systems):
/// - Shock (충격): 충돌, 급제동 → 종G/횡G/충돌속도 임계 낮음
/// - Spin  (회전): 급커브, 드리프트 → yaw rate 임계 낮음
/// - Tilt  (기울기): 경사 지속, 차체 기울기 → tilt angle 임계 낮음
/// </summary>
public enum CargoType
{
    /// <summary>표준 박스. 균형 잡힌 민감도 (현재 CargoBox 기본 동작).</summary>
    Box,

    /// <summary>계란 — Shock(종G/횡G/충돌)에 매우 민감. 코너/제동 부드럽게.</summary>
    Eggs,

    /// <summary>유리 — Shock + 충돌에 극민감. 절대 부딪치면 안 됨.</summary>
    Glass,

    /// <summary>활어 — Spin(지속 yaw rate)에 민감. 급커브/드리프트 금지.</summary>
    Fish,

    /// <summary>케이크 — Tilt(차체 기울기)에 극민감. 경사 길게 못 탐, 기울어진 상태 지속 X.</summary>
    Cake,
}
