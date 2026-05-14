using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixGameContextMissingScript
{
    public static void Execute()
    {
        var go = GameObject.Find("GameContext");
        if (go == null)
        {
            Debug.LogError("[Fix] GameContext GameObject not found in scene.");
            return;
        }

        // Remove missing script components (m_Script가 null인 MonoBehaviour).
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        Debug.Log($"[Fix] Removed {removed} missing scripts from GameContext.");

        // Add proper GameContext component if not present.
        var existing = go.GetComponent<GameContext>();
        if (existing == null)
        {
            existing = go.AddComponent<GameContext>();
            Debug.Log("[Fix] Added GameContext component.");
        }
        else
        {
            Debug.Log("[Fix] GameContext component already present.");
        }

        EditorUtility.SetDirty(go);
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Fix] Done. Scene saved.");
    }
}
