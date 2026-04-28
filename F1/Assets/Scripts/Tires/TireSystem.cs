using System;
using UnityEngine;

/// <summary>
/// 타이어 종류. Day 20 (Phase 1 Week 3).
/// 출발 전 결정 → 런 전체에 영향.
/// </summary>
public enum TireType
{
    /// <summary>Soft — 그립 매우 높음, 빠른 마모. 짧은 고속 코너 코스용.</summary>
    Soft,
    /// <summary>Medium — 균형. 표준 베이스라인.</summary>
    Medium,
    /// <summary>Hard — 그립 낮음, 느린 마모. 장거리 안정 주행용.</summary>
    Hard,
}

/// <summary>
/// 타이어 시스템 (Day 20).
/// 출발 전 RoutePlannerPanel(Day 19)에서 선택 → ArcadeCarController.sideGripDamping 곱하기 적용.
/// 마모는 Phase 1에선 표시만, Phase 3에 실제 그립 감소 추가 예정.
/// </summary>
[DisallowMultipleComponent]
public class TireSystem : MonoBehaviour
{
    [SerializeField] private TireType currentTire = TireType.Medium;
    [Tooltip("현재 타이어 마모도 0~1. 0=새 타이어, 1=완전 마모.")]
    [Range(0f, 1f)]
    [SerializeField] private float wear = 0f;

    [Header("Wear Rates (per minute)")]
    [Tooltip("Soft 마모율 (분당, 정상 주행 기준).")]
    [SerializeField] private float wearRateSoft = 0.30f;
    [SerializeField] private float wearRateMedium = 0.18f;
    [SerializeField] private float wearRateHard = 0.10f;

    public TireType Current => currentTire;
    public float Wear => wear;

    /// <summary>그립 배율. ArcadeCarController.sideGripDamping에 곱함.</summary>
    public float GripMultiplier
    {
        get
        {
            float baseMul = currentTire switch
            {
                TireType.Soft => 1.4f,
                TireType.Hard => 0.7f,
                _ => 1.0f,
            };
            // 마모도 1에 도달하면 그립 50% (Phase 3에서 점진 감소 추가).
            return baseMul * Mathf.Lerp(1f, 0.5f, wear);
        }
    }

    /// <summary>최대 속도 배율. 타이어가 가속도/최대속도에 미치는 영향.</summary>
    public float SpeedMultiplier => currentTire switch
    {
        TireType.Soft => 1.05f,    // 살짝 빠름
        TireType.Hard => 0.95f,    // 살짝 느림
        _ => 1.0f,
    };

    public event Action<TireType> OnTireChanged;
    public event Action<float> OnWearChanged;

    /// <summary>출발 전 타이어 선택. 자동으로 wear 0 리셋.</summary>
    public void SetTire(TireType type)
    {
        currentTire = type;
        wear = 0f;
        OnTireChanged?.Invoke(type);
        OnWearChanged?.Invoke(0f);
    }

    /// <summary>피트스탑에서 새 타이어로 교체.</summary>
    public void Replace(TireType type) => SetTire(type);

    void Update()
    {
        // 마모 누적 (분당 비율). Phase 1 = 표시만, 그립에 영향 줌.
        float rate = currentTire switch
        {
            TireType.Soft => wearRateSoft,
            TireType.Hard => wearRateHard,
            _ => wearRateMedium,
        };
        float prev = wear;
        wear = Mathf.Clamp01(wear + rate * Time.deltaTime / 60f);
        if (Mathf.Abs(wear - prev) > 0.001f)
            OnWearChanged?.Invoke(wear);
    }

    public static string DisplayName(TireType t) => t switch
    {
        TireType.Soft => "Soft",
        TireType.Hard => "Hard",
        _ => "Medium",
    };

    public static string Description(TireType t) => t switch
    {
        TireType.Soft => "그립↑↑ · 빠른 마모 · 고속 코너",
        TireType.Hard => "그립↓ · 느린 마모 · 장거리",
        _ => "균형 · 표준",
    };
}
