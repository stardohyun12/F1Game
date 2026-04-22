using UnityEditor;
using UnityEngine;

public static class RunSetupOrderSystem
{
    public static void Execute()
    {
        SetupOrderSystem.Run();
        Debug.Log("[RunSetupOrderSystem] done.");
    }
}
