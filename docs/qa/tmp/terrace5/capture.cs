// terrace4 캡처(3차와 같은 카메라 + 마당 눈높이 2컷). 4차 고침: cam.cameraType=Game — SceneView 카메라를 CopyFrom 하면 SceneView 형이라 UIRoot(오버레이 캔버스, 1920×1080 m)가 월드에 남색 판으로 그려졌다.
// (원 주석) terrace3 캡처: 1·2차와 같은 9 카메라(v2 지형 사본 기준 → 전후 같은 자리) + 사람 눈높이 1.6 m 걷기 3컷(지금 지형+보도 콜라이더 기준)
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string outDir="D:/Github/F1Game/docs/qa/evidence/terrace5/after/";System.IO.Directory.CreateDirectory(outDir);
var owned=new System.Collections.Generic.List<UnityEngine.Object>();var log=new System.Text.StringBuilder();
var v2=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_v2.asset");
var tg=new GameObject("_T4CAP_col"){hideFlags=HideFlags.HideAndDontSave};owned.Add(tg);var tmc=tg.AddComponent<MeshCollider>();tmc.sharedMesh=v2;
float Ground(float x,float z){RaycastHit h;if(tmc.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000))return h.point.y;throw new System.Exception("Ground miss "+x+","+z);}
var nowCols=new System.Collections.Generic.List<Collider>();nowCols.Add(GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>());
var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways");if(wk)nowCols.Add(wk.GetComponent<MeshCollider>());
float Now(float x,float z){float y=float.NegativeInfinity;foreach(var c in nowCols){RaycastHit h;if(c&&c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000))y=Mathf.Max(y,h.point.y);}if(float.IsInfinity(y))throw new System.Exception("Now miss "+x+","+z);return y;}
var roads=new System.Collections.Generic.List<Collider>();foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform)if(ch.gameObject.activeInHierarchy&&ch.name.StartsWith("ROAD_V2e")){var c=ch.GetComponent<Collider>();if(c)roads.Add(c);}
Vector3 RoadAt(float x){float z0=float.NaN,z1=float.NaN,y=0;for(float z=-66;z>=-100;z-=.25f){bool hit=false;foreach(var c in roads){RaycastHit h;if(c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000)){hit=true;y=h.point.y;break;}}if(hit){if(float.IsNaN(z0))z0=z;z1=z;}else if(!float.IsNaN(z0))break;}if(float.IsNaN(z0))throw new System.Exception("no road at x="+x);float zm=(z0+z1)/2;foreach(var c in roads){RaycastHit h;if(c.Raycast(new Ray(new Vector3(x,500,zm),Vector3.down),out h,1000)){y=h.point.y;break;}}return new Vector3(x,y,zm);}
try{
var sview=UnityEditor.SceneView.lastActiveSceneView;var src=sview!=null?sview.camera:Camera.main;if(!src)throw new System.Exception("No source camera");
var camGo=new GameObject("_T4CAP_CAMERA"){hideFlags=HideFlags.HideAndDontSave};owned.Add(camGo);var cam=camGo.AddComponent<Camera>();cam.CopyFrom(src);cam.cameraType=CameraType.Game;cam.enabled=false;var pcl=UnityEngine.Object.FindFirstObjectByType<PCLookScene>();if(pcl!=null)pcl.ApplySky(cam);else{cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.56f,.72f,.90f,1);}
cam.farClipPlane=2000;cam.nearClipPlane=.05f;
log.AppendLine("terrace4 cameraType="+cam.cameraType+" PCLookScene="+(pcl!=null)+" walkways="+(wk!=null)+" terrainMesh="+GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshFilter>().sharedMesh.name);
void Shoot(string file,Vector3 pos,Vector3 tgt,float fov){cam.fieldOfView=fov;camGo.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
 if(!PitCrewPlan.PlanIO.RenderPng(cam,outDir+file,1280,720,null))throw new System.Exception("Capture failed "+file);log.AppendLine(file+" pos="+pos.ToString("F2")+" target="+tgt.ToString("F2")+" fov="+fov);}
