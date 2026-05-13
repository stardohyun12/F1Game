using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 런의 진행 추적 + 정산.
/// OrderBoard → BeginRun(job) → 주행 → 마지막 배달 또는 시간초과 → EndRun → ResultPanel.
///
/// Phase 1 리팩터: FindFirstObjectByType 6개 제거. 모든 의존을 Services 경유.
/// 와이어링 책임은 GameContext (씬에 단 하나).
/// 학습: GDD/architecture/phase1-dip-service-locator.md
/// </summary>
public class RunController : MonoBehaviour, IRunController
{
    // IArcadeCarController.Transform 충족용 explicit (인터페이스 자체 추가 위함이 아닌 RunController는 IRunController 구현)

    // Phase 1 Before:
    //   [SerializeField] DeliveryManager deliveryManager;
    //   void Awake() { if(deliveryManager == null) deliveryManager = FindFirstObjectByType<DeliveryManager>(); }
    //   ...6 fields, 6 Find calls
    //
    // Phase 1 After:
    //   IDeliveryManager Delivery => Services.Delivery;
    //   ... 인스펙터 fallback 0, Find 0. 모든 의존 Services 경유.

    IDeliveryManager Delivery => Services.Delivery;
    IOverviewCamera Overview => Services.Overview;
    IOrderBoard Board => Services.Board;
    IResultPanel ResultUI => Services.Result;
    IFuelSystem Fuel => Services.Fuel;
    ICargoBox Cargo => Services.Cargo;
    ICargoShakeSimulator Shake => Services.Shake;
    IUpgradeSystem Upgrade => Services.Upgrade;

    [Header("State")]
    public OrderJob CurrentJob { get; private set; }
    public float Elapsed { get; private set; }
    public bool RunActive { get; private set; }
    public int CompletedCount { get; private set; }
    public bool LastRunSuccess { get; private set; }
    public int LastReward { get; private set; }
    public float LastElapsed { get; private set; }
    /// <summary>EndRun 시점에 캡처. ResultPanel "이야기"용 (Day 12).</summary>
    public float LastIntegrityPercent { get; private set; } = 1f;

    public float TimeRemaining => CurrentJob != null && CurrentJob.timeLimitSec > 0f
        ? Mathf.Max(0f, CurrentJob.timeLimitSec - Elapsed)
        : -1f; // -1 = 무제한

    // Phase 1: Awake의 6개 Find 제거. GameContext가 Services에 등록 보장.
    // (남은 RunController-only 초기화가 있으면 여기.)

    void Start()
    {
        OpenBoard();
    }

    void OpenBoard()
    {
        // 세션 14 fix: OrderBoard가 씬에 없으면 정지하지 말고 디폴트 job으로 즉시 BeginRun.
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
        j.pointCount = 7;
        j.timeLimitSec = 0f; // 무제한
        j.baseReward = 0;
        j.timeBonus = 0;
        return j;
    }

    public void BeginRun(OrderJob job)
    {
        InitRunState(job);
        ActivateDeliveryPoints(job);
        if (Fuel != null) Fuel.Refill();
        ApplyCargoSetup(job);
        UpdateShakeSimColor(job);
        StartFollowCameraAndCloseBoard();
        SubscribeDeliveryEvents();
    }

    /// <summary>
    /// Task 14: packingOverride 무시. 3D 포장 시스템으로 갈아치워짐.
    /// </summary>
    public void BeginRun(OrderJob job, PackingTier? packingOverride)
    {
        // Task 14: packingOverride 무시. 3D 포장 시스템으로 갈아치워짐.
        BeginRun(job);
    }

    /// <summary>
    /// Task 13: PackingScreen에서 wrap된 화물 GameObject 인계.
    /// Task 15에서 CargoShakeSimulator/cargo prefab 통합 시 packedItems 활용 예정.
    /// 현재는 디버그 로그 + 기존 BeginRun(job) 위임.
    /// </summary>
    public void BeginRun(OrderJob job, System.Collections.Generic.List<GameObject> packedItems)
    {
        if (packedItems != null)
            Debug.Log($"[RunController] BeginRun w/ {packedItems.Count} packed items");
        BeginRun(job);
    }

    void InitRunState(OrderJob job)
    {
        CurrentJob = job;
        Elapsed = 0f;
        CompletedCount = 0;
        RunActive = true;
    }

    void ActivateDeliveryPoints(OrderJob job)
    {
        if (Delivery != null) Delivery.ActivateSubset(job.pointCount);
    }

    /// <summary>화물 타입 적용. Task 14: SetPacking 제거 (PackingTier 폐기).</summary>
    void ApplyCargoSetup(OrderJob job)
    {
        if (Cargo == null) return;
        Cargo.SetCargoType(job.cargoType);
        // Task 14: SetPacking removed (PackingTier 폐기)
    }

    /// <summary>D-043: CCTV 박스 색상 화물별 차별화 (즉시 인식).</summary>
    void UpdateShakeSimColor(OrderJob job)
    {
        if (Shake != null) Shake.SetBoxColor(CargoTypePresets.BoxColor(job.cargoType));
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

        if (CurrentJob != null && CurrentJob.timeLimitSec > 0f && Elapsed >= CurrentJob.timeLimitSec)
        {
            EndRun(success: false);
        }
    }

    void EndRun(bool success)
    {
        RunActive = false;
        LastRunSuccess = success;
        LastElapsed = Elapsed;
        LastIntegrityPercent = Cargo != null ? Cargo.IntegrityPercent : 1f;

        // 보상 계산 (Day 11: 화물 상태 가중 추가)
        // GDD systems/cargo.md 5단계: 100~80=100%, 80~50=80%, 50~20=50%, 20~1=20%, 0=0%
        float integrityMul = ComputeIntegrityMultiplier(LastIntegrityPercent);
        int reward = 0;
        if (success)
        {
            reward = CurrentJob.baseReward;
            if (CurrentJob.timeLimitSec > 0f)
                reward += CurrentJob.timeBonus;
        }
        else
        {
            // 시간 초과: 완료한 만큼 비례 보상 (절반 가중)
            float ratio = CurrentJob.pointCount > 0
                ? (float)CompletedCount / CurrentJob.pointCount
                : 0f;
            reward = Mathf.RoundToInt(CurrentJob.baseReward * ratio * 0.5f);
        }
        reward = Mathf.RoundToInt(reward * integrityMul);
        LastReward = reward;

        // Day 21: UpgradeSystem 자금 누적 (Phase 1: Services.Upgrade 경유, 싱글톤 직접 접근 제거)
        if (Upgrade != null && reward > 0) Upgrade.AddReward(reward);

        Time.timeScale = 0f;
        if (Overview != null) Overview.GoToOverview();
        if (ResultUI != null) ResultUI.Show(this);
    }

    /// <summary>
    /// 화물 무결성 → 보상 배율 (GDD 5단계).
    /// </summary>
    public static float ComputeIntegrityMultiplier(float integrityPercent)
    {
        float i = integrityPercent * 100f; // 0~100
        if (i >= 80f) return 1.0f;   // 완벽
        if (i >= 50f) return 0.8f;   // 약간 손상
        if (i >= 20f) return 0.5f;   // 크게 손상
        if (i > 0f)   return 0.2f;   // 거의 전파
        return 0f;                   // 전파
    }

    /// <summary>화물 무결성 → 5단계 라벨 (정산 화면용).</summary>
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
