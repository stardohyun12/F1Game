using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>단계: 보드 → 경로 → 포장 → 운전 → 정산.</summary>
public enum RunStage { Board, Routing, Packing, Driving, Result }

/// <summary>
/// 한 런의 진행 추적 + 정산.
/// OrderBoard → (RoutePlan →) PackingScreen → BeginRun(orders) → 주행 →
/// 마지막 배달 또는 시간초과 → EndRun → ResultPanel.
///
/// 채널 8 정정 (2026-05-15): 한 런 = 여러 주문(orders). CurrentJob은 primary(=orders[0])
/// 으로 유지 — UI/ResultPanel/CargoBox 등 단일 주문 인터페이스 호환.
/// 보상/시간/완료수 계산은 CurrentOrders 기준.
///
/// Phase 1 리팩터: FindFirstObjectByType 6개 제거. 모든 의존을 Services 경유.
/// 학습: GDD/architecture/phase1-dip-service-locator.md
/// </summary>
public class RunController : MonoBehaviour, IRunController
{
    IDeliveryManager Delivery => Services.Delivery;
    IOverviewCamera Overview => Services.Overview;
    IOrderBoard Board => Services.Board;
    IResultPanel ResultUI => Services.Result;
    IFuelSystem Fuel => Services.Fuel;
    ICargoBox Cargo => Services.Cargo;
    ICargoShakeSimulator Shake => Services.Shake;
    IUpgradeSystem Upgrade => Services.Upgrade;

    // =========================================================
    //  Stage
    // =========================================================
    RunStage stage = RunStage.Board;
    public RunStage CurrentStage => stage;
    public event System.Action<RunStage> OnStageChanged;

    void SetStage(RunStage s)
    {
        if (stage == s) return;
        stage = s;
        OnStageChanged?.Invoke(s);
    }

    /// <summary>OrderBoard → 경로 화면 진입 시 호출.</summary>
    public void EnterRouting() => SetStage(RunStage.Routing);
    /// <summary>경로 확정 → 포장 화면 진입 시 호출.</summary>
    public void EnterPacking() => SetStage(RunStage.Packing);

    // D-091: 타이어 마모 + 펑크 추적
    float _distanceTraveledMeters;
    float _punctureRollTimer;

    [Header("State")]
    /// <summary>이번 런의 주문들 (1~capacity). 채널 8 정정.</summary>
    public List<OrderJob> CurrentOrders { get; private set; } = new List<OrderJob>();
    /// <summary>primary(=CurrentOrders[0]). 단일 주문 인터페이스 호환용. 없으면 null.</summary>
    public OrderJob CurrentJob => (CurrentOrders != null && CurrentOrders.Count > 0) ? CurrentOrders[0] : null;
    public float Elapsed { get; private set; }
    public bool RunActive { get; private set; }
    public int CompletedCount { get; private set; }
    public bool LastRunSuccess { get; private set; }
    public int LastReward { get; private set; }
    /// <summary>이번 판 포장비(₩). 정산 때 보상에서 뺀다.</summary>
    public int LastPackCost { get; private set; }
    /// <summary>이번 판 순이익 = LastReward − LastPackCost (음수 가능, 지갑은 0 에서 멈춤).</summary>
    public int LastNet { get; private set; }
    int pendingPackCost;
    public float LastElapsed { get; private set; }
    /// <summary>EndRun 시점에 캡처. ResultPanel "이야기"용 (Day 12).</summary>
    public float LastIntegrityPercent { get; private set; } = 1f;

