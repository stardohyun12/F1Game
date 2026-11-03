var t = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
var e = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");
int n = (int)t.GetMethod("StartGettingEntries").Invoke(null, null);
var sb = new System.Text.StringBuilder(); int err = 0, warn = 0;
var ent = System.Activator.CreateInstance(e);
for (int i = 0; i < n; i++) {
  t.GetMethod("GetEntryInternal").Invoke(null, new object[] { i, ent });
  int mode = (int)e.GetField("mode").GetValue(ent);
  string msg = (string)e.GetField("message").GetValue(ent);
  bool isErr = (mode & (1 | 2 | 16 | 64 | 256 | 2048 | 131072 | 1048576)) != 0;
  bool isWarn = (mode & (128 | 512 | 4096)) != 0;
  if (isErr) { err++; sb.AppendLine("E: " + msg.Split('\n')[0]); }
  else if (isWarn) { warn++; if (warn <= 8) sb.AppendLine("W: " + msg.Split('\n')[0]); }
}
t.GetMethod("EndGettingEntries").Invoke(null, null);
return "entries=" + n + " errors=" + err + " warnings=" + warn + "\n" + sb.ToString();
