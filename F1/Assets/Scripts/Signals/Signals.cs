using System;

/// <summary>
/// D-287 Signals: 하루 흐름에 지금 실제로 있는 신호만. 단계·물건·화면이 서로를 직접 부르지 않고 여기로 말한다.
/// 지금은 감독(OnFootDirector)이 전환 지점에서 쏘기만 하고, 듣는 쪽은 없다(동작 그대로) — 다음 과제부터 하나씩 듣게 한다.
/// </summary>
public enum Signal
{
    NotePicked,    // 칠판에서 쪽지를 집음
    BoxPacked,     // 작업대에서 싼 상자를 듦
    BoxLoaded,     // 상자를 카트에 싣고 출발
    Arrived,       // 배달지 도착
    HandedOver,    // 문 앞에서 상자를 건넴
    DuskStarted,   // 노을 시작
    SleptIn,       // 침대에 누움(일기)
    DayStarted,    // 다음 날 아침
    KeyReceived,   // 사장에게 열쇠를 받음
}

public static class SignalBus
{
    /// <summary>신호와 짧은 설명(로그용). 듣는 쪽은 += 로 붙는다.</summary>
    public static event Action<Signal, string> Raised;
    /// <summary>지금까지 쏜 횟수(QA·테스트).</summary>
    public static int Count { get; private set; }
    public static Signal? Last { get; private set; }

    public static void Raise(Signal s, string note = "")
    {
        Count++; Last = s;
        Raised?.Invoke(s, note ?? "");
    }

    /// <summary>첫 배달 단계 글(OrderRules.Step) → 그 단계에 들어선 신호. "없음" 은 신호 없음.</summary>
    public static void RaiseStep(string step)
    {
        switch (step)
        {
            case "쪽지": Raise(Signal.NotePicked, step); break;
            case "포장": Raise(Signal.BoxPacked, step); break;
            case "실음": Raise(Signal.BoxLoaded, step); break;
            case "도착": Raise(Signal.Arrived, step); break;
            case "건넴": Raise(Signal.HandedOver, step); break;
        }
    }

    public static void Reset() { Count = 0; Last = null; }
}
