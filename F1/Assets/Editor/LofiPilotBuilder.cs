using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PitCrew.ArtPilot;

// Isolated art proof: rebuilds only Assets/Art/LofiPilot, never the playable town.
public static class LofiPilotBuilder
{
    const string Dir = "Assets/Art/LofiPilot";
    const int Layer = 30;
    [Serializable] class Geometry { public Vector3[] vertices; public Vector2[] uv; public int[] triangles; }
    [Serializable] class ModelResult { public string name; public int triangles; public Vector3 size; public float bottom; public bool uvValid; public bool fbxBoundsMatch; }
    [Serializable] class Report { public ModelResult[] assets; public int placements; public int physicsGroundChecks; public string[] issues; public string[] shaderErrors; public string sourceScene; public bool sourceDirty; }
    static string Output => Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/source/lofi-pilot"));

    static T Save<T>(T obj, string path) where T:UnityEngine.Object
    {
        var existing=AssetDatabase.LoadAssetAtPath<T>(path);
        if(existing){EditorUtility.CopySerialized(obj,existing);UnityEngine.Object.DestroyImmediate(obj);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(obj,path);return obj;
    }
    static Material Material(string name, Color tint, Texture texture=null)
    {
        var shader=Shader.Find("PitCrew/LofiPilot/Surface");
        if(!shader)throw new Exception("Lofi shader missing");
        var m=new Material(shader){name=name};m.SetColor("_Tint",tint);
        m.SetColor("_Ambient",new Color(1f,.99f,.96f));m.SetFloat("_FaceShade",.7f);
        m.SetColor("_FogColor",new Color(.58f,.69f,.72f));m.SetFloat("_FogStart",35);m.SetFloat("_FogEnd",115);
        if(texture)m.SetTexture("_MainTex",texture);
        return Save(m,$"{Dir}/{name}.mat");
    }
    [MenuItem("PitCrew/Art/Lofi Pilot/Build and Capture")]
    public static void BuildAndCapture()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before building art proof");
        Directory.CreateDirectory(Output);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var ti=(TextureImporter)AssetImporter.GetAtPath(Dir+"/LofiAtlas.png");
        ti.filterMode=FilterMode.Point;ti.wrapMode=TextureWrapMode.Clamp;ti.mipmapEnabled=false;
        ti.textureCompression=TextureImporterCompression.Uncompressed;ti.sRGBTexture=true;ti.alphaSource=TextureImporterAlphaSource.None;ti.SaveAndReimport();
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/LofiAtlas.png");
        var surface=Material("LofiShared",Color.white,atlas);
        var results=new List<ModelResult>();
        foreach(var path in Directory.GetFiles(Dir,"*.mesh.json"))
        {
            var name=Path.GetFileName(path).Replace(".mesh.json","");
            var g=JsonUtility.FromJson<Geometry>(File.ReadAllText(path));
            if(g.vertices.Length!=g.uv.Length)throw new Exception("UV count mismatch: "+name);
            if(g.triangles.Any(t=>t<0||t>=g.vertices.Length))throw new Exception("Invalid index: "+name);
            var mesh=new Mesh{name=name,vertices=g.vertices,uv=g.uv,triangles=g.triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();
            // JSON is independent measurement evidence, FBX is the production geometry.
            if(AssetDatabase.LoadAssetAtPath<Mesh>($"{Dir}/{name}.asset"))AssetDatabase.DeleteAsset($"{Dir}/{name}.asset");
            var imported=AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.fbx");
            if(!imported)throw new Exception("FBX import missing: "+name);
            var go=new GameObject(name);var visual=(GameObject)PrefabUtility.InstantiatePrefab(imported);visual.transform.SetParent(go.transform,false);
            foreach(var r in visual.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=new[]{surface};
            if(name=="DeliveryBag"||name=="Parcel"||name=="Crate"){var b=Bounds(go);var bc=go.AddComponent<BoxCollider>();bc.center=b.center;bc.size=b.size;}
            else foreach(var mf in visual.GetComponentsInChildren<MeshFilter>())mf.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;
            PrefabUtility.SaveAsPrefabAsset(go,$"{Dir}/{name}.prefab");UnityEngine.Object.DestroyImmediate(go);
            // Validate the portable FBX independently of the native Unity mesh.
            var fbx=AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.fbx");
            bool match=false;
            if(fbx){var inst=(GameObject)PrefabUtility.InstantiatePrefab(fbx);var b=Bounds(inst);match=Vector3.Distance(b.size,mesh.bounds.size)<.025f;UnityEngine.Object.DestroyImmediate(inst);}
            results.Add(new ModelResult{name=name,triangles=mesh.triangles.Length/3,size=mesh.bounds.size,bottom=mesh.bounds.min.y,uvValid=g.uv.All(v=>v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1),fbxBoundsMatch=match});
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        var layout=ScriptableObject.CreateInstance<LofiPilotLayout>();
        JsonUtility.FromJsonOverwrite(File.ReadAllText(Dir+"/layout.json"),layout);layout=Save(layout,Dir+"/LofiPilotLayout.asset");
        AssetDatabase.SaveAssets();
        var old=SceneManager.GetActiveScene();bool oldDirty=old.isDirty;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var issues=new List<string>();var materials=new List<Material>{surface};
        try
        {
            var sunGo=new GameObject("PilotSun");sunGo.layer=Layer;sunGo.transform.rotation=Quaternion.Euler(48,-36,0);
            var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1;sun.shadows=LightShadows.Soft;sun.cullingMask=1<<Layer;RenderSettings.sun=sun;
            foreach(var p in layout.placements)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{p.asset}.prefab");
                if(!prefab)throw new Exception("Missing asset "+p.asset);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                go.transform.SetPositionAndRotation(p.position,Quaternion.Euler(0,p.yaw,0));go.transform.localScale=p.scale;SetLayer(go);
                var b=Bounds(go);float expected=p.position.y+(p.asset=="Road7x8"?-.18f:0);
                if(Mathf.Abs(b.min.y-expected)>.025f)issues.Add(go.name+" bottom "+b.min.y+" expected "+expected);
                if(Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.x,0))>.1||Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.z,0))>.1)issues.Add(go.name+" tilted");
            }
            var ground=Material("Ground",new Color(.46f,.48f,.32f));materials.Add(ground);
            Cube("Ground",new Vector3(0,-.24f,18),new Vector3(35,.12f,65),ground);
            var water=Material("Water",new Color(.43f,.6f,.66f));materials.Add(water);
            Cube("Sea",new Vector3(0,-1.2f,100),new Vector3(300,.2f,100),water);
            var marks=Material("RoadPaint",new Color(.72f,.71f,.61f));materials.Add(marks);
            for(int z=-8;z<43;z+=5)Cube("Lane dash",new Vector3(0,.012f,z),new Vector3(.11f,.012f,1.5f),marks);
            Physics.SyncTransforms();int groundChecks=0;
            foreach(float x in new[]{-3f,1f,3f})foreach(float z in new[]{-4.01f,-4f,-3.99f,3.99f,4f,4.01f,11.99f,12f,12.01f})
            {
                if(!Physics.Raycast(new Vector3(x,2,z),Vector3.down,out var hit,3,1<<Layer)||Mathf.Abs(hit.point.y)>.025f)issues.Add("Road collision seam "+x+","+z);
                groundChecks++;
            }
            var carAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/Generated/race_v2.fbx");
            if(!carAsset)throw new Exception("Existing race_v2 missing");
            var car=(GameObject)PrefabUtility.InstantiatePrefab(carAsset,scene);car.name="ExistingFormula_ArtOnly";
            car.transform.position=Vector3.zero;car.transform.localScale=Vector3.one*1.4f;SetLayer(car);
            var table=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Assets/Model/Generated/race_v2.materials.json"));
            foreach(var renderer in car.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                    var key=m.name.Replace(" (Instance)","");var val=table[key];Color c=val==null?Color.gray:new Color((float)val[0],(float)val[1],(float)val[2]);
                    if(key=="Body")c=new Color(.72f,.2f,.12f);if(key=="Helmet")c=new Color(.88f,.86f,.77f);
                    var cm=Material("Car_"+key,c);if(!materials.Contains(cm))materials.Add(cm);return cm;
                }).ToArray();
            }
            var cb=Bounds(car);car.transform.position+=Vector3.up*(-cb.min.y+.018f);
            var bag=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/DeliveryBag.prefab"),scene);
            bag.name="DeliveryBag_mount_visual";bag.transform.position=new Vector3(0,.87f,-.6f);SetLayer(bag);
            var camGo=new GameObject("LofiPreviewCamera");var cam=camGo.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.58f,.69f,.72f);cam.nearClipPlane=.1f;cam.farClipPlane=180;cam.cullingMask=1<<Layer;
            cam.allowHDR=false;cam.allowMSAA=false;cam.fieldOfView=layout.fieldOfView;
            cam.transform.position=layout.cameraPosition;cam.transform.LookAt(layout.cameraTarget);
            // Authoritative render A: published ambient-only principle, not a reproduction of proprietary assets.
            foreach(var m in materials)m.SetFloat("_FaceShade",0);
            Capture(cam,Output+"/00_ambient_456.png",456,256);
            Capture(cam,Output+"/01_ambient_baseline.png",640,360);
            foreach(var m in materials)m.SetFloat("_FaceShade",1f);
            Capture(cam,Output+"/02_warm_gameplay.png",960,540);
            Capture(cam,Output+"/03_warm_gameplay_640.png",640,360);
            cam.transform.position=new Vector3(-1,4,13);cam.transform.LookAt(new Vector3(-7,1.7f,23));
            Capture(cam,Output+"/04_garage.png",960,540);
            cam.orthographic=true;cam.orthographicSize=27;cam.transform.position=new Vector3(0,55,18);cam.transform.rotation=Quaternion.Euler(90,0,0);
            Capture(cam,Output+"/05_topdown.png",1000,1000);
            cam.orthographicSize=12;cam.transform.position=new Vector3(25,7,17);cam.transform.LookAt(new Vector3(-3,3,17));
            Capture(cam,Output+"/06_side.png",1200,700);
            // New kit versus previous untextured harbor asset under identical camera & shader.
            cam.orthographicSize=7;cam.transform.position=new Vector3(-.5f,7,-4);cam.transform.LookAt(new Vector3(-8,3,9));
            Capture(cam,Output+"/07_shop_close.png",960,720);
            cam.orthographic=false;cam.fieldOfView=48;cam.transform.position=new Vector3(2,1.25f,3);cam.transform.LookAt(new Vector3(3.45f,.1f,4));
            Capture(cam,Output+"/08_road_seam.png",960,540);
            // Asset-only comparison & kit board. Remove this stage before saving the scene.
            var stage=new GameObject("TemporaryAssetReview");stage.layer=Layer;
            var newShop=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/CornerShop.prefab"),scene);
            newShop.transform.SetParent(stage.transform);newShop.transform.position=new Vector3(60,0,0);SetLayer(newShop);
            var oldAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/Generated/harbor_0.fbx");
            if(oldAsset)
            {
                var previous=(GameObject)PrefabUtility.InstantiatePrefab(oldAsset,scene);previous.transform.SetParent(stage.transform);SetLayer(previous);
                float scale=6.8f/Bounds(previous).size.y;previous.transform.localScale*=scale;
                previous.transform.position=new Vector3(69,-Bounds(previous).min.y,0);
                foreach(var renderer in previous.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{var c=m.HasProperty("_Color")?m.color:Color.gray;var cm=Material("Previous_"+m.name,c);materials.Add(cm);return cm;}).ToArray();
            }
            foreach(var m in materials){m.SetFloat("_FaceShade",1);m.SetFloat("_FogStart",1000);m.SetFloat("_FogEnd",2000);}
            cam.orthographic=true;cam.orthographicSize=6.2f;cam.transform.position=new Vector3(76,10,20);cam.transform.LookAt(new Vector3(64,3,0));
            Capture(cam,Output+"/09_previous_vs_new.png",1400,850);
            UnityEngine.Object.DestroyImmediate(stage);
            stage=new GameObject("TemporaryKitReview");stage.layer=Layer;
            var kitNames=new[]{"CornerShop","RepairGarage","CoastalHouse","UtilityPole","CoastalTree","Wall3m","Crate","Parcel","DeliveryBag","TireStack","Road7x8","Sidewalk2x4"};
            for(int i=0;i<kitNames.Length;i++)
            {
                var asset=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{kitNames[i]}.prefab"),scene);
                asset.transform.SetParent(stage.transform);asset.transform.position=new Vector3(56+(i%3)*9,0,6-(i/3)*7);SetLayer(asset);
            }
            cam.orthographicSize=15;cam.transform.position=new Vector3(85,26,25);cam.transform.LookAt(new Vector3(64,1,-4));
            Capture(cam,Output+"/10_asset_kit.png",1500,1100);
            UnityEngine.Object.DestroyImmediate(stage);
            stage=new GameObject("TemporaryProps");
            var props=new[]{"Crate","Parcel","DeliveryBag","TireStack"};
            for(int i=0;i<props.Length;i++){
                var prop=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{props[i]}.prefab"),scene);
                prop.transform.SetParent(stage.transform);prop.transform.position=new Vector3(60+i*1.15f,0,0);SetLayer(prop);
            }
            cam.orthographicSize=1.2f;cam.transform.position=new Vector3(66,3,6);cam.transform.LookAt(new Vector3(61.7f,.25f,0));
            Capture(cam,Output+"/11_props.png",1300,500);UnityEngine.Object.DestroyImmediate(stage);
            foreach(var m in materials){m.SetFloat("_FogStart",35);m.SetFloat("_FogEnd",115);}
            cam.orthographic=false;cam.fieldOfView=layout.fieldOfView;cam.transform.position=layout.cameraPosition;cam.transform.LookAt(layout.cameraTarget);
            EditorSceneManager.SaveScene(scene,Dir+"/LofiPilot.unity");
            AssetDatabase.SaveAssets();
            var shaderErrors=ShaderUtil.GetShaderMessages(surface.shader).Where(x=>x.severity.ToString()=="Error").Select(x=>x.message).ToArray();
            foreach(var a in results){if(!a.uvValid)issues.Add(a.name+" bad UV");if(!a.fbxBoundsMatch)issues.Add(a.name+" FBX dimensions differ");}
            File.WriteAllText(Output+"/unity-verification.json",JsonUtility.ToJson(new Report{assets=results.ToArray(),placements=layout.placements.Length,physicsGroundChecks=groundChecks,issues=issues.ToArray(),shaderErrors=shaderErrors,sourceScene=old.path,sourceDirty=oldDirty},true));
            if(issues.Count>0||shaderErrors.Length>0)throw new Exception("Lofi validation failed: "+string.Join(";",issues.Concat(shaderErrors)));
            Debug.Log("LOFI_PILOT_OK: "+results.Count+" assets, "+layout.placements.Length+" placements; captures "+Output);
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(old);}
    }
    static Bounds Bounds(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static void SetLayer(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=Layer;}
    static void Cube(string n,Vector3 pos,Vector3 scale,Material m){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.layer=Layer;go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=m;}
    static void Capture(Camera cam,string path,int w,int h)
    {
        var previous=RenderTexture.active;var target=cam.targetTexture;var rt=new RenderTexture(w,h,24){antiAliasing=1,filterMode=FilterMode.Point};
        var image=new Texture2D(w,h,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.aspect=(float)w/h;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{cam.targetTexture=target;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
}
