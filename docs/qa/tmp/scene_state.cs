var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (s.isDirty) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
return "play " + UnityEditor.EditorApplication.isPlaying + " scene " + s.path + " dirtyAfter " + s.isDirty;
