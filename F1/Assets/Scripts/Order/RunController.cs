using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 런의 진행 추적 + 정산.
/// OrderBoard → BeginRun(job) → 주행 → 마지막 배달 또는 시간초과 → EndRun → ResultPanel.
/// </summary>
public class RunController : MonoBehaviour
{
    [SerializeField] private DeliveryManager deliveryManager;
    [SerializeField] private OverviewCamera overviewCam;
    [SerializeField] private OrderBoard orderBoard;
    [SerializeField] private ResultPanel resultPanel;
    [SerializeField] private FuelSystem fuel;
    [SerializeField] private CargoBox cargo;

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

    void Awake()
    {
        if (deliveryManager == null) deliveryManager = FindFirstObjectByType<DeliveryManager>();
        if (overviewCam == null) overviewCam = FindFirstObjectByType<OverviewCamera>();
        if (orderBoard == null) orderBoard = FindFirstObjectByType<OrderBoard>();
        if (resultPanel == null) resultPanel = FindFirstObjectByType<ResultPanel>();
        if (fuel == null) fuel = FindFirstObjectByType<FuelSystem>();
        if (cargo == null) cargo = FindFirstObjectByType<CargoBox>();
    }

    void Start()
    {
        // 시작 시 보드 오픈
        OpenBoard();
    }

    void OpenBoard()
    {
        // 세션 14 fix: OrderBoard가 씬에 없으면 정지하지 말고 디폴트 job으로 즉시 BeginRun.
        // 도현 "운전 안됨" 원인 — Time.timeScale=0이 영구 고정되던 문제.
        if (orderBoard == null)
        {
            Time.timeScale = 1f;
            BeginRun(MakeFallbackJob());
            return;
        }

        Time.timeScale = 0f;
        if (overviewCam != null && !overviewCam.IsInOverview) overviewCam.GoToOverview();
        orderBoard.Open();
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
        CurrentJob = job;
        Elapsed = 0f;
        CompletedCount = 0;
        RunActive = true;

        // DeliveryManager에 부분집합 활성화
        if (deliveryManager != null)
        {
            deliveryManager.ActivateSubset(job.pointCount);
        }

        // 연료 풀충전
        if (fuel != null) fuel.Refill();

        // 화물 타입 적용 (Day 11). 프리셋 자동 + Repair까지.
        if (cargo != null)
        {
            cargo.SetCargoType(job.cargoType);
            // 도현 세션 15: 포장 적용 (임계 배율 반영).
            cargo.SetPacking(job.defaultPacking);
        }

        // D-043: CCTV 박스 색상 화물별 차별화 (즉시 인식).
        var shakeSim = FindFirstObjectByType<CargoShakeSimulator>();
        if (shakeSim != null)
        {
            shakeSim.SetBoxColor(CargoTypePresets.BoxColor(job.cargoType));
        }

        // 카메라 팔로우로 + 시간 재개
        if (overviewCam != null) overviewCam.GoToFollow(() => {
            Time.timeScale = 1f;
        });
        if (orderBoard != null) orderBoard.Close();

        // DeliveryManager 이벤트 구독
        if (deliveryManager != null)
        {
            deliveryManager.OnDeliveryCompleted.RemoveListener(OnDeliveryCompleted);
            deliveryManager.OnAllCompleted.RemoveListener(OnAllCompleted);
            deliveryManager.OnDeliveryCompleted.AddListener(OnDeliveryCompleted);
            deliveryManager.OnAllCompleted.AddListener(OnAllCompleted);
        }
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
        LastIntegrityPercent = cargo != null ? cargo.IntegrityPercent : 1f;

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

        // Day 21: UpgradeSystem 자금 누적
        if (UpgradeSystem.Instance != null && reward > 0)
            UpgradeSystem.Instance.AddReward(reward);

        Time.timeScale = 0f;
        if (overviewCam != null) overviewCam.GoToOverview();
        if (resultPanel != null) resultPanel.Show(this);
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
        // ResultPanel "다시" 버튼이 호출
        if (resultPanel != null) resultPanel.Hide();
        OpenBoard();
    }
}
