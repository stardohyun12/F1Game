using UnityEngine;

/// <summary>
/// 트렁크 내 박스 물리 시뮬 — 패턴 C+ (도현 세션 15 — 사면 중력 추가).
///
/// 핵심 모델: 박스는 트렁크 **바닥**에 놓여있음.
/// 작용 힘 2종류:
///   1. 관성력 (차 가속/제동/코너 시) — 기존
///   2. **사면 중력** (차체 기울기 시) — D-044 신규
///      차가 좌측 캠버 → 박스 우측 미끄러짐
///      차가 다운힐 → 박스 전방 미끄러짐
///      차가 정지 상태에서도 사면이면 박스 미끄러짐 (실제 차량 박스)
///
/// 동작:
/// - 차 평지 정지 → 박스 가만히
/// - 차 가속/제동/코너 → 관성력으로 미끄러짐
/// - 차 사면 → 사면 중력으로 미끄러짐 (정지 상태에서도)
/// - 차 사면 + 가속 → 두 힘 합산
/// - 회전 X (마찰이 박스 회전 막음)
///
/// 파라미터:
/// - staticFrictionG 0.25 (작은 G에도 미끄러짐 시작 — 사면 작아도 작동)
/// - kineticFriction 1.0 (천천히 감속)
/// - accelScale 1.0 (G가 박스 잘 밀어냄)
/// - maxOffset 0.5 (트렁크 ±0.5m)
/// </summary>
[DisallowMultipleComponent]
public class CargoShakeSimulator : MonoBehaviour, ICargoShakeSimulator
{
    [SerializeField] CargoBox cargo;
    [SerializeField] Transform boxTransform;

    [Header("Rotation (도현 세션 15: 기본 OFF — 갈대 제거)")]
    [Tooltip("박스 회전 적용 여부. false = 트렁크 박스 단단한 느낌. true = 미세 기울기.")]
    [SerializeField] bool enableRotation = false;
    [SerializeField] float degPerLongG = 1.5f;
    [SerializeField] float degPerLatG = 1.5f;
    [SerializeField] float maxRotDeg = 4f;
    [SerializeField] float rotLerp = 6f;

    [Header("Trunk Visual Match (D-048 — yaw만 매칭)")]
    [Tooltip("트렁크 yaw만 차량 매칭. pitch/roll 무시 → 트렁크 visual 항상 평면. " +
             "도현 진단 (D-048): 트렁크는 평평한 곳, 차량 가속/사면에도 평면 유지. " +
             "박스 시뮬은 차량 frame 그대로 작동 (사면 중력 + 관성).")]
    [SerializeField] bool matchCarRotation = true;
    [Tooltip("CargoSimCamRig 자동 추적 (trunk yaw 따라가서 시점 일관 유지).")]
    [SerializeField] Transform simCamRig;

    [Header("Position Physics (패턴 C+ — 자동차 안 화물 느낌)")]
    // D-046 (도현 세션 15): "시뮬레이션 느낌" 진단. 마찰 강화 + 반응 줄임.
    [Tooltip("정지 마찰 임계 G. 일반 사면(10° 이하)에서는 박스 안 움직임.")]
    [SerializeField] float staticFrictionG = 0.30f;
    [Tooltip("운동 마찰 감쇠율 (per sec). 박스가 빨리 멈춤 = 자동차 안 화물 느낌.")]
    [SerializeField] float kineticFriction = 3.0f;
    [Tooltip("G 1단위를 m/s²로 변환. 0.7 = 박스 반응 70% (실제 마찰 큰 화물칸).")]
    [SerializeField] float accelScale = 0.7f;
    [Tooltip("트렁크 내 박스 이동 반경 (local x/z 각각, meters). 트렁크 공간.")]
    [SerializeField] float maxOffset = 0.5f;
    [Tooltip("벽 충돌 시 반발 (0=멈춤, 0.5=절반 반발). 0.05 = 거의 멈춤.")]
    [SerializeField] float wallBounce = 0.05f;
    [Tooltip("velocity 이하이면 정지로 간주 (snap to 0).")]
    [SerializeField] float velocityDeadZone = 0.02f;

    Vector3 restPos;
    Quaternion restRot;
    Vector3 simVelLocal;
    bool captured;

