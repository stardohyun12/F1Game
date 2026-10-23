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

    [Header("F1 느낌 (D-122) — 다운포스")]
    [Tooltip("속도² 비례 옆 그립 증가. 1 = 최고속에서 그립 2배.")]
    [SerializeField] private float downforceGripGain = 0.8f;
    [Tooltip("속도² 비례 아래로 누르는 힘(m/s², 최고속 기준). 고속 안정감.")]
    [SerializeField] private float downforceAccel = 6f;

    [Header("Drift 2 (D-122) — 목표 회전 속도 방식")]
    [Tooltip("핸드브레이크 + 풀 조향 때 목표 회전 속도(deg/s).")]
    [SerializeField] private float driftYawRateDeg = 95f;
    [Tooltip("현재 회전 속도가 목표로 따라가는 빠르기(1/s). 높을수록 즉각.")]
    [SerializeField] private float driftYawResponse = 6f;
    [Tooltip("드리프트 중 속도 방향이 차 머리 쪽으로 돌아오는 빠르기(1/s, slip 각에 비례). 평형 slip ≈ 회전속도/이 값. 0 이면 계속 미끄러짐.")]
    [SerializeField] private float driftVelocityAlign = 1.6f;
    [Tooltip("드리프트를 놓은 뒤 그립이 완전히 돌아오기까지(초). 갑자기 꺾이는 스냅 방지.")]
    [SerializeField] private float driftGripRecoverTime = 0.35f;
    private float driftGripBlend = 1f; // 0 = 드리프트 그립, 1 = 정상 그립
    // QA 합성 주행용(리플렉션으로 켬) — 에디터 포커스 없이 핸드브레이크 재현.
    private bool qaForceHandbrake = false;

    [Header("Engine Restore (D-089)")]
    [SerializeField] private float engineMaxSpeedMul = 1.0f;   // 1.20 after restore
    [SerializeField] private float engineAccelMul = 1.0f;       // 1.30 after restore

    public float EngineMaxSpeedMul => engineMaxSpeedMul;
    public float EngineAccelMul => engineAccelMul;

    public void ApplyEngineRestore(float maxSpeedMul = 1.20f, float accelMul = 1.30f)
    {
        engineMaxSpeedMul = maxSpeedMul;
        engineAccelMul = accelMul;
        Debug.Log($"[ArcadeCarController] Engine restored: maxSpeedMul={maxSpeedMul:F2} accelMul={accelMul:F2}");
    }

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 1.5f;
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
        maxSpeedMps * (tireSystem != null ? tireSystem.SpeedMultiplier : 1f) * engineMaxSpeedMul
        * ((Services.Boost?.IsActive ?? false) ? 1.50f : 1f);

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
        // Stage gating: 운전 단계가 아니면 입력/물리 모두 차단.
        var stageRc = Services.Run as RunController;
        if (stageRc == null || stageRc.CurrentStage != RunStage.Driving)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            return;
        }

        // TOP 1: 입력 SmoothDamp (세션 14 리서치) — steer/throttle에 자연 지연
        smoothedMove.x = Mathf.SmoothDamp(smoothedMove.x, moveInput.x, ref smoothVel.x, steerLag);
        smoothedMove.y = Mathf.SmoothDamp(smoothedMove.y, moveInput.y, ref smoothVel.y, throttleLag);

        // TOP 2: 접지 복원 (기존 하드코딩 제거)
        // 세션 14 fix: transform.position이 BoxCollider 바닥과 같은 높이라 raycast origin이
        // ground 표면 위/아래 경계에 걸려 자기 collider 무시 후에도 ground 못 잡음.
        // 0.5m 위에서 쏘고 distance 보정 (+0.5).
        Vector3 groundOrigin = transform.position + Vector3.up * 0.5f;
        // 트리거(DeliveryPoint SphereCollider 등) 무시 — 트리거 위에서 grounded 오판 차단.
        isGrounded = Physics.Raycast(groundOrigin, Vector3.down, groundCheckDistance + 0.5f, groundLayer, QueryTriggerInteraction.Ignore);
        IsDrifting = (handbrakeAction != null && handbrakeAction.IsPressed()) || qaForceHandbrake;

        if (IsDrifting && !wasDrifting)
        {
            rb.angularDamping = driftAngularDamping;
            // D-122: 진입 톡은 조향 중일 때만, 작게 (예전엔 조향 없이도 돌아 휘청).
            if (Mathf.Abs(moveInput.x) > 0.2f)
                rb.AddTorque(Vector3.up * (driftEntryYawImpulse * 0.25f * Mathf.Sign(moveInput.x)), ForceMode.VelocityChange);
        }
        else if (!IsDrifting && wasDrifting)
        {
            rb.angularDamping = angularDamping;
        }
        wasDrifting = IsDrifting;

        Vector3 v = rb.linearVelocity;
        float forwardSpeed = Vector3.Dot(v, transform.forward);
        float sideSpeed = Vector3.Dot(v, transform.right);

        // 드리프트 그립 블렌드: 들어갈 땐 즉시, 나올 땐 driftGripRecoverTime 동안 서서히.
        if (IsDrifting) driftGripBlend = 0f;
        else driftGripBlend = Mathf.MoveTowards(driftGripBlend, 1f, Time.fixedDeltaTime / Mathf.Max(0.01f, driftGripRecoverTime));

        ApplyThrottle(forwardSpeed);
        ApplySteering(forwardSpeed);
        ApplySideGrip(sideSpeed, forwardSpeed);
        ApplyDriftAlign();
        ApplyDriftDrag(forwardSpeed);
        ApplyAirDrag(forwardSpeed);
        ApplyDownforce(forwardSpeed);
    }

    /// <summary>D-122 — 속도² 비례로 차를 바닥에 누른다(고속 안정, F1 다운포스 느낌).</summary>
    void ApplyDownforce(float forwardSpeed)
    {
        if (!isGrounded || downforceAccel <= 0f) return;
        float n = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(0.1f, EffectiveMaxSpeedMps));
        rb.AddForce(-transform.up * (downforceAccel * n * n), ForceMode.Acceleration);
    }

    /// <summary>D-122 — 드리프트 중 속도 방향을 차 머리 쪽으로 천천히 돌린다(속력 유지). 끝없이 미끄러지지 않게.</summary>
    void ApplyDriftAlign()
    {
        if (!IsDrifting || !isGrounded || driftVelocityAlign <= 0f) return;
        Vector3 v = rb.linearVelocity;
        Vector3 horiz = Vector3.ProjectOnPlane(v, Vector3.up);
        float spd = horiz.magnitude;
        if (spd < 1f) return;
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (Vector3.Dot(horiz, fwd) < 0f) return; // 후진 방향이면 건드리지 않음
        // 정렬 속도 ∝ 미끄러짐 각 → 평형 slip = 회전속도 / driftVelocityAlign. 상수 속도면 회전이 더 빠를 때 slip 이 0 으로 붙는다.
        float slipRad = Vector3.Angle(horiz, fwd) * Mathf.Deg2Rad;
        float step = Mathf.Max(driftVelocityAlign * slipRad, 0.05f) * Time.fixedDeltaTime;
        Vector3 aligned = Vector3.RotateTowards(horiz, fwd * spd, step, 0f);
        rb.linearVelocity = aligned + Vector3.up * v.y;
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
        float boostMul = (Services.Boost?.IsActive ?? false) ? 1.50f : 1f;
        float effectiveAccelForce = accelForce * Mathf.Max(0.01f, engineAccelMul) * boostMul;

        if (t > 0.01f)
        {
            if (forwardSpeed < EffectiveMaxSpeedMps)
            {
                float throttleScale = IsDrifting ? driftBrakeThrottleScale : 1f;
                // 세션 16 #1-3: 너무 빠름 → 저속 약간 무게 복귀 (0.85). 도달 1.5m/s.
                float lowSpeedRamp = Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 1.5f));
                rb.AddForce(transform.forward * (effectiveAccelForce * t * throttleScale * lowSpeedRamp), ForceMode.Force);
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
                rb.AddForce(transform.forward * (effectiveAccelForce * t * 0.5f), ForceMode.Force);
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
            // D-122: 토크를 쌓는 대신 목표 회전 속도로 수렴 — 조향을 놓으면 회전도 멈춰서 예측 가능.
            float target = smoothedMove.x * driftYawRateDeg * Mathf.Deg2Rad * Mathf.Clamp01(speedMag / 8f);
            Vector3 av = rb.angularVelocity;
            av.y = Mathf.Lerp(av.y, target, 1f - Mathf.Exp(-driftYawResponse * Time.fixedDeltaTime));
            rb.angularVelocity = av;
        }
        else
        {
            float yaw = maxTurnDegPerSec * smoothedMove.x * response * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));
        }
    }

    void ApplySideGrip(float sideSpeed, float forwardSpeed)
    {
        if (!isGrounded) return;
        // D-122: 드리프트 그립 ↔ 정상 그립을 블렌드(놓은 직후 스냅 방지) + 속도² 다운포스 그립.
        float grip = Mathf.Lerp(sideGripDamping * driftGripMultiplier, sideGripDamping, driftGripBlend);
        float n = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(0.1f, EffectiveMaxSpeedMps));
        grip *= 1f + downforceGripGain * n * n;
        // Day 20: 타이어 그립 배율 — Soft ×1.4, Medium ×1.0, Hard ×0.7. 마모 시 점진 감소.
        if (tireSystem != null) grip *= tireSystem.GripMultiplier;
        rb.AddForce(-transform.right * (sideSpeed * grip), ForceMode.Acceleration);
    }
}
