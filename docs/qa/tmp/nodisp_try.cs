Application.runInBackground=true;
var lr=UnityEngine.Object.FindFirstObjectByType<LowResRenderTest>();
lr.enabled=false;
return "lowres off, main tt="+(Camera.main.targetTexture!=null)+" cams="+Camera.allCamerasCount;
