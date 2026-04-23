using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 한 번에:
/// 1. OrderJob 3개 + OrderCatalog 에셋 생성/갱신 (Assets/Data/Orders/)
/// 2. ComplexTrack 레이아웃에 추가 배달 셀 (총 8개) 추가 + 저장
/// 3. 현재 씬에 8개 DeliveryPoint 생성 + DeliveryManager 갱신
/// 4. RunRoot GameObject + RunController/OrderBoard/ResultPanel/CountdownHUD 부착
/// 5. Main Camera에 OverviewCamera 부착
/// </summary>
public static class SetupOrderSystem
{
    const string OrdersDir = "Assets/Data/Orders";
    const string CatalogPath = "Assets/Data/Orders/OrderCatalog.asset";

    [MenuItem("PitCrew/Setup Order System (B)")]
    public static void Run()
    {
        Directory.CreateDirectory(OrdersDir);

        // 1. OrderJob 3개
        var jobA = LoadOrCreate<OrderJob>($"{OrdersDir}/Order_A_Safe.asset");
        jobA.displayName = "A. 안전 배달";
        jobA.color = new Color(0.4f, 0.95f, 0.45f);
        jobA.pointCount = 3;
        jobA.timeLimitSec = 0f;
        jobA.baseReward = 500;
        jobA.timeBonus = 0;
        jobA.weightTier = 0;
        EditorUtility.SetDirty(jobA);

        var jobB = LoadOrCreate<OrderJob>($"{OrdersDir}/Order_B_Balanced.asset");
        jobB.displayName = "B. 균형 배달";
        jobB.color = new Color(0.95f, 0.85f, 0.25f);
        jobB.pointCount = 5;
        jobB.timeLimitSec = 300f; // 5분
        jobB.baseReward = 900;
        jobB.timeBonus = 200;
        jobB.weightTier = 1;
        EditorUtility.SetDirty(jobB);

        var jobC = LoadOrCreate<OrderJob>($"{OrdersDir}/Order_C_Risky.asset");
        jobC.displayName = "C. 도전 배달";
        jobC.color = new Color(1f, 0.4f, 0.4f);
        jobC.pointCount = 7;
        jobC.timeLimitSec = 360f; // 6분
        jobC.baseReward = 1800;
        jobC.timeBonus = 500;
        jobC.weightTier = 2;
        EditorUtility.SetDirty(jobC);

        var catalog = LoadOrCreate<OrderCatalog>(CatalogPath);
        catalog.jobs = new List<OrderJob> { jobA, jobB, jobC };
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();

        // 2. Layout SO 에 8개 배달 셀 보장 (ComplexTrack)
        var layoutGuids = AssetDatabase.FindAssets("t:TrackLayout ComplexTrackLayout");
        TrackLayout layout = null;
        if (layoutGuids.Length > 0)
            layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(layoutGuids[0]));

        if (layout != null)
        {
            // Layout SO가 truth. SetupComplexScene이 작성한 deliveries를 존중해 GameObject만 생성.
            Debug.Log($"[SetupOrderSystem] Layout 'ComplexTrackLayout' deliveries: {layout.deliveries.Count}개 (SO 기준).");
        }
        else
        {
            Debug.LogWarning("[SetupOrderSystem] ComplexTrackLayout 못 찾음. 씬 셋업은 진행하되 배달 셀 풀이 비어있을 수 있음.");
        }

        // 3. 현재 씬에 DeliveryPoint 생성 + DeliveryManager
        var oldRoot = GameObject.Find("DeliverySystem");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        var root = new GameObject("DeliverySystem");
        Undo.RegisterCreatedObjectUndo(root, "Setup Order System");

        var pointsList = new List<DeliveryPoint>();
        if (layout != null)
        {
            foreach (var cell in layout.deliveries)
            {
                var pos = new Vector3(cell.col * 6f, 0.5f, cell.row * 6f);
                var go = new GameObject(cell.label);
                go.transform.SetParent(root.transform);
                go.transform.position = pos;
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 3f;
                var dp = go.AddComponent<DeliveryPoint>();
                go.AddComponent<DeliveryPointMarker>();
                pointsList.Add(dp);
            }
        }

