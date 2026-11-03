Application.runInBackground=true;
var dir=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
float yaw=67f; var feet=new Vector3(206f,8.2f,-168f);
dir.DebugTeleport(feet,yaw,null,yaw+90f);
return "tp";
