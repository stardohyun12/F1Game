using UnityEditor;
using UnityEngine;

public static class PreviewBend
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";

    public static void Execute()
    {
        var host = GameObject.Find("BendPreview");
        if (host != null) Object.DestroyImmediate(host);
        host = new GameObject("BendPreview");
        host.transform.position = new Vector3(0, 0, -20);

        // Scale 4 each, side-by-side at x = 0, 4, 8, 12
        Place("road-straight.fbx", host, 0);
        Place("road-bend.fbx", host, 4);
        Place("road-bend-sidewalk.fbx", host, 8);
        Place("road-bend-square.fbx", host, 12);
    }

    static void Place(string filename, GameObject parent, float x)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + filename);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        inst.name = filename;
        inst.transform.SetParent(parent.transform, false);
        inst.transform.localPosition = new Vector3(x, 0, 0);
        inst.transform.localScale = Vector3.one * 4f;
    }
}