        var returnGo = new GameObject("ReturnZone");
        returnGo.transform.SetParent(root.transform);
        returnGo.transform.position = layout != null
            ? new Vector3(layout.startCell.x * 6f, 0.5f, layout.startCell.y * 6f)
            : Vector3.zero;

        var mgrGo = new GameObject("DeliveryManager");
        mgrGo.transform.SetParent(root.transform);
        var mgr = mgrGo.AddComponent<DeliveryManager>();
        mgr.SetPoints(pointsList, null); // ReturnZone은 MVP에서 미사용 (마지막 배달 = 정산)

        // 4. RunRoot 에 OrderBoard / RunController / ResultPanel / CountdownHUD
        var oldRunRoot = GameObject.Find("RunRoot");
        if (oldRunRoot != null) Object.DestroyImmediate(oldRunRoot);

        var runRoot = new GameObject("RunRoot");
        Undo.RegisterCreatedObjectUndo(runRoot, "Setup Order System");
        var board = runRoot.AddComponent<OrderBoard>();
        var ctrl = runRoot.AddComponent<RunController>();
        var result = runRoot.AddComponent<ResultPanel>();
        runRoot.AddComponent<CountdownHUD>();

        // DeliveryHUD (화살표) — RequireComponent(Canvas)이므로 별도 GameObject
        var oldHud = GameObject.Find("DeliveryHUD");
        if (oldHud != null) Object.DestroyImmediate(oldHud);
        var hudGo = new GameObject("DeliveryHUD");
        Undo.RegisterCreatedObjectUndo(hudGo, "Setup Order System");
        var canvas = hudGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var hud = hudGo.AddComponent<DeliveryHUD>();
        var soHud = new SerializedObject(hud);
        soHud.FindProperty("manager").objectReferenceValue = mgr;
        soHud.FindProperty("cam").objectReferenceValue = Camera.main;
        soHud.ApplyModifiedProperties();

        // OrderBoard.catalog 와이어링
        var so = new SerializedObject(board);
        so.FindProperty("catalog").objectReferenceValue = catalog;
        so.ApplyModifiedProperties();

        // RunController 와이어링
        var soCtrl = new SerializedObject(ctrl);
        soCtrl.FindProperty("deliveryManager").objectReferenceValue = mgr;
        soCtrl.FindProperty("orderBoard").objectReferenceValue = board;
        soCtrl.FindProperty("resultPanel").objectReferenceValue = result;
        soCtrl.ApplyModifiedProperties();

        // 5. Main Camera 에 OverviewCamera
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            // 기존 ortho/highangle 설정 정리 (perspective 로 전환)
            mainCam.orthographic = false;
            mainCam.fieldOfView = 60f;

            var ov = mainCam.GetComponent<OverviewCamera>();
            if (ov == null) ov = mainCam.gameObject.AddComponent<OverviewCamera>();

            // overviewCenter = layout 중심, 그리고 height/pitch/fov 도 명시적으로 (씬 직렬화 옛 값 덮어씀)
            if (layout != null)
            {
                float cx = (layout.Width - 1) * 6f * 0.5f;
                float cz = (layout.Height - 1) * 6f * 0.5f;
                var soOv = new SerializedObject(ov);
                soOv.FindProperty("overviewCenter").vector3Value = new Vector3(cx, 0f, cz);
                soOv.FindProperty("overviewHeight").floatValue = 95f;
                soOv.FindProperty("overviewPitchDeg").floatValue = 75f;
                soOv.FindProperty("overviewFov").floatValue = 55f;
                soOv.ApplyModifiedProperties();
            }

            // RunController.overviewCam 와이어링
            var soCtrl2 = new SerializedObject(ctrl);
            soCtrl2.FindProperty("overviewCam").objectReferenceValue = ov;
            soCtrl2.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[SetupOrderSystem] 완료. 배달지 {pointsList.Count}개, 카탈로그 {catalog.jobs.Count}개 작업.");
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }
}
