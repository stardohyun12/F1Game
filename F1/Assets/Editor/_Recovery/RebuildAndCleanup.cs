using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RebuildAndCleanup
{
    public static void Execute()
    {
        var oldDecor = GameObject.Find("TrackDecor");
        if (oldDecor != null) Object.DestroyImmediate(oldDecor);

        TileMapAuto.Build();
        SetupDeliverySystem.Setup();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("RebuildAndCleanup: removed TrackDecor, rebuilt tilemap track + delivery.");
    }
}
