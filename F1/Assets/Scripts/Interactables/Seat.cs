using UnityEngine;

/// <summary>낚시 자리(D-227) — 가까이 서면 [E 앉기]. SeatTaken 에 자리를 실어 쏜다.</summary>
public class Seat : Interactable
{
    /// <summary>앉을 수 있는 자리(Gaze 가 고른다).</summary>
    public FishingSpot Target;

    public override string Prompt => "E 앉기";
    public override Vector3? Anchor => Target.transform.position + Vector3.up * 0.6f;
    public override bool Low => true;
    public override void Use() => SignalBus.Raise(Signal.SeatTaken, "", Target);
}
