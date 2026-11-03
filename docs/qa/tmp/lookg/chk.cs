var s = SunsetLook.Find();
var t = typeof(SunsetLook).GetField("afternoon");
return "compiling=" + UnityEditor.EditorApplication.isCompiling + " hasAfternoon=" + (t != null) + " on=" + s.IsOn + " amount=" + s.amount + " dusk=" + (t != null ? typeof(SunsetLook).GetField("dusk").GetValue(s) : "-");
