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
    public Mode State { get; private set; } = Mode.Starting;
    public string Prompt { get; private set; } = "";
    public OnFootPlayer Player => player;
    public ArcadeCarController Car => car;
    /// <summary>지금 시선 레이가 맞은 것(없으면 null) — 봇·QA 용.</summary>
    public string GazeTarget { get; private set; }
    public readonly List<string> Log = new List<string>();
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
    DotScreenUI dotUI;
    OpeningSequence opening;
    public OpeningSequence OpeningSeq => opening;
    Canvas overlayUI;
    readonly Dictionary<Object, Vector3> anchorOffset = new Dictionary<Object, Vector3>();
    bool UseDot => dotUI != null && dotUI.isActiveAndEnabled;

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

        ParkMorningCart();
        SetCarControl(false);
        if (spawn != null) player.Teleport(spawn.position, spawn.eulerAngles.y);
        if (orderBoard != null) orderBoard.SetLines(BoardLines);
        HideLegacyPoints();
        var look0 = SunsetLook.Find();
        lookWasOn = look0 != null && look0.IsOn;
        if (look0 != null && look0.IsOn && DepotFlow) { look0.dusk = 0f; look0.Apply(); }   // 아침은 오후 빛(D-239)
        EnterFoot();
        opening = GetComponent<OpeningSequence>();
        if (opening == null) opening = gameObject.AddComponent<OpeningSequence>();
        opening.Begin(this);                     // C-002: 방에서 눈을 뜬다
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
