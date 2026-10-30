if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) throw new System.Exception("F1Studio only");
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var log = new System.Text.StringBuilder(Application.dataPath + " | " + s.path + " | play=" + Application.isPlaying + " dirty=" + s.isDirty);
foreach(var g in s.GetRootGameObjects()) log.Append("\nROOT " + g.name);
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if(r.name.Contains("Ground") || r.name.Contains("ground")) log.Append("\nGROUND " + r.name + " " + r.sharedMaterial.name + " " + r.sharedMaterial.shader.name);
return log.ToString();
