using NUnit.Framework;

/// <summary>D-287: Core 규칙 테스트(EditMode, 씬 없이). 값은 옮기기 전 코드 그대로인지 본다.</summary>
public class CoreRulesTests
{
    // 파손 3단 — 80% 이상 멀쩡, 40% 이상 조금 상함, 밑은 깨짐
    [Test] public void Tier_80이상_멀쩡() { Assert.AreEqual(0, HandoverTier.Of(0.8f)); Assert.AreEqual(0, HandoverTier.Of(1f)); }
    [Test] public void Tier_40이상80미만_조금상함() { Assert.AreEqual(1, HandoverTier.Of(0.79f)); Assert.AreEqual(1, HandoverTier.Of(0.4f)); }
    [Test] public void Tier_40미만_깨짐() { Assert.AreEqual(2, HandoverTier.Of(0.39f)); Assert.AreEqual(2, HandoverTier.Of(0f)); }
    [Test] public void Tier_이름3개() { CollectionAssert.AreEqual(new[] { "멀쩡", "조금 상함", "깨짐" }, HandoverTier.Name); }

    // 배율 ×1.0 / 0.7 / 0.3
    [Test] public void Pay_배율() { CollectionAssert.AreEqual(new[] { 1.0f, 0.7f, 0.3f }, HandoverTier.Pay); }
    [Test] public void Pay_첫배달_멀쩡_1200() { Assert.AreEqual(1200, Rules.HandoverPayFor(Rules.FirstOrderReward, 0.95f)); }
    [Test] public void Pay_첫배달_조금상함_840() { Assert.AreEqual(840, Rules.HandoverPayFor(Rules.FirstOrderReward, 0.6f)); }
    [Test] public void Pay_첫배달_깨짐_360() { Assert.AreEqual(360, Rules.HandoverPayFor(Rules.FirstOrderReward, 0.1f)); }

    // 첫 배달 +1,200 · 수리 −300
    [Test] public void 첫배달_보수_1200() { Assert.AreEqual(1200, Rules.FirstOrderReward); }
    [Test] public void 수리비_300() { Assert.AreEqual(300, Rules.RepairCost); }
    [Test] public void 오늘번돈_배달빼기수리()
    {
        DayLog.ResetAll();
        DayLog.Record("a", "b", "멀쩡", Rules.FirstOrderReward);
        DayLog.RecordRepair(Rules.RepairCost);
        Assert.AreEqual(900, DayLog.TodayMoney);
        Assert.AreEqual(300, DayLog.TodayRepair);
        DayLog.ResetAll();
    }

    // 달력 28칸 · 주말
    [Test] public void 달력_28칸() { Assert.AreEqual(28, DayLog.TotalDays); Assert.AreEqual(28, VacationCalendar.Days); }
    [Test] public void 달력_주말8_평일20() { Assert.AreEqual(8, VacationCalendar.CountWeekend()); }
    [Test] public void 달력_1일째월_6일째토_7일째일()
    {
        Assert.IsFalse(VacationCalendar.IsWeekend(1));
        Assert.IsTrue(VacationCalendar.IsWeekend(6));
        Assert.IsTrue(VacationCalendar.IsWeekend(7));
        Assert.IsFalse(VacationCalendar.IsWeekend(8));
    }
    [Test] public void 달력_28일째에서_멈춤()
    {
        DayLog.ResetAll();
        for (int i = 0; i < 40; i++) DayLog.NextDay();
        Assert.AreEqual(28, DayLog.Day);
        Assert.AreEqual(40, DayLog.DaysEnded);
        DayLog.ResetAll();
        Assert.AreEqual(1, DayLog.Day);
    }

    // 주문 규칙
    [Test] public void 주문_단계순서()
    {
        Assert.AreEqual("없음", OrderRules.Step(false, false, false, false, false));
        Assert.AreEqual("쪽지", OrderRules.Step(false, false, false, true, false));
        Assert.AreEqual("포장", OrderRules.Step(false, false, false, true, true));
        Assert.AreEqual("실음", OrderRules.Step(false, false, true, true, true));
        Assert.AreEqual("도착", OrderRules.Step(false, true, true, true, true));
        Assert.AreEqual("건넴", OrderRules.Step(true, true, true, true, true));
    }
    [Test] public void 주문_카트칩()
    {
        Assert.AreEqual("상자부터 실어", OrderRules.CartChip(true, true, false, false));
        Assert.AreEqual("E 싣고 타기", OrderRules.CartChip(true, true, true, false));
        Assert.AreEqual("E 타기", OrderRules.CartChip(true, false, false, false));
        Assert.AreEqual("E 타기", OrderRules.CartChip(false, true, false, false));
    }
}
