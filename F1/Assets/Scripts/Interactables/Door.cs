using UnityEngine;

/// <summary>여닫는 문(DoorHinge) — 신호 없이 그 자리에서 연다·닫는다(단계가 할 일 없음).</summary>
public class Door : Interactable
{
    /// <summary>시선이 맞은 문(Gaze 가 고른다).</summary>
    public DoorHinge Target;
    readonly Transform player;
    public Door(Transform player) { this.player = player; }

    public override string Prompt => Target.IsOpen ? "E 닫기" : "E 열기";
    public override bool CanUse => !Target.Moving;
    public override Vector3? Anchor => Gaze.CenterOf(Target);
    public override void Use()
    {
        Target.Toggle(player.position);
        SignalBus.Mark((Target.IsOpen ? "열기 " : "닫기 ") + Target.name);
    }
}
