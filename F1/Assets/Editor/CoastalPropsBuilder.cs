using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 2026-10-13 Claude — 브리프 town-density. lofi_pilot.py 가 새로 뱉은 소품·도로 디테일 FBX 를 프리팹으로.
/// Codex 의 LofiPilotBuilder 는 전체 재빌드+리뷰 캡처까지 돌리므로, 새 16종만 가볍게 만든다. 머티리얼은 같은 LofiShared.
/// 콜라이더: 바닥 소품 = MeshCollider / 작은 것 = BoxCollider / 바닥 데칼·벽걸이 = 없음(주행 방해·충돌 없게).
/// 메뉴: PitCrew/Art/Coastal Town/Build Prop Prefabs
/// </summary>
public static class CoastalPropsBuilder
{
    const string Dir = "Assets/Art/LofiPilot";
    static readonly string[] Names = { "Awning", "Planter", "Pots", "Bench", "Barrel", "AcUnit", "WallLamp", "Clothesline",
                                       "SignTea", "SignIce", "SignRest", "Bollard", "CrateStack", "Crosswalk", "Manhole", "Drain" };
    public static readonly HashSet<string> NoCollider = new HashSet<string> { "Awning", "AcUnit", "WallLamp", "Crosswalk", "Manhole", "Drain" };
    static readonly HashSet<string> BoxCol = new HashSet<string> { "Pots", "Bollard", "CrateStack", "Barrel" };

    static Bounds Bounds(GameObject go) { var rr = go.GetComponentsInChildren<Renderer>(); var b = rr[0].bounds; foreach (var r in rr) b.Encapsulate(r.bounds); return b; }

    [MenuItem("PitCrew/Art/Coastal Town/Build Prop Prefabs")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var surface = AssetDatabase.LoadAssetAtPath<Material>(Dir + "/LofiShared.mat");
        if (surface == null) throw new Exception("LofiShared.mat 없음 — Lofi Pilot/Build and Capture 먼저");
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/LofiAtlas.png");
        surface.SetTexture("_MainTex", atlas); EditorUtility.SetDirty(surface);
        var log = new List<string>();
        foreach (var name in Names)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.fbx");
            if (fbx == null) { log.Add(name + ": FBX 없음"); continue; }
            var go = new GameObject(name);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx); visual.transform.SetParent(go.transform, false);
            foreach (var r in visual.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials = new[] { surface };
            if (BoxCol.Contains(name)) { var b = Bounds(go); var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size; }
            else if (!NoCollider.Contains(name)) foreach (var mf in visual.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{name}.prefab"); var bb = Bounds(go);
            log.Add($"{name}: size={bb.size:F2} min.y={bb.min.y:F2}");
            UnityEngine.Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[CoastalProps] " + string.Join(" | ", log));
    }
}
