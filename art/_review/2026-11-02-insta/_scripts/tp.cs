// SessionState insta.tp = "장소번호|dusk" → 장소 이동(+시간대). 다음 exec(cap.cs)는 1.5 s 뒤에.
var a = UnityEditor.SessionState.GetString("insta.tp", "8|1").Split('|');
DebugWindow.SetDusk(float.Parse(a[1]));
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var r = DebugWindow.GoPlace(int.Parse(a[0]));
return r + " state=" + d.State;
