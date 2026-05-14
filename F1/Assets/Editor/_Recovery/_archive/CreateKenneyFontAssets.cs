using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using System.IO;

public static class CreateKenneyFontAssets
{
    const string OutDir = "Assets/UI/Fonts";

    [MenuItem("PitCrew/UI/Create Kenney TMP FontAssets")]
    public static void Execute()
    {
        if (!Directory.Exists(OutDir))
        {
            Directory.CreateDirectory(OutDir);
            AssetDatabase.Refresh();
        }

        BuildOne("Assets/UI/kenney_ui-pack/Font/Kenney Future.ttf", "KenneyFuture SDF");
        BuildOne("Assets/UI/kenney_ui-pack/Font/Kenney Future Narrow.ttf", "KenneyFutureNarrow SDF");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FontAsset] Done.");
    }

    static void BuildOne(string ttfPath, string assetName)
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null)
        {
            Debug.LogError("[FontAsset] TTF not found: " + ttfPath);
            return;
        }

        var outPath = $"{OutDir}/{assetName}.asset";
        if (File.Exists(outPath))
        {
            Debug.Log("[FontAsset] Already exists, skipping: " + outPath);
            return;
        }

        // Dynamic atlas — 필요 시 런타임 glyph 추가. 영문/숫자 위주라 512로 충분.
        var fa = TMP_FontAsset.CreateFontAsset(
            font,
            samplingPointSize: 90,
            atlasPadding: 9,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 1024,
            atlasHeight: 1024,
            atlasPopulationMode: AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true
        );
        fa.name = assetName;

        AssetDatabase.CreateAsset(fa, outPath);
        Debug.Log("[FontAsset] Created: " + outPath);
    }
}
