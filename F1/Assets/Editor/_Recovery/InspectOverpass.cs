using UnityEditor;
using UnityEngine;

// 오버패스 col 14 rows 7-11 타일들의 실제 world Y 위치 + bounds 점검.
public static class InspectOverpass
{
    [MenuItem("PitCrew/Layout/Inspect Overpass Tiles")]
    public static void Execute()
    {
        string[] names = {
            "Track/Tile_Slant_14_7_r0",
            "Track/Tile_SlantFlat_14_8_r90",
            "Track/Tile_SlantFlat_14_9_r90",
            "Track/Tile_Slant_14_10_r180",
        };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go == null) { Debug.LogWarning($"missing {n}"); continue; }
            var renderers = go.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) { Debug.Log($"{n}: pos={go.transform.position} (no renderer)"); continue; }
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            Debug.Log($"{n}: pos={go.transform.position:F2} euler={go.transform.eulerAngles:F0} scale={go.transform.localScale.x:F2} | bounds.min.y={b.min.y:F2} bounds.max.y={b.max.y:F2} size={b.size:F2}");
        }
    }
}