    [Header("Prototype 목표 시간 (D-117)")]
    [Tooltip("목표 시간 = 경로 직선거리 × 우회 계수 ÷ 기준 속도. 기준 속도가 낮을수록 여유.")]
    [SerializeField] private float parSpeedKmh = 55f;
    [SerializeField] private float parDetourFactor = 1.35f;
    [Tooltip("목표 시간의 몇 배가 지나면 런 종료(시간 초과).")]
    [SerializeField] private float parHardLimitMul = 2f;
    [Tooltip("D-119 제한 시간(초). 주문판이 열리는 순간부터 고르기·포장·주행 전부 포함. 넘으면 런 종료.")]
    [SerializeField] private float runTimeLimitSec = 90f;
    [Tooltip("D-119 긴급 전화 이벤트로 받은 주문의 추가 보상 비율(기본 보상 대비).")]
    [SerializeField] private float urgentRewardBonus = 0.5f;

    /// <summary>D-119 — 주문판부터 도는 제한 시간 모드(프로토타입).</summary>
    public bool ClockRunning { get; private set; }
    /// <summary>호환용 별칭 (HUD).</summary>
    public bool UrgentClockRunning => ClockRunning;
    /// <summary>이번 런에서 긴급 전화로 받은 주문.</summary>
    readonly HashSet<OrderJob> urgentOrders = new HashSet<OrderJob>();
    public float UrgentRewardBonus => urgentRewardBonus;
    public void MarkUrgent(OrderJob job) { if (job != null) urgentOrders.Add(job); }
    public bool IsUrgent(OrderJob job) => job != null && urgentOrders.Contains(job);

    /// <summary>D-117 — 이번 런의 목표 시간(초). 0 = 계산 안 함(프로토타입 모드 아님).</summary>
    public float ParTime { get; private set; }
    /// <summary>EndRun 시점 목표 시간 안에 들어왔는지.</summary>
    public bool LastBeatPar { get; private set; }

    bool UsePar => Services.PrototypeMode && ParTime > 0f;

    /// <summary>유효 시간 제한(초). 프로토타입: 목표 시간 × parHardLimitMul. 그 외: 픽한 주문들 중 가장 큰 값 (0 무제한이 하나라도 있으면 0).</summary>
    public float EffectiveTimeLimit
    {
        get
        {
            if (UsePar) return ClockRunning ? ParTime : ParTime * parHardLimitMul;
            if (CurrentOrders == null || CurrentOrders.Count == 0) return 0f;
            float maxT = 0f;
            foreach (var o in CurrentOrders)
            {
                if (o == null) continue;
                if (o.timeLimitSec <= 0f) return 0f; // 하나라도 무제한 → 전체 무제한
                if (o.timeLimitSec > maxT) maxT = o.timeLimitSec;
            }
            return maxT;
        }
    }

    /// <summary>HUD 표시용. 프로토타입: 목표 시간까지 남은 초(넘으면 0). -1 = 무제한.</summary>
    public float TimeRemaining => UsePar
        ? Mathf.Max(0f, ParTime - Elapsed)
        : EffectiveTimeLimit > 0f
            ? Mathf.Max(0f, EffectiveTimeLimit - Elapsed)
            : -1f; // -1 = 무제한

    void Start()
    {
        OpenBoard();
    }

    void OpenBoard()
    {
        SetStage(RunStage.Board);
        if (Board == null)
        {
            Time.timeScale = 1f;
            BeginRun(MakeFallbackJob());
            return;
        }
        Time.timeScale = 0f;
        if (Overview != null && !Overview.IsInOverview) Overview.GoToOverview();
        if (Services.PrototypeMode) StartRunClock();
        Board.Open();
    }

    static OrderJob MakeFallbackJob()
    {
        var j = ScriptableObject.CreateInstance<OrderJob>();
        j.name = "FallbackJob";
        j.timeLimitSec = 0f;
        j.baseReward = 0;
        j.timeBonus = 0;
        return j;
    }

    // =========================================================
    //  BeginRun — 진입점 (단일/다중/포장 핸드오프 오버로드)
    // =========================================================

    public void BeginRun(OrderJob job)
    {
        BeginRun(new List<OrderJob> { job }, null);
    }

    public void BeginRun(OrderJob job, List<GameObject> packedItems)
    {
        BeginRun(new List<OrderJob> { job }, packedItems);
    }

