// 밭 투입 캡처: f1_high(=94a3 카메라) · y1 문 앞 마당 → 밭 · y2 집 앞 모퉁이 마당 눈높이 → 밭 입구 · y3 입구 정면 가까이 · t_top 위에서 수직
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string outDir="D:/Github/F1Game/docs/qa/evidence/field-placed/";System.IO.Directory.CreateDirectory(outDir);
var owned=new System.Collections.Generic.List<UnityEngine.Object>();var log=new System.Text.StringBuilder();
var terr=GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();
float Now(float x,float z){RaycastHit h;if(terr.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000))return h.point.y;throw new System.Exception("miss "+x+","+z);}
try{
var sview=UnityEditor.SceneView.lastActiveSceneView;var src=sview!=null?sview.camera:Camera.main;if(!src)throw new System.Exception("No source camera");
var camGo=new GameObject("_FPCAP_CAMERA"){hideFlags=HideFlags.HideAndDontSave};owned.Add(camGo);var cam=camGo.AddComponent<Camera>();cam.CopyFrom(src);cam.cameraType=CameraType.Game;cam.enabled=false;var pcl=UnityEngine.Object.FindFirstObjectByType<PCLookScene>();if(pcl!=null)pcl.ApplySky(cam);
cam.farClipPlane=2000;cam.nearClipPlane=.05f;
void Shoot(string file,Vector3 pos,Vector3 tgt,float fov){cam.fieldOfView=fov;camGo.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
 if(!PitCrewPlan.PlanIO.RenderPng(cam,outDir+file,1280,720,null))throw new System.Exception("Capture failed "+file);log.AppendLine(file+" pos="+pos.ToString("F2")+" target="+tgt.ToString("F2")+" fov="+fov);}
Shoot("f1_high.png",new Vector3(196.881f,53.714f,-81.658f),new Vector3(197.27f,-95.469f,39.031f),60);
var hT=GameObject.Find("D198_Buildings/house_home").transform;var hq=Quaternion.Euler(0,hT.eulerAngles.y,0);
Vector3 HLw(float lx,float lz){return hT.position+hq*new Vector3(lx,0,lz);}
Vector3 Eye(Vector3 w,float h){return new Vector3(w.x,Now(w.x,w.z)+h,w.z);}
{var a=HLw(1.2f,7.2f);var b=HLw(-9.6f,.25f);Shoot("y1_yard_door.png",Eye(a,1.6f),new Vector3(b.x,Now(b.x,b.z)+.3f,b.z),70);}
{var a=HLw(-3.6f,7.4f);var b=HLw(-9.2f,1.6f);Shoot("y2_yard_garden.png",Eye(a,1.6f),new Vector3(b.x,Now(b.x,b.z)+.2f,b.z),70);}
{var a=HLw(-8.6f,5.8f);var b=HLw(-9.2f,1.2f);Shoot("y3_gate.png",Eye(a,1.6f),new Vector3(b.x,Now(b.x,b.z)+.1f,b.z),70);}
{var c=HLw(-6.5f,3.2f);var tp=new Vector3(c.x,Now(c.x,c.z)+16f,c.z);camGo.transform.SetPositionAndRotation(tp,Quaternion.LookRotation(Vector3.down,hq*Vector3.forward));cam.fieldOfView=50;PitCrewPlan.PlanIO.RenderPng(cam,outDir+"t_top.png",1280,720,null);log.AppendLine("t_top.png pos="+tp.ToString("F2")+" down fov=50");}
}finally{
 for(int i=owned.Count-1;i>=0;i--)if(owned[i])UnityEngine.Object.DestroyImmediate(owned[i]);
 log.AppendLine("dirty="+scene.isDirty+" playing="+Application.isPlaying);
 System.IO.File.WriteAllText(outDir+"render_evidence.txt",log.ToString());
}
return log.ToString();
