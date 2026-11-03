// Play: 낚시 자리로 옮겨 앉고 노을 켬
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null) return "no director";
if (d.State.ToString() != "OnFoot" && d.State.ToString() != "Seated") return "state=" + d.State;
if (d.State.ToString() == "OnFoot")
{
    d.GoToSpot();
    var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    var spots = (System.Collections.IList)typeof(OnFootDirector).GetField("spots", bf).GetValue(d);
    typeof(OnFootDirector).GetMethod("Sit", bf).Invoke(d, new object[] { spots[0] });
}
var look = SunsetLook.Find(); if (!look.IsOn) look.SetOn(true);
return "state=" + d.State + " sunset=" + look.IsOn + " rot=" + Camera.main.transform.eulerAngles.ToString("F0") + " srOn=" + SeaReflection.Find().enabled;
