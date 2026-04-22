using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// One-shot: creates BuildingDef SOs from measured candidates, a BuildingCatalog SO grouping them,
// and a default TrackLayout SO that mirrors the current hard-coded layout with inner buildings.
// Safe to re-run: overwrites existing SOs at the same paths.
public static class SetupLayoutAssets
{
    const string DefsDir = "Assets/Data/Buildings";
    const string CatalogPath = "Assets/Data/BuildingCatalog.asset";
    const string LayoutPath = "Assets/Data/DefaultTrackLayout.asset";
    const string SuburbanRoot = "Assets/Model/kenney_city-kit-suburban_20/Models/FBX format/";
    const string CommercialRoot = "Assets/Model/kenney_city-kit-commercial_2.1/Models/FBX format/";

    // (filename, nativeSize measured via MeasureBuildings.cs). 1x1 entries (max <= ~1.5).
    static readonly (string file, float size)[] SuburbanCandidates =
    {
        ("building-type-k.fbx", 1.02f), ("building-type-r.fbx", 1.03f), ("building-type-l.fbx", 1.03f),
        ("building-type-q.fbx", 1.24f), ("building-type-p.fbx", 1.24f), ("building-type-o.fbx", 1.27f),
        ("building-type-c.fbx", 1.29f), ("building-type-i.fbx", 1.29f), ("building-type-a.fbx", 1.30f),
        ("building-type-e.fbx", 1.30f), ("building-type-h.fbx", 1.30f), ("building-type-j.fbx", 1.37f),
        ("building-type-s.fbx", 1.41f), ("building-type-t.fbx", 1.41f), ("building-type-m.fbx", 1.43f),
        ("building-type-f.fbx", 1.43f), ("building-type-u.fbx", 1.43f), ("building-type-g.fbx", 1.45f),
    };

    // 1x1 commercials (max <= ~1.5). Skyscrapers included — they are narrow but tall.
    static readonly (string file, float size)[] CommercialCandidates =
    {
        ("building-d.fbx", 0.90f), ("building-a.fbx", 0.94f), ("building-g.fbx", 0.97f),
        ("building-b.fbx", 0.97f), ("building-h.fbx", 1.01f), ("building-f.fbx", 1.03f),
        ("building-c.fbx", 1.09f), ("building-m.fbx", 1.24f), ("building-i.fbx", 1.30f),
        ("building-l.fbx", 1.40f),
        ("building-skyscraper-e.fbx", 1.30f), ("building-skyscraper-a.fbx", 1.36f),
        ("building-skyscraper-b.fbx", 1.36f), ("building-skyscraper-c.fbx", 1.39f),
        ("building-skyscraper-d.fbx", 1.39f),
    };

    // 2x2 big commercials (nativeSize ~1.6-2.3). At fixed scale 4, they render ~6.5-9.3m — fills 2x2 footprint (8m).
    static readonly (string file, float size)[] BigCommercialCandidates =
    {
        ("building-e.fbx", 1.64f), ("building-j.fbx", 2.08f), ("building-k.fbx", 2.08f), ("building-n.fbx", 2.32f),
    };

    // 2x2 big suburbans (nativeSize ~1.76-1.83). Render ~7-7.3m in 2x2 footprint — slightly small but OK for variety.
    static readonly (string file, float size)[] BigSuburbanCandidates =
    {
        ("building-type-d.fbx", 1.76f), ("building-type-n.fbx", 1.78f), ("building-type-b.fbx", 1.83f),
    };

    [MenuItem("PitCrew/Setup Layout Assets (BuildingDefs + Catalog + Default Layout)")]
    public static void Run()
    {
        Directory.CreateDirectory("Assets/Data");
        Directory.CreateDirectory(DefsDir);

        var suburbanDefs = CreateDefs(SuburbanCandidates, SuburbanRoot, "suburban", 1, 1);
        var commercialDefs = CreateDefs(CommercialCandidates, CommercialRoot, "commercial", 1, 1);
        var bigCommercialDefs = CreateDefs(BigCommercialCandidates, CommercialRoot, "commercial_big", 2, 2);
        var bigSuburbanDefs = CreateDefs(BigSuburbanCandidates, SuburbanRoot, "suburban_big", 2, 2);

        // Catalog. maskChar 'S' = 1x1 suburban, 'C' = 1x1 commercial, 'K' = 2x2 commercial, 'X' = 2x2 suburban.
        var catalog = LoadOrCreate<BuildingCatalog>(CatalogPath);
        catalog.entries = new List<BuildingCatalog.Entry>
        {
            new BuildingCatalog.Entry { maskChar = "S", pool = suburbanDefs },
            new BuildingCatalog.Entry { maskChar = "C", pool = commercialDefs },
            new BuildingCatalog.Entry { maskChar = "K", pool = bigCommercialDefs },
            new BuildingCatalog.Entry { maskChar = "X", pool = bigSuburbanDefs },
        };
        EditorUtility.SetDirty(catalog);

        // Default layout: the 7x5 loop, with inner buildings around the hole perimeter.
        var layout = LoadOrCreate<TrackLayout>(LayoutPath);
        layout.roadMask = new[]
        {
            "#######", // row 0
            "#.....#", // row 1
            "#.....#", // row 2
            "#.....#", // row 3
            "#######", // row 4
        };
        layout.buildingMask = new[]
        {
            ".......", // row 0
            ".SCCCS.", // row 1
            ".S...S.", // row 2
            ".SCCCS.", // row 3
            ".......", // row 4
        };
        layout.catalog = catalog;
        layout.startCell = new Vector2Int(1, 0);
        layout.startYawDeg = 90f;
        layout.deliveries = new List<TrackLayout.DeliveryCell>
        {
            new TrackLayout.DeliveryCell { col = 3, row = 0, label = "Delivery_1" },
            new TrackLayout.DeliveryCell { col = 6, row = 2, label = "Delivery_2" },
            new TrackLayout.DeliveryCell { col = 0, row = 3, label = "Delivery_3" },
        };
        EditorUtility.SetDirty(layout);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"SetupLayoutAssets: {suburbanDefs.Count} suburban(1x1) + {commercialDefs.Count} commercial(1x1) + {bigCommercialDefs.Count} commercial(2x2) + {bigSuburbanDefs.Count} suburban(2x2) defs.");
    }

    static List<BuildingDef> CreateDefs((string file, float size)[] entries, string root, string category, int cellsWide, int cellsDeep)
    {
        var list = new List<BuildingDef>();
        foreach (var (file, size) in entries)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + file);
            if (prefab == null) { Debug.LogWarning($"SetupLayoutAssets: missing {file}"); continue; }

            string assetName = $"{category}_{Path.GetFileNameWithoutExtension(file)}.asset";
            string defPath = $"{DefsDir}/{assetName}";
            var def = LoadOrCreate<BuildingDef>(defPath);
            def.prefab = prefab;
            def.nativeSize = size;
            def.category = category;
            def.entryLocalDir = new Vector3(0, 0, 1); // default +Z; adjust per-prefab later if needed
            def.cellsWide = cellsWide;
            def.cellsDeep = cellsDeep;
            EditorUtility.SetDirty(def);
            list.Add(def);
        }
        return list;
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }
}
