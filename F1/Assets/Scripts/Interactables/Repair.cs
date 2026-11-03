using UnityEngine;

/// <summary>상한 카트 고치기(C-001) — 정비 자리 안에 카트와 같이 서 있으면 타기보다 먼저.</summary>
public class Repair : Interactable
{
    readonly ArcadeCarController car;
    public Repair(ArcadeCarController car) { this.car = car; }

    public override string Prompt => "E 고치기";
    public override Vector3? Anchor => car.transform.position + Vector3.up * 0.8f;
    public override void Use() => SignalBus.Raise(Signal.RepairAsked);
}
