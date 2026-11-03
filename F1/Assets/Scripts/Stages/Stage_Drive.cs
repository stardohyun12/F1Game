using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-287 3단계: 운전 단계 — 카트 타기·내리기·도착·첫 주문 시작/끝. 감독이 만들고 부른다(MonoBehaviour 아님).
/// 듣는 신호: CartBoarded(물건 Cart → 타기 = Enter) · DayStarted(주문 상태·꺼 둔 배달지 되돌림).
/// 쏘는 신호: BoxLoaded(상자 실음) · OrderBegan(첫 주문 출발) · CartLeft(내림).
/// 지도: docs/world/CODEMAP.md
/// </summary>
public class Stage_Drive : DayStage
{
    public Stage_Drive(OnFootDirector d) : base(d) { }
    public override OnFootDirector.Mode Mode => OnFootDirector.Mode.InCart;

    public bool OrderStarted { get; private set; }
    public bool Delivered { get; private set; }
    /// <summary>지금 달리는 첫 주문(출발 전·다음 날 = null).</summary>
    internal OrderJob FirstJob { get; private set; }
    /// <summary>자유 주행에 한 번 들어갔다(정비소 → 보관소). 주문 출발도 여기에 친다.</summary>
    internal bool freeDriveEntered;
    /// <summary>디버그 창의 카트 타기만 true — 첫 주문을 시작하지 않는다(정식 빌드에선 늘 false).</summary>
    internal bool debugFreeCart;
    readonly List<DeliveryPoint> pointsOff = new List<DeliveryPoint>();   // BeginFirstOrder 가 꺼 둔 배달지 — 다음 날 되살림

    static DeliveryManager Mgr => Services.Delivery as DeliveryManager ?? Object.FindFirstObjectByType<DeliveryManager>();

    public override void Plug()
    {
        base.Plug();
        Listen(Signal.CartBoarded, _ => Enter());
    }

    protected override void NewDay()
    {
        var mgr = Mgr;
        if (mgr != null) mgr.OnDeliveryCompleted.RemoveListener(OnFirstDelivered);
        FirstJob = null;
        OrderStarted = false; Delivered = false; freeDriveEntered = false;
        // 배달지: 꺼 둔 것 되살리고 「완료」 풀기
        foreach (var dp in pointsOff) if (dp != null) dp.gameObject.SetActive(true);
        pointsOff.Clear();
        foreach (var dp in Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) dp.ResetCompleted();
    }

    /// <summary>카트 타기. 보관소 흐름이면 포장까지 끝낸 뒤 탈 때 첫 주문 출발(정비소 → 보관소 운전은 자유 주행).</summary>
    public override void Enter()
    {
        d.Mark("카트 타기");
        if (d.opening != null) d.opening.OnCartEnter();
        d.StopDestFocus();
        d.State = OnFootDirector.Mode.InCart;
        EngineAudio.OnFootMute = false;
        d.player.gameObject.SetActive(false);
        d.footCam.enabled = false;
        d.SetCarControl(true);
        bool ready = !d.DepotFlow || d.Packed;
        if (!OrderStarted && ready && d.firstDrop != null && d.rc != null && !debugFreeCart)
        {
            if (d.DepotFlow && d.packSpot != null) { d.packSpot.Load(); SignalBus.Raise(Signal.BoxLoaded); d.Mark("상자 카트에 실음"); }
            BeginFirstOrder(); freeDriveEntered = true;
            SignalBus.Raise(Signal.OrderBegan);   // → 포장 결과를 화물 시작값으로
        }
        else if (d.rc != null && !freeDriveEntered) { if (d.DepotFlow) ClearPreOrderRoute(); d.rc.EnterFreeDrive(); freeDriveEntered = true; }
        else Services.Overview?.GoToFollow();
        // 같은 프레임에 B 자리로 — 눈높이에서 차 뒤로 날아가는 프레임이 없다.
        if (d.follow != null) { d.follow.enabled = true; d.follow.SnapToTarget(); }
        if (d.dotRoot != null) d.dotRoot.SetActive(false);
        d.GazeTarget = null;
        d.PlacePrompt(false);
        d.SetHud(true);
        d.SetPrompt("");
    }

    public override void Tick(bool press)
    {
        var car = d.car;
        bool slow = Mathf.Abs(car.CurrentSpeedMps) < d.exitMaxSpeed;
        d.PromptAnchor = car.transform.position + Vector3.up * 1.0f;
        d.SetPrompt(slow ? "E 내리기" : "");
        if (press && slow) { ArriveIfNear(); Exit(); }
    }

    /// <summary>카트 내리기 — 차 옆 빈자리에 선다. 도착했으면 Stage_Door 가 CartLeft 를 듣고 상자를 든다.</summary>
    public override void Exit()
    {
        var car = d.car;
        d.Mark($"카트 내리기 차=({car.transform.position.x:F1},{car.transform.position.y:F1},{car.transform.position.z:F1})");
        d.SetCarControl(false);
        if (d.carRb != null) { d.carRb.linearVelocity = Vector3.zero; d.carRb.angularVelocity = Vector3.zero; }
        d.player.gameObject.SetActive(true);
        d.player.Teleport(ExitPoint(), car.transform.eulerAngles.y);
        d.EnterFoot();
        SignalBus.Raise(Signal.CartLeft);   // C-003: 다음 = 문 두드리기
    }

