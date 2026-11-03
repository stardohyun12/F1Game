var d=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>(); var s=d.DebugSpot;
string n=UnityEditor.SessionState.GetString("dbg.name","x");
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/debug-window/"+n+".png");
return "{\"name\":\""+n+"\",\"state\":\""+d.State+"\",\"seated\":"+s.Seated.ToString().ToLower()+",\"landed\":"+s.Landed.ToString().ToLower()+",\"surfaceY\":\""+s.SurfaceY.ToString("F2")+"\",\"bobberInPath\":"+s.BobberInPath.ToString().ToLower()+",\"feet\":\""+d.Player.transform.position.ToString("F2")+"\"}";
