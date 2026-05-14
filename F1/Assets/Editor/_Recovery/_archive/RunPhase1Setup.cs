using UnityEditor;
using UnityEngine;

public static class RunPhase1Setup
{
    public static void Execute()
    {
        Debug.Log("[Recovery] Executing PitCrew/Phase 1/Full Integration Setup");
        EditorApplication.ExecuteMenuItem("PitCrew/Phase 1/Full Integration Setup");

        Debug.Log("[Recovery] Executing PitCrew/Setup Order System (B)");
        EditorApplication.ExecuteMenuItem("PitCrew/Setup Order System (B)");

        Debug.Log("[Recovery] Done. Save scene manually if results look good.");
    }
}
