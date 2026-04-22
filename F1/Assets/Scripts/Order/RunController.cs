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

    [Header("State")]
    public OrderJob CurrentJob { get; private set; }
    public float Elapsed { get; private set; }
    public bool RunActive { get; private set; }
    public int CompletedCount { get; private set; }
    public bool LastRunSuccess { get; private set; }
    public int LastReward { get; private set; }
    public float LastElapsed { get; private set; }

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
    }

    void Start()
    {
        // 시작 시 보드 오픈
        OpenBoard();
    }

    void OpenBoard()
    {
        Time.timeScale = 0f;
        if (overviewCam != null && !overviewCam.IsInOverview) overviewCam.GoToOverview();
        if (orderBoard != null) orderBoard.Open();
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

        // 보상 계산
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
        LastReward = reward;

        Time.timeScale = 0f;
        if (overviewCam != null) overviewCam.GoToOverview();
        if (resultPanel != null) resultPanel.Show(this);
    }

    public void RestartFromBoard()
    {
        // ResultPanel "다시" 버튼이 호출
        if (resultPanel != null) resultPanel.Hide();
        OpenBoard();
    }
}
