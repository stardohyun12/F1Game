// 콘솔 에러 메시지(최대 5개)
var t = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
var et = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");
var bf = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public;
int n = (int)t.GetMethod("StartGettingEntries", bf).Invoke(null, null);
var sb = new System.Text.StringBuilder("n=" + n + " playing=" + Application.isPlaying + "\n");
var e = System.Activator.CreateInstance(et); int shown = 0;
for (int i = n - 1; i >= 0 && shown < 5; i--)
{
    t.GetMethod("GetEntryInternal", bf).Invoke(null, new object[] { i, e });
    int mode = (int)et.GetField("mode").GetValue(e);
    string msg = (string)et.GetField("message").GetValue(e);
    if ((mode & (1 | 2 | 16 | 64 | 256 | 2048 | 131072 | 4194304)) != 0 && !msg.Contains("arning")) { sb.Append(mode).Append(": ").Append(msg.Length > 400 ? msg.Substring(0, 400) : msg).Append("\n"); shown++; }
}
t.GetMethod("EndGettingEntries", bf).Invoke(null, null);
return sb.ToString();
