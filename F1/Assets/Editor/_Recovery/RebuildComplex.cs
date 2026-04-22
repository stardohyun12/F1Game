using UnityEditor;

public static class RebuildComplex
{
    public static void Execute()
    {
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>("Assets/Data/ComplexTrackLayout.asset");
        TileMapAuto.BuildFromLayout(layout);
        BuildingPlacer.Build(layout);
    }
}
