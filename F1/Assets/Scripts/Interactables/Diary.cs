using UnityEngine;

/// <summary>하루 끝 일기장(풀스크린) — 펼치고 최소 시간이 지나면 덮을 수 있다. 덮으면 DiaryClosed(→ 다음 날 아침).</summary>
public class Diary : Interactable
{
    public override string Prompt => "";
    public override bool CanUse => DayState.Now == DayState.Mode.Diary && Time.unscaledTime - DayState.DiaryAt >= DayState.DiaryMinSec;
    public override Vector3? Anchor => null;
    public override void Use() => SignalBus.Raise(Signal.DiaryClosed);
}
