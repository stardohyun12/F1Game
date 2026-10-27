using UnityEngine;

/// <summary>
/// 카트 외형(바퀴·차체) 움직임 — 물리 무손상, 보기만.
/// - 바퀴 4개: 속도/반지름만큼 굴림, 앞바퀴는 조향 입력만큼 꺾음.
/// - 차체(body): 가속·코너 G 와 (Wheels 모드) 바퀴 압축 차이로 롤·피치 소량.
/// 바퀴 피벗은 바퀴 중심, 축은 차와 같게(로컬 X = 차 오른쪽) 놓여 있어야 한다 — Scene_A4d_Town 빌드에서 만든다.
/// </summary>
[DisallowMultipleComponent]
public class KartWheelVisual : MonoBehaviour
{
    [SerializeField] Transform body;
    [Tooltip("앞왼·앞오·뒤왼·뒤오 (ArcadeCarController 바퀴 순서와 같다)")]
    [SerializeField] Transform[] wheels = new Transform[4];
    [SerializeField] float wheelRadius = 0.13f;     // 월드 m, 렌더러가 없을 때만 씀
    [SerializeField] float maxSteerDeg = 24f;

    [Header("차체 기울기")]
    [SerializeField] float pitchPerG = 2.5f;
    [SerializeField] float rollPerG = 3f;
    [SerializeField] float maxLeanDeg = 4f;
    [SerializeField] float trackWidth = 1.0f;       // 월드 m, 서스펜션 롤 계산용
    [SerializeField] float wheelBase = 1.1f;
    [SerializeField] float lerpSpeed = 8f;

    ArcadeCarController car;
    Rigidbody rb;
    Quaternion[] rest;
    Quaternion bodyRest;
    float[] radii;
    readonly float[] spin = new float[4];
    Vector3 prevVel, accelSmooth;
    float pitch, roll;

    void Awake()
    {
        car = GetComponentInParent<ArcadeCarController>();
        rb = GetComponentInParent<Rigidbody>();
        rest = new Quaternion[wheels.Length];
        radii = new float[wheels.Length];
        for (int i = 0; i < wheels.Length; i++)
        {
            radii[i] = wheelRadius;
            if (!wheels[i]) continue;
            rest[i] = wheels[i].localRotation;
            // 앞뒤 타이어 크기가 달라(kart_a2 뒤가 큼) 바퀴마다 렌더러 높이로 반지름을 잰다
            var rs = wheels[i].GetComponentsInChildren<Renderer>();
            if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); radii[i] = Mathf.Max(0.02f, b.extents.y); }
        }
        if (body) bodyRest = body.localRotation;
    }

    void LateUpdate()
    {
        if (car == null || rb == null) return;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        float roll0 = car.CurrentSpeedMps * Mathf.Rad2Deg * dt;   // 굴러간 거리(도·m) — 반지름으로 나누면 회전각
        float steer = car.SteerInput * maxSteerDeg;
        for (int i = 0; i < wheels.Length; i++)
        {
            if (!wheels[i]) continue;
            spin[i] = Mathf.Repeat(spin[i] + roll0 / radii[i], 360f);
            wheels[i].localRotation = rest[i] * Quaternion.Euler(0f, i < 2 ? steer : 0f, 0f) * Quaternion.Euler(spin[i], 0f, 0f);
        }
        if (!body) return;

        // 차 기준 가속도(m/s²) — 프레임 잡음 줄이려고 평활
        Vector3 v = car.transform.InverseTransformDirection(rb.linearVelocity);
        accelSmooth = Vector3.Lerp(accelSmooth, (v - prevVel) / dt, 1f - Mathf.Exp(-10f * dt));
        prevVel = v;
        float g = -Physics.gravity.y;
        float tPitch = -accelSmooth.z / g * pitchPerG;      // 가속 → 코 들림
        float tRoll = accelSmooth.x / g * rollPerG;         // 우회전(구심 +x) → 바깥(왼)쪽으로 눌림
        if (car.Mode == ArcadeCarController.DriveMode.Wheels)
        {
            var c = car.WheelCompression;                   // 0 앞왼 1 앞오 2 뒤왼 3 뒤오
            tPitch += Mathf.Atan2((c[0] + c[1]) - (c[2] + c[3]), 2f * wheelBase) * Mathf.Rad2Deg;
            tRoll += Mathf.Atan2((c[0] + c[2]) - (c[1] + c[3]), 2f * trackWidth) * Mathf.Rad2Deg;
        }
        float k = 1f - Mathf.Exp(-lerpSpeed * dt);
        pitch = Mathf.Lerp(pitch, Mathf.Clamp(tPitch, -maxLeanDeg, maxLeanDeg), k);
        roll = Mathf.Lerp(roll, Mathf.Clamp(tRoll, -maxLeanDeg, maxLeanDeg), k);
        body.localRotation = bodyRest * Quaternion.Euler(pitch, 0f, roll);
    }
}
