// terrace6: 집~차고 사이 위에서 내려다본 1장(직교) — 경로 잡기용. 중심 (215,-64), 가로 80 m.
string outDir="D:/Github/F1Game/docs/qa/tmp/terrace6/";var owned=new System.Collections.Generic.List<UnityEngine.Object>();
try{
var sview=UnityEditor.SceneView.lastActiveSceneView;var src=sview!=null?sview.camera:Camera.main;
var camGo=new GameObject("_T6CAP"){hideFlags=HideFlags.HideAndDontSave};owned.Add(camGo);var cam=camGo.AddComponent<Camera>();cam.CopyFrom(src);cam.cameraType=CameraType.Game;cam.enabled=false;
var pcl=UnityEngine.Object.FindFirstObjectByType<PCLookScene>();if(pcl!=null)pcl.ApplySky(cam);
cam.orthographic=true;cam.orthographicSize=22.5f;cam.farClipPlane=400;cam.nearClipPlane=.1f;
camGo.transform.SetPositionAndRotation(new Vector3(215,150,-64),Quaternion.LookRotation(Vector3.down,Vector3.forward));
PitCrewPlan.PlanIO.RenderPng(cam,outDir+"top_before.png",1600,900,null);
return "ok ortho 80x45 m center 215,-64 -> 20 px/m";
}finally{for(int i=owned.Count-1;i>=0;i--)if(owned[i])UnityEngine.Object.DestroyImmediate(owned[i]);}
