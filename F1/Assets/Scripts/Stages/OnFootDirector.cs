using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 기능 1(과제 20261030-2243): 집-차고 구간 감독. 주인공 방에서 걸어서 시작 → 문(E) → 차고 마당 카트(E 타기) → 운전 → 정차 중 E 내리기.
/// 시작 순서는 OpeningTutorial 과 같다(주문판 닫기·시계 없음·timeScale 1). OpeningTutorial·BayLoopDriveOnly 는 이 감독이 있으면 끈다(D-199 로 보류 중).
/// 화면엔 한 줄 안내만(E 열기 / E 닫기 / E 타기 / E 내리기). 걷기 = 1인칭(D-219): 가운데 조준점 + 시선 레이 2 m 에 문·카트가 맞을 때만 안내.
/// 설치: 에디터 메뉴 PitCrew/기능/집-차고 구간 설치(OnFootSetup).
/// </summary>
public partial class OnFootDirector : MonoBehaviour
{
    public enum Mode { Starting, OnFoot, InCart, Seated, Board, Pack, Calendar, Diary }

    // ── QA·봇이 읽는 상태 ──
    public Mode State { get; internal set; } = Mode.Starting;
    public string Prompt { get; private set; } = "";
    public OnFootPlayer Player => player;
    public ArcadeCarController Car => car;
    /// <summary>지금 시선 레이가 맞은 것(없으면 null) — 봇·QA 용.</summary>
    public string GazeTarget { get; internal set; }
    public readonly List<string> Log = new List<string>();
    public Transform Boss => boss;
    public DeliveryPoint FirstDrop => firstDrop;
    // ── D-250 보관소 흐름 ──
    public bool DepotFlow => orderBoard != null;
    public DepotOrderBoard OrderBoard => orderBoard;
    public DepotPackSpot PackSpot => packSpot;
    public DeliveryPoint DepotDest => depotDest;
    /// <summary>칠판에서 주문을 맡았다 — 전표(Tab)가 생기고 목적지 표식이 찻집으로.</summary>
    public bool OrderPicked => depot.PickedJob != null;
    public OrderJob PickedJob => depot.PickedJob;
    // ── D-287 2단계: 단계 클래스(감독이 만들고 부른다). 아래 공개 이름은 봇·화면·디버그가 그대로 쓰게 넘겨 주기만 ──
    internal Stage_Depot depot;
    internal Stage_DayEnd dayEnd;
    internal Stage_Drive drive;     // 3단계
    internal Stage_Door door;
    internal Stage_Return ret;
    internal Stage_Garage garage;   // 4단계
    internal Stage_Morning morning;
    internal Stage_Packing packing;
    internal Diary Diary => gaze.Diary;
    public HeldPaper Held => depot.Held;
    public void SimBoardMove(int m) => depot.SimBoardMove(m);
    public void SimBoardClose() => depot.SimBoardClose();
    public void SimTab() => depot.SimTab();
    public void RefreshBoard() => depot.RefreshBoard();
    /// <summary>칠판 보기 시점으로 옮겨 가는 중/돌아오는 중(QA·캡처용).</summary>
    public bool BoardMoving => boardMove != null;
    public RoomBed Bed => bed;
    public float DiaryAt => dayEnd.DiaryAt;
    public float MorningAt => dayEnd.MorningAt;
    public bool DiaryCanClose => dayEnd.CanClose;
    public void SimDiaryClose() => dayEnd.SimClose();
    public bool Packed => packSpot == null ? OrderPicked : packSpot.Packed;
    DotScreenUI dotUI;
    internal OpeningSequence opening;
    public OpeningSequence OpeningSeq => opening;
    Canvas overlayUI;
    readonly Dictionary<Object, Vector3> anchorOffset = new Dictionary<Object, Vector3>();
    internal bool UseDot => dotUI != null && dotUI.isActiveAndEnabled;

