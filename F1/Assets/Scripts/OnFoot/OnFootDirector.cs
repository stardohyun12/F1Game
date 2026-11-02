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
public class OnFootDirector : MonoBehaviour
{
    public enum Mode { Starting, OnFoot, InCart, Seated, Board, Pack, Calendar, Diary }

    [SerializeField] OnFootPlayer player;
    [SerializeField] Transform spawn;
    [SerializeField] Transform cartSpot;
    [Tooltip("1인칭 시선 레이 길이(m). 눈에서 이 거리 안에 문·카트가 맞으면 E.")]
    [SerializeField] float gazeReach = 2.0f;
    [Tooltip("이 속도(m/s) 밑이면 내릴 수 있다.")]
    [SerializeField] float exitMaxSpeed = 1.0f;
    [Tooltip("카트에서 내릴 때 차 옆 거리(m).")]
    [SerializeField] float exitSide = 1.7f;

    [Header("기능 2 — 정비소 사장 → 첫 주문(과제 20261031-0209)")]
    [Tooltip("정비소 사장 자리(1.7m 캡슐 대역). 비면 기능 1 처럼 바로 카트를 탈 수 있다.")]
    [SerializeField] Transform boss;
    [Tooltip("첫 주문 배달지(찻집 앞 길). 비면 탑승 때 주문 없이 자유 주행.")]
    [SerializeField] DeliveryPoint firstDrop;
    [Tooltip("사장에게 말 거는 시선 레이 길이(m). 사람은 문보다 멀리서 말을 건다.")]
    [SerializeField] float talkReach = 3.0f;

    [Header("D-250·251 — 택배 보관소에서 주문 받기(과제 20261102-1515)")]
    [Tooltip("보관소 안 주문 칠판. 비면 예전 기능 2(사장이 말로 찻집 주문, 탑승 = 출발).")]
    [SerializeField] DepotOrderBoard orderBoard;
    [Tooltip("포장 자리 대역(작업대 위 상자). 비면 칠판에서 고르는 것만으로 출발할 수 있다.")]
    [SerializeField] DepotPackSpot packSpot;
    [Tooltip("사장 대화 뒤 목적지 표식이 가리킬 곳(보관소 앞). 꺼진 DeliveryPoint — 표식 자리로만 쓴다.")]
    [SerializeField] DeliveryPoint depotDest;
    [Tooltip("칠판 보기 시점으로 옮겨 가는 시간(초).")]
    [SerializeField] float boardFocusSec = 0.5f;

    [Header("D-250 ③④ — 문 앞 건네기(과제 20261102-1543)")]
    [Tooltip("배달지 문 앞 대역(문짝·받는 사람). 비면 예전처럼 도착 = 끝(정산창).")]
    [SerializeField] DoorHandover handover;
    [Tooltip("배달지 표식에서 이 거리(m, 수평) 안에서 세우고 내리면 도착으로 친다(트리거를 못 밟았어도).")]
    [SerializeField] float arriveM = 15f;
    [Tooltip("돈 글자가 떠 있는 시간(초).")]
    [SerializeField] float moneySec = 2.6f;
    [Tooltip("두드린 뒤 문이 열리기까지(초).")]
    [SerializeField] float knockWaitSec = 0.9f;

    [Header("하루 끝 일기장·방 달력(과제 20261102-1951, DELIVERY-LOOP 5-5)")]
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
    public DoorHandover Handover => handover;
    /// <summary>카트에서 상자를 내려 들고 있다(화면 아래 상자).</summary>
    public bool CarryingBox { get; private set; }
    /// <summary>문을 두드렸다 ~ 받는 사람이 나올 때까지(입력 막음).</summary>
    public bool Knocking { get; private set; }
    public bool HandedOver { get; private set; }
    /// <summary>건넬 때 화물 상태 3단(0 멀쩡 · 1 조금 상함 · 2 깨짐, -1 = 아직).</summary>
    public int HandoverTierIdx { get; private set; } = -1;
    public int HandoverMoney { get; private set; }
    /// <summary>떠오르는 돈 글자("" = 없음) · 시작 시각(unscaled) · 사라지는 시각 · 세상 자리(받는 사람 손).
    /// 최소 moneySec 초, 반응 말풍선이 떠 있는 동안은 계속 + 대화 끝난 뒤 1.2초.</summary>
    public string MoneyText => moneyText != null && Time.unscaledTime < MoneyEnd ? moneyText : "";
    public float MoneyAt { get; private set; } = -99f;
    public float MoneyEnd { get; private set; } = -99f;
    public float MoneySec => moneySec;
    public Vector3 MoneyAnchor { get; private set; }
    string moneyText;
    GameObject heldBox;

    // ── QA·봇이 읽는 상태 ──
    public Mode State { get; private set; } = Mode.Starting;
    public string Prompt { get; private set; } = "";
    public OnFootPlayer Player => player;
    public ArcadeCarController Car => car;
    /// <summary>지금 시선 레이가 맞은 것(없으면 null) — 봇·QA 용.</summary>
    public string GazeTarget { get; private set; }
    public readonly List<string> Log = new List<string>();
    /// <summary>사장 대화를 끝냈는지 — 이 뒤에만 카트를 탄다.</summary>
    public bool Talked { get; private set; }
    public bool InTalk => talkLine >= 0;
    /// <summary>지금 자막 한 줄(대화 중이 아니면 "").</summary>
    public string Subtitle => InTalk ? Lines[talkLine].who + ": " + Lines[talkLine].text : "";
    public bool OrderStarted { get; private set; }
    public bool Delivered { get; private set; }
    public Transform Boss => boss;
    public DeliveryPoint FirstDrop => firstDrop;
    // ── D-250 보관소 흐름 ──
    public bool DepotFlow => orderBoard != null;
    public DepotOrderBoard OrderBoard => orderBoard;
    public DepotPackSpot PackSpot => packSpot;
    public DeliveryPoint DepotDest => depotDest;
    /// <summary>칠판에서 주문을 맡았다 — 전표(Tab)가 생기고 목적지 표식이 찻집으로.</summary>
    public bool OrderPicked => pickedJob != null;
    public OrderJob PickedJob => pickedJob;
    public bool Packed => packSpot == null ? OrderPicked : packSpot.Packed;
    /// <summary>칠판 보기 시점으로 옮겨 가는 중/돌아오는 중(QA·캡처용).</summary>
    public bool BoardMoving => boardMove != null;
    OrderJob pickedJob;
    Coroutine boardMove;
    Vector3 boardEyePos; Quaternion boardEyeRot;
    bool simBoardClose; int simBoardMove;
    // ── D-251 보관소 포장 작업대 ──
    /// <summary>포장 시점(작업대 앞)으로 옮겨 가는 중/돌아오는 중.</summary>
    public bool PackMoving => packMove != null;
    /// <summary>작업대를 떠나려고 든 상자가 내려앉길 기다리는 중.</summary>
    bool packLeaving, simPackLeave;
    Coroutine packMove;
    Vector3 packEyePos; Quaternion packEyeRot;
    /// <summary>출발 때 정한 화물 상태 시작값(0~1, 포장 결과). 음수 = 아직.</summary>
    public float StartIntegrity { get; private set; } = -1f;

    // ── UI 1판(D-240): DotScreenUI 가 읽는 것 ──
    /// <summary>지금 안내([E] 칩)가 붙을 세상 점 — 문짝 가운데·카트 손잡이·사장 머리·낚시 자리. 없으면 null.</summary>
    public Vector3? PromptAnchor { get; private set; }
    /// <summary>[E] 칩을 물건 곁이 아니라 화면 아래쪽 자리에 둔다 — 칠판 글씨·앞 나무를 가리지 않게(20261102-2305 ②). 칠판·앉기.</summary>
    public bool PromptLow { get; private set; }
    public string TalkWho => InTalk ? Lines[talkLine].who : "";
    public string TalkText => InTalk ? Lines[talkLine].text : "";
    /// <summary>지금 줄이 시작된 때(unscaled) — 말풍선 한 글자씩.</summary>
    public float TalkLineAt => talkLineAt;
    /// <summary>말하는 사람 머리 꼭대기(렌더러 경계 맨 위).</summary>
    public Vector3 SpeakerTop => TopOf(speaker != null ? speaker : boss);
    /// <summary>지금 말하는 사람(사장 또는 문 앞 받는 사람).</summary>
    public Transform Speaker => speaker != null ? speaker : boss;
    Transform speaker;                       // null = 사장
    (string who, string text)[] curLines;    // null = 사장 대사
    System.Action<int> onTalkLine;           // 줄이 바뀔 때(새 줄 번호)
    System.Action onTalkEnd;

