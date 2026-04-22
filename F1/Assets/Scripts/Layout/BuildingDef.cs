using UnityEngine;

// Metadata for a single building prefab so it can be placed on the 1x1 grid.
[CreateAssetMenu(menuName = "PitCrew/Building Def", fileName = "BuildingDef")]
public class BuildingDef : ScriptableObject
{
    public GameObject prefab;

    [Tooltip("Max(x,z) of the prefab's mesh bounds at scale 1. Used to scale the instance to occupy one cell.")]
    public float nativeSize = 1f;

    [Tooltip("Prefab local direction that should face the adjacent road. Default +Z (forward). If the building's entry naturally faces -X, set this to (-1,0,0).")]
    public Vector3 entryLocalDir = new Vector3(0f, 0f, 1f);

    [Tooltip("Footprint in grid cells (west-east). Multi-cell buildings occupy cellsWide x cellsDeep cells starting at the anchor cell.")]
    public int cellsWide = 1;
    [Tooltip("Footprint in grid cells (south-north).")]
    public int cellsDeep = 1;

    public string category = "suburban";
}
