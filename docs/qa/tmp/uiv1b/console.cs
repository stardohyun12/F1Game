var t = System.Type.GetType("UnityEditor.LogEntries,UnityEditor");
var te = System.Type.GetType("UnityEditor.LogEntry,UnityEditor");
int n = (int)t.GetMethod("StartGettingEntries").Invoke(null, null);
var e = System.Activator.CreateInstance(te);
int err = 0; var sb = new System.Text.StringBuilder();
for (int i = 0; i < n; i++) {
  t.GetMethod("GetEntryInternal").Invoke(null, new object[] { i, e });
  int mode = (int)te.GetField("mode").GetValue(e);
  string msg = (string)te.GetField("message").GetValue(e);
  bool isErr = (mode & (1 | 2 | 16 | 256 | 2048 | 131072 | 1048576)) != 0;
  if (isErr) { err++; if (err <= 6) sb.Append("ERR: " + msg.Split('\n')[0] + "\n"); }
}
t.GetMethod("EndGettingEntries").Invoke(null, null);
return "entries=" + n + " errors=" + err + "\n" + sb;
