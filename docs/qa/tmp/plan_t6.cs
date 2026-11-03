var le = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
var ent = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");
int n = (int)le.GetMethod("StartGettingEntries").Invoke(null, null);
var sb = new System.Text.StringBuilder("n=" + n + "\n");
var e = System.Activator.CreateInstance(ent);
for (int i = 0; i < n; i++) {
  le.GetMethod("GetEntryInternal").Invoke(null, new object[] { i, e });
  var msg = (string)ent.GetField("message").GetValue(e);
  int mode = (int)ent.GetField("mode").GetValue(e);
  sb.Append("[" + mode + "] " + (msg.Length > 1500 ? msg.Substring(0, 1500) : msg) + "\n---\n");
}
le.GetMethod("EndGettingEntries").Invoke(null, null);
return sb.ToString();
