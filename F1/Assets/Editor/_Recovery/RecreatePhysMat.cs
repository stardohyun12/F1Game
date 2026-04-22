using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RecreatePhysMat
{
    public static void Execute()
    {
        const string path = "Assets/GroundPhysicsMat.physicMaterial";
        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (existing == null)
        {
            var mat = new PhysicsMaterial("GroundPhysicsMat")
            {
                dynamicFriction = 0.15f,
                staticFriction = 0.15f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            existing = mat;
            Debug.Log($"RecreatePhysMat: created {path}");
        }

        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
        {
            var col = groundGO.GetComponent<Collider>();
            if (col != null)
            {
                col.sharedMaterial = existing;
                EditorUtility.SetDirty(col);
                Debug.Log($"RecreatePhysMat: ground material = {col.sharedMaterial.name}");
            }
        }
        EditorSceneManager.SaveOpenScenes();
    }
}
