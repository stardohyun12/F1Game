using UnityEngine;

/// <summary>
/// D-289 World 층: 규칙 수치(SPEC §2·§7). 원천 = world/world.yaml. Core 는 엔진을 몰라서 이 에셋을 직접 못 읽는다 —
/// Stages 가 시작 때 ApplyTo 로 Core.Rules 에 값을 넣는다. 지금 Core 에 들어가는 건 수리비·첫 주문 보수 둘뿐이고,
/// 나머지(파손 3단·배율·노을·달력)는 에셋에 값만 있고 사용처는 아직 옛 상수다.
/// </summary>
public class WorldNumbers : ScriptableObject
{
    public int repairCost = 300;
    public float tierOkMin = 0.8f;
    public float tierDentMin = 0.4f;
    public float payOk = 1.0f;
    public float payDent = 0.7f;
    public float payBroken = 0.3f;
    public float sunsetSeconds = 60f;
    public int calendarDays = 28;
    /// <summary>첫 주문 기본 보수 — gen 이 WorldOrders 의 첫 주문에서 복사한 값.</summary>
    public int firstOrderReward = 1200;

    /// <summary>Core.Rules 에 값을 넣는다(동작 불변: 에셋 값이 옛 상수와 같다).</summary>
    public void ApplyTo()
    {
        Rules.RepairCost = repairCost;
        Rules.FirstOrderReward = firstOrderReward;
    }
}
