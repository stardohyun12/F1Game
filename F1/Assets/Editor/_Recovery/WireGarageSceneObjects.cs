#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PitCrew.Garage;

public static class WireGarageSceneObjects
{
    [MenuItem("PitCrew/Garage/Wire Scene Objects")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) { Debug.LogError("[WireGarageSceneObjects] No active scene"); return; }

        var ctx = Object.FindFirstObjectByType<GameContext>();
        if (ctx == null) { Debug.LogError("[WireGarageSceneObjects] GameContext 없음"); return; }

        // F1RestoreSystem GO
        var restoreGo = GameObject.Find("F1RestoreSystem") ?? new GameObject("F1RestoreSystem");
        var restoreSys = restoreGo.GetComponent<F1RestoreSystem>() ?? restoreGo.AddComponent<F1RestoreSystem>();
        var catalog = AssetDatabase.LoadAssetAtPath<F1RestoreCatalog>("Assets/Data/Garage/F1RestoreCatalog.asset");
        if (catalog == null) Debug.LogError("[WireGarageSceneObjects] F1RestoreCatalog asset 못 찾음 (Assets/Data/Garage/F1RestoreCatalog.asset)");
        var soRestore = new SerializedObject(restoreSys);
        var pCatalog = soRestore.FindProperty("catalog");
        if (pCatalog != null) pCatalog.objectReferenceValue = catalog;
        soRestore.ApplyModifiedProperties();

        // BoostSystem GO
        var boostGo = GameObject.Find("BoostSystem") ?? new GameObject("BoostSystem");
        var boostSys = boostGo.GetComponent<BoostSystem>() ?? boostGo.AddComponent<BoostSystem>();

        // TireWearStore GO
        var twGo = GameObject.Find("TireWearStore") ?? new GameObject("TireWearStore");
        var twSys = twGo.GetComponent<TireWearStore>() ?? twGo.AddComponent<TireWearStore>();

        // GarageScreen GO (+ cutscene 자식)
        var garageGo = GameObject.Find("GarageScreen") ?? new GameObject("GarageScreen");
        var garage = garageGo.GetComponent<GarageScreen>() ?? garageGo.AddComponent<GarageScreen>();
        var cutGo = GameObject.Find("GarageCutscene") ?? new GameObject("GarageCutscene");
        cutGo.transform.SetParent(garageGo.transform, false);
        var cut = cutGo.GetComponent<GarageRestoreCutscene>() ?? cutGo.AddComponent<GarageRestoreCutscene>();
        var soGarage = new SerializedObject(garage);
        var pCut = soGarage.FindProperty("cutscene");
        if (pCut != null) pCut.objectReferenceValue = cut;
        soGarage.ApplyModifiedProperties();

        // GameContext wiring
        var soCtx = new SerializedObject(ctx);
        var pRestore = soCtx.FindProperty("restoreSystem");
        var pBoost = soCtx.FindProperty("boostSystem");
        var pTw = soCtx.FindProperty("tireWearStore");
        if (pRestore != null) pRestore.objectReferenceValue = restoreSys;
        if (pBoost != null) pBoost.objectReferenceValue = boostSys;
        if (pTw != null) pTw.objectReferenceValue = twSys;
        soCtx.ApplyModifiedProperties();

        if (pRestore == null) Debug.LogWarning("[WireGarageSceneObjects] GameContext.restoreSystem SerializedField 못 찾음");
        if (pBoost == null) Debug.LogWarning("[WireGarageSceneObjects] GameContext.boostSystem SerializedField 못 찾음");
        if (pTw == null) Debug.LogWarning("[WireGarageSceneObjects] GameContext.tireWearStore SerializedField 못 찾음");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[WireGarageSceneObjects] 완료 — 4 GameObject + GameContext wiring");
    }
}
#endif
