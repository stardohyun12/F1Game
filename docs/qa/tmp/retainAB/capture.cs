// retainAB: 축대 질감 두 안 비교용 캡처. 태그 = docs/qa/tmp/retainAB/tag.txt (cur|A|B). 카메라는 terrace2/capture.cs 의 c1_cart·c2_cart·f1_high + 근접 z1_close(축대+집 기단).
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string tag=System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/retainAB/tag.txt").Trim();
string outDir="D:/Github/F1Game/docs/qa/evidence/retainAB/"+tag+"/";System.IO.Directory.CreateDirectory(outDir);
var owned=new System.Collections.Generic.List<UnityEngine.Object>();var log=new System.Text.StringBuilder();
var v2=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_v2.asset");
var tg=new GameObject("_RAB_col"){hideFlags=HideFlags.HideAndDontSave};owned.Add(tg);var tmc=tg.AddComponent<MeshCollider>();tmc.sharedMesh=v2;
var roads=new System.Collections.Generic.List<Collider>();foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform)if(ch.gameObject.activeInHierarchy&&ch.name.StartsWith("ROAD_V2e")){var c=ch.GetComponent<Collider>();if(c)roads.Add(c);}
Vector3 RoadAt(float x){float z0=float.NaN,z1=float.NaN,y=0;for(float z=-66;z>=-100;z-=.25f){bool hit=false;foreach(var c in roads){RaycastHit h;if(c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000)){hit=true;y=h.point.y;break;}}if(hit){if(float.IsNaN(z0))z0=z;z1=z;}else if(!float.IsNaN(z0))break;}if(float.IsNaN(z0))throw new System.Exception("no road at x="+x);float zm=(z0+z1)/2;foreach(var c in roads){RaycastHit h;if(c.Raycast(new Ray(new Vector3(x,500,zm),Vector3.down),out h,1000)){y=h.point.y;break;}}return new Vector3(x,y,zm);}
try{
var sview=UnityEditor.SceneView.lastActiveSceneView;var src=sview!=null?sview.camera:Camera.main;if(!src)throw new System.Exception("No source camera");
var camGo=new GameObject("_RAB_CAMERA"){hideFlags=HideFlags.HideAndDontSave};owned.Add(camGo);var cam=camGo.AddComponent<Camera>();cam.CopyFrom(src);cam.enabled=false;var pcl=UnityEngine.Object.FindFirstObjectByType<PCLookScene>();if(pcl!=null)pcl.ApplySky(cam);else{cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.56f,.72f,.90f,1);}
cam.farClipPlane=2000;
log.AppendLine("tag="+tag+" PCLookScene="+(pcl!=null)+" A7Shade="+Shader.GetGlobalColor("_A7Shade")+" terrainMesh="+GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshFilter>().sharedMesh.name);
var wmr=GameObject.Find("TerraceBlock/RetainingWalls").GetComponent<MeshRenderer>();var origMats=wmr.sharedMaterials;
if(tag!="cur"){ // 임시 사본 재질·텍스처(에셋 아님, hideFlags 로 씬에 안 남음). finally 에서 원복.
 var caps=System.IO.File.ReadAllLines("D:/Github/F1Game/docs/qa/tmp/retainAB/caps.txt");var cl=caps[tag=="A"?0:1].Split(' ');
 var tex=new Texture2D(2,2,TextureFormat.RGBA32,true){hideFlags=HideFlags.HideAndDontSave};tex.LoadImage(System.IO.File.ReadAllBytes("D:/Github/F1Game/docs/qa/tmp/retainAB/"+tag+"_stone.png"));tex.filterMode=FilterMode.Point;tex.wrapMode=TextureWrapMode.Repeat;tex.anisoLevel=0;owned.Add(tex);
 var wall=new Material(origMats[0]){hideFlags=HideFlags.HideAndDontSave};wall.SetTexture("_MainTex",tex);wall.SetColor("_Tint",Color.white);if(tag=="A")wall.SetTextureScale("_MainTex",new Vector2(0.2f,0.26667f)); // A 는 160×120 px = 5×3.75 mowned.Add(wall);
 var cap=new Material(origMats[1]){hideFlags=HideFlags.HideAndDontSave};cap.SetColor("_Tint",new Color(float.Parse(cl[0]),float.Parse(cl[1]),float.Parse(cl[2]),1));owned.Add(cap);
 wmr.sharedMaterials=new[]{wall,cap};log.AppendLine("swapped tex "+tex.width+"x"+tex.height+" mips="+tex.mipmapCount+" scale="+wall.GetTextureScale("_MainTex"));
}
var wm=wmr.sharedMaterials;foreach(var m in wm)log.AppendLine("wallmat "+m.name+" tex="+(m.mainTexture?m.mainTexture.name+" "+m.mainTexture.width+"x"+m.mainTexture.height+" filter="+m.mainTexture.filterMode:"-")+" scale="+m.mainTextureScale+" col="+m.color);
void Shoot(string file,Vector3 pos,Vector3 tgt,float fov){cam.fieldOfView=fov;camGo.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
 if(!PitCrewPlan.PlanIO.RenderPng(cam,outDir+file,1280,720,null))throw new System.Exception("Capture failed "+file);log.AppendLine(file+" pos="+pos.ToString("F2")+" target="+tgt.ToString("F2")+" fov="+fov);}
Shoot("f1_high.png",new Vector3(196.881f,53.714f,-81.658f),new Vector3(197.27f,-95.469f,39.031f),60);
var house=new Vector3(185,27,-61);int k=1;
foreach(var x in new[]{170f,188f}){var r=RoadAt(x);var p=r+Vector3.up*1.0f;var dir=new Vector3(1,0,0);var t=Vector3.Lerp(p+dir*20f,house,.55f);Shoot("c"+k+"_cart.png",p,t,70);k++;}
{var r=RoadAt(186f);var p=r+Vector3.up*1.3f;Shoot("z1_close.png",p,new Vector3(189.5f,24.6f,-67f),50);}
{var r=RoadAt(196f);var p=r+Vector3.up*1.3f+new Vector3(0,0,6f);Shoot("z2_close.png",p,new Vector3(190f,24.3f,-66.5f),45);}
}finally{
 var wmr2=GameObject.Find("TerraceBlock/RetainingWalls");if(wmr2!=null){var mr2=wmr2.GetComponent<MeshRenderer>();var o=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/Map_TownV6_Mats/TV6_Retain.mat");var oc=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/Map_TownV6_Mats/TV6_RetainCap.mat");mr2.sharedMaterials=new[]{o,oc};log.AppendLine("restored "+mr2.sharedMaterials[0].name+","+mr2.sharedMaterials[1].name);}
 for(int i=owned.Count-1;i>=0;i--)if(owned[i])UnityEngine.Object.DestroyImmediate(owned[i]);
 log.AppendLine("dirty="+scene.isDirty+" playing="+Application.isPlaying);
 System.IO.File.WriteAllText(outDir+"render_evidence.txt",log.ToString());
}
return log.ToString();