    [Header("UI 1판(D-240) — 대화 시선")]
    [Tooltip("대화가 시작되면 카메라가 말하는 사람 쪽으로 도는 시간(초). 0 = 안 돌림.")]
    [SerializeField] float talkFocusSec = 0.5f;
    [Tooltip("화면 가운데에 둘 점 = 머리 꼭대기에서 이만큼 아래(m). 말풍선이 그 위에 들어갈 자리.")]
    [SerializeField] float talkFocusDrop = 0.05f;
    [Header("목적지 표식(D-252) — 대화 끝 시선")]
    [Tooltip("사장 대화가 끝나면 카메라가 첫 배달지 표식 쪽으로 도는 시간(초). 0 = 안 돌림(대화 시선과 같은 방식).")]
    [SerializeField] float destFocusSec = 0.5f;
    [Tooltip("표식 쪽을 본 채 멈춰 있는 시간(초) — 그 뒤 다시 플레이어 시선.")]
    [SerializeField] float destHoldSec = 0.8f;
    /// <summary>사장 대화가 끝나 첫 배달지를 들었다 — DotScreenUI 가 걷는 동안에도 목적지 표식을 그린다(탑승 전까지).</summary>
    public bool DestKnown => WalkDest != null;
    /// <summary>
    /// 주문 출발 전 목적지 표식이 가리킬 곳. 예전 흐름 = 사장 대화 뒤 찻집.
    /// 보관소 흐름(D-250) = 사장 대화 뒤 보관소 앞(depotDest) → 칠판에서 주문을 맡은 뒤 찻집.
    /// </summary>
    public DeliveryPoint WalkDest
    {
        get
        {
            if (!Talked || firstDrop == null || Delivered) return null;
            if (!DepotFlow) return firstDrop;
            return OrderPicked ? firstDrop : depotDest;
        }
    }
    /// <summary>대화 끝 시선 이동 중(QA·캡처용).</summary>
    public bool DestFocusing => destFocus != null;
    Coroutine talkFocus, destFocus;
    DotScreenUI dotUI;
    Canvas overlayUI;
    readonly Dictionary<Object, Vector3> anchorOffset = new Dictionary<Object, Vector3>();
    bool UseDot => dotUI != null && dotUI.isActiveAndEnabled;

    // 대사: docs/design/tutorial/first-delivery.yaml 의 garage 장면 1·2줄 그대로. 3줄(첫 주문 안내)은 설계에 사장 대사가 없어
    // 보관소 직원 대사("첫 주문. 달걀 한 판, 언덕 위 찻집.")를 사장 입으로 옮기고 줄리아 이름을 붙였다 — 도현 확인 필요.
    static readonly (string who, string text)[] BossLines =
    {
        ("사장", "왔냐. 오늘부터 배달이다. 대회는 그 다음."),
        ("사장", "뒤에 짐상자 달아 놨어. 깨지는 건 네 책임."),
        ("사장", "첫 주문. 달걀 한 판, 언덕 위 찻집 줄리아네."),
    };
    // D-250 보관소 흐름의 사장 대사 — 대역(도현이 정함). 정비소 = 카트 점검, 주문은 택배 보관소 칠판에서.
    static readonly (string who, string text)[] DepotBossLines =
    {
        ("사장", "왔냐. 카트는 내가 봐 놨다. 브레이크 멀쩡해."),
        ("사장", "뒤에 짐상자 달아 놨어. 깨지는 건 네 책임."),
        ("사장", "주문은 택배 보관소 칠판에 있어. 오늘은 거기 가 봐."),
    };
    (string who, string text)[] Lines => curLines ?? (DepotFlow ? DepotBossLines : BossLines);
    // 칠판 줄: 첫 주문 1줄(찻집·줄리아·달걀 한 판) + 대역 2줄 — 글은 도현이 정함.
    static readonly DepotOrderBoard.Line[] BoardLines =
    {
        new DepotOrderBoard.Line("달걀 한 판 → 언덕 위 찻집 (줄리아)", true),
        new DepotOrderBoard.Line("(주문 자리)", false),
        new DepotOrderBoard.Line("(주문 자리)", false),
    };
    int talkLine = -1;
    float talkLineAt;
    OrderJob firstJob;
    TextMeshProUGUI subText;
    GameObject subRoot;

    ArcadeCarController car;
    Rigidbody carRb;
    UnityEngine.InputSystem.PlayerInput carInput;
    RunController rc;
    Camera cam;
    FollowCamera follow;
    OnFootCamera footCam;
    readonly List<DoorHinge> doors = new List<DoorHinge>();
    readonly List<FishingSpot> spots = new List<FishingSpot>();   // D-227 낚시 자리(E 앉기)
    FishingSpot seatedAt;
    readonly List<Canvas> hud = new List<Canvas>();
    TextMeshProUGUI promptText;
    GameObject promptRoot;
    GameObject dotRoot;
    readonly RaycastHit[] gazeHits = new RaycastHit[16];
    bool simInteract;
    bool freeDriveEntered;
    float t0;

    void Awake()
    {
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

        DayLog.ResetAll();                       // 저장 방식이 없다 — 플레이마다 방학 1일째
        if (rc != null) rc.BeginNoClock();
        Services.Board?.Close();
        UrgentCallPanel.Ensure().Hide();
        Time.timeScale = 1f;
        Services.Overview?.GoToFollow();
        if (follow != null) follow.enabled = false;

        foreach (var d in FindObjectsByType<DoorHinge>(FindObjectsSortMode.None)) if (d.configured) doors.Add(d);
        foreach (var s in FindObjectsByType<FishingSpot>(FindObjectsSortMode.None)) if (s.seat != null) spots.Add(s);
        CollectHud();
        BuildUI();
        BuildHeldBox();
        dotUI = FindFirstObjectByType<DotScreenUI>();

        if (cartSpot != null) ParkCar(cartSpot.position, cartSpot.eulerAngles.y);
        SetCarControl(false);
        if (spawn != null) player.Teleport(spawn.position, spawn.eulerAngles.y);
        if (orderBoard != null) orderBoard.SetLines(BoardLines);
        EnterFoot();
        Mark($"Start doors={doors.Count} spawn={(spawn != null ? spawn.position.ToString("F1") : "-")} 흐름={(DepotFlow ? "보관소(D-250)" : "사장 말 주문")}");
    }

    void Mark(string s)
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

