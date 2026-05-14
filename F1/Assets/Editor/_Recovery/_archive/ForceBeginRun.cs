using UnityEditor;
using UnityEngine;

/// <summary>
/// OrderBoard 첫 카드 강제 선택 — Coplay에서 마우스 클릭 못 보내는 환경용.
/// Play 모드 + RunController + OrderCatalog 필요.
/// </summary>
public static class ForceBeginRun
{
    [MenuItem("PitCrew/Game/Force BeginRun (first job)")]
    public static void Execute()
    {
        if (!Application.isPlaying) { Debug.LogError("[Force] Play 모드에서만."); return; }

        var rc = Object.FindFirstObjectByType<RunController>();
        if (rc == null) { Debug.LogError("[Force] RunController 없음."); return; }

        var board = Object.FindFirstObjectByType<OrderBoard>();
        OrderCatalog cat = null;
        if (board != null)
        {
            var fld = typeof(OrderBoard).GetField("catalog",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cat = fld.GetValue(board) as OrderCatalog;
        }

        OrderJob job = null;
        if (cat != null && cat.jobs != null && cat.jobs.Count > 0) job = cat.jobs[0];

        if (job == null)
        {
            job = ScriptableObject.CreateInstance<OrderJob>();
            job.name = "FallbackJob";
            job.pointCount = 7;
        }

        if (board != null) board.Close();
        rc.BeginRun(job);
        Time.timeScale = 1f;
        Debug.Log($"[Force] BeginRun({job.name}, points={job.pointCount}). timeScale=1.");
    }
}
