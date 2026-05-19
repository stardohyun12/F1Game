using UnityEngine;
using System;
using UnityEngine.InputSystem;

/// <summary>
/// 기어 시스템 MVP — Auto만 (2주차 baseline).
/// GDD: GDD/systems/gears.md, 결정: D-030 K3+K5.
///
/// 2주차 범위: Auto + HUD 기어 숫자 표시. Manual 토글은 3주차.
///
/// 속도대별 기어 매핑 (5단 고정):
///   1: 0~20% maxSpeed
///   2: 20~40%
///   3: 40~60%
///   4: 60~80%
///   5: 80%+
///   R: 후진 시
///   N: 정차 + 무입력
/// </summary>
[RequireComponent(typeof(ArcadeCarController))]
public class GearSystem : MonoBehaviour, IGearSystem
{
    [Header("Setup")]
    [SerializeField] private int gearCount = 5;
    [Tooltip("기어별 속도 임계값 (0~1, maxSpeed 비율). 길이 = gearCount.")]
    [SerializeField] private float[] shiftUpThresholds = { 0.20f, 0.40f, 0.60f, 0.80f };

    [Tooltip("히스테리시스: 다운시프트는 업시프트보다 이만큼 낮은 속도에서 발생 (덜덜거림 방지).")]
    [SerializeField] private float hysteresis = 0.04f;

    [Tooltip("정지 + 무입력 시 N (Neutral) 표시.")]
    [SerializeField] private bool useNeutral = true;
    [SerializeField] private float neutralSpeedThreshold = 0.03f;

    [Header("Manual Mode (D-089)")]
    [SerializeField] private bool manualUnlocked = false;
    [SerializeField] private int minGear = -1;
    [SerializeField] private int maxGear = 5;

    public bool ManualUnlocked => manualUnlocked;

    public void SetManualUnlocked(bool v)
    {
        manualUnlocked = v;
        Debug.Log($"[GearSystem] ManualUnlocked={v}");
    }

    /// <summary>현재 기어. -1 = R, 0 = N, 1~N = 정상 기어.</summary>
    public int CurrentGear { get; private set; } = 0;

    /// <summary>표시용 문자열 (HUD에서 그대로 사용).</summary>
    public string GearLabel
    {
        get
        {
            if (CurrentGear == -1) return "R";
            if (CurrentGear == 0) return useNeutral ? "N" : "1";
            return CurrentGear.ToString();
        }
    }

    /// <summary>기어 변경 순간 1회 발동 (HUD 플래시용).</summary>
    public event Action<int, int> OnShift; // (이전, 현재)

    ArcadeCarController car;

    void Awake()
    {
        car = GetComponent<ArcadeCarController>();
    }

    void FixedUpdate()
    {
        if (manualUnlocked) return;
        int newGear = ComputeGear();
        if (newGear != CurrentGear)
        {
            int prev = CurrentGear;
            CurrentGear = newGear;
            OnShift?.Invoke(prev, newGear);
        }
    }

    void Update()
    {
        if (!manualUnlocked) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.kKey.wasPressedThisFrame)
        {
            int prev = CurrentGear;
            CurrentGear = Mathf.Max(minGear, CurrentGear - 1);
            if (CurrentGear != prev) OnShift?.Invoke(prev, CurrentGear);
        }
        if (kb.lKey.wasPressedThisFrame)
        {
            int prev = CurrentGear;
            CurrentGear = Mathf.Min(maxGear, CurrentGear + 1);
            if (CurrentGear != prev) OnShift?.Invoke(prev, CurrentGear);
        }
    }

    int ComputeGear()
    {
        float speedSigned = car.CurrentSpeedMps;
        float speedNorm = car.MaxSpeedMps > 0 ? speedSigned / car.MaxSpeedMps : 0f;

        // 후진
        if (speedNorm < -neutralSpeedThreshold) return -1;

        // 중립 (정지 + 무입력)
        if (useNeutral && Mathf.Abs(speedNorm) < neutralSpeedThreshold && Mathf.Abs(car.ThrottleInput) < 0.05f)
            return 0;

        // 정상 기어 — 히스테리시스 적용
        int target = 1;
        for (int i = 0; i < shiftUpThresholds.Length && i < gearCount - 1; i++)
        {
            float thresh = shiftUpThresholds[i];
            // 업시프트는 thresh 초과에서, 다운시프트는 (thresh - hysteresis) 미만에서
            if (CurrentGear > i + 1)
            {
                // 이미 더 높은 기어. 다운시프트 조건 체크.
                if (speedNorm > thresh - hysteresis) target = i + 2;
            }
            else
            {
                // 같거나 낮은 기어. 업시프트 조건 체크.
                if (speedNorm > thresh) target = i + 2;
            }
        }
        return target;
    }
}
