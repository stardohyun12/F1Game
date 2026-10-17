using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Build approved designs into NEW prefabs and a separate review scene only.</summary>
public static class StreetFurniturePreview
{
    const string Dir="Assets/Art/StreetFurniture";
    static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/source/street-furniture"));
    static Bounds Bounds(GameObject go) {
        var rr=go.GetComponentsInChildren<Renderer>(); var b=rr[0].bounds;
        foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static void ImportTexture(string name,bool road) {
        var i=(TextureImporter)AssetImporter.GetAtPath(Dir+"/"+name+".png");
        i.textureType=TextureImporterType.Default;i.sRGBTexture=true;i.maxTextureSize=256;
        i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=road;
        i.filterMode=road?FilterMode.Trilinear:FilterMode.Point;i.anisoLevel=road?8:1;
        i.wrapMode=road?TextureWrapMode.Repeat:TextureWrapMode.Clamp;i.SaveAndReimport();
    }
    static Material Material(string name,Material template) {
        var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/"+name+".mat");
        if(!m){m=new Material(template);AssetDatabase.CreateAsset(m,Dir+"/"+name+".mat");}
        return m;
    }
    [MenuItem("PitCrew/Art/Street Furniture/Build Approved Design Preview")]
    public static void Build() {
        if(EditorApplication.isPlaying)throw new Exception("Exit play mode first.");
        Directory.CreateDirectory(Output);
        ImportTexture("FurnitureAtlas",false);ImportTexture("AsphaltAggregate",true);
        var furniture=Material("Furniture",AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CoastalTown/BuildingsSand.mat"));
        furniture.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/FurnitureAtlas.png"));
        furniture.SetColor("_Tint",Color.white);EditorUtility.SetDirty(furniture);
        var road=Material("Asphalt",AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CoastalTown/Asphalt.mat"));
        road.shader=Shader.Find("PitCrew/StreetFurniture/Asphalt");
        road.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/AsphaltAggregate.png"));
        road.SetFloat("_WorldScale",.25f);road.SetColor("_Tint",Color.white);EditorUtility.SetDirty(road);
        var log=new List<string>();
        foreach(string name in new[]{"Bench","Bin"}) {
            var importer=(ModelImporter)AssetImporter.GetAtPath(Dir+"/"+name+".fbx");
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.globalScale=1;importer.SaveAndReimport();
            var go=new GameObject(name);
            try {
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/"+name+".fbx"));
                visual.transform.SetParent(go.transform,false);
                foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(furniture,r.sharedMaterials.Length).ToArray();
                var b=Bounds(go);
                if(Mathf.Abs(b.min.y)>.002f||b.size.y<.7f||b.size.y>1.1f)throw new Exception(name+" bad imported bounds "+b);
                if(name=="Bin"){var c=go.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;}
                else foreach(var mf in go.GetComponentsInChildren<MeshFilter>())mf.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;
                PrefabUtility.SaveAsPrefabAsset(go,Dir+"/"+name+".prefab");
                log.Add(name+": dimensions="+b.size.ToString("F3")+" base="+b.min.y+" triangles="+go.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3));
            } finally {UnityEngine.Object.DestroyImmediate(go);}
        }
        if(ShaderUtil.ShaderHasError(road.shader)||ShaderUtil.ShaderHasError(furniture.shader))throw new Exception("Shader error");
        AssetDatabase.SaveAssets();File.WriteAllLines(Output+"/preview-validation.txt",log);
        Capture();
    }
    [MenuItem("PitCrew/Art/Street Furniture/Capture Approved Design Preview")]
    public static void Capture() {
        if(EditorApplication.isPlaying)throw new Exception("Exit play mode first.");
        var main=SceneManager.GetActiveScene();bool wasDirty=main.isDirty;
        var sunlight=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional&&l.enabled);
        var sunColor=sunlight.color;var sunIntensity=sunlight.intensity;var sunRotation=sunlight.transform.rotation;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var previous=RenderTexture.active;bool previousAsync=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        try {
            void Layer(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=30;}
            GameObject Model(string path,Vector3 pos,float scale=1) {
                var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);
                g.transform.position=pos;g.transform.localScale=Vector3.one*scale;Layer(g);return g;
            }
            var road=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Asphalt.mat");
            var pavement=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CoastalTown/Pavement.mat");
            var stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CoastalTown/BuildingsSand.mat");
            GameObject Box(string name,Vector3 pos,Vector3 size,Material mat) {
                var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;Layer(g);return g;
            }
            Box("Sidewalk",new Vector3(0,.06f,0),new Vector3(8,.12f,2),pavement);
            var wall=Model("Assets/Art/LofiPilot/Wall3m.prefab",new Vector3(.25f,.12f,-1.05f));
            wall.transform.localScale=new Vector3(1.7f,.6f,1);
            var bench=Model(Dir+"/Bench.prefab",new Vector3(.25f,.12f,0));
            var bin=Model(Dir+"/Bin.prefab",new Vector3(-1.3f,.12f,0));
            // Six independent road tiles. World-space texture must join continuously.
            for(int x=-1;x<=0;x++)for(int z=0;z<3;z++) {
                var p=GameObject.CreatePrimitive(PrimitiveType.Plane);p.name="RoadTile_"+x+"_"+z;
                p.transform.position=new Vector3(x*4+2,0,3+z*4);p.transform.localScale=new Vector3(.4f,1,.4f);p.GetComponent<Renderer>().sharedMaterial=road;Layer(p);
            }
            for(int i=-4;i<4;i++) {
                var curb=Model("Assets/Art/LofiPilot/Curb.prefab",new Vector3(i+.5f,0,1));curb.transform.localScale=new Vector3(1/6f,1,1);
            }
            var drain=Model("Assets/Art/LofiPilot/Drain.prefab",new Vector3(-2,0,1.3f));
            var car=Model("Assets/Art/CoastalFormula/FormulaGameplayVisual.prefab",new Vector3(1,0,3.8f),.464286f);
            car.transform.rotation=Quaternion.Euler(0,180,0);
            var sunGo=new GameObject("Review Sun");var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.color=sunColor;sun.intensity=sunIntensity;sun.transform.rotation=sunRotation;sun.shadows=LightShadows.Soft;sun.cullingMask=1<<30;RenderSettings.sun=sun;
            var cameraGo=new GameObject("Review Camera");var camera=cameraGo.AddComponent<Camera>();
            camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.76f,.86f,.93f);camera.farClipPlane=80;camera.fieldOfView=48;
            void Shot(string name,Vector3 eye,Vector3 target,bool ortho=false,float size=2) {
                camera.orthographic=ortho;camera.orthographicSize=size;camera.transform.position=eye;camera.transform.LookAt(target);
                var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;
                try {camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Output+"/"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
                finally {camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
            }
            car.SetActive(false);
            Shot("approved_props",new Vector3(2.1f,2.25f,4.5f),new Vector3(-.25f,.48f,0));
            Shot("approved_side",new Vector3(4,1.35f,2.4f),new Vector3(-.25f,.5f,0),true,1.55f);
            Shot("approved_top",new Vector3(-.25f,8,.001f),new Vector3(-.25f,0,0),true,2.3f);
            Shot("approved_road_seams",new Vector3(0,4,7),new Vector3(0,0,2.5f));
            Shot("approved_road_close",new Vector3(-.5f,.9f,3),new Vector3(-1.8f,0,1.3f));
            car.SetActive(true);
            Shot("approved_in_context",new Vector3(5,3.3f,7.5f),new Vector3(-.15f,.55f,1.8f));
            // Scale review: identical camera/composition. Gameplay scale is a legacy
            // compact collider fit, not a metre-consistent real-world vehicle scale.
            car.transform.localScale=Vector3.one;
            Shot("scale_corrected_in_context",new Vector3(5,3.3f,7.5f),new Vector3(-.15f,.55f,1.8f));
            Shot("scale_corrected_top",new Vector3(0,12,3),new Vector3(0,0,3),true,4.4f);
            File.WriteAllText(Output+"/scale-review.txt","Preview only; no gameplay changes.\nPrevious car: 1.757m long x 0.909m wide (scale 0.464286).\nProposal: 3.785m long x 1.959m wide (original asset scale 1).\nBench: 1.800m wide, seat 0.450m high. Bin: 0.952m high.\nThis is a stylized proportion proposal, not a homologated F1 dimension claim.\n");
            // Save an isolated inspection scene, not the running map.
            EditorSceneManager.SaveScene(scene,Dir+"/StreetFurnitureReview.unity");
            File.AppendAllText(Output+"/preview-validation.txt","\nShader errors=0\nSide/top/close/seam/context renders captured.\nMain scene not saved or edited.\n");
        } finally {
            RenderTexture.active=previous;ShaderUtil.allowAsyncCompilation=previousAsync;EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(main);
        }
        if(main.isDirty!=wasDirty)throw new Exception("Main scene dirty-state changed unexpectedly");
    }
}
