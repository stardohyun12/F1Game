
// tools/world/gen.py 가 만든 에디터 exec 조각 — 에셋이 있으면 그대로(같은 guid) 덮어쓰고, 없으면 만든다.
var dir = "D:/Github/F1Game/tools/world/_out";
if (UnityEditor.EditorApplication.isPlaying) return "FAIL: Play 중";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/World")) UnityEditor.AssetDatabase.CreateFolder("Assets", "World");
var sb = new System.Text.StringBuilder();
var types = new System.Type[] { typeof(WorldPlaces), typeof(WorldPeople), typeof(WorldOrders), typeof(WorldNumbers) };
foreach (var ty in types)
{
    string name = ty.Name;
    string path = "Assets/World/" + name + ".asset";
    var a = UnityEditor.AssetDatabase.LoadAssetAtPath(path, ty) as UnityEngine.ScriptableObject;
    bool made = a == null;
    if (made) { a = UnityEngine.ScriptableObject.CreateInstance(ty); UnityEditor.AssetDatabase.CreateAsset(a, path); }
    UnityEngine.JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(dir + "/" + name + ".json"), a);
    UnityEditor.EditorUtility.SetDirty(a);
    sb.Append(made ? "new " : "overwrite ").Append(name).Append(" guid=").Append(UnityEditor.AssetDatabase.AssetPathToGUID(path)).Append("; ");
}
UnityEditor.AssetDatabase.SaveAssets();
DebugPlaces.Reload();
var pl = WorldData.Places; var pe = WorldData.People; var od = WorldData.Orders; var nu = WorldData.Numbers;
sb.Append("spots=").Append(pl.spots.Count).Append(" dps=").Append(pl.deliveryPoints.Count).Append(" people=").Append(pe.people.Count)
return "end2";
