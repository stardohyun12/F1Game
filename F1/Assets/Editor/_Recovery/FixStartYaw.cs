using UnityEditor;

public static class FixStartYaw
{
    public static void Execute()
    {
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>("Assets/Data/ComplexTrackLayout.asset");
        if (layout == null) { UnityEngine.Debug.LogError("layout missing"); return; }
        layout.startYawDeg = 90f;
        EditorUtility.SetDirty(layout);
        AssetDatabase.SaveAssets();

        SetupRaceInScene.PlaceInCurrent();
    }
}
