PitCrewLookMenu.Set(4);
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return "look " + PitCrewLookMenu.Current + " saved " + sc.path + " " + ok + " snapshot " + System.IO.File.Exists("Library/PitCrewMoodSnapshot.json");
