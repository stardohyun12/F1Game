using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 화물 원물 카탈로그 빌드 (B안, 2026-05-20). Glass → 로우폴리 와인병 프리팹(초록 유리 머티리얼).
/// 프리팹은 메시를 ~1유닛으로 정규화한 자식 래퍼 구조 → root 스케일을 PackingScreen/트렁크가
/// 큐브와 동일하게 다룰 수 있음 (트렁크 EnterPlacement가 root.localScale=ItemScale로 덮어씀).
/// </summary>
public static class CargoCatalogSetup
{
    const string WineFbx     = "Assets/Model/AI/wine_bottle_lowpoly.fbx";
    const string MatDir      = "Assets/Materials/Cargo";
    const string MatPath     = MatDir + "/Mat_Cargo_Glass.mat";
    const string PrefabDir   = "Assets/Prefabs/Cargo";
    const string PrefabPath  = PrefabDir + "/Cargo_Glass.prefab";
    const string CatalogDir  = "Assets/Resources/CargoModels";
    const string CatalogPath = CatalogDir + "/CargoModelCatalog.asset";

    /// <summary>
    /// 모델 import scale을 메시 최대변이 targetMaxDim(유닛)이 되도록 조정. 멱등.
    /// useFileScale=false 기준 native 크기를 측정 후 globalScale 역산 → 재import.
    /// Meshy 출력은 종마다 native 크기가 달라 매 자산 정규화 필요.
    /// </summary>
    static void NormalizeImportScale(string path, float targetMaxDim)
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(path);
        if (imp == null) { Debug.LogWarning($"[CargoCatalogSetup] ModelImporter 없음 {path}"); return; }

        // 기준 측정: useFileScale=false, globalScale=1 에서의 native 최대변.
        if (imp.useFileScale || !Mathf.Approximately(imp.globalScale, 1f))
        {
            imp.useFileScale = false; imp.globalScale = 1f;
            EditorUtility.SetDirty(imp); imp.SaveAndReimport();
        }
        float native = MeshMaxDim(path);
        if (native <= 0f) { Debug.LogWarning("[CargoCatalogSetup] native maxDim 0"); return; }

        float scale = targetMaxDim / native;
        if (!Mathf.Approximately(imp.globalScale, scale))
        {
            imp.globalScale = scale;
            EditorUtility.SetDirty(imp); imp.SaveAndReimport();
        }
        Debug.Log($"[CargoCatalogSetup] import scale 정규화 {path}: native={native:F3} → globalScale={scale:F3} (maxDim≈{MeshMaxDim(path):F3})");
    }

    static float MeshMaxDim(string path)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Bounds b = default; bool init = false;
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            if (!init) { b = mf.sharedMesh.bounds; init = true; } else b.Encapsulate(mf.sharedMesh.bounds);
        }
        return Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
    }

    [MenuItem("PitCrew/Cargo/Build Model Catalog")]
    public static void Build()
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(WineFbx);
        if (fbx == null) { Debug.LogError($"[CargoCatalogSetup] {WineFbx} 없음"); return; }

        // ── 0. import scale 정규화 (메시를 ~1유닛으로) — D-073 incident 2026-05-21 ──
        // WrapRibbon의 미터 임계(minSampleDist·width·lift)는 부모-로컬 기준 = 큐브 1:1 가정.
        // 메시를 transform 스케일(52x)로 키우면 부모 lossyScale이 깨져 리본이 안 생김.
        // → 스케일을 transform이 아니라 import에서 baking. 부모 lossyScale≈1 유지.
        NormalizeImportScale(WineFbx, 1.0f);
        fbx = AssetDatabase.LoadAssetAtPath<GameObject>(WineFbx);

        // ── 1. 초록 유리 머티리얼 ──
        Directory.CreateDirectory(MatDir);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        mat.SetColor("_BaseColor", new Color(0.047f, 0.278f, 0.129f)); // #0C4721
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);

        // ── 2. 프리팹 (root → 정규화 자식 → fbx 메시) ──
        Directory.CreateDirectory(PrefabDir);
        var root = new GameObject("Cargo_Glass");
        try
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            inst.transform.SetParent(root.transform, false);

            // 메시 결합 bounds → 최대변 1.0 정규화
            Bounds b = default; bool init = false;
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                if (!init) { b = mf.sharedMesh.bounds; init = true; } else b.Encapsulate(mf.sharedMesh.bounds);
            }
            float maxD = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (maxD <= 0f) maxD = 1f;
            inst.transform.localScale = Vector3.one / maxD;
            // 바닥 기준 정렬 — 정규화 후 중심을 살짝 올려 바닥(y=0)에 닿게
            inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z) / maxD;

            // 초록 머티리얼 적용
            foreach (var r in inst.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = mat;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        // ── 3. 카탈로그 SO ──
        Directory.CreateDirectory(CatalogDir);
        var cat = AssetDatabase.LoadAssetAtPath<CargoModelCatalog>(CatalogPath);
        if (cat == null)
        {
            cat = ScriptableObject.CreateInstance<CargoModelCatalog>();
            AssetDatabase.CreateAsset(cat, CatalogPath);
        }
        var so = new SerializedObject(cat);
        var prop = so.FindProperty("entries");
        prop.arraySize = 1;
        var el = prop.GetArrayElementAtIndex(0);
        el.FindPropertyRelative("cargoType").enumValueIndex = (int)CargoType.Glass;
        el.FindPropertyRelative("modelPrefab").objectReferenceValue = prefab;
        el.FindPropertyRelative("uniformScale").floatValue = 1.0f; // 작업대 = 옛 큐브 크기(1유닛). 트렁크는 ItemScale로 별도.
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CargoCatalogSetup] 완료 — mat={MatPath} prefab={PrefabPath} catalog Glass→Cargo_Glass");
    }
}
