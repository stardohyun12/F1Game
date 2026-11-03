import sys
p = 'F1/Assets/Scripts/OnFoot/OnFootDirector.cs'
s = open(p, encoding='utf-8').read()

def rep(a, b):
    global s
    if s.count(a) != 1:
        print('ANCHOR COUNT', s.count(a), a[:70]); sys.exit(1)
    s = s.replace(a, b)

rep("public enum Mode { Starting, OnFoot, InCart, Seated, Board, Pack }",
    "public enum Mode { Starting, OnFoot, InCart, Seated, Board, Pack, Calendar, Diary }")

rep("    // ── 문 앞 건네기 상태(QA·봇·DotScreenUI 가 읽음) ──\n", """    [Header("하루 끝 일기장·방 달력(과제 20261102-1951, DELIVERY-LOOP 5-5)")]
    [Tooltip("방 벽 달력 실물. 비면 달력 [E] 없음.")]
    [SerializeField] RoomCalendar calendar;
    [Tooltip("방 침대 [E] 자리. 비면 하루 끝 일기장 없음.")]
    [SerializeField] RoomBed bed;
    [Tooltip("일기를 펼친 뒤 이 초가 지나야 덮인다(실수로 바로 넘기지 않게).")]
    [SerializeField] float diaryMinSec = 0.8f;

    public RoomCalendar Calendar => calendar;
    public RoomBed Bed => bed;
    /// <summary>일기를 펼친 때(unscaled) · 덮어서 아침이 된 때 — DotScreenUI 가 읽는다.</summary>
    public float DiaryAt { get; private set; } = -99f;
    public float MorningAt { get; private set; } = -99f;
    public bool DiaryCanClose => State == Mode.Diary && Time.unscaledTime - DiaryAt >= diaryMinSec;
    /// <summary>달력 보기 시점으로 옮겨 가는 중/돌아오는 중(QA).</summary>
    public bool CalendarMoving => State == Mode.Calendar && boardMove != null;
    bool simCalClose, simDiaryClose;
    readonly List<DeliveryPoint> pointsOff = new List<DeliveryPoint>();   // BeginFirstOrder 가 꺼 둔 배달지 — 다음 날 되살림

    // ── 문 앞 건네기 상태(QA·봇·DotScreenUI 가 읽음) ──
""")

rep("        if (rc != null) rc.BeginNoClock();\n        Services.Board?.Close();",
    "        DayLog.ResetAll();                       // 저장 방식이 없다 — 플레이마다 방학 1일째\n        if (rc != null) rc.BeginNoClock();\n        Services.Board?.Close();")

rep("                         : handover != null && hit == handover ? handover.DoorCenter : CenterOf(door);",
    "                         : handover != null && hit == handover ? handover.DoorCenter\n"
    "                         : calendar != null && hit == calendar ? calendar.Center : bed != null && hit == bed ? bed.Anchor : CenterOf(door);")

rep("            else if (hit == car)\n            {\n                // 기능 2: 사장 말을 듣기 전엔 못 탄다.",
    """            else if (calendar != null && hit == calendar)
            {
                SetPrompt("E 달력 보기");
                if (press) OpenCalendar();
            }
            else if (bed != null && hit == bed)
            {
                // 하루 끝: 오늘 배달(문 앞 건네기)을 마친 뒤에만 눕는다.
                if (HandedOver) { SetPrompt("E 자기 (일기)"); if (press) OpenDiary(); }
                else SetPrompt("오늘 배달 먼저");
            }
            else if (hit == car)
            {
                // 기능 2: 사장 말을 듣기 전엔 못 탄다.""")

rep("        else if (State == Mode.Pack) UpdatePack();\n",
    "        else if (State == Mode.Pack) UpdatePack();\n        else if (State == Mode.Calendar) UpdateCalendar(press);\n        else if (State == Mode.Diary) UpdateDiary(press);\n")

rep("        // 작업대 판(BenchTop)은 카운터 윗면과 같은 높이",
    """        // 방 달력(벽에 붙은 판)·침대(충돌체 없이 경계 상자) — 앞에 다른 것이 먼저 맞으면 못 잡는다.
        if (calendar != null && first != null && first.transform.IsChildOf(calendar.transform) && best <= talkReach) return calendar;
        if (bed != null && bed.Hit(new Ray(eye.position, eye.forward), Mathf.Min(best + 0.02f, talkReach), out _)) return bed;
        // 작업대 판(BenchTop)은 카운터 윗면과 같은 높이""")

