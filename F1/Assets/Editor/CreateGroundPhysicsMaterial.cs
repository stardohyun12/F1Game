using UnityEngine;
using UnityEditor;

public class CreateGroundPhysicsMaterial
{
    public static void Execute()
    {
        const string path = "Assets/GroundPhysics.physicsMaterial";

        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial("GroundPhysics")
            {
                dynamicFriction = 0.15f,
                staticFriction = 0.15f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            Debug.Log("Created GroundPhysics material at " + path);
        }

        var ground = GameObject.Find("Ground");
        if (ground == null) { Debug.LogError("Ground not found"); return; }

        var mc = ground.GetComponent<MeshCollider>();
        if (mc == null) { Debug.LogError("Ground has no MeshCollider"); return; }

        mc.sharedMaterial = mat;
        EditorUtility.SetDirty(ground);
        Debug.Log("Assigned GroundPhysics to Ground MeshCollider");
    }
}
