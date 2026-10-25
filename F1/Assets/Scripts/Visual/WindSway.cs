using UnityEngine;

/// <summary>
/// 과제 0131(갭 #1): 차양·파라솔 같은 천 소품을 바람에 살짝 흔든다(원점 기준 회전, 느린 두 겹 사인 + 돌풍).
/// 셰이더 정점 흔들림이 생기면 그걸로 바꾼다 — 지금은 통짜 메시라 물체째 흔든다.
/// </summary>
public class WindSway : MonoBehaviour
{
    public Vector3 axis = Vector3.right;   // 로컬 축
    public float degrees = 2.5f, speed = 1.1f;
    Quaternion rest;
    float phase;

    void Start() { rest = transform.localRotation; phase = (transform.position.x * 0.37f + transform.position.z * 0.21f) % 6.28f; }

    void Update()
    {
        float t = Time.time * speed + phase;
        float a = Mathf.Sin(t) * 0.7f + Mathf.Sin(t * 2.3f + 1.1f) * 0.3f;
        a *= 0.75f + 0.25f * Mathf.Sin(Time.time * 0.23f + phase);   // 돌풍: 10~30초 주기로 세기 오르내림
        transform.localRotation = rest * Quaternion.AngleAxis(a * degrees, axis);
    }
}
