using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화물 Shock 시스템 MVP (세션 14, D-040).
/// GDD: GDD/research/realtime-tension-research.md 섹션 1.
///
/// 설계:
/// - 차량 Rigidbody의 linearVelocity 변화를 차 local frame으로 투영 → 종G/횡G.
/// - 피크(0.2초 윈도우) + 누적(1초 이동평균) 병존.
/// - 충돌 속도 기반 데미지 (ETS2 운동에너지 참고).
/// - HP 100 단일 모델. 0 도달 시 배달 보상 0 (런 실패 아님, CLAUDE.md "런 중 게임오버 없음").
///
/// 튜닝 목표: 안전 주행 95+, 평소 80~90, 공격 주행 50~70.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class CargoBox : MonoBehaviour
{
    [Header("Cargo Type (Day 9~10)")]
    [Tooltip("화물 타입. Awake에서 CargoTypePresets로 sensitivities 자동 적용. " +
             "applyPresetOnAwake = false로 끄면 SerializeField 수동 사용.")]
    [SerializeField] private CargoType cargoType = CargoType.Box;
    [Tooltip("Awake 시 타입별 프리셋(CargoTypePresets) 자동 적용. " +
             "false면 아래 SerializeField 값 그대로 사용 (수동 튜닝 모드).")]
    [SerializeField] private bool applyPresetOnAwake = true;

    [Header("Packing — 충격흡수재 (도현 세션 15 신규)")]
    [Tooltip("포장 등급. 임계값 배율 ↑. 출발 전 OrderBoard에서 선택. " +
             "기본 Bubble = 일반 주행 안전 (좌절 해소).")]
    [SerializeField] private PackingTier packing = PackingTier.Bubble;

    [Header("Integrity")]
    [Tooltip("박스 HP. 100 = 완벽, 0 = 전파.")]
    [SerializeField] private float integrityHP = 100f;
    [SerializeField] private float maxHP = 100f;

    [Header("Longitudinal G (전/후) — 급가속·급브레이크")]
    [Tooltip("경고 시작 G. 도현 피드백(세션 15)으로 0.6→0.9 상향. 일반 제동 안전, 급제동만 데미지.")]
    [SerializeField] private float longWarnG = 0.9f;
    [Tooltip("초과분당 데미지 배율.")]
    [SerializeField] private float longDamageMul = 10f;

    [Header("Lateral G (좌우) — 코너링")]
    [Tooltip("경고 시작 G. 도현 피드백으로 0.7→1.0 상향. 일반 코너 안전.")]
    [SerializeField] private float latWarnG = 1.0f;
    [Tooltip("초과분당 데미지 배율.")]
    [SerializeField] private float latDamageMul = 8f;

    [Header("Spin (Yaw Rate) — 지속 회전, Day 9")]
    [Tooltip("경고 시작 yaw rate (deg/s). 100+ = 급커브, 150+ = 드리프트.")]
    [SerializeField] private float spinWarnDegPerSec = 100f;
    [Tooltip("초과분당 데미지 배율.")]
    [SerializeField] private float spinDamageMul = 0.3f;

    [Header("Tilt (Body Angle) — 차체 기울기, Day 15")]
    [Tooltip("경고 시작 tilt 각도(deg). 차체 up 벡터와 월드 up 사이 각도. " +
             "5~10° = 보통 경사, 15+ = 급경사, 25+ = 위험.")]
    [SerializeField] private float tiltWarnDeg = 10f;
    [Tooltip("초과분당 데미지 배율 (deg×sec). 케이크/탑 적재용 보수치 (타입별 차등).")]
    [SerializeField] private float tiltDamageMul = 0.5f;

    [Header("Collision — 충돌속도 기반")]
    [Tooltip("이 속도 이하 충돌은 무시 (km/h).")]
    [SerializeField] private float impactWarnKmh = 10f;
    [Tooltip("리서치 공식: (impact_kmh - 10) * 0.8.")]
    [SerializeField] private float impactDamageMul = 0.8f;

    [Header("Cumulative — 1초 이동평균 초과분")]
    [Tooltip("안전선 초과 면적 * 배율/초. 공격 주행 전반 포착.")]
    [SerializeField] private float cumulativeMul = 0.5f;

    [Header("Peak Window")]
    [Tooltip("피크 버퍼 프레임 수 (50Hz 기준 10 = 0.2초).")]
    [SerializeField] private int peakWindowFrames = 10;
    [Tooltip("누적 버퍼 프레임 수 (50Hz 기준 50 = 1초).")]
    [SerializeField] private int cumulativeWindowFrames = 50;

    public CargoType Type => cargoType;
    public PackingTier Packing => packing;
    public float IntegrityHP => integrityHP;
    public float MaxHP => maxHP;
    public float IntegrityPercent => maxHP > 0f ? Mathf.Clamp01(integrityHP / maxHP) : 0f;
    public bool IsTotaled => integrityHP <= 0f;
    public float CurrentLongG => curLongG;
    public float CurrentLatG => curLatG;
    // Signed: 양수=가속/우회전, 음수=제동/좌회전. 흔들림 시뮬레이션용.
    public float SignedLongG => curSignedLongG;
    public float SignedLatG => curSignedLatG;
    public float PeakLongG => peakLong;
    public float PeakLatG => peakLat;
    public float CurrentSpinDegPerSec => curSpinDegPerSec;
    public float PeakSpinDegPerSec => peakSpin;
    public float CurrentTiltDeg => curTiltDeg;
    public float PeakTiltDeg => peakTilt;

    /// <summary>Integrity 변화 시 발동 (0~1 normalized).</summary>
    public event Action<float> OnIntegrityChanged;

    /// <summary>충돌 이벤트 발생 시 (km/h).</summary>
    public event Action<float> OnImpact;

    /// <summary>0% 도달 시 1회 발동.</summary>
    public event Action OnTotaled;

    Rigidbody rb;
    Vector3 prevVelocity;
    readonly Queue<float> longPeakBuffer = new Queue<float>();
    readonly Queue<float> latPeakBuffer = new Queue<float>();
    readonly Queue<float> longCumBuffer = new Queue<float>();
    readonly Queue<float> spinPeakBuffer = new Queue<float>();
    readonly Queue<float> tiltPeakBuffer = new Queue<float>();
    float curLongG, curLatG;
    float curSignedLongG, curSignedLatG;
    float peakLong, peakLat;
    float curSpinDegPerSec, peakSpin;
    float curTiltDeg, peakTilt;
    bool totaledFired;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        prevVelocity = Vector3.zero;
        if (applyPresetOnAwake) ApplyPreset();
    }

    /// <summary>
    /// CargoTypePresets에서 타입별 sensitivities 적용.
    /// Day 10 도입. 런타임에 화물 타입 교체할 때도 호출 가능 (예: 허브에서 화물 선택).
    /// Day 15에 tilt 필드 추가, Day 21에 UpgradeSystem 효과 후크 추가.
    /// </summary>
    public void ApplyPreset()
    {
        var p = CargoTypePresets.Get(cargoType);
        longWarnG = p.longWarnG;
        longDamageMul = p.longDamageMul;
        latWarnG = p.latWarnG;
        latDamageMul = p.latDamageMul;
        spinWarnDegPerSec = p.spinWarnDegPerSec;
        spinDamageMul = p.spinDamageMul;
        tiltWarnDeg = p.tiltWarnDeg;
        tiltDamageMul = p.tiltDamageMul;
        impactWarnKmh = p.impactWarnKmh;
        impactDamageMul = p.impactDamageMul;
        cumulativeMul = p.cumulativeMul;

        // 도현 세션 15: 포장(충격흡수재) 임계 배율 적용.
        // 일반 주행 좌절 해소 — 기본 Bubble = ×1.3.
        float packMul = PackingTierHelper.ThresholdMultiplier(packing);
        longWarnG *= packMul;
        latWarnG *= packMul;
        spinWarnDegPerSec *= packMul;
        tiltWarnDeg *= packMul;
        impactWarnKmh *= packMul;

        // Day 21: 업그레이드 효과 적용 (보유 시 임계 완화).
        var ups = UpgradeSystem.Instance;
        if (ups != null)
        {
            if (ups.IsOwned(UpgradeEffect.SuspensionUpgrade))
            {
                longWarnG += 0.1f;
                latWarnG += 0.1f;
            }
            if (ups.IsOwned(UpgradeEffect.SmoothSteering))
            {
                spinWarnDegPerSec += 20f;
            }
            if (ups.IsOwned(UpgradeEffect.ReinforcedTrunk))
            {
                maxHP += 20f;
                integrityHP = maxHP;
            }
        }
    }

    /// <summary>런타임에 포장 등급 변경. 즉시 임계값 재계산.</summary>
    public void SetPacking(PackingTier tier)
    {
        packing = tier;
        ApplyPreset();
    }

    /// <summary>런타임에 화물 타입 교체. 프리셋 자동 적용 + Repair.</summary>
    public void SetCargoType(CargoType newType)
    {
        cargoType = newType;
        ApplyPreset();
        Repair();
    }

    void FixedUpdate()
    {
        if (IsTotaled) return;

        Vector3 v = rb.linearVelocity;
        Vector3 dv = (v - prevVelocity) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        prevVelocity = v;

        // 차 local frame 투영. signed 먼저 구하고 절대값은 피크·누적·데미지에 사용.
        curSignedLongG = Vector3.Dot(dv, transform.forward) / 9.81f;
        curSignedLatG = Vector3.Dot(dv, transform.right) / 9.81f;
        curLongG = Mathf.Abs(curSignedLongG);
        curLatG = Mathf.Abs(curSignedLatG);

        // 피크 버퍼 (0.2초)
        EnqueueBounded(longPeakBuffer, curLongG, peakWindowFrames);
        EnqueueBounded(latPeakBuffer, curLatG, peakWindowFrames);
        peakLong = Max(longPeakBuffer);
        peakLat = Max(latPeakBuffer);

        // 누적 버퍼 (1초)
        EnqueueBounded(longCumBuffer, curLongG, cumulativeWindowFrames);

        // Spin (yaw rate magnitude) — Day 9
        // 차 위 방향 축의 각속도 = yaw rate. 코너링/드리프트 시 큼.
        // transform.up 사용 (월드 up이 아님 — 경사 구간에서 차 자체 yaw 측정).
        float yawRateRad = Mathf.Abs(Vector3.Dot(rb.angularVelocity, transform.up));
        curSpinDegPerSec = yawRateRad * Mathf.Rad2Deg;
        EnqueueBounded(spinPeakBuffer, curSpinDegPerSec, peakWindowFrames);
        peakSpin = Max(spinPeakBuffer);

        // Tilt (body angle vs world up) — Day 15
        // 차체 up과 월드 up 사이 각도. 평지 = 0, 경사·뒤집힘 시 큼.
        // 경사 지속 + 차체 기울기 모두 포착.
        curTiltDeg = Vector3.Angle(transform.up, Vector3.up);
        EnqueueBounded(tiltPeakBuffer, curTiltDeg, peakWindowFrames);
        peakTilt = Max(tiltPeakBuffer);

        // 데미지 계산
        float dt = Time.fixedDeltaTime;
        float dmg = 0f;

        // 피크 초과분 (지속적)
        dmg += Mathf.Max(0f, peakLong - longWarnG) * longDamageMul * dt;
        dmg += Mathf.Max(0f, peakLat - latWarnG) * latDamageMul * dt;

        // Spin 초과분 — Day 9
        dmg += Mathf.Max(0f, peakSpin - spinWarnDegPerSec) * spinDamageMul * dt;

        // Tilt 초과분 — Day 15. 지속 기울기 (deg×sec).
        dmg += Mathf.Max(0f, peakTilt - tiltWarnDeg) * tiltDamageMul * dt;

        // 누적 초과분 (1초 평균 - 안전선)
        float cumAvg = Average(longCumBuffer);
        dmg += Mathf.Max(0f, cumAvg - longWarnG) * cumulativeMul * dt;

        if (dmg > 0f)
            ApplyDamage(dmg);
    }

    void OnCollisionEnter(Collision col)
    {
        if (IsTotaled) return;
        float speedKmh = col.relativeVelocity.magnitude * 3.6f;
        if (speedKmh < impactWarnKmh) return;

        float dmg = (speedKmh - impactWarnKmh) * impactDamageMul;
        OnImpact?.Invoke(speedKmh);
        ApplyDamage(dmg);
    }

    void ApplyDamage(float dmg)
    {
        if (dmg <= 0f) return;
        float prev = integrityHP;
        integrityHP = Mathf.Max(0f, integrityHP - dmg);
        if (!Mathf.Approximately(integrityHP, prev))
            OnIntegrityChanged?.Invoke(IntegrityPercent);

        if (integrityHP <= 0f && !totaledFired)
        {
            totaledFired = true;
            OnTotaled?.Invoke();
        }
    }

    /// <summary>
    /// 외부 시스템(신호등 위반, 이벤트 페널티 등)에서 호출하는 public 데미지 진입점.
    /// OnImpact 이벤트도 함께 발동해 IntegrityHUD가 빨강 flash 표시 + 충돌음 후크 활용.
    /// </summary>
    public void ApplyExternalDamage(float dmg, float flashSpeedKmh = 30f)
    {
        if (dmg <= 0f) return;
        OnImpact?.Invoke(flashSpeedKmh);
        ApplyDamage(dmg);
    }

    /// <summary>박스를 새것으로 교체 (허브에서 호출).</summary>
    public void Repair()
    {
        integrityHP = maxHP;
        totaledFired = false;
        longPeakBuffer.Clear();
        latPeakBuffer.Clear();
        longCumBuffer.Clear();
        spinPeakBuffer.Clear();
        tiltPeakBuffer.Clear();
        peakLong = 0f;
        peakLat = 0f;
        peakSpin = 0f;
        peakTilt = 0f;
        curSpinDegPerSec = 0f;
        curTiltDeg = 0f;
        OnIntegrityChanged?.Invoke(IntegrityPercent);
    }

    static void EnqueueBounded(Queue<float> q, float value, int maxSize)
    {
        q.Enqueue(value);
        while (q.Count > maxSize) q.Dequeue();
    }

    static float Max(Queue<float> q)
    {
        float m = 0f;
        foreach (var v in q) if (v > m) m = v;
        return m;
    }

    static float Average(Queue<float> q)
    {
        if (q.Count == 0) return 0f;
        float sum = 0f;
        foreach (var v in q) sum += v;
        return sum / q.Count;
    }
}
