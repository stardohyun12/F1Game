using UnityEngine;

/// <summary>
/// 트렁크 내 박스 물리 시뮬 — 패턴 B (Kinematic + Custom physics state).
/// 세션 14 후반, 리서치 `GDD/research/cargo-shake-research.md` 패턴 B.
///
/// 도현 피드백 "바람에 흔들리는 갈대" 해결:
/// - rest 복귀 Lerp 제거 → velocity 기억 (관성)
/// - 정지 마찰 (staticFrictionG) → G 임계 미만은 박스가 움직이지 않음
/// - 운동 마찰 (kineticFriction) → 일단 미끄러지기 시작하면 서서히 감쇠
/// - AABB clamp + wall bounce → 벽에 부딪히면 그 자리에서 멈춤 (rest 복귀 X)
///
/// 회전은 기존 Lerp 방식 유지 — 갈대 문제의 근본은 position이었음.
/// 회전은 살짝 기울어지는 정도라 rest 복귀가 자연스러움.
/// </summary>
[DisallowMultipleComponent]
public class CargoShakeSimulator : MonoBehaviour
{
    [SerializeField] CargoBox cargo;
    [SerializeField] Transform boxTransform;

    [Header("Rotation (Lerp 유지)")]
    [SerializeField] float degPerLongG = 3f;
    [SerializeField] float degPerLatG = 3f;
    [SerializeField] float maxRotDeg = 8f;
    [SerializeField] float rotLerp = 3f;

    [Header("Position Physics (패턴 B)")]
    [Tooltip("정지 마찰 임계 G. 이 이하의 관성력은 박스를 움직이지 못함.")]
    [SerializeField] float staticFrictionG = 0.3f;
    [Tooltip("운동 마찰 감쇠율 (per sec). 움직이는 박스를 서서히 멈춤.")]
    [SerializeField] float kineticFriction = 3.5f;
    [Tooltip("G 1단위를 m/s²로 변환. 낮을수록 박스 반응 약함 (현실 9.81 과잉).")]
    [SerializeField] float accelScale = 1.2f;
    [Tooltip("트렁크 내 박스 이동 반경 (local x/z 각각, meters).")]
    [SerializeField] float maxOffset = 0.45f;
    [Tooltip("벽 충돌 시 반발 (0=멈춤, 0.5=절반 반발).")]
    [SerializeField] float wallBounce = 0.25f;
    [Tooltip("velocity 이하이면 정지로 간주 (snap to 0).")]
    [SerializeField] float velocityDeadZone = 0.02f;

    Vector3 restPos;
    Quaternion restRot;
    Vector3 simVelLocal;
    bool captured;

    void Awake()
    {
        if (cargo == null) cargo = FindFirstObjectByType<CargoBox>();
        if (boxTransform == null) boxTransform = transform.Find("Box");
        CaptureRest();
    }

    void CaptureRest()
    {
        if (boxTransform == null) return;
        restPos = boxTransform.localPosition;
        restRot = boxTransform.localRotation;
        simVelLocal = Vector3.zero;
        captured = true;
    }

    void FixedUpdate()
    {
        if (cargo == null)
        {
            cargo = FindFirstObjectByType<CargoBox>();
            if (cargo == null) return;
        }
        if (boxTransform == null)
        {
            boxTransform = transform.Find("Box");
            if (boxTransform == null) return;
            CaptureRest();
        }
        if (!captured) CaptureRest();

        float dt = Time.fixedDeltaTime;

        // 차량 frame에서 박스는 -G 방향 관성력. local x = lateral(우+), local z = longitudinal(앞+).
        Vector3 inertialAccel = new Vector3(-cargo.SignedLatG, 0f, -cargo.SignedLongG) * accelScale;

        // 정지 마찰: accel 크기가 임계 미만이면 push 없음 (박스 고정)
        float accelMag = Mathf.Sqrt(inertialAccel.x * inertialAccel.x + inertialAccel.z * inertialAccel.z);
        if (accelMag >= staticFrictionG * accelScale)
        {
            simVelLocal += inertialAccel * dt;
        }

        // 운동 마찰: 항상 감쇠 (일단 움직이면 서서히 멈춤)
        float dampFactor = Mathf.Max(0f, 1f - kineticFriction * dt);
        simVelLocal.x *= dampFactor;
        simVelLocal.z *= dampFactor;

        // velocity dead zone
        if (simVelLocal.sqrMagnitude < velocityDeadZone * velocityDeadZone)
            simVelLocal = Vector3.zero;

        // 위치 적분 (local offset 기준)
        Vector3 offset = boxTransform.localPosition - restPos;
        offset += simVelLocal * dt;

        // AABB clamp + wall bounce
        if (Mathf.Abs(offset.x) > maxOffset)
        {
            offset.x = Mathf.Sign(offset.x) * maxOffset;
            simVelLocal.x = -simVelLocal.x * wallBounce;
        }
        if (Mathf.Abs(offset.z) > maxOffset)
        {
            offset.z = Mathf.Sign(offset.z) * maxOffset;
            simVelLocal.z = -simVelLocal.z * wallBounce;
        }
        offset.y = 0f;

        boxTransform.localPosition = restPos + offset;
    }

    void LateUpdate()
    {
        if (cargo == null || boxTransform == null) return;

        float pitch = Mathf.Clamp(-cargo.SignedLongG * degPerLongG, -maxRotDeg, maxRotDeg);
        float roll = Mathf.Clamp(cargo.SignedLatG * degPerLatG, -maxRotDeg, maxRotDeg);
        Quaternion targetRot = restRot * Quaternion.Euler(pitch, 0f, roll);
        float rotT = 1f - Mathf.Exp(-rotLerp * Time.deltaTime);
        boxTransform.localRotation = Quaternion.Slerp(boxTransform.localRotation, targetRot, rotT);
    }

    /// <summary>허브에서 박스 교체/Repair 시 호출.</summary>
    public void ResetBoxPosition()
    {
        if (boxTransform == null) return;
        boxTransform.localPosition = restPos;
        boxTransform.localRotation = restRot;
        simVelLocal = Vector3.zero;
    }
}
