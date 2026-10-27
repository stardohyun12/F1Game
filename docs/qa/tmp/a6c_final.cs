// 과제 1704 마무리: Play 꺼짐 확인 후 Scene_A6c_Town 다시 열기
if (EditorApplication.isPlaying) return "PLAYING";
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A6c_Town.unity");
return sc.path + " dirty " + sc.isDirty + " playing " + EditorApplication.isPlaying;
