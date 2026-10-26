var res = (string)typeof(BuildBayLoop).GetMethod("Build", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, new object[] {
  System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/map/town-v6b.json")), "Assets/Scenes/Map_TownV6.unity" });
// 과제 0320-4(도현 확정): 720 + Nearest, 정수배 끔 — 이 씬엔 PCLookScene 이 없어 카메라의 LowResRenderTest 를 직접 켠다
var lr = UnityEngine.Object.FindFirstObjectByType<LowResRenderTest>(FindObjectsInactive.Include); string ls = "LowResRenderTest 없음";
if (lr != null) { var so = new SerializedObject(lr); so.FindProperty("targetHeight").intValue = 720; so.FindProperty("integerScale").boolValue = false; so.FindProperty("smooth").boolValue = false; so.FindProperty("m_Enabled").boolValue = true; so.ApplyModifiedProperties(); ls = "LowRes 720 Nearest 켬 on " + lr.name; }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/townv6e-build.txt"), res + "\n" + ls);
return res + " | " + ls;
