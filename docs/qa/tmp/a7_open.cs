var p = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/a7_mode.txt")).Trim() == "a6d" ? "Assets/Scenes/Scene_A6d_Town.unity" : "Assets/Scenes/Scene_A7_Light.unity";
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(p);
return sc.path + " dirty " + sc.isDirty;
