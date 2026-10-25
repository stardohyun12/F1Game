var le=System.Type.GetType("UnityEditor.LogEntries, UnityEditor"); le.GetMethod("Clear").Invoke(null,null);
return "cleared count="+le.GetMethod("GetCount").Invoke(null,null);
