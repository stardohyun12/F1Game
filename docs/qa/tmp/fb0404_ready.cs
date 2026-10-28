if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) return "busy";
string s = "";
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { if (a.GetType("A7ShadeGlobals") != null) s += "A7ShadeGlobals@" + a.GetName().Name + " "; var b = a.GetType("BuildBayLoop"); if (b != null && b.GetMethod("LimitGrade", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) != null) s += "LimitGrade@" + a.GetName().Name; }
return s == "" ? "notype" : s;
