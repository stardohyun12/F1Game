// SessionState skyalive_ms_* 읽기 + 콘솔 에러 수
string r = "";
foreach (var k in new[] { "aft_old", "aft_new", "dusk_old", "dusk_new" }) r += k + ": " + UnityEditor.SessionState.GetString("skyalive_ms_" + k, "-") + "\n";
var le = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
int err = 0, warn = 0, log = 0;
le.GetMethod("GetCountsByType").Invoke(null, new object[] { err, warn, log });
var args = new object[] { 0, 0, 0 };
le.GetMethod("GetCountsByType").Invoke(null, args);
return r + "console err=" + args[0] + " warn=" + args[1] + " log=" + args[2];