    void Update()
    {
        if (State == Mode.Starting) return;
        if (overlayUI != null && overlayUI.enabled == UseDot) overlayUI.enabled = !UseDot;   // 도트 UI 가 켜져 있으면 예전 화면 안내·자막을 끈다
        PromptLow = false;
        bool press = InteractPressed();
        UpdateHeldBox();
        if (moneyText != null && InTalk && handover != null && Speaker == handover.recipient) MoneyEnd = Mathf.Max(MoneyEnd, Time.unscaledTime + 1.2f);
        if (State == Mode.OnFoot)
        {
            if (InTalk) { PromptAnchor = null; UpdateTalk(press); return; }
            if (Knocking) { PromptAnchor = null; SetPrompt(""); return; }
            // 1인칭(D-219): 화면 가운데 점이 가리키는 것만 — 시선 레이 2 m 에 처음 맞은 것이 문·카트일 때.
            Object hit = Gaze(out DoorHinge door);
            GazeTarget = hit != null ? hit.name : null;
            PromptAnchor = hit == null ? (Vector3?)null : hit == car ? car.transform.position + Vector3.up * 0.8f : hit == boss ? TopOf(boss)
                         : orderBoard != null && hit == orderBoard ? orderBoard.Center : packSpot != null && hit == packSpot ? packSpot.Top
                         : handover != null && hit == handover ? handover.DoorCenter
                         : calendar != null && hit == calendar ? calendar.PromptPoint : bed != null && hit == bed ? bed.Anchor : CenterOf(door);
            if (handover != null && hit == handover)
            {
                // D-250 ③: 상자를 들고 있을 때만 두드린다. 건네기 전·후엔 그냥 문.
                if (CarryingBox && !handover.Busy && !handover.IsOpen)
                {
                    SetPrompt("E 두드리기");
                    if (press) Knock();
                }
                else { SetPrompt(""); PromptAnchor = null; }
            }
            else if (boss != null && hit == boss)
            {
                SetPrompt("E 말하기");
                if (press) BeginTalk();
            }
            else if (orderBoard != null && hit == orderBoard)
            {
                SetPrompt(OrderPicked ? "E 칠판 보기(맡음)" : "E 칠판 보기");
                PromptLow = true;
                if (press) OpenBoard();
            }
            else if (packSpot != null && hit == packSpot && packSpot.HasBench)
            {
                // D-251: 작업대 — 서서 싸고 흔들어 본 상자를 들고 나간다
                if (!OrderPicked) SetPrompt("칠판에서 주문 먼저");
                else if (packSpot.Taken) { SetPrompt(""); PromptAnchor = null; }
                else if (packSpot.Ready && !CarryingBox)
                {
                    SetPrompt("E 상자 들기");
                    if (press) TakePacked();
                }
                else
                {
                    SetPrompt(packSpot.NeedsShake ? "E 작업대(들어서 흔들어 보기)" : "E 포장하기");
                    if (press) EnterPack();
                }
            }
            else if (packSpot != null && hit == packSpot)
            {
                if (!OrderPicked) SetPrompt("칠판에서 주문 먼저");
                else if (!packSpot.Packed)
                {
                    SetPrompt("E 포장하기");
                    if (press) { packSpot.Pack(); Mark("포장됨(대역 — E 한 번)"); }
                }
                else SetPrompt("포장됨 — 카트로");
            }
            else if (calendar != null && hit == calendar)
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
                // 기능 2: 사장 말을 듣기 전엔 못 탄다. D-250: 주문을 맡았으면 포장해야 싣는다.
                if (boss != null && !Talked) SetPrompt("사장님한테 먼저");
                else if (DepotFlow && OrderPicked && !Packed) SetPrompt(packSpot != null && packSpot.Ready ? "작업대 상자 들고 오기" : "포장 먼저");
                else
                {
                    SetPrompt(DepotFlow && Packed && !OrderStarted ? "E 싣고 타기" : "E 타기");
                    if (press) EnterCart();
                }
            }
            else if (door != null)
            {
                SetPrompt(door.IsOpen ? "E 닫기" : "E 열기");
                if (press && !door.Moving) { door.Toggle(player.transform.position); Mark((door.IsOpen ? "열기 " : "닫기 ") + door.name); }
            }
            else if (NearSpot() is FishingSpot spot)
            {
                SetPrompt("E 앉기");
                PromptAnchor = spot.transform.position + Vector3.up * 0.6f;
                PromptLow = true;
                if (press) Sit(spot);
            }
            else SetPrompt("");
        }
        else if (State == Mode.Seated)
        {
            PromptAnchor = null;
            if (press) StandUp();   // 앉아 있는 동안 화면에 아무것도 없음(D-227)
        }
        else if (State == Mode.Board) UpdateBoard(press);
        else if (State == Mode.Pack) UpdatePack();
        else if (State == Mode.Calendar) UpdateCalendar(press);
        else if (State == Mode.Diary) UpdateDiary(press);
        else if (State == Mode.InCart)
        {
            bool slow = Mathf.Abs(car.CurrentSpeedMps) < exitMaxSpeed;
            PromptAnchor = car.transform.position + Vector3.up * 1.0f;
            SetPrompt(slow ? "E 내리기" : "");
            if (press && slow) { ArriveIfNear(); ExitCart(); }
        }
    }

    /// <summary>
    /// 눈(카메라)에서 앞으로 gazeReach 만큼 레이. 내 몸을 뺀 첫 번째 맞은 것이 문짝(또는 문과 같이 도는 손잡이)이면 그 문, 차 몸통이면 차.
    /// 벽·문틀이 먼저 맞으면 아무것도 아니다 — 벽 너머 문은 안 잡힌다.
    /// </summary>
    Object Gaze(out DoorHinge door)
    {
        door = null;
        Transform eye = cam.transform;
        int n = Physics.RaycastNonAlloc(eye.position, eye.forward, gazeHits, Mathf.Max(gazeReach, boss != null || orderBoard != null || handover != null ? talkReach : 0f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Collider first = null; float best = float.MaxValue; float packD = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var c = gazeHits[i].collider;
            if (c == null || c.transform.IsChildOf(player.transform)) continue;
            if (gazeHits[i].distance < best) { best = gazeHits[i].distance; first = c; }
            if (packSpot != null && c.transform.IsChildOf(packSpot.transform)) packD = Mathf.Min(packD, gazeHits[i].distance);
        }
        // 방 달력(벽에 붙은 판)·침대(충돌체 없이 경계 상자) — 앞에 다른 것이 먼저 맞으면 못 잡는다.
        if (calendar != null && first != null && first.transform.IsChildOf(calendar.transform) && best <= talkReach) return calendar;
        if (bed != null && bed.Hit(new Ray(eye.position, eye.forward), Mathf.Min(best + 0.02f, talkReach), out _)) return bed;
        // 작업대 판(BenchTop)은 카운터 윗면과 같은 높이 — 2cm 안으로 비기면 작업대가 이긴다.
        if (packD <= best + 0.02f && packD <= gazeReach) return packSpot;
        // 사장(사람)은 talkReach 까지, 나머지(문·카트)는 예전처럼 gazeReach 까지.
        if (first != null && boss != null && first.transform.IsChildOf(boss) && best <= talkReach) return boss;
        // 칠판은 벽에 붙어 있어 한 발 물러서서 본다 — 사장처럼 talkReach 까지.
        if (first != null && orderBoard != null && first.transform.IsChildOf(orderBoard.transform) && best <= talkReach) return orderBoard;
        if (first != null && packSpot != null && first.transform.IsChildOf(packSpot.transform) && best <= gazeReach) return packSpot;
        // 문 앞 건네기: 문은 사람처럼 한 발 떨어져서 두드린다(talkReach).
        if (first != null && handover != null && first.transform.IsChildOf(handover.transform) && best <= talkReach) return handover;
        if (first != null && best > gazeReach) first = null;
        // 카트는 낮아서(충돌 0.44m) 서서 내려다보면 레이가 빗나간다 — 카트 중심이 3m 안(가장자리 약 2m)이고 대략 앞쪽이면 타기 허용(PD 10/31, 기능팀 보고 1c)
        if (car != null)
        {
            Vector3 to = car.transform.position - player.transform.position; to.y = 0f;
            Vector3 fw = eye.forward; fw.y = 0f;
            if (to.magnitude < 3.0f && fw.sqrMagnitude > 1e-4f && Vector3.Dot(fw.normalized, to.normalized) > 0.5f
                && (first == null || first.GetComponentInParent<DoorHinge>() == null)) return car;
        }
        if (first == null) return null;
        var hitCar = first.GetComponentInParent<ArcadeCarController>();
        if (hitCar != null && hitCar == car) return car;
        var dh = first.GetComponentInParent<DoorHinge>();
        if (dh != null && doors.Contains(dh)) { door = dh; return dh; }
        foreach (var d in doors)
            if (d != null) foreach (var r in d.riders) if (r != null && first.transform.IsChildOf(r)) { door = d; return d; }
        return null;
    }

    void SetPrompt(string s)
    {
        if (Prompt == s) return;
        Prompt = s;
        if (promptText != null) { promptText.text = s; promptRoot.SetActive(s.Length > 0); }
    }

    void OnDestroy() { EngineAudio.OnFootMute = false; }   // 감독 없는 씬에선 엔진 소리 원래대로

    // ── 걷기 ↔ 카트 ──
    void EnterFoot()
    {
        State = Mode.OnFoot;
        EngineAudio.OnFootMute = true;
        if (follow != null) follow.enabled = false;
        player.gameObject.SetActive(true);
        footCam.Yaw = player.transform.eulerAngles.y;
        footCam.enabled = true;
        if (dotRoot != null) dotRoot.SetActive(true);
        PlacePrompt(true);
        SetHud(false);
    }

    bool debugFreeCart;   // 디버그 창의 카트 타기만 true — 첫 주문을 시작하지 않는다(정식 빌드에선 늘 false)

    void EnterCart()
    {
        Mark("카트 타기");
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        State = Mode.InCart;
        EngineAudio.OnFootMute = false;
        player.gameObject.SetActive(false);
        footCam.enabled = false;
        SetCarControl(true);
        // D-250: 보관소 흐름이면 포장까지 끝낸 뒤 탈 때 출발(정비소 → 보관소 운전은 자유 주행).
        bool ready = !DepotFlow || Packed;
        if (!OrderStarted && ready && firstDrop != null && rc != null && !debugFreeCart)
        {
            if (DepotFlow && packSpot != null) { packSpot.Load(); CarryingBox = false; Mark("상자 카트에 실음"); }
            BeginFirstOrder(); freeDriveEntered = true;
            ApplyPackResult();
        }
        else if (rc != null && !freeDriveEntered) { rc.EnterFreeDrive(); freeDriveEntered = true; }
        else Services.Overview?.GoToFollow();
        // 같은 프레임에 B 자리로 — 눈높이에서 차 뒤로 날아가는 프레임이 없다.
        if (follow != null) { follow.enabled = true; follow.SnapToTarget(); }
        if (dotRoot != null) dotRoot.SetActive(false);
        GazeTarget = null;
        PlacePrompt(false);
        SetHud(true);
        SetPrompt("");
    }

    FishingSpot NearSpot()
    {
        foreach (var s in spots) if (s != null && s.CanSit(player.transform.position)) return s;
        return null;
    }

    /// <summary>D-227 낚시 자리에 앉기 — 몸·걷기 카메라·조준점·안내를 끄고 자리(FishingSpot)가 카메라를 잡는다.</summary>
    void Sit(FishingSpot s)
    {
        Mark("앉기 " + s.name);
        State = Mode.Seated; seatedAt = s;
        EngineAudio.OnFootMute = true;
        footCam.enabled = false;
        player.gameObject.SetActive(false);
        if (dotRoot != null) dotRoot.SetActive(false);
        SetPrompt("");
        GazeTarget = null;
        s.Sit(cam);
    }

    void StandUp()
    {
        var s = seatedAt; seatedAt = null;
        Mark("일어나기 " + (s != null ? s.name : "-"));
        if (s != null) s.Stand();
        player.gameObject.SetActive(true);
        if (s != null) player.Teleport(s.StandPoint(), s.SeatYaw);
        EnterFoot();
    }

    /// <summary>개발용(메뉴 PitCrew/노을/낚시 자리로 가기): 걷는 중이면 자리 바로 뒤로 옮긴다.</summary>
    public bool GoToSpot()
    {
        if (State != Mode.OnFoot || spots.Count == 0) return false;
        var s = spots[0];
        player.Teleport(s.StandPoint(), s.SeatYaw);
        footCam.Yaw = s.SeatYaw; footCam.Pitch = 0f;
        Mark("자리로 이동 " + s.name);
        return true;
    }

    void ExitCart()
    {
        Mark($"카트 내리기 차=({car.transform.position.x:F1},{car.transform.position.y:F1},{car.transform.position.z:F1})");
        SetCarControl(false);
        if (carRb != null) { carRb.linearVelocity = Vector3.zero; carRb.angularVelocity = Vector3.zero; }
        player.gameObject.SetActive(true);
        player.Teleport(ExitPoint(), car.transform.eulerAngles.y);
        EnterFoot();
        if (Delivered && handover != null && !HandedOver && !CarryingBox) BeginCarry();
    }

    // ── D-250 ③④ 문 앞 건네기 ──
    /// <summary>배달지 표식 arriveM 안에서 세우고 내리면 도착(트리거를 못 밟았어도). 도착 처리 = 기존 DeliveryManager 경로 그대로.</summary>
    void ArriveIfNear()
    {
        if (!OrderStarted || Delivered || firstDrop == null || handover == null) return;
        Vector3 d = car.transform.position - firstDrop.transform.position; d.y = 0f;
        if (d.magnitude > arriveM) return;
        var mgr = Services.Delivery as DeliveryManager ?? FindFirstObjectByType<DeliveryManager>();
        Mark($"내린 자리가 배달지 {d.magnitude:F1}m 안(≤{arriveM:F0}) → 도착 처리");
        if (mgr != null) mgr.OnDeliveryReached(firstDrop);
    }

    /// <summary>카트 짐칸에서 상자를 내려 든다 — 짐칸 상자는 숨기고 화면 아래 상자(대역)를 켠다.</summary>
    void BeginCarry()
    {
        CarryingBox = true;
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        if (cargo != null && car != null && cargo.transform != car.transform)
            foreach (var r in cargo.GetComponentsInChildren<Renderer>()) r.enabled = false;
        Mark($"상자 내림(들고 걷기) 화물={(cargo != null ? (cargo.IntegrityPercent * 100f).ToString("F0") + "%" : "?")}");
    }

    /// <summary>걷는 동안만 손에 든 상자를 보인다(카메라는 주행과 같이 쓰므로 카트·대화 시점에선 끈다).</summary>
    void UpdateHeldBox()
    {
        if (heldBox == null) return;
        bool on = CarryingBox && State == Mode.OnFoot && footCam != null && footCam.enabled;
        if (heldBox.activeSelf != on) heldBox.SetActive(on);
    }

    void BuildHeldBox()
    {
        // 회색 대역 상자: 카메라 자식, 화면 아래 가운데 조금 오른쪽. 아트는 나중에 이 자리(HeldBox)에 모델로.
        heldBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        heldBox.name = "HeldBox";
        Destroy(heldBox.GetComponent<Collider>());
        heldBox.transform.SetParent(cam.transform, false);
        heldBox.transform.localPosition = new Vector3(0.06f, -0.34f, 0.62f);
        heldBox.transform.localRotation = Quaternion.Euler(14f, -4f, 0f);
        heldBox.transform.localScale = new Vector3(0.42f, 0.24f, 0.32f);
        var mr = heldBox.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", new Color(0.58f, 0.57f, 0.55f)); mpb.SetColor("_Color", new Color(0.58f, 0.57f, 0.55f));
        mr.SetPropertyBlock(mpb);
        heldBox.SetActive(false);
    }

    void Knock()
    {
        Knocking = true;
        player.enabled = false;
        SetPrompt(""); PromptAnchor = null;
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        float pct = cargo != null ? cargo.IntegrityPercent : 1f;
        HandoverTierIdx = HandoverTier.Of(pct);
        handover.PlayKnock();
        Mark($"똑똑 — 문 {handover.name} 화물 {pct * 100f:F0}% → {HandoverTier.Name[HandoverTierIdx]}");
        StartCoroutine(KnockSeq(pct));
    }

    IEnumerator KnockSeq(float pct)
    {
        yield return new WaitForSecondsRealtime(knockWaitSec);
        yield return handover.Open(player.transform.position);
        Knocking = false;
        string who = handover.recipientName;
        var lines = new[] { (who, HandoverTier.Greeting), (who, HandoverTier.Reaction[HandoverTierIdx]) };
        Mark($"{who} 나옴 {handover.recipient.position.ToString("F1")}");
        BeginTalk(lines, handover.recipient, line => { if (line == 1) GiveBox(pct); }, EndHandover);
    }

    /// <summary>둘째 줄(반응)로 넘어갈 때 상자를 건넨다 — 상자가 받는 사람 손으로 옮겨 가고, 돈 글자가 떠오른다.</summary>
    void GiveBox(float pct)
    {
        CarryingBox = false;
        if (heldBox != null) StartCoroutine(MoveBoxToHands());
        int money = rc != null ? rc.PayHandover(HandoverTier.Pay[HandoverTierIdx], pct) : 0;
        HandoverMoney = money;
        moneyText = $"+{money:N0}원";
        MoneyAt = Time.unscaledTime;
        MoneyEnd = MoneyAt + moneySec;
        MoneyAnchor = handover.HandsPoint;
        Mark($"상자 건넴 → {HandoverTier.Name[HandoverTierIdx]} 반응 '{HandoverTier.Reaction[HandoverTierIdx]}' 돈 {moneyText}(×{HandoverTier.Pay[HandoverTierIdx]:F1}) 정산창={ResultPanel.ShowCount}회");
    }

    IEnumerator MoveBoxToHands()
    {
        // 카메라 자식에서 떼어 세상에서 받는 사람 손 앞으로(0.35초), 그 뒤 받는 사람 자식으로.
        var go = heldBox; heldBox = null; heldBoxGiven = go;       // UpdateHeldBox 가 더는 안 건드린다
        var t = go.transform;
        go.SetActive(true);
        t.SetParent(null, true);
        Vector3 p0 = t.position; Quaternion r0 = t.rotation;
        var rcp = handover.recipient;
        for (float k = 0f; k < 0.35f; k += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float s = Mathf.SmoothStep(0f, 1f, k / 0.35f);
            t.SetPositionAndRotation(Vector3.Lerp(p0, handover.HandsPoint, s), Quaternion.Slerp(r0, rcp.rotation, s));
            yield return null;
        }
        t.SetPositionAndRotation(handover.HandsPoint, rcp.rotation);
        t.SetParent(rcp, true);
    }
    GameObject heldBoxGiven;

    void EndHandover()
    {
        HandedOver = true;
        string metWho = firstJob != null && !string.IsNullOrEmpty(firstJob.recipientName) ? firstJob.recipientName : handover.recipientName;
        DayLog.Record(metWho, firstJob != null ? firstJob.callerName : "", HandoverTier.Name[HandoverTierIdx], HandoverMoney);
        Mark($"건네기 끝 — {HandoverTier.Name[HandoverTierIdx]} 돈 {HandoverMoney} 정산창 {ResultPanel.ShowCount}회");
        StartCoroutine(CloseDoorLater());
    }

    IEnumerator CloseDoorLater()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        yield return handover.Close();
        if (heldBoxGiven != null) heldBoxGiven.SetActive(false);
    }

    // ── 기능 2: 사장 대화 → 첫 주문 ──
    void BeginTalk() => BeginTalk(null, null, null, null);

    /// <summary>대화 시작. lines·who 가 null 이면 사장 대사. 문 앞 건네기도 같은 말풍선·시선 이동(D-240)을 쓴다.</summary>
    void BeginTalk((string who, string text)[] lines, Transform who, System.Action<int> onLine, System.Action onEnd)
    {
        curLines = lines; speaker = who; onTalkLine = onLine; onTalkEnd = onEnd;
        var sp = Speaker;
        talkLine = 0; talkLineAt = Time.unscaledTime;
        player.enabled = false;                                    // 듣는 동안 안 걷는다(시선은 그대로)
        Vector3 to = player.transform.position - sp.position; to.y = 0f;
        if (to.sqrMagnitude > 1e-4f) sp.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        SetPrompt("");
        ShowSubtitle();
        if (talkFocusSec > 0f) { if (talkFocus != null) StopCoroutine(talkFocus); talkFocus = StartCoroutine(FocusSpeaker()); }
        Mark(sp == boss ? "사장 대화 시작" : sp.name + " 대화 시작");
    }

    /// <summary>
    /// UI 1판(D-240): 대화가 시작되면 카메라가 말하는 사람 쪽으로 talkFocusSec 동안 부드럽게 돈다(머리가 화면 가운데 근처, 말풍선은 그 위).
    /// 말하는 사람이 화면 밖이어도 이 회전으로 화면 안에 들어온다. 대화 동안 마우스 시선은 잠그고, 끝나면 그 자리에서 다시 플레이어 조작.
    /// </summary>
    IEnumerator FocusSpeaker()
    {
        footCam.HoldLook = true;
        float y0 = footCam.Yaw, p0 = footCam.Pitch;
        footCam.LookAt(SpeakerTop - Vector3.up * talkFocusDrop);
        float dy = Mathf.DeltaAngle(y0, footCam.Yaw), p1 = footCam.Pitch;
        Mark($"시선 이동 {dy:F1}° 옆 / {p1 - p0:F1}° 위아래, {talkFocusSec:F2}s");
        // 한 프레임이 길어도(첫 한글 글자 굽기 등) 최대 1/30초씩만 — 회전이 한 번에 건너뛰지 않게.
        for (float t = 0f; t < talkFocusSec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t / talkFocusSec);
            footCam.Yaw = y0 + dy * k; footCam.Pitch = Mathf.Lerp(p0, p1, k);
            yield return null;
        }
        footCam.Yaw = y0 + dy; footCam.Pitch = p1;
        talkFocus = null;
    }

    /// <summary>개발·캡처용: 시선이 어디를 보든 바로 사장 대화를 시작한다(화면 밖 사장 → 시선 이동 확인).</summary>
    public bool ForceTalk()
    {
        if (State != Mode.OnFoot || InTalk || boss == null) return false;
        BeginTalk();
        return true;
    }

    Vector3 TopOf(Transform t)
    {
        if (t == null) return Vector3.zero;
        if (!anchorOffset.TryGetValue(t, out var off))
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) off = Vector3.up * 1.8f;
            else { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); off = new Vector3(b.center.x, b.max.y, b.center.z) - t.position; }
            anchorOffset[t] = off;
        }
        return t.position + off;
    }

    Vector3? CenterOf(DoorHinge d)
    {
        if (d == null) return null;
        var rs = d.GetComponentsInChildren<Renderer>();   // 문은 돌아가므로 매번(문 몇 개뿐)
        if (rs.Length == 0) return d.transform.position;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.center;
    }

    void UpdateTalk(bool press)
    {
        if (!press || Time.unscaledTime - talkLineAt < 0.3f) return;   // 한 줄은 최소 0.3초(연타로 건너뛰기 방지)
        talkLine++; talkLineAt = Time.unscaledTime;
        if (talkLine < Lines.Length) { ShowSubtitle(); onTalkLine?.Invoke(talkLine); return; }
        talkLine = -1;
        player.enabled = true;
        if (talkFocus != null) { StopCoroutine(talkFocus); talkFocus = null; }
        footCam.HoldLook = false;                                  // 다시 플레이어 시선
        if (subRoot != null) subRoot.SetActive(false);
        if (curLines != null)
        {
            // 문 앞 건네기 등 사장 아닌 대화 — 사장 대화 끝 처리(목적지 시선)는 안 한다.
            var end = onTalkEnd;
            curLines = null; speaker = null; onTalkLine = null; onTalkEnd = null;
            end?.Invoke();
            return;
        }
        Talked = true;
        Mark("사장 대화 끝" + (DepotFlow ? " → 보관소로" : ""));
        if (destFocusSec > 0f && WalkDest != null && UseDot) destFocus = StartCoroutine(FocusDest(WalkDest));
    }

    // ── 과제 20261102-1951: 방 달력 보기 · 하루 끝 일기장 · 다음 날 아침 ──
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
    /// <summary>칠판 보기 시점: 카메라가 boardFocusSec 동안 칠판 앞(DepotOrderBoard.ViewPose)으로 옮겨 가 정면으로 본다. 몸은 그 자리.</summary>
    void OpenBoard()
    {
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        State = Mode.Board;
        player.enabled = false;
        footCam.enabled = false;
        boardEyePos = cam.transform.position; boardEyeRot = cam.transform.rotation;
        orderBoard.SetHint(true);
        SetPrompt(""); PromptAnchor = null; GazeTarget = null;
        if (dotRoot != null) dotRoot.SetActive(false);
        orderBoard.ViewPose(cam.fieldOfView, cam.aspect, out var pos, out var rot);
        if (boardMove != null) StopCoroutine(boardMove);
        boardMove = StartCoroutine(MoveCam(pos, rot, boardFocusSec, null));
        Mark($"칠판 보기 시점 {pos.ToString("F2")} 칠판까지 {Vector3.Distance(pos, orderBoard.Center):F2}m fov={cam.fieldOfView:F0}");
    }

    void CloseBoard()
    {
        orderBoard.SetHint(false);
        if (boardMove != null) StopCoroutine(boardMove);
        boardMove = StartCoroutine(MoveCam(boardEyePos, boardEyeRot, boardFocusSec * 0.6f, () =>
        {
            player.enabled = true;
            EnterFoot();
            footCam.HoldLook = false;
            Mark("칠판 닫기");
            if (OrderPicked && destFocusSec > 0f && WalkDest != null && UseDot) destFocus = StartCoroutine(FocusDest(WalkDest));
        }));
    }

    IEnumerator MoveCam(Vector3 to, Quaternion toRot, float sec, System.Action done)
    {
        Vector3 p0 = cam.transform.position; Quaternion r0 = cam.transform.rotation;
        for (float t = 0f; t < sec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t / sec);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(p0, to, k), Quaternion.Slerp(r0, toRot, k));
            yield return null;
        }
        cam.transform.SetPositionAndRotation(to, toRot);
        boardMove = null;
        done?.Invoke();
    }

    /// <summary>봇: 칠판에서 W(-1)/S(+1) 한 번, 닫기(Esc).</summary>
    public void SimBoardMove(int d) { simBoardMove = d; }
    public void SimBoardClose() { simBoardClose = true; }

    void UpdateBoard(bool press)
    {
        PromptAnchor = null;
        var kb = Keyboard.current;
        int mv = simBoardMove; simBoardMove = 0;
        if (kb != null) { if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) mv = -1; if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) mv = 1; }
        bool close = simBoardClose || (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame));
        simBoardClose = false;
        if (boardMove != null) return;                                      // 옮겨 가는 동안은 입력 안 받음
        if (mv != 0) orderBoard.Move(mv);
        if (close) { CloseBoard(); return; }
        if (!press) return;
        if (OrderPicked) { CloseBoard(); return; }                          // 이미 맡았으면 E = 닫기
        if (!orderBoard.IsOpenLine(orderBoard.Selected)) { Mark($"칠판 {orderBoard.Selected + 1}줄 = 대역(주문 자리) — 못 맡음"); return; }
        orderBoard.Pick();
        pickedJob = MakeFirstJob();
        if (packSpot != null) packSpot.ShowCargo();                        // D-251: 맡은 화물(달걀 한 판)이 작업대에 놓인다
        Mark($"주문 맡음: {pickedJob.displayName} → 전표·목적지 표식 {firstDrop?.Label}");
        StartCoroutine(CloseBoardAfter(0.6f));
    }

    IEnumerator CloseBoardAfter(float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        if (State == Mode.Board && boardMove == null) CloseBoard();
    }

    // ── D-251: 보관소 포장 작업대(Proto_PackBench 그대로) ──
    /// <summary>작업대 앞에 서기: 카메라가 칠판 보기와 같은 방식으로 작업대 눈 자리로 옮겨 간 뒤 PackBench 가 마우스·시선을 맡는다. 몸은 그 자리.</summary>
    void EnterPack()
    {
        if (packSpot == null || packSpot.Eye == null) return;
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        State = Mode.Pack; packLeaving = false;
        player.enabled = false;
        footCam.enabled = false;
        packEyePos = cam.transform.position; packEyeRot = cam.transform.rotation;
        SetPrompt(""); PromptAnchor = null; GazeTarget = null;
        if (dotRoot != null) dotRoot.SetActive(false);
        var e = packSpot.Eye;
        if (packMove != null) StopCoroutine(packMove);
        packMove = StartCoroutine(MovePackCam(e.position, e.rotation, boardFocusSec, () => packSpot.SetPacking(true, cam)));
        Mark($"작업대 서기 눈 {e.position.ToString("F2")} 내려다봄 {e.eulerAngles.x:F0}°");
    }

    /// <summary>봇: 작업대에서 물러나기(Q 와 같은 길).</summary>
    public void SimPackLeave() { simPackLeave = true; }

    void UpdatePack()
    {
        PromptAnchor = null;
        var kb = Keyboard.current;
        bool leave = simPackLeave || (kb != null && kb.qKey.wasPressedThisFrame);
        simPackLeave = false;
        if (packMove != null) return;                                       // 옮겨 가는 동안은 입력 안 받음
        var bench = packSpot.Bench;
        if (leave && !packLeaving) { packLeaving = true; bench.ReleaseAll(); }
        if (packLeaving && bench.CanLeave) LeavePack();
    }

    void LeavePack()
    {
        packLeaving = false;
        packSpot.SetPacking(false);                                         // PackBench.OnDisable 가 커서 잠금을 푼다 → 걷기 카메라가 다시 잠금
        if (packMove != null) StopCoroutine(packMove);
        packMove = StartCoroutine(MovePackCam(packEyePos, packEyeRot, boardFocusSec * 0.6f, () =>
        {
            player.enabled = true;
            EnterFoot();
            footCam.HoldLook = false;
            Mark(packSpot.Ready ? $"작업대에서 물러남 — 흔들어 본 상자 있음(시작 {packSpot.StartIntegrity01 * 100f:F0}%)" : "작업대에서 물러남");
        }));
    }

    IEnumerator MovePackCam(Vector3 to, Quaternion toRot, float sec, System.Action done)
    {
        yield return MoveCam(to, toRot, sec, null);
        packMove = null;
        done?.Invoke();
    }

    /// <summary>걷기 시점에서 흔들어 본 상자를 든다 → 화면 아래 든 상자(HeldBox 대역), 카트에서 [E] 싣고 타기.</summary>
    void TakePacked()
    {
        packSpot.Take();
        CarryingBox = true;
        Mark($"작업대 상자 들기 rattle={packSpot.Result.rattle:F2} → 화물 시작 {packSpot.StartIntegrity01 * 100f:F0}% 예정");
    }

    /// <summary>출발 직후(BeginRun 이 화물을 새것으로 채운 뒤) 포장 결과를 화물 상태 시작값으로.</summary>
    void ApplyPackResult()
    {
        if (packSpot == null || !packSpot.HasBench || !packSpot.Taken) return;
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        if (cargo == null) { Mark("포장 결과 적용 실패: CargoBox 없음"); return; }
        cargo.SetStartIntegrity(packSpot.StartIntegrity01);
        StartIntegrity = cargo.IntegrityPercent;
        var r = packSpot.Result;
        Mark($"출발 화물 상태 {StartIntegrity * 100f:F0}% ← 포장 rattle={r.rattle:F2} upright={r.upright} escaped={r.escaped} gap={r.gap:F3}");
    }

    /// <summary>
    /// D-252: 목적지 표식이 처음 뜨는 순간(사장이 "언덕 위 찻집"이라고 말한 뒤) 시선이 한 번 그쪽으로 — FocusSpeaker 와 같은 방식(destFocusSec 동안 SmoothStep).
    /// 멀리(수백 m) 있어도 표식은 도트 UI 라 건물에 안 가린다. 위아래는 반만 따라간다(먼 점을 정확히 보면 하늘·땅만 보여서).
    /// </summary>
    IEnumerator FocusDest(DeliveryPoint target)
    {
        footCam.HoldLook = true;
        float y0 = footCam.Yaw, p0 = footCam.Pitch;
        footCam.LookAt(DotScreenUI.DestAnchorOf(target));
        float dy = Mathf.DeltaAngle(y0, footCam.Yaw), p1 = Mathf.Lerp(p0, footCam.Pitch, 0.5f);
        Mark($"목적지 시선 {dy:F1}° 옆 / {p1 - p0:F1}° 위아래, {destFocusSec:F2}s + {destHoldSec:F2}s");
        for (float t = 0f; t < destFocusSec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t / destFocusSec);
            footCam.Yaw = y0 + dy * k; footCam.Pitch = Mathf.Lerp(p0, p1, k);
            yield return null;
        }
        footCam.Yaw = y0 + dy; footCam.Pitch = p1;
        yield return new WaitForSecondsRealtime(destHoldSec);
        footCam.HoldLook = false;
        destFocus = null;
    }

    void ShowSubtitle()
    {
        Mark("자막 " + Subtitle);
        if (subText == null) return;
        subText.text = "<b>" + Lines[talkLine].who + "</b>  " + Lines[talkLine].text +"  <size=70%><color=#FFFFFFAA>[E]</color></size>";
        subRoot.SetActive(true);
    }

    /// <summary>
    /// 첫 탑승 = 첫 주문 출발. OpeningTutorial 과 같은 달걀 주문을 만들어 기존 RunController.BeginRun 으로 시작한다
    /// (DeliveryManager 경로·화물 종류·HUD 화살표 모두 기존 그대로). 다른 배달지는 꺼서 화살표가 찻집만 가리키게.
    /// </summary>
    void BeginFirstOrder()
    {
        var mgr = Services.Delivery as DeliveryManager ?? FindFirstObjectByType<DeliveryManager>();
        if (mgr == null) { Mark("첫 주문 실패: DeliveryManager 없음"); rc.EnterFreeDrive(); return; }
        foreach (var dp in FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None)) if (dp != firstDrop) { if (!pointsOff.Contains(dp)) pointsOff.Add(dp); dp.gameObject.SetActive(false); }
        firstDrop.gameObject.SetActive(true);
        var route = new List<DeliveryPoint> { firstDrop };
        mgr.SetPoints(route, null);                                // 귀환 없음 — 찻집 도착이 곧 끝(범위: 출발 → 도착 감지)
        firstJob = pickedJob != null ? pickedJob : MakeFirstJob();   // D-250: 칠판에서 맡은 주문 그대로
        mgr.OnDeliveryCompleted.AddListener(OnFirstDelivered);
        var progress = FindFirstObjectByType<DeliveryProgressHUD>(FindObjectsInactive.Include);
        if (progress != null) progress.gameObject.SetActive(true);
        rc.DoorHandover = handover != null;                        // D-250 ③: 도착 = 정산창 대신 문 앞 건네기
        rc.BeginRun(firstJob);
        OrderStarted = true;
        var t = mgr.CurrentTarget;
        Mark($"첫 주문 출발 화물={firstJob.cargoType} 목표={(t != null ? t.Label + " " + t.transform.position.ToString("F1") : "없음")}");
    }

    OrderJob MakeFirstJob()
    {
        var j = ScriptableObject.CreateInstance<OrderJob>();
        j.name = "TUT_FirstEggs"; j.displayName = "달걀 한 판 → 언덕 위 찻집"; j.cargoType = CargoType.Eggs;
        j.destinationLabels = new List<string> { firstDrop.Label }; j.timeLimitSec = 0f; j.baseReward = 1200; j.timeBonus = 0;
        j.callerName = "언덕 위 찻집";
        j.recipientName = "줄리아";                                 // 사장 대사(BossLines)의 이름 — 도현 확인 필요(위 주석)
        return j;
    }

    void OnFirstDelivered(DeliveryPoint dp)
    {
        if (dp != firstDrop || Delivered) return;
        Delivered = true;
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        Mark($"찻집 도착 {dp.Label} 화물={(cargo != null ? (cargo.IntegrityPercent * 100f).ToString("F0") + "%" : "?")}");
    }

    Vector3 ExitPoint()
    {
        Transform c = car.transform;
        foreach (var off in new[] { -c.right * exitSide, c.right * exitSide, -c.forward * (exitSide + 1.2f), c.forward * (exitSide + 1.2f) })
        {
            Vector3 p = c.position + off;
            if (!Physics.Raycast(p + Vector3.up * 2.5f, Vector3.down, out var hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            p = hit.point + Vector3.up * 0.05f;
            var cc = player.Controller;
            if (Physics.CheckCapsule(p + Vector3.up * (cc.radius + 0.05f), p + Vector3.up * (cc.height - cc.radius), cc.radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            return p;
        }
        return c.position + Vector3.up * 1.5f;   // 다 막혔으면 차 위(떨어져 내려옴)
    }

    void SetCarControl(bool on)
    {
        car.SetMoveInput(Vector2.zero);
        if (carInput != null) carInput.enabled = on;
    }

    void ParkCar(Vector3 pos, float yaw)
    {
        if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out var hit, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            pos.y = hit.point.y + 0.05f;
        var rot = Quaternion.Euler(0f, yaw, 0f);
        car.transform.SetPositionAndRotation(pos, rot);
        if (carRb != null) { carRb.position = pos; carRb.rotation = rot; carRb.linearVelocity = Vector3.zero; carRb.angularVelocity = Vector3.zero; }
        Physics.SyncTransforms();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // ── 개발 디버그 창(D-248, DebugWindow)이 부르는 것 — 정식 빌드엔 없다 ──
    public bool DebugReady => player != null && cam != null && State != Mode.Starting;
    public FishingSpot DebugSpot => spots.Count > 0 ? spots[0] : null;
    public Vector3 DebugCartSpot => cartSpot != null ? cartSpot.position : Vector3.zero;

    /// <summary>대화·앉기·카트에서 빠져나와 걷기 몸을 켠다(위치는 그대로). 카메라·시야각은 각자 정리한다.</summary>
    void DebugLeaveState()
    {
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        if (InTalk)
        {
            talkLine = -1;
            curLines = null; speaker = null; onTalkLine = null; onTalkEnd = null;
            player.enabled = true;
            if (talkFocus != null) { StopCoroutine(talkFocus); talkFocus = null; }
            footCam.HoldLook = false;
            if (subRoot != null) subRoot.SetActive(false);
        }
        if (State == Mode.Board)
        {
            if (boardMove != null) { StopCoroutine(boardMove); boardMove = null; }
            orderBoard.SetHint(false);
            cam.transform.SetPositionAndRotation(boardEyePos, boardEyeRot);
            player.enabled = true;
        }
        if (State == Mode.Pack)
        {
            if (packMove != null) { StopCoroutine(packMove); packMove = null; }
            packSpot.Bench.ReleaseAll(); packSpot.SetPacking(false); packLeaving = false;
            cam.transform.SetPositionAndRotation(packEyePos, packEyeRot);
            player.enabled = true;
        }
        if (State == Mode.Seated)
        {
            var s = seatedAt; seatedAt = null;
            if (s != null) s.Stand();
            player.gameObject.SetActive(true);
        }
        else if (State == Mode.InCart)
        {
            SetCarControl(false);
            if (carRb != null) { carRb.linearVelocity = Vector3.zero; carRb.angularVelocity = Vector3.zero; }
            player.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 걷기 상태로 feet 에 순간이동(땅은 레이로 맞춘다). carPos 가 있으면 카트도 그 자리로. 결과 한 줄(땅 높이·카트)을 돌려준다.
    /// </summary>
    public string DebugTeleport(Vector3 feet, float yaw, Vector3? carPos, float carYaw)
    {
        if (!DebugReady) return "감독이 아직 시작 전";
        DebugLeaveState();
        string g = "땅 못 찾음(표 높이 그대로)";
        if (Physics.Raycast(feet + Vector3.up * 1.5f, Vector3.down, out var hit, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        { feet = hit.point + Vector3.up * 0.05f; g = $"땅 {hit.collider.name} y={hit.point.y:F2}"; }
        string c = "카트 그대로";
        if (carPos.HasValue) { ParkCar(carPos.Value, carYaw); c = "카트 " + car.transform.position.ToString("F1"); }
        player.Teleport(feet, yaw);
        EnterFoot();
        footCam.Yaw = yaw; footCam.Pitch = 0f;
        Mark($"디버그 이동 {feet.ToString("F1")} yaw{yaw:F0} / {g} / {c}");
        return $"{feet.ToString("F1")} {g} {c}";
    }

    /// <summary>카트를 walker 주변 빈자리(오른쪽·왼쪽·뒤·앞 순)에 놓을 후보. 다 막히면 null(= 카트는 그대로).</summary>
    public Vector3? DebugFindCarSpot(Vector3 feet, float yaw)
    {
        var fw = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward; var rt = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        var half = new Vector3(1.0f, 0.5f, 2.2f);
        foreach (var off in new[] { rt * 3.5f, -rt * 3.5f, -fw * 5f, fw * 5f, rt * 6f, -rt * 6f })
        {
            Vector3 p = feet + off;
            if (!Physics.Raycast(p + Vector3.up * 2.5f, Vector3.down, out var h, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(h.point.y - feet.y) > 1.2f) continue;
            Vector3 center = h.point + Vector3.up * 0.7f;
            if (Physics.CheckBox(center, half, Quaternion.Euler(0f, yaw, 0f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            return h.point;
        }
        // 경사라 위 검사에서 다 떨어졌으면: 높이 차 3 m 까지 받고 상자를 0.5 m 띄워 검사(내려앉는 건 ParkCar 가 맡는다)
        foreach (var off in new[] { rt * 3.5f, -rt * 3.5f, -fw * 5f, fw * 5f })
        {
            Vector3 p = feet + off;
            if (!Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out var h, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(h.point.y - feet.y) > 3f) continue;
            if (Physics.CheckBox(h.point + Vector3.up * 1.2f, half, Quaternion.Euler(0f, yaw, 0f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            return h.point;
        }
        return null;
    }

    /// <summary>카트 바로 옆(내릴 때 서는 자리)으로 — 카트를 보고 선다.</summary>
    public string DebugGoToCar()
    {
        if (!DebugReady) return "감독이 아직 시작 전";
        DebugLeaveState();
        Vector3 p = ExitPoint();
        Vector3 to = car.transform.position - p; to.y = 0f;
        float yaw = to.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(to).eulerAngles.y : 0f;
        player.Teleport(p, yaw);
        EnterFoot();
        footCam.Yaw = yaw; footCam.Pitch = 15f;
        Mark($"디버그 카트 옆 {p.ToString("F1")} 카트 {car.transform.position.ToString("F1")}");
        return $"{p.ToString("F1")} 카트 {car.transform.position.ToString("F1")}";
    }

    /// <summary>낚시 자리로 가서 앉는다(앉기 + 찌 던지기까지 — 앉으면 자리가 알아서 던진다).</summary>
    public string DebugFishNow()
    {
        if (!DebugReady) return "감독이 아직 시작 전";
        var s = DebugSpot;
        if (s == null) return "낚시 자리 없음";
        DebugLeaveState();
        player.Teleport(s.StandPoint(), s.SeatYaw);
        EnterFoot();
        footCam.Yaw = s.SeatYaw; footCam.Pitch = 0f;
        Sit(s);
        return "앉음 " + s.name;
    }

    public bool DebugRecast() => seatedAt != null && seatedAt.Recast();

    /// <summary>
    /// QA(과제 20261102-1543): 첫 주문을 출발시키고(아직이면) 카트를 배달지 앞 길에 세운 뒤 화물 HP 를 hp01 로 맞추고 도착·내리기까지.
    /// 그 뒤는 사람이 하듯 문을 보고 E. 운전을 건너뛰고 3단 반응만 빨리 보려는 것.
    /// </summary>
    public string DebugArriveForHandover(float hp01, Vector3 carPos, float carYaw, Vector3 feet, float feetYaw)
    {
        if (!DebugReady) return "감독이 아직 시작 전";
        if (handover == null || firstDrop == null) return "배선 없음(handover/firstDrop)";
        if (Delivered) return "이미 도착함 — Play 다시";
        DebugLeaveState();
        Talked = true;
        if (!OrderStarted) { if (pickedJob == null) pickedJob = MakeFirstJob(); BeginFirstOrder(); freeDriveEntered = true; }
        ParkCar(carPos, carYaw);
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        if (cargo != null) cargo.DebugSetIntegrity(hp01);
        var mgr = Services.Delivery as DeliveryManager ?? FindFirstObjectByType<DeliveryManager>();
        if (mgr != null) mgr.OnDeliveryReached(firstDrop);
        SetCarControl(false);
        player.gameObject.SetActive(true);
        player.Teleport(feet, feetYaw);
        EnterFoot();
        footCam.Yaw = feetYaw; footCam.Pitch = 0f;
        if (!CarryingBox) BeginCarry();
        return $"도착={Delivered} 들기={CarryingBox} 화물={(cargo != null ? (cargo.IntegrityPercent * 100f).ToString("F0") : "?")}% runActive={(rc != null && rc.RunActive)} 정산창={ResultPanel.ShowCount}";
    }

    /// <summary>QA: 시선이 어디를 보든 바로 두드린다(문이 화면 밖 → 받는 사람이 시선 이동으로 들어오는지).</summary>
    public bool DebugKnock()
    {
        if (State != Mode.OnFoot || !CarryingBox || handover == null || handover.Busy || handover.IsOpen || InTalk || Knocking) return false;
        Knock();
        return true;
    }

    /// <summary>타기 ↔ 내리기. 정지 속도 조건·사장 대화 조건 없이 바로.</summary>
    public string DebugToggleCart()
    {
        if (!DebugReady) return "감독이 아직 시작 전";
        if (State == Mode.InCart) { ExitCart(); return "내림"; }
        DebugLeaveState();
        debugFreeCart = true;   // 첫 주문(포장 화면·timeScale 0)을 건드리지 않고 자유 주행으로 탄다
        EnterCart();
        debugFreeCart = false;
        return "탐";
    }
#endif

    // ── 화면 ──
    void CollectHud()
    {
        foreach (var m in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (m == null) continue;
            string n = m.GetType().Name;
            if (!(n.EndsWith("HUD") || n == "CargoWarningHUD")) continue;
            var c = m.GetComponentInParent<Canvas>(true);
            if (c != null) { c = c.rootCanvas; if (!hud.Contains(c)) hud.Add(c); }
        }
    }

    void SetHud(bool on) { foreach (var c in hud) if (c != null) c.enabled = on; }

    void BuildUI()
    {
        var anyTmp = FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        var font = anyTmp != null && anyTmp.font != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
        var go = new GameObject("OnFootUI", typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        var ui = go.GetComponent<Canvas>(); ui.renderMode = RenderMode.ScreenSpaceOverlay; ui.sortingOrder = 90;
        overlayUI = ui;
        var sc = go.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        // 한 줄 안내: 반투명 띠 + 글(외곽선은 폰트 재질을 건드리므로 안 씀). 아트는 나중에 띠 스프라이트 슬롯에.
        var bg = new GameObject("Prompt", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var rt = (RectTransform)bg.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 150f); rt.sizeDelta = new Vector2(320f, 64f);
        var img = bg.GetComponent<Image>(); img.color = new Color(0f, 0f, 0f, 0.45f); img.raycastTarget = false;
        var tgo = new GameObject("Text", typeof(RectTransform));
        tgo.transform.SetParent(bg.transform, false);
        var trt = (RectTransform)tgo.transform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        promptText = tgo.AddComponent<TextMeshProUGUI>();
        promptText.font = font; promptText.fontSize = 38; promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white; promptText.raycastTarget = false;
        promptRoot = bg;
        bg.SetActive(false);
        // 조준점: 화면 가운데 작은 점(걷는 동안만). 아트는 나중에 이 Image 의 스프라이트 슬롯에.
        dotRoot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
        dotRoot.transform.SetParent(go.transform, false);
        var drt = (RectTransform)dotRoot.transform;
        drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero; drt.sizeDelta = new Vector2(8f, 8f);
        var dimg = dotRoot.GetComponent<Image>(); dimg.color = new Color(1f, 1f, 1f, 0.85f); dimg.raycastTarget = false;
        dotRoot.SetActive(false);
        // 사장 대사 자막(기능 2): 화면 아래 넓은 반투명 띠. 글만 — 연출은 나중.
        subRoot = new GameObject("Subtitle", typeof(RectTransform), typeof(Image));
        subRoot.transform.SetParent(go.transform, false);
        var srt = (RectTransform)subRoot.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f); srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 90f); srt.sizeDelta = new Vector2(1200f, 96f);
        var simg = subRoot.GetComponent<Image>(); simg.color = new Color(0f, 0f, 0f, 0.55f); simg.raycastTarget = false;
        var sgo = new GameObject("Text", typeof(RectTransform));
        sgo.transform.SetParent(subRoot.transform, false);
        var strt = (RectTransform)sgo.transform; strt.anchorMin = Vector2.zero; strt.anchorMax = Vector2.one; strt.offsetMin = new Vector2(32f, 0f); strt.offsetMax = new Vector2(-32f, 0f);
        subText = sgo.AddComponent<TextMeshProUGUI>();
        subText.font = font; subText.fontSize = 40; subText.alignment = TextAlignmentOptions.Center;
        subText.color = Color.white; subText.raycastTarget = false; subText.richText = true;
        subRoot.SetActive(false);
    }

    /// <summary>걷기 = 안내를 조준점 바로 아래에(보는 곳 옆에서 읽게) · 카트 = 화면 아래 띠.</summary>
    void PlacePrompt(bool nearDot)
    {
        if (promptRoot == null) return;
        var rt = (RectTransform)promptRoot.transform;
        if (nearDot) { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0f, -40f); rt.sizeDelta = new Vector2(220f, 52f); promptText.fontSize = 30; }
        else { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0f, 150f); rt.sizeDelta = new Vector2(320f, 64f); promptText.fontSize = 38; }
    }
}
