using System;
using System.Collections.Generic;

/// <summary>
/// 과제 20261102-1951(DELIVERY-LOOP §5-5): 하루 기록. 방학 28일 중 몇째 날인지 + 오늘 만난 사람·번 돈.
/// 일기장(DotScreenUI.Diary)과 방 달력(RoomCalendar)이 읽고, OnFootDirector 가 건넨 순간 쓴다.
/// 저장: 지금 게임엔 저장 방식이 없다(업그레이드 지갑도 메모리뿐) → 이것도 메모리뿐. 플레이를 끄면 1일째로 돌아간다.
/// </summary>
public static class DayLog
{
    public const int TotalDays = 28;

    public struct Visit
    {
        public string who, from, tier;
        public int money;
    }

    /// <summary>방학 몇째 날(1~28).</summary>
    public static int Day { get; private set; } = 1;
    /// <summary>오늘 만난 사람(건넨 순서).</summary>
    public static readonly List<Visit> Today = new List<Visit>();
    public static int Visits => Today.Count;
    /// <summary>C-001: 오늘 정비소에서 낸 수리비(고친 순서). 오늘 번 돈에서 뺀다.</summary>
    public static readonly List<int> Repairs = new List<int>();
    public static int TodayRepair { get { int s = 0; foreach (var r in Repairs) s += r; return s; } }
    public static int TodayMoney { get { int s = 0; foreach (var v in Today) s += v.money; return s - TodayRepair; } }
    /// <summary>지금까지 하루를 마친 횟수(QA).</summary>
    public static int DaysEnded { get; private set; }

    public static event Action Changed;

    public static void ResetAll()
    {
        Day = 1; DaysEnded = 0; Today.Clear(); Repairs.Clear();
        Changed?.Invoke();
    }

    public static void Record(string who, string from, string tier, int money)
    {
        Today.Add(new Visit { who = who ?? "", from = from ?? "", tier = tier ?? "", money = money });
        Changed?.Invoke();
    }

    public static void RecordRepair(int cost)
    {
        Repairs.Add(cost);
        Changed?.Invoke();
    }

    /// <summary>일기를 덮고 다음 날 아침 — 날짜가 하나 간다(28일째는 그대로: 방학 끝 장면은 이 과제 밖).</summary>
    public static void NextDay()
    {
        DaysEnded++;
        if (Day < TotalDays) Day++;
        Today.Clear(); Repairs.Clear();
        Changed?.Invoke();
    }
}