    void Awake()
    {
        if (cargo == null) cargo = Services.Cargo as CargoBox;
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
            cargo = Services.Cargo as CargoBox;
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

        // (1) 관성력: 차 가속/제동/코너 시 박스에 작용. local x = lateral(우+), local z = longitudinal(앞+).
        Vector3 inertialAccel = new Vector3(-cargo.SignedLatG, 0f, -cargo.SignedLongG) * accelScale;

        // (2) 사면 중력 (D-044, 도현 세션 15): 차체 기울어지면 중력의 차 local x/z 성분이 박스를 밀어냄.
        // "박스가 바닥에 있다" 가정의 핵심 — 정지 상태에서도 사면이면 미끄러짐.
        Vector3 worldGravity = Physics.gravity;                // (0, -9.81, 0)
        Vector3 localGravity = cargo.transform.InverseTransformDirection(worldGravity);
        // localGravity.y는 정상 누름 (-9.81 근처) — 무시.
        // localGravity.x/z = 사면 G 성분 (deg/s² 단위). G 단위로 변환해 accelScale 적용.
        Vector3 slopeAccel = new Vector3(localGravity.x, 0f, localGravity.z) / 9.81f * accelScale;

        // 총 가속도 = 관성력 + 사면 중력
        Vector3 totalAccel = inertialAccel + slopeAccel;

        // 정지 마찰: accel 크기가 임계 미만이면 push 없음 (박스 고정)
        float accelMag = Mathf.Sqrt(totalAccel.x * totalAccel.x + totalAccel.z * totalAccel.z);
        if (accelMag >= staticFrictionG * accelScale)
        {
            simVelLocal += totalAccel * dt;
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
        // D-048: 트렁크 yaw만 차량 매칭, pitch/roll 무시 → 트렁크 항상 평면.
        // 도현 진단: 트렁크는 차량의 평평한 부분이라 가정. 차량이 사면/기울기에 있어도
        // 트렁크 visual은 평면 유지. 박스 시뮬만 사면 중력 + 관성으로 평면 위에서 미끄러짐.
        if (matchCarRotation && cargo != null)
        {
            float carYaw = cargo.transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, carYaw, 0f);

            // SimCamRig도 yaw만 차량 매칭 (시점 일관 + position 회전 따라감)
            // Phase 1: GameObject.Find by name 1회 fallback (SerializeField 비어있으면).
            // 정상 경로는 SetupCargoView가 인스펙터에 할당. fallback 발동 시 경고.
            if (simCamRig == null)
            {
                var rigGO = GameObject.Find("CargoSimCamRig");
                if (rigGO != null)
                {
                    simCamRig = rigGO.transform;
                    Debug.LogWarning("[CargoShakeSimulator] simCamRig을 GameObject.Find로 fallback 설정. SetupCargoView 메뉴 실행 권장.");
                }
            }
            if (simCamRig != null)
            {
                Vector3 trunkPos = transform.position;
                // CargoSimCamRig의 SetupCargoView default offset (0, 2.5, -0.8)
                Vector3 offsetLocal = new Vector3(0f, 2.5f, -0.8f);
                simCamRig.position = trunkPos + Quaternion.Euler(0f, carYaw, 0f) * offsetLocal;
                simCamRig.rotation = Quaternion.Euler(65f, carYaw, 0f);
            }
        }

        // 박스 자체 회전 (도현 세션 15: 회전 복귀가 갈대 느낌. 기본 OFF.)
        if (!enableRotation)
        {
            // D-049 안전장치: 외부 영향 차단 — 박스 회전 매 프레임 restRot 강제.
            // 어떤 코드가 박스 localRotation 변경해도 즉시 reset.
            if (boxTransform != null) boxTransform.localRotation = restRot;
            return;
        }
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

    /// <summary>
    /// 박스 색상 변경 (D-043, 도현 세션 15).
    /// RunController.BeginRun에서 화물 타입에 따라 자동 호출.
    /// </summary>
    public void SetBoxColor(Color c)
    {
        if (boxTransform == null) return;
        var mr = boxTransform.GetComponent<MeshRenderer>();
        if (mr == null) return;
        // material instance — sharedMaterial 변경하면 다른 박스에도 영향. material로 instance 생성.
        mr.material.color = c;
    }
}