rep("        HandedOver = true;\n        Mark($\"건네기 끝",
    """        HandedOver = true;
        string metWho = firstJob != null && !string.IsNullOrEmpty(firstJob.recipientName) ? firstJob.recipientName : handover.recipientName;
        DayLog.Record(metWho, firstJob != null ? firstJob.callerName : "", HandoverTier.Name[HandoverTierIdx], HandoverMoney);
        Mark($"건네기 끝""")

rep("        foreach (var dp in FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None)) if (dp != firstDrop) dp.gameObject.SetActive(false);",
    "        foreach (var dp in FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None)) if (dp != firstDrop) { if (!pointsOff.Contains(dp)) pointsOff.Add(dp); dp.gameObject.SetActive(false); }")

rep("    // ── D-250: 주문 칠판 보기 ──\n", """    // ── 과제 20261102-1951: 방 달력 보기 · 하루 끝 일기장 · 다음 날 아침 ──
    /// <summary>달력 보기 시점: 칠판 보기와 같은 방식(카메라가 달력 앞 ViewPose 로). 몸은 그 자리.</summary>
    void OpenCalendar()
    {
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        State = Mode.Calendar;
        player.enabled = false;
        footCam.enabled = false;
        boardEyePos = cam.transform.position; boardEyeRot = cam.transform.rotation;
        calendar.SetHint(true);
        SetPrompt(""); PromptAnchor = null; GazeTarget = null;
        if (dotRoot != null) dotRoot.SetActive(false);
        calendar.ViewPose(cam.fieldOfView, cam.aspect, out var pos, out var rot);
        if (boardMove != null) StopCoroutine(boardMove);
        boardMove = StartCoroutine(MoveCam(pos, rot, boardFocusSec, null));
        Mark($"달력 보기 시점 {pos.ToString("F2")} 달력까지 {Vector3.Distance(pos, calendar.Center):F2}m — {calendar.Written}");
    }

    void CloseCalendar()
    {
        calendar.SetHint(false);
        if (boardMove != null) StopCoroutine(boardMove);
        boardMove = StartCoroutine(MoveCam(boardEyePos, boardEyeRot, boardFocusSec * 0.6f, () =>
        {
            player.enabled = true;
            EnterFoot();
            footCam.HoldLook = false;
            Mark("달력 닫기");
        }));
    }

    /// <summary>봇: 달력 닫기 · 일기 덮기.</summary>
    public void SimCalendarClose() { simCalClose = true; }
    public void SimDiaryClose() { simDiaryClose = true; }

    void UpdateCalendar(bool press)
    {
        PromptAnchor = null;
        var kb = Keyboard.current;
        bool close = press || simCalClose || (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame));
        simCalClose = false;
        if (boardMove != null) return;                                      // 옮겨 가는 동안은 입력 안 받음
        if (close) CloseCalendar();
    }

    /// <summary>하루 끝: 침대 [E] → 일기장 풀스크린(DotScreenUI.DiaryUI). 몸·시선을 잠근다.</summary>
    void OpenDiary()
    {
        State = Mode.Diary;
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
        DayLog.NextDay();
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
        OrderStarted = false; Delivered = false; HandedOver = false; CarryingBox = false; Knocking = false;
        HandoverTierIdx = -1; HandoverMoney = 0; moneyText = null; MoneyAt = -99f; MoneyEnd = -99f;
        StartIntegrity = -1f; freeDriveEntered = false;
        talkLine = -1; curLines = null; speaker = null; onTalkLine = null; onTalkEnd = null;
        // 손에 든 상자: 받는 사람에게 건넨 것은 치우고 새로 만든다(MoveBoxToHands 가 heldBox 를 비웠다)
        if (heldBoxGiven != null) { Destroy(heldBoxGiven); heldBoxGiven = null; }
        if (heldBox == null) BuildHeldBox();
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
        // 카트는 정비소 자리로, 나는 방으로
        if (cartSpot != null) ParkCar(cartSpot.position, cartSpot.eulerAngles.y);
        SetCarControl(false);
        if (spawn != null) player.Teleport(spawn.position, spawn.eulerAngles.y);
        player.enabled = true;
        EnterFoot();
        footCam.HoldLook = false;
        simInteract = false;
    }

    // ── D-250: 주문 칠판 보기 ──
""")

open(p, 'w', encoding='utf-8').write(s)
print('OK')
