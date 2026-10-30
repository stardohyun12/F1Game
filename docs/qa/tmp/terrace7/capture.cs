// terrace7 캡처: 카트 뒤 카메라 근사(차 뒤 5.5 m · 위 2.2 m · 앞 6 m 를 봄, fov 60) 3컷 — 큰길에서 진입로로 꺾기 전 / 원호 중간 / 마당 앞 — + 위에서 직교 1장
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string outDir="D:/Github/F1Game/docs/qa/evidence/terrace7/";System.IO.Directory.CreateDirectory(outDir);
var owned=new System.Collections.Generic.List<UnityEngine.Object>();var log=new System.Text.StringBuilder();
var rs=GameObject.Find("TownV6_Gray/TerraceBlock/Driveway").GetComponent<RoadSurface>();var cl=rs.centerline;
var cols=new System.Collections.Generic.List<Collider>();
foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&c.bounds.max.x>215&&c.bounds.min.x<265&&c.bounds.max.z>-80&&c.bounds.min.z<-35)cols.Add(c);
float Gy(Vector2 p){float y=float.NegativeInfinity;foreach(var c in cols){RaycastHit h;if(c.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000))y=Mathf.Max(y,h.point.y);}return y;}
Vector2 C(int i)=>new Vector2(cl[i].x,cl[i].z);
try{
var sview=UnityEditor.SceneView.lastActiveSceneView;var src=sview!=null?sview.camera:Camera.main;
var camGo=new GameObject("_T7CAP"){hideFlags=HideFlags.HideAndDontSave};owned.Add(camGo);var cam=camGo.AddComponent<Camera>();cam.CopyFrom(src);cam.cameraType=CameraType.Game;cam.enabled=false;
var pcl=UnityEngine.Object.FindFirstObjectByType<PCLookScene>();if(pcl!=null)pcl.ApplySky(cam);
cam.farClipPlane=2000;cam.nearClipPlane=.05f;
void Shoot(string file,Vector2 car,Vector2 d){d=d.normalized;float gy=Gy(car);var cp=car-d*5.5f;var pos=new Vector3(cp.x,Mathf.Max(gy,Gy(cp))+2.2f,cp.y);var tp=car+d*6f;var tgt=new Vector3(tp.x,gy+.8f,tp.y);
 cam.orthographic=false;cam.fieldOfView=60;camGo.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
 PitCrewPlan.PlanIO.RenderPng(cam,outDir+file,1280,720,null);log.AppendLine(file+" car=("+car.x.ToString("F1")+","+gy.ToString("F2")+","+car.y.ToString("F1")+") cam="+pos.ToString("F2")+" tgt="+tgt.ToString("F2"));}
var d0=(C(1)-C(0)).normalized;
Shoot("c1_mainroad.png",C(0)-d0*6f,d0);                       // 큰길 위, 갈림목 6 m 전
Shoot("c2_arc.png",C(7),C(8)-C(6));                            // 원호 중간(s≈7)
Shoot("c3_yard.png",C(18),C(19)-C(17));                        // 마당 6 m 전(s≈18)
cam.orthographic=true;cam.orthographicSize=18f;cam.farClipPlane=400;cam.nearClipPlane=.1f;
camGo.transform.SetPositionAndRotation(new Vector3(242,150,-58),Quaternion.LookRotation(Vector3.down,Vector3.forward));
PitCrewPlan.PlanIO.RenderPng(cam,outDir+"top_raw.png",1200,900,null);log.AppendLine("top_raw.png ortho center(242,-58) 25px/m topleft=(218,-40)");
}finally{for(int i=owned.Count-1;i>=0;i--)if(owned[i])UnityEngine.Object.DestroyImmediate(owned[i]);log.AppendLine("dirty="+scene.isDirty+" playing="+Application.isPlaying);System.IO.File.WriteAllText(outDir+"render_evidence.txt",log.ToString());}
return log.ToString();
