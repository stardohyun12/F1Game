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
}