    /// <summary>
    /// C-002: 정비소 → 보관소 자유 주행(주문 전)엔 배달 경로를 비운다. DeliveryManager.Start 가 씬 배달지 전부를 미리 켜 두어
    /// 카트 화살·거리·깃발(DotScreenUI 는 CurrentTarget 먼저)이 보관소 대신 찻집(270 m)을 가리켰다. 비우면 WalkDest(보관소)로 떨어진다.
    /// 주문 출발(BeginFirstOrder)이 찻집 경로를 다시 세운다.
    /// </summary>
    void ClearPreOrderRoute()
    {
        var mgr = Mgr;
        if (mgr == null || mgr.CurrentTarget == null) return;
        mgr.ActivateRoute(new List<DeliveryPoint>());
        d.Mark("주문 전 자유 주행 — 배달 경로 비움(표식 = 보관소)");
    }

    // ── D-250 ③④ 문 앞 건네기 ──
    /// <summary>배달지 표식 arriveM 안에서 세우고 내리면 도착(트리거를 못 밟았어도). 도착 처리 = 기존 DeliveryManager 경로 그대로.</summary>
    void ArriveIfNear()
    {
        var drop = d.firstDrop;
        if (!OrderStarted || Delivered || drop == null || d.handover == null) return;
        Vector3 v = d.car.transform.position - drop.transform.position; v.y = 0f;
        if (v.magnitude > d.arriveM) return;
        var mgr = Mgr;
        d.Mark($"내린 자리가 배달지 {v.magnitude:F1}m 안(≤{d.arriveM:F0}) → 도착 처리");
        if (mgr != null) mgr.OnDeliveryReached(drop);
    }

    /// <summary>
    /// 첫 탑승 = 첫 주문 출발. OpeningTutorial 과 같은 달걀 주문을 만들어 기존 RunController.BeginRun 으로 시작한다
    /// (DeliveryManager 경로·화물 종류·HUD 화살표 모두 기존 그대로). 다른 배달지는 꺼서 화살표가 찻집만 가리키게.
    /// </summary>
    internal void BeginFirstOrder()
    {
        var mgr = Mgr;
        var drop = d.firstDrop;
        if (mgr == null) { d.Mark("첫 주문 실패: DeliveryManager 없음"); d.rc.EnterFreeDrive(); return; }
        foreach (var dp in Object.FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None)) if (dp != drop) { if (!pointsOff.Contains(dp)) pointsOff.Add(dp); dp.gameObject.SetActive(false); }
        drop.gameObject.SetActive(true);
        var route = new List<DeliveryPoint> { drop };
        mgr.SetPoints(route, null);                                // 귀환 없음 — 찻집 도착이 곧 끝(범위: 출발 → 도착 감지)
        FirstJob = d.depot.PickedJob != null ? d.depot.PickedJob : d.depot.MakeFirstJob();   // D-250: 칠판에서 맡은 주문 그대로
        mgr.OnDeliveryCompleted.AddListener(OnFirstDelivered);
        var progress = Object.FindFirstObjectByType<DeliveryProgressHUD>(FindObjectsInactive.Include);
        if (progress != null) progress.gameObject.SetActive(true);
        d.rc.DoorHandover = d.handover != null;                    // D-250 ③: 도착 = 정산창 대신 문 앞 건네기
        d.rc.BeginRun(FirstJob);
        OrderStarted = true;
        var t = mgr.CurrentTarget;
        d.Mark($"첫 주문 출발 화물={FirstJob.cargoType} 목표={(t != null ? t.Label + " " + t.transform.position.ToString("F1") : "없음")}");
    }

    void OnFirstDelivered(DeliveryPoint dp)
    {
        if (dp != d.firstDrop || Delivered) return;
        Delivered = true;
        var cargo = Services.Cargo as CargoBox ?? Object.FindFirstObjectByType<CargoBox>();
        d.Mark($"찻집 도착 {dp.Label} 화물={(cargo != null ? (cargo.IntegrityPercent * 100f).ToString("F0") + "%" : "?")}");
    }

    /// <summary>카트 옆 내릴 자리(오른쪽·왼쪽·뒤·앞 순으로 빈 곳). 다 막혔으면 차 위.</summary>
    internal Vector3 ExitPoint()
    {
        Transform c = d.car.transform;
        float side = d.exitSide;
        foreach (var off in new[] { -c.right * side, c.right * side, -c.forward * (side + 1.2f), c.forward * (side + 1.2f) })
        {
            Vector3 p = c.position + off;
            if (!Physics.Raycast(p + Vector3.up * 2.5f, Vector3.down, out var hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            p = hit.point + Vector3.up * 0.05f;
            var cc = d.player.Controller;
            if (Physics.CheckCapsule(p + Vector3.up * (cc.radius + 0.05f), p + Vector3.up * (cc.height - cc.radius), cc.radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            return p;
        }
        return c.position + Vector3.up * 1.5f;   // 다 막혔으면 차 위(떨어져 내려옴)
    }
}
