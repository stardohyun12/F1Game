using UnityEditor;
using UnityEngine;

// One-shot: SetupLayoutAssets → SetupComplexScene → SetupRaceInScene → SetupOrderSystem.
// Rebuilds Data SOs, the ComplexTrack scene, car placement, and delivery/order wiring.
public static class RunFullRebuild
{
    public static void Execute()
    {
        Debug.Log("[RunFullRebuild] Step 1 — SetupLayoutAssets.Run");
        SetupLayoutAssets.Run();

        Debug.Log("[RunFullRebuild] Step 2 — SetupComplexScene.Run");
        SetupComplexScene.Run();

        Debug.Log("[RunFullRebuild] Step 3 — SetupRaceInScene.PlaceInCurrent");
        SetupRaceInScene.PlaceInCurrent();

        Debug.Log("[RunFullRebuild] Step 4 — SetupOrderSystem.Run");
        SetupOrderSystem.Run();

        Debug.Log("[RunFullRebuild] Step 5 — SetupUIScene.Execute");
        SetupUIScene.Execute();

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[RunFullRebuild] DONE");
    }
}
