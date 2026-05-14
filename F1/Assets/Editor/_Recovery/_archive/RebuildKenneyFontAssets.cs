using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using System.IO;

/// <summary>
/// Kenney TMP FontAsset 재생성 + ASCII pre-populate.
/// 기존 자산을 지우고 새로 만들어 런타임 Dynamic rasterize 실패 리스크 제거.
/// </summary>
public static class RebuildKenneyFontAssets
{
    const string OutDir = "Assets/UI/Fonts";
    // 주행 HUD에서 쓰는 글자 + 일반 ASCII pre-populate
    const string PrePopulate =
        " !\"#$%&'()*+,-./0123456789:;<=>?@" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
        "abcdefghijklmnopqrstuvwxyz{|}~";

    [MenuItem("PitCrew/UI/Rebuild Kenney TMP FontAssets")]
    public static void Execute()
    {
        Directory.CreateDirectory(OutDir);

        Build("Assets/UI/kenney_ui-pack/Font/Kenney Future.ttf", "KenneyFuture SDF");
        Build("Assets/UI/kenney_ui-pack/Font/Kenney Future Narrow.ttf", "KenneyFutureNarrow SDF");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RebuildFont] Done.");
    }

    static void Build(string ttfPath, string assetName)
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null) { Debug.LogError("[RebuildFont] TTF not found: " + ttfPath); return; }

        var outPath = $"{OutDir}/{assetName}.asset";

        if (File.Exists(outPath))
        {
            AssetDatabase.DeleteAsset(outPath);
            Debug.Log("[RebuildFont] Deleted old: " + outPath);
        }

        // 재생성 — sampling 64, padding 5. 먼저 Dynamic으로 만들고 glyph 추가 → Static 전환.
        var fa = TMP_FontAsset.CreateFontAsset(
            font,
            samplingPointSize: 64,
            atlasPadding: 5,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 1024,
            atlasHeight: 1024,
            atlasPopulationMode: AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: false // 단일 atlas만 — Static 저장 단순화
        );
        fa.name = assetName;

        // CreateAsset 먼저 (서브에셋 추가 전제)
        AssetDatabase.CreateAsset(fa, outPath);

        // Dynamic 상태에서 glyph pre-populate (atlas texture는 이 호출에서 처음 할당됨)
        bool ok = fa.TryAddCharacters(PrePopulate);

        // Atlas texture와 material을 서브에셋으로 등록
        if (fa.atlasTextures != null)
        {
            foreach (var tex in fa.atlasTextures)
            {
                if (tex == null) continue;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex)))
                    AssetDatabase.AddObjectToAsset(tex, fa);
            }
        }
        if (fa.material != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(fa.material)))
            AssetDatabase.AddObjectToAsset(fa.material, fa);

        // Play stop 시 atlas 초기화 방지 — Static으로 전환
        fa.atlasPopulationMode = AtlasPopulationMode.Static;
        fa.ReadFontAssetDefinition();

        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(outPath);

        Debug.Log($"[RebuildFont] {assetName}: TryAddCharacters ok={ok}, glyphs={fa.characterTable.Count}, atlasTex={fa.atlasTextures?.Length}, mode=Static");
    }
}
