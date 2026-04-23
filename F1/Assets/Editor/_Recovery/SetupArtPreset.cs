using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-shot: configures warm-afternoon art preset on the current scene.
// - Creates/updates Assets/Settings/PitCrewCityVolume.asset (URP Volume Profile)
// - Adds a "GlobalVolume" GameObject if missing, assigns profile
// - Tunes Directional Light to warm late-afternoon
// - Sets Ambient gradient + Linear Fog
public static class SetupArtPreset
{
    const string ProfilePath = "Assets/Settings/PitCrewCityVolume.asset";

    public static void Execute()
    {
        // 1. Volume Profile
        Directory.CreateDirectory("Assets/Settings");
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        // Clear existing overrides to avoid duplicates on re-run
        var existing = profile.components.ToArray();
        foreach (var c in existing) profile.Remove(c.GetType());

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.8f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.7f);
        bloom.tint.Override(new Color(1f, 0.95f, 0.85f));

        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.3f);
        vig.smoothness.Override(0.35f);

        var tm = profile.Add<Tonemapping>(true);
        tm.mode.Override(TonemappingMode.ACES);

        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.3f);
        ca.saturation.Override(10f);
        ca.contrast.Override(15f);
        ca.colorFilter.Override(new Color(1f, 0.98f, 0.93f));

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // 2. GlobalVolume GameObject in scene
        var vol = GameObject.Find("GlobalVolume");
        if (vol == null)
        {
            vol = new GameObject("GlobalVolume");
            Undo.RegisterCreatedObjectUndo(vol, "Setup Art Preset");
        }
        var volume = vol.GetComponent<Volume>();
        if (volume == null) volume = vol.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.weight = 1f;
        volume.profile = profile;

        // 3. Directional Light — warm late-afternoon
        var dl = GameObject.Find("Directional Light");
        if (dl != null)
        {
            dl.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = dl.GetComponent<Light>();
            if (l != null)
            {
                l.color = new Color(1.0f, 0.895f, 0.71f); // #FFE4B5 Moccasin
                l.intensity = 1.3f;
                l.shadows = LightShadows.Soft;
            }
        }

        // 4. Ambient gradient + Linear Fog
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.529f, 0.808f, 0.922f); // #87CEEB
        RenderSettings.ambientEquatorColor = new Color(0.784f, 0.722f, 0.612f); // #C8B89C
        RenderSettings.ambientGroundColor = new Color(0.290f, 0.251f, 0.188f); // #4A4030

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.78f, 0.78f, 0.80f); // soft gray-blue, matches sky bottom
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 220f;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SetupArtPreset] Volume profile + Directional Light + Ambient + Fog applied.");
    }
}
