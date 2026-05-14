using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Phase 1 GameContext 자동 setup.
// 1. 씬에 GameContext GameObject 없으면 생성
// 2. AutoFindMissing이 모든 시스템 검색 → SerializedField로 자동 등록
// 3. 씬 저장
public static class SetupGameContext
{
    [MenuItem("PitCrew/Phase 1/Setup GameContext (Phase 1)")]
    public static void Execute()
    {
        var existing = Object.FindFirstObjectByType<GameContext>();
        GameContext ctx;
        if (existing == null)
        {
            var go = new GameObject("GameContext");
            ctx = go.AddComponent<GameContext>();
            Undo.RegisterCreatedObjectUndo(go, "Create GameContext");
            Debug.Log("[SetupGameContext] GameObject 'GameContext' 신규 생성.");
        }
        else
        {
            ctx = existing;
            Debug.Log("[SetupGameContext] 기존 GameContext 발견 — 참조 갱신만.");
        }

        // SerializedObject로 SerializedField 등록 (인스펙터 와이어링과 동등)
        var so = new SerializedObject(ctx);

        var dm = Object.FindFirstObjectByType<DeliveryManager>();
        var ov = Object.FindFirstObjectByType<OverviewCamera>();
        var ob = Object.FindFirstObjectByType<OrderBoard>();
        var rp = Object.FindFirstObjectByType<ResultPanel>();
        var fs = Object.FindFirstObjectByType<FuelSystem>();
        var cb = Object.FindFirstObjectByType<CargoBox>();
        var us = Object.FindFirstObjectByType<UpgradeSystem>();
        var ss = Object.FindFirstObjectByType<CargoShakeSimulator>();
        var rc = Object.FindFirstObjectByType<RunController>();
        var ac = Object.FindFirstObjectByType<ArcadeCarController>();
        var ts = Object.FindFirstObjectByType<TireSystem>();
        var gs = Object.FindFirstObjectByType<GearSystem>();

        SetRef(so, "deliveryManager", dm);
        SetRef(so, "overviewCamera", ov);
        SetRef(so, "orderBoard", ob);
        SetRef(so, "resultPanel", rp);
        SetRef(so, "fuelSystem", fs);
        SetRef(so, "cargoBox", cb);
        SetRef(so, "upgradeSystem", us);
        SetRef(so, "shakeSimulator", ss);
        SetRef(so, "runController", rc);
        SetRef(so, "carController", ac);
        SetRef(so, "tireSystem", ts);
        SetRef(so, "gearSystem", gs);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctx);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        int count = 0;
        if (dm != null) count++;
        if (ov != null) count++;
        if (ob != null) count++;
        if (rp != null) count++;
        if (fs != null) count++;
        if (cb != null) count++;
        if (us != null) count++;
        if (ss != null) count++;
        if (rc != null) count++;
        if (ac != null) count++;
        if (ts != null) count++;
        if (gs != null) count++;
        Debug.Log($"[SetupGameContext] 완료 — {count}/12 시스템 와이어링 (씬 저장됨).");
    }

    static void SetRef(SerializedObject so, string propName, Object obj)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) { Debug.LogWarning($"  → property '{propName}' 없음"); return; }
        prop.objectReferenceValue = obj;
    }
}
