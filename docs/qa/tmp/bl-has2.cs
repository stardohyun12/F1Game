var t = System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } }).FirstOrDefault(x => x.Name == "BuildBayLoop");
var m = t == null ? null : t.GetMethod("Bands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
return "compiling=" + UnityEditor.EditorApplication.isCompiling + " v4=" + (m != null) + " playing=" + Application.isPlaying;
