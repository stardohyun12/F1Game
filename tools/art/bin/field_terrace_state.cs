var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
int gos=0;foreach(var o in Resources.FindObjectsOfTypeAll<GameObject>())if(o.name.StartsWith("_FIELDT"))gos++;
return Application.dataPath+" scene="+s.path+" dirty="+s.isDirty+" playing="+Application.isPlaying+" compiling="+UnityEditor.EditorApplication.isCompiling+" temp="+gos;