    /// <summary>채널 8 정정 — 여러 주문 + 포장된 큐브로 런 시작.</summary>
    public void BeginRun(List<OrderJob> orders, List<GameObject> packedItems)
    {
        if (orders == null || orders.Count == 0)
        {
            Debug.LogWarning("[RunController] BeginRun: orders 비어있음 — 폴백 잡으로 진입");
            orders = new List<OrderJob> { MakeFallbackJob() };
        }
        SetStage(RunStage.Driving);
        TransferWrapStateToCargo(packedItems);
        InitRunState(orders);
        // D-091: 거리/펑크 추적 초기화
        _distanceTraveledMeters = 0f;
        _punctureRollTimer = 0f;
        if (Services.TireWear != null)
        {
            Services.TireWear.OnPunctured -= HandlePuncture;
            Services.TireWear.OnPunctured += HandlePuncture;
        }
        ActivateDeliveryPoints();
        if (!ClockRunning) ParTime = Services.PrototypeMode ? ComputeParTime() : 0f;
        if (Fuel != null) Fuel.Refill();
        ApplyCargoSetup();        // primary 주문 cargoType
        UpdateShakeSimColor();    // primary 주문 색
        if (Shake != null) Shake.StartGracePeriod(1.0f);
        if (Cargo != null) Cargo.StartGracePeriod(1.0f); // 세션 27 — CargoBox FixedUpdate/OnCollisionEnter도 막음
        StartFollowCameraAndCloseBoard();
        SubscribeDeliveryEvents();
    }

    /// <summary>포장한 cube들을 트렁크 시뮬에 마운트.</summary>
    void TransferWrapStateToCargo(System.Collections.Generic.List<GameObject> packedItems)
    {
        if (Shake == null)
        {
            Debug.LogWarning("[RunController] Shake 미등록 — 포장 큐브 폐기");
            if (packedItems != null) foreach (var go in packedItems) if (go != null) UnityEngine.Object.Destroy(go);
            return;
        }
        Shake.ClearMountedItems();
        if (packedItems == null || packedItems.Count == 0) return;

        int totalCost = 0;
        foreach (var cube in packedItems)
        {
            if (cube == null) continue;
            var srcWrap = cube.GetComponent<PitCrew.Packing.WrapState>();
            if (srcWrap != null) totalCost += Mathf.RoundToInt(srcWrap.TotalCost);
            var painter = cube.GetComponent<PitCrew.Packing.WrapPainter>();
            if (painter != null) painter.enabled = false;
            // 콜라이더는 지우지 않고 끈다 — WrapState.QueryAbsorption 이 충격 면 UV 를 얻으려면 MeshCollider 가 필요하다.
            // (예전엔 지워서 주행 중 흡수가 늘 0 = 포장해도 손상이 안 줄었다.)
            foreach (var col in cube.GetComponentsInChildren<Collider>(true))
                col.enabled = false;
            cube.name = "WrappedItem";
            Shake.MountWrappedItem(cube);
        }
        // 포장비는 장착 때 빼지 않는다 — 정산(EndRun)에서 보상에서 차감(시작 지갑이 모자라도 포장 가능).
        pendingPackCost = totalCost;
        Debug.Log($"[RunController] 포장 큐브 {packedItems.Count}개 → 트렁크 마운트 (총 cost={totalCost})");
    }

    void InitRunState(List<OrderJob> orders)
    {
        CurrentOrders = new List<OrderJob>(orders);
        if (!ClockRunning) Elapsed = 0f; // D-119: 주문판부터 잰 시간은 이어 간다
        CompletedCount = 0;
        RunActive = true;
        OnRunStarted?.Invoke(this);
    }

    /// <summary>BeginRun 직후 발화. RunMetricsRecorder가 거리 누적 시작.</summary>
    public event System.Action<RunController> OnRunStarted;

