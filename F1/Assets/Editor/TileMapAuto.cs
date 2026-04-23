using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Grid-mask-driven track builder.
// User defines a 2D mask (rows of strings) where '#' = road and '.' = empty.
// For each road cell, neighbors are inspected; tile kind + rotation are deduced from
// validated metadata (TileMetadata constants below). No manual rotation per cell.
//
// Coordinate convention: rows go bottom-to-top in the mask the same way they appear here
// (mask[0] is row 0 = bottom). Cell (col, row) -> world (col*CellSize, TileY, row*CellSize).
public static class TileMapAuto
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";
    const float CellSize = 6f;
    const float TileScale = 6f;
    const float TileY = 0.05f;

    // ----- Metadata (validated empirically via top-down capture, 2026-04-22) -----
    // Each tile rotation maps to the world-direction set of its road exits.
    // Direction bitfield: N=+Z, S=-Z, E=+X, W=-X.
    [System.Flags]
    public enum Dir { None = 0, N = 1, S = 2, E = 4, W = 8 }

    public enum Kind { Straight, Curve, Intersection, Crossroad, End }

    // Lookup: given (kind, exit-set), what rotation places the tile correctly?
    // Built from validated rotation->exits mappings.
    static readonly Dictionary<(Kind, Dir), int> RotationFor = new()
    {
        // Straight: two opposite exits.
        { (Kind.Straight, Dir.E | Dir.W), 0 },   // road axis along X
        { (Kind.Straight, Dir.N | Dir.S), 90 },  // road axis along Z

        // Curve: two perpendicular exits. Validated mapping:
        //   rot 0   -> +X, +Z (NE)
        //   rot 90  -> +X, -Z (SE)
        //   rot 180 -> -X, -Z (SW)
        //   rot 270 -> -X, +Z (NW)
        { (Kind.Curve, Dir.E | Dir.N), 0 },
        { (Kind.Curve, Dir.E | Dir.S), 90 },
        { (Kind.Curve, Dir.W | Dir.S), 180 },
        { (Kind.Curve, Dir.W | Dir.N), 270 },

        // Crossroad: 4 exits, rotation irrelevant.
        { (Kind.Crossroad, Dir.N | Dir.S | Dir.E | Dir.W), 0 },

        // Intersection (T): 3 exits. Verified via _Recovery/MeasureIntersectionRotations.cs (session 11).
        // Top-down capture of 4 yaws side-by-side: the thick sidewalk "missing" leg lands at
        //   rot 0   → missing -Z (South)
        //   rot 90  → missing -X (West)
        //   rot 180 → missing +Z (North)
        //   rot 270 → missing +X (East)
        { (Kind.Intersection, Dir.N | Dir.E | Dir.W), 0 },     // missing S
        { (Kind.Intersection, Dir.N | Dir.S | Dir.E), 90 },    // missing W
        { (Kind.Intersection, Dir.S | Dir.E | Dir.W), 180 },   // missing N
        { (Kind.Intersection, Dir.N | Dir.S | Dir.W), 270 },   // missing E

        // End (dead-end): 1 exit. Tile points outward in -X by default.
        { (Kind.End, Dir.E), 0 },
        { (Kind.End, Dir.S), 90 },
        { (Kind.End, Dir.W), 180 },
        { (Kind.End, Dir.N), 270 },
    };

    static readonly Dictionary<Kind, string> SourcePath = new()
    {
        { Kind.Straight, "road-straight.fbx" },
        { Kind.Curve, "road-bend.fbx" },
        { Kind.Intersection, "road-intersection.fbx" },
        { Kind.Crossroad, "road-crossroad.fbx" },
        { Kind.End, "road-end.fbx" },
    };

    // Per-tile native footprint in mesh units (measured via MeasureTiles).
    // Used to compute scale so each tile occupies exactly one CellSize unit.
    static readonly Dictionary<Kind, float> NativeSize = new()
    {
        { Kind.Straight, 1f },
        { Kind.Curve, 1f },          // road-bend is 1x1 (matches straight road width)
        { Kind.Intersection, 1f },
        { Kind.Crossroad, 1f },
        { Kind.End, 1f },
    };

    // ----- Layout (rectangular loop, 7 wide x 5 tall) -----
    // Bottom row first. '#' = road, '.' = empty.
    public static readonly string[] DefaultMask =
    {
        "#######", // row 0
        "#.....#", // row 1
        "#.....#", // row 2
        "#.....#", // row 3
        "#######", // row 4
    };

    // Delivery + start cells (independent of mask, must lie on a road cell).
    public static readonly List<(int col, int row, string label)> DeliveryCells = new()
    {
        (3, 0, "Delivery_1"),
        (6, 2, "Delivery_2"),
        (0, 3, "Delivery_3"),
    };
    public static readonly Vector2Int StartCell = new Vector2Int(1, 0);
    public static readonly float StartYawDeg = 90f;

    [MenuItem("PitCrew/Build Auto Tilemap Track")]
    public static void Build()
    {
        BuildMask(DefaultMask);
    }

    // Builds the road portion from a TrackLayout SO. Does not place buildings (see BuildingPlacer).
    public static GameObject BuildFromLayout(TrackLayout layout)
    {
        if (layout == null || layout.roadMask == null || layout.roadMask.Length == 0)
        { Debug.LogError("TileMapAuto.BuildFromLayout: layout/roadMask is empty"); return null; }
        return BuildMaskInternal(layout.roadMask);
    }

    public static void BuildMask(string[] mask)
    {
        BuildMaskInternal(mask);
    }

    static GameObject BuildMaskInternal(string[] mask)
    {
        var sources = new Dictionary<Kind, GameObject>();
        foreach (var kv in SourcePath)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(Root + kv.Value);
            if (go == null) Debug.LogWarning($"TileMapAuto: missing {kv.Value}");
            sources[kv.Key] = go;
        }

        int height = mask.Length;
        int width = mask[0].Length;
        // Validate uniform width
        for (int r = 0; r < height; r++)
            if (mask[r].Length != width)
            { Debug.LogError($"TileMapAuto: row {r} width {mask[r].Length} != expected {width}"); return null; }

        bool IsRoad(int c, int r)
        {
            if (c < 0 || c >= width || r < 0 || r >= height) return false;
            return mask[r][c] == '#';
        }

        Dir NeighborExits(int c, int r)
        {
            if (!IsRoad(c, r)) return Dir.None;
            Dir d = Dir.None;
            if (IsRoad(c, r + 1)) d |= Dir.N;
            if (IsRoad(c, r - 1)) d |= Dir.S;
            if (IsRoad(c + 1, r)) d |= Dir.E;
            if (IsRoad(c - 1, r)) d |= Dir.W;
            return d;
        }

        // road-straight.fbx has a crosswalk only on its +X end (native). Rotating the tile
        // by 180° moves the crosswalk to the opposite side. For smoother T/curve boundaries,
        // we want the crosswalk to meet another straight (which merges into a clean edge)
        // rather than a curve/intersection. Return the adjusted rotation.
        int AdjustStraightRotation(int c, int r, Dir exits, int baseRot)
        {
            if (exits == (Dir.E | Dir.W))
            {
                // rot 0 => crosswalk on east. rot 180 => crosswalk on west.
                bool eastStraight = NeighborExits(c + 1, r) == (Dir.E | Dir.W);
                bool westStraight = NeighborExits(c - 1, r) == (Dir.E | Dir.W);
                if (!eastStraight && westStraight) return 180; // push crosswalk to straight side (west)
                return 0;
            }
            if (exits == (Dir.N | Dir.S))
            {
                // rot 90 => crosswalk on north (+Z). rot 270 => crosswalk on south.
                bool northStraight = NeighborExits(c, r + 1) == (Dir.N | Dir.S);
                bool southStraight = NeighborExits(c, r - 1) == (Dir.N | Dir.S);
                if (!northStraight && southStraight) return 270;
                return 90;
            }
            return baseRot;
        }

        var existing = GameObject.Find("Track");
        if (existing != null) Object.DestroyImmediate(existing);
        var track = new GameObject("Track");
        Undo.RegisterCreatedObjectUndo(track, "Build Auto Tilemap Track");

        int placed = 0;
        for (int r = 0; r < height; r++)
        {
            for (int c = 0; c < width; c++)
            {
                if (!IsRoad(c, r)) continue;
                Dir exits = Dir.None;
                if (IsRoad(c, r + 1)) exits |= Dir.N;
                if (IsRoad(c, r - 1)) exits |= Dir.S;
                if (IsRoad(c + 1, r)) exits |= Dir.E;
                if (IsRoad(c - 1, r)) exits |= Dir.W;

                int count = PopCount((int)exits);
                Kind kind;
                switch (count)
                {
                    case 4: kind = Kind.Crossroad; break;
                    case 3: kind = Kind.Intersection; break;
                    case 2:
                        bool opposite = (exits == (Dir.N | Dir.S)) || (exits == (Dir.E | Dir.W));
                        kind = opposite ? Kind.Straight : Kind.Curve;
                        break;
                    case 1: kind = Kind.End; break;
                    default:
                        Debug.LogWarning($"TileMapAuto: isolated road cell at ({c},{r})");
                        continue;
                }

                if (!RotationFor.TryGetValue((kind, exits), out int rot))
                {
                    Debug.LogWarning($"TileMapAuto: no rotation for ({kind}, {exits}) at ({c},{r})");
                    continue;
                }
                if (kind == Kind.Straight) rot = AdjustStraightRotation(c, r, exits, rot);

                if (!sources.TryGetValue(kind, out var src) || src == null)
                {
                    Debug.LogWarning($"TileMapAuto: no source for {kind}");
                    continue;
                }

                var pos = new Vector3(c * CellSize, TileY, r * CellSize);
                var rotQ = Quaternion.Euler(0, rot, 0);
                float nativeSize = NativeSize.TryGetValue(kind, out var ns) ? ns : 1f;
                float scale = TileScale / nativeSize;
                Place(src, track, $"Tile_{kind}_{c}_{r}_r{rot}", pos, rotQ, scale);
                placed++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"TileMapAuto: placed {placed} tiles from mask {width}x{height}.");
        return track;
    }

    public static Vector3 CellToWorld(int col, int row)
        => new Vector3(col * CellSize, TileY, row * CellSize);

    static int PopCount(int x)
    {
        int n = 0;
        while (x != 0) { n += x & 1; x >>= 1; }
        return n;
    }

    static void Place(GameObject source, GameObject parent, string name, Vector3 pos, Quaternion rot, float scale)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
        go.name = name;
        go.transform.SetParent(parent.transform, worldPositionStays: false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = Vector3.one * scale;
    }
}
