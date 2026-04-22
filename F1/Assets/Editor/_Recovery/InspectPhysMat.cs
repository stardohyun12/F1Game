using UnityEditor;
using UnityEngine;

public static class InspectPhysMat
{
    public static void Execute()
    {
        var path = "Assets/GroundPhysics.physicsMaterial";
        var obj = AssetDatabase.LoadMainAssetAtPath(path);
        Debug.Log($"InspectPhysMat: obj={(obj != null)} type={(obj != null ? obj.GetType().FullName : "null")} name={(obj != null ? obj.name : "-")}");
    }
}
