using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

/// <summary>
/// UITheme.asset을 Resources/ 밑에 생성하고 Kenny UI + TMP 폰트 참조를 자동 연결.
/// 9-slice 경계 설정까지 포함.
/// </summary>
public static class SetupUITheme
{
    const string ResourcesDir = "Assets/Resources";
    const string ThemePath = "Assets/Resources/UITheme.asset";

    const string FontPrimaryPath = "Assets/UI/Fonts/KenneyFuture SDF.asset";
    const string FontCondensedPath = "Assets/UI/Fonts/KenneyFutureNarrow SDF.asset";

    // Kenny UI sprite 경로 (9-slice 후보)
    const string PanelSpritePath = "Assets/UI/kenney_ui-pack/PNG/Grey/Default/button_rectangle_depth_flat.png";
    const string TrackSpritePath = "Assets/UI/kenney_ui-pack/PNG/Grey/Default/slide_horizontal_grey_section_wide.png";
    const string FillSpritePath = "Assets/UI/kenney_ui-pack/PNG/Yellow/Default/button_rectangle_flat.png";

    // 사운드 경로
    const string SfxClickPath = "Assets/Sound/kenney_interface-sounds (1)/Audio/click_001.ogg";
    const string SfxBackPath = "Assets/Sound/kenney_interface-sounds (1)/Audio/back_001.ogg";
    const string SfxConfirmPath = "Assets/Sound/kenney_interface-sounds (1)/Audio/confirmation_001.ogg";
    const string SfxShiftPath = "Assets/Sound/kenney_interface-sounds (1)/Audio/bong_001.ogg";

    [MenuItem("PitCrew/UI/Setup UITheme Asset")]
    public static void Execute()
    {
        if (!Directory.Exists(ResourcesDir))
        {
            Directory.CreateDirectory(ResourcesDir);
            AssetDatabase.Refresh();
        }

        // 0. Kenny sprite들의 9-slice 경계 + SpriteMode 설정
        ConfigureSpriteImporter(PanelSpritePath, borderPx: 14);
        ConfigureSpriteImporter(TrackSpritePath, borderPx: 14);
        ConfigureSpriteImporter(FillSpritePath, borderPx: 14);
        AssetDatabase.Refresh();

        // 1. 기존 에셋 로드 or 새로 생성
        var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        bool created = false;
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<UITheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
            created = true;
        }

        // 2. 참조 연결
        theme.fontPrimary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPrimaryPath);
        theme.fontCondensed = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontCondensedPath);

        theme.panelBackground = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
        theme.barTrack = AssetDatabase.LoadAssetAtPath<Sprite>(TrackSpritePath);
        theme.barFill = AssetDatabase.LoadAssetAtPath<Sprite>(FillSpritePath);

        theme.sfxClick = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxClickPath);
        theme.sfxBack = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBackPath);
        theme.sfxConfirm = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxConfirmPath);
        theme.sfxShift = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxShiftPath);

        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[UITheme] {(created ? "Created" : "Updated")} {ThemePath}. "
            + $"fontPrimary={NullToStr(theme.fontPrimary)} fontCondensed={NullToStr(theme.fontCondensed)} "
            + $"panel={NullToStr(theme.panelBackground)} track={NullToStr(theme.barTrack)} fill={NullToStr(theme.barFill)}");
    }

    static string NullToStr(Object o) => o == null ? "null" : o.name;

    static void ConfigureSpriteImporter(string path, int borderPx)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
        {
            Debug.LogWarning("[UITheme] Sprite not found: " + path);
            return;
        }

        bool changed = false;
        if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; changed = true; }
        if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }

        var current = imp.spriteBorder;
        var want = new Vector4(borderPx, borderPx, borderPx, borderPx);
        if (current != want) { imp.spriteBorder = want; changed = true; }
        if (imp.spritePixelsPerUnit != 100f) { imp.spritePixelsPerUnit = 100f; changed = true; }

        if (changed)
        {
            imp.SaveAndReimport();
            Debug.Log($"[UITheme] Configured sprite: {path} border={borderPx}");
        }
    }
}
