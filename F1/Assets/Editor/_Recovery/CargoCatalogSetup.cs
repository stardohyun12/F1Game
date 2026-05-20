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

    [MenuItem("PitCrew/Cargo/Build Model Catalog")]
    public static void Build()
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(WineFbx);
        if (fbx == null) { Debug.LogError($"[CargoCatalogSetup] {WineFbx} 없음"); return; }

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
        el.FindPropertyRelative("uniformScale").floatValue = 0.5f;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CargoCatalogSetup] 완료 — mat={MatPath} prefab={PrefabPath} catalog Glass→Cargo_Glass");
    }
}
