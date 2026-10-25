using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 과제 0226: 아트 납품 통합 — 지형 질감 4종 · 주민 6종 · 랜드마크 4 · 시설 4 · 찻집 소품 3 → 임포트 설정·머티리얼·프리팹.
///  - 납품: art-20261026-0131(지형·주민), art-20261026-0155(랜드마크·찻집). 규격·인계 art/_review/2026-10-26-terrain-people/PROGRAMMER-HANDOFF.md
///  - 텍스처는 ImportBayLoopBuildings 와 같은 Point·무압축·밉 없음, 머티리얼은 PitCrew/LofiPilot/Surface(LofiShared 값).
///  - 지형: 새 셰이더 PitCrew/BayLoop/TerrainSlope + 새 머티리얼 Terrain/M_BL_TerrainSlope.mat (공용 Ground.mat·WorldSurface 는 그대로 — Proto·ComplexTrack 보호)
///  - 주민: Generic 아바타, Stand/Walk/Sit 루프, Resident.controller(int State, float WalkSpeed), 프리팹 루트에 Resident.
/// </summary>
public static class ImportBayLoopDelivery
{
    const string Art = "Assets/Art/BayLoop/";
    public const string PeopleDir = Art + "People/", TerrainDir = Art + "Terrain/", LandDir = Art + "Landmarks/";
    public const string TerrainMat = TerrainDir + "M_BL_TerrainSlope.mat";
    public static readonly string[] People = { "people_young_01", "people_young_02", "people_young_03", "people_elder_01", "people_elder_02", "people_elder_03" };

    [MenuItem("PitCrew/Map/Import BayLoop Delivery 0226 (terrain·people·landmarks)")]
    public static void Menu() => Debug.Log(Run());

    public static string Run()
    {
        var sb = new StringBuilder();
        Terrain(sb); Landmarks(sb); Facilities(sb); Residents(sb);
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }

