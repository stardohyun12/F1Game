using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private ArcadeCarController car;

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -6f);
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

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (car == null && target != null) car = target.GetComponent<ArcadeCarController>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        float yaw = target.eulerAngles.y;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

        Vector3 desiredPos = target.position + yawRot * offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVelocity, positionSmoothTime);

        Quaternion lookRot = Quaternion.LookRotation(target.position + Vector3.up * 1.2f - transform.position, Vector3.up);
        Vector3 e = lookRot.eulerAngles;

        float targetRoll = 0f;
        if (car != null)
        {
            float maxSpdForRoll = car.MaxSpeedMps > 0.01f ? car.MaxSpeedMps : 16.67f;
            float speedNormForRoll = Mathf.Clamp01(Mathf.Abs(car.CurrentSpeedMps) / maxSpdForRoll);
            if (speedNormForRoll > rollMinSpeedNorm)
            {
                float scale = (speedNormForRoll - rollMinSpeedNorm) / (1f - rollMinSpeedNorm);
                targetRoll = car.SteerInput * maxRollDeg * scale;
            }
        }
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, rollLerpSpeed * Time.deltaTime);
        Quaternion flatLook = Quaternion.Euler(e.x, e.y, currentRoll);
        transform.rotation = Quaternion.Slerp(transform.rotation, flatLook, rotationLerpSpeed * Time.deltaTime);

        if (cam != null)
        {
            float maxSpd = referenceMaxSpeedMps > 0.01f
                ? referenceMaxSpeedMps
                : (car != null ? car.MaxSpeedMps : 16.67f);
            float speed = car != null ? Mathf.Abs(car.CurrentSpeedMps) : 0f;
            float t = maxSpd > 0.01f ? Mathf.Clamp01(speed / maxSpd) : 0f;
            float targetFov = Mathf.Lerp(baseFov, maxFov, t);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovLerpSpeed * Time.deltaTime);

            // 속도 진동 (Day 16 보강) — 고속에서 카메라 미세 흔들림.
            if (t > shakeMinSpeedNorm && maxShakeAmplitude > 0f)
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
        // D-045: CargoBox.OnImpact 후크 (충돌 시 카메라 shake).
        var cargo = FindFirstObjectByType<CargoBox>();
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
