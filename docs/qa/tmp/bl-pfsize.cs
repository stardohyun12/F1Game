var o = new System.Text.StringBuilder();
foreach (var p in new[] { "Assets/Art/LofiPilot/Patch.prefab", "Assets/Art/LofiPilot/Manhole.prefab", "Assets/Art/LofiPilot/Drain.prefab", "Assets/Art/LofiPilot/OilStain.prefab",
    "Assets/Art/LofiPilot/Clothesline.prefab", "Assets/Art/LofiPilot/Awning.prefab", "Assets/Art/LofiPilot/Stall.prefab", "Assets/Art/LofiPilot/Crosswalk.prefab",
    "Assets/Art/LofiPilot/Wall3m.prefab", "Assets/Art/LofiPilot/CoastalTree.prefab", "Assets/Art/LofiPilot/Shrub.prefab",
    "Assets/Model/kenney_car-kit/Models/FBX format/sedan.fbx", "Assets/Model/kenney_car-kit/Models/FBX format/van.fbx", "Assets/Model/kenney_car-kit/Models/FBX format/truck-flat.fbx", "Assets/Model/kenney_car-kit/Models/FBX format/taxi.fbx" })
{
    var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
    if (pf == null) { o.Append(p + " MISSING\n"); continue; }
    var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf);
    var rs = go.GetComponentsInChildren<Renderer>(); var b = rs.Length > 0 ? rs[0].bounds : new Bounds(); foreach (var r in rs) b.Encapsulate(r.bounds);
    o.Append(System.IO.Path.GetFileName(p) + " c=" + b.center.ToString("F2") + " s=" + b.size.ToString("F2") + " col=" + go.GetComponentsInChildren<Collider>().Length + " rb=" + go.GetComponentsInChildren<Rigidbody>().Length + "\n");
    UnityEngine.Object.DestroyImmediate(go);
}
return o.ToString();
