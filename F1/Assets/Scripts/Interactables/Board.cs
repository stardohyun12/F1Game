using UnityEngine;

/// <summary>보관소 칠판(D-250) — 쪽지가 없을 때 [E] 로 칠판 앞에 선다.</summary>
public class Board : Interactable
{
    readonly DepotOrderBoard board;
    public Board(DepotOrderBoard board) { this.board = board; }

    public override string Prompt => DayState.OrderPicked ? "E 칠판 보기(맡음)" : "E 칠판 보기";
    public override Vector3? Anchor => board.Center;
    public override bool Low => true;
    public override void Use() => SignalBus.Raise(Signal.BoardOpened);
}
