using UnityEngine;

// 2203(D-150 톤 구간): 길가를 천천히 오가는 사람. 시작점 기준 dir 방향 ±span/2 를 speed m/s 로 왕복, 걸음마다 살짝 위아래.
// 사람 박스 루트에 붙인다(BuildToneSlice). 땅 높이는 시작점 그대로(구간이 짧고 평평한 길가라 가정).
public class ToneWalker : MonoBehaviour
{
    public Vector3 dir = Vector3.forward;
    public float span = 6f, speed = 1.1f, bob = .04f;
    Vector3 start; bool init;

    void OnEnable() { if (!init) { start = transform.position; init = true; } }

    void Update()
    {
        float len = Mathf.Max(span, .1f), t = Time.time * speed;
        float u = Mathf.PingPong(t, len);
        bool back = Mathf.Repeat(t, len * 2f) > len;
        var d = dir; d.y = 0; d.Normalize();
        transform.position = start + d * (u - len * .5f) + Vector3.up * Mathf.Abs(Mathf.Sin(t * 5.5f)) * bob;
        transform.rotation = Quaternion.LookRotation(back ? -d : d);
    }
}
