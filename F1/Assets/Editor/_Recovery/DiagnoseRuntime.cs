using UnityEditor;
using UnityEngine;

public static class DiagnoseRuntime
{
    public static void Execute()
    {
        var board = Object.FindFirstObjectByType<OrderBoard>();
        if (board != null)
        {
            var so = new SerializedObject(board);
            var panel = so.FindProperty("panelRoot")?.objectReferenceValue;
            var container = so.FindProperty("cardsContainer")?.objectReferenceValue;
            var prefab = so.FindProperty("cardPrefab")?.objectReferenceValue;
            var catalog = so.FindProperty("catalog")?.objectReferenceValue;
            Debug.Log($"[Diag] OrderBoard panelRoot={panel?.name ?? "NULL"}, container={container?.name ?? "NULL"}, prefab={prefab?.name ?? "NULL"}, catalog={catalog?.name ?? "NULL"}");
        }
        else Debug.LogError("[Diag] No OrderBoard.");

        var ctrl = Object.FindFirstObjectByType<RunController>();
        if (ctrl != null)
        {
            var so = new SerializedObject(ctrl);
            Debug.Log($"[Diag] RunController: " +
                $"deliveryManager={so.FindProperty("deliveryManager")?.objectReferenceValue?.name ?? "NULL"}, " +
                $"orderBoard={so.FindProperty("orderBoard")?.objectReferenceValue?.name ?? "NULL"}, " +
                $"resultPanel={so.FindProperty("resultPanel")?.objectReferenceValue?.name ?? "NULL"}, " +
                $"overviewCam={so.FindProperty("overviewCam")?.objectReferenceValue?.name ?? "NULL"}");
            Debug.Log($"[Diag] RunActive={ctrl.RunActive}, TimeRemaining={ctrl.TimeRemaining}");
        }

        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car != null)
        {
            Debug.Log($"[Diag] Car pos={car.transform.position}, active={car.gameObject.activeInHierarchy}, enabled={car.enabled}");
            var rb = car.GetComponent<Rigidbody>();
            if (rb != null) Debug.Log($"[Diag] Car Rigidbody vel={rb.linearVelocity.magnitude:F2}, kinematic={rb.isKinematic}");
        }
        else Debug.LogError("[Diag] No ArcadeCarController.");

        var mainCam = Camera.main;
        Debug.Log($"[Diag] Main camera: name={mainCam?.name ?? "NULL"}, pos={mainCam?.transform.position}");
    }
}
