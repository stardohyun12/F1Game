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
    [SerializeField] private float baseFov = 60f;
    [SerializeField] private float maxFov = 80f;
    [Tooltip("차량 최고속(m/s). 0이면 ArcadeCarController.MaxSpeedMps 사용.")]
    [SerializeField] private float referenceMaxSpeedMps = 0f;
    [SerializeField] private float fovLerpSpeed = 5f;

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
        }
    }
}
