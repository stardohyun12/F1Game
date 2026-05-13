using UnityEditor;
using UnityEngine;
using System.IO;
using PitCrew.Packing;

public static class PackingDevHarness
{
    [MenuItem("PitCrew/Packing/Create WrapMaterial Assets")]
    public static void CreateWrapMaterials()
    {
        Directory.CreateDirectory("Assets/Resources/WrapMaterials");
        Make("Bubble",    0, costPerPixel: 0.03f, absorption: 0.25f, color: new Color(0.42f, 0.72f, 0.91f, 0.85f), key: KeyCode.Alpha1);
        Make("Foam",      1, costPerPixel: 0.07f, absorption: 0.55f, color: new Color(0.91f, 0.84f, 0.42f, 0.95f), key: KeyCode.Alpha2);
        Make("Hardcase",  2, costPerPixel: 0.18f, absorption: 0.85f, color: new Color(0.33f, 0.33f, 0.36f, 1.0f),  key: KeyCode.Alpha3);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void Make(string name, int idx, float costPerPixel, float absorption, Color color, KeyCode key)
    {
        var so = ScriptableObject.CreateInstance<WrapMaterialDef>();
        so.displayName = name; so.channelIndex = idx;
        so.costPerPixel = costPerPixel; so.absorption = absorption;
        so.displayColor = color; so.hotkey = key;
        AssetDatabase.CreateAsset(so, $"Assets/Resources/WrapMaterials/{name}.asset");
    }
}
