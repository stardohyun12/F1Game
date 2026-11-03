using UnityEngine;

/// <summary>
/// 작업대(D-251). 판(HasBench)이 있으면 서서 싸고 흔들어 본 상자를 들고 나간다 — 포장하기(PackOpened) / 상자 들기(BoxTaken).
/// 판이 없는 대역은 E 한 번에 포장됨(그 자리에서 끝 — 단계가 할 일 없음).
/// </summary>
public class PackTable : Interactable
{
    readonly DepotPackSpot spot;
    public PackTable(DepotPackSpot spot) { this.spot = spot; }

    public override Vector3? Anchor => spot.Taken && spot.HasBench && DayState.OrderPicked ? (Vector3?)null : spot.Top;

    public override string Prompt
    {
        get
        {
            if (!DayState.OrderPicked) return "칠판에서 주문 먼저";
            if (spot.HasBench)
            {
                if (spot.Taken) return "";
                if (spot.Ready && !DayState.CarryingBox) return "E 상자 들기";
                return spot.NeedsShake ? "E 작업대(들어서 흔들어 보기)" : "E 포장하기";
            }
            return spot.Packed ? "포장됨 — 카트로" : "E 포장하기";
        }
    }

    public override void Use()
    {
        if (!spot.HasBench) { spot.Pack(); SignalBus.Mark("포장됨(대역 — E 한 번)"); return; }
        if (spot.Ready && !DayState.CarryingBox) SignalBus.Raise(Signal.BoxTaken);
        else SignalBus.Raise(Signal.PackOpened);
    }
}
