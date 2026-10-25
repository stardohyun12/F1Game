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
    [Tooltip("CargoBox.OnImpact 시 카메라 일시 강한 shake. 충격감 강화.")]
    [SerializeField] private float impactShakeAmplitude = 0.25f;
    [SerializeField] private float impactShakeDuration = 0.35f;

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
    private Vector2 heldShakeSteps;
    private float nextShakeTime;
    private float renderScale = -1f;

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
    void OnEnable() { hasBasePos = false; hasPrevYaw = false; yawRateRad = 0f; heldShakeSteps = Vector2.zero; }

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

    void LateUpdate()
    {
        if (target == null) return;
        if (targetRb == null) targetRb = target.GetComponent<Rigidbody>();
        if (!hasBasePos) { basePos = transform.position; hasBasePos = true; }

        float yaw = target.eulerAngles.y;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

        Vector3 off = preset != null ? new Vector3(0f, preset.height, -preset.distance) : offset;
        float lookH = preset != null ? preset.lookHeight : lookHeight;
        Vector3 desiredPos = target.position + yawRot * off;
        basePos = Vector3.SmoothDamp(basePos, desiredPos, ref posVelocity, positionSmoothTime);
        transform.position = basePos;

        Vector3 lookPoint = target.position + Vector3.up * (lookH - CameraFramingZone.Drop) + (preset != null ? yawRot * Vector3.forward * preset.lookAhead : Vector3.zero);
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
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, rollSpeed * Time.deltaTime);
        Quaternion flatLook = Quaternion.Euler(e.x, e.y, currentRoll);
        transform.rotation = Quaternion.Slerp(transform.rotation, flatLook, rotationLerpSpeed * Time.deltaTime);

        if (cam != null)
        {
            float fov0 = preset != null ? preset.baseFov : baseFov;
            float fov1 = PresetMaxFov();
            float k = preset != null && preset.fovBySpeed != null ? Mathf.Clamp01(preset.fovBySpeed.Evaluate(t)) : t;
            float targetFov = Mathf.Lerp(fov0, fov1, k);
            float fovSpeed = preset != null ? preset.fovLerpSpeed : fovLerpSpeed;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovSpeed * Time.deltaTime);

            if (preset != null)
            {
                // 화면 픽셀 단위 흔들림: 렌더 픽셀 N 칸 = 차 깊이에서 월드 몇 m 인지 환산해 그만큼만 옮긴다.
                if (preset.shakeMaxPx > 0 && preset.shakeStepPx > 0 && t > preset.shakeMinSpeedNorm)
                {
                    float strength = (t - preset.shakeMinSpeedNorm) / Mathf.Max(0.001f, 1f - preset.shakeMinSpeedNorm);
                    // 올림: 문턱을 넘자마자 최소 한 칸. 이후 속도에 따라 최대 shakeMaxPx 까지.
                    int maxSteps = Mathf.CeilToInt(preset.shakeMaxPx * strength / preset.shakeStepPx);
                    if (Time.time >= nextShakeTime)
                    {
                        nextShakeTime = Time.time + 1f / Mathf.Max(1f, preset.shakeHz);
                        heldShakeSteps = new Vector2(Random.Range(-maxSteps, maxSteps + 1), Random.Range(-maxSteps, maxSteps + 1));
                    }
                    float renderH = Mathf.Max(1f, cam.pixelHeight * RenderScale());
                    float depth = Vector3.Distance(basePos, lookPoint);
                    float metersPerPx = 2f * depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / renderH;
                    Vector2 px = heldShakeSteps * preset.shakeStepPx;
                    transform.position += (transform.right * px.x + transform.up * px.y) * metersPerPx;
                }
                else heldShakeSteps = Vector2.zero;
            }
            // 속도 진동 (Day 16 보강) — 고속에서 카메라 미세 흔들림. 프리셋이 있으면 위 픽셀 흔들림이 대신한다.
            else if (t > shakeMinSpeedNorm && maxShakeAmplitude > 0f)
            {
                float shakeStrength = (t - shakeMinSpeedNorm) / (1f - shakeMinSpeedNorm);
                float amp = maxShakeAmplitude * shakeStrength;
                Vector2 shake = new Vector2(
                    (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f
                ) * amp;
                transform.position += transform.right * shake.x + transform.up * shake.y;
            }

            // D-045: 충돌 shake — CargoBox.OnImpact 이벤트로 트리거.
            float impactRemain = impactShakeEndTime - Time.unscaledTime;
            if (impactRemain > 0f && impactShakeAmplitude > 0f)
            {
                float decay = Mathf.Clamp01(impactRemain / impactShakeDuration);
                float amp = impactShakeAmplitude * decay * impactShakeStrength;
                Vector2 shake = (Vector2)Random.insideUnitCircle * amp;
                transform.position += transform.right * shake.x + transform.up * shake.y;
            }
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
