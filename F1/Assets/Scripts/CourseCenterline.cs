using UnityEngine;

/// <summary>
/// 코스 중심선 점(빌더가 구움). 추락 리스폰이 "마지막 안전 지점" 대신 그 근처 길 한가운데로 가게 한다.
/// 씬에 없으면 ArcadeCarController 는 옛 동작(안전 지점 그대로).
/// </summary>
public class CourseCenterline : MonoBehaviour
{
    public static CourseCenterline Instance { get; private set; }
    [SerializeField] public Vector3[] points = new Vector3[0];   // 본선 + 우회로, 2m 간격
    [SerializeField] public Vector3[] forwards = new Vector3[0];

    void OnEnable() { Instance = this; }
    void OnDisable() { if (Instance == this) Instance = null; }

    /// <summary>near 에 가장 가까운 중심선 점(3D 거리 — 헤어핀 위·아래 다리를 헷갈리지 않게).</summary>
    public bool Nearest(Vector3 near, out Vector3 pos, out Vector3 fwd)
    {
        pos = near; fwd = Vector3.forward;
        if (points == null || points.Length == 0) return false;
        int best = 0; float bd = float.MaxValue;
        for (int i = 0; i < points.Length; i++) { float d = (points[i] - near).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
        pos = points[best]; fwd = forwards[best];
        return true;
    }
}
