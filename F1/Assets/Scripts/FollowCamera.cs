using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private ArcadeCarController car;

    [Header("손맛 프리셋 (과제 1720) — 꽂으면 아래 Follow·FOV·Shake·Roll 값 대신 이걸 쓴다")]
    [SerializeField] private CameraFeelPreset preset;
    public enum FovCapTest { Preset, Cap75, Cap85, Cap100 }
    [Tooltip("시험용 최고속 FOV 상한. Preset 이면 프리셋의 maxFov.")]
    [SerializeField] private FovCapTest fovCapTest = FovCapTest.Preset;
    public CameraFeelPreset Preset { get => preset; set => preset = value; }

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -6f);
    [SerializeField] private float lookHeight = 1.2f;
    [SerializeField] private float positionSmoothTime = 0.15f;
    [SerializeField] private float rotationLerpSpeed = 8f;

    [Header("FOV")]
    // 도현 피드백 (세션 15): 속도감 부족. base 50→55→50, max 90→100. 변화폭 큼.
    [SerializeField] private float baseFov = 50f;
    [SerializeField] private float maxFov = 100f;
    [Tooltip("차량 최고속(m/s). 0이면 ArcadeCarController.MaxSpeedMps 사용.")]
    [SerializeField] private float referenceMaxSpeedMps = 0f;
    [SerializeField] private float fovLerpSpeed = 5f;

    [Header("Speed Shake (Day 16 보강)")]
    [Tooltip("고속에서 카메라 미세 진동. 속도감 보강용.")]
    [SerializeField] private float maxShakeAmplitude = 0.05f;
    [Tooltip("진동 시작 속도 비율 (0~1).")]
    [SerializeField] private float shakeMinSpeedNorm = 0.6f;
    [SerializeField] private float shakeFrequency = 18f;

    [Header("Impact Shake (D-045, 도현 세션 15)")]
    [Tooltip("CargoBox.OnImpact 시 카메라 일시 강한 shake. 충격감 강화. (과제 2338: 위치 대신 회전으로만 쓴다 — 이 값은 켜고 끄는 스위치)")]
    [SerializeField] private float impactShakeAmplitude = 0.25f;
    [SerializeField] private float impactShakeDuration = 0.35f;

    [Header("떨림 정리 (과제 20261025-2338) — 흔들림은 위치가 아니라 회전으로, 아주 약하게")]
    [Tooltip("차가 도는 방향을 카메라가 따라가는 시간(초). 0 이면 예전처럼 차 방향에 딱 붙는다 — 키보드 조향 툭툭이 화면 전체로 번진다.")]
    [SerializeField] private float yawSmoothTime = 0.12f;
    [Tooltip("차 높이(노면 턱·바운스)를 카메라가 따라가는 시간(초). 턱 하나에 화면이 위아래로 튀지 않게.")]
    [SerializeField] private float heightSmoothTime = 0.15f;
    [Tooltip("고속 흔들림 최대 각도(도). 화면이 옮겨지지 않고 살짝 기울기만. 0 = 끔.")]
    [SerializeField] private float speedShakeDeg = 0.05f;
    [Tooltip("노면이 거칠 때(차 위아래 속도 변화) 더하는 흔들림 최대 각도(도).")]
    [SerializeField] private float roughShakeDeg = 0.15f;
    [Tooltip("화물 충돌 때 흔들림 최대 각도(도). 0.35초 동안 줄어든다.")]
    [SerializeField] private float impactShakeDeg = 0.8f;

    private float impactShakeEndTime;
    private float impactShakeStrength;

    [Header("Corner Roll")]
    [SerializeField] private float maxRollDeg = 5f;
    [SerializeField] private float rollLerpSpeed = 6f;
    [SerializeField] private float rollMinSpeedNorm = 0.2f;
    private float currentRoll;

    private Camera cam;
    private Vector3 posVelocity;
    private Rigidbody targetRb;
    // 흔들림을 뺀 따라가기 위치. 흔들림이 SmoothDamp 로 새어 들어가 누적되지 않게 따로 든다.
    private Vector3 basePos;
    private bool hasBasePos;
    private float renderScale = -1f;
    // 흔들림을 뺀 회전. 흔들림이 Slerp 로 새어 누적되지 않게 따로 든다.
    private Quaternion baseRot;
    private float smoothYaw, yawVel, smoothY, yVel, prevVy, rough;
    public float DebugShakeDeg { get; private set; }

    /// <summary>QA·디버그용: 마지막 프레임의 목표 롤(도)과 횡가속(m/s²).</summary>
    public float DebugRoll => currentRoll;
    public float DebugLatAccel { get; private set; }

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (car == null && target != null) car = target.GetComponent<ArcadeCarController>();
        if (target != null) targetRb = target.GetComponent<Rigidbody>();
    }

    // 회전 속도는 물리 스텝마다 rb.rotation 차이로 잰다 — 일반 조향은 MoveRotation(ArcadeCarController:362)이라 angularVelocity 가 0 이다.
    private float prevYaw;
    private bool hasPrevYaw;
    private float yawRateRad;

    void FixedUpdate()
    {
        if (targetRb == null) return;
        float y = targetRb.rotation.eulerAngles.y;
        if (hasPrevYaw) yawRateRad = Mathf.DeltaAngle(prevYaw, y) * Mathf.Deg2Rad / Time.fixedDeltaTime;
        prevYaw = y; hasPrevYaw = true;
    }

    // OverviewCamera 가 enabled 를 껐다 켠다 — 다시 켜질 때 지금 자리에서부터 따라가기 시작(예전 동작).
    void OnEnable() { hasBasePos = false; hasPrevYaw = false; yawRateRad = 0f; }

    /// <summary>정지 시 FOV(게임 기본 시야각). 걷기 1인칭(OnFootCamera)이 같은 값을 쓴다.</summary>
    public float BaseFov => preset != null ? preset.baseFov : baseFov;

    /// <summary>
    /// 기능 1b(D-219): 걷기 1인칭 → 카트 탈 때 첫 프레임부터 B 자리에 놓는다(눈높이에서 날아오는 프레임 없이).
    /// 켠 직후(OnEnable 다음) 부른다. 자리·방향은 LateUpdate 의 정지 상태 식과 같고, 그다음부터는 평소 따라가기 그대로.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        if (targetRb == null) targetRb = target.GetComponent<Rigidbody>();
        smoothYaw = target.eulerAngles.y; smoothY = target.position.y; yawVel = yVel = 0f; rough = 0f;
        posVelocity = Vector3.zero; currentRoll = 0f;
        prevVy = targetRb != null ? targetRb.linearVelocity.y : 0f;
        Quaternion yawRot = Quaternion.Euler(0f, smoothYaw, 0f);
        Vector3 off = preset != null ? new Vector3(0f, preset.height, -preset.distance) : offset;
        float lookH = preset != null ? preset.lookHeight : lookHeight;
        basePos = target.position + yawRot * off;
        Vector3 lookPoint = target.position + Vector3.up * (lookH - CameraFramingZone.Drop) + (preset != null ? yawRot * Vector3.forward * preset.lookAhead : Vector3.zero);
        Vector3 e = Quaternion.LookRotation(lookPoint - basePos, Vector3.up).eulerAngles;
        baseRot = Quaternion.Euler(e.x, e.y, 0f);
        transform.SetPositionAndRotation(basePos, baseRot);
        if (cam != null) cam.fieldOfView = BaseFov;
        hasBasePos = true;
    }

    float PresetMaxFov()
    {
        switch (fovCapTest)
        {
            case FovCapTest.Cap75: return 75f;
            case FovCapTest.Cap85: return 85f;
            case FovCapTest.Cap100: return 100f;
            default: return preset != null ? preset.maxFov : maxFov;
        }
    }

    /// <summary>URP renderScale(0.5 → 960×540). PitCrew 어셈블리는 URP 참조가 없어 리플렉션으로 한 번만 읽는다.</summary>
    float RenderScale()
    {
        if (renderScale > 0f) return renderScale;
        renderScale = 1f;
        var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        var p = rp != null ? rp.GetType().GetProperty("renderScale") : null;
        if (p != null) renderScale = Mathf.Max(0.1f, (float)p.GetValue(rp));
        return renderScale;
    }

    static float Ease(float speed, float dt) => 1f - Mathf.Exp(-speed * dt);

    void LateUpdate()
    {
        if (target == null) return;
        if (targetRb == null) targetRb = target.GetComponent<Rigidbody>();
        float dt = Time.deltaTime;
        if (!hasBasePos)
        {
            basePos = transform.position; baseRot = transform.rotation; hasBasePos = true;
            smoothYaw = target.eulerAngles.y; smoothY = target.position.y; yawVel = yVel = 0f; rough = 0f;
            prevVy = targetRb != null ? targetRb.linearVelocity.y : 0f;
        }

        // 과제 2338: 차 방향·높이를 그대로 물지 않고 짧게 늦춰 따라간다 — 조향 툭툭·노면 턱이 화면 전체를 흔들던 길.
        smoothYaw = yawSmoothTime > 0f ? Mathf.SmoothDampAngle(smoothYaw, target.eulerAngles.y, ref yawVel, yawSmoothTime) : target.eulerAngles.y;
        smoothY = heightSmoothTime > 0f ? Mathf.SmoothDamp(smoothY, target.position.y, ref yVel, heightSmoothTime) : target.position.y;
        Vector3 anchor = new Vector3(target.position.x, smoothY, target.position.z);
        Quaternion yawRot = Quaternion.Euler(0f, smoothYaw, 0f);

        Vector3 off = preset != null ? new Vector3(0f, preset.height, -preset.distance) : offset;
        float lookH = preset != null ? preset.lookHeight : lookHeight;
        Vector3 desiredPos = anchor + yawRot * off;
        basePos = Vector3.SmoothDamp(basePos, desiredPos, ref posVelocity, positionSmoothTime);
        transform.position = basePos;

        Vector3 lookPoint = anchor + Vector3.up * (lookH - CameraFramingZone.Drop) + (preset != null ? yawRot * Vector3.forward * preset.lookAhead : Vector3.zero);
        Quaternion lookRot = Quaternion.LookRotation(lookPoint - basePos, Vector3.up);
        Vector3 e = lookRot.eulerAngles;

        float maxSpd = referenceMaxSpeedMps > 0.01f
            ? referenceMaxSpeedMps
            : (car != null && car.MaxSpeedMps > 0.01f ? car.MaxSpeedMps : 16.67f);
        float speed = car != null ? Mathf.Abs(car.CurrentSpeedMps) : 0f;
        float t = Mathf.Clamp01(speed / maxSpd);

        // 횡가속 ≈ 회전 속도(rad/s) × 전진 속도. 오른쪽으로 돌면 +(조향 + 와 같은 부호).
        DebugLatAccel = car != null ? yawRateRad * car.CurrentSpeedMps : 0f;

        float targetRoll = 0f;
        if (car != null)
        {
            float rollMin = preset != null ? preset.rollMinSpeedNorm : rollMinSpeedNorm;
            if (t > rollMin)
            {
                float scale = (t - rollMin) / (1f - rollMin);
                if (preset != null)
                {
                    float lat = Mathf.Clamp(DebugLatAccel / Mathf.Max(0.1f, preset.rollFullLatAccel), -1f, 1f);
                    float mix = Mathf.Clamp(car.SteerInput * preset.rollSteerWeight + lat * (1f - preset.rollSteerWeight), -1f, 1f);
                    targetRoll = mix * preset.maxRollDeg * scale;
                }
                else targetRoll = car.SteerInput * maxRollDeg * scale;
            }
        }
        float rollSpeed = preset != null ? preset.rollLerpSpeed : rollLerpSpeed;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Ease(rollSpeed, dt));
        Quaternion flatLook = Quaternion.Euler(e.x, e.y, currentRoll);
        baseRot = Quaternion.Slerp(baseRot, flatLook, Ease(rotationLerpSpeed, dt));

        // 흔들림 = 회전 미세 노이즈 하나로 모은다(속도 + 노면 거칠기 + 충돌). 화면을 옮기지 않아 저해상도 픽셀이 통째로 튀지 않는다.
        float shakeDeg = 0f;
        float shakeMin = preset != null ? preset.shakeMinSpeedNorm : shakeMinSpeedNorm;
        if (t > shakeMin) shakeDeg += speedShakeDeg * (t - shakeMin) / Mathf.Max(0.001f, 1f - shakeMin);
        if (targetRb != null && dt > 0f)
        {
            float vy = targetRb.linearVelocity.y;
            float jolt = Mathf.Clamp01(Mathf.Abs(vy - prevVy) / dt / 30f);
            prevVy = vy;
            rough = Mathf.Max(jolt, Mathf.Lerp(rough, 0f, Ease(4f, dt)));
            shakeDeg += roughShakeDeg * rough * Mathf.Clamp01(t * 2f);
        }
        float impactRemain = impactShakeEndTime - Time.unscaledTime;
        if (impactRemain > 0f && impactShakeAmplitude > 0f)
            shakeDeg += impactShakeDeg * impactShakeStrength * Mathf.Clamp01(impactRemain / impactShakeDuration);
        DebugShakeDeg = shakeDeg;
        Quaternion noise = Quaternion.identity;
        if (shakeDeg > 0.0001f)
        {
            float f = Time.time * 9f;
            noise = Quaternion.Euler((Mathf.PerlinNoise(f, 0.3f) - 0.5f) * 2f * shakeDeg, (Mathf.PerlinNoise(0.7f, f) - 0.5f) * 2f * shakeDeg, 0f);
        }
        transform.rotation = baseRot * noise;

        if (cam != null)
        {
            float fov0 = preset != null ? preset.baseFov : baseFov;
            float fov1 = PresetMaxFov();
            float k = preset != null && preset.fovBySpeed != null ? Mathf.Clamp01(preset.fovBySpeed.Evaluate(t)) : t;
            float targetFov = Mathf.Lerp(fov0, fov1, k);
            float fovSpeed = preset != null ? preset.fovLerpSpeed : fovLerpSpeed;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Ease(fovSpeed, dt));
        }
    }

    void Start()
    {
        // D-045: CargoBox.OnImpact 후크. Phase 1: Services 경유.
        var cargo = Services.Cargo as CargoBox;
        if (cargo != null)
        {
            cargo.OnImpact -= OnCargoImpact;
            cargo.OnImpact += OnCargoImpact;
        }
    }

    void OnCargoImpact(float speedKmh)
    {
        // 충돌 강도 비례 (0~1).
        impactShakeStrength = Mathf.Clamp01(speedKmh / 50f);
        impactShakeEndTime = Time.unscaledTime + impactShakeDuration;
    }
}
