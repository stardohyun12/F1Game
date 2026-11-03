/// <summary>
/// D-287 Core: 첫 배달 주문 규칙(순수 함수). 옮기기 전 OnFootDirector.DeliveryStep · CartChip 의 몸통 그대로.
/// 감독은 자기 상태를 넣어 이걸 부른다.
/// </summary>
public static class OrderRules
{
    /// <summary>C-003: 첫 배달 단계 — 없음 → 쪽지 → 포장 → 실음 → 도착 → 건넴.</summary>
    public static string Step(bool handedOver, bool delivered, bool orderStarted, bool orderPicked, bool packed) =>
        handedOver ? "건넴" : delivered ? "도착" : orderStarted ? "실음" : orderPicked && packed ? "포장" : orderPicked ? "쪽지" : "없음";

    /// <summary>카트 칩 글. "E " 로 시작하면 누를 수 있다.</summary>
    public static string CartChip(bool depotFlow, bool orderPicked, bool packed, bool orderStarted)
    {
        if (!depotFlow || orderStarted) return "E 타기";
        if (packed) return "E 싣고 타기";
        return orderPicked ? "상자부터 실어" : "E 타기";   // 주문을 맡고도 상자가 없으면 못 탄다 — 무엇부터 할지 한 줄
    }
}
