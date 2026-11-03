using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    public RoomBed Bed => bed;
    /// <summary>일기를 펼친 때(unscaled) · 덮어서 아침이 된 때 — DotScreenUI 가 읽는다.</summary>
    public float DiaryAt { get; private set; } = -99f;
    public float MorningAt { get; private set; } = -99f;
    public bool DiaryCanClose => State == Mode.Diary && Time.unscaledTime - DiaryAt >= diaryMinSec;
    bool simCalClose, simDiaryClose;
    public void SimDiaryClose() { simDiaryClose = true; }

    /// <summary>하루 끝: 침대 [E] → 일기장 풀스크린(DotScreenUI.DiaryUI). 몸·시선을 잠근다.</summary>
    void OpenDiary()
    {
        State = Mode.Diary; SignalBus.Raise(Signal.SleptIn);
        player.enabled = false;
        SetPrompt(""); PromptAnchor = null; GazeTarget = null;
        if (dotRoot != null) dotRoot.SetActive(false);
        DiaryAt = Time.unscaledTime;
        Mark($"일기장 펼침 — 방학 {DayLog.Day}일째 · 만난 사람 {DayLog.Visits}명 · 오늘 번 돈 {DayLog.TodayMoney}");
    }

    void UpdateDiary(bool press)
    {
        PromptAnchor = null;
        var kb = Keyboard.current;
        bool close = press || simDiaryClose || (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame));
        simDiaryClose = false;
        if (close && DiaryCanClose) NextMorning();
    }

    /// <summary>
    /// 일기를 덮었다 → 다음 날 아침. 날짜 +1(달력 한 칸 지워짐) 뒤 하루 상태를 처음으로: 주문·상자·화물·배달지·작업대·카트·나.
    /// 안 되돌리는 것: 돈(지갑), 사장 대사를 들었다(Talked). 가설: 2일차도 1일차와 같은 경로(칠판 → 포장 → 찻집)로 돈다.
    /// </summary>
    void NextMorning()
    {
        DayLog.NextDay(); SignalBus.Raise(Signal.DayStarted);
        ResetForNextDay();
        MorningAt = Time.unscaledTime;
        Mark($"다음 날 아침 — 방학 {DayLog.Day}일째, 달력 지운 칸 {(calendar != null ? calendar.ErasedCount : -1)}");
    }

    void ResetForNextDay()
    {
        var mgr = Services.Delivery as DeliveryManager ?? FindFirstObjectByType<DeliveryManager>();
        if (mgr != null) mgr.OnDeliveryCompleted.RemoveListener(OnFirstDelivered);
        // 이번 날 주문·건네기 상태
        pickedJob = null; firstJob = null;
        OrderStarted = false; Delivered = false; HandedOver = false; CarryingBox = false; Knocking = false; BedBox = false;
        HandoverTierIdx = -1; HandoverMoney = 0; moneyText = null; MoneyAt = -99f; MoneyEnd = -99f;
        StartIntegrity = -1f; freeDriveEntered = false;
        StepTrail = "없음";
        // 아침 = 오후 빛으로(노을은 건넨 뒤에만)
        if (duskRamp != null) { StopCoroutine(duskRamp); duskRamp = null; }
        HandedAt = -99f;
        var look = SunsetLook.Find();
        if (look != null && look.IsOn) { look.dusk = 0f; if (lookWasOn) look.Apply(); else look.SetOn(false); }
        talkLine = -1; curLines = null; speaker = null; onTalkLine = null; onTalkEnd = null;
        // 손에 든 상자: 받는 사람에게 건넨 것은 치우고 새로 만든다(MoveBoxToHands 가 heldBox 를 비웠다)
        if (heldBoxGiven != null) { Destroy(heldBoxGiven); heldBoxGiven = null; }
        if (heldBox == null) BuildHeldBox();
        if (Held != null) { Destroy(Held.gameObject); Held = null; }   // D-269: 어제 전표는 버리고 칠판에 새 쪽지
        // 화물(카트 짐칸) — BeginCarry 가 꺼 둔 모양을 되살린다
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        if (cargo != null && car != null && cargo.transform != car.transform)
            foreach (var r in cargo.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        // 배달지: 꺼 둔 것 되살리고 「완료」 풀기
        foreach (var dp in pointsOff) if (dp != null) dp.gameObject.SetActive(true);
        pointsOff.Clear();
        foreach (var dp in FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) dp.ResetCompleted();
        // 칠판 · 작업대
        if (packSpot != null) packSpot.ResetForNextDay();
        if (orderBoard != null) orderBoard.SetLines(BoardLines);
        // 카트는 아침 자리(보관소 흐름 = 보관소 앞)로, 나는 방으로
        ParkMorningCart();
        SetCarControl(false);
        if (spawn != null) player.Teleport(spawn.position, spawn.eulerAngles.y);
        player.enabled = true;
        EnterFoot();
        footCam.HoldLook = false;
        simInteract = false;
    }
}
