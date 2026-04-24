using UnityEngine;

/// <summary>
/// CargoSim 월드 (메인 씬에서 멀리 떨어진 외딴 위치)의 박스 Transform을
/// 차량 CargoBox의 G 값에 따라 기울이고/미끄러뜨림. LateUpdate lerp.
/// 실제 물리 시뮬은 아님 — 시각용 fake motion (MVP).
///
/// 방향 관례:
/// - 가속(+SignedLongG) → 박스 뒤로 밀림 + 뒤로 기움 (pitch 음수)
/// - 제동(-SignedLongG) → 박스 앞으로 밀림 + 앞으로 기움 (pitch 양수)
/// - 우회전(+SignedLatG) → 박스 왼쪽 밀림 + 왼쪽으로 기움 (roll 양수)
/// - 좌회전(-SignedLatG) → 박스 오른쪽 밀림 + 오른쪽으로 기움 (roll 음수)
/// </summary>
[DisallowMultipleComponent]
public class CargoShakeSimulator : MonoBehaviour
{
    [SerializeField] CargoBox cargo;
    [SerializeField] Transform boxTransform;

    [Header("Rotation Response (deg per 1G)")]
    [SerializeField] float degPerLongG = 18f;
    [SerializeField] float degPerLatG = 22f;
    [SerializeField] float maxRotDeg = 30f;
    [SerializeField] float rotLerp = 8f;

    [Header("Position Response (meters per 1G)")]
    [SerializeField] float posPerLongG = 0.15f;
    [SerializeField] float posPerLatG = 0.15f;
    [SerializeField] float maxPosOffset = 0.3f;
    [SerializeField] float posLerp = 10f;

    Vector3 restPos;
    Quaternion restRot;
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
        captured = true;
    }

    void LateUpdate()
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

        float pitch = Mathf.Clamp(-cargo.SignedLongG * degPerLongG, -maxRotDeg, maxRotDeg);
        float roll = Mathf.Clamp(cargo.SignedLatG * degPerLatG, -maxRotDeg, maxRotDeg);
        Quaternion targetRot = restRot * Quaternion.Euler(pitch, 0f, roll);

        float dx = Mathf.Clamp(-cargo.SignedLatG * posPerLatG, -maxPosOffset, maxPosOffset);
        float dz = Mathf.Clamp(-cargo.SignedLongG * posPerLongG, -maxPosOffset, maxPosOffset);
        Vector3 targetPos = restPos + new Vector3(dx, 0f, dz);

        float rotT = 1f - Mathf.Exp(-rotLerp * Time.deltaTime);
        float posT = 1f - Mathf.Exp(-posLerp * Time.deltaTime);
        boxTransform.localRotation = Quaternion.Slerp(boxTransform.localRotation, targetRot, rotT);
        boxTransform.localPosition = Vector3.Lerp(boxTransform.localPosition, targetPos, posT);
    }
}
