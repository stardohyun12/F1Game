using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// 한글 UI 깨짐 해결: 맑은 고딕 TTF를 프로젝트에 복사 → dynamic SDF TMP_FontAsset 생성 →
// TMP_Settings.m_fallbackFontAssets에 등록. 모든 TMP 텍스트가 미보유 글리프 시 자동 fallback.
public static class AddKoreanFontFallback
{
    const string SourceTtf = @"C:/Windows/Fonts/malgun.ttf";
    const string DestDir = "Assets/UI/Fonts/Korean";
    const string DestTtf = "Assets/UI/Fonts/Korean/Malgun.ttf";
    const string FontAssetPath = "Assets/UI/Fonts/Korean/Malgun SDF.asset";

    public static void Execute()
    {
        // 1. TTF 복사
        if (!File.Exists(SourceTtf)) { Debug.LogError($"[KFont] no source ttf at {SourceTtf}"); return; }
        Directory.CreateDirectory(DestDir);
        if (!File.Exists(DestTtf))
        {
            File.Copy(SourceTtf, DestTtf, overwrite: false);
            Debug.Log($"[KFont] Copied ttf → {DestTtf}");
        }
        AssetDatabase.ImportAsset(DestTtf, ImportAssetOptions.ForceUpdate);

        var ttf = AssetDatabase.LoadAssetAtPath<Font>(DestTtf);
        if (ttf == null) { Debug.LogError("[KFont] failed to load ttf as Font"); return; }

        // 2. TMP_FontAsset (Dynamic atlas — 글리프 런타임 추가)
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(
                ttf,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 2048,
                atlasHeight: 2048,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);
            fontAsset.name = "Malgun SDF";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[KFont] Created TMP_FontAsset → {FontAssetPath}");
        }
        else
        {
            Debug.Log("[KFont] TMP_FontAsset already exists, reusing.");
        }

        // 3. TMP_Settings 글로벌 fallback 등록
        var settings = TMP_Settings.instance;
        if (settings == null) { Debug.LogError("[KFont] TMP_Settings.instance is null"); return; }

        var so = new SerializedObject(settings);
        var list = so.FindProperty("m_fallbackFontAssets");
        if (list == null) { Debug.LogError("[KFont] m_fallbackFontAssets prop missing"); return; }

        // 중복 방지
        bool already = false;
        for (int i = 0; i < list.arraySize; i++)
        {
            var el = list.GetArrayElementAtIndex(i);
            if (el.objectReferenceValue == fontAsset) { already = true; break; }
        }
        if (!already)
        {
            int idx = list.arraySize;
            list.InsertArrayElementAtIndex(idx);
            list.GetArrayElementAtIndex(idx).objectReferenceValue = fontAsset;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[KFont] Registered as TMP global fallback.");
        }
        else Debug.Log("[KFont] Already in TMP fallback list.");

        Debug.Log("[KFont] Done. All TMP text will fall back to Malgun for missing glyphs.");
    }
}
