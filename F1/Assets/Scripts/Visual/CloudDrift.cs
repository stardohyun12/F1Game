using UnityEngine;

/// <summary>
/// 과제 1827: 천천히 흐르는 구름. 자식(구름 덩어리)을 바람 방향으로 옮기고 범위를 벗어나면 반대편으로 되돌린다.
/// 같은 이동량을 셰이더 전역 _CloudOffset 으로 넣어 WindGrass 의 구름 그림자가 같이 흐른다(그림자는 노이즈라 구름 모양과 1:1 은 아님 — "가볍게").
/// </summary>
[ExecuteAlways]
public class CloudDrift : MonoBehaviour
{
    public Vector3 wind = new Vector3(0.8f, 0f, -0.6f);
    public float speed = 3f;            // m/s — 시속 11km, 눈으로 겨우 보이는 정도
    public Vector3 areaCenter;
    public float areaHalf = 900f;
    static readonly int CloudOffsetId = Shader.PropertyToID("_CloudOffset");

    void Update()
    {
        var d = wind.normalized;
        float t = Application.isPlaying ? Time.time : (float)UnityEditor_Time();
        Shader.SetGlobalVector(CloudOffsetId, new Vector4(-d.x * speed * t, -d.z * speed * t, 0, 0));
        if (!Application.isPlaying) return;
        var step = d * speed * Time.deltaTime;
        foreach (Transform c in transform)
        {
            var p = c.position + step;
            var rel = p - areaCenter;
            if (rel.x > areaHalf) p.x -= areaHalf * 2; else if (rel.x < -areaHalf) p.x += areaHalf * 2;
            if (rel.z > areaHalf) p.z -= areaHalf * 2; else if (rel.z < -areaHalf) p.z += areaHalf * 2;
            c.position = p;
        }
    }

    static double UnityEditor_Time()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorApplication.timeSinceStartup;
#else
        return Time.time;
#endif
    }
}
