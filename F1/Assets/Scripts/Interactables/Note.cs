using UnityEngine;

/// <summary>칠판에 꽂힌 쪽지(D-269). 집으면 그 쪽지가 손에 든 전표가 된다 — NotePicked 에 쪽지를 실어 쏜다.</summary>
public class Note : Interactable
{
    /// <summary>시선이 가리키는 쪽지(Gaze 가 매 프레임 고른다). 없거나 이미 맡았으면 칩 없음.</summary>
    public HeldPaper Target;
    bool Live => Target != null && !DayState.OrderPicked;

    public override string Prompt => Live ? "E 쪽지 집기" : "";
    public override Vector3? Anchor => Live ? DepotOrderBoard.NoteChipAnchor(Target) : (Vector3?)null;
    public override void Use() => SignalBus.Raise(Signal.NotePicked, "쪽지", Target);
}
