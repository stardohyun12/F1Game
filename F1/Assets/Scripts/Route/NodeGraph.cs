using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경로 계획용 노드 타입. Day 18 (Phase 1 Week 3).
/// </summary>
public enum GraphNodeType
{
    /// <summary>주인공 출발 지점 (허브에서 나오는 곳).</summary>
    Start,
    /// <summary>배달지.</summary>
    Delivery,
    /// <summary>피트스탑(주유, 화물 정비, 타이어 교체).</summary>
    Pit,
    /// <summary>교차점/분기. 경로 계획 시 분기점으로 표시.</summary>
    Junction,
    /// <summary>특수 (이벤트 위치, 광장 등).</summary>
    Special,
}

[Serializable]
public class GraphNode
{
    [Tooltip("고유 ID. 'D1', 'P1', 'J3' 등 짧은 식별자.")]
    public string id;

    [Tooltip("월드 좌표. TrackLayout의 cell 좌표를 변환한 값.")]
    public Vector3 worldPos;

    [Tooltip("그리드 좌표 (col, row). TrackLayout 참조용.")]
    public Vector2Int gridCell;

    public GraphNodeType type = GraphNodeType.Junction;

    [Tooltip("UI 표시 라벨. '식당 A', '꽃집' 등.")]
    public string displayName;
}

[Serializable]
public class GraphEdge
{
    [Tooltip("시작 노드 ID.")]
    public string fromId;

    [Tooltip("끝 노드 ID.")]
    public string toId;

    [Tooltip("엣지 거리 (m). TileMap 셀 거리 합산.")]
    public float distanceM;

    [Tooltip("평균 통과 속도 (km/h). 코너 많은 곳은 낮음.")]
    public float avgSpeedKmh = 50f;

    [Tooltip("경사각 (deg). 양수=업힐, 음수=다운힐. TrackLayout.SlopeDegBetween로 계산.")]
    public float slopeDeg;

    [Tooltip("코너 수 (대략). 경로 표시용.")]
    public int cornerCount;

    [Tooltip("지름길 표식 (정보 가치 있음). 업그레이드로 해제 가능.")]
    public bool isShortcut;

    /// <summary>예상 통과 시간(s) = distanceM / (avgSpeedKmh / 3.6).</summary>
    public float EstimatedTimeSec => avgSpeedKmh > 0.1f ? distanceM / (avgSpeedKmh / 3.6f) : 0f;
}

/// <summary>
/// 맵 노드-엣지 그래프 (Day 18, Phase 1 Week 3).
/// 경로 계획 UI(Day 19)의 데이터 소스. TrackLayout과 짝지어 사용.
///
/// 빌드 흐름:
/// 1. TileMapAuto 빌드 후, JunctionFinder 같은 Editor 도구로 자동 추출 (향후)
/// 2. 일단 MVP는 수동 ScriptableObject 작성 (도현이 인스펙터에서)
/// 3. Day 19 RoutePlannerPanel에서 nodes/edges 그려주고 드래그 순서 결정
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Node Graph", fileName = "NodeGraph")]
public class NodeGraph : ScriptableObject
{
    [Tooltip("어떤 트랙의 그래프인지 (참조).")]
    public TrackLayout track;

    public List<GraphNode> nodes = new List<GraphNode>();
    public List<GraphEdge> edges = new List<GraphEdge>();

    /// <summary>ID로 노드 찾기. null 가능.</summary>
    public GraphNode FindNode(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var n in nodes) if (n != null && n.id == id) return n;
        return null;
    }

    /// <summary>두 노드 사이 엣지 찾기 (방향 무관). null 가능.</summary>
    public GraphEdge FindEdge(string idA, string idB)
    {
        foreach (var e in edges)
        {
            if (e == null) continue;
            if ((e.fromId == idA && e.toId == idB) ||
                (e.fromId == idB && e.toId == idA))
                return e;
        }
        return null;
    }

    /// <summary>노드 시퀀스의 총 거리. 미연결이면 -1 반환.</summary>
    public float TotalDistance(IList<string> sequence)
    {
        if (sequence == null || sequence.Count < 2) return 0f;
        float sum = 0f;
        for (int i = 0; i < sequence.Count - 1; i++)
        {
            var e = FindEdge(sequence[i], sequence[i + 1]);
            if (e == null) return -1f;
            sum += e.distanceM;
        }
        return sum;
    }

    /// <summary>노드 시퀀스의 총 예상 시간 (s). 미연결이면 -1.</summary>
    public float TotalEstimatedTime(IList<string> sequence)
    {
        if (sequence == null || sequence.Count < 2) return 0f;
        float sum = 0f;
        for (int i = 0; i < sequence.Count - 1; i++)
        {
            var e = FindEdge(sequence[i], sequence[i + 1]);
            if (e == null) return -1f;
            sum += e.EstimatedTimeSec;
        }
        return sum;
    }
}
