using UnityEngine;

/// <summary>
/// 과제 1827: 언덕 정점 같은 "보여주는 자리"에서만 카메라가 시선을 살짝 내린다(lookHeight − drop).
/// 차가 반경 안(수평 거리)에 들어오면 drop 이 서서히 걸리고, 나가면 풀린다. FollowCamera 가 Drop 을 읽는다.
/// 근거: 원화 프리셋은 시선 1.0m 라 수평선이 화면 위에서 약 42% — 정점에서 바다를 화면 위 1/3 에 두려면 시선 약 0.9m 를 더 내려야 한다.
/// </summary>
public class CameraFramingZone : MonoBehaviour
{
    public float radius = 45f;
    public float drop = 0.9f;
    [Tooltip("drop 이 걸리고 풀리는 속도(m/s). 언덕 꼭대기처럼 drop 이 크면 기본 0.6 으로는 다 숙이기 전에 지나간다.")]
    public float rate = 0.6f;
    public static float Drop { get; private set; }
    static float target, targetRate = 0.6f;
    static int lastFrame = -1;
    Transform car;

    void Update()
    {
        if (car == null) { var c = FindFirstObjectByType<ArcadeCarController>(); if (c == null) return; car = c.transform; }
        var d = car.position - transform.position; d.y = 0f;
        float k = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(radius * 0.6f, radius, d.magnitude));
        if (lastFrame != Time.frameCount) { lastFrame = Time.frameCount; target = 0f; targetRate = rate; }
        if (k * drop >= target) { target = k * drop; targetRate = rate; }
        Drop = Mathf.MoveTowards(Drop, target, Time.deltaTime * targetRate);
    }

    void OnDisable() { Drop = 0f; target = 0f; }
}
