var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (s.isDirty) return "DIRTY " + s.path;
UnityEditor.AssetDatabase.Refresh();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Proto_PackBench.unity");
return "opened " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;