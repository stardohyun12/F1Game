Application.runInBackground=true;
var d=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var dot=UnityEngine.Object.FindFirstObjectByType<DotScreenUI>(FindObjectsInactive.Include);
var l=SunsetLook.Find();
return "state="+d.State+" win="+(DebugWindow.Instance!=null)+" dot="+(dot!=null)+" font="+(dot!=null&&dot.BodyFont!=null)+" screen="+Screen.width+"x"+Screen.height+" dusk="+l.dusk+" on="+l.IsOn+" ts="+Time.timeScale+" playing="+Application.isPlaying;
