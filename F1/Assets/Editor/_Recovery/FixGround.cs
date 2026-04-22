using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixGround
{
    public static void Execute()
    {
        var groundGO = GameObject.Find("Ground");
        if (groundGO == null) { Debug.LogError("No Ground"); return; }
        var col = groundGO.GetComponent<Collider>();
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/GroundPhysics.physicsMaterial");
        Debug.Log($"FixGround: col={(col != null)} mat={(mat != null)} matType={(mat != null ? mat.GetType().FullName : "null")}");
        if (col != null && mat != null)
        {
            col.sharedMaterial = mat;
            EditorUtility.SetDirty(col);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"FixGround: assigned. sharedMaterial now = {(col.sharedMaterial != null ? col.sharedMaterial.name : "null")}");
        }
    }
}
