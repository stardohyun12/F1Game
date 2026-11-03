var look = SunsetLook.Find();
if (look != null && look.IsOn) look.SetOn(false);
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.AssetDatabase.Refresh();
return "saved=" + ok + " " + sc.path + " sunsetOn=" + (look != null && look.IsOn);
