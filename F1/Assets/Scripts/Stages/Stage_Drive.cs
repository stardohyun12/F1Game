using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    readonly List<DeliveryPoint> pointsOff = new List<DeliveryPoint>();   // BeginFirstOrder 가 꺼 둔 배달지 — 다음 날 되살림

    /// <summary>
    /// 디버그 순간이동용 보관소 앞 길 카트 자리(DebugPlaces.DepotCart). 두 번째 한 판 메모(D-284): 아침 카트는 여기 서 있지 않다 —
    /// 첫날 카트는 정비소(cartSpot)에서 받아 타고 보관소로 간다(SPEC §1 거점은 몸으로 배운다). 보관소에서 상자를 싣는 논리는 카트가 어디 서 있든 같다(NearCart).
    /// 좌표는 DebugPlaces.DepotCart 가 이 값을 가져다 쓴다(디버그 코드는 정식 빌드에 없어 값은 여기).
    /// </summary>
    public static readonly Vector3 DepotCartSpot = new Vector3(273.5f, 29.6f, -37.4f);
    public const float DepotCartYaw = 90f;
    /// <summary>상자를 든 채 카트에서 이만큼(m, 카트 몸 가장자리까지 수평) 안이면 시선이 카트를 안 가리켜도 [E 싣고 타기] 칩이 뜬다.</summary>
    public const float CartLoadReach = 2f;

    /// <summary>카트 칩 글(순수 함수 — WorldInvariants 「상자 든 채 카트 칩」이 부른다). "E " 로 시작하면 누를 수 있다.</summary>
    public static string CartChip(bool depotFlow, bool orderPicked, bool packed, bool orderStarted)
        => OrderRules.CartChip(depotFlow, orderPicked, packed, orderStarted);   // D-287: 몸통은 Core/OrderRules

    /// <summary>상자를 들었고 아직 싣기 전 — 카트 위 「!」(DotScreenUI)가 켜진다.</summary>
    public bool CartMark => State == Mode.OnFoot && DepotFlow && !OrderStarted && CarryingBox && car != null;
    public Vector3 CartMarkAnchor => car.transform.position + Vector3.up * 1.5f;
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
            if (firstDrop == null || Delivered) return null;
            // 첫 과제 01-first-loop(SPEC §11-7): 보관소 흐름은 사장 대화가 없어도 칠판 쪽지를 집으면 찻집을 가리킨다.
            if (DepotFlow && OrderPicked) return firstDrop;
            if (!Talked) return null;   // C-002(도현 D-277 B): 걷는 구간엔 방향 표시 없음
            return DepotFlow ? depotDest : firstDrop;
        }
    }
    OrderJob firstJob;
    bool debugFreeCart;   // 디버그 창의 카트 타기만 true — 첫 주문을 시작하지 않는다(정식 빌드에선 늘 false)

    void EnterCart()
    {
        Mark("카트 타기");
        if (opening != null) opening.OnCartEnter();
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
            if (DepotFlow && packSpot != null) { packSpot.Load(); CarryingBox = false; BedBox = true; Mark("상자 카트에 실음"); }
            BeginFirstOrder(); freeDriveEntered = true;
            ApplyPackResult();
        }
        else if (rc != null && !freeDriveEntered) { if (DepotFlow) ClearPreOrderRoute(); rc.EnterFreeDrive(); freeDriveEntered = true; }
        else Services.Overview?.GoToFollow();
        // 같은 프레임에 B 자리로 — 눈높이에서 차 뒤로 날아가는 프레임이 없다.
        if (follow != null) { follow.enabled = true; follow.SnapToTarget(); }
        if (dotRoot != null) dotRoot.SetActive(false);
        GazeTarget = null;
        PlacePrompt(false);
        SetHud(true);
        SetPrompt("");
    }

    /// <summary>
    /// C-002: 정비소 → 보관소 자유 주행(주문 전)엔 배달 경로를 비운다. DeliveryManager.Start 가 씬 배달지 전부를 미리 켜 두어
    /// 카트 화살·거리·깃발(DotScreenUI 는 CurrentTarget 먼저)이 보관소 대신 찻집(270 m)을 가리켰다. 비우면 WalkDest(보관소)로 떨어진다.
    /// 주문 출발(BeginFirstOrder)이 찻집 경로를 다시 세운다.
    /// </summary>
    void ClearPreOrderRoute()
    {
        var mgr = Services.Delivery as DeliveryManager ?? FindFirstObjectByType<DeliveryManager>();
        if (mgr == null || mgr.CurrentTarget == null) return;
        mgr.ActivateRoute(new List<DeliveryPoint>());
        Mark("주문 전 자유 주행 — 배달 경로 비움(표식 = 보관소)");
    }

    void ExitCart()
    {
        Mark($"카트 내리기 차=({car.transform.position.x:F1},{car.transform.position.y:F1},{car.transform.position.z:F1})");
        SetCarControl(false);
        if (carRb != null) { carRb.linearVelocity = Vector3.zero; carRb.angularVelocity = Vector3.zero; }
        player.gameObject.SetActive(true);
        player.Teleport(ExitPoint(), car.transform.eulerAngles.y);
        EnterFoot();
        if (Delivered && handover != null && !HandedOver && !CarryingBox) { BeginCarry(); FocusThing(handover.DoorCenter, "배달지 문"); }   // C-003: 다음 = 문 두드리기
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
        firstJob = depot.PickedJob != null ? depot.PickedJob : depot.MakeFirstJob();   // D-250: 칠판에서 맡은 주문 그대로
        mgr.OnDeliveryCompleted.AddListener(OnFirstDelivered);
        var progress = FindFirstObjectByType<DeliveryProgressHUD>(FindObjectsInactive.Include);
        if (progress != null) progress.gameObject.SetActive(true);
        rc.DoorHandover = handover != null;                        // D-250 ③: 도착 = 정산창 대신 문 앞 건네기
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
}
