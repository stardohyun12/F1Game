using System;
using System.Collections.Generic;
using UnityEngine;

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
    public float LastElapsed { get; private set; }
    /// <summary>EndRun 시점에 캡처. ResultPanel "이야기"용 (Day 12).</summary>
    public float LastIntegrityPercent { get; private set; } = 1f;

    /// <summary>유효 시간 제한(초). 픽한 주문들 중 가장 큰 값 (0 무제한이 하나라도 있으면 0).</summary>
    public float EffectiveTimeLimit
    {
        get
        {
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

    public float TimeRemaining => EffectiveTimeLimit > 0f
        ? Mathf.Max(0f, EffectiveTimeLimit - Elapsed)
        : -1f; // -1 = 무제한

    void Start()
    {
        OpenBoard();
    }

    void OpenBoard()
    {
        if (Board == null)
        {
            Time.timeScale = 1f;
            BeginRun(MakeFallbackJob());
            return;
        }
        Time.timeScale = 0f;
        if (Overview != null && !Overview.IsInOverview) Overview.GoToOverview();
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
        TransferWrapStateToCargo(packedItems);
        InitRunState(orders);
        ActivateDeliveryPoints();
        if (Fuel != null) Fuel.Refill();
        ApplyCargoSetup();        // primary 주문 cargoType
        UpdateShakeSimColor();    // primary 주문 색
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
            foreach (var col in cube.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(col);
            cube.name = "WrappedItem";
            Shake.MountWrappedItem(cube);
        }
        if (totalCost > 0 && UpgradeSystem.Instance != null) UpgradeSystem.Instance.Spend(totalCost);
        Debug.Log($"[RunController] 포장 큐브 {packedItems.Count}개 → 트렁크 마운트 (총 cost={totalCost})");
    }

    void InitRunState(List<OrderJob> orders)
    {
        CurrentOrders = new List<OrderJob>(orders);
        Elapsed = 0f;
        CompletedCount = 0;
        RunActive = true;
        OnRunStarted?.Invoke(this);
    }

    /// <summary>BeginRun 직후 발화. RunMetricsRecorder가 거리 누적 시작.</summary>
    public event System.Action<RunController> OnRunStarted;

    void ActivateDeliveryPoints()
    {
        if (Delivery == null) return;
        // 채널 8: RoutePlanScreen이 이미 ActivateRoute로 경로 세팅했으면 그대로.
        var mgr = Delivery as DeliveryManager;
        if (mgr != null && mgr.HasPlannedRoute) return;
        // 폴백: 주문 수만큼.
        Delivery.ActivateSubset(CurrentOrders.Count);
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
        if (Overview != null) Overview.GoToFollow(() => { Time.timeScale = 1f; });
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
        if (!RunActive) return;
        Elapsed += Time.deltaTime;
        float lim = EffectiveTimeLimit;
        if (lim > 0f && Elapsed >= lim) EndRun(success: false);
    }

    void EndRun(bool success)
    {
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

        int reward;
        if (success)
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
        reward = Mathf.RoundToInt(reward * integrityMul);
        LastReward = reward;

        if (Upgrade != null && reward > 0) Upgrade.AddReward(reward);

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
