var t = System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } }).FirstOrDefault(x => x.Name == "BuildWindowGlow");
var f = typeof(MoodProfile).GetField("windowsLit");
return "compiling=" + UnityEditor.EditorApplication.isCompiling + " new=" + (t != null) + " profileField=" + (f != null) + " playing=" + Application.isPlaying;
