using UnityEngine;

/// <summary>배달 카트 — 타기 / 싣고 타기. 사장 말을 듣기 전엔 못 탄다(기능 2). 주문을 맡았으면 포장해야 싣는다(D-250).</summary>
public class Cart : Interactable
{
    readonly ArcadeCarController car;
    readonly DepotPackSpot packSpot;
    public Cart(ArcadeCarController car, DepotPackSpot packSpot) { this.car = car; this.packSpot = packSpot; }

    bool Packed => packSpot == null ? DayState.OrderPicked : packSpot.Packed;

    public override string Prompt => DayState.CartNeedsBossTalk && !DayState.Talked ? "사장님한테 먼저"
        : OrderRules.CartChip(DayState.DepotFlow, DayState.OrderPicked, Packed, DayState.OrderStarted);
    public override Vector3? Anchor => car.transform.position + Vector3.up * 0.8f;
    public override void Use() => SignalBus.Raise(Signal.CartBoarded);
}
