using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class PitCrewTrialBuilder
{
    const string Dir="Assets/PitCrewTrial";
    static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/pitcrew-first-flow"));
    [Serializable] public class Option {public string id,name,bodyTint,groundTint,reason;}
    [Serializable] public class Options {public string observed;public Option[] options;}
    static Material Material(string name,Color color,Material original=null)
    {
        var path=Dir+"/"+name+".mat";
        var mat=original?new Material(original):new Material(Shader.Find("PitCrew/LofiPilot/Surface"));
        mat.name=name;mat.SetColor("_Tint",color);mat.SetColor("_Ambient",Color.white);mat.SetFloat("_FogStart",150);mat.SetFloat("_FogEnd",250);
        var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(existing){EditorUtility.CopySerialized(mat,existing);UnityEngine.Object.DestroyImmediate(mat);return existing;}
        AssetDatabase.CreateAsset(mat,path);return mat;
    }
    static Color ColorOf(string hex){if(!ColorUtility.TryParseHtmlString(hex,out var value))throw new Exception("Invalid model color: "+hex);return value;}
    static GameObject Box(string name,Vector3 position,Vector3 scale,Material mat)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
    static void Capture(Camera c,string name)
    {
        var rt=new RenderTexture(1280,800,24);var tex=new Texture2D(1280,800,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{c.targetTexture=rt;c.aspect=1.6f;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,800),0,0);tex.Apply();File.WriteAllBytes(Output+"/"+name+".png",tex.EncodeToPNG());}
        finally{c.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    [MenuItem("PitCrew/Trial/Create isolated coastal trial")]
    public static void Create()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Editor must be idle");
        var options=JsonUtility.FromJson<Options>(File.ReadAllText(Output+"/art-options.json"));
        if(options.options.Length!=2)throw new Exception("Expected two model proposals");
        var old=SceneManager.GetActiveScene();bool dirty=old.isDirty;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        // A unique scene culling bit isolates comparison renders from every open scene.
        EditorSceneManager.SetSceneCullingMask(scene,1UL<<62);
        try{
            var road=Material("TrialRoad",new Color(.42f,.43f,.42f));var sand=Material("TrialSand",ColorOf(options.options[0].groundTint));
            var sea=Material("TrialSea",new Color(.24f,.48f,.55f));var wall=Material("TrialWalls",new Color(.88f,.81f,.65f));
            Box("Coastal road",new Vector3(0,-.16f,35),new Vector3(12,.3f,110),road);
            var sandObject=Box("Sand",new Vector3(18,-.25f,35),new Vector3(24,.3f,110),sand);
            Box("Sea",new Vector3(42,-.3f,35),new Vector3(24,.3f,110),sea);
            for(int i=0;i<8;i++){Box("Harbor building "+i,new Vector3(-10,2,12+i*10),new Vector3(5,4+(i%3),5),wall);Box("Road marker "+i,new Vector3(0,.005f,10+i*10),new Vector3(.12f,.02f,3),wall);}
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/CoastalFormula/CoastalFormula.prefab"),scene);
            visual.name="Trial vehicle — original asset copy";visual.transform.position=new Vector3(0,.1f,0);
            var collider=visual.GetComponent<BoxCollider>();collider.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/CarLowFriction.physicMaterial");
            if(!collider.sharedMaterial)throw new Exception("Production low-friction material missing");
            collider.size=new Vector3(collider.size.x,collider.center.y*2,collider.size.z);
            var rb=visual.AddComponent<Rigidbody>();rb.mass=100;rb.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;rb.interpolation=RigidbodyInterpolation.Interpolate;
            var car=visual.AddComponent<ArcadeCarController>();
            var camera=new GameObject("TrialCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.backgroundColor=new Color(.57f,.69f,.73f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=50;camera.nearClipPlane=.05f;camera.farClipPlane=200;
            camera.overrideSceneCullingMask=1UL<<62;camera.gameObject.AddComponent<AudioListener>();
            var light=new GameObject("TrialSun").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-30,0);light.intensity=1;
            var driver=new GameObject("PitCrew Trial Controls").AddComponent<PitCrewTrialDriver>();driver.car=car;driver.chase=camera;
            driver.sand=sandObject.GetComponent<Renderer>();driver.sandA=ColorOf(options.options[0].groundTint);driver.sandB=ColorOf(options.options[1].groundTint);
            driver.paint=visual.GetComponentsInChildren<Renderer>();
            var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CoastalFormula/FormulaShared.mat");
            var a=Material("PaletteA",ColorOf(options.options[0].bodyTint),original);var b=Material("PaletteB",ColorOf(options.options[1].bodyTint),original);
            driver.paletteA=driver.paint.Select(r=>a).ToArray();driver.paletteB=driver.paint.Select(r=>b).ToArray();
            if(UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Any(r=>r.gameObject.scene!=scene&&r.gameObject.layer==31))throw new Exception("Capture layer 31 is already in use");
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            camera.cullingMask=1<<31;light.cullingMask=1<<31;
            camera.transform.position=new Vector3(4,2.8f,5.5f);camera.transform.LookAt(new Vector3(0,.6f,0));
            foreach(var r in driver.paint)r.sharedMaterial=original;Capture(camera,"palette-original");
            foreach(var r in driver.paint)r.sharedMaterial=a;Capture(camera,"palette-A");
            sand.SetColor("_Tint",ColorOf(options.options[1].groundTint));foreach(var r in driver.paint)r.sharedMaterial=b;Capture(camera,"palette-B");sand.SetColor("_Tint",ColorOf(options.options[0].groundTint));
            foreach(var r in driver.paint)r.sharedMaterial=a;
            // Capture isolation must not inherit the project's collision settings for layer 31.
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=0;
            camera.cullingMask=~0;light.cullingMask=~0;
            camera.overrideSceneCullingMask=0;camera.transform.position=new Vector3(0,3.4f,-6.5f);camera.transform.LookAt(new Vector3(0,.6f,0));
            EditorSceneManager.SaveScene(scene,Dir+"/CoastalTrial.unity");
            File.WriteAllText(Output+"/unity-scene-evidence.json",JsonUtility.ToJson(new Evidence{originalScene=old.path,originalDirty=dirty,scene=Dir+"/CoastalTrial.unity",controller="Unchanged ArcadeCarController; only steerLag runtime variant",art="Actual Unity renders with identical camera; model-proposed multiplicative material tint; not generative image output"},true));
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(old);}
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Dir}))AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
        if(old.isDirty!=dirty)throw new Exception("Original dirty state unexpectedly changed");
    }
    [Serializable] class Evidence{public string originalScene,scene,controller,art;public bool originalDirty;}
    [MenuItem("PitCrew/Trial/Build isolated Windows trial")]
    public static void Build()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Dir+"/CoastalTrial.unity"},locationPathName=Output+"/Windows/PitCrewCoastalTrial.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        File.WriteAllText(Output+"/build-result.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+"\nWarnings="+report.summary.totalWarnings+"\nBytes="+report.summary.totalSize);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Trial build failed");
    }
}
