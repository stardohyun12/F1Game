using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Scans every road tile in kenney_city-kit-roads and writes a catalog file
// (GDD/research/kenny-roads-catalog.md) + console log. For each FBX:
//   - name, bounds size (x,y,z)
//   - maxXZ (for nativeSize)
//   - rough classification (straight/curve/intersection/split/roundabout/slant/tile/sign/light/construction/bridge/barrier)
// The catalog lets future map sessions pick from a pre-measured pool without re-running measurements.
public static class MeasureAllRoads
{
    const string KitRoot = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";
    const string CatalogOut = "Assets/../../../GDD/research/kenny-roads-catalog.md";

    public static void Execute()
    {
        var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Model/kenney_city-kit-roads" });
        var entries = new List<(string cat, string name, Vector3 size, float maxXZ)>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx")) continue;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
            var renderers = inst.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) { Object.DestroyImmediate(inst); continue; }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            Object.DestroyImmediate(inst);

            string fname = Path.GetFileNameWithoutExtension(path);
            string cat = Classify(fname);
            entries.Add((cat, fname, b.size, Mathf.Max(b.size.x, b.size.z)));
        }

        entries.Sort((a, b) =>
        {
            int cmp = string.Compare(a.cat, b.cat, System.StringComparison.Ordinal);
            return cmp != 0 ? cmp : string.Compare(a.name, b.name, System.StringComparison.Ordinal);
        });

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Kenney city-kit-roads — 타일 카탈로그");
        sb.AppendLine();
        sb.AppendLine("> 세션 11에서 자동 실측. 모든 FBX의 bounds + maxXZ(=nativeSize 후보).");
        sb.AppendLine("> 향후 맵 세션에서 재측정 없이 `TileMapAuto.NativeSize` / `BuildingDef.nativeSize`에 직접 사용 가능.");
        sb.AppendLine();
        sb.AppendLine("| Category | File | x | y | z | maxXZ |");
        sb.AppendLine("|---|---|---|---|---|---|");
        string lastCat = null;
        foreach (var e in entries)
        {
            if (e.cat != lastCat)
            {
                sb.AppendLine($"| **{e.cat}** | | | | | |");
                lastCat = e.cat;
            }
            sb.AppendLine($"| | {e.name} | {e.size.x:0.00} | {e.size.y:0.00} | {e.size.z:0.00} | {e.maxXZ:0.00} |");
            Debug.Log($"[{e.cat}] {e.name}: size=({e.size.x:0.00},{e.size.y:0.00},{e.size.z:0.00}) maxXZ={e.maxXZ:0.00}");
        }

        sb.AppendLine();
        sb.AppendLine("## 활용 매트릭스 (세션 11 기준)");
        sb.AppendLine();
        sb.AppendLine("| 유형 | 현재 사용 | 향후 활용 후보 |");
        sb.AppendLine("|---|---|---|");
        sb.AppendLine("| 직선 | `road-straight` | `road-straight-half`(1/2 셀), `road-straight-barrier` (edge) |");
        sb.AppendLine("| 커브 | `road-bend` | `road-bend-sidewalk`, `road-curve` (2×2 큰 커브), `road-curve-intersection` (커브+T) |");
        sb.AppendLine("| T 교차로 | `road-intersection` | `road-intersection-line`, `road-intersection-path` (pedestrian crossing) |");
        sb.AppendLine("| + 교차로 | `road-crossroad` | `road-crossroad-line`, `road-crossroad-path` |");
        sb.AppendLine("| Dead-end | `road-end` | `road-end-round`, `road-end-barrier` (cul-de-sac) |");
        sb.AppendLine("| **Roundabout** | 없음 | `road-roundabout` — Downtown 중앙 허브 후보 |");
        sb.AppendLine("| **Split (Y-분기)** | 없음 | `road-split` — 오픈월드 지름길 시작점 |");
        sb.AppendLine("| **Side-entry/exit** | 없음 | `road-side-entry`, `road-side-exit` — 블록 진입/진출 |");
        sb.AppendLine("| **Driveway** | 없음 | `road-driveway-single/double` — 건물 앞 진입로 |");
        sb.AppendLine("| **Slant (경사)** | 없음 | `road-slant`, `road-slant-curve`, `road-slant-flat` — 언덕/오르막 |");
        sb.AppendLine("| **Bridge** | 없음 | `road-bridge`, `bridge-pillar` — 다리 |");
        sb.AppendLine("| **Square/Tile** | 없음 | `road-square`, `tile-high`, `tile-low`, `tile-slant`, `tile-slantHigh` — 광장/테라스 |");
        sb.AppendLine("| **Crossing** | 없음 | `road-crossing` — 횡단보도 단독 |");
        sb.AppendLine("| **Signs** | 없음 | `sign-highway`, `sign-highway-detailed`, `sign-highway-wide` — 랜드마크 후보 |");
        sb.AppendLine("| **Lights** | 없음 | `light-square`, `light-curved`, `*-cross`, `*-double` — 디테일 프롭 (4주차 파킹) |");
        sb.AppendLine("| **Construction** | 없음 | `construction-barrier/cone/light` — 장애물/데코 |");

        // Write under GDD/research/
        string outPath = "C:/Users/stard/GameDevelope/PitCrew/GDD/research/kenny-roads-catalog.md";
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log($"[MeasureAllRoads] wrote catalog → {outPath} ({entries.Count} entries)");
    }

    static string Classify(string name)
    {
        if (name.StartsWith("road-straight")) return "straight";
        if (name.StartsWith("road-bend")) return "bend";
        if (name.StartsWith("road-curve-intersection")) return "curve-intersection";
        if (name.StartsWith("road-curve")) return "curve";
        if (name.StartsWith("road-intersection")) return "intersection";
        if (name.StartsWith("road-crossroad")) return "crossroad";
        if (name.StartsWith("road-roundabout")) return "roundabout";
        if (name.StartsWith("road-split")) return "split";
        if (name.StartsWith("road-side")) return "side-entry-exit";
        if (name.StartsWith("road-driveway")) return "driveway";
        if (name.StartsWith("road-end")) return "dead-end";
        if (name.StartsWith("road-slant")) return "slant";
        if (name.StartsWith("road-bridge")) return "bridge";
        if (name.StartsWith("road-crossing")) return "pedestrian-crossing";
        if (name.StartsWith("road-square")) return "square";
        if (name.StartsWith("bridge-")) return "bridge-pillar";
        if (name.StartsWith("sign-")) return "sign";
        if (name.StartsWith("light-")) return "light";
        if (name.StartsWith("construction-")) return "construction";
        if (name.StartsWith("tile-")) return "tile-slant";
        return "other";
    }
}
