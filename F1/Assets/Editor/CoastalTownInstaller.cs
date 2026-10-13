using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CoastalTownInstaller
{
 const string Dir="Assets/Art/CoastalTown";
 const string Kit="Assets/Art/LofiPilot";
 static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/source/coastal-town"));
 static readonly Vector2Int[] Directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
 static Bounds Bounds(GameObject go){var rr=go.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
 static Material Mat(string name,Color tint,int tile=-1,float repeat=.5f)
 {
  string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find(tile>=0?"PitCrew/CoastalTown/WorldSurface":"PitCrew/LofiPilot/Surface");
  if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;
  m.SetColor("_Tint",tint);m.SetColor("_Ambient",new Color(1,.99f,.95f));m.SetFloat("_FaceShade",.65f);m.SetFloat("_FogStart",45);m.SetFloat("_FogEnd",165);m.SetColor("_FogColor",new Color(.62f,.73f,.77f));
  if(tile>=0){m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Kit+"/LofiAtlas.png"));m.SetVector("_Tile",new Vector4(tile%8/8f,tile/8/8f,.125f,.125f));m.SetFloat("_WorldScale",repeat);}
  else if(name.StartsWith("Buildings"))m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Kit+"/LofiAtlas.png"));
  EditorUtility.SetDirty(m);return m;
 }
 static void Assign(GameObject root,Material m){if(root)foreach(var r in root.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>m).ToArray();}
 static CoastalTownLayout Layout()
 {
  var data=AssetDatabase.LoadAssetAtPath<CoastalTownLayout>(Dir+"/TownLayout.asset");if(data)return data;
  data=ScriptableObject.CreateInstance<CoastalTownLayout>();data.streets=AssetDatabase.LoadAssetAtPath<TrackLayout>("Assets/Data/ComplexTrackLayout.asset");
  var list=new List<CoastalTownLayout.Placement>();var map=data.streets;
  void Add(string asset,Vector3 pos,float yaw,float scale,int palette=0)=>list.Add(new CoastalTownLayout.Placement{asset=asset,position=pos,yaw=yaw,scale=Vector3.one*scale,palette=palette});
  for(int r=0;r<map.Height;r++)for(int c=0;c<map.Width;c++)
  {
   if(map.IsRoad(c,r)||map.BuildingChar(c,r)=='.')continue;
   int hash=Math.Abs(c*157+r*73);Vector2Int dir=Vector2Int.up;int distance=99;
   foreach(var d in Directions)for(int k=1;k<=3;k++)if(map.IsRoad(c+d.x*k,r+d.y*k)&&k<distance){distance=k;dir=d;}
   if(distance==99)continue;
   float yaw=Mathf.Atan2(dir.x,dir.y)*Mathf.Rad2Deg;Vector3 face=new Vector3(dir.x,0,dir.y),right=new Vector3(dir.y,0,-dir.x);var p=new Vector3(c*6,0,r*6);
   char zone=map.GetZoneChar(c,r);
   string asset=zone=='I'?(hash%3==0?"CoastalHouse":"RepairGarage"):(hash%5==0?"CornerShop":"CoastalHouse");
   float scale=asset=="RepairGarage"?.64f:(asset=="CornerShop"?.64f:.62f+hash%3*.035f);
   Add(asset,p,yaw,scale,hash%3);
   if(distance==1&&hash%3==0){Add("CoastalTree",p-right*2.15f-face*.9f,hash%360,.78f);}
   if(distance==1&&hash%4==0){Add(asset=="RepairGarage"?"TireStack":"Crate",p+face*2.55f+right*1.2f,yaw,.65f);}
   if(distance==1&&hash%5==1){Add("Wall3m",p-right*2.4f-face*.6f,yaw+90,.65f);}
  }
  for(int r=0;r<map.Height;r++)for(int c=0;c<map.Width;c++)
  {
   if(!map.IsRoad(c,r)||(c+r)%4!=0)continue;
   foreach(var d in Directions){if(map.IsRoad(c+d.x,r+d.y))continue;var pos=new Vector3(c*6+d.x*2.64f,.12f,r*6+d.y*2.64f);Add("UtilityPole",pos,Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg,.65f);break;}
  }
  data.placements=list.ToArray();AssetDatabase.CreateAsset(data,Dir+"/TownLayout.asset");return data;
 }
 [MenuItem("PitCrew/Art/Coastal Town/Install")]
 public static void Install()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode");var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/ComplexTrack.unity"||scene.isDirty)throw new Exception("Use saved ComplexTrack scene");
  Directory.CreateDirectory(Output);Directory.CreateDirectory(Dir);var backup=Output+"/ComplexTrack.before-town.unity";if(!File.Exists(backup))File.Copy(scene.path,backup);
  var data=Layout();var map=data.streets;var network=TrackConnectivityVerifier.Verify(map,"CoastalTown");if(!network.ok)throw new Exception("Road topology invalid before install");
  // Preserve the original collision surfaces and navigation anchors; use a new facade layout.
  foreach(string name in new[]{"Buildings","Props","StreetFurniture","GeneratedPilot"}){var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==name);if(old)old.SetActive(false);}
  var existing=GameObject.Find("CoastalTown");if(existing)UnityEngine.Object.DestroyImmediate(existing);
  var town=new GameObject("CoastalTown");var mats=new[]{Mat("BuildingsSand",Color.white),Mat("BuildingsCool",new Color(.89f,.97f,1)),Mat("BuildingsWarm",new Color(1,.94f,.85f))};
  int count=0;var issues=new List<string>();var poles=new List<Transform>();
  foreach(var p in data.placements)
  {
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"/"+p.asset+".prefab");if(!prefab)throw new Exception("Missing "+p.asset);
   var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.name=p.asset+"_"+count++;go.transform.SetParent(town.transform,false);go.transform.SetPositionAndRotation(p.position,Quaternion.Euler(0,p.yaw,0));go.transform.localScale=p.scale;Assign(go,mats[p.palette]);
   if(p.asset=="CoastalTree"){
    foreach(var col in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(col);
    var trunk=go.AddComponent<CapsuleCollider>();trunk.radius=.15f;trunk.height=2.6f;trunk.center=new Vector3(0,1.3f,0);
   }
   var b=Bounds(go);if(Mathf.Abs(b.min.y-p.position.y)>.03f)issues.Add(go.name+" base "+b.min.y);
   if(p.asset=="UtilityPole")poles.Add(go.transform);
  }
  // Connections only between nearby aligned poles, with a modest hanging curve.
  var cable=Mat("Cable",new Color(.22f,.25f,.24f));
  for(int i=0;i<poles.Count;i++)for(int j=i+1;j<poles.Count;j++){
   Vector3 a=poles[i].position+Vector3.up*3.9f,b=poles[j].position+Vector3.up*3.9f;float distance=Vector3.Distance(a,b);
   if(distance<10||distance>25||Mathf.Min(Mathf.Abs(a.x-b.x),Mathf.Abs(a.z-b.z))>.1f)continue;
   var go=new GameObject("PowerLine");go.transform.SetParent(town.transform);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=cable;line.startWidth=line.endWidth=.014f;line.positionCount=9;
   for(int n=0;n<9;n++){float t=n/8f;line.SetPosition(n,Vector3.Lerp(a,b,t)-Vector3.up*(Mathf.Sin(t*Mathf.PI)*.35f));}
  }
  Assign(GameObject.Find("Track"),Mat("Asphalt",new Color(.92f,.94f,.97f),12,.65f));
  Assign(GameObject.Find("Sidewalks"),Mat("Pavement",new Color(1,.98f,.90f),1,.75f));
  Assign(GameObject.Find("Ground"),Mat("Ground",Color.white,13,.45f));
  // The legacy plane covered only 100x100m while roads span 180x120m.
  // Extend the existing flat collision floor to include every street and its verge.
  var floor=GameObject.Find("Ground");var floorBounds=floor.GetComponent<Renderer>().bounds;var floorScale=floor.transform.localScale;
  floorScale.x*=(map.Width*6+12)/floorBounds.size.x;floorScale.z*=(map.Height*6+12)/floorBounds.size.z;floor.transform.localScale=floorScale;
  floor.transform.position=new Vector3((map.Width-1)*3,0,(map.Height-1)*3);
  var zones=GameObject.Find("ZoneGrounds");if(zones)foreach(Transform z in zones.transform)Assign(z.gameObject,Mat("ZoneGround",Color.white,13,.45f));
  var lines=GameObject.Find("RoadLines");var lineMat=Mat("RoadPaint",new Color(.80f,.77f,.64f));Assign(lines,lineMat);
  foreach(Transform dash in lines.transform){var s=dash.localScale;if(s.x>s.z){s.x=1.8f;s.z=.1f;}else{s.z=1.8f;s.x=.1f;}dash.localScale=s;}
  var vehicles=GameObject.Find("StreetVehicles");if(vehicles)foreach(Transform v in vehicles.transform){if(!v.name.EndsWith("_CoastalSized")){v.localScale*=.78f;v.name+="_CoastalSized";}}
  var sea=GameObject.Find("HarborSea");if(sea)Assign(sea,Mat("Sea",new Color(.48f,.66f,.71f)));
  // Background shapes only; no collision or route changes.
  var water=GameObject.CreatePrimitive(PrimitiveType.Cube);water.name="CoastalWater";water.transform.SetParent(town.transform);water.transform.position=new Vector3(87,-.02f,164);water.transform.localScale=new Vector3(240,.08f,88);UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());Assign(water,Mat("Sea",new Color(.48f,.66f,.71f)));
  BuildIslands(town.transform);
  var light=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()).FirstOrDefault(l=>l.type==LightType.Directional);if(light){light.transform.rotation=Quaternion.Euler(48,-35,0);light.color=new Color(1,.96f,.86f);light.intensity=1.05f;light.shadows=LightShadows.Soft;}
  RenderSettings.ambientLight=new Color(.68f,.72f,.73f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.62f,.73f,.77f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=55;RenderSettings.fogEndDistance=190;
  var camera=UnityEngine.Object.FindFirstObjectByType<FollowCamera>();var so=new SerializedObject(camera);so.FindProperty("offset").vector3Value=data.cameraOffset;so.FindProperty("lookHeight").floatValue=data.cameraLookHeight;so.ApplyModifiedPropertiesWithoutUndo();
  Physics.SyncTransforms();int samples=0;
  for(int r=0;r<map.Height;r++)for(int c=0;c<map.Width;c++)if(map.IsRoad(c,r)){
   CheckRoad(new Vector3(c*6,0,r*6),ref samples,issues);
   foreach(var d in new[]{Vector2Int.up,Vector2Int.right})if(map.IsRoad(c+d.x,r+d.y))foreach(float offset in new[]{2.99f,3f,3.01f})CheckRoad(new Vector3(c*6+d.x*offset,0,r*6+d.y*offset),ref samples,issues);
  }
  var shaderErrors=ShaderUtil.GetShaderMessages(Shader.Find("PitCrew/CoastalTown/WorldSurface")).Where(m=>m.severity.ToString()=="Error").ToArray();
  File.WriteAllText(Output+"/validation.txt","Buildings and props="+count+"\nRoad cells="+network.totalRoad+" reachable="+network.reachable+"\nRoad collision samples="+samples+"\nShader errors="+shaderErrors.Length+"\nIssues="+string.Join(";",issues));
  if(issues.Count>0||shaderErrors.Length>0)throw new Exception("Town verification failed: "+string.Join(";",issues.Take(8)));
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }
 static void CheckRoad(Vector3 p,ref int samples,List<string> issues){samples++;if(!Physics.Raycast(p+Vector3.up*8,Vector3.down,out var hit,12,~0,QueryTriggerInteraction.Ignore)){issues.Add("No road at "+p);return;}if(hit.collider.GetComponentInParent<ArcadeCarController>())return;if(hit.point.y>.2f||hit.point.y<-.1f)issues.Add("Road blocked/step "+p+" y="+hit.point.y+" by "+hit.collider.name);}
 static void BuildIslands(Transform parent)
 {
  var mat=Mat("DistantHills",new Color(.47f,.61f,.66f));
  for(int k=0;k<7;k++){
   var vertices=new List<Vector3>();for(int ring=0;ring<2;ring++)for(int j=0;j<9;j++){float a=j*Mathf.PI*2/9;float rad=ring==0?1:.55f;vertices.Add(new Vector3(Mathf.Cos(a)*(17+k%3*4)*rad,ring==0?0:3+(j*7+k*3)%5,Mathf.Sin(a)*11*rad));}
   vertices.Add(new Vector3(-2,6+k%3,1));
   var tri=new List<int>();for(int j=0;j<9;j++){int n=(j+1)%9;tri.AddRange(new[]{j,9+n,n,j,9+j,9+n,18,9+n,9+j});}
   var mesh=new Mesh{name="Island"+k};mesh.SetVertices(vertices);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();
   string path=Dir+"/Island"+k+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
   var go=new GameObject("Island"+k);go.transform.SetParent(parent);go.transform.position=new Vector3(15+k*27,-.2f,191+k%2*12);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
  }
 }
 public static void Capture()
 {
  Directory.CreateDirectory(Output);ArtCapture.CaptureCar(Output+"/car-street",1200,800);ArtCapture.CaptureChaseAt(Output+"/harbor.png",60,101,0,1200,700);
  var go=new GameObject("TownInspectionCamera");var cam=go.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.Skybox;cam.nearClipPlane=.05f;cam.farClipPlane=400;cam.orthographic=true;
  var materials=AssetDatabase.FindAssets("t:Material",new[]{Dir}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
  bool fog=RenderSettings.fog;
  try{
   RenderSettings.fog=false;foreach(var m in materials){m.SetFloat("_FogStart",1000);m.SetFloat("_FogEnd",2000);}
   // Local capture helper writes to the town directory.
   cam.orthographicSize=100;cam.transform.position=new Vector3(87,170,60);cam.transform.rotation=Quaternion.Euler(90,0,0);Save(cam,"topdown",1400,1100);
   cam.orthographicSize=11;cam.transform.position=new Vector3(93,6,57);cam.transform.LookAt(new Vector3(72,2,57));Save(cam,"side",1200,750);
   cam.orthographic=false;cam.fieldOfView=48;cam.transform.position=new Vector3(72.4f,.85f,48.8f);cam.transform.LookAt(new Vector3(72.5f,.01f,51));Save(cam,"road-seam",1000,650);
  }finally{RenderSettings.fog=fog;foreach(var m in materials){m.SetFloat("_FogStart",45);m.SetFloat("_FogEnd",165);}UnityEngine.Object.DestroyImmediate(go);}
 }
 public static void Save(Camera cam,string name,int w=1200,int h=750){var old=RenderTexture.active;var rt=new RenderTexture(w,h,24);var tex=new Texture2D(w,h,TextureFormat.RGB24,false);try{cam.targetTexture=rt;cam.aspect=(float)w/h;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(Output+"/"+name+".png",tex.EncodeToPNG());}finally{cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
}
