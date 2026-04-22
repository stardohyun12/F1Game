using UnityEditor;
using UnityEngine;

public static class RebuildOnly
{
    public static void Execute()
    {
        var preview = GameObject.Find("BendPreview");
        if (preview != null) Object.DestroyImmediate(preview);
        TileMapAuto.Build();
    }
}
