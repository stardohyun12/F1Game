using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TileMapTrack
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";
    const float CellSize = 4f;
    const float TileScale = 4f;
    const float TileY = 0.05f;

    public enum Kind { Straight, Curve, Crossroad, Intersection, End }

    public struct Cell
    {
        public int col, row;
        public Kind kind;
        public int rot;
        public Cell(int col, int row, Kind kind, int rot) { this.col = col; this.row = row; this.kind = kind; this.rot = rot; }
    }

    // Rectangular loop. Rotation rules (validated session 5: tile road axis = local +X):
    //  - Straight along world +X: rot 0
    //  - Straight along world +Z: rot 90
    //  - Curve corners (validated empirically via top-down close-up):
    //      rot 0   -> exits (+X, +Z) (bottom-left corner of loop)
    //      rot 90  -> exits (+X, -Z) (top-left corner)
    //      rot 180 -> exits (-X, -Z) (top-right corner)
    //      rot 270 -> exits (-X, +Z) (bottom-right corner)
    static readonly List<Cell> Layout = new List<Cell>
    {
        // bottom row (col 0..6, row 0)
        new Cell(0, 0, Kind.Curve,    0),
        new Cell(1, 0, Kind.Straight, 0),
        new Cell(2, 0, Kind.Straight, 0),
        new Cell(3, 0, Kind.Straight, 0),
        new Cell(4, 0, Kind.Straight, 0),
        new Cell(5, 0, Kind.Straight, 0),
        new Cell(6, 0, Kind.Curve,    270),

        // right column (col 6, row 1..3)
        new Cell(6, 1, Kind.Straight, 90),
        new Cell(6, 2, Kind.Straight, 90),
        new Cell(6, 3, Kind.Straight, 90),

        // top row (col 6..0, row 4)
        new Cell(6, 4, Kind.Curve,    180),
        new Cell(5, 4, Kind.Straight, 0),
        new Cell(4, 4, Kind.Straight, 0),
        new Cell(3, 4, Kind.Straight, 0),
        new Cell(2, 4, Kind.Straight, 0),
        new Cell(1, 4, Kind.Straight, 0),
        new Cell(0, 4, Kind.Curve,    90),

        // left column (col 0, row 1..3)
        new Cell(0, 1, Kind.Straight, 90),
        new Cell(0, 2, Kind.Straight, 90),
        new Cell(0, 3, Kind.Straight, 90),
    };

    // Delivery zones: (col, row, label). Placed centered on a tile, used as trigger anchors.
    public static readonly List<(int col, int row, string label)> DeliveryCells = new()
    {
        (3, 0, "Delivery_1"),
        (6, 2, "Delivery_2"),
        (0, 3, "Delivery_3"),
    };

    // Start position: bottom-left curve cell, facing +X (along bottom row).
    public static readonly Vector3 StartCell = new Vector3(1, 0, 0);
    public static readonly float StartYawDeg = 90f; // car's forward (+Z by default) rotated to face +X

    [MenuItem("PitCrew/Build Tilemap Track")]
    public static void Build()
    {
        var straight = Load("road-straight.fbx");
        var curve = Load("road-curve.fbx");
        var crossroad = Load("road-crossroad.fbx");
        var intersection = Load("road-intersection.fbx");
        var end = Load("road-end.fbx");
        if (straight == null || curve == null) { Debug.LogError("TileMapTrack: missing core asset(s)."); return; }

        var existing = GameObject.Find("Track");
        if (existing != null) Object.DestroyImmediate(existing);
        var track = new GameObject("Track");
        Undo.RegisterCreatedObjectUndo(track, "Build Tilemap Track");

        foreach (var c in Layout)
        {
            GameObject src = c.kind switch
            {
                Kind.Straight => straight,
                Kind.Curve => curve,
                Kind.Crossroad => crossroad,
                Kind.Intersection => intersection,
                Kind.End => end,
                _ => straight
            };
            if (src == null) continue;

            var pos = CellToWorld(c.col, c.row);
            var rot = Quaternion.Euler(0, c.rot, 0);
            Place(src, track, $"Tile_{c.kind}_{c.col}_{c.row}", pos, rot);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"TileMapTrack: placed {Layout.Count} tiles.");
    }

    public static Vector3 CellToWorld(float col, float row)
    {
        return new Vector3(col * CellSize, TileY, row * CellSize);
    }

    static GameObject Load(string filename)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(Root + filename);
    }

    static void Place(GameObject source, GameObject parent, string name, Vector3 pos, Quaternion rot)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
        go.name = name;
        go.transform.SetParent(parent.transform, worldPositionStays: false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = Vector3.one * TileScale;
    }
}
