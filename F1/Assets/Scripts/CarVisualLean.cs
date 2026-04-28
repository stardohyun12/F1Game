using UnityEngine;

/// <summary>
/// 차체 시각 기울기 (세션 14 리서치 TOP 3).
/// benmcinnes/ArcadeVehiclePhysics 패턴 차용 — 물리 Rigidbody 회전 무손상.
///
/// Race 루트에 부착. 자식(body + wheel-*) 각각의 localRotation을 개별 Lerp.
/// Nested prefab(Kenney car-kit) 구조 변경 없이 작동 — Reparent 불필요.
///
/// CargoBox의 SignedLongG/SignedLatG 재사용 (이미 측정됨).
/// - 가속(+SignedLongG) → 박스가 뒤로 기움 = 차 앞쪽이 뜸(-pitch)
/// - 제동(-SignedLongG) → 차 앞쪽이 내려감(+pitch)
/// - 우회전(+SignedLatG) → roll 양수 (왼쪽 기움)
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

    static readonly string[] VisualNames = {
        "body",
        "wheel-back-left",
        "wheel-back-right",
        "wheel-front-left",
        "wheel-front-right",
    };

    struct PartRef { public Transform t; public Quaternion restRot; }
    PartRef[] parts;

    void Awake()
    {
        if (cargo == null) cargo = GetComponent<CargoBox>();
        BuildParts();
    }

    void BuildParts()
    {
        parts = new PartRef[VisualNames.Length];
        for (int i = 0; i < VisualNames.Length; i++)
        {
            var t = transform.Find(VisualNames[i]);
            parts[i] = new PartRef
            {
                t = t,
                restRot = t != null ? t.localRotation : Quaternion.identity,
            };
        }
    }

    void LateUpdate()
    {
        // 도현 #2-13: NRE 방어 — parts null이면 lazy init.
        // Awake 못 돌았거나 Apply 메뉴로 GameObject 동적 변경됐을 때 안전.
        if (parts == null) BuildParts();

        if (cargo == null)
        {
            cargo = GetComponent<CargoBox>();
            if (cargo == null) return;
        }

        float pitch = Mathf.Clamp(-cargo.SignedLongG * pitchPerLongG, -maxPitchDeg, maxPitchDeg);
        float roll = Mathf.Clamp(cargo.SignedLatG * rollPerLatG, -maxRollDeg, maxRollDeg);
        Quaternion delta = Quaternion.Euler(pitch, 0f, roll);
        float lerpT = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].t == null) continue;
            Quaternion target = parts[i].restRot * delta;
            parts[i].t.localRotation = Quaternion.Slerp(parts[i].t.localRotation, target, lerpT);
        }
    }
}
