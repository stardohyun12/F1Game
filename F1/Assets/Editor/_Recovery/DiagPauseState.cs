using UnityEditor;
using UnityEngine;

public static class DiagPauseState
{
    [MenuItem("PitCrew/Car/DIAG Pause State")]
    public static void Execute()
    {
        Debug.Log($"[DIAG2] Time.timeScale={Time.timeScale} EditorApplication.isPaused={EditorApplication.isPaused} isPlaying={Application.isPlaying} fixedDelta={Time.fixedDeltaTime}");
        if (EditorApplication.isPaused)
        {
            EditorApplication.isPaused = false;
            Debug.Log("[DIAG2] unpaused.");
        }
    }
}
