int mode = int.Parse(UnityEditor.SessionState.GetString("l5.mode", "5"));
PitCrewLookMenu.Set(mode);
var c = UnityEngine.Object.FindFirstObjectByType<PCLookScene>();
if (c != null && mode == 5) { c.PushGlobals(); c.MarkRenderers(true); }
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return "look " + PitCrewLookMenu.Current + " comp " + (c != null) + " shader " + (c != null && c.discShader != null) + " _PCLook " + Shader.GetGlobalFloat("_PCLook") + " fogP " + Shader.GetGlobalVector("_PCFogParams") + " light " + Shader.GetGlobalColor("_PCLight") + " saved " + ok + " " + sc.name;
