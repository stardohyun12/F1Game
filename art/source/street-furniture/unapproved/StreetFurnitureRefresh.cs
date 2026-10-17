using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scoped refresh: two props and road surface. No town/layout regeneration.</summary>
public static class StreetFurnitureRefresh
{
    public const string Dir = "Assets/Art/StreetFurniture";
    const string Kit = "Assets/Art/LofiPilot";
    const string RoadPath = "Assets/Art/CoastalTown/Asphalt.mat";
    static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../../art/source/street-furniture"));
    public static bool IsFurniture(string name) => name == "Bench" || name == "Bin";
    public static string ModelPath(string name) => IsFurniture(name) && AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.fbx") ? $"{Dir}/{name}.fbx" : $"{Kit}/{name}.fbx";
    public static Material MaterialFor(string name, Material fallback) => IsFurniture(name) ? AssetDatabase.LoadAssetAtPath<Material>(Dir + "/Furniture.mat") ?? fallback : fallback;
    static Bounds Bounds(GameObject go)
    {
        var rr = go.GetComponentsInChildren<Renderer>(); var b = rr[0].bounds;
        foreach (var r in rr.Skip(1)) b.Encapsulate(r.bounds); return b;
    }
    static void Texture(string path, bool road)
    {
        var i = (TextureImporter)AssetImporter.GetAtPath(path);
        i.textureType = TextureImporterType.Default; i.sRGBTexture = true;
        i.textureCompression = TextureImporterCompression.Uncompressed;
        i.mipmapEnabled = road; i.filterMode = road ? FilterMode.Trilinear : FilterMode.Point;
        i.wrapMode = road ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        i.anisoLevel = road ? 8 : 1; i.maxTextureSize = 256; i.SaveAndReimport();
    }
    public static Material ConfigureRoad(Material m)
    {
        var shader = Shader.Find("PitCrew/StreetFurniture/Asphalt");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/AsphaltAggregate.png");
        if (!shader || !texture) return m;
        m.shader = shader; m.SetTexture("_MainTex", texture); m.SetFloat("_WorldScale", .25f);
        m.SetColor("_Tint", Color.white); EditorUtility.SetDirty(m); return m;
    }
    [MenuItem("PitCrew/Art/Street Furniture/Build and Apply")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/ComplexTrack.unity" || scene.isDirty)
            throw new Exception("Use saved ComplexTrack in edit mode.");
        Directory.CreateDirectory(Output);
        var backup = Output + "/ComplexTrack.before-street.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        var road = AssetDatabase.LoadAssetAtPath<Material>(RoadPath);
        if (!AssetDatabase.LoadAssetAtPath<Material>(Dir + "/BeforeAsphalt.mat"))
            AssetDatabase.CreateAsset(new Material(road), Dir + "/BeforeAsphalt.mat");
        Texture(Dir + "/FurnitureAtlas.png", false); Texture(Dir + "/AsphaltAggregate.png", true);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(Dir + "/Furniture.mat");
        if (!mat) { mat = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CoastalTown/BuildingsSand.mat")); AssetDatabase.CreateAsset(mat, Dir + "/Furniture.mat"); }
        mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/FurnitureAtlas.png"));
        mat.SetColor("_Tint", Color.white); EditorUtility.SetDirty(mat);
        foreach (string name in new[] { "Bench", "Bin" })
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath(name));
            importer.materialImportMode = ModelImporterMaterialImportMode.None; importer.importAnimation = false;
            importer.globalScale = 1; importer.SaveAndReimport();
            var go = new GameObject(name);
            try {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(name)));
                model.transform.SetParent(go.transform, false);
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
                if (name == "Bin") { var b = Bounds(go); var c = go.AddComponent<BoxCollider>(); c.center = b.center; c.size = b.size; }
                else foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                var bounds = Bounds(go);
                if (Mathf.Abs(bounds.min.y) > .002f || bounds.size.y > 1.1f || bounds.size.y < .7f) throw new Exception(name + " import axes/scale invalid " + bounds);
                PrefabUtility.SaveAsPrefabAsset(go, $"{Kit}/{name}.prefab");
            } finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        ConfigureRoad(road);
        int props = 0, removed = 0;
        var town = GameObject.Find("CoastalTown");
        foreach (Transform t in town.transform) {
            string name = t.name.Split('_')[0];
            if (IsFurniture(name)) {
                foreach (var r in t.GetComponentsInChildren<Renderer>()) {
                    r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }
                props++;
            }
            // Old raised, identical stains look like obstacles. Preserve data/objects for rollback.
            if (name == "OilStain" || name == "Patch") { t.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject); removed++; }
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Output + "/apply.txt", $"Furniture instances={props}\nDisabled repeated road stains={removed}\nRoad topology, car and lighting unchanged.\n");
        Debug.Log("[StreetFurniture] Applied " + props + " props; removed " + removed + " repeated stains.");
    }

    // Render old/new models under the same light, with orthographic side/top checks.
    [MenuItem("PitCrew/Art/Street Furniture/Capture Review")]
    public static void CaptureReview()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play first");
        var root = new GameObject("StreetFurniture_Review_Temporary"); root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.position = new Vector3(420, 0, 0);
        var current = RenderTexture.active;
        try {
            void SetLayer(GameObject go) { foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.layer = 31; }
            void Model(string path, Material mat, Vector3 pos) {
                var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
                go.transform.localPosition = pos; SetLayer(go);
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
            }
            var old = AssetDatabase.LoadAssetAtPath<Material>(Kit + "/LofiShared.mat");
            var fresh = AssetDatabase.LoadAssetAtPath<Material>(Dir + "/Furniture.mat");
            Model(Kit + "/Bench.fbx",old,new Vector3(-1.8f,0,0));
            Model(Dir + "/Bench.fbx",fresh,new Vector3(1.2f,0,0));
            Model(Kit + "/Bin.fbx",old,new Vector3(-.7f,0,.1f));
            Model(Dir + "/Bin.fbx",fresh,new Vector3(2.5f,0,.1f));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(root.transform,false);
            floor.transform.localPosition = new Vector3(0,-.06f,0); floor.transform.localScale = new Vector3(9,.12f,5); SetLayer(floor);
            floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CoastalTown/Pavement.mat");
            var camGo = new GameObject("ReviewCamera"); camGo.transform.SetParent(root.transform,false);
            var camera=camGo.AddComponent<Camera>(); camera.cullingMask=1<<31; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.69f,.79f,.83f); camera.farClipPlane=50; camera.orthographic=true;
            void Shot(string name,Vector3 eye,Vector3 target,float size) {
                camera.transform.localPosition=eye; camera.transform.LookAt(root.transform.TransformPoint(target)); camera.orthographicSize=size;
                var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32); camera.targetTexture=rt;
                try { camera.Render(); RenderTexture.active=rt; var tex=new Texture2D(1600,900,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,1600,900),0,0); tex.Apply(); File.WriteAllBytes(Output+"/"+name+".png",tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex); }
                finally { camera.targetTexture=null; RenderTexture.active=current; RenderTexture.ReleaseTemporary(rt); }
            }
            Shot("props_before_left_after_right",new Vector3(3.5f,2.7f,7),new Vector3(.35f,.4f,0),2.1f);
            Shot("props_side",new Vector3(7,1.1f,4),new Vector3(.35f,.4f,0),2.1f);
            Shot("props_top",new Vector3(.35f,9,.001f),new Vector3(.35f,0,0),2.1f);
            foreach(Transform child in root.transform) if(child!=camGo.transform) child.gameObject.SetActive(false);
            for(int i=0;i<2;i++) {
                var p=GameObject.CreatePrimitive(PrimitiveType.Plane);p.transform.SetParent(root.transform,false);p.transform.localPosition=new Vector3(i==0?-3:3,0,0);p.transform.localScale=new Vector3(.6f,1,1.2f);SetLayer(p);
                p.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(i==0?Dir+"/BeforeAsphalt.mat":RoadPath);
            }
            Shot("road_before_left_after_right",new Vector3(0,5,7),Vector3.zero,4);
        } finally { RenderTexture.active=current; UnityEngine.Object.DestroyImmediate(root); }
    }
}
