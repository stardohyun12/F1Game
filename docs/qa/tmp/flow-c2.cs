Application.runInBackground=true;
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/20261023-1434-packing-first-view.png");
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>(); var it=ps.ActiveItem; var cam=Camera.allCameras;
var wz=it.transform.forward; var pc=typeof(PitCrew.Packing.PackingScreen).GetField("packingCamera",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(ps) as Camera;
var ws=it.GetComponent<PitCrew.Packing.WrapState>();
return "itemFwd="+wz+" camFwd="+pc.transform.forward+" dot="+Vector3.Dot(wz,pc.transform.forward)+" cap="+ws.CostCap;
