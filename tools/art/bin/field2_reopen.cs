var sc=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(sc.isDirty)return "DIRTY - abort";
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sc.path,UnityEditor.SceneManagement.OpenSceneMode.Single);
sc=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sg=UnityEngine.Object.FindObjectsByType<A7ShadeGlobals>(FindObjectsInactive.Include,FindObjectsSortMode.None);
return "reopened dirty="+sc.isDirty+" count="+sg.Length+" shade="+Shader.GetGlobalColor("_A7Shade")+" T="+Shader.GetGlobalVector("_A7T")+" mid="+Shader.GetGlobalFloat("_A7Mid");
