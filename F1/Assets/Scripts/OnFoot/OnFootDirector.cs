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
    public enum Mode { Starting, OnFoot, InCart, Seated }

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
    public string Subtitle => InTalk ? BossLines[talkLine].who + ": " + BossLines[talkLine].text : "";
    public bool OrderStarted { get; private set; }
    public bool Delivered { get; private set; }
    public Transform Boss => boss;
    public DeliveryPoint FirstDrop => firstDrop;

    // 대사: docs/design/tutorial/first-delivery.yaml 의 garage 장면 1·2줄 그대로. 3줄(첫 주문 안내)은 설계에 사장 대사가 없어
    // 보관소 직원 대사("첫 주문. 달걀 한 판, 언덕 위 찻집.")를 사장 입으로 옮기고 줄리아 이름을 붙였다 — 도현 확인 필요.
    static readonly (string who, string text)[] BossLines =
    {
        ("사장", "왔냐. 오늘부터 배달이다. 대회는 그 다음."),
        ("사장", "뒤에 짐상자 달아 놨어. 깨지는 건 네 책임."),
        ("사장", "첫 주문. 달걀 한 판, 언덕 위 찻집 줄리아네."),
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

        if (cartSpot != null) ParkCar(cartSpot.position, cartSpot.eulerAngles.y);
        SetCarControl(false);
        if (spawn != null) player.Teleport(spawn.position, spawn.eulerAngles.y);
        EnterFoot();
        Mark($"Start doors={doors.Count} spawn={(spawn != null ? spawn.position.ToString("F1") : "-")}");
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
        bool press = InteractPressed();
        if (State == Mode.OnFoot)
        {
            if (InTalk) { UpdateTalk(press); return; }
            // 1인칭(D-219): 화면 가운데 점이 가리키는 것만 — 시선 레이 2 m 에 처음 맞은 것이 문·카트일 때.
            Object hit = Gaze(out DoorHinge door);
            GazeTarget = hit != null ? hit.name : null;
            if (boss != null && hit == boss)
            {
                SetPrompt("E 말하기");
                if (press) BeginTalk();
            }
            else if (hit == car)
            {
                // 기능 2: 사장 말을 듣기 전엔 못 탄다.
                if (boss != null && !Talked) SetPrompt("사장님한테 먼저");
                else
                {
                    SetPrompt("E 타기");
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
                if (press) Sit(spot);
            }
            else SetPrompt("");
        }
        else if (State == Mode.Seated)
        {
            if (press) StandUp();   // 앉아 있는 동안 화면에 아무것도 없음(D-227)
        }
        else if (State == Mode.InCart)
        {
            bool slow = Mathf.Abs(car.CurrentSpeedMps) < exitMaxSpeed;
            SetPrompt(slow ? "E 내리기" : "");
            if (press && slow) ExitCart();
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
        int n = Physics.RaycastNonAlloc(eye.position, eye.forward, gazeHits, Mathf.Max(gazeReach, boss != null ? talkReach : 0f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Collider first = null; float best = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var c = gazeHits[i].collider;
            if (c == null || c.transform.IsChildOf(player.transform)) continue;
            if (gazeHits[i].distance < best) { best = gazeHits[i].distance; first = c; }
        }
        // 사장(사람)은 talkReach 까지, 나머지(문·카트)는 예전처럼 gazeReach 까지.
        if (first != null && boss != null && first.transform.IsChildOf(boss) && best <= talkReach) return boss;
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

    // ── 걷기 ↔ 카트 ──
    void EnterFoot()
    {
        State = Mode.OnFoot;
        if (follow != null) follow.enabled = false;
        player.gameObject.SetActive(true);
        footCam.Yaw = player.transform.eulerAngles.y;
        footCam.enabled = true;
        if (dotRoot != null) dotRoot.SetActive(true);
        PlacePrompt(true);
        SetHud(false);
    }

    void EnterCart()
    {
        Mark("카트 타기");
        State = Mode.InCart;
        player.gameObject.SetActive(false);
        footCam.enabled = false;
        SetCarControl(true);
        if (!OrderStarted && firstDrop != null && rc != null) { BeginFirstOrder(); freeDriveEntered = true; }
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
    }

    // ── 기능 2: 사장 대화 → 첫 주문 ──
    void BeginTalk()
    {
        talkLine = 0; talkLineAt = Time.unscaledTime;
        player.enabled = false;                                    // 듣는 동안 안 걷는다(시선은 그대로)
        Vector3 to = player.transform.position - boss.position; to.y = 0f;
        if (to.sqrMagnitude > 1e-4f) boss.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        SetPrompt("");
        ShowSubtitle();
        Mark("사장 대화 시작");
    }

    void UpdateTalk(bool press)
    {
        if (!press || Time.unscaledTime - talkLineAt < 0.3f) return;   // 한 줄은 최소 0.3초(연타로 건너뛰기 방지)
        talkLine++; talkLineAt = Time.unscaledTime;
        if (talkLine < BossLines.Length) { ShowSubtitle(); return; }
        talkLine = -1; Talked = true;
        player.enabled = true;
        if (subRoot != null) subRoot.SetActive(false);
        Mark("사장 대화 끝");
    }

    void ShowSubtitle()
    {
        Mark("자막 " + Subtitle);
        if (subText == null) return;
        subText.text = "<b>" + BossLines[talkLine].who + "</b>  " + BossLines[talkLine].text + "  <size=70%><color=#FFFFFFAA>[E]</color></size>";
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
        foreach (var dp in FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None)) if (dp != firstDrop) dp.gameObject.SetActive(false);
        firstDrop.gameObject.SetActive(true);
        var route = new List<DeliveryPoint> { firstDrop };
        mgr.SetPoints(route, null);                                // 귀환 없음 — 찻집 도착이 곧 끝(범위: 출발 → 도착 감지)
        firstJob = ScriptableObject.CreateInstance<OrderJob>();
        firstJob.name = "TUT_FirstEggs"; firstJob.displayName = "달걀 한 판 → 언덕 위 찻집"; firstJob.cargoType = CargoType.Eggs;
        firstJob.destinationLabels = new List<string> { firstDrop.Label }; firstJob.timeLimitSec = 0f; firstJob.baseReward = 1200; firstJob.timeBonus = 0;
        firstJob.callerName = "언덕 위 찻집";
        mgr.OnDeliveryCompleted.AddListener(OnFirstDelivered);
        var progress = FindFirstObjectByType<DeliveryProgressHUD>(FindObjectsInactive.Include);
        if (progress != null) progress.gameObject.SetActive(true);
        rc.BeginRun(firstJob);
        OrderStarted = true;
        var t = mgr.CurrentTarget;
        Mark($"첫 주문 출발 화물={firstJob.cargoType} 목표={(t != null ? t.Label + " " + t.transform.position.ToString("F1") : "없음")}");
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
