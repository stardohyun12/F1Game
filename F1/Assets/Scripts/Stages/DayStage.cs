using System.Collections.Generic;

/// <summary>
/// D-287 2단계: 하루의 한 단계(보관소·하루 끝 …). MonoBehaviour 가 아니다 — 감독(OnFootDirector)이 만들고 부른다.
/// Enter = 그 단계 화면을 연다 · Tick = 그 화면이 떠 있는 동안 매 프레임 · Exit = 닫는다 · Always = 모드와 상관없이 매 프레임.
/// 단계는 신호(SignalBus)를 듣고 일한다 — 물건(Interactables)이 「해 주세요」를 쏘면 Listen 으로 받은 메서드가 돈다.
/// 감독 멤버는 internal 로 본다(지금은 같은 어셈블리). 자기 상태는 자기 필드에, 하루가 바뀌면(DayStarted) 자기 것만 되돌린다.
/// </summary>
public abstract class DayStage
{
    protected readonly OnFootDirector d;
    readonly List<(Signal s, System.Action<object> h)> ears = new List<(Signal, System.Action<object>)>();

    protected DayStage(OnFootDirector d) { this.d = d; }

    /// <summary>이 단계가 Tick 을 받는 감독 모드.</summary>
    public abstract OnFootDirector.Mode Mode { get; }
    public virtual void Enter() { }
    public virtual void Tick(bool press) { }
    public virtual void Exit() { }
    public virtual void Always() { }
    /// <summary>다음 날 아침(DayStarted) — 이 단계 것만 처음으로.</summary>
    protected virtual void NewDay() { }

    /// <summary>신호를 듣기 시작(감독 Start 에서 한 번).</summary>
    public virtual void Plug() { Listen(Signal.DayStarted, _ => NewDay()); }

    protected void Listen(Signal s, System.Action<object> h) { ears.Add((s, h)); SignalBus.On(s, h); }

    public void Unplug()
    {
        foreach (var (s, h) in ears) SignalBus.Off(s, h);
        ears.Clear();
    }
}
