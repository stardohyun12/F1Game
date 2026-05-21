using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 화물 원물 카탈로그 빌드 (B안, 2026-05-20~21). 데이터 테이블 기반 — 4종(Glass/Eggs/Fish/Cake).
/// 각 종: Meshy fbx → import scale 정규화(1유닛) → 단색 머티리얼 → 정규화 프리팹 → 카탈로그 엔트리.
/// fbx 없는 종은 스킵(부분 생성 대응). Box는 엔트리 없음 = 큐브+카드보드 폴백.
///
/// import scale은 transform이 아니라 ModelImporter에서 baking — WrapRibbon 미터 임계가
/// 부모-로컬 기준(큐브 1:1 가정)이라 transform 스케일로 키우면 리본이 깨짐 (incident 2026-05-21).
/// </summary>
public static class CargoCatalogSetup
{
    struct Def
    {
        public CargoType type;
        public string fbx;     // Assets/... 경로
        public Color color;    // 단색 (Kenney 결 — 쨍한 단색)
        public Def(CargoType t, string f, Color c) { type = t; fbx = f; color = c; }
    }

    static readonly Def[] Defs =
    {
        new(CargoType.Glass, "Assets/Model/AI/wine_bottle_lowpoly.fbx", new Color(0.047f, 0.278f, 0.129f)), // #0C4721 진초록 유리
        new(CargoType.Eggs,  "Assets/Model/AI/eggs_carton_01.fbx",      new Color(0.804f, 0.722f, 0.553f)), // #CDB88D 베이지 카톤
        new(CargoType.Fish,  "Assets/Model/AI/fish_01.fbx",             new Color(0.616f, 0.706f, 0.780f)), // #9DB4C7 실버블루
        new(CargoType.Cake,  "Assets/Model/AI/cake_01.fbx",             new Color(0.902f, 0.659f, 0.737f)), // #E6A8BC 소프트핑크
    };

    const string MatDir      = "Assets/Materials/Cargo";
    const string PrefabDir   = "Assets/Prefabs/Cargo";
    const string CatalogDir  = "Assets/Resources/CargoModels";
    const string CatalogPath = CatalogDir + "/CargoModelCatalog.asset";

    [MenuItem("PitCrew/Cargo/Build Model Catalog")]
    public static void Build()
    {
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(PrefabDir);
        Directory.CreateDirectory(CatalogDir);

        var entries = new List<(CargoType type, GameObject prefab)>();

        foreach (var d in Defs)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(d.fbx);
            if (fbx == null) { Debug.Log($"[CargoCatalogSetup] {d.type} 스킵 — {d.fbx} 없음"); continue; }

            NormalizeImportScale(d.fbx, 1.0f);

            // 단색 머티리얼
            var matPath = $"{MatDir}/Mat_Cargo_{d.type}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
            mat.SetColor("_BaseColor", d.color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);

            // 프리팹 (root → 정규화 자식 → 메시). import scale 정규화 후라 자식 스케일≈1.
            var prefabPath = $"{PrefabDir}/Cargo_{d.type}.prefab";
            var root = new GameObject($"Cargo_{d.type}");
            try
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                inst.transform.SetParent(root.transform, false);

                Bounds b = default; bool init = false;
                foreach (var mf in inst.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    if (!init) { b = mf.sharedMesh.bounds; init = true; } else b.Encapsulate(mf.sharedMesh.bounds);
                }
                float maxD = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (maxD <= 0f) maxD = 1f;
                inst.transform.localScale = Vector3.one / maxD;
                inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z) / maxD;

                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = mat;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { Object.DestroyImmediate(root); }

            entries.Add((d.type, AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)));
        }

        // 카탈로그 SO
        var cat = AssetDatabase.LoadAssetAtPath<CargoModelCatalog>(CatalogPath);
        if (cat == null) { cat = ScriptableObject.CreateInstance<CargoModelCatalog>(); AssetDatabase.CreateAsset(cat, CatalogPath); }
        var so = new SerializedObject(cat);
        var prop = so.FindProperty("entries");
        prop.arraySize = entries.Count;
        for (int i = 0; i < entries.Count; i++)
        {
            var el = prop.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("cargoType").enumValueIndex = (int)entries[i].type;
            el.FindPropertyRelative("modelPrefab").objectReferenceValue = entries[i].prefab;
            el.FindPropertyRelative("uniformScale").floatValue = 1.0f;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CargoCatalogSetup] 완료 — {entries.Count}종 와이어: {string.Join(", ", entries.ConvertAll(e => e.type.ToString()))}");
    }

    /// <summary>
    /// 모델 import scale을 메시 최대변이 targetMaxDim(유닛)이 되도록 조정. 멱등.
    /// useFileScale=false 기준 native 크기 측정 후 globalScale 역산 → 재import.
    /// </summary>
    static void NormalizeImportScale(string path, float targetMaxDim)
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(path);
        if (imp == null) { Debug.LogWarning($"[CargoCatalogSetup] ModelImporter 없음 {path}"); return; }

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
        Debug.Log($"[CargoCatalogSetup] {path}: native={native:F3} → globalScale={scale:F3} (maxDim≈{MeshMaxDim(path):F3})");
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
}
