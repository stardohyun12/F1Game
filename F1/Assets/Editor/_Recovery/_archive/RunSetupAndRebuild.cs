using UnityEditor;
using UnityEngine;

// Temporary runner: executes SetupLayoutAssets.Run() then BuildingPlacer.RebuildFromAssignedLayout().
public static class RunSetupAndRebuild
{
    public static void Execute()
    {
        Debug.Log("RunSetupAndRebuild: step 1 — SetupLayoutAssets.Run()");
        SetupLayoutAssets.Run();
        Debug.Log("RunSetupAndRebuild: step 2 — BuildingPlacer.RebuildFromAssignedLayout()");
        BuildingPlacer.RebuildFromAssignedLayout();
        Debug.Log("RunSetupAndRebuild: done.");
    }

    // Regenerates the ComplexTrack scene fully at the current CellSize, then places the car.
    public static void RegenerateComplexTrack()
    {
        Debug.Log("RegenerateComplexTrack: step 1 — SetupComplexScene.Run()");
        SetupComplexScene.Run();
        Debug.Log("RegenerateComplexTrack: step 2 — SetupRaceInScene.PlaceInCurrent()");
        SetupRaceInScene.PlaceInCurrent();
        Debug.Log("RegenerateComplexTrack: done.");
    }
}
