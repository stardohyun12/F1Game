using System;
using System.Collections.Generic;

/// <summary>
/// D-287 Signals: 단계·물건·화면이 서로를 직접 부르지 않고 여기로 말한다.
/// 2단계(과제 06): 물건(Interactables)이 「해 주세요」를 쏘고, 그 일을 맡은 단계(Stages)가 듣는다.
/// </summary>
public enum Signal
{
    NotePicked,    // 칠판에서 쪽지를 집음 (물건 Note 가 쪽지를 실어 쏨 → Stage_Depot)
    BoxPacked,     // 상자 포장 끝 (배달 단계 「포장」)
    BoxLoaded,     // 상자를 카트에 싣고 출발 (Stage_Drive 가 쏨)
    Arrived,       // 배달지 도착 (Stage_Drive 가 쏨)
    HandedOver,    // 문 앞에서 상자를 건넴 (Stage_Door 가 쏨)
    DuskStarted,   // 노을 시작
    SleptIn,       // 침대에 누움(일기) (물건 Bed → Stage_DayEnd)
    DayStarted,    // 다음 날 아침 (Stage_DayEnd 가 쏨 → 단계마다 제 것을 되돌림)
    KeyReceived,   // 사장에게 열쇠를 받음
    // ── 2단계: 물건이 쏘는 「해 주세요」 ──
    TalkAsked,     // 사장 [E 말하기]
    BoardOpened,   // 칠판 [E 칠판 보기]
    PackOpened,    // 작업대 [E 포장하기]
    BoxTaken,      // 작업대 [E 상자 들기]
    CartBoarded,   // 카트 [E 타기 / 싣고 타기]
    RepairAsked,   // 상한 카트 [E 고치기]
    DoorKnocked,   // 배달지 문 [E 두드리기]
    CalendarOpened,// 방 달력 [E 달력 보기]
    SeatTaken,     // 낚시 자리 [E 앉기] (arg = 자리)
    DiaryClosed,   // 일기장 덮기
}

public static class SignalBus
{
    /// <summary>신호와 짧은 설명(로그용).</summary>
    public static event Action<Signal, string> Raised;
    /// <summary>물건이 남기는 한 줄(감독 로그에 그대로 들어간다 — 분리 전후 로그 비교용).</summary>
    public static event Action<string> Marked;
    /// <summary>지금까지 쏜 횟수(QA·테스트).</summary>
    public static int Count { get; private set; }
    public static Signal? Last { get; private set; }

    static readonly Dictionary<Signal, List<Action<object>>> ears = new Dictionary<Signal, List<Action<object>>>();

    /// <summary>신호 s 를 듣는다. arg = 쏜 쪽이 붙인 것(쪽지·낚시 자리 등, 없으면 null).</summary>
    public static void On(Signal s, Action<object> h)
    {
        if (!ears.TryGetValue(s, out var l)) ears[s] = l = new List<Action<object>>();
        l.Add(h);
    }
    public static void Off(Signal s, Action<object> h) { if (ears.TryGetValue(s, out var l)) l.Remove(h); }

    public static void Raise(Signal s, string note = "", object arg = null)
    {
        Count++; Last = s;
        Raised?.Invoke(s, note ?? "");
        if (!ears.TryGetValue(s, out var l)) return;
        foreach (var h in l.ToArray()) h(arg);     // 듣는 중에 붙거나 떨어져도 안전하게 사본으로
    }

    public static void Mark(string line) => Marked?.Invoke(line);

    /// <summary>
    /// 첫 배달 단계 글(OrderRules.Step) → 그 단계에 들어선 신호. "없음" 은 신호 없음.
    /// "쪽지" 는 여기서 안 쏜다 — 물건 Note 가 집는 순간 쪽지를 실어 쏜다(한 번만).
    /// </summary>
    public static void RaiseStep(string step)
    {
        switch (step)
        {
            case "포장": Raise(Signal.BoxPacked, step); break;
            case "실음": Raise(Signal.BoxLoaded, step); break;
            case "도착": Raise(Signal.Arrived, step); break;
            case "건넴": Raise(Signal.HandedOver, step); break;
        }
    }

    public static void Reset() { Count = 0; Last = null; }
    /// <summary>Play 시작 때 지난 Play 의 귀를 지운다(도메인 리로드가 꺼져 있어도 깨끗하게).</summary>
    public static void ClearEars() { ears.Clear(); }
}
