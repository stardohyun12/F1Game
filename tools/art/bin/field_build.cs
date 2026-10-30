// Review-only geometric concept. Never writes through Library/SceneA7 junctions.
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets") || Application.isPlaying) throw new System.Exception("Studio edit mode required");
string dir = "Assets/Art/Review/field_patch";
System.IO.Directory.CreateDirectory(dir);
var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (current.isDirty && current.path != dir + "/FieldMock.unity") throw new System.Exception("Unsaved unrelated scene");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Studio_Building.unity");
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), dir + "/FieldMock.unity", true);
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(dir + "/FieldMock.unity");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var studio = GameObject.Find("STUDIO");
foreach (var r in studio.GetComponentsInChildren<Renderer>(true)) r.gameObject.SetActive(false);
foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) c.gameObject.SetActive(false);
var root = new GameObject("FIELD_REVIEW_ONLY");
var shader = Shader.Find("PitCrew/SceneA7/Lit");
if (!shader) throw new System.Exception("SceneA7 missing");
int serial = 0;
void SaveAsset(UnityEngine.Object a, string filename) {
    string p = dir + "/" + filename;
    var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
    if(existing) { UnityEditor.EditorUtility.CopySerialized(a, existing); UnityEngine.Object.DestroyImmediate(a); }
    else UnityEditor.AssetDatabase.CreateAsset(a,p);
}
Material Mat(string name, Color c, float blotch) {
    var m = new Material(shader) {name=name};
    m.SetColor("_Tint",c); m.SetFloat("_A7Lit",1); m.SetFloat("_Outline",0); m.SetFloat("_R3Ok",0); m.SetFloat("_SN",0);
    m.SetFloat("_S1",0);m.SetFloat("_S2",0);m.SetFloat("_S3",0);m.SetFloat("_Ramp",0);m.SetFloat("_LightAmt",0);
    m.SetFloat("_Blotch",blotch);m.SetFloat("_BlotchSize",2.4f);m.SetColor("_BlotchHue",Color.white);
    m.SetFloat("_FogStart",90);m.SetFloat("_FogEnd",950);m.SetFloat("_FogMax",.55f);m.SetColor("_FogColor",new Color(.74f,.83f,.9f));
    SaveAsset(m,name+".mat"); return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(dir+"/"+name+".mat");
}
// Shader color properties receive sRGB colors; avoid applying .linear twice.
Color Sample(int r,int g,int b) {var c=new Color(r/255f,g/255f,b/255f); return new Color(c.r,c.g/.86f,c.b/.68f,1);}
var grass=Mat("review_grass",Sample(145,144,94),.10f);
var soil=Mat("review_soil",Sample(145,127,99),.06f);
var soilRidge=Mat("review_ridge",Sample(151,132,104),.07f);
// Exact existing house palette entries c137, c168, c135, c399.
var crop=Mat("review_crop",Sample(102,112,89),.06f);
var cropLight=Mat("review_crop_lighter",Sample(109,120,95),.05f);
var wood=Mat("review_wood",Sample(102,99,91),.04f);
var stone=Mat("review_stone",Sample(153,148,134),.02f);
GameObject MeshObject(string name,Transform parent,Vector3[] vertices,int[] triangles,Material mat) {
    var mesh=new Mesh(){name=name}; mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
    string path="mesh_"+(serial++).ToString("000")+".asset";SaveAsset(mesh,path);
    var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(dir+"/"+path);
    go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go;
}
float Height(float x,float z,bool b) {return b?Mathf.SmoothStep(0,1,(x+2)/5)*(.07f*(z+4)) : 0;}
GameObject Blob(string name,Transform p,Vector3 center,Vector3 size,Material mat,int seed) {
    const int rings=6,sides=12;var vv=new System.Collections.Generic.List<Vector3>();var tt=new System.Collections.Generic.List<int>();
    vv.Add(center+new Vector3(0,size.y,0));
    for(int y=1;y<rings;y++) for(int j=0;j<sides;j++) {float v=Mathf.PI*y/rings,a=2*Mathf.PI*j/sides;
        float noise=1+.09f*Mathf.Sin(a*3+seed)+.06f*Mathf.Cos(v*4+a*2+seed);
        vv.Add(center+Vector3.Scale(new Vector3(Mathf.Sin(v)*Mathf.Cos(a)*noise,Mathf.Cos(v),Mathf.Sin(v)*Mathf.Sin(a)*noise),size));
    }
    int bottom=vv.Count;vv.Add(center-new Vector3(0,size.y,0));
    for(int j=0;j<sides;j++) {int next=(j+1)%sides;tt.AddRange(new[]{0,1+next,1+j,bottom,1+(rings-2)*sides+j,1+(rings-2)*sides+next});}
    for(int y=0;y<rings-2;y++)for(int j=0;j<sides;j++){int next=(j+1)%sides,a=1+y*sides+j,ar=1+y*sides+next;tt.AddRange(new[]{a,ar,ar+sides,a,ar+sides,a+sides});}
    return MeshObject(name,p,vv.ToArray(),tt.ToArray(),mat);
}
GameObject Pole(string name,Transform p,Vector3 bottom,Vector3 top,float radius) {
    var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(p,false);
    go.transform.position=(bottom+top)*.5f;go.transform.up=(top-bottom).normalized;go.transform.localScale=new Vector3(radius*2,(top-bottom).magnitude*.5f,radius*2);
    go.GetComponent<Renderer>().sharedMaterial=wood;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
}
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Library/Buildings/house_home/house_home.prefab");
var localHouseMats=new System.Collections.Generic.Dictionary<Material,Material>();
foreach(bool b in new[]{false,true}) {
    var variant=new GameObject(b?"B_curved_10x7":"A_aligned_8x6");variant.transform.SetParent(root.transform,false);
    var vv=new System.Collections.Generic.List<Vector3>();var tt=new System.Collections.Generic.List<int>();
    int n=42;
    for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){float xx=x==0?-100:(x==n?100:x-21),zz=z==0?-100:(z==n?100:z-21);vv.Add(new Vector3(xx,Height(xx,Mathf.Clamp(zz,-10,10),b)-.025f,zz));}
    for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*(n+1)+x;tt.AddRange(new[]{a,a+n+1,a+1,a+1,a+n+1,a+n+2});}
    MeshObject("olive_ground",variant.transform,vv.ToArray(),tt.ToArray(),grass);
    var house=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);house.name="house_home_reference";house.transform.SetParent(variant.transform,false);
    house.transform.rotation=Quaternion.Euler(0,180,0);
    var br=house.GetComponentsInChildren<Renderer>();var bounds=br[0].bounds;foreach(var r in br)bounds.Encapsulate(r.bounds);
    house.transform.position+=new Vector3(-6.4f-bounds.center.x,-bounds.min.y,-bounds.center.z);
    foreach(var r in br) {var ms=r.sharedMaterials; for(int i=0;i<ms.Length;i++) {
        if(!localHouseMats.ContainsKey(ms[i])) {var m=new Material(ms[i]);string mn="review_house_"+localHouseMats.Count;m.name=mn;m.SetFloat("_Outline",0);m.SetFloat("_SN",0);SaveAsset(m,mn+".mat");localHouseMats[ms[i]]=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(dir+"/"+mn+".mat");}
        ms[i]=localHouseMats[ms[i]];
    }r.sharedMaterials=ms;}
    float cx=b?4.5f:4f,wide=b?10:8,deep=b?7:6;
    // Soft scalloped soil edge; no raised rectangular frame.
    vv.Clear();tt.Clear();vv.Add(new Vector3(cx,Height(cx,0,b)+.015f,0));
    for(int j=0;j<=40;j++){float a=j*Mathf.PI*2/40,rr=1-.025f*Mathf.Abs(Mathf.Sin(a*4));float x=cx+Mathf.Sign(Mathf.Cos(a))*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),.55f)*(wide*.5f)*rr,z=Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),.55f)*(deep*.5f)*rr; vv.Add(new Vector3(x,Height(x,z,b)+.015f,z));if(j>0)tt.AddRange(new[]{0,j+1,j});}
    MeshObject("worked_soil",variant.transform,vv.ToArray(),tt.ToArray(),soil);
    for(int row=0;row<4;row++) {
        float rx=cx+(row-1.5f)*(b?2.0f:1.65f),len=b?5.7f:4.7f;
        vv.Clear();tt.Clear();int along=16,across=8;
        for(int j=0;j<=along;j++) {float t=j/(float)along,z=(t-.5f)*len,curve=b?.52f*Mathf.Sin(t*Mathf.PI)-.24f:0;
            float taper=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.24f);
            for(int k=0;k<=across;k++){float a=Mathf.PI*k/across,x=rx+curve+Mathf.Cos(a)*.56f*taper; vv.Add(new Vector3(x,Height(x,z,b)+.025f+.19f*Mathf.Sin(a)*taper,z));}}
        for(int j=0;j<along;j++)for(int k=0;k<across;k++){int a=j*(across+1)+k;tt.AddRange(new[]{a,a+1,a+across+1,a+1,a+across+2,a+across+1});}
        MeshObject("rounded_ridge_"+(row+1),variant.transform,vv.ToArray(),tt.ToArray(),soilRidge);
        for(int j=0;j<7;j++) {float t=(j+.55f)/7.1f,z=(t-.5f)*len,x=rx+(b?.52f*Mathf.Sin(t*Mathf.PI)-.24f:0);float h=Height(x,z,b);
            if(row==2 && j>3)continue; // A short tended, harvested section.
            float sy=row==3?.40f:.19f;
            Blob("crop_mass_"+row+"_"+j,variant.transform,new Vector3(x,h+.21f+sy*.65f,z),new Vector3(.38f,sy,.39f),j%3==0?cropLight:crop,row*9+j);
            if(row==3 && j%2==0) Pole("support_"+j,variant.transform,new Vector3(x+.13f,h,z),new Vector3(x+.10f,h+1.25f,z+.06f),.026f);
        }
    }
    for(int j=0;j<5;j++){float x=cx-wide*.48f+j*wide*.235f,z=deep*.52f,h=Height(x,z,b);Pole("weathered_post_"+j,variant.transform,new Vector3(x,h-.03f,z),new Vector3(x+.035f*Mathf.Sin(j),h+.58f,z+.03f),.048f);}
    for(int j=0;j<4;j++){float x=-1.8f+j*.60f,z=-2.3f+.1f*Mathf.Sin(j);Blob("door_step_"+j,variant.transform,new Vector3(x,Height(x,z,b)+.045f,z),new Vector3(.27f,.065f,.21f),stone,50+j);}
    if(b)for(int j=0;j<14;j++){float a=j*Mathf.PI*2/14,x=cx+Mathf.Cos(a)*wide*.50f,z=Mathf.Sin(a)*deep*.50f;Blob("soft_grass_edge_"+j,variant.transform,new Vector3(x,Height(x,z,b)+.025f,z),new Vector3(.42f,.065f,.32f),grass,70+j);}
    variant.SetActive(!b);
}
var lt=UnityEngine.Object.FindFirstObjectByType<A7LightToggles>();
lt.painterly=true;lt.painterlyRadius=2;lt.kuwaharaShader=Shader.Find("Hidden/PitCrew/A7Kuwahara");lt.realtimeShadows=true;
var rp=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
if(rp){var copy=UnityEngine.Object.Instantiate(rp);copy.name="FieldReviewRP";SaveAsset(copy,"FieldReviewRP.asset");lt.sceneRP=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dir+"/FieldReviewRP.asset");}
lt.Apply();
var camGo=new GameObject("FieldReviewCamera");camGo.transform.SetParent(root.transform,false);var cam=camGo.AddComponent<Camera>();
cam.tag="MainCamera";cam.nearClipPlane=.1f;cam.farClipPlane=500;cam.fieldOfView=40;cam.aspect=16f/9;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.74f,.83f,.9f);
camGo.transform.position=new Vector3(7,30,-23);camGo.transform.LookAt(new Vector3(0,1,0));
var lr=camGo.AddComponent<LowResRenderTest>();lr.targetHeight=720;lr.integerScale=false;lr.msaa=1;
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("",new[]{dir})) {
    var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if(asset) UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "FieldMock saved; meshes="+serial+"; 2 variants; painterly shader="+lt.kuwaharaShader;
