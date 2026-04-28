using UnityEngine;

/// <summary>
/// 신호등 위반 검출 영역 (Day 25, 도현 #3 게임플레이).
///
/// 동작
/// - BoxCollider trigger를 교차로 진입 직전(정지선)에 배치.
/// - 차량(CargoBox 보유 Rigidbody) trigger 진입 시 그 시점의 신호등 state + 속도 평가.
/// - Red + 일정 속도 초과 → 위반: 화물 데미지 + 충격음(있으면).
/// - Yellow + 빠른 진입 → 경고만 (데미지 X) — 신호 변경 직후 통과는 정당.
/// - Green → OK.
///
/// 부착: TrafficLight와 같은 GameObject에 부착 권장 (자동 참조).
///       다른 GameObject 가능 — 인스펙터에서 light 명시.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public class TrafficLightArea : MonoBehaviour
{
    [SerializeField] private TrafficLight trafficLight;
    [Tooltip("이 속도 이상으로 Red 진입 시 위반 (km/h).")]
    [SerializeField] private float minSpeedKmhForViolation = 8f;
    [Tooltip("위반 시 적용할 화물 데미지.")]
    [SerializeField] private float redViolationDamage = 15f;

    BoxCollider trigger;
    float lastViolationTime;

    void Awake()
    {
        trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        if (trafficLight == null) trafficLight = GetComponent<TrafficLight>();
        if (trafficLight == null) trafficLight = GetComponentInParent<TrafficLight>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (trafficLight == null) return;
        if (Time.time - lastViolationTime < 1.0f) return;     // chatter 방지

        var cargo = other.GetComponentInParent<CargoBox>();
        if (cargo == null) return;
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        var state = trafficLight.CurrentState;

        if (state == TrafficLight.State.Red && speedKmh > minSpeedKmhForViolation)
        {
            ApplyViolation(cargo, speedKmh);
            lastViolationTime = Time.time;
        }
    }

    void ApplyViolation(CargoBox cargo, float speedKmh)
    {
        // CargoBox.OnImpact 이벤트 재사용 — IntegrityHUD가 빨강 flash 처리.
        // 데미지는 직접 적용 (private 메서드 X — public Repair만 있어 SendMessage 사용)
        // 가장 간단: ApplyDamage가 private이라 public 통로 추가 필요. 임시: OnImpact 발동 + 큰 충돌처럼 보이도록 하기.
        // 깔끔: CargoBox.ApplyExternalDamage public API 추가. 여기선 우선 reflection 회피 — 큰 가짜 충돌로 처리.
        // → CargoBox에 ApplyExternalDamage public 추가하는 것이 정공. 그것을 다음 commit에 같이.
        SendMessageUpwards("OnTrafficViolation", speedKmh, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"[TrafficLight] RED VIOLATION at {speedKmh:F0} km/h — cargo damage {redViolationDamage}.");
        cargo.ApplyExternalDamage(redViolationDamage);
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<BoxCollider>();
        if (c == null) return;
        Gizmos.color = trafficLight != null && trafficLight.CurrentState == TrafficLight.State.Red
            ? new Color(1f, 0.2f, 0.2f, 0.35f)
            : new Color(0.2f, 0.7f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(c.center, c.size);
    }
}
