var n=UnityEditor.SessionState.GetString("qacapn","a"); UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/20261023-1504-boundary-"+n+".png");
var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); return "cap "+n+" car="+c.transform.position+" cam="+Camera.main.transform.position;
