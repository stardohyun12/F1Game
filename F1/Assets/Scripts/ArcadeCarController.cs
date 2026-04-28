using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ArcadeCarController : MonoBehaviour
{
    [Header("Performance")]
    // 도현 피드백 (세션 15): "속도감 부족". 90→130 km/h, accelTime 2.8→2.0.
    [SerializeField] private float maxSpeedKmh = 130f;
    [SerializeField] private float accelTime = 2.0f;
    [SerializeField] private float brakeDecel = 8f;
    [SerializeField] private float reverseMaxKmh = 25f;
    [SerializeField] private float coastDrag = 0.8f;
    [SerializeField] private float airDrag = 0.35f;
    [SerializeField] private AnimationCurve brakeStrengthBySpeed =
        AnimationCurve.Linear(0f, 1f, 1f, 0.6f);
    [SerializeField] private float angularDamping = 1.5f;

    [Header("Steering")]
    [SerializeField] private float maxTurnDegPerSec = 110f;
    [SerializeField] private AnimationCurve turnResponseBySpeed =
        AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
    [SerializeField] private float minSpeedForSteer = 0.5f;

    [Header("Input Smoothing (세션 14 리서치 TOP 1)")]
    [Tooltip("조향 입력 SmoothDamp 지연. 0=즉각, 0.12=자연 무게감.")]
    [SerializeField] private float steerLag = 0.12f;
    [Tooltip("스로틀 입력 SmoothDamp 지연. 0=즉각, 0.08=관성감.")]
    [SerializeField] private float throttleLag = 0.08f;

    [Header("Grip")]
    [SerializeField] private float sideGripDamping = 6f;
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.3f, 0f);

    [Header("Drift")]
    [SerializeField] private float driftGripMultiplier = 0.02f;
    [SerializeField] private float driftYawBoost = 1.5f;
    [SerializeField] private float driftBrakeThrottleScale = 0.9f;
    [SerializeField] private float driftLongitudinalDrag = 0f;
    [SerializeField] private float driftMaxSpeedKmh = 90f;
    [SerializeField] private float driftAngularDamping = 0.3f;
    [SerializeField] private float driftEntryYawImpulse = 4f;
    [SerializeField] private float driftYawTorque = 15f;

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
        // Day 20: TireSystem 자동 연결 — 같은 GO 우선, 없으면 씬 단일 인스턴스.
        // null 허용(없으면 1.0 배율 = 기존 동작 그대로).
        tireSystem = GetComponent<TireSystem>();
        if (tireSystem == null) tireSystem = FindFirstObjectByType<TireSystem>();
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
                // TOP 2: 저속 램프 — 0~10 km/h(2.8 m/s)에서 가속력 50%→100% 램프 (무게감)
                float lowSpeedRamp = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 2.8f));
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
