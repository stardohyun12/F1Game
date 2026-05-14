using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// SetupOrderSystem의 OpenWorld 변종.
// SetupOrderSystem은 ComplexTrackLayout을 하드코드 — OpenWorld_v2 씬에서는 OpenWorldLayout을 강제 주입해야 함.
// 동일 단계: DeliverySystem(8 DeliveryPoints) + RunRoot(OrderBoard/RunController/ResultPanel) + CountdownHUD/DeliveryHUD + OverviewCamera.
public static class SetupOrderSystemOpenWorld
{
    const string LayoutPath = "Assets/Data/OpenWorldLayout.asset";
    const string CatalogPath = "Assets/Data/Orders/OrderCatalogExpanded.asset";
    const float CellSize = 6f;

    [MenuItem("PitCrew/Layout/Setup Order System (OpenWorld)")]
    public static void Execute()
    {
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(LayoutPath);
        if (layout == null) { Debug.LogError($"OpenWorldLayout not found: {LayoutPath}"); return; }
        var catalog = AssetDatabase.LoadAssetAtPath<OrderCatalog>(CatalogPath);
        if (catalog == null) { Debug.LogError($"OrderCatalogExpanded not found: {CatalogPath}"); return; }

        // 1. DeliverySystem
        var oldDS = GameObject.Find("DeliverySystem");
        if (oldDS != null) Object.DestroyImmediate(oldDS);
        var dsRoot = new GameObject("DeliverySystem");

        var pointsList = new List<DeliveryPoint>();
        foreach (var cell in layout.deliveries)
        {
            var pos = new Vector3(cell.col * CellSize, 0.5f, cell.row * CellSize);
            var go = new GameObject(cell.label);
            go.transform.SetParent(dsRoot.transform);
            go.transform.position = pos;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 3f;
            var dp = go.AddComponent<DeliveryPoint>();
            go.AddComponent<DeliveryPointMarker>();
            pointsList.Add(dp);
        }

        var returnGo = new GameObject("ReturnZone");
        returnGo.transform.SetParent(dsRoot.transform);
        returnGo.transform.position = new Vector3(layout.startCell.x * CellSize, 0.5f, layout.startCell.y * CellSize);

        var mgrGo = new GameObject("DeliveryManager");
        mgrGo.transform.SetParent(dsRoot.transform);
        var mgr = mgrGo.AddComponent<DeliveryManager>();
        mgr.SetPoints(pointsList, null);

        // 2. RunRoot
        var oldRR = GameObject.Find("RunRoot");
        if (oldRR != null) Object.DestroyImmediate(oldRR);
        var runRoot = new GameObject("RunRoot");
        var board = runRoot.AddComponent<OrderBoard>();
        var ctrl = runRoot.AddComponent<RunController>();
        var result = runRoot.AddComponent<ResultPanel>();

        // 3. CountdownHUD (UGUI)
        var oldCD = GameObject.Find("UIRoot/CountdownHUD");
        if (oldCD != null) Object.DestroyImmediate(oldCD);
        var uiRootT = UIRoot.Ensure();
        var cdGo = new GameObject("CountdownHUD", typeof(RectTransform));
        cdGo.transform.SetParent(uiRootT, false);
        cdGo.AddComponent<CountdownHUD>();

        // 4. DeliveryHUD
        var oldHUD = GameObject.Find("DeliveryHUD");
        if (oldHUD != null) Object.DestroyImmediate(oldHUD);
        var hudGo = new GameObject("DeliveryHUD");
        var canvas = hudGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var hud = hudGo.AddComponent<DeliveryHUD>();
        var soHud = new SerializedObject(hud);
        soHud.FindProperty("manager").objectReferenceValue = mgr;
        soHud.FindProperty("cam").objectReferenceValue = Camera.main;
        soHud.ApplyModifiedProperties();

        // 5. OrderBoard.catalog
        var soBoard = new SerializedObject(board);
        soBoard.FindProperty("catalog").objectReferenceValue = catalog;
        soBoard.ApplyModifiedProperties();

        // 6. RunController 와이어링
        var soCtrl = new SerializedObject(ctrl);
        soCtrl.FindProperty("deliveryManager").objectReferenceValue = mgr;
        soCtrl.FindProperty("orderBoard").objectReferenceValue = board;
        soCtrl.FindProperty("resultPanel").objectReferenceValue = result;
        soCtrl.ApplyModifiedProperties();

        // 7. OverviewCamera
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = false;
            mainCam.fieldOfView = 60f;
            var ov = mainCam.GetComponent<OverviewCamera>();
            if (ov == null) ov = mainCam.gameObject.AddComponent<OverviewCamera>();

            float cx = (layout.Width - 1) * CellSize * 0.5f;
            float cz = (layout.Height - 1) * CellSize * 0.5f;
            var soOv = new SerializedObject(ov);
            soOv.FindProperty("overviewCenter").vector3Value = new Vector3(cx, 0f, cz);
            soOv.FindProperty("overviewHeight").floatValue = 110f;
            soOv.FindProperty("overviewPitchDeg").floatValue = 75f;
            soOv.FindProperty("overviewFov").floatValue = 55f;
            soOv.ApplyModifiedProperties();

            var soCtrl2 = new SerializedObject(ctrl);
            soCtrl2.FindProperty("overviewCam").objectReferenceValue = ov;
            soCtrl2.ApplyModifiedProperties();
        }

        // 8. EventSystem (UGUI 클릭)
        if (GameObject.Find("EventSystem") == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[SetupOrderSystemOpenWorld] 완료. 배달지 {pointsList.Count}개, 카탈로그 {catalog.jobs.Count}개 작업.");
    }
}
