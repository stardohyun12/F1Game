using UnityEngine;

/// <summary>
/// 과제 20261102-1951: 방 침대 [E] 자리. 침대 메시(BED_01)는 건드리지 않고, 에디트 모드 설치 스크립트가 잰 월드 경계 상자로만 시선을 잡는다
/// (충돌체를 새로 달면 방 안 걷기가 막힐 수 있어 레이-상자 검사 — 앞에 벽·소품이 있으면 못 잡는다).
/// 설치: RoomDaySetup(PitCrew/기능/하루 끝 일기장·방 달력 설치).
/// </summary>
public class RoomBed : MonoBehaviour
{
    [Tooltip("침대 월드 경계 상자(설치 스크립트가 BED_01 렌더러에서 잰다).")]
    [SerializeField] Vector3 center;
    [SerializeField] Vector3 size = new Vector3(1f, 0.6f, 2f);

    public Vector3 Center => center;
    /// <summary>경계 상자(Gaze 부채꼴 E 가 가장자리까지 거리를 잰다 — D-306).</summary>
    public Bounds Box => new Bounds(center, size);
    /// <summary>[E] 칩 자리 = 침대 윗면 가운데 조금 위.</summary>
    public Vector3 Anchor => center + Vector3.up * (size.y * 0.5f + 0.12f);

    public void Set(Vector3 c, Vector3 s) { center = c; size = s; }

    /// <summary>시선 레이가 침대 상자에 maxDist 안에서 닿나. 눈이 상자 안이면 아니다.</summary>
    public bool Hit(Ray ray, float maxDist, out float dist)
    {
        var b = new Bounds(center, size);
        dist = 0f;
        if (b.Contains(ray.origin)) return false;
        if (!b.IntersectRay(ray, out dist)) return false;
        return dist <= maxDist;
    }
}
