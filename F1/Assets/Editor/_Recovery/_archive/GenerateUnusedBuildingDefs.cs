using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 미사용 Kenny FBX → BuildingDef 일괄 생성. 세션 17 OpenWorld_v2 위해.
// 측정: 각 FBX 인스턴스 bounds → max(x,z) = nativeSize.
// cellsWide/cellsDeep: nativeSize ≤ 1.4 → 1x1, 그 이상 → 2x2.
// industrial 20 + commercial low-detail 일부 + commercial big building gap fill.
public static class GenerateUnusedBuildingDefs
{
    const string OutDir = "Assets/Data/Buildings/";

    struct Spec
    {
        public string sourcePath;   // FBX path
        public string defName;      // .asset name (without extension)
        public string category;
        public bool addCollider;
    }

    static readonly Spec[] Specs = BuildSpecs();

    static Spec[] BuildSpecs()
    {
        var list = new List<Spec>();
        const string indRoot = "Assets/Model/kenney_city-kit-industrial_1.0/Models/FBX format/";
        const string comRoot = "Assets/Model/kenney_city-kit-commercial_2.1/Models/FBX format/";

        // industrial building-a ~ building-t (20개)
        for (char c = 'a'; c <= 't'; c++)
        {
            list.Add(new Spec {
                sourcePath = $"{indRoot}building-{c}.fbx",
                defName = $"industrial_building-{c}",
                category = "industrial",
                addCollider = true,
            });
        }

        // industrial detail-tank
        list.Add(new Spec {
            sourcePath = $"{indRoot}detail-tank.fbx",
            defName = "industrial_detail-tank",
            category = "industrial",
            addCollider = true,
        });

        // commercial low-detail building-a ~ n (배경 채움용 - 14개)
        for (char c = 'a'; c <= 'n'; c++)
        {
            list.Add(new Spec {
                sourcePath = $"{comRoot}low-detail-building-{c}.fbx",
                defName = $"commercial_low-detail-building-{c}",
                category = "commercial-low",
                addCollider = true,
            });
        }

        // commercial big building 누락분 (e, j, k, n은 이미 있음 — 다른 알파벳도 만들기)
        // Skip — big buildings already covered in catalog.

        // commercial detail-awning, detail-overhang, detail-parasol-a/b
        // (작은 props지만 매장 입구 분위기용. 1x1로 배치 가능)
        string[] decor = { "detail-awning", "detail-awning-wide", "detail-overhang", "detail-overhang-wide", "detail-parasol-a", "detail-parasol-b" };
        foreach (var d in decor)
        {
            list.Add(new Spec {
                sourcePath = $"{comRoot}{d}.fbx",
                defName = $"commercial_{d}",
                category = "commercial-decor",
                addCollider = false,    // 작은 데코는 통과 가능하게
            });
        }

        return list.ToArray();
    }

    [MenuItem("PitCrew/Layout/Generate Unused BuildingDefs")]
    public static void Execute()
    {
        if (!AssetDatabase.IsValidFolder(OutDir.TrimEnd('/')))
        {
            Debug.LogError($"GenerateUnusedBuildingDefs: '{OutDir}' 폴더 없음 — 중단");
            return;
        }

        int created = 0, skipped = 0, missing = 0;
        foreach (var s in Specs)
        {
            string assetPath = $"{OutDir}{s.defName}.asset";
            if (File.Exists(assetPath))
            {
                skipped++;
                continue;
            }

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(s.sourcePath);
            if (fbx == null)
            {
                Debug.LogWarning($"missing FBX: {s.sourcePath}");
                missing++;
                continue;
            }

            // Measure bounds
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            var renderers = inst.GetComponentsInChildren<MeshRenderer>();
            float native = 1f;
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                native = Mathf.Max(b.size.x, b.size.z);
                if (native < 0.1f) native = 1f;
            }
            Object.DestroyImmediate(inst);

            int cellsWide = native > 1.4f ? 2 : 1;
            int cellsDeep = native > 1.4f ? 2 : 1;
            // Decor (parasol etc.) tend to be very small; force 1x1 even if native tiny
            if (s.category == "commercial-decor")
            {
                cellsWide = 1;
                cellsDeep = 1;
            }

            var def = ScriptableObject.CreateInstance<BuildingDef>();
            def.prefab = fbx;
            def.nativeSize = native;
            def.entryLocalDir = new Vector3(0f, 0f, 1f);
            def.cellsWide = cellsWide;
            def.cellsDeep = cellsDeep;
            def.category = s.category;
            def.addCollider = s.addCollider;

            AssetDatabase.CreateAsset(def, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"GenerateUnusedBuildingDefs: created={created}, skipped(existing)={skipped}, missing(FBX)={missing}");
    }
}
