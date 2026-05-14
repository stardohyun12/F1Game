using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixRunControllerWiring
{
    public static void Execute()
    {
        var ctrl = Object.FindFirstObjectByType<RunController>();
        if (ctrl == null) { Debug.LogError("[Wire] RunController not found."); return; }

        var mgr = Object.FindFirstObjectByType<DeliveryManager>();
        var board = Object.FindFirstObjectByType<OrderBoard>();
        var result = Object.FindFirstObjectByType<ResultPanel>();

        // OverviewCamera는 Main Camera 또는 별도 카메라에 컴포넌트로 붙어 있음.
        var ov = Object.FindFirstObjectByType<OverviewCamera>();
        if (ov == null)
        {
            // 없으면 Main Camera에 추가.
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                ov = mainCam.gameObject.AddComponent<OverviewCamera>();
                Debug.Log("[Wire] OverviewCamera added to Main Camera.");
            }
        }

        var so = new SerializedObject(ctrl);
        var pMgr = so.FindProperty("deliveryManager");
        var pBoard = so.FindProperty("orderBoard");
        var pResult = so.FindProperty("resultPanel");
        var pOv = so.FindProperty("overviewCam");

        if (pMgr != null) pMgr.objectReferenceValue = mgr;
        if (pBoard != null) pBoard.objectReferenceValue = board;
        if (pResult != null) pResult.objectReferenceValue = result;
        if (pOv != null) pOv.objectReferenceValue = ov;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(ctrl);
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Wire] RunController wired: mgr={mgr?.name}, board={board?.name}, result={result?.name}, ov={ov?.name}");
    }
}
