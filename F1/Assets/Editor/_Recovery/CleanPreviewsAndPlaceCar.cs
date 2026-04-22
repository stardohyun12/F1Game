using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CleanPreviewsAndPlaceCar
{
    public static void Execute()
    {
        // Clean test preview objects if present (ComplexTrack was sullied during exploration).
        foreach (var name in new[] { "StraightPreview", "BendPreview" })
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
        EditorSceneManager.SaveOpenScenes();
        SetupRaceInScene.PlaceInCurrent();
    }
}
