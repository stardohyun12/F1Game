var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEditor.EditorApplication.isPlaying) return "PLAYING — 설치 안 함";
if (!sc.path.EndsWith("Map_TownV6.unity")) return "씬이 Map_TownV6 아님: " + sc.path;
var a = OnFootSetup.Run();
var b = FirstOrderSetup.Run();
return a + "\n" + b + "\ndirty=" + sc.isDirty;
