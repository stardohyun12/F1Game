using UnityEngine;

/// <summary>
/// 차체 시각 기울기 (세션 14 리서치 TOP 3).
/// benmcinnes/ArcadeVehiclePhysics 패턴 차용 — 물리 Rigidbody 회전 무손상.
///
/// CargoBox의 SignedLongG/SignedLatG 재사용 (이미 측정됨).
/// - 가속(+SignedLongG) → 박스가 뒤로(-pitch) = 차 앞쪽이 뜸(squat)
/// - 제동(-SignedLongG) → 박스가 앞으로(+pitch) = 차 앞쪽 내려감(nose dive)
/// - 우회전(+SignedLatG) → roll 양수 (왼쪽 기움)
///
/// Race/CarVisual 자식에 부착. body + wheels 4개가 CarVisual 자식으로 묶여 함께 기울어짐.
/// </summary>
[DisallowMultipleComponent]
public class CarVisualLean : MonoBehaviour
{
    [SerializeField] CargoBox cargo;

    [Header("Pitch (가속/제동)")]
    [SerializeField] float pitchPerLongG = 4f;
    [SerializeField] float maxPitchDeg = 6f;

    [Header("Roll (코너)")]
    [SerializeField] float rollPerLatG = 5f;
    [SerializeField] float maxRollDeg = 6f;

    [Header("Response")]
    [SerializeField] float lerpSpeed = 6f;

    Quaternion restRot;
    bool captured;

    void Awake()
    {
        if (cargo == null) cargo = GetComponentInParent<CargoBox>();
        restRot = transform.localRotation;
        captured = true;
    }

    void LateUpdate()
    {
        if (cargo == null)
        {
            cargo = GetComponentInParent<CargoBox>();
            if (cargo == null) return;
        }
        if (!captured) { restRot = transform.localRotation; captured = true; }

        float pitch = Mathf.Clamp(-cargo.SignedLongG * pitchPerLongG, -maxPitchDeg, maxPitchDeg);
        float roll = Mathf.Clamp(cargo.SignedLatG * rollPerLatG, -maxRollDeg, maxRollDeg);
        Quaternion target = restRot * Quaternion.Euler(pitch, 0f, roll);
        float t = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, t);
    }
}