var rot=Quaternion.Euler(0,10,0);
Shoot("f1_high.png",new Vector3(196.881f,53.714f,-81.658f),new Vector3(197.27f,-95.469f,39.031f),60);
Shoot("f2_eye.png",new Vector3(190,Ground(190,-51)+1.65f,-51),new Vector3(198,Ground(198,-57)+.20f,-57),78);
{var tp=new Vector3(198,Ground(198,-57)+15f,-57);camGo.transform.SetPositionAndRotation(tp,Quaternion.LookRotation(Vector3.down,rot*Vector3.forward));cam.fieldOfView=40;PitCrewPlan.PlanIO.RenderPng(cam,outDir+"f3_top.png",1280,720,null);log.AppendLine("f3_top.png pos="+tp.ToString("F2")+" down fov=40");}
Shoot("f4_cart.png",new Vector3(195f,Ground(195f,-66f)+1.0f,-66f),new Vector3(198,Ground(198,-57f)+.35f,-57f),70);
Shoot("f5_edge.png",new Vector3(193.5f,Ground(193.5f,-61.5f)+.55f,-61.5f),new Vector3(198,Ground(198,-57)+.1f,-57),70);
var house=new Vector3(185,27,-61);int k=1;
foreach(var x in new[]{170f,188f,206f}){var r=RoadAt(x);var p=r+Vector3.up*1.0f;var dir=new Vector3(1,0,0);var t=Vector3.Lerp(p+dir*20f,house,.55f);Shoot("c"+k+"_cart.png",p,t,70);k++;}
{var tp=new Vector3(185,24.4f+110f,-61);camGo.transform.SetPositionAndRotation(tp,Quaternion.LookRotation(Vector3.down,Vector3.forward));cam.fieldOfView=60;PitCrewPlan.PlanIO.RenderPng(cam,outDir+"t1_top.png",1280,720,null);log.AppendLine("t1_top.png pos="+tp.ToString("F2")+" down fov=60");}
// 걷기 3컷 (눈 1.6 m): 집 마당 계단 머리 → 큰길 보도 → 밭 골목 계단 바닥
Vector3 Eye(float x,float z){return new Vector3(x,Now(x,z)+1.6f,z);}
// f5 는 v2 땅 +0.55 m 라 밭 단(+0.5)을 올린 뒤엔 낮은 돌담 속에 들어간다 → 같은 자리·같은 방향, 지금 땅 기준 +0.55 m 를 따로 찍음
Shoot("f5b_edge_now.png",new Vector3(193.5f,Now(193.5f,-61.5f)+.55f,-61.5f),new Vector3(198,Now(198,-57)+.1f,-57),70);
Shoot("w1_gate.png",Eye(187.4f,-68.7f),new Vector3(201.9f,19.96f+1.0f,-76.2f),70);
Shoot("w2_walk.png",Eye(204f,-76.9f),new Vector3(216f,Now(216f,-76.2f)+1.4f,-76.2f),70);
Shoot("w3_alley.png",Eye(211.0f,-75.6f),new Vector3(197.8f,25.8f+1.2f,-61.0f),70);
// 4차: 마당 눈높이 2컷(지금 땅 +1.6 m). y1 = 문 앞 마당에서 텃밭 쪽, y2 = 텃밭 남동 모서리(골목 계단 머리)에서 집·흙길 쪽
var hT=GameObject.Find("D198_Buildings/house_home").transform;var hq=Quaternion.Euler(0,hT.eulerAngles.y,0);
Vector3 HLw(float lx,float lz){var w=hT.position+hq*new Vector3(lx,0,lz);return w;}
{var a=HLw(1.2f,7.2f);var b=HLw(-9.6f,.25f);Shoot("y1_yard_door.png",Eye(a.x,a.z),new Vector3(b.x,Now(b.x,b.z)+.3f,b.z),70);}
{var a=HLw(-12.4f,5.4f);var b=HLw(-2f,2.5f);Shoot("y2_yard_garden.png",Eye(a.x,a.z),new Vector3(b.x,Now(b.x,b.z)+.9f,b.z),70);}
}finally{
 for(int i=owned.Count-1;i>=0;i--)if(owned[i])UnityEngine.Object.DestroyImmediate(owned[i]);
 log.AppendLine("dirty="+scene.isDirty+" playing="+Application.isPlaying);
 System.IO.File.WriteAllText(outDir+"render_evidence.txt",log.ToString());
}
return log.ToString();
