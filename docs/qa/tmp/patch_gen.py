p='tools/world/gen.py'
s=open(p,encoding='utf-8').read()
a=s.index("var sb = new System.Text.StringBuilder();")
b=s.index("UnityEditor.AssetDatabase.SaveAssets();")
new='''var sb = new System.Text.StringBuilder();
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
    sb.Append(made ? "new " : "overwrite ").Append(name).Append(" guid=").Append(UnityEditor.AssetDatabase.AssetPathToGUID(path)).Append('\n');
}
'''
s=s[:a]+new+s[b:]
open(p,'w',encoding='utf-8').write(s)
