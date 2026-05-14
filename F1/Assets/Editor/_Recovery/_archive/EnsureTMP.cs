using UnityEngine;
using UnityEditor;
using System.IO;

public static class EnsureTMP
{
    [MenuItem("PitCrew/UI/Ensure TMP Essentials")]
    public static void Execute()
    {
        var tmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        bool tmpInstalled = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), tmpSettingsPath));

        Debug.Log("[TMP Check] TMP Settings exists: " + tmpInstalled);

        if (!tmpInstalled)
        {
            // Unity 6 UGUI 2.0: PackageCache에 unitypackage 존재
            string found = null;
            var cacheRoot = "Library/PackageCache";
            if (Directory.Exists(cacheRoot))
            {
                foreach (var d in Directory.GetDirectories(cacheRoot, "com.unity.ugui@*"))
                {
                    var candidate = Path.Combine(d, "Package Resources", "TMP Essential Resources.unitypackage");
                    if (File.Exists(candidate)) { found = candidate; break; }
                }
            }
            if (!string.IsNullOrEmpty(found))
            {
                Debug.Log("[TMP Check] Importing from: " + found);
                AssetDatabase.ImportPackage(found, false);
            }
            else
            {
                Debug.Log("[TMP Check] Trying legacy menu path.");
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            }
        }
        else
        {
            Debug.Log("[TMP Check] Already installed.");
        }

        string[] fontPaths = {
            "Assets/UI/kenney_ui-pack/Font/Kenney Future.ttf",
            "Assets/UI/kenney_ui-pack/Font/Kenney Future Narrow.ttf"
        };
        foreach (var p in fontPaths)
            Debug.Log("[Font Check] " + p + " = " + File.Exists(p));
    }
}
