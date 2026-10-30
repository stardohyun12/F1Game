var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
return s.name + " dirty=" + s.isDirty + " play=" + Application.isPlaying + " tmp=" + System.IO.Directory.Exists(Application.dataPath + "/_TmpEnv") + " proj=" + Application.dataPath;
