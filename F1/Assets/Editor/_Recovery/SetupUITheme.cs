using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

/// <summary>
/// UITheme.asset 생성 + Kenney UI sprite/font/sfx 전체 자동 연결.
/// 세션 13: Sprite 기반 상태 표현으로 전환. 5색 variants 전부 import.
/// </summary>
public static class SetupUITheme
{
    const string ResourcesDir = "Assets/Resources";
    const string ThemePath = "Assets/Resources/UITheme.asset";

    const string FontPrimaryPath = "Assets/UI/Fonts/KenneyFuture SDF.asset";
    const string FontCondensedPath = "Assets/UI/Fonts/KenneyFutureNarrow SDF.asset";

    const string KennyRoot = "Assets/UI/kenney_ui-pack/PNG/";

    // Panels (Grey)
    const string PanelDark = KennyRoot + "Grey/Default/button_rectangle_depth_flat.png";
    const string PanelBright = KennyRoot + "Grey/Default/button_rectangle_flat.png";
    const string PanelGloss = KennyRoot + "Grey/Default/button_rectangle_gloss.png";
    const string BarTrack = KennyRoot + "Grey/Default/slide_horizontal_grey_section_wide.png";

    // Fills (5색)
    const string FillOk = KennyRoot + "Green/Default/button_rectangle_flat.png";
    const string FillWarn = KennyRoot + "Yellow/Default/button_rectangle_flat.png";
    const string FillCrit = KennyRoot + "Red/Default/button_rectangle_flat.png";
    const string FillAccent = KennyRoot + "Yellow/Default/button_rectangle_gloss.png";

    // Buttons
    const string ButtonNormal = KennyRoot + "Grey/Default/button_rectangle_gloss.png";
    const string ButtonHighlight = KennyRoot + "Yellow/Default/button_rectangle_gloss.png";

    // Icons (non-9-slice, 작은 것)
    const string ArrowNorth = KennyRoot + "Yellow/Default/arrow_basic_n.png";
    const string IconCheck = KennyRoot + "Green/Default/icon_checkmark.png";

    // Sounds
    const string SfxClickPath = "Assets/Sound/kenney_interface-sounds (1)/Audio/click_001.ogg";
    const string SfxBackPath = "Assets/Sound/kenney_interface-sounds (1)/Audio/back_001.ogg";
    const string SfxConfirmPath = "Assets/Sound/kenney_interface-sounds (1)/Audio/confirmation_001.ogg";
    const string SfxShiftPath = "Assets/Sound/kenney_interface-sounds (1)/Audio/bong_001.ogg";

    [MenuItem("PitCrew/UI/Setup UITheme Asset")]
    public static void Execute()
    {
        if (!Directory.Exists(ResourcesDir)) { Directory.CreateDirectory(ResourcesDir); AssetDatabase.Refresh(); }

        // 9-slice sprites (border 14)
        string[] nineSlice = { PanelDark, PanelBright, PanelGloss, BarTrack, FillOk, FillWarn, FillCrit, FillAccent, ButtonNormal, ButtonHighlight };
        foreach (var p in nineSlice) ConfigureSpriteImporter(p, borderPx: 14);

        // 단일 icon (no border)
        ConfigureSpriteImporter(ArrowNorth, borderPx: 0);
        ConfigureSpriteImporter(IconCheck, borderPx: 0);

        AssetDatabase.Refresh();

        var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        bool created = false;
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<UITheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
            created = true;
        }

        theme.fontPrimary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPrimaryPath);
        theme.fontCondensed = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontCondensedPath);

        theme.panelDark = AssetDatabase.LoadAssetAtPath<Sprite>(PanelDark);
        theme.panelBright = AssetDatabase.LoadAssetAtPath<Sprite>(PanelBright);
        theme.panelGloss = AssetDatabase.LoadAssetAtPath<Sprite>(PanelGloss);
        theme.barTrack = AssetDatabase.LoadAssetAtPath<Sprite>(BarTrack);

        theme.fillOk = AssetDatabase.LoadAssetAtPath<Sprite>(FillOk);
        theme.fillWarn = AssetDatabase.LoadAssetAtPath<Sprite>(FillWarn);
        theme.fillCrit = AssetDatabase.LoadAssetAtPath<Sprite>(FillCrit);
        theme.fillAccent = AssetDatabase.LoadAssetAtPath<Sprite>(FillAccent);

        theme.buttonNormal = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonNormal);
        theme.buttonHighlight = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonHighlight);

        theme.arrowNorth = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowNorth);
        theme.iconCheck = AssetDatabase.LoadAssetAtPath<Sprite>(IconCheck);

        theme.sfxClick = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxClickPath);
        theme.sfxBack = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBackPath);
        theme.sfxConfirm = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxConfirmPath);
        theme.sfxShift = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxShiftPath);

        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[UITheme] {(created ? "Created" : "Updated")} {ThemePath}. "
            + $"panels={NullCheck(theme.panelDark, theme.panelBright, theme.panelGloss)} "
            + $"fills={NullCheck(theme.fillOk, theme.fillWarn, theme.fillCrit, theme.fillAccent)} "
            + $"arrow={theme.arrowNorth?.name ?? "null"}");
    }

    static string NullCheck(params Object[] objs)
    {
        int ok = 0;
        foreach (var o in objs) if (o != null) ok++;
        return $"{ok}/{objs.Length}";
    }

    static void ConfigureSpriteImporter(string path, int borderPx)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogWarning("[UITheme] Sprite not found: " + path); return; }

        bool changed = false;
        if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; changed = true; }
        if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (imp.spritePixelsPerUnit != 100f) { imp.spritePixelsPerUnit = 100f; changed = true; }

        var want = new Vector4(borderPx, borderPx, borderPx, borderPx);
        if (imp.spriteBorder != want) { imp.spriteBorder = want; changed = true; }

        if (changed) imp.SaveAndReimport();
    }
}
