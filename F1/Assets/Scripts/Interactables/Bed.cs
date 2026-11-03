using UnityEngine;

/// <summary>하루 끝 침대 — 오늘 배달(문 앞 건네기)을 마친 뒤에만 눕는다. 누우면 SleptIn.</summary>
public class Bed : Interactable
{
    readonly RoomBed bed;
    public Bed(RoomBed bed) { this.bed = bed; }

    public override string Prompt => DayState.HandedOver ? "E 자기 (일기)" : "오늘 배달 먼저";
    public override Vector3? Anchor => bed.Anchor;
    public override void Use() => SignalBus.Raise(Signal.SleptIn);
}
