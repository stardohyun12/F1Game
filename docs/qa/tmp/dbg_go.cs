Application.runInBackground=true;
int i = UnityEditor.SessionState.GetInt("dbg.i", 0);
UnityEditor.SessionState.SetInt("dbg.i", i + 1);
string[] nm = { "room", "front", "boss", "cartside", "tea", "sunset", "harbor", "sink", "main" };
UnityEditor.SessionState.SetString("dbg.name", (i + 1).ToString("00") + "_" + nm[i]);
string r = DebugWindow.GoPlace(i);
return i + ": " + r;
