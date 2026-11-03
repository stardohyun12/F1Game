var t=System.Type.GetType("UnityEditor.LogEntries,UnityEditor");var e=System.Type.GetType("UnityEditor.LogEntry,UnityEditor");
int n=(int)t.GetMethod("StartGettingEntries").Invoke(null,null);var sb=new System.Text.StringBuilder();sb.AppendLine("n="+n);
var en=System.Activator.CreateInstance(e);
for(int i=System.Math.Max(0,n-30);i<n;i++){t.GetMethod("GetEntryInternal").Invoke(null,new object[]{i,en});var msg=(string)e.GetField("message").GetValue(en);if(msg.Contains("error")||msg.Contains("TerraceBlock"))sb.AppendLine(msg.Split('\n')[0]);}
t.GetMethod("EndGettingEntries").Invoke(null,null);
var ms=typeof(TerraceBlock).GetMethods(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);sb.Append("methods:");foreach(var m in ms)sb.Append(" "+m.Name);
return sb.ToString();
