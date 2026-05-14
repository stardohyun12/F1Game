/// <summary>
/// 타이어 타입별 데이터 프리셋 (Phase 5 — OCP + DRY).
///
/// 동기: TireSystem 안에 동일한 enum switch가 5번 반복 (Grip / Speed / WearRate / DisplayName / Description).
///       새 타이어 enum 추가 = 5곳 수정 + 누락 위험. 데이터·코드 분리해서 추가 비용을 1곳으로.
///
/// 패턴 일관성: `CargoTypePresets`(Day 10)와 동일 형태 — static class + Preset struct + switch 1회.
/// Struct는 stack 할당이라 hot path(GripMultiplier가 매 프레임 호출되어도) GC 영향 없음.
/// 문자열 리터럴은 intern 되어 alloc 0.
///
/// 새 타이어 추가 시:
/// 1. TireType enum에 항목 추가.
/// 2. TireTypePresets.Get() switch에 case 한 줄.
/// 3. (선택) 인스펙터 wearRate SerializeField 추가 — TireSystem 안.
/// </summary>
public static class TireTypePresets
{
    public struct Preset
    {
        /// <summary>그립 배율. ArcadeCarController.sideGripDamping에 곱함.</summary>
        public float grip;

        /// <summary>최대 속도/가속 배율.</summary>
        public float speed;

        /// <summary>UI 표시명.</summary>
        public string displayName;

        /// <summary>UI 한 줄 설명 (코스 가이드).</summary>
        public string description;
    }

    public static Preset Get(TireType t)
    {
        switch (t)
        {
            case TireType.Soft:
                // 그립 매우 높음, 살짝 빠름. 마모 빠름은 wearRateSoft에서.
                return new Preset
                {
                    grip = 1.4f, speed = 1.05f,
                    displayName = "Soft",
                    description = "그립↑↑ · 빠른 마모 · 고속 코너",
                };

            case TireType.Hard:
                // 그립 낮음, 살짝 느림. 마모 느림.
                return new Preset
                {
                    grip = 0.7f, speed = 0.95f,
                    displayName = "Hard",
                    description = "그립↓ · 느린 마모 · 장거리",
                };

            case TireType.Medium:
            default:
                return new Preset
                {
                    grip = 1.0f, speed = 1.0f,
                    displayName = "Medium",
                    description = "균형 · 표준",
                };
        }
    }

    public static string DisplayName(TireType t) => Get(t).displayName;
    public static string Description(TireType t) => Get(t).description;
}
