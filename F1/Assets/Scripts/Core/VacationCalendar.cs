/// <summary>
/// D-287 Core: 방학 달력 규칙. 28칸(DayLog.TotalDays) = 4주, 주말은 토·일(SPEC §11-2) → 평일 20 · 주말 8.
/// 가설: 방학 1일째 = 월요일(SPEC 에 요일이 없다 — 도현이 정하면 FirstWeekday 만 바꾼다).
/// 지금 게임 코드는 주말을 쓰지 않는다 — 규칙만 여기 두고 테스트한다.
/// </summary>
public static class VacationCalendar
{
    public const int Days = DayLog.TotalDays;
    /// <summary>요일 번호 0=월 … 5=토 6=일.</summary>
    public const int FirstWeekday = 0;

    public static int Weekday(int day) => (FirstWeekday + day - 1) % 7;
    public static bool IsWeekend(int day) => Weekday(day) >= 5;
    public static bool InRange(int day) => day >= 1 && day <= Days;

    public static int CountWeekend()
    {
        int n = 0;
        for (int d = 1; d <= Days; d++) if (IsWeekend(d)) n++;
        return n;
    }
}
