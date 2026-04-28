/// <summary>
/// 화물 포장 단계 (도현 세션 15 신규 아이디어).
///
/// 문제 진단: 일반 주행에도 화물 데미지 → 좌절스러움.
/// 해결: 출발 전 박스 안에 충격흡수재(스티로폼/뽁뽁이) 채우기 결정.
///        포장 등급에 따라 데미지 임계값 배율 ↑.
///
/// 즉, 포장 = 운전 자유도 구매. 비싼 포장 = 자유로운 운전 / 자금 손해.
/// 결정 밀도 추가 (USP "결정 4개"에 정합).
///
/// 임계 배율:
/// - None     : ×1.0 (포장 없음, 임계 그대로 — 매우 조심)
/// - Bubble   : ×1.3 (뽁뽁이, 임계 30% 여유 — 일반 주행 안전)
/// - Foam     : ×1.6 (스티로폼, 임계 60% 여유 — 공격 운전 가능)
/// - Hardcase : ×2.0 (충격 흡수 케이스, 임계 2배 — 거친 운전 OK)
///
/// MVP: 기본 Bubble (자동 적용 - 도현 좌절 즉시 해소).
/// Phase 1 끝: OrderBoard에 4단계 선택 + 자금 차감.
/// Phase 2: 포장 종류별 차등 (충격은 Foam, 회전은 Bubble 등).
/// </summary>
public enum PackingTier
{
    /// <summary>포장 없음 — 임계 그대로. 매우 조심해서 운전.</summary>
    None,

    /// <summary>뽁뽁이 — 임계 ×1.3. 일반 주행 안전. 무료 또는 저가.</summary>
    Bubble,

    /// <summary>스티로폼 — 임계 ×1.6. 공격 운전 가능. 중가.</summary>
    Foam,

    /// <summary>충격 흡수 케이스 — 임계 ×2.0. 거의 모든 운전 OK. 고가.</summary>
    Hardcase,
}

public static class PackingTierHelper
{
    public static float ThresholdMultiplier(PackingTier tier)
    {
        return tier switch
        {
            PackingTier.Bubble => 1.3f,
            PackingTier.Foam => 1.6f,
            PackingTier.Hardcase => 2.0f,
            _ => 1.0f,
        };
    }

    /// <summary>포장 자금 비용 (₩). 출발 전 자동 차감.</summary>
    public static int Cost(PackingTier tier)
    {
        return tier switch
        {
            PackingTier.Bubble => 100,
            PackingTier.Foam => 300,
            PackingTier.Hardcase => 800,
            _ => 0,
        };
    }

    public static string DisplayName(PackingTier tier)
    {
        return tier switch
        {
            PackingTier.Bubble => "뽁뽁이",
            PackingTier.Foam => "스티로폼",
            PackingTier.Hardcase => "충격 케이스",
            _ => "포장 없음",
        };
    }

    /// <summary>UI 부제목 — 운전 자유도 한 줄.</summary>
    public static string DriveTip(PackingTier tier)
    {
        return tier switch
        {
            PackingTier.Bubble => "일반 주행 안전",
            PackingTier.Foam => "공격 운전 가능",
            PackingTier.Hardcase => "거의 모든 운전 OK",
            _ => "매우 조심해서 운전",
        };
    }
}
