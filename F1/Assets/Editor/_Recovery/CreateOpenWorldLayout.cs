using UnityEditor;
using UnityEngine;

// OpenWorldLayout — 17×13 grid, 4 districts + 2 hill clusters.
// 세션 17 신규 씬 OpenWorld_v2 위해. 도현 요청: "크기 + 고저차 + 미사용 모델로 도시 같이".
//
// Districts (mask grid bottom-up):
//   SW (rows 0-5, cols 0-7)   → 교외 주거 (S/H/P)
//   SE (rows 0-5, cols 8-16)  → 상업 저밀도 (C/L/A)
//   NW (rows 6-12, cols 0-7)  → 공업 (I/F/W/T/N)
//   NE (rows 6-12, cols 8-16) → 다운타운 (X/K/H)
//
// Roads: 직사각형 grid network. 외곽 + 내부 십자.
// Heights: 두 개 plateau cluster — NW industrial '2'(3m), 중앙 hill '1'(1.5m).
//          현재 ramp tile 미통합 → road heightMask는 0 유지, building cell만 상승.
public static class CreateOpenWorldLayout
{
    const string OutPath = "Assets/Data/OpenWorldLayout.asset";
    const string CatalogPath = "Assets/Data/BuildingCatalog.asset";

    [MenuItem("PitCrew/Layout/Create OpenWorldLayout")]
    public static void Execute()
    {
        // road mask (17 wide × 13 tall, bottom-to-top)
        // # = road. 비대칭 격자 — 동쪽 large block, 서쪽 narrow block, partial inner road.
        // 세션 17: col 14 rows 7-11에 N-S 오버패스 (n=ramp up, ===flat elevated, s=ramp down).
        // 차로 (14,6) ground → (14,7) ramp up → (14,8-9) flat 1.6m → (14,10) ramp down → (14,11) ground → (14,12) intersection.
        string[] roadMask = new[]
        {
            "#################", // row 0  (south perimeter)
            "#...#.....#.....#", // row 1
            "#...#.....#.....#", // row 2
            "#...#.....#.....#", // row 3
            "#...#######.....#", // row 4  (partial horizontal cut, only center)
            "#...#.....#.....#", // row 5
            "#################", // row 6  (main central highway, full E-W)
            "#...#.....#...n.#", // row 7  ← col 14 'n' ramp up (north)
            "#...#######...=.#", // row 8  ← col 14 '=' flat elevated 1.6m
            "#...#.....#...=.#", // row 9  ← col 14 '=' flat elevated 1.6m
            "#...#.....#...s.#", // row 10 ← col 14 's' ramp down (south=high, drive north=descend)
            "#...#.....#...#.#", // row 11 ← col 14 '#' ground (overpass landing)
            "#################", // row 12 (north perimeter)
        };

        // building mask. 도로 셀은 '.'로. block 안만 채움.
        // SW 교외 (rows 0-5, cols 0-7)         → S, H, P
        // SE 상업 저밀도 (rows 0-5, cols 8-16) → C, L, A
        // NW 공업 (rows 6-12, cols 0-7)         → I, F, W, T, N
        // NE 다운타운 (rows 6-12, cols 8-16)    → X, K, H
        // 비도로 cell 채움. 도로 cell은 항상 '.'.
        // 도로 위치 (roadMask 기준):
        //   col 0, 16, row 0, 6, 12 = full road
        //   col 4, 10 = vertical road (모든 row)
        //   row 4, 8 cols 4-10 = partial 횡단 (이미 col 4·10 road와 합쳐져 cols 4-10 전부)
        // 비-road blocks:
        //   SW narrow (rows 1-5, cols 1-3) — 5×3 = 15 cells
        //   center (rows 1-3, cols 5-9) — 3×5 = 15
        //   center (row 5, cols 5-9) — 5×1 = 5
        //   SE wide (rows 1-5, cols 11-15) — 5×5 = 25
        //   NW narrow (rows 7-11, cols 1-3) — 5×3 = 15
        //   center (row 7, cols 5-9) — 5×1 = 5
        //   center (rows 9-11, cols 5-9) — 5×3 = 15
        //   NE wide (rows 7-11, cols 11-15) — 5×5 = 25
        string[] buildingMask = new[]
        {
            ".................", // 0 road
            ".SSS.CCCCC.XKXKL.", // 1 — 교외 + 상업 + downtown skyscraper
            ".SHS.LCKLC.KXKHA.", // 2
            ".SSS.CKCKC.LXLXK.", // 3
            ".SHP...........K.", // 4 partial road cols 4-10 → 비-road = cols 1-3, 11-15
            ".PSS.LCKCL.LXLXK.", // 5
            ".................", // 6 road
            ".IFI.WWIWW.III.I.", // 7  col 14 도로 → '.' (overpass 시작)
            ".IIT.........W...", // 8  col 14, 16 모두 도로
            ".FII.WIWIW.III.W.", // 9  col 14 = 도로
            ".IIN.IFIFI.WWI.W.", // 10 col 14 = 도로
            ".TIF.WIIIW.IIW.I.", // 11 col 14 = 도로
            ".................", // 12 road
        };

        // height mask. '.' = 0. '1' = 1.5m, '2' = 3m.
        // NW 공업은 평지(0). NE downtown 동쪽에 작은 언덕 클러스터.
        // 도로 cell 모두 0 유지 (ramp tile 미통합). building cell만 상승.
        // 큰 plateau 패턴 — NW narrow tier 2, 중앙 north tier 1, NE wide tier 2-3.
        // col 16 = perimeter road = 항상 '.'. 도로 cell 위치는 어차피 GetHeight=0.
        // 17 chars: . + 3 + . + 5 + . + 5 + . = 17
        string[] heightMask = new[]
        {
            ".................", // 0 road
            ".................", // 1 SW 평지
            ".................", // 2
            ".................", // 3
            ".................", // 4 partial road
            ".................", // 5
            ".................", // 6 road
            ".222.11111.111.1.", // 7 NW tier2 + 동쪽 tier1, col 14=overpass road('.')
            ".222.......111.1.", // 8 partial road (cols 4-10), col 14=overpass
            ".222.11111.222.2.", // 9 NE 더 높음, col 14=overpass
            ".222.22222.222.2.", // 10
            ".222.22222.222.2.", // 11
            ".................", // 12 road
        };

        // delivery cells — 4구역 분산. 모두 도로 셀 위(roadMask=#).
        var deliveries = new System.Collections.Generic.List<TrackLayout.DeliveryCell>
        {
            new() { col = 4,  row = 0,  label = "SuburbanGate" },     // 남 perimeter 위
            new() { col = 12, row = 0,  label = "DowntownEntry" },    // 남 perimeter
            new() { col = 4,  row = 12, label = "FactoryNorth" },     // 북 perimeter
            new() { col = 14, row = 12, label = "PlateauTop" },       // 북 perimeter (NE plateau 인접)
            new() { col = 0,  row = 6,  label = "WestHub" },          // 서 perimeter 중앙 highway
            new() { col = 16, row = 6,  label = "EastHub" },          // 동 perimeter
            new() { col = 8,  row = 6,  label = "CentralCross" },     // 중앙 highway 중앙
            new() { col = 4,  row = 4,  label = "LowerWest" },        // SW vertical & partial 교차
        };

        // 기존 자산 있으면 갱신
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(OutPath);
        bool isNew = layout == null;
        if (isNew) layout = ScriptableObject.CreateInstance<TrackLayout>();

        layout.roadMask = roadMask;
        layout.buildingMask = buildingMask;
        layout.heightMask = heightMask;
        layout.cellHeightMeters = 1.5f;
        layout.catalog = AssetDatabase.LoadAssetAtPath<BuildingCatalog>(CatalogPath);
        layout.deliveries = deliveries;
        layout.startCell = new Vector2Int(8, 6);   // 중앙 십자 교차로
        layout.startYawDeg = 0f;

        if (isNew) AssetDatabase.CreateAsset(layout, OutPath);
        else EditorUtility.SetDirty(layout);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"CreateOpenWorldLayout: {(isNew ? "created" : "updated")} {OutPath} " +
                  $"({layout.Width}×{layout.Height}, {deliveries.Count} delivery cells, " +
                  $"catalog={(layout.catalog != null ? "linked" : "MISSING")})");
    }
}
