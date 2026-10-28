var cur = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (cur.isDirty) return "DIRTY " + cur.path;
var g = UnityEditor.AssetDatabase.FindAssets("Map_TownV6 t:Scene");
var paths = string.Join(",", g.Select(x => UnityEditor.AssetDatabase.GUIDToAssetPath(x)));
var p = g.Select(x => UnityEditor.AssetDatabase.GUIDToAssetPath(x)).First(x => x.EndsWith("/Map_TownV6.unity"));
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(p);
return "opened " + p + " (all: " + paths + ")";