    void ActivateDeliveryPoints()
    {
        if (Delivery == null) return;
        var mgr = Delivery as DeliveryManager;
        // D-117: 경로 계획 화면이 꺼져 있으므로 주문 행선지 라벨 → 가까운 순서로 직접 경로를 만든다.
        // (DeliveryManager.Start 가 전체 지점을 미리 활성화해 HasPlannedRoute 가 늘 true 이므로 아래 분기보다 먼저.)
        if (Services.PrototypeMode && mgr != null)
        {
            var ordered = BuildRouteFromLabels(mgr, CurrentOrders);
            if (ordered.Count > 0) mgr.ActivateRoute(ordered);
            else mgr.ActivateSubset(CurrentOrders.Count);
            return;
        }
        // 채널 8: RoutePlanScreen이 이미 ActivateRoute로 경로 세팅했으면 그대로.
        if (mgr != null && mgr.HasPlannedRoute) return;
        // 폴백: 주문 수만큼.
        Delivery.ActivateSubset(CurrentOrders.Count);
    }

    /// <summary>
    /// D-119 — 제한 시간 시작. 주문판이 열리는 순간부터 고르기·포장·주행이 모두 한 시계를 쓴다.
    /// </summary>
    public void StartRunClock()
    {
        ParTime = runTimeLimitSec;
        Elapsed = 0f;
        CurrentOrders = new List<OrderJob>();
        urgentOrders.Clear();
        pendingPackCost = 0;
        ClockRunning = ParTime > 0f;
        Debug.Log($"[RunController] 제한 시간 시작(D-119): {ParTime:F0}s");
    }

