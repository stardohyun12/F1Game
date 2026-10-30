if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets") || Application.isPlaying) throw new System.Exception("Studio edit mode required");
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(s.isDirty)throw new System.Exception("Unsaved review changes");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Studio_Building.unity");
return "Studio_Building restored; play=false; scene dirty="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty+"; compiling="+UnityEditor.EditorApplication.isCompiling;
