using UnityEditor;

public static class RebuildComplexBuildings
{
    public static void Execute()
    {
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>("Assets/Data/ComplexTrackLayout.asset");
        BuildingPlacer.Build(layout);
    }
}