    // ---------------------------------------------------------------- 지형
    static void Terrain(StringBuilder sb)
    {
        foreach (var n in new[] { "BL_TerrainAtlas_32ppm", "BL_Terrain_grass_32ppm", "BL_Terrain_soil_32ppm", "BL_Terrain_dry_grass_32ppm", "BL_Terrain_rock_32ppm" })
            ImportBayLoopBuildings.SetupTexture(TerrainDir + n + ".png", sb);
        var shader = Shader.Find("PitCrew/BayLoop/TerrainSlope");
        if (shader == null) { sb.Append("셰이더 TerrainSlope 없음\n"); return; }
        var m = AssetDatabase.LoadAssetAtPath<Material>(TerrainMat);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, TerrainMat); }
        m.shader = shader;
        // 빛·안개 값은 기존 Ground 와 같게(OpeningTutorial 아침 안개도 _Ambient/_Fog* 로 같이 움직인다)
        var g = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CoastalTown/Ground.mat");
        if (g != null) foreach (var p in new[] { "_Ambient", "_FogColor", "_Tint" }) if (g.HasProperty(p)) m.SetColor(p, g.GetColor(p));
        if (g != null) foreach (var p in new[] { "_FaceShade", "_FogStart", "_FogEnd" }) if (g.HasProperty(p)) m.SetFloat(p, g.GetFloat(p));
        m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainDir + "BL_TerrainAtlas_32ppm.png"));
        m.SetFloat("_WorldScale", 32f / (64f * .93f));   // SPEC: 셰이더 안쪽 93% 표본 → 정확 32px/m
        m.SetFloat("_PatchThreshold", 0.70f);   // 첫 캡처: 0.62 면 흙이 풀밭의 1/3 — 패치로 안 읽힘(가설: 0.70 ≈ 1/8)
        EditorUtility.SetDirty(m);
        sb.Append($"지형 머티리얼 {TerrainMat} 경사 마른풀≥{m.GetFloat("_DryDeg")}° 바위≥{m.GetFloat("_RockDeg")}°\n");
    }

    // ---------------------------------------------------------------- 랜드마크 (빌더 Landmark() 가 이름으로 찾는다)
    // col: 피벗 기준 BoxCollider (center, size). 크레인은 기둥만, 어시장 지붕은 네 귀 기둥만.
    static void Landmarks(StringBuilder sb)
    {
        var atlases = new[] { "HarborPhotoAtlas", "ChapelMarketPhotoAtlas", "FishMarketPhotoAtlas" };
        var mats = atlases.ToDictionary(a => a, a => { ImportBayLoopBuildings.SetupTexture(LandDir + a + ".png", sb); return ImportBayLoopBuildings.SetupMaterial(LandDir, a); });
        Make(LandDir, "lighthouse_01", mats["HarborPhotoAtlas"], sb, (go, b) => Box(go, new Vector3(0, 8f, 0), new Vector3(3.76f, 16f, 3.76f)));
        Make(LandDir, "harbor_crane_01", mats["HarborPhotoAtlas"], sb, (go, b) => Box(go, new Vector3(0, 9f, 0), new Vector3(3.7f, 18f, 3.7f)));
        Make(LandDir, "chapel_bell_01", mats["ChapelMarketPhotoAtlas"], sb, (go, b) => Box(go, new Vector3(0, 6.4f, -1.8f), new Vector3(3.6f, 12.8f, 3.6f)));
        Make(LandDir, "FishMarketRoof", mats["FishMarketPhotoAtlas"], sb, (go, b) =>
        {
            foreach (var x in new[] { -5.8f, 5.8f }) foreach (var z in new[] { -0.2f, -8.2f }) Box(go, new Vector3(x, 1.6f, z), new Vector3(0.4f, 3.2f, 0.4f));
        });
    }

    // ---------------------------------------------------------------- 시설·찻집·찻집 소품
    static void Facilities(StringBuilder sb)
    {
        string bd = Art + "Buildings/", pd = Art + "Props/";
        ImportBayLoopBuildings.SetupTexture(bd + "DeferredPhotoAtlas.png", sb); ImportBayLoopBuildings.SetupTexture(bd + "TeaHousePhotoAtlas.png", sb); ImportBayLoopBuildings.SetupTexture(pd + "TeaHousePhotoAtlas.png", sb);
        var defM = ImportBayLoopBuildings.SetupMaterial(bd, "DeferredPhotoAtlas"); var teaM = ImportBayLoopBuildings.SetupMaterial(bd, "TeaHousePhotoAtlas"); var teaPM = ImportBayLoopBuildings.SetupMaterial(pd, "TeaHousePhotoAtlas");
        // 건물: 본체 footprint 박스(빌더 Buildings() 와 같은 규칙 — 앞벽 z=0 에서 뒤로)
        foreach (var (id, m) in new[] { ("teahouse_01", teaM), ("bus_stop_01", defM), ("cafe_kiosk_01", defM), ("farmhouse_01", defM) })
        {
            var fp = ImportBayLoopBuildings.Footprint[id];
            Make(bd, id, m, sb, (go, b) => Box(go, new Vector3(0, b.max.y * .5f, -fp.y * .5f), new Vector3(fp.x, b.max.y, fp.y)));
        }
        // 소품: 배달지 바로 앞이라 콜라이더 없음(차가 문 앞 5m 안에 서야 한다 — 짐이 소품에 부딪혀 깨지지 않게, 가설)
        foreach (var id in new[] { "tea_table_01", "tea_tray_display_01", "tea_owner_bicycle_01" }) Make(pd, id, teaPM, sb, null, true);
    }

    static void Make(string dir, string id, Material m, StringBuilder sb, System.Action<GameObject, Bounds> col, bool prop = false)
    {
        string fbx = dir + id + ".fbx";
        if (AssetImporter.GetAtPath(fbx) == null) { sb.Append($"모델 없음 {fbx}\n"); return; }
        var go = ImportBayLoopBuildings.Build(fbx, id, m, out var b, out var flipped, prop);
        col?.Invoke(go, b);
        PrefabUtility.SaveAsPrefabAsset(go, dir + id + ".prefab");
        int tris = go.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
        sb.Append($"프리팹 {id} min{b.min:F2} max{b.max:F2} flip{flipped} 콜라이더 {go.GetComponents<BoxCollider>().Length} tris {tris}\n");
        Object.DestroyImmediate(go);
    }

    static void Box(GameObject go, Vector3 c, Vector3 s) { var bx = go.AddComponent<BoxCollider>(); bx.center = c; bx.size = s; }

    // ---------------------------------------------------------------- 주민
    static void Residents(StringBuilder sb)
    {
        ImportBayLoopBuildings.SetupTexture(PeopleDir + "PeoplePhotoAtlas.png", sb);
        var mat = ImportBayLoopBuildings.SetupMaterial(PeopleDir, "PeoplePhotoAtlas");
        // 1) 임포트 설정 먼저(루프 클립 생성) → 2) 그 클립으로 컨트롤러 → 3) 프리팹
        foreach (var id in People)
        {
            string fbx = PeopleDir + id + ".fbx";
            var mi = AssetImporter.GetAtPath(fbx) as ModelImporter; if (mi == null) { sb.Append($"모델 없음 {fbx}\n"); continue; }
            mi.globalScale = 1f; mi.useFileScale = true; mi.bakeAxisConversion = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false;
            mi.animationType = ModelImporterAnimationType.Generic; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = true;
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips) { c.loopTime = true; c.name = c.name.Replace("ResidentRig|", ""); }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
        }
        var ctrl = Controller(sb);
        foreach (var id in People)
        {
            string fbx = PeopleDir + id + ".fbx";
            if (AssetImporter.GetAtPath(fbx) == null) continue;
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var go = new GameObject(id); inst.name = id + "_model";
            inst.transform.SetParent(go.transform, false);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.sharedMaterials = Enumerable.Repeat(mat, Mathf.Max(1, r.sharedMaterials.Length)).ToArray(); if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = false; }
            var an = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            an.avatar = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>().FirstOrDefault();
            go.AddComponent<Resident>().anim = an;
            PrefabUtility.SaveAsPrefabAsset(go, PeopleDir + id + ".prefab");
            var bb = go.GetComponentsInChildren<Renderer>()[0].bounds;
            sb.Append($"주민 {id} 키 {bb.size.y:F2}m 클립 {string.Join(",", clips.Select(c => c.name))} 아바타 {(an.avatar != null ? an.avatar.isValid.ToString() : "없음")}\n");
            Object.DestroyImmediate(go);
        }
    }

    static AnimatorController Controller(StringBuilder sb)
    {
        string path = PeopleDir + "Resident.controller";
        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("State", AnimatorControllerParameterType.Int);
        ctrl.AddParameter(new AnimatorControllerParameter { name = "WalkSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
        var sm = ctrl.layers[0].stateMachine;
        // 클립은 첫 모델 것을 같이 쓴다(6종 같은 ResidentRig — 본 이름 동일, i0226-probe)
        var src = PeopleDir + People[0] + ".fbx";
        AnimationClip Clip(string n) => AssetDatabase.LoadAllAssetsAtPath(src).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).FirstOrDefault(c => c.name == n || c.name.EndsWith("|" + n));
        var names = new[] { "Stand", "Walk", "Sit" };
        for (int i = 0; i < 3; i++)
        {
            var st = sm.AddState(names[i]); st.motion = Clip(names[i]);
            if (i == 1) { st.speedParameterActive = true; st.speedParameter = "WalkSpeed"; }
            if (i == 0) sm.defaultState = st;
            var tr = sm.AddAnyStateTransition(st); tr.hasExitTime = false; tr.duration = 0.15f; tr.canTransitionToSelf = false;
            tr.AddCondition(AnimatorConditionMode.Equals, i, "State");
            sb.Append($"상태 {names[i]} 클립 {(st.motion is AnimationClip ac ? $"{ac.name} {ac.length:F2}s loop={ac.isLooping}" : "없음")}\n");
        }
        return ctrl;
    }
}
