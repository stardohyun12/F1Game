using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ArcadeCarController : MonoBehaviour, IArcadeCarController
{
    // IArcadeCarController 충족: Unity의 transform(소문자)을 인터페이스 Transform(대문자)로 노출.
    Transform IArcadeCarController.Transform => transform;

    [Header("Performance")]
    // 세션 16 #1-4 "여전히 너무 빠름". 110→85, accelTime 1.6→2.2 (배달 게임 톤에 맞춤).
    [SerializeField] private float maxSpeedKmh = 85f;
    [SerializeField] private float accelTime = 2.2f;
    [SerializeField] private float brakeDecel = 8f;
    [SerializeField] private float reverseMaxKmh = 25f;
    [SerializeField] private float coastDrag = 0.8f;
    [SerializeField] private float airDrag = 0.35f;
    [SerializeField] private AnimationCurve brakeStrengthBySpeed =
        AnimationCurve.Linear(0f, 1f, 1f, 0.6f);
    [SerializeField] private float angularDamping = 1.5f;

    [Header("Steering")]
    // 도현 #1-2: 110 → 135 (조향 응답 22%↑).
    [SerializeField] private float maxTurnDegPerSec = 135f;
    [SerializeField] private AnimationCurve turnResponseBySpeed =
        AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
    [SerializeField] private float minSpeedForSteer = 0.5f;

    [Header("Input Smoothing (세션 14 리서치 TOP 1)")]
    // 세션 16 #1-2: 1차 0.06/0.03 → 2차 0.025/0.01 (거의 즉각).
    [Tooltip("조향 입력 SmoothDamp 지연. 0=즉각, 0.025=경쾌.")]
    [SerializeField] private float steerLag = 0.025f;
    [Tooltip("스로틀 입력 SmoothDamp 지연. 0=즉각, 0.01=경쾌.")]
    [SerializeField] private float throttleLag = 0.01f;

    [Header("Grip")]
    [SerializeField] private float sideGripDamping = 6f;
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.3f, 0f);

    [Header("Drift (세션 16 #4 도현 피드백 — 재튜닝)")]
    // 기존 너무 공격적이라 차가 휘청. 차분한 슬라이드 + 입력 제어성 ↑.
    [SerializeField] private float driftGripMultiplier = 0.05f;        // 0.02→0.05 (옆 미끄러짐 약간 줄임)
    [SerializeField] private float driftYawBoost = 1.25f;              // 1.5→1.25 (회전 boost 완화)
    [SerializeField] private float driftBrakeThrottleScale = 0.95f;    // 0.9→0.95 (스로틀 거의 보존)
    [SerializeField] private float driftLongitudinalDrag = 0.6f;       // 0→0.6 (드리프트 중 자연 감속)
    [SerializeField] private float driftMaxSpeedKmh = 65f;             // base 85에 맞춰 비례 인하
    [SerializeField] private float driftAngularDamping = 0.5f;         // 0.3→0.5 (회전 lock 완화)
    [SerializeField] private float driftEntryYawImpulse = 2.5f;        // 4→2.5 (진입 톡 줄임)
    [SerializeField] private float driftYawTorque = 11f;               // 15→11 (지속 토크 완화)

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer = ~0;

    private Rigidbody rb;
    private PlayerInput playerInput;
    private InputAction handbrakeAction;
    private TireSystem tireSystem;
    private Vector2 moveInput;       // raw input (OnMove 즉시 반영)
    private Vector2 smoothedMove;    // smoothed (FixedUpdate에서 SmoothDamp)
    private Vector2 smoothVel;       // SmoothDamp 내부 velocity 상태
    private float maxSpeedMps;
    private float reverseMaxMps;
    private float accelForce;
    private bool isGrounded;
    private bool wasDrifting;

    public float CurrentSpeedMps => rb != null ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f;
    public float MaxSpeedMps => maxSpeedMps;
    public bool IsDrifting { get; private set; }
    public float SteerInput => smoothedMove.x;
    public float ThrottleInput => smoothedMove.y;
    public float RawSteerInput => moveInput.x;
    public float RawThrottleInput => moveInput.y;
    /// <summary>현재 차량 진행 방향의 경사각(deg). 양수=업힐, 음수=다운힐. Day 17 도입.</summary>
    public float CurrentSlopeDeg
    {
        get
        {
            Vector3 fwd = transform.forward;
            return Mathf.Asin(Mathf.Clamp(fwd.y, -1f, 1f)) * Mathf.Rad2Deg;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset;
        rb.angularDamping = angularDamping;
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
            handbrakeAction = playerInput.actions.FindAction("Handbrake", throwIfNotFound: false);
        // Day 20: TireSystem 자동 연결 — 같은 GO 우선, 없으면 Services.
        // null 허용(없으면 1.0 배율 = 기존 동작 그대로).
        tireSystem = GetComponent<TireSystem>();
        if (tireSystem == null) tireSystem = Services.Tires as TireSystem;
        RecalculateForces();
    }

    /// <summary>
    /// 타이어 보정이 적용된 최대 속도(m/s). Soft 타이어 +5%, Hard −5%.
    /// throttle 컷오프, brake 정규화, drag 정규화, 조향 곡선 normalize에 모두 사용.
    /// </summary>
    private float EffectiveMaxSpeedMps =>
        maxSpeedMps * (tireSystem != null ? tireSystem.SpeedMultiplier : 1f);

    void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        RecalculateForces();
    }

    void RecalculateForces()
    {
        maxSpeedMps = maxSpeedKmh / 3.6f;
        reverseMaxMps = reverseMaxKmh / 3.6f;
        if (rb != null)
            accelForce = rb.mass * maxSpeedMps / Mathf.Max(0.1f, accelTime);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        // TOP 1: 입력 SmoothDamp (세션 14 리서치) — steer/throttle에 자연 지연
        smoothedMove.x = Mathf.SmoothDamp(smoothedMove.x, moveInput.x, ref smoothVel.x, steerLag);
        smoothedMove.y = Mathf.SmoothDamp(smoothedMove.y, moveInput.y, ref smoothVel.y, throttleLag);

        // TOP 2: 접지 복원 (기존 하드코딩 제거)
        // 세션 14 fix: transform.position이 BoxCollider 바닥과 같은 높이라 raycast origin이
        // ground 표면 위/아래 경계에 걸려 자기 collider 무시 후에도 ground 못 잡음.
        // 0.5m 위에서 쏘고 distance 보정 (+0.5).
        Vector3 groundOrigin = transform.position + Vector3.up * 0.5f;
        isGrounded = Physics.Raycast(groundOrigin, Vector3.down, groundCheckDistance + 0.5f, groundLayer);
        IsDrifting = handbrakeAction != null && handbrakeAction.IsPressed();

        if (IsDrifting && !wasDrifting)
        {
            rb.angularDamping = driftAngularDamping;
            // Drift 진입 impulse는 raw input 방향 (즉각 반응 유지)
            rb.AddTorque(Vector3.up * (driftEntryYawImpulse * Mathf.Sign(moveInput.x == 0 ? 1f : moveInput.x)), ForceMode.VelocityChange);
        }
        else if (!IsDrifting && wasDrifting)
        {
            rb.angularDamping = angularDamping;
        }
        wasDrifting = IsDrifting;

        Vector3 v = rb.linearVelocity;
        float forwardSpeed = Vector3.Dot(v, transform.forward);
        float sideSpeed = Vector3.Dot(v, transform.right);

        ApplyThrottle(forwardSpeed);
        ApplySteering(forwardSpeed);
        ApplySideGrip(sideSpeed);
        ApplyDriftDrag(forwardSpeed);
        ApplyAirDrag(forwardSpeed);
    }

    void ApplyAirDrag(float forwardSpeed)
    {
        if (forwardSpeed <= 0.1f) return;
        float speedNorm = Mathf.Clamp01(forwardSpeed / EffectiveMaxSpeedMps);
        // TOP 2: 공중(점프/언덕)에서 drag 2배 — 자연스러운 감속감
        float drag = isGrounded ? airDrag : airDrag * 2f;
        rb.AddForce(-transform.forward * (forwardSpeed * drag * speedNorm), ForceMode.Acceleration);
    }

    void ApplyDriftDrag(float forwardSpeed)
    {
        if (!IsDrifting || !isGrounded) return;
        if (forwardSpeed > 0.1f)
            rb.AddForce(-transform.forward * (forwardSpeed * driftLongitudinalDrag), ForceMode.Acceleration);

        float driftMaxMps = driftMaxSpeedKmh / 3.6f;
        if (forwardSpeed > driftMaxMps)
        {
            Vector3 excess = transform.forward * (forwardSpeed - driftMaxMps);
            rb.AddForce(-excess * 10f, ForceMode.Acceleration);
        }
    }

    void ApplyThrottle(float forwardSpeed)
    {
        if (!isGrounded) return;
        float t = smoothedMove.y;

        if (t > 0.01f)
        {
            if (forwardSpeed < EffectiveMaxSpeedMps)
            {
                float throttleScale = IsDrifting ? driftBrakeThrottleScale : 1f;
                // 세션 16 #1-3: 너무 빠름 → 저속 약간 무게 복귀 (0.85). 도달 1.5m/s.
                float lowSpeedRamp = Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 1.5f));
                rb.AddForce(transform.forward * (accelForce * t * throttleScale * lowSpeedRamp), ForceMode.Force);
            }
        }
        else if (t < -0.01f)
        {
            if (forwardSpeed > 0.2f)
            {
                float brakeSpeedNorm = Mathf.Clamp01(forwardSpeed / EffectiveMaxSpeedMps);
                float brakeMult = brakeStrengthBySpeed.Evaluate(brakeSpeedNorm);
                rb.AddForce(-transform.forward * (brakeDecel * rb.mass * -t * brakeMult), ForceMode.Force);
            }
            else if (forwardSpeed > -reverseMaxMps)
                rb.AddForce(transform.forward * (accelForce * t * 0.5f), ForceMode.Force);
        }
        else
        {
            Vector3 horiz = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            rb.AddForce(-horiz * coastDrag, ForceMode.Acceleration);
        }
    }

    void ApplySteering(float forwardSpeed)
    {
        float speedMag = Mathf.Abs(forwardSpeed);
        if (speedMag < minSpeedForSteer) return;

        float speedNorm = Mathf.Clamp01(speedMag / EffectiveMaxSpeedMps);
        float response = turnResponseBySpeed.Evaluate(speedNorm);

        if (IsDrifting)
        {
            rb.AddTorque(Vector3.up * (smoothedMove.x * driftYawTorque * driftYawBoost * response), ForceMode.Acceleration);
        }
        else
        {
            float yaw = maxTurnDegPerSec * smoothedMove.x * response * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));
        }
    }

    void ApplySideGrip(float sideSpeed)
    {
        if (!isGrounded) return;
        float grip = IsDrifting ? sideGripDamping * driftGripMultiplier : sideGripDamping;
        // Day 20: 타이어 그립 배율 — Soft ×1.4, Medium ×1.0, Hard ×0.7. 마모 시 점진 감소.
        if (tireSystem != null) grip *= tireSystem.GripMultiplier;
        rb.AddForce(-transform.right * (sideSpeed * grip), ForceMode.Acceleration);
    }
}
