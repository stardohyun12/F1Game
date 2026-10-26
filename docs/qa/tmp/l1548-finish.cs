PitCrewLookMenu.Set(0);
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return "look " + PitCrewLookMenu.Current + " global " + Shader.GetGlobalFloat("_PCLook") + " saved " + sc.path + " " + ok;