    /// <summary>주문 행선지 라벨 → 차에서 가까운 순으로 이은 경로.</summary>
    List<DeliveryPoint> BuildRouteFromLabels(DeliveryManager mgr, List<OrderJob> orders)
    {
        var stops = new List<DeliveryPoint>();
        if (orders != null)
            foreach (var o in orders)
            {
                if (o == null || o.destinationLabels == null) continue;
                foreach (var label in o.destinationLabels)
                {
                    var p = mgr.FindByLabel(label);
                    if (p != null && !stops.Contains(p)) stops.Add(p);
                }
            }
        var ordered = new List<DeliveryPoint>(stops.Count);
        if (stops.Count == 0) return ordered;
        Vector3 cur = Services.Car != null ? Services.Car.Transform.position : stops[0].transform.position;
        while (stops.Count > 0)
        {
            int best = 0; float bestD = float.MaxValue;
            for (int i = 0; i < stops.Count; i++)
            {
                float d = (stops[i].transform.position - cur).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            cur = stops[best].transform.position;
            ordered.Add(stops[best]);
            stops.RemoveAt(best);
        }
        return ordered;
    }

    /// <summary>D-117 — 이번 런 경로로 목표 시간 계산.</summary>
    float ComputeParTime()
    {
        var mgr = Delivery as DeliveryManager;
        if (mgr == null || mgr.Route == null || mgr.Route.Count == 0) return 0f;
        var list = new List<DeliveryPoint>(mgr.Route);
        return ComputeDrivePar(list);
    }

    /// <summary>차 위치 → 배달지들 → (복귀 지점) 직선거리 합 × 우회 계수 ÷ 기준 속도.</summary>
    float ComputeDrivePar(List<DeliveryPoint> route)
    {
        var mgr = Delivery as DeliveryManager;
        if (route == null || route.Count == 0) return 0f;
        Vector3 prev = Services.Car != null ? Services.Car.Transform.position : route[0].transform.position;
        float dist = 0f;
        foreach (var p in route)
        {
            if (p == null) continue;
            dist += Vector3.Distance(prev, p.transform.position);
            prev = p.transform.position;
        }
        if (mgr != null && mgr.ReturnTarget != null) dist += Vector3.Distance(prev, mgr.ReturnTarget.position);
        float mps = Mathf.Max(1f, parSpeedKmh / 3.6f);
        float par = Mathf.Max(10f, dist * parDetourFactor / mps);
        Debug.Log($"[RunController] 목표 시간(D-117): 경로 {dist:F0}m × {parDetourFactor} ÷ {parSpeedKmh}km/h = {par:F1}s (초과 종료 {par * parHardLimitMul:F0}s)");
        return par;
    }

    /// <summary>화물 타입 적용 — primary 주문 기준 (Task 14: SetPacking 제거).</summary>
    void ApplyCargoSetup()
    {
        if (Cargo == null || CurrentJob == null) return;
        Cargo.SetCargoType(CurrentJob.cargoType);
    }

    /// <summary>D-043: CCTV 박스 색상 — primary 주문 기준.</summary>
    void UpdateShakeSimColor()
    {
        if (Shake != null && CurrentJob != null)
            Shake.SetBoxColor(CargoTypePresets.BoxColor(CurrentJob.cargoType));
    }

    void StartFollowCameraAndCloseBoard()
    {
        // 세션 27 fix — Overview 없거나 콜백 안 터지면 게임이 timeScale=0으로 멈춤.
        // 무조건 즉시 timeScale=1, Overview는 옵션으로.
        Time.timeScale = 1f;
        if (Overview != null) Overview.GoToFollow(null);
        if (Board != null) Board.Close();
    }

    void SubscribeDeliveryEvents()
    {
        if (Delivery == null) return;
        Delivery.OnDeliveryCompleted.RemoveListener(OnDeliveryCompleted);
        Delivery.OnAllCompleted.RemoveListener(OnAllCompleted);
        Delivery.OnDeliveryCompleted.AddListener(OnDeliveryCompleted);
        Delivery.OnAllCompleted.AddListener(OnAllCompleted);
    }

    void OnDeliveryCompleted(DeliveryPoint p)
    {
        if (!RunActive) return;
        CompletedCount++;
    }

    void OnAllCompleted()
    {
        if (!RunActive) return;
        EndRun(success: true);
    }

    void Update()
    {
        // D-118: 전화 수락 ~ 출발 전(포장, timeScale 0) 에도 시계가 돈다.
        // D-119: 주문판·포장(timeScale 0) 에서도 시계가 돈다. 0 이 돼도 여기선 끝내지 않고(UI 꼬임 방지) 출발 즉시 시간 초과.
        if (ClockRunning && !RunActive && (stage == RunStage.Board || stage == RunStage.Routing || stage == RunStage.Packing))
        {
            Elapsed += Time.unscaledDeltaTime;
            return;
        }
        if (!RunActive) return;
        Elapsed += Time.deltaTime;
        float lim = EffectiveTimeLimit;
        if (lim > 0f && Elapsed >= lim) EndRun(success: false);

        // D-091: 거리 누적 + 매 초 펑크 롤
        _distanceTraveledMeters += Time.deltaTime * (Services.Car?.CurrentSpeedMps ?? 0f);
        _punctureRollTimer += Time.deltaTime;
        if (_punctureRollTimer >= 1f)
        {
            _punctureRollTimer = 0f;
            if (Services.TireWear != null && Services.Tires != null)
                Services.TireWear.RollPuncture(Services.Tires.Current);
        }
    }

    void HandlePuncture()
    {
        if (!RunActive) return;
        Debug.LogWarning("[RunController] PUNCTURE → 슬로모 → EndRun(success=false)");
        StartCoroutine(PunctureSlowMoThenEnd());
    }

    System.Collections.IEnumerator PunctureSlowMoThenEnd()
    {
        // Day 4: 펑크 = 슬로모 0.5초 (실시간 0.5초) → 정산 직행
        float originalScale = Time.timeScale;
        Time.timeScale = 0.25f;
        Time.fixedDeltaTime = 0.02f * 0.25f;
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = originalScale;
        Time.fixedDeltaTime = 0.02f;
        EndRun(success: false);
    }

    void EndRun(bool success)
    {
        // D-091: 마모 기록 + 이벤트 구독 해제
        if (Services.TireWear != null)
        {
            if (Services.Tires != null)
            {
                float peakG = (Services.Cargo as CargoBox)?.PeakLongG ?? 0f;
                Services.TireWear.RecordJobUsage(Services.Tires.Current, _distanceTraveledMeters, peakG);
            }
            Services.TireWear.OnPunctured -= HandlePuncture;
        }

        SetStage(RunStage.Result);
        RunActive = false;
        LastRunSuccess = success;
        LastElapsed = Elapsed;
        LastIntegrityPercent = Cargo != null ? Cargo.IntegrityPercent : 1f;

        float integrityMul = ComputeIntegrityMultiplier(LastIntegrityPercent);

        // 채널 8 정정: 보상은 픽한 주문들 합. time bonus는 시간 내 + (effectiveLimit > 0)일 때만.
        int totalBase = 0, totalTimeBonus = 0;
        foreach (var o in CurrentOrders)
        {
            if (o == null) continue;
            totalBase += o.baseReward;
            totalTimeBonus += o.timeBonus;
        }
        float effectiveLim = EffectiveTimeLimit;

        LastBeatPar = UsePar && success && Elapsed <= ParTime;

        int reward;
        if (success && UsePar)
        {
            // D-117: 목표 시간 안 = 시간 보너스 전액, 넘으면 0 (런은 끝까지).
            reward = totalBase + (LastBeatPar ? totalTimeBonus : 0);
        }
        else if (success)
        {
            reward = totalBase + (effectiveLim > 0f ? totalTimeBonus : 0);
        }
        else
        {
            // 시간 초과: 완료한 비율의 절반.
            int n = CurrentOrders.Count;
            float ratio = n > 0 ? (float)CompletedCount / n : 0f;
            reward = Mathf.RoundToInt(totalBase * ratio * 0.5f);
        }
        // D-119: 긴급 전화로 받은 주문은 배달 성공 시 추가 보상.
        if (success && urgentOrders.Count > 0)
            foreach (var o in CurrentOrders)
                if (o != null && urgentOrders.Contains(o)) reward += Mathf.RoundToInt(o.baseReward * urgentRewardBonus);
        reward = Mathf.RoundToInt(reward * integrityMul);
        LastReward = reward;
        ClockRunning = false;

        // 포장비 정산: 순이익 = 보상 − 포장비. 손해면 지갑에서 빼되 0 밑으로는 안 내린다(프로토 — 빚 없음).
        LastPackCost = pendingPackCost;
        pendingPackCost = 0;
        LastNet = reward - LastPackCost;
        if (Upgrade != null)
        {
            if (LastNet > 0) Upgrade.AddReward(LastNet);
            else if (LastNet < 0) Upgrade.Spend(Mathf.Min(-LastNet, Mathf.Max(0, Upgrade.Wallet)));
        }

        // 2026-05-16 수치 audit — RunMetricsRecorder가 구독해서 CSV 기록.
        OnRunEnded?.Invoke(this);

        Time.timeScale = 0f;
        if (Overview != null) Overview.GoToOverview();
        if (ResultUI != null) ResultUI.Show(this);
    }

    /// <summary>EndRun 직후 발화. RunMetricsRecorder가 구독해 CSV 기록.</summary>
    public event System.Action<RunController> OnRunEnded;

    public static float ComputeIntegrityMultiplier(float integrityPercent)
    {
        float i = integrityPercent * 100f;
        if (i >= 80f) return 1.0f;
        if (i >= 50f) return 0.8f;
        if (i >= 20f) return 0.5f;
        if (i > 0f)   return 0.2f;
        return 0f;
    }

    public static string IntegrityGrade(float integrityPercent)
    {
        float i = integrityPercent * 100f;
        if (i >= 80f) return "완벽";
        if (i >= 50f) return "약간 손상";
        if (i >= 20f) return "크게 손상";
        if (i > 0f)   return "거의 전파";
        return "전파";
    }

    public void RestartFromBoard()
    {
        if (ResultUI != null) ResultUI.Hide();
        OpenBoard();
    }
}
