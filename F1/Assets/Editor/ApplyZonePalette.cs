using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// A+C 팔레트 실험 (2026-10-12). 건물 137채가 공유하는 Kenney colormap 을 구역별 변형
/// (tools/asset-gen/palette_colormap.py 가 생성한 Assets/Rendering/Palette/colormap_&lt;kit&gt;_&lt;zone&gt;.png)
/// 으로 바꾼 머티리얼을 만들어 갈아 끼운다. 메시·배치는 손대지 않는다.
/// 구역 = zoneMask 행(z/6): 0~6 공업 / 7~14 다운타운 / 15~19 항구 (PlaceZoneGround 와 동일).
/// 되돌리기 = PitCrew/Art/Revert Zone Palette (FBX 원본 머티리얼로 복귀) 또는 씬 재오픈.
/// </summary>
public static class ApplyZonePalette
{
    const string PaletteDir = "Assets/Rendering/Palette";
    const float Cell = 6f;

    static string ZoneOf(Vector3 pos)
    {
        int row = Mathf.RoundToInt(pos.z / Cell);
        if (row <= 6) return "industrial";
        if (row <= 14) return "downtown";
        return "harbor";
    }

    static string KitOf(Material original)
    {
        var path = AssetDatabase.GetAssetPath(original).ToLowerInvariant();
        if (path.Contains("industrial")) return "industrial";
        if (path.Contains("commercial")) return "commercial";
        if (path.Contains("suburban")) return "suburban";
        return null;
    }

    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    static Material ZoneMaterial(string kit, string zone)
    {
        string key = kit + "_" + zone;
        if (cache.TryGetValue(key, out var m) && m != null) return m;
        string matPath = $"{PaletteDir}/Mat_{key}.mat";
        string texPath = $"{PaletteDir}/colormap_{key}.png";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null) { Debug.LogWarning($"[ZonePalette] 텍스처 없음: {texPath} — palette_colormap.py 먼저"); return null; }
        m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, matPath);
        }
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.08f);
        EditorUtility.SetDirty(m);
        cache[key] = m;
        return m;
    }

    [MenuItem("PitCrew/Art/Apply Zone Palette")]
    public static void Execute()
    {
        var root = GameObject.Find("Buildings");
        if (root == null) { Debug.LogWarning("[ZonePalette] Buildings 루트 없음"); return; }
        int swapped = 0, skipped = 0;
        var perZone = new Dictionary<string, int>();
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            string zone = ZoneOf(r.transform.position);
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || m.mainTexture == null || m.mainTexture.name != "colormap") { continue; }
                string kit = KitOf(m);
                if (kit == null) { skipped++; continue; }
                var zm = ZoneMaterial(kit, zone);
                if (zm == null) { skipped++; continue; }
                mats[i] = zm; changed = true; swapped++;
            }
            if (changed)
            {
                r.sharedMaterials = mats;
                perZone[zone] = perZone.TryGetValue(zone, out var c) ? c + 1 : 1;
            }
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[ZonePalette] ✓ 머티리얼 교체 {swapped} (skip {skipped}) — " +
                  string.Join(", ", perZone.Keys) + " / 되돌리기: PitCrew/Art/Revert Zone Palette");
    }

    [MenuItem("PitCrew/Art/Revert Zone Palette")]
    public static void Revert()
    {
        var root = GameObject.Find("Buildings");
        if (root == null) return;
        int restored = 0;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            // 원본 = 같은 메시의 FBX 안에 들어 있는 'colormap' 머티리얼
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            string fbx = AssetDatabase.GetAssetPath(mf.sharedMesh);
            Material orig = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
                if (o is Material om && om.name == "colormap") { orig = om; break; }
            if (orig == null) continue;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name.StartsWith("Mat_")) { mats[i] = orig; restored++; }
            r.sharedMaterials = mats;
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[ZonePalette] 되돌림 {restored}");
    }
}
