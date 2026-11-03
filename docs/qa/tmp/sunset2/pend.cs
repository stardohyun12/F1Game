// Play: 일어서기 → 걷기 상태 엔진 상태 + 콘솔 에러 수
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
if (spot.Seated) d.SimInteract();
string info = "state=" + d.State + " OnFootMute=" + EngineAudio.OnFootMute;
foreach (var ea in UnityEngine.Object.FindObjectsByType<EngineAudio>(FindObjectsSortMode.None))
{ var s = ea.GetComponent<AudioSource>(); info += " | engine " + ea.name + " playing=" + s.isPlaying + " mute=" + s.mute; }
var le = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
int err = 0, warn = 0, log = 0;
var m = le.GetMethod("GetCountsByType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
var args = new object[] { 0, 0, 0 }; m.Invoke(null, args); err = (int)args[0]; warn = (int)args[1]; log = (int)args[2];
info += " | console err=" + err + " warn=" + warn;
return info;
