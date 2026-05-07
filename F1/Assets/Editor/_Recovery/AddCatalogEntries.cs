using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BuildingCatalog에 industrial/low-detail/decor 신규 maskChar 추가.
// 'I' = industrial (industrial_building-a~t, 20개)
// 'L' = commercial low-detail (commercial_low-detail-building-a~n, 14개)
// 'A' = commercial decor (awning/overhang/parasol, 6개)
// 'N' = industrial detail-tank (단독, 1개) — props용 1x1 cellsWide
public static class AddCatalogEntries
{
    const string CatalogPath = "Assets/Data/BuildingCatalog.asset";
    const string DefDir = "Assets/Data/Buildings/";

    [MenuItem("PitCrew/Layout/Add Catalog Entries (I/L/A/N)")]
    public static void Execute()
    {
        var cat = AssetDatabase.LoadAssetAtPath<BuildingCatalog>(CatalogPath);
        if (cat == null) { Debug.LogError($"BuildingCatalog not found: {CatalogPath}"); return; }

        // 'I' industrial
        AddOrReplace(cat, "I", LoadByPrefix("industrial_building-"));
        // 'L' low-detail commercial
        AddOrReplace(cat, "L", LoadByPrefix("commercial_low-detail-building-"));
        // 'A' commercial decor (awning/overhang/parasol)
        var decor = new List<BuildingDef>();
        decor.AddRange(LoadByPrefix("commercial_detail-awning"));
        decor.AddRange(LoadByPrefix("commercial_detail-overhang"));
        decor.AddRange(LoadByPrefix("commercial_detail-parasol"));
        AddOrReplace(cat, "A", decor);
        // 'N' industrial detail-tank (props)
        AddOrReplace(cat, "N", LoadByPrefix("industrial_detail-tank"));

        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"AddCatalogEntries: catalog updated, total entries = {cat.entries.Count}");
        foreach (var e in cat.entries)
            Debug.Log($"  '{e.maskChar}' → {e.pool.Count} prefabs");
    }

    static List<BuildingDef> LoadByPrefix(string prefix)
    {
        var list = new List<BuildingDef>();
        var guids = AssetDatabase.FindAssets("t:BuildingDef", new[] { DefDir.TrimEnd('/') });
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!name.StartsWith(prefix)) continue;
            var def = AssetDatabase.LoadAssetAtPath<BuildingDef>(path);
            if (def != null) list.Add(def);
        }
        list = list.OrderBy(d => d.name).ToList();
        return list;
    }

    static void AddOrReplace(BuildingCatalog cat, string maskChar, List<BuildingDef> pool)
    {
        var existing = cat.entries.FirstOrDefault(e => e.maskChar == maskChar);
        if (existing == null)
        {
            cat.entries.Add(new BuildingCatalog.Entry { maskChar = maskChar, pool = pool });
        }
        else
        {
            existing.pool = pool;
        }
    }
}
