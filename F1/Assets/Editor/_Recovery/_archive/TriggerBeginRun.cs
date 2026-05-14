using UnityEditor;
using UnityEngine;

public static class TriggerBeginRun
{
    public static void Execute()
    {
        var ctrl = Object.FindFirstObjectByType<RunController>();
        if (ctrl == null) { Debug.LogError("[Trigger] RunController not found."); return; }
        var catalog = AssetDatabase.LoadAssetAtPath<OrderCatalog>("Assets/Data/Orders/OrderCatalog.asset");
        if (catalog == null || catalog.jobs.Count == 0)
        {
            Debug.LogError("[Trigger] OrderCatalog not found or empty.");
            return;
        }
        ctrl.BeginRun(catalog.jobs[0]);
        Debug.Log($"[Trigger] BeginRun invoked with '{catalog.jobs[0].displayName}'");
    }
}
