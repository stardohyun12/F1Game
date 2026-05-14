using UnityEditor;
using UnityEngine;

/// <summary>
/// ResultPanel 테스트 — Play 모드에서 ResultPanel을 강제로 표시하여 시각 검증.
/// 메뉴: PitCrew/Phase 1/Test ResultPanel (Play Mode)
/// </summary>
public static class TestResultPanel
{
    [MenuItem("PitCrew/Phase 1/Test ResultPanel (Play Mode)")]
    public static void Execute()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[TestResultPanel] Play 모드 진입 후 실행 필요.");
            return;
        }
        var ctrl = Object.FindFirstObjectByType<RunController>();
        var panel = Object.FindFirstObjectByType<ResultPanel>();
        if (ctrl == null || panel == null)
        {
            Debug.LogError("[TestResultPanel] RunController 또는 ResultPanel 없음.");
            return;
        }
        // 가짜 BeginRun + 즉시 Show
        var jobs = AssetDatabase.LoadAssetAtPath<OrderCatalog>("Assets/Data/Orders/OrderCatalogExpanded.asset");
        if (jobs != null && jobs.jobs != null && jobs.jobs.Count > 0)
        {
            ctrl.BeginRun(jobs.jobs[0]);
        }
        // private SetLastRun이 없으니 일단 Show만 호출. CurrentJob 등 stale일 수 있지만 UI 시각만 확인.
        panel.Show(ctrl);
        Debug.Log("[TestResultPanel] ResultPanel.Show 호출 — 화면 확인.");
    }
}
