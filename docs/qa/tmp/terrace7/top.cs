// terrace7: 차고 동쪽 위에서(직교) — 진입로 노선 잡기용. 중심 (242,-58), 가로 48 m, 1200x900 → 25 px/m. 좌상단 = (218,-40)
string outDir="D:/Github/F1Game/docs/qa/tmp/terrace7/";var owned=new System.Collections.Generic.List<UnityEngine.Object>();
try{
var sview=UnityEditor.SceneView.lastActiveSceneView;var src=sview!=null?sview.camera:Camera.main;
var camGo=new GameObject("_T7CAP"){hideFlags=HideFlags.HideAndDontSave};owned.Add(camGo);var cam=camGo.AddComponent<Camera>();cam.CopyFrom(src);cam.cameraType=CameraType.Game;cam.enabled=false;
var pcl=UnityEngine.Object.FindFirstObjectByType<PCLookScene>();if(pcl!=null)pcl.ApplySky(cam);
cam.orthographic=true;cam.orthographicSize=18f;cam.farClipPlane=400;cam.nearClipPlane=.1f;
camGo.transform.SetPositionAndRotation(new Vector3(242,150,-58),Quaternion.LookRotation(Vector3.down,Vector3.forward));
PitCrewPlan.PlanIO.RenderPng(cam,outDir+"top_"+(UnityEngine.Random.value<2?"cur":"")+".png",1200,900,null);
return "ok";
}finally{for(int i=owned.Count-1;i>=0;i--)if(owned[i])UnityEngine.Object.DestroyImmediate(owned[i]);}
