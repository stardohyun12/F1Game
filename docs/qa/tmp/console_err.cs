var t = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
var gc = t.GetMethod("GetCountsByType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
var a = new object[] { 0, 0, 0 };
gc.Invoke(null, a);
return "err " + a[0] + " warn " + a[1] + " log " + a[2];
