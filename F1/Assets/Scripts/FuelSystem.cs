using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// 연료 시스템 MVP (2주차 baseline).
/// GDD: GDD/systems/fuel.md, 결정: D-030 K4.
///
/// 설계 기준:
/// - "한 런 안에 주유 결정 2~4번"이 감각 목표
/// - 정속 주행만으로 풀탱크 = ~12분 (시작 튠값)
/// - 행동별 상대 소비: 정속 1.0x / 최고속 2.0x / 급가속 1.5x / 공회전 0.3x
///
/// 부스트는 별도 자원 (boost.md 참조). 연료에서 차감하지 않음.
/// </summary>
[RequireComponent(typeof(ArcadeCarController))]
public class FuelSystem : MonoBehaviour, IFuelSystem
{
    [Header("Tank")]
    [Tooltip("풀탱크 = 1.0. 정상 운전에서 ~12분 지속을 시작 튠값으로.")]
    [SerializeField] private float fuelLevel = 1.0f;

    [Header("Consumption (per second, fraction of tank)")]
    [Tooltip("정속 주행 (속도 30~70% 구간) 소비율. 1/300s = 5분 풀소진. 공격적 주행은 2~3분.")]
    [SerializeField] private float baseConsumptionPerSec = 1f / 300f; // 5분 (튠값)

    [Tooltip("최고속(80%+) 시 배율.")]
    [SerializeField] private float topSpeedMultiplier = 2.0f;

    [Tooltip("급가속 (속도 70%+ 풀쓰로틀) 시 배율.")]
    [SerializeField] private float fullThrottleMultiplier = 1.5f;

    [Tooltip("공회전 (정지 + 시동 켬) 시 배율.")]
    [SerializeField] private float idleMultiplier = 0.3f;

    [Header("Depletion")]
    [Tooltip("고갈 후 견인 발동까지 유예 (초). 0이면 즉시.")]
    [SerializeField] private float depletionGraceSec = 0f;

    [Header("Debug")]
    [Tooltip("F12 누르면 풀탱크 (개발 중 테스트용).")]
    [SerializeField] private bool debugRefillKeyEnabled = true;

    public float FuelLevel => fuelLevel;
    public bool IsDepleted => fuelLevel <= 0f;

    /// <summary>연료 0% 진입 순간 1회 발동.</summary>
    public event Action OnFuelDepleted;

    /// <summary>풀탱크 주유 시 발동 (잔량과 무관).</summary>
    public event Action OnRefill;

    ArcadeCarController car;
    bool depletedFired;
    float depletionTimer;

    void Awake()
    {
        car = GetComponent<ArcadeCarController>();
    }

    void Update()
    {
        if (debugRefillKeyEnabled && Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
            Refill();
    }

    void FixedUpdate()
    {
        if (IsDepleted)
        {
            // 이미 고갈. 견인 이벤트 후 외부에서 Refill 호출 대기.
            return;
        }

        float speedAbs = Mathf.Abs(car.CurrentSpeedMps);
        float speedNorm = car.MaxSpeedMps > 0 ? speedAbs / car.MaxSpeedMps : 0f;
        float throttle = Mathf.Max(0f, car.ThrottleInput); // 후진/브레이크는 소비 안함

        float multiplier;
        if (speedNorm < 0.05f && throttle < 0.05f)
            multiplier = idleMultiplier;       // 정지 + 무입력 = 공회전
        else if (throttle >= 0.8f && speedNorm >= 0.7f)
            multiplier = topSpeedMultiplier;   // 고속 풀쓰로틀
        else if (throttle >= 0.5f)
            multiplier = fullThrottleMultiplier; // 가속 중
        else
            multiplier = 1.0f;                  // 정속/관성

        float burn = baseConsumptionPerSec * multiplier * Time.fixedDeltaTime;
        fuelLevel = Mathf.Max(0f, fuelLevel - burn);

        if (fuelLevel <= 0f)
        {
            depletionTimer += Time.fixedDeltaTime;
            if (!depletedFired && depletionTimer >= depletionGraceSec)
            {
                depletedFired = true;
                OnFuelDepleted?.Invoke();
            }
        }
    }

    /// <summary>풀탱크로 주유. 견인/주유소 호출.</summary>
    public void Refill()
    {
        fuelLevel = 1.0f;
        depletedFired = false;
        depletionTimer = 0f;
        OnRefill?.Invoke();
    }

    /// <summary>지정 비율(0~1)만큼 주유. 절반/풀 옵션용.</summary>
    public void RefillPartial(float amount)
    {
        fuelLevel = Mathf.Clamp01(fuelLevel + amount);
        if (fuelLevel > 0f)
        {
            depletedFired = false;
            depletionTimer = 0f;
        }
        OnRefill?.Invoke();
    }
}
