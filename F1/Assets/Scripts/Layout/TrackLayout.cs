using System.Collections.Generic;
using UnityEngine;

// Whole-track layout data: road mask, building mask, delivery cells, start cell.
// mask[0] corresponds to grid row 0 (bottom). Add rows bottom-to-top in the inspector.
[CreateAssetMenu(menuName = "PitCrew/Track Layout", fileName = "TrackLayout")]
public class TrackLayout : ScriptableObject
{
    [Tooltip("Road mask rows (bottom-to-top). '#' = road, '.' = empty.")]
    public string[] roadMask;

    [Tooltip("Building mask rows (bottom-to-top). '.' = empty. Any other character is a BuildingCatalog entry key.")]
    public string[] buildingMask;

    [Tooltip("Height mask rows (bottom-to-top). '.' or null = 평지(0). " +
             "'0'~'9' = 0~9 단계 높이. 단계당 cellHeightMeters 만큼 Y 상승. " +
             "Day 16 도입 (Phase 1 Week 3). null이면 전 셀 평지로 간주.")]
    public string[] heightMask;

    [Tooltip("heightMask 1단계당 Y 상승 (m). 기본 1.5m = Cell 6m × 0.25 경사.")]
    public float cellHeightMeters = 1.5f;

    public BuildingCatalog catalog;

    [System.Serializable]
    public class DeliveryCell
    {
        public int col;
        public int row;
        public string label = "Delivery";
    }

    public List<DeliveryCell> deliveries = new();

    public Vector2Int startCell = Vector2Int.zero;
    public float startYawDeg = 90f;

    public int Width => (roadMask != null && roadMask.Length > 0) ? roadMask[0].Length : 0;
    public int Height => roadMask != null ? roadMask.Length : 0;

    public bool IsRoad(int c, int r)
    {
        if (roadMask == null || r < 0 || r >= roadMask.Length) return false;
        if (c < 0 || c >= roadMask[r].Length) return false;
        return roadMask[r][c] == '#';
    }

    public char BuildingChar(int c, int r)
    {
        if (buildingMask == null || r < 0 || r >= buildingMask.Length) return '.';
        if (c < 0 || c >= buildingMask[r].Length) return '.';
        return buildingMask[r][c];
    }

    /// <summary>
    /// Height mask 셀 높이 조회 (m). Day 16 도입.
    /// '.' or out-of-range → 0. '0'~'9' → tier × cellHeightMeters.
    /// TileMapAuto에서 Y offset 적용 시 사용.
    /// </summary>
    public float GetHeight(int c, int r)
    {
        if (heightMask == null || r < 0 || r >= heightMask.Length) return 0f;
        if (c < 0 || c >= heightMask[r].Length) return 0f;
        char ch = heightMask[r][c];
        if (ch >= '0' && ch <= '9') return (ch - '0') * cellHeightMeters;
        return 0f;
    }

    /// <summary>인접 셀과의 높이차 → 이 엣지의 경사 각도(deg). Day 18 graph 빌더용.</summary>
    public float SlopeDegBetween(int c1, int r1, int c2, int r2, float cellSize = 6f)
    {
        float h1 = GetHeight(c1, r1);
        float h2 = GetHeight(c2, r2);
        float horizontalDist = Mathf.Sqrt((c2 - c1) * (c2 - c1) + (r2 - r1) * (r2 - r1)) * cellSize;
        if (horizontalDist <= 0.01f) return 0f;
        return Mathf.Atan2(h2 - h1, horizontalDist) * Mathf.Rad2Deg;
    }
}
