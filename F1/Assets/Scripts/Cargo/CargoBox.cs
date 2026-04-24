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
    [Header("Integrity")]
    [Tooltip("박스 HP. 100 = 완벽, 0 = 전파.")]
    [SerializeField] private float integrityHP = 100f;
    [SerializeField] private float maxHP = 100f;

    [Header("Longitudinal G (전/후) — 급가속·급브레이크")]
    [Tooltip("경고 시작 G. 이 이상부터 손상 누적 (0.6g 리서치 기본).")]
    [SerializeField] private float longWarnG = 0.6f;
    [Tooltip("초과분당 데미지 배율 (리서치 공식: (peak - 0.6) * 10).")]
    [SerializeField] private float longDamageMul = 10f;

    [Header("Lateral G (좌우) — 코너링")]
    [Tooltip("경고 시작 G (0.7g 리서치 기본).")]
    [SerializeField] private float latWarnG = 0.7f;
    [Tooltip("초과분당 데미지 배율 (리서치 공식: (peak - 0.7) * 8).")]
    [SerializeField] private float latDamageMul = 8f;

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

    public float IntegrityHP => integrityHP;
    public float MaxHP => maxHP;
    public float IntegrityPercent => maxHP > 0f ? Mathf.Clamp01(integrityHP / maxHP) : 0f;
    public bool IsTotaled => integrityHP <= 0f;
    public float CurrentLongG => curLongG;
    public float CurrentLatG => curLatG;
    public float PeakLongG => peakLong;
    public float PeakLatG => peakLat;

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
    float curLongG, curLatG;
    float peakLong, peakLat;
    bool totaledFired;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        prevVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (IsTotaled) return;

        Vector3 v = rb.linearVelocity;
        Vector3 dv = (v - prevVelocity) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        prevVelocity = v;

        // 차 local frame 투영 (절대값). G = m/s² / 9.81.
        curLongG = Mathf.Abs(Vector3.Dot(dv, transform.forward)) / 9.81f;
        curLatG = Mathf.Abs(Vector3.Dot(dv, transform.right)) / 9.81f;

        // 피크 버퍼 (0.2초)
        EnqueueBounded(longPeakBuffer, curLongG, peakWindowFrames);
        EnqueueBounded(latPeakBuffer, curLatG, peakWindowFrames);
        peakLong = Max(longPeakBuffer);
        peakLat = Max(latPeakBuffer);

        // 누적 버퍼 (1초)
        EnqueueBounded(longCumBuffer, curLongG, cumulativeWindowFrames);

        // 데미지 계산
        float dt = Time.fixedDeltaTime;
        float dmg = 0f;

        // 피크 초과분 (지속적)
        dmg += Mathf.Max(0f, peakLong - longWarnG) * longDamageMul * dt;
        dmg += Mathf.Max(0f, peakLat - latWarnG) * latDamageMul * dt;

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

    /// <summary>박스를 새것으로 교체 (허브에서 호출).</summary>
    public void Repair()
    {
        integrityHP = maxHP;
        totaledFired = false;
        longPeakBuffer.Clear();
        latPeakBuffer.Clear();
        longCumBuffer.Clear();
        peakLong = 0f;
        peakLat = 0f;
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
