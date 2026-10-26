var r = MoodSwitcher.Apply(MoodSwitcher.PathAfternoon);
if (System.IO.File.Exists("Library/PitCrewMoodSnapshot.json")) System.IO.File.Delete("Library/PitCrewMoodSnapshot.json");
AssetDatabase.SaveAssets();
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
return r + " saved " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc) + " look " + PitCrewLookMenu.Current + " fog " + RenderSettings.fogStartDistance + "-" + RenderSettings.fogEndDistance;
