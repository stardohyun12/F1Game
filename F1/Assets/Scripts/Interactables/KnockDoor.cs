using UnityEngine;

/// <summary>배달지 문(D-250 ③) — 상자를 들고 있을 때만 두드린다. 건네기 전·후엔 그냥 문(칩 없음).</summary>
public class KnockDoor : Interactable
{
    readonly DoorHandover door;
    public KnockDoor(DoorHandover door) { this.door = door; }

    bool Live => DayState.CarryingBox && !door.Busy && !door.IsOpen;
    public override string Prompt => Live ? "E 두드리기" : "";
    public override Vector3? Anchor => Live ? door.DoorCenter : (Vector3?)null;
    public override void Use() => SignalBus.Raise(Signal.DoorKnocked);
}
