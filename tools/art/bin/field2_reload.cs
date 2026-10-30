var sc=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(sc.isDirty)return "DIRTY - abort";
UnityEditor.AssetDatabase.ImportAsset("Assets/Scripts/Visual/A7ShadeGlobals.cs",UnityEditor.ImportAssetOptions.ForceUpdate);
return "imported; compiling="+UnityEditor.EditorApplication.isCompiling;
