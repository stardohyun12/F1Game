System.Type t = null; string asm = "";
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { var x = a.GetType("ParcelBuild"); if (x != null) { t = x; asm = a.GetName().Name; } }
var rb = System.Type.GetType("RoadsideBuild, Assembly-CSharp-Editor");
var msgs = UnityEditor.Compilation.CompilationPipeline.GetAssemblies().Length;
return "compiling=" + UnityEditor.EditorApplication.isCompiling + " ParcelBuild=" + (t != null) + " asm=" + asm + " roadside@CSharpEditor=" + (rb != null) + " playing=" + UnityEngine.Application.isPlaying + " scene=" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
