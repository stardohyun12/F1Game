// 스튜디오 씬 보장 — F1Studio 에디터에서 Studio_Building 이 아니면 연다(studio_cap.sh 첫 단계). Play 중이면 먼저 끈다.
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (s.name == "Studio_Building") return "scene ok";
if (UnityEditor.EditorApplication.isPlaying) UnityEditor.EditorApplication.ExitPlaymode();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Studio_Building.unity");
return "opened Studio_Building (was " + s.name + ")";
