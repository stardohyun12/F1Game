// 과제 0640 — Map_TownV6 를 town-v6h.json 으로 재빌드 + 720 Nearest 다시 켬 (v6e_build.cs 복사)
var res = (string)typeof(BuildBayLoop).GetMethod("Build", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, new object[] {
  System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/map/town-v6h.json")), "Assets/Scenes/Map_TownV6.unity" });
var lr = UnityEngine.Object.FindFirstObjectByType<LowResRenderTest>(FindObjectsInactive.Include); string ls = "LowResRenderTest 없음";
if (lr != null) { var so = new SerializedObject(lr); so.FindProperty("targetHeight").intValue = 720; so.FindProperty("integerScale").boolValue = false; so.FindProperty("smooth").boolValue = false; so.FindProperty("m_Enabled").boolValue = true; so.ApplyModifiedProperties(); ls = "LowRes 720 Nearest 켬 on " + lr.name; }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/townv6h-json-build.txt"), res + "\n" + ls);
return res + " | " + ls;
