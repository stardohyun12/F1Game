using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class DiagnoseGameContextScript
{
    public static void Execute()
    {
        Debug.Log("=== Diagnose GameContext script resolution ===");

        // 1) Path → GUID
        const string path = "Assets/Scripts/Core/Core.cs";
        var guid = AssetDatabase.AssetPathToGUID(path);
        Debug.Log($"[Diag] AssetPathToGUID({path}) = '{guid}'");

        // 2) GUID → Path 역검증
        const string knownGuid = "9191dca5050595d4cb0ebbe342cb604c";
        var path2 = AssetDatabase.GUIDToAssetPath(knownGuid);
        Debug.Log($"[Diag] GUIDToAssetPath({knownGuid}) = '{path2}'");

        // 3) Load MonoScript at path
        var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        if (ms == null) { Debug.LogError("[Diag] MonoScript load FAILED."); return; }
        Debug.Log($"[Diag] MonoScript loaded. Class='{ms.GetClass()?.FullName}'");

        // 4) Find GameContext type via Reflection (PitCrew assembly)
        Type t = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType("GameContext", false);
            if (t != null) { Debug.Log($"[Diag] GameContext found in assembly: {asm.GetName().Name}"); break; }
        }
        if (t == null) { Debug.LogError("[Diag] GameContext type NOT found in any assembly!"); return; }

        // 5) Compare GUID of MonoScript at path vs known
        var pathGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(ms));
        Debug.Log($"[Diag] Reload check: pathGuid of loaded MonoScript = '{pathGuid}' (should match known)");
        Debug.Log($"[Diag] MonoScript.GetClass().Assembly = {ms.GetClass()?.Assembly?.GetName()?.Name}");

        // 6) Inspect every type in script
        var classes = ms.GetClass();
        if (classes != null) Debug.Log($"[Diag] ms.GetClass() = {classes.FullName}, GameContext type match = {classes == t}");
    }
}
