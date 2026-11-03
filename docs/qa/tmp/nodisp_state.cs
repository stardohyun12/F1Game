// Play: SessionState "ns" = foot | seat | cart | sunsetoff | sunseton — 시나리오 상태로 전환.
Application.runInBackground = true;
Time.timeScale = 1f;
string want = UnityEditor.SessionState.GetString("ns", "foot");
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var look = SunsetLook.Find();
if (want == "sunsetoff") { look.SetOn(false); return "sunset=" + look.IsOn; }
if (want == "sunseton") { look.SetOn(true); return "sunset=" + look.IsOn; }
if (want == "seat")
{
    if (d.State.ToString() == "OnFoot") { d.GoToSpot(); var spots = (System.Collections.IList)typeof(OnFootDirector).GetField("spots", bf).GetValue(d); typeof(OnFootDirector).GetMethod("Sit", bf).Invoke(d, new object[] { spots[0] }); }
}
else if (want == "cart")
{
    if (d.State.ToString() == "OnFoot") typeof(OnFootDirector).GetMethod("EnterCart", bf).Invoke(d, null);
}
else if (want == "foot")
{
    if (d.State.ToString() != "OnFoot") typeof(OnFootDirector).GetMethod("EnterFoot", bf).Invoke(d, null);
}
return "state=" + d.State + " sunset=" + look.IsOn + " cam=" + Camera.main.transform.position.ToString("F1") + " frame=" + Time.frameCount;
