var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("RoadsideBuild")).FirstOrDefault(x => x != null);
return (UnityEditor.EditorApplication.isCompiling ? "compiling" : "idle") + " " + (t != null ? t.Assembly.GetName().Name : "none") + " " + UnityEditor.EditorUtility.scriptCompilationFailed;
