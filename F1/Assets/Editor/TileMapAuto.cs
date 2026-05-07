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

    public enum Kind { Straight, Curve, Intersection, Crossroad, End, Crossing, Driveway, Bridge, Slant, SlantFlat, SlantHigh, SlantFlatHigh }

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

        // Crossing (pedestrian crossing): same exits as Straight. Rotation reuses Straight mapping.
        { (Kind.Crossing, Dir.E | Dir.W), 0 },
        { (Kind.Crossing, Dir.N | Dir.S), 90 },

        // Bridge (시각 변종, B4): same exits as Straight. road-bridge.fbx — 약간 높은 다리 외형.
        { (Kind.Bridge, Dir.E | Dir.W), 0 },
        { (Kind.Bridge, Dir.N | Dir.S), 90 },

        // Driveway (single-car entry): 1 exit. Same rotation semantics as End.
        { (Kind.Driveway, Dir.E), 0 },
        { (Kind.Driveway, Dir.S), 90 },
        { (Kind.Driveway, Dir.W), 180 },
        { (Kind.Driveway, Dir.N), 270 },

        // Slant (램프) — opposite-exit straight. native FBX는 +Z(북)로 상승.
        // rotation은 ASCII char에 따라 직접 지정 (auto-infer 어려움). 아래 ChooseSlantRotation 참조.
        // 여기 RotationFor는 placeholder — 실제는 ChooseSlantRotation에서 결정.
        { (Kind.Slant, Dir.E | Dir.W), 0 },
        { (Kind.Slant, Dir.N | Dir.S), 0 },
        { (Kind.SlantHigh, Dir.E | Dir.W), 0 },
        { (Kind.SlantHigh, Dir.N | Dir.S), 0 },
        { (Kind.SlantFlat, Dir.E | Dir.W), 0 },
        { (Kind.SlantFlat, Dir.N | Dir.S), 90 },
        { (Kind.SlantFlatHigh, Dir.E | Dir.W), 0 },
        { (Kind.SlantFlatHigh, Dir.N | Dir.S), 90 },
    };

    static readonly Dictionary<Kind, string> SourcePath = new()
    {
        { Kind.Straight, "road-straight.fbx" },
        { Kind.Curve, "road-bend.fbx" },
        { Kind.Intersection, "road-intersection.fbx" },
        { Kind.Crossroad, "road-crossroad.fbx" },
        { Kind.End, "road-end-round.fbx" },       // 세션 13: 막다른 골목은 round cap 사용 (교외 분위기)
        { Kind.Crossing, "road-crossing.fbx" },   // 세션 13: 횡단보도 straight 대체
        { Kind.Driveway, "road-driveway-single.fbx" }, // 세션 13: 배달점 진입로
        { Kind.Bridge, "road-bridge.fbx" },        // 세션 16 B4: 다리 변종
        { Kind.Slant, "road-slant.fbx" },          // 세션 17: 램프 (1.6m 상승, native FBX +Z 방향)
        { Kind.SlantHigh, "road-slant-high.fbx" }, // 세션 17: 더 가파른 램프 (3m)
        { Kind.SlantFlat, "road-slant-flat.fbx" }, // 세션 17: 램프 위 평지 (저)
        { Kind.SlantFlatHigh, "road-slant-flat-high.fbx" }, // 세션 17: 램프 위 평지 (고)
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
        { Kind.Crossing, 1f },        // road-crossing = 1x1 (D-034 catalog)
        { Kind.Driveway, 1f },        // road-driveway-single = 1x1
        { Kind.Bridge, 1f },          // road-bridge = 1x1 (B4)
        { Kind.Slant, 1f },
        { Kind.SlantHigh, 1f },
        { Kind.SlantFlat, 1f },
        { Kind.SlantFlatHigh, 1f },
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
        return BuildMaskInternal(layout.roadMask, layout);
    }

    public static void BuildMask(string[] mask)
    {
        BuildMaskInternal(mask, null);
    }

    // 세션 17 D-050: heightMask 통합. layout이 null이면 평지 (기존 동작 유지).
    // layout.GetHeight(c, r)로 셀별 Y offset 추가. 인접 도로 셀의 높이가 다르면 경고 로그
    // (현재 ramp tile 미통합 — 평지 plateau로만 작성 권장).
    static GameObject BuildMaskInternal(string[] mask, TrackLayout layout)
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

        // 세션 13: 복합 도로 문자 지원
        //   '#' = 일반 도로 (자동 Kind 추론)
        //   'X' = pedestrian crossing (Straight 시각 변종, 연결성 동일)
        //   'D' = driveway 진입로 (End 변종, 1 exit 강제)
        // 전부 IsRoad = true 로 취급해 이웃 탐색에 참여.
        // 세션 17 elevated road chars:
        //   'n'/'s'/'e'/'w' = 램프 (저→고, 화살표 가리키는 방향이 높음)
        //   'N'/'S'/'E'/'W' = 가파른 램프 (slant-high, 3m)
        //   '=' = 평지 elevated 1.6m (slant-flat)
        //   '+' = 평지 elevated 3m (slant-flat-high)
        bool IsSlope(char ch) => "nseworNSEW=+".IndexOf(ch) >= 0;
        bool IsRoad(int c, int r)
        {
            if (c < 0 || c >= width || r < 0 || r >= height) return false;
            char ch = mask[r][c];
            return ch == '#' || ch == 'X' || ch == 'D' || ch == 'B' || IsSlope(ch);
        }

        char CellChar(int c, int r)
        {
            if (c < 0 || c >= width || r < 0 || r >= height) return '.';
            return mask[r][c];
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

                // 세션 13: mask char 기반 시각 변종 override
                char ch = CellChar(c, r);
                if (ch == 'X')
                {
                    if (kind == Kind.Straight) kind = Kind.Crossing;
                    else Debug.LogWarning($"TileMapAuto: 'X' crossing at ({c},{r}) needs opposite exits, got {kind} {exits}");
                }
                else if (ch == 'D')
                {
                    if (kind == Kind.End) kind = Kind.Driveway;
                    else Debug.LogWarning($"TileMapAuto: 'D' driveway at ({c},{r}) needs 1 exit, got {kind} {exits}");
                }
                else if (ch == 'B')
                {
                    if (kind == Kind.Straight) kind = Kind.Bridge;
                    else Debug.LogWarning($"TileMapAuto: 'B' bridge at ({c},{r}) needs opposite exits, got {kind} {exits}");
                }
                // 세션 17: 램프/elevated flat 처리
                int slantRotOverride = -1;  // -1 = no override
                if (IsSlope(ch))
                {
                    if (kind != Kind.Straight)
                    {
                        Debug.LogWarning($"TileMapAuto: slope '{ch}' at ({c},{r}) needs Straight (2 opposite exits), got {kind} {exits}");
                    }
                    else
                    {
                        char low = char.ToLower(ch);
                        bool isHigh = char.IsUpper(ch);  // N/S/E/W = 가파름
                        if (low == '=') kind = Kind.SlantFlat;
                        else if (low == '+') kind = Kind.SlantFlatHigh;
                        else
                        {
                            kind = isHigh ? Kind.SlantHigh : Kind.Slant;
                            // road-slant.fbx native: rises to +Z (north). rot으로 다른 방향.
                            // n/N (north 상승): rot 0
                            // s/S (south 상승): rot 180
                            // e/E (east 상승): rot 270 (90° CW makes +Z point to -X, but we want +X up — actual: try 270)
                            // w/W (west 상승): rot 90
                            switch (low)
                            {
                                case 'n': slantRotOverride = 0; break;
                                case 's': slantRotOverride = 180; break;
                                case 'e': slantRotOverride = 270; break;
                                case 'w': slantRotOverride = 90; break;
                            }
                        }
                        // SlantFlat/SlantFlatHigh: rotation은 exits 기반 (E|W=0, N|S=90)
                        if (low == '=' || low == '+')
                            slantRotOverride = (exits == (Dir.E | Dir.W)) ? 0 : 90;
                    }
                }

                if (!RotationFor.TryGetValue((kind, exits), out int rot))
                {
                    Debug.LogWarning($"TileMapAuto: no rotation for ({kind}, {exits}) at ({c},{r})");
                    continue;
                }
                if (slantRotOverride >= 0) rot = slantRotOverride;
                if (kind == Kind.Straight) rot = AdjustStraightRotation(c, r, exits, rot);

                if (!sources.TryGetValue(kind, out var src) || src == null)
                {
                    Debug.LogWarning($"TileMapAuto: no source for {kind}");
                    continue;
                }

                float yOffset = layout != null ? layout.GetHeight(c, r) : 0f;
                bool isSlopeKind = kind == Kind.Slant || kind == Kind.SlantHigh ||
                                   kind == Kind.SlantFlat || kind == Kind.SlantFlatHigh;
                // 세션 17: slant flat은 ramp 위에 얹히므로 자체 Y 오프셋 적용 (FBX native 두께)
                if (kind == Kind.SlantFlat) yOffset = 1.62f;        // road-slant 1.62m
                else if (kind == Kind.SlantFlatHigh) yOffset = 3.12f; // road-slant-high 3.12m
                // 인접 도로 셀과 높이차가 있으면 경고. slope cell은 의도된 step이라 skip.
                if (layout != null && layout.heightMask != null && !isSlopeKind)
                {
                    int[] dx = { 0, 0, 1, -1 };
                    int[] dy = { 1, -1, 0, 0 };
                    for (int k = 0; k < 4; k++)
                    {
                        int nc = c + dx[k], nr = r + dy[k];
                        if (!IsRoad(nc, nr)) continue;
                        float nh = layout.GetHeight(nc, nr);
                        if (Mathf.Abs(nh - yOffset) > 0.01f)
                        {
                            Debug.LogWarning($"TileMapAuto: road height step at ({c},{r})↔({nc},{nr}) ({yOffset:0.0}m → {nh:0.0}m) — ramp tile 미통합. plateau 단절 권장.");
                            break;
                        }
                    }
                }
                var pos = new Vector3(c * CellSize, TileY + yOffset, r * CellSize);
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
