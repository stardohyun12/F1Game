using UnityEngine;

/// <summary>방 벽 달력 — [E 달력 보기].</summary>
public class WallCalendar : Interactable
{
    readonly RoomCalendar cal;
    public WallCalendar(RoomCalendar cal) { this.cal = cal; }

    public override string Prompt => "E 달력 보기";
    public override Vector3? Anchor => cal.PromptPoint;
    public override void Use() => SignalBus.Raise(Signal.CalendarOpened);
}
