using UnityEngine;

/// <summary>사장(사람) — [E 말하기]. 칩은 머리 위.</summary>
public class Boss : Interactable
{
    readonly Transform boss;
    Vector3? off;
    public Boss(Transform boss) { this.boss = boss; }

    public override string Prompt => "E 말하기";
    public override Vector3? Anchor => boss.position + (off ??= Gaze.TopOffset(boss));
    public override void Use() => SignalBus.Raise(Signal.TalkAsked);
}
