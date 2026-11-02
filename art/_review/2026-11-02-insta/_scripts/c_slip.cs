// 전표(Tab) — 칠판에서 첫 주문을 맡은 상태를 만든다(Play 상태만, 코드 변경 없음): pickedJob = MakeFirstJob() 를 리플렉션으로. 그 뒤 SimSlip=true.
DebugWindow.SetDusk(0f);
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
var mk = typeof(OnFootDirector).GetMethod("MakeFirstJob", bf); var fld = typeof(OnFootDirector).GetField("pickedJob", bf);
if (mk == null || fld == null) return "FAIL: MakeFirstJob/pickedJob 없음";
if (d.PickedJob == null) fld.SetValue(d, mk.Invoke(d, null));
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>(); ui.SimSlip = true;
return "picked=" + (d.PickedJob != null) + " state=" + d.State;
