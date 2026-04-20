using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RefreshAndLinkEngineClip
{
    const string ClipPath = "Assets/Sound/EngineLoop.mp3";

    [MenuItem("PitCrew/Refresh And Link Engine Clip")]
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (clip == null)
        {
            Debug.LogError($"[RefreshAndLinkEngineClip] Clip not found at {ClipPath}");
            return;
        }

        var race = GameObject.Find("Race");
        if (race == null)
        {
            Debug.LogError("[RefreshAndLinkEngineClip] Race not found.");
            return;
        }

        var engine = race.GetComponent<EngineAudio>();
        if (engine == null)
        {
            Debug.LogError("[RefreshAndLinkEngineClip] EngineAudio component not found on Race.");
            return;
        }

        var so = new SerializedObject(engine);
        var prop = so.FindProperty("engineLoop");
        prop.objectReferenceValue = clip;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(engine);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[RefreshAndLinkEngineClip] Linked {ClipPath} to EngineAudio.engineLoop.");
    }
}
