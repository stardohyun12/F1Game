using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// D-287 2단계: 하루 끝 단계 — 침대 → 일기장 → 다음 날 아침. 감독이 만들고 부른다(MonoBehaviour 아님).
/// 듣는 신호: SleptIn(물건 Bed → 일기장 펼침 = Enter) · DiaryClosed(물건 Diary → 다음 날 아침 = Exit). 쏘는 신호: DayStarted.
/// 지도: docs/world/CODEMAP.md
/// </summary>
public class Stage_DayEnd : DayStage
{
    public Stage_DayEnd(OnFootDirector d) : base(d) { }
    public override OnFootDirector.Mode Mode => OnFootDirector.Mode.Diary;

    /// <summary>일기를 펼친 때(unscaled) · 덮어서 아침이 된 때 — DotScreenUI 가 읽는다.</summary>
    public float DiaryAt { get; private set; } = -99f;
    public float MorningAt { get; private set; } = -99f;
    public bool CanClose => d.State == OnFootDirector.Mode.Diary && Time.unscaledTime - DiaryAt >= d.diaryMinSec;
    bool simClose;
    public void SimClose() { simClose = true; }

    public override void Plug()
    {
        base.Plug();
        Listen(Signal.SleptIn, _ => Enter());
        Listen(Signal.DiaryClosed, _ => Exit());
    }

    /// <summary>하루 끝: 침대 [E] → 일기장 풀스크린(DotScreenUI.DiaryUI). 몸·시선을 잠근다.</summary>
    public override void Enter()
    {
        d.State = OnFootDirector.Mode.Diary;   // SleptIn 은 물건 Bed 가 쐈다(이 메서드가 그 신호를 듣는다)
        d.player.enabled = false;
        d.SetPrompt(""); d.PromptAnchor = null; d.GazeTarget = null;
        if (d.dotRoot != null) d.dotRoot.SetActive(false);
        DiaryAt = Time.unscaledTime;
        d.Mark($"일기장 펼침 — 방학 {DayLog.Day}일째 · 만난 사람 {DayLog.Visits}명 · 오늘 번 돈 {DayLog.TodayMoney}");
    }

    public override void Tick(bool press)
    {
        d.PromptAnchor = null;
        var kb = Keyboard.current;
        bool close = press || simClose || (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame));
        simClose = false;
        var diary = d.Diary;
        if (close && diary.CanUse) diary.Use();   // → DiaryClosed → Exit
    }

    /// <summary>
    /// 일기를 덮었다 → 다음 날 아침. 날짜 +1(달력 한 칸 지워짐) 뒤 하루 상태를 처음으로: 주문·상자·화물·배달지·작업대·카트·나.
    /// 안 되돌리는 것: 돈(지갑), 사장 대사를 들었다(Talked). 가설: 2일차도 1일차와 같은 경로(칠판 → 포장 → 찻집)로 돈다.
    /// </summary>
    public override void Exit()
    {
        DayLog.NextDay(); SignalBus.Raise(Signal.DayStarted);   // 단계마다 제 것을 되돌린다(Stage_Depot.NewDay …)
        d.ResetForNextDay();
        MorningAt = Time.unscaledTime;
        d.Mark($"다음 날 아침 — 방학 {DayLog.Day}일째, 달력 지운 칸 {(d.calendar != null ? d.calendar.ErasedCount : -1)}");
    }
}
