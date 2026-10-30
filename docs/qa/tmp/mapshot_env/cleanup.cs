var ok = AssetDatabase.DeleteAsset("Assets/_TmpEnv");
AssetDatabase.Refresh();
int rem = 0; foreach (var o in Resources.FindObjectsOfTypeAll<GameObject>()) if (o.name.StartsWith("_ENVSHOT")) rem++;
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
return "deleted=" + ok + " exists=" + AssetDatabase.IsValidFolder("Assets/_TmpEnv") + " dir=" + System.IO.Directory.Exists(Application.dataPath + "/_TmpEnv") + " scene=" + sc.name + " dirty=" + sc.isDirty + " leftover=" + rem + " tmpEnvMeta=" + System.IO.File.Exists(Application.dataPath + "/_TmpEnv.meta");
