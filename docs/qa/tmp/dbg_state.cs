var d=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var cc=d.Player.Controller; var p=d.Player.transform.position;
RaycastHit h; bool g=Physics.Raycast(p+Vector3.up*0.3f,Vector3.down,out h,3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
string n = UnityEditor.SessionState.GetString("dbg.name","x");
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/debug-window/"+n+".png");
return "{\"name\":\""+n+"\",\"state\":\""+d.State+"\",\"feet\":\""+p.ToString("F2")+"\",\"groundBelow\":"+(g?(p.y-h.point.y).ToString("F2"):"null")+",\"groundName\":\""+(g?h.collider.name:"")+"\",\"grounded\":"+cc.isGrounded.ToString().ToLower()+",\"prompt\":\""+d.Prompt+"\",\"car\":\""+d.Car.transform.position.ToString("F1")+"\",\"carDist\":"+Vector3.Distance(d.Car.transform.position,p).ToString("F1")+"}";