    internal ArcadeCarController car;
    internal Rigidbody carRb;
    UnityEngine.InputSystem.PlayerInput carInput;
    internal RunController rc;
    internal Camera cam;
    internal FollowCamera follow;
    internal OnFootCamera footCam;
    readonly List<DoorHinge> doors = new List<DoorHinge>();
    internal readonly List<FishingSpot> spots = new List<FishingSpot>();   // D-227 낚시 자리(E 앉기)
    readonly List<Canvas> hud = new List<Canvas>();
    TextMeshProUGUI promptText;
    GameObject promptRoot;
    internal GameObject dotRoot;
    readonly Gaze gaze = new Gaze();   // D-287 2단계: 시선 → 물건(Interactables/Gaze.cs)
    bool simInteract;
    float t0;

    void Awake()
    {
        WorldData.Orders?.ApplyPacking();   // D-290: 상자·완충재 궁합 → Core.PackRules
        WorldData.Numbers?.ApplyTo();// D-289: 수치 원천 world/world.yaml → Core.Rules (에셋 없으면 Core 기본값)
        DayView.Reset(); DayView.Present = true;   // 5단계: 화면은 감독 대신 View 를 읽는다
        depot = new Stage_Depot(this); dayEnd = new Stage_DayEnd(this);
        drive = new Stage_Drive(this); door = new Stage_Door(this); ret = new Stage_Return(this);
        garage = new Stage_Garage(this); morning = new Stage_Morning(this); packing = new Stage_Packing(this);
        var ot = FindFirstObjectByType<OpeningTutorial>();
        if (ot != null) ot.enabled = false;
        var drv = FindFirstObjectByType<BayLoopDriveOnly>();
        if (drv != null) drv.enabled = false;
    }

    IEnumerator Start()
    {
        yield return null;                       // RunController.Start(주문판 열기) 다음
        t0 = Time.unscaledTime;
        rc = Services.Run as RunController ?? FindFirstObjectByType<RunController>();
        car = Services.Car as ArcadeCarController ?? FindFirstObjectByType<ArcadeCarController>();
        cam = Camera.main;
        if (player == null || car == null || cam == null)
        {
            Debug.LogWarning($"[OnFoot] 배선 부족 player={player != null} car={car != null} cam={cam != null} — 끈다");
            enabled = false; yield break;
        }
        carRb = car.GetComponent<Rigidbody>();
        carInput = car.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        follow = cam.GetComponent<FollowCamera>();
        footCam = cam.GetComponent<OnFootCamera>();
        if (footCam == null) footCam = cam.gameObject.AddComponent<OnFootCamera>();
        footCam.enabled = false;
        footCam.target = player.transform;
        footCam.Fov = follow != null ? follow.BaseFov : cam.fieldOfView;   // 걷기도 게임 기본 시야각 그대로
        player.Cam = footCam;
        // 실내 디오라마(과제 20261104-1559, D-298): 2층 방 상자 안이면 카메라가 뒤·위로 빠진다
        var dio = cam.GetComponent<RoomDiorama>();
        if (dio == null) dio = cam.gameObject.AddComponent<RoomDiorama>();
        dio.Init(footCam, player.transform, () => dotRoot);

        DayLog.ResetAll();                       // 저장 방식이 없다 — 플레이마다 방학 1일째
        if (rc != null) rc.BeginNoClock();
        Services.Board?.Close();
        UrgentCallPanel.Ensure().Hide();
        Time.timeScale = 1f;
        Services.Overview?.GoToFollow();
        if (follow != null) follow.enabled = false;

        foreach (var d in FindObjectsByType<DoorHinge>(FindObjectsSortMode.None)) if (d.configured) doors.Add(d);
        foreach (var s in FindObjectsByType<FishingSpot>(FindObjectsSortMode.None)) if (s.seat != null) spots.Add(s);
        Plug();
        CollectHud();
        BuildUI();
        door.BuildHeldBox();
        dotUI = FindFirstObjectByType<DotScreenUI>();

        garage.ParkMorningCart();
        SetCarControl(false);
        if (spawn != null) player.Teleport(spawn.position, spawn.eulerAngles.y);
        if (orderBoard != null) orderBoard.SetLines(Stage_Depot.BoardLines);
        morning.HideLegacyPoints();
        var look0 = SunsetLook.Find();
        ret.LookWasOn = look0 != null && look0.IsOn;
        if (look0 != null && look0.IsOn && DepotFlow) { look0.dusk = 0f; look0.Apply(); }   // 아침은 오후 빛(D-239)
        EnterFoot();
        opening = GetComponent<OpeningSequence>();
        if (opening == null) opening = gameObject.AddComponent<OpeningSequence>();
        opening.Begin(player, calendar, bed, () => State == Mode.OnFoot);   // C-002: 방에서 눈을 뜬다
        Mark($"Start doors={doors.Count} spawn={(spawn != null ? spawn.position.ToString("F1") : "-")} 흐름={(DepotFlow ? "보관소(D-250)" : "사장 말 주문")}");
    }

