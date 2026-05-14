using UnityEditor;
using UnityEngine;

public static class MeasureIndustrial
{
    public static void Execute()
    {
        string[] candidates = {
            "chimney-large", "chimney-medium", "chimney-basic", "chimney-small",
            "detail-tank",
            "building-a","building-b","building-c","building-d","building-e",
            "building-f","building-g","building-h","building-i","building-j",
            "building-k","building-l","building-m","building-n","building-o",
            "building-p","building-q","building-r","building-s","building-t"
        };
        string kitRoot = "Assets/Model/kenney_city-kit-industrial_1.0/Models/FBX format/";

        foreach (var name in candidates)
        {
            var path = kitRoot + name + ".fbx";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) { Debug.LogWarning($"MISS: {path}"); continue; }

            // Instantiate in scene temporarily to get world bounds
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
            var renderers = inst.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) { Object.DestroyImmediate(inst); Debug.LogWarning($"{name}: no renderers"); continue; }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            float maxXZ = Mathf.Max(b.size.x, b.size.z);
            Debug.Log($"INDUSTRIAL {name}: size=({b.size.x:0.00}, {b.size.y:0.00}, {b.size.z:0.00}) maxXZ={maxXZ:0.00} h={b.size.y:0.00}");
            Object.DestroyImmediate(inst);
        }
    }
}
