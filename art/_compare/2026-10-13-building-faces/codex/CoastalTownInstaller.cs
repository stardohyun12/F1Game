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
 /// <summary>tile2>=0 이면 WorldSurface 의 덩어리 무늬(_Patch)로 두 번째 칸을 섞는다 (3차: 잔디·흙·모래 변화).</summary>
 static Material Mat(string name,Color tint,int tile=-1,float repeat=.5f,int tile2=-1,float patch=.55f,float patchScale=.09f)
 {
  string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find(tile>=0?"PitCrew/CoastalTown/WorldSurface":"PitCrew/LofiPilot/Surface");
  if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;
  m.SetColor("_Tint",tint);m.SetColor("_Ambient",new Color(1,.99f,.95f));m.SetFloat("_FaceShade",.65f);m.SetFloat("_FogStart",45);m.SetFloat("_FogEnd",165);m.SetColor("_FogColor",new Color(.62f,.73f,.77f));
  if(tile>=0){m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Kit+"/LofiAtlas.png"));m.SetVector("_Tile",new Vector4(tile%8/8f,tile/8/8f,.125f,.125f));m.SetFloat("_WorldScale",repeat);
   if(m.HasProperty("_Patch")){m.SetFloat("_Patch",tile2>=0?1:0);m.SetFloat("_PatchThreshold",patch);m.SetFloat("_PatchScale",patchScale);if(tile2>=0)m.SetVector("_Tile2",new Vector4(tile2%8/8f,tile2/8/8f,.125f,.125f));}}
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
   Add(asset+(asset=="CoastalHouse"?new[]{"","B","C"}[(hash/7)%3]:new[]{"","B"}[(hash/7)%2]),p,yaw,scale,hash%3); // Keep base asset for scale/Body/props; vary only the prefab selection.
   if(distance==1&&hash%3==0){Add("CoastalTree",p-right*2.15f-face*.9f,hash%360,.78f);}
   if(distance==1&&hash%5==1){Add("Wall3m",p-right*2.4f-face*.6f,yaw+90,.65f);}
   // 2026-10-13 Claude(브리프 town-density ①): 건물당 생활 소품 5개 안팎. 옆이 도로/건물이 아닐 때만 옆 소품.
   if(distance==1){
    Vector2Int rd=new Vector2Int((int)Mathf.Round(right.x),(int)Mathf.Round(right.z));
    bool rightOpen=!map.IsRoad(c+rd.x,r+rd.y)&&map.BuildingChar(c+rd.x,r+rd.y)=='.';
    bool leftOpen=!map.IsRoad(c-rd.x,r-rd.y)&&map.BuildingChar(c-rd.x,r-rd.y)=='.';
    CoastalTownDensity.ForBuilding((a2,pos,yw,sc,pal)=>Add(a2,pos,yw,sc,pal),asset,p,face,right,scale,hash,zone,rightOpen,leftOpen);
   }
  }
  CoastalTownDensity.ForRoads((a2,pos,yw,sc,pal)=>Add(a2,pos,yw,sc,pal),map);   // ②: 횡단보도·맨홀·배수구 데칼 + 3차 연석·얼룩·보도 가구
  CoastalTownDensity.ForGround((a2,pos,yw,sc,pal)=>Add(a2,pos,yw,sc,pal),map);  // 3차: 빈 마당 풀·꽃·덤불·바위·가로수
  CoastalTownDensity.ForHarbor((a2,pos,yw,sc,pal)=>Add(a2,pos,yw,sc,pal),map);  // 3차: 석축·계류·잔교·배
  CoastalTownDensity.ForOutskirts((a2,pos,yw,sc,pal)=>Add(a2,pos,yw,sc,pal),map); // 3차: 마을 밖 나무 띠
  for(int r=0;r<map.Height;r++)for(int c=0;c<map.Width;c++)
  {
   if(!map.IsRoad(c,r)||(c+r)%4!=0)continue;
   foreach(var d in Directions){if(map.IsRoad(c+d.x,r+d.y))continue;var pos=new Vector3(c*6+d.x*2.64f,.12f,r*6+d.y*2.64f);Add("UtilityPole",pos,Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg,.65f);break;}
  }
  data.placements=list.ToArray();AssetDatabase.CreateAsset(data,Dir+"/TownLayout.asset");return data;
 }
 /// <summary>레이아웃 캐시(TownLayout.asset)를 버리고 규칙대로 다시 만든 뒤 설치. 소품 규칙 바꿨을 때 이걸 쓴다.</summary>
 [MenuItem("PitCrew/Art/Coastal Town/Rebuild Layout + Install")]
 public static void RebuildAndInstall(){AssetDatabase.DeleteAsset(Dir+"/TownLayout.asset");Install();}

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
   var b=Bounds(go);if(!CoastalTownDensity.Mounted.Contains(p.asset)&&Mathf.Abs(b.min.y-p.position.y)>.03f)issues.Add(go.name+" base "+b.min.y);
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
  Assign(GameObject.Find("Track"),Mat("Asphalt",new Color(.92f,.94f,.97f),12,.65f,43,.5f,.3f));   // 3차-c: 덩어리 11m→3.3m (게이트 "차보다 큰 모자이크") // 3차-b: 아스팔트 + 균열·얼룩 덩어리(asphalt2) — 넓은 면이 민무늬 한 장으로 보이던 것
  Assign(GameObject.Find("Sidewalks"),Mat("Pavement",new Color(1,.98f,.90f),1,.75f));
  Assign(GameObject.Find("Ground"),Mat("Ground",Color.white,13,.45f,42,.56f));   // 잔디 + 짙은 잔디 덩어리
  // The legacy plane covered only 100x100m while roads span 180x120m.
  // Extend the existing flat collision floor to include every street and its verge.
  var floor=GameObject.Find("Ground");var floorBounds=floor.GetComponent<Renderer>().bounds;var floorScale=floor.transform.localScale;
  floorScale.x*=(map.Width*6+12)/floorBounds.size.x;floorScale.z*=(map.Height*6+12)/floorBounds.size.z;floor.transform.localScale=floorScale;
  floor.transform.position=new Vector3((map.Width-1)*3,0,(map.Height-1)*3);
  // 3차: 구역별 바닥 — 공업=흙(풀 덩어리) / 다운타운=잔디(짙은 덩어리) / 항구=모래(흙 덩어리). 경계는 2칸 폭 도로 밑에 숨는다.
  var zones=GameObject.Find("ZoneGrounds");if(zones)foreach(Transform z in zones.transform)Assign(z.gameObject,
   z.name.Contains("Industrial")?Mat("ZoneGroundIndustrial",new Color(1,.97f,.92f),37,.45f,42,.62f):z.name.Contains("Harbor")?Mat("ZoneGroundHarbor",Color.white,38,.45f,37,.66f):Mat("ZoneGroundDowntown",Color.white,13,.45f,42,.56f));
  // 3차: 땅 끝 너머 원경 바닥 — 안개 끝(190m)까지 하늘이 바로 보이지 않게. 충돌 없음, y=-0.06 (나무 띠와 같은 높이).
  var far=GameObject.CreatePrimitive(PrimitiveType.Plane);far.name="FarGround";far.transform.SetParent(town.transform);UnityEngine.Object.DestroyImmediate(far.GetComponent<Collider>());
  far.transform.position=new Vector3((map.Width-1)*3,-.06f,-270);far.transform.localScale=new Vector3(140,1,78);   // z ≤ 120: 바다(HarborSea y=-0.15) 는 덮지 않는다
  Assign(far,Mat("FarGround",new Color(.93f,.97f,1),13,.45f,42,.5f));
  var lines=GameObject.Find("RoadLines");var lineMat=Mat("RoadPaint",new Color(.80f,.77f,.64f));Assign(lines,lineMat);
  foreach(Transform dash in lines.transform){var s=dash.localScale;if(s.x>s.z){s.x=1.8f;s.z=.1f;}else{s.z=1.8f;s.x=.1f;}dash.localScale=s;}
  var vehicles=GameObject.Find("StreetVehicles");if(vehicles)foreach(Transform v in vehicles.transform){if(!v.name.EndsWith("_CoastalSized")){v.localScale*=.78f;v.name+="_CoastalSized";}}
  var sea=GameObject.Find("HarborSea");if(sea)Assign(sea,Mat("Sea",new Color(.48f,.66f,.71f)));
  // Background shapes only; no collision or route changes.
  var water=GameObject.CreatePrimitive(PrimitiveType.Cube);water.name="CoastalWater";water.transform.SetParent(town.transform);water.transform.position=new Vector3(87,-.02f,164);water.transform.localScale=new Vector3(240,.08f,88);UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());Assign(water,Mat("Sea",new Color(.48f,.66f,.71f)));
  BuildIslands(town.transform);BuildHills(town.transform);
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
 /// <summary>3차: 마을 밖 세 방향(서·남·동)에 낮은 언덕 능선 — 02 외곽 캡처에서 땅 끝이 하늘과 바로 만나던 것. 안개(55~190m) 속 실루엣.</summary>
 static void BuildHills(Transform parent)
 {
  var mat=Mat("DistantHills",new Color(.47f,.61f,.66f));
  var spots=new List<(float x,float z,float rx,float rz,float h)>();
  foreach(float z in new[]{-70f,-15f,40f,95f})spots.Add((-110f+(z%3)*4,z,62f,40f,16f+(spots.Count%3)*4));
  foreach(float x in new[]{-60f,10f,80f,150f,220f})spots.Add((x,-100f,70f,42f,14f+(spots.Count%3)*5));
  foreach(float z in new[]{-70f,-15f,40f,95f})spots.Add((290f,z,62f,40f,15f+(spots.Count%3)*4));
  int k=0;
  foreach(var s in spots){
   var vertices=new List<Vector3>();for(int ring=0;ring<2;ring++)for(int j=0;j<11;j++){float a=j*Mathf.PI*2/11;float rad=ring==0?1:.5f;vertices.Add(new Vector3(Mathf.Cos(a)*s.rx*rad,ring==0?0:s.h*.45f+(j*5+k*3)%4,Mathf.Sin(a)*s.rz*rad));}
   vertices.Add(new Vector3(s.rx*.1f,s.h,-s.rz*.05f));
   var tri=new List<int>();for(int j=0;j<11;j++){int n=(j+1)%11;tri.AddRange(new[]{j,11+n,n,j,11+j,11+n,22,11+n,11+j});}
   var mesh=new Mesh{name="Hill"+k};mesh.SetVertices(vertices);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();
   string path=Dir+"/Hill"+k+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
   var go=new GameObject("Hill"+k);go.transform.SetParent(parent);go.transform.position=new Vector3(s.x,-.3f,s.z);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;k++;
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
