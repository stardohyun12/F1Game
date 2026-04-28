/// <summary>
/// 화물 타입별 민감도 프리셋 (Day 10, Phase 1).
/// CargoBox.Awake에서 자동 적용 → 디자이너는 enum만 바꾸면 됨.
///
/// 4종 차등 의도:
/// - Box   : 표준. 모든 축 균형. 베이스라인.
/// - Eggs  : Shock 극민감. 일반 가속/제동에도 데미지. 코너+직선 모두 부드럽게.
/// - Glass : 충돌 절대 금지. 작은 부딪힘도 큰 데미지.
/// - Fish  : Spin 극민감. 직선은 빠르게 OK, 코너만 부드럽게.
///
/// 튜닝 원칙: 화물 타입에 따라 운전 패턴 자체가 달라져야 함.
/// → 같은 맵, 같은 코스라도 화물 보면 "오늘 어떻게 운전해야 하지" 의식 발생.
///
/// Day 10 시점 = 첫 패스. 도현 플레이테스트(Day 13~14) 후 재튜닝.
/// </summary>
public static class CargoTypePresets
{
    public struct Preset
    {
        public float longWarnG, longDamageMul;
        public float latWarnG, latDamageMul;
        public float spinWarnDegPerSec, spinDamageMul;
        public float tiltWarnDeg, tiltDamageMul;
        public float impactWarnKmh, impactDamageMul;
        public float cumulativeMul;
    }

    public static Preset Get(CargoType type)
    {
        // 도현 피드백 (세션 15): "일반 운전에도 화물 파괴". 임계 +0.25g 일괄 상향.
        // brakeDecel 8 m/s² = 0.82g 이므로 정상 제동 안전. 급제동(handbrake)/충돌만 데미지.
        // 데미지 mul은 그대로 — overflow 작아져서 자연스럽게 완화됨.
        switch (type)
        {
            case CargoType.Eggs:
                // Shock 극민감 — 다른 화물보다 빨리 데미지.
                return new Preset
                {
                    longWarnG = 0.7f, longDamageMul = 15f,
                    latWarnG = 0.8f, latDamageMul = 12f,
                    spinWarnDegPerSec = 90f, spinDamageMul = 0.4f,
                    tiltWarnDeg = 12f, tiltDamageMul = 0.4f,
                    impactWarnKmh = 8f, impactDamageMul = 1.5f,
                    cumulativeMul = 0.8f,
                };

            case CargoType.Glass:
                // 충돌 극민감 — 충돌 임계 매우 낮음. 일반 G는 표준.
                return new Preset
                {
                    longWarnG = 0.8f, longDamageMul = 12f,
                    latWarnG = 0.9f, latDamageMul = 10f,
                    spinWarnDegPerSec = 100f, spinDamageMul = 0.3f,
                    tiltWarnDeg = 12f, tiltDamageMul = 0.4f,
                    impactWarnKmh = 5f, impactDamageMul = 2.5f,
                    cumulativeMul = 0.6f,
                };

            case CargoType.Fish:
                // Spin 극민감 — yaw rate 임계 낮음. 직선은 표준.
                return new Preset
                {
                    longWarnG = 1.0f, longDamageMul = 8f,
                    latWarnG = 1.0f, latDamageMul = 8f,
                    spinWarnDegPerSec = 70f, spinDamageMul = 0.8f,
                    tiltWarnDeg = 15f, tiltDamageMul = 0.4f,
                    impactWarnKmh = 12f, impactDamageMul = 0.8f,
                    cumulativeMul = 0.5f,
                };

            case CargoType.Cake:
                // Tilt 극민감 — 차체 기울기 임계 낮음.
                return new Preset
                {
                    longWarnG = 1.0f, longDamageMul = 8f,
                    latWarnG = 1.0f, latDamageMul = 8f,
                    spinWarnDegPerSec = 110f, spinDamageMul = 0.3f,
                    tiltWarnDeg = 5f, tiltDamageMul = 1.2f,
                    impactWarnKmh = 10f, impactDamageMul = 1.0f,
                    cumulativeMul = 0.5f,
                };

            case CargoType.Box:
            default:
                // 표준 — 적정선. 일반 운전 안전, 급제동/급커브만 데미지.
                return new Preset
                {
                    longWarnG = 0.9f, longDamageMul = 10f,
                    latWarnG = 1.0f, latDamageMul = 8f,
                    spinWarnDegPerSec = 100f, spinDamageMul = 0.3f,
                    tiltWarnDeg = 15f, tiltDamageMul = 0.3f,
                    impactWarnKmh = 12f, impactDamageMul = 0.8f,
                    cumulativeMul = 0.5f,
                };
        }
    }

    /// <summary>UI 표시용 한국어 이름. 주문 보드/정산 화면에서 사용.</summary>
    public static string DisplayName(CargoType type)
    {
        switch (type)
        {
            case CargoType.Eggs:  return "계란";
            case CargoType.Glass: return "유리";
            case CargoType.Fish:  return "활어";
            case CargoType.Cake:  return "케이크";
            case CargoType.Box:
            default:              return "상자";
        }
    }

    /// <summary>주문 보드 부제목 — 운전 가이드 한 줄.</summary>
    public static string DriveTip(CargoType type)
    {
        switch (type)
        {
            case CargoType.Eggs:  return "급가속·급제동 금지";
            case CargoType.Glass: return "충돌 절대 금지";
            case CargoType.Fish:  return "급커브·드리프트 금지";
            case CargoType.Cake:  return "경사·차체 기울기 금지";
            case CargoType.Box:
            default:              return "표준 화물";
        }
    }

    /// <summary>
    /// CCTV 박스 색상 (D-043, 도현 세션 15).
    /// 화물 타입 즉시 인식 — CCTV에 색깔로 어떤 화물인지 보임.
    /// </summary>
    public static UnityEngine.Color BoxColor(CargoType type)
    {
        switch (type)
        {
            case CargoType.Eggs:  return new UnityEngine.Color(0.95f, 0.92f, 0.78f); // 상아 (계란껍질)
            case CargoType.Glass: return new UnityEngine.Color(0.55f, 0.80f, 1.00f); // 청 (유리)
            case CargoType.Fish:  return new UnityEngine.Color(0.35f, 0.80f, 0.80f); // 청록 (활어 수조)
            case CargoType.Cake:  return new UnityEngine.Color(1.00f, 0.75f, 0.85f); // 핑크 (케이크)
            case CargoType.Box:
            default:              return new UnityEngine.Color(0.62f, 0.42f, 0.22f); // 골판지 갈색
        }
    }
}
