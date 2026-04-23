using UnityEditor;
using UnityEngine;

// Places 4 copies of road-intersection.fbx at each yaw (0/90/180/270) in a row.
// Labels them and captures top-down to confirm which yaw produces which missing side.
public static class MeasureIntersectionRotations
{
    public static void Execute()
    {
        var old = GameObject.Find("__TeeRotProbe");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("__TeeRotProbe");
        root.transform.position = new Vector3(-80f, 0f, -80f);

        string path = "Assets/Model/kenney_city-kit-roads/Models/FBX format/road-intersection.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogError($"MISS: {path}"); return; }

        float[] yaws = { 0f, 90f, 180f, 270f };
        for (int i = 0; i < yaws.Length; i++)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = $"Tee_r{yaws[i]:0}";
            inst.transform.SetParent(root.transform, false);
            inst.transform.localPosition = new Vector3(i * 8f, 0f, 0f);
            inst.transform.localRotation = Quaternion.Euler(0f, yaws[i], 0f);
            inst.transform.localScale = Vector3.one * 6f;

            // Text marker (TextMesh) showing yaw
            var txt = new GameObject($"Label_r{yaws[i]:0}");
            txt.transform.SetParent(root.transform, false);
            txt.transform.localPosition = new Vector3(i * 8f, 3f, 5f);
            txt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var tm = txt.AddComponent<TextMesh>();
            tm.text = $"r{yaws[i]:0}";
            tm.fontSize = 48;
            tm.characterSize = 0.3f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.red;
        }

        // Blue marker indicating +Z direction
        var zMark = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zMark.name = "Mark_PlusZ_North";
        zMark.transform.SetParent(root.transform, false);
        zMark.transform.localPosition = new Vector3(-5f, 1f, 5f);
        zMark.transform.localScale = new Vector3(0.5f, 1f, 3f);
        var mr = zMark.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.blue };

        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.orthographic = true;
            sv.size = 18f;
            sv.pivot = new Vector3(-80f + 12f, 0f, -80f);
            sv.rotation = Quaternion.Euler(90f, 0f, 0f);
            sv.Repaint();
        }
        Debug.Log("MeasureIntersectionRotations: 4 tees at yaws 0/90/180/270. Blue mark = +Z (north).");
    }

    public static void Cleanup()
    {
        var old = GameObject.Find("__TeeRotProbe");
        if (old != null) Object.DestroyImmediate(old);
    }
}
