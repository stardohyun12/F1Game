using UnityEditor;
using UnityEngine;

// One-shot: SetupLayoutAssets → SetupComplexScene → SetupRaceInScene → SetupOrderSystem.
// Rebuilds Data SOs, the ComplexTrack scene, car placement, and delivery/order wiring.
public static class RunFullRebuild
{
    [MenuItem("PitCrew/Run Full Rebuild")]
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

        // Step 6 — 연결성 검증 (D-038 세션 13 방지 시스템)
        Debug.Log("[RunFullRebuild] Step 6 — TrackConnectivityVerifier");
        var layoutGuids = AssetDatabase.FindAssets("t:TrackLayout ComplexTrackLayout");
        if (layoutGuids.Length > 0)
        {
            var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(layoutGuids[0]));
            TrackConnectivityVerifier.Verify(layout, logPrefix: "RunFullRebuild");
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[RunFullRebuild] DONE");
    }
}
