using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 아트 납품 BayLoop 건물·소품(F1/Assets/Art/BayLoop/{Buildings,Props}/*.fbx + 아틀라스 PNG) → 임포트 설정·머티리얼·프리팹.
///  - 텍스처: Point · 무압축 · sRGB · 최대 1024 · 밉맵 없음(LofiAtlas 와 같은 설정)
///  - 모델: scale 1, 축 변환 굽기, FBX 머티리얼은 안 씀
///  - 머티리얼: PitCrew/LofiPilot/Surface(LofiShared 값 복사) — MoodController 가 _Ambient/_FogColor/_FogStart/_FogEnd 를 다른 건물과 같이 맞춘다
///  - 건물 프리팹: &lt;id&gt;.prefab, 루트 scale1/rot0, 본체 footprint 만 덮는 BoxCollider 1개(처마·계단·차양은 안 막음)
///  - 소품 프리팹: Props/&lt;id&gt;.prefab, 렌더 bounds BoxCollider 1개(벽 부착물은 콜라이더 없음). 리지드바디는 빌더가 붙인다.
/// 규격: art/models/bayloop_italy_v01/SPEC.md · bayloop_italy_props_v01/SPEC.md (D-137 이탈리아 판, 앞 +Z, 몸체 -Z)
///  · bayloop_variety_v01/SPEC.md (D-140 변형 &lt;type&gt;_&lt;nn&gt;, 공유 아틀라스 BayLoopShared_01~04, 치수·아틀라스는 manifest.json 에서)
/// </summary>
public static class ImportBayLoopBuildings
{
    const string Dir = "Assets/Art/BayLoop/Buildings/";
    const string PropDir = "Assets/Art/BayLoop/Props/";
    // manifest.json footprint_m (폭 x, 깊이 z) — 본체만. 앞벽 z=0 에서 뒤(-z)로.
    public static readonly Dictionary<string, Vector2> Footprint = new Dictionary<string, Vector2>
    {
        { "shop_house_a", new Vector2(5.5f, 8f) },        { "shop_house_b", new Vector2(6.75f, 8f) },     { "shop_house_c", new Vector2(6.25f, 8f) },
        { "terrace_house_a", new Vector2(5.5f, 6f) },     { "terrace_house_b", new Vector2(6.75f, 6f) },  { "terrace_house_c", new Vector2(5.5f, 6f) },
        { "cliff_villa_a", new Vector2(8f, 7f) },
        { "harbor_warehouse_a", new Vector2(9f, 8.5f) },  { "harbor_warehouse_b", new Vector2(11f, 9.5f) },
    };
    // D-140 매개변수 변형(<type>_<nn>, art/models/bayloop_variety_v01/manifest.json): 폭·깊이·아틀라스·앞 돌출을 장부에서 읽는다.
    // 앞 돌출(FrontOut) = bounds_max.z — 앞마당·담장·차양이 앞벽(z=0)보다 길 쪽으로 나온 길이. 빌더가 그만큼 물려 앉힌다.
    const string VarietyManifest = "../../art/models/bayloop_variety_v01/manifest.json";
    public static readonly Dictionary<string, float> FrontOut = new Dictionary<string, float>();
    static readonly Dictionary<string, string> VarietyAtlas = new Dictionary<string, string>();
    [System.Serializable] class VAsset { public string id, texture; public float[] footprint_m, bounds_max; }
    [System.Serializable] class VManifest { public VAsset[] assets; }
    static ImportBayLoopBuildings()
    {
        var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, VarietyManifest));
        if (!System.IO.File.Exists(path)) return;
        var m = JsonUtility.FromJson<VManifest>(System.IO.File.ReadAllText(path, Encoding.UTF8));
        foreach (var a in m.assets)
        {
            if (a.footprint_m == null || a.footprint_m.Length < 2 || Footprint.ContainsKey(a.id)) continue;
            Footprint[a.id] = new Vector2(a.footprint_m[0], a.footprint_m[1]);
            FrontOut[a.id] = a.bounds_max != null && a.bounds_max.Length > 2 ? Mathf.Max(0f, a.bounds_max[2]) : 0f;
            VarietyAtlas[a.id] = System.IO.Path.GetFileNameWithoutExtension(a.texture);
        }
    }
    // manifest.json texture — 이탈리아 판은 변형마다 회벽 색 아틀라스가 다르다. 창고는 지난 판 그대로.
    static readonly Dictionary<string, string> AtlasOf = new Dictionary<string, string>
    {
        { "shop_house_a", "Italy_cream_Atlas" }, { "shop_house_b", "Italy_ochre_Atlas" }, { "shop_house_c", "Italy_blue_Atlas" },
        { "terrace_house_a", "Italy_cream_Atlas" }, { "terrace_house_b", "Italy_ochre_Atlas" }, { "terrace_house_c", "Italy_blue_Atlas" },
        { "cliff_villa_a", "Italy_cream_Atlas" },
        { "harbor_warehouse_a", "harbor_warehouse_Atlas" }, { "harbor_warehouse_b", "harbor_warehouse_Atlas" },
    };
    // 소품 manifest — 아틀라스, 벽 부착(콜라이더 없음) 여부. 폴더에 FBX 가 있는 것만 만든다(보류 4개는 F1 에 없음).
    static readonly Dictionary<string, (string atlas, bool wall)> PropOf = new Dictionary<string, (string, bool)>
    {
        { "vending_machine", ("PropsPhotoAtlas", false) }, { "small_signs_a", ("PropsPhotoAtlas", true) }, { "small_signs_c", ("PropsPhotoAtlas", true) },
        { "ac_unit", ("PropsPhotoAtlas", false) }, { "mailbox", ("PropsPhotoAtlas", true) },
        { "bicycle", ("PropsPhotoAtlas_r2", false) },
        { "planter_row", ("PropsPhotoAtlas_r3", false) }, { "small_signs_b", ("PropsPhotoAtlas_r3", true) }, { "small_signs_d", ("PropsPhotoAtlas_r3", true) },
        { "meter_box", ("PropsPhotoAtlas_r3", true) }, { "lemon_tree_pot", ("PropsPhotoAtlas_r3", false) },
    };

    // 소품 2차(D-141, art-20261025-2201 delivery.json 승인 14종) — 규격: 바닥 가운데 피벗·앞 +Z 라 뒤집기 판정 안 함.
    // col: 0 = 콜라이더 없음(노면 칠·땜질·벽 부착), 1 = 렌더 bounds 박스, 2 = 기둥만(아래 0.3m 굵기 박스 — 표지·반사경 판이 차를 막지 않게)
    static readonly Dictionary<string, (string atlas, int col)> Prop2Of = new Dictionary<string, (string, int)>
    {
        { "cafe_set_01", ("Props2FurnitureAtlas", 1) }, { "crosswalk_paint_01", ("Props2PavingAtlas", 0) }, { "garage_door_inset_01", ("Props2VehiclesAtlas", 0) },
        { "mailbox_cluster_01", ("Props2FurnitureAtlas", 0) }, { "parasol_01", ("Props2LinenAtlas", 2) }, { "road_patch_01", ("Props2PavingAtlas", 0) },
        { "road_patch_02", ("Props2PavingAtlas", 0) }, { "scooter_01", ("Props2FurnitureAtlas", 1) }, { "scooter_02", ("Props2FurnitureAtlas", 1) },
        { "shop_display_01", ("Props2FurnitureAtlas", 1) }, { "street_sign_01", ("Props2PhotoAtlas", 2) }, { "street_sign_02", ("Props2RoadAtlas", 2) },
        { "street_sign_03", ("Props2RoadAtlas", 2) }, { "traffic_mirror_01", ("Props2PhotoAtlas", 2) },
    };

    [MenuItem("PitCrew/Map/Import BayLoop Props 2")]
    public static void MenuProps2() => Debug.Log(RunProps2());

    public static string RunProps2()
    {
        var sb = new StringBuilder(); var pmats = new Dictionary<string, Material>();
        foreach (var kv in Prop2Of)
        {
            string id = kv.Key, fbx = PropDir + id + ".fbx";
            if (AssetImporter.GetAtPath(fbx) == null) { sb.Append($"모델 없음 {fbx}\n"); continue; }
            string atlas = kv.Value.atlas;
            if (!pmats.ContainsKey(atlas)) { SetupTexture(PropDir + atlas + ".png", sb); pmats[atlas] = SetupMaterial(PropDir, atlas); }
            var go = Build(fbx, id, pmats[atlas], out var b, out var flipped, true);
            if (flipped) { go.transform.GetChild(0).localRotation = Quaternion.identity; b = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; }); flipped = false; }
            if (kv.Value.col == 1) { var box = go.AddComponent<BoxCollider>(); box.center = b.center; box.size = b.size; }
            else if (kv.Value.col == 2) { var box = go.AddComponent<BoxCollider>(); box.center = new Vector3(0, b.size.y * .5f, 0); box.size = new Vector3(0.3f, b.size.y, 0.3f); }
            PrefabUtility.SaveAsPrefabAsset(go, PropDir + id + ".prefab");
            int tris = go.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
            sb.Append($"소품2 {id} [{atlas}] min{b.min:F2} max{b.max:F2} col{kv.Value.col} tris {tris}\n");
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }

    [MenuItem("PitCrew/Map/Import BayLoop Buildings")]
    public static void Menu() => Debug.Log(Run());

    static void SetupTexture(string path, StringBuilder sb)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { sb.Append($"텍스처 없음 {path}\n"); return; }
        ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Point; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 1024;
        ti.wrapMode = TextureWrapMode.Clamp; ti.alphaIsTransparency = false; ti.npotScale = TextureImporterNPOTScale.None;
        ti.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        sb.Append($"텍스처 {System.IO.Path.GetFileNameWithoutExtension(path)} {tex.width}x{tex.height} {tex.filterMode} {tex.format}\n");
    }

    static Material SetupMaterial(string dir, string atlas)
    {
        var refMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/LofiPilot/LofiShared.mat");
        var shader = Shader.Find("PitCrew/LofiPilot/Surface");
        string mp = dir + "M_" + atlas + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, mp); }
        m.shader = shader;
        if (refMat != null && refMat.shader == shader) m.CopyPropertiesFromMaterial(refMat);
        m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + atlas + ".png"));
        m.SetColor("_Tint", Color.white);
        EditorUtility.SetDirty(m);
        return m;
    }

    // FBX → 루트(scale1/rot0) 밑에 메시 자식. 앞이 +Z 가 되도록 필요하면 Y180.
    // 판정: 규격상 몸체는 -Z 쪽(bounds z 대략 -8..+1) → 임포트 후 bounds 가운데가 +Z 면 뒤집힌 것.
    // 소품은 피벗이 뒤끝(몸체 +Z) 이라 반대로 판정, 앞뒤 대칭(자전거·화분)은 건물에서 본 결과를 따른다.
    static bool buildingsFlipped;
    static GameObject Build(string fbx, string id, Material mat, out Bounds b, out bool flipped, bool prop = false)
    {
        var mi = AssetImporter.GetAtPath(fbx) as ModelImporter;
        mi.globalScale = 1f; mi.useFileScale = true; mi.bakeAxisConversion = true;
        mi.materialImportMode = ModelImporterMaterialImportMode.None;
        mi.importAnimation = false; mi.importCameras = false; mi.importLights = false; mi.isReadable = false;
        mi.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
        PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var go = new GameObject(id); inst.name = id + "_mesh";
        inst.transform.SetParent(go.transform, false); inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity; inst.transform.localScale = Vector3.one;
        foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        System.Func<Bounds> B = () => { var rs = go.GetComponentsInChildren<Renderer>(); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); return bb; };
        b = B(); flipped = false;
        bool flip = !prop ? b.center.z > 0.05f : Mathf.Abs(b.center.z) > 0.05f ? b.center.z < 0 : buildingsFlipped;
        if (!prop) buildingsFlipped = flip;
        if (flip) { inst.transform.localRotation = Quaternion.Euler(0, 180f, 0); b = B(); flipped = true; }
        return go;
    }

    public static string Run()
    {
        var sb = new StringBuilder();
        // 1) 건물
        var mats = new Dictionary<string, Material>();
        int vN = 0;
        foreach (var atlas in AtlasOf.Values.Concat(VarietyAtlas.Values).Distinct())
        {
            if (AssetImporter.GetAtPath(Dir + atlas + ".png") == null) continue;
            SetupTexture(Dir + atlas + ".png", sb); mats[atlas] = SetupMaterial(Dir, atlas);
        }
        foreach (var kv in Footprint)
        {
            string id = kv.Key, fbx = Dir + id + ".fbx";
            bool variety = VarietyAtlas.ContainsKey(id);
            if (AssetImporter.GetAtPath(fbx) == null) { if (!variety) sb.Append($"모델 없음 {fbx}\n"); continue; }   // 장부의 보류분(F1 에 없음)은 조용히 건너뜀
            string atl = variety ? VarietyAtlas[id] : AtlasOf[id];
            if (!mats.ContainsKey(atl)) { sb.Append($"아틀라스 없음 {atl} ({id})\n"); continue; }
            if (variety) vN++;
            var go = Build(fbx, id, mats[atl], out var b, out var flipped);
            var fp = kv.Value;
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0, b.max.y * .5f, -fp.y * .5f); box.size = new Vector3(fp.x, b.max.y, fp.y);
            PrefabUtility.SaveAsPrefabAsset(go, Dir + id + ".prefab");
            int tris = go.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
            sb.Append($"프리팹 {id} [{atl}] bounds min{b.min:F2} max{b.max:F2} flip{flipped} 박스 {box.size:F2} tris {tris}\n");
            Object.DestroyImmediate(go);
        }
        sb.Append($"변형 프리팹 {vN}개(장부 {VarietyAtlas.Count})\n");
        // 2) 소품
        var pmats = new Dictionary<string, Material>();
        foreach (var kv in PropOf)
        {
            string id = kv.Key, fbx = PropDir + id + ".fbx";
            if (AssetImporter.GetAtPath(fbx) == null) continue;
            string atlas = kv.Value.atlas;
            if (!pmats.ContainsKey(atlas)) { SetupTexture(PropDir + atlas + ".png", sb); pmats[atlas] = SetupMaterial(PropDir, atlas); }
            var go = Build(fbx, id, pmats[atlas], out var b, out var flipped, true);
            if (!kv.Value.wall)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = b.center; box.size = b.size;
            }
            PrefabUtility.SaveAsPrefabAsset(go, PropDir + id + ".prefab");
            int tris = go.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
            sb.Append($"소품 {id} [{atlas}] bounds min{b.min:F2} max{b.max:F2} flip{flipped} wall{kv.Value.wall} tris {tris}\n");
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }
}
