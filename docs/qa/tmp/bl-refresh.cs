UnityEditor.AssetDatabase.Refresh();
UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
return "refresh requested playing=" + UnityEditor.EditorApplication.isPlaying;
