using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Malgun SDF asset 통째로 재생성. atlas + material 둘 다 sub-asset으로 묶음.
// 이전 CreateFontAsset 단독 호출은 atlas/material을 sub-asset으로 안 묶어 reload 시 null.
public static class FixKoreanFontAtlas
{
    const string Ttf = "Assets/UI/Fonts/Korean/Malgun.ttf";
    const string FontPath = "Assets/UI/Fonts/Korean/Malgun SDF.asset";

    [MenuItem("PitCrew/Fix/Fix Korean Font Atlas")]
    public static void Execute()
    {
        var ttf = AssetDatabase.LoadAssetAtPath<Font>(Ttf);
        if (ttf == null) { Debug.LogError($"[KFontFix] ttf missing at {Ttf}"); return; }

        // 기존 깨진 asset 제거
        if (File.Exists(FontPath))
        {
            AssetDatabase.DeleteAsset(FontPath);
            Debug.Log("[KFontFix] deleted broken asset");
        }

        // Dynamic SDF 폰트 asset 생성 — atlas + material 내부적으로 만들어짐
        var fa = TMP_FontAsset.CreateFontAsset(
            ttf,
            samplingPointSize: 90,
            atlasPadding: 9,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 2048,
            atlasHeight: 2048,
            atlasPopulationMode: AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true);
        fa.name = "Malgun SDF";

        // 메인 asset 저장
        AssetDatabase.CreateAsset(fa, FontPath);

        // atlas texture + material을 sub-asset으로 묶음 (이게 빠지면 reload 시 null)
        if (fa.atlasTexture != null)
        {
            fa.atlasTexture.name = "Malgun Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
        }
        if (fa.material != null)
        {
            fa.material.name = "Malgun Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }

        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceUpdate);

        // reload해서 검증
        var reload = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Debug.Log($"[KFontFix] reloaded: atlas={reload.atlasTexture?.name ?? "NULL"}, mat={reload.material?.name ?? "NULL"}");

        // TMP_Settings 글로벌 fallback에 재등록 (있는지 확인 후)
        var settings = TMP_Settings.instance;
        var so = new SerializedObject(settings);
        var list = so.FindProperty("m_fallbackFontAssets");
        bool present = false;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == reload) { present = true; break; }
        if (!present)
        {
            int idx = list.arraySize;
            list.InsertArrayElementAtIndex(idx);
            list.GetArrayElementAtIndex(idx).objectReferenceValue = reload;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[KFontFix] registered to TMP fallback");
        }
        else Debug.Log("[KFontFix] already in TMP fallback");
    }
}