    // ── D-287: 물건(Interactables)이 쏜 신호는 그 일을 맡은 단계가 직접 듣는다(감독 귀는 Mark 한 줄만) ──
    System.Action<string> markEar;

    /// <summary>시선 배선(Gaze) + 단계 귀 꽂기. Start 에서 문·낚시 자리를 모은 뒤 한 번.</summary>
    void Plug()
    {
        gaze.player = player.transform; gaze.cam = cam; gaze.car = car; gaze.boss = boss; gaze.board = orderBoard; gaze.packSpot = packSpot; gaze.pack3 = packSpot != null ? packSpot.V3Table : null;
        gaze.handover = handover; gaze.calendar = calendar; gaze.bed = bed; gaze.doors = doors; gaze.spots = spots;
        gaze.gazeReach = gazeReach; gaze.talkReach = talkReach; gaze.cartLoadReach = CartLoadReach;
        gaze.Build();
        DayState.Reset(); SyncDay();
        markEar = Mark; SignalBus.Marked += markEar;
        // 순서 = 같은 신호를 듣는 차례: Door 가 BoxTaken 을 Packing 보다 먼저(상자 듦 → 그다음 작업대 쪽 일)
        foreach (var st in Stages) st.Plug();
    }

    void Unplug()
    {
        if (markEar != null) SignalBus.Marked -= markEar;
    }

    /// <summary>하루 상태 칠판(DayState)에 지금 값을 적는다 — 물건은 감독 대신 그걸 읽는다. 매 프레임 물건 고르기 전에.</summary>
    void SyncDay()
    {
        DayState.Now = (DayState.Mode)(int)State;
        DayState.DepotFlow = DepotFlow; DayState.CartNeedsBossTalk = CartNeedsBossTalk;
        DayState.Talked = Talked; DayState.OrderPicked = OrderPicked; DayState.OrderStarted = OrderStarted; DayState.Delivered = Delivered;
        DayState.CarryingBox = CarryingBox; DayState.Knocking = Knocking; DayState.HandedOver = HandedOver; DayState.InTalk = InTalk;
        DayState.DiaryAt = dayEnd.DiaryAt; DayState.DiaryMinSec = diaryMinSec;
    }

    /// <summary>모든 단계 — 귀 꽂는 차례(Plug)이자 아침 되돌리기(DayStarted) 차례. OnDestroy 에서 귀를 뗀다.</summary>
    System.Collections.Generic.IEnumerable<DayStage> Stages
    {
        get
        {
            yield return door; yield return drive; yield return ret;
            yield return garage; yield return packing; yield return morning;
            yield return depot; yield return dayEnd;
        }
    }

    internal void Mark(string s)
    {
        string line = $"[{Time.unscaledTime - t0:F1}s] {s}";
        Log.Add(line); Debug.Log("[OnFoot] " + line);
    }

    /// <summary>봇: 이번 프레임에 E 를 누른 것과 같다.</summary>
    public void SimInteract() { simInteract = true; }

    bool InteractPressed()
    {
        bool p = simInteract || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
        simInteract = false;
        return p;
    }

    /// <summary>C-003: 첫 배달 단계 — 없음 → 쪽지 → 포장 → 실음 → 도착 → 건넴. QA·start.cs 가 읽는다.</summary>
    public string DeliveryStep => OrderRules.Step(HandedOver, Delivered, OrderStarted, OrderPicked, Packed);
    /// <summary>지금까지 거친 단계(바뀔 때마다 한 칸) — 예: 없음→쪽지→포장→실음→도착→건넴.</summary>
    public string StepTrail { get; private set; } = "없음";

    void TrackStep()
    {
        string s = DeliveryStep;
        if (StepTrail.EndsWith(s)) return;
        StepTrail += "→" + s;
        Mark("배달 단계 " + s); SignalBus.RaiseStep(s);
    }

}
