#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 채널 3 (W1) 시드 — Zone 2개 + ZoneRegistry 생성하고 Pool/ 의 OrderJob을 분배한다.
/// 메뉴: PitCrew/W1/Seed Zones
/// </summary>
public static class SeedW1Zones
{
    const string ZONES_DIR = "Assets/Data/Zones";
    const string DOWNTOWN_PATH = "Assets/Data/Zones/Zone_Downtown.asset";
    const string HARBOR_PATH = "Assets/Data/Zones/Zone_Harbor.asset";
    const string INDUSTRIAL_PATH = "Assets/Data/Zones/Zone_Industrial.asset";
    const string REGISTRY_PATH = "Assets/Data/Zones/ZoneRegistry.asset";

    [MenuItem("PitCrew/W1/Seed Zones")]
    public static void Seed()
    {
        if (!AssetDatabase.IsValidFolder(ZONES_DIR))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            AssetDatabase.CreateFolder("Assets/Data", "Zones");
        }

        var downtown = EnsureZone(DOWNTOWN_PATH, "downtown", "다운타운", 0, new Color(0.55f, 0.85f, 0.95f));
        var harbor = EnsureZone(HARBOR_PATH, "harbor", "항구 구역", 1000, new Color(1f, 0.65f, 0.30f));
        var industrial = EnsureZone(INDUSTRIAL_PATH, "industrial", "공업지대", 2500, new Color(0.75f, 0.85f, 0.45f));

        AssignJobs(downtown, new[]
        {
            "Assets/Data/Orders/Pool/Order_05_StandardBox.asset",
            "Assets/Data/Orders/Pool/Order_08_BirthdayCake.asset",
            "Assets/Data/Orders/Pool/Order_01_Eggs.asset",
        });

        AssignJobs(harbor, new[]
        {
            "Assets/Data/Orders/Pool/Order_03_Fish.asset",
            "Assets/Data/Orders/Pool/Order_07_SashimiTray.asset",
            "Assets/Data/Orders/Pool/Order_06_PremiumWine.asset",
        });

        AssignJobs(industrial, new[]
        {
            "Assets/Data/Orders/Pool/Order_02_Glass.asset",
            "Assets/Data/Orders/Pool/Order_04_WeddingCake.asset",
        });

        var registry = EnsureRegistry(REGISTRY_PATH, downtown, harbor, industrial);

        EditorUtility.SetDirty(downtown);
        EditorUtility.SetDirty(harbor);
        EditorUtility.SetDirty(industrial);
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SeedW1Zones] downtown={downtown.jobs.Count} jobs, harbor(₩{harbor.unlockCost})={harbor.jobs.Count} jobs, industrial(₩{industrial.unlockCost})={industrial.jobs.Count} jobs.");
        Selection.activeObject = registry;
    }

    static Zone EnsureZone(string path, string id, string displayName, int unlockCost, Color tint)
    {
        var z = AssetDatabase.LoadAssetAtPath<Zone>(path);
        if (z == null)
        {
            z = ScriptableObject.CreateInstance<Zone>();
            AssetDatabase.CreateAsset(z, path);
        }
        z.id = id;
        z.displayName = displayName;
        z.unlockCost = unlockCost;
        z.tintColor = tint;
        return z;
    }

    static void AssignJobs(Zone z, string[] jobPaths)
    {
        z.jobs.Clear();
        foreach (var p in jobPaths)
        {
            var job = AssetDatabase.LoadAssetAtPath<OrderJob>(p);
            if (job != null) z.jobs.Add(job);
            else Debug.LogWarning($"[SeedW1Zones] missing job: {p}");
        }
    }

    static ZoneRegistry EnsureRegistry(string path, params Zone[] zones)
    {
        var r = AssetDatabase.LoadAssetAtPath<ZoneRegistry>(path);
        if (r == null)
        {
            r = ScriptableObject.CreateInstance<ZoneRegistry>();
            AssetDatabase.CreateAsset(r, path);
        }
        r.allZones.Clear();
        foreach (var z in zones) if (z != null) r.allZones.Add(z);
        return r;
    }
}
#endif
