// terrace6 캡처: 눈높이 1.6 m 걷기 컷(대문 → 집 계단 아래 → 보도 중간 → 차고 계단 발치 → 골목 → 차고 입구) + 위에서 직교 1장(경로선은 sheet.py 가 그림)
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string outDir="D:/Github/F1Game/docs/qa/evidence/terrace6/";System.IO.Directory.CreateDirectory(outDir);
var owned=new System.Collections.Generic.List<UnityEngine.Object>();var log=new System.Text.StringBuilder();
var cols=new System.Collections.Generic.List<Collider>();cols.Add(GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>());
var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways");if(wk)cols.Add(wk.GetComponent<MeshCollider>());
foreach(var c in GameObject.Find("D198_Buildings/D198_garage").GetComponentsInChildren<Collider>())if(c.name.Contains("shell")||c.name.Contains("GARAGE"))cols.Add(c);
float Now(float x,float z){float y=float.NegativeInfinity;foreach(var c in cols){RaycastHit h;if(c&&c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000))y=Mathf.Max(y,h.point.y);}if(float.IsInfinity(y))throw new System.Exception("miss "+x+","+z);return y;}
Vector3 Eye(float x,float z){return new Vector3(x,Now(x,z)+1.6f,z);}
Vector3 At(float x,float z,float up){return new Vector3(x,Now(x,z)+up,z);}
var hT=GameObject.Find("D198_Buildings/house_home").transform;var hq=Quaternion.Euler(0,hT.eulerAngles.y,0);
Vector2 HL(float lx,float lz){var w=hT.position+hq*new Vector3(lx,0,lz);return new Vector2(w.x,w.z);}
try{
var sview=UnityEditor.SceneView.lastActiveSceneView;var src=sview!=null?sview.camera:Camera.main;if(!src)throw new System.Exception("No source camera");
var camGo=new GameObject("_T6CAP_CAMERA"){hideFlags=HideFlags.HideAndDontSave};owned.Add(camGo);var cam=camGo.AddComponent<Camera>();cam.CopyFrom(src);cam.cameraType=CameraType.Game;cam.enabled=false;
var pcl=UnityEngine.Object.FindFirstObjectByType<PCLookScene>();if(pcl!=null)pcl.ApplySky(cam);else{cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.56f,.72f,.90f,1);}
cam.farClipPlane=2000;cam.nearClipPlane=.05f;
void Shoot(string file,Vector3 pos,Vector3 tgt,float fov){cam.orthographic=false;cam.fieldOfView=fov;camGo.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
 if(!PitCrewPlan.PlanIO.RenderPng(cam,outDir+file,1280,720,null))throw new System.Exception("Capture failed "+file);log.AppendLine(file+" pos="+pos.ToString("F2")+" target="+tgt.ToString("F2")+" fov="+fov);}
var door=HL(-.7f,5.12f);
Shoot("w1_gate.png",Eye(door.x,door.y),At(196.6f,-70.5f,.8f),70);
Shoot("w2_stair_foot.png",Eye(197.6f,-77.6f),At(211f,-75.4f,1.2f),70);
Shoot("w3_walk_mid.png",Eye(206f,-76.4f),At(215.6f,-72.0f,1.6f),70);
Shoot("w4_garage_stair.png",Eye(215.6f,-75.2f),At(215.5f,-65.5f,1.5f),70);
Shoot("w5_alley.png",Eye(215.6f,-64.3f),At(232f,-63.3f,1.2f),70);
Shoot("w6_garage_door.png",Eye(231.5f,-62.0f),At(223.5f,-53.5f,1.4f),70);
{cam.orthographic=true;cam.orthographicSize=22.5f;cam.farClipPlane=400;cam.nearClipPlane=.1f;camGo.transform.SetPositionAndRotation(new Vector3(215,150,-64),Quaternion.LookRotation(Vector3.down,Vector3.forward));
 PitCrewPlan.PlanIO.RenderPng(cam,outDir+"top_raw.png",1600,900,null);log.AppendLine("top_raw.png ortho center(215,-64) 80x45m 20px/m right=+x up=+z");}
}finally{
 for(int i=owned.Count-1;i>=0;i--)if(owned[i])UnityEngine.Object.DestroyImmediate(owned[i]);
 log.AppendLine("dirty="+scene.isDirty+" playing="+Application.isPlaying);
 System.IO.File.WriteAllText(outDir+"render_evidence.txt",log.ToString());
}
return log.ToString();
