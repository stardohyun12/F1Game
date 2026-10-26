using UnityEngine;

// 2124(D-150 톤 구간): 바람에 흔들리는 것 묶음 — 풀 포기·빨래·손 흔드는 팔. 자식마다 따로 Update 하지 않고 부모 하나가 직접 자식을 돌린다.
// 자식 피벗(풀=밑동, 빨래=윗변, 팔=어깨)을 축으로 바람 방향(로컬 X축) 회전. 위상은 자식 위치로 → 바람이 물결처럼 지나간다.
// 진폭은 작게: 저해상도에서 큰 진폭은 지글거림(D-149)이 된다.
public class ToneSway : MonoBehaviour
{
    public float amplitude = 5f;      // 도
    public float frequency = 0.6f;    // Hz
    public float waveLength = 14f;    // m, 바람 물결 길이
    public Vector3 axis = Vector3.forward;
    public Vector3 windDir = new Vector3(1f, 0f, 0.4f);

    Transform[] kids; Quaternion[] rest; float[] phase;

    void Awake()
    {
        int n = transform.childCount;
        kids = new Transform[n]; rest = new Quaternion[n]; phase = new float[n];
        var w = windDir.normalized;
        for (int i = 0; i < n; i++)
        {
            kids[i] = transform.GetChild(i); rest[i] = kids[i].localRotation;
            phase[i] = Vector3.Dot(kids[i].position, w) / Mathf.Max(1f, waveLength) * Mathf.PI * 2f + (i * 0.37f % 1f);
        }
    }

    void Update()
    {
        float t = Time.time * frequency * Mathf.PI * 2f;
        for (int i = 0; i < kids.Length; i++)
        {
            if (kids[i] == null) continue;
            float s = Mathf.Sin(t - phase[i]) * 0.7f + Mathf.Sin(t * 0.43f - phase[i] * 0.5f) * 0.3f;   // 고르지 않은 바람
            kids[i].localRotation = rest[i] * Quaternion.AngleAxis(s * amplitude, axis);
        }
    }
}
