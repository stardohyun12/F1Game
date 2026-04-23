using UnityEngine;

public static class RebuildAfterTeeFix
{
    public static void Execute()
    {
        MeasureIntersection.Cleanup();
        MeasureIntersectionRotations.Cleanup();
        RunFullRebuild.Execute();
        SetupArtPreset.Execute();
        Debug.Log("[RebuildAfterTeeFix] Done.");
    }
}
