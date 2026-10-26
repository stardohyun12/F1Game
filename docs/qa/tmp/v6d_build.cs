var res = (string)typeof(BuildBayLoop).GetMethod("Build", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, new object[] {
  System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/map/town-v6b.json")), "Assets/Scenes/Map_TownV6.unity" });
// 과제 0246-4: 씬 기본값 = A(720 + 부드러운 확대)
var pc = UnityEngine.Object.FindFirstObjectByType<PCLookScene>(); string lr = "PCLookScene 없음";
if (pc != null) { var so = new SerializedObject(pc); so.FindProperty("lowResHeight").intValue = 720; so.FindProperty("smoothUpscale").boolValue = true; so.ApplyModifiedProperties(); lr = "lowRes 720 smooth"; }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/townv6d-build.txt"), res + "\n" + lr);
return res + " | " + lr;
