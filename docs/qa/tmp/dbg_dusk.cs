float v=UnityEditor.SessionState.GetFloat("dbg.dusk",0f); string n=UnityEditor.SessionState.GetString("dbg.name","x");
string r=DebugWindow.SetDusk(v);
var l=SunsetLook.Find();
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/debug-window/"+n+".png");
var sun=RenderSettings.sun;
return "{\"name\":\""+n+"\",\"set\":"+v+",\"dusk\":"+l.dusk+",\"isOn\":"+l.IsOn.ToString().ToLower()+",\"sunEuler\":\""+(sun!=null?sun.transform.eulerAngles.ToString("F1"):"null")+"\",\"sunColor\":\""+(sun!=null?sun.color.ToString("F2"):"null")+"\",\"res\":\""+r+"\"}";
