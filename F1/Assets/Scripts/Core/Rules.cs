/// <summary>
/// D-287 Core: 돈 규칙 숫자 한 곳. 유니티 없이 돈다(PitCrew.Core, noEngineReferences).
/// 값은 옮기기 전 코드 그대로 — RepairSpot.Cost 300(가설), 첫 배달 기본 보수 1,200(OnFootDirector.MakeFirstJob),
/// 문 앞 정산 = 기본 × 3단 배율 반올림(RunController.PayHandover 의 Mathf.RoundToInt).
/// </summary>
public static class Rules
{
    /// <summary>정비소 수리 한 번(가설: 도현 한 판 뒤 정함). D-289: 원천은 world/world.yaml — Stages 가 시작 때 WorldNumbers 값을 넣는다(기본값 = 옛 상수).</summary>
    public static int RepairCost = 300;
    /// <summary>첫 배달(과제 01 쪽지) 기본 보수. 값을 넣는 곳은 RepairCost 와 같다.</summary>
    public static int FirstOrderReward = 1200;

    /// <summary>문 앞 정산: 기본 보수 × 배율, 반올림(Mathf.RoundToInt 와 같은 Math.Round).</summary>
    public static int HandoverPay(int baseReward, float pay)
    {
        float v = baseReward * pay;
        return (int)System.Math.Round(v);
    }

    /// <summary>화물 상태(0~1) → 문 앞 정산 금액.</summary>
    public static int HandoverPayFor(int baseReward, float integrity01) =>
        HandoverPay(baseReward, HandoverTier.Pay[HandoverTier.Of(integrity01)]);
}
