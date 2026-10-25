using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// D-123 규칙 11 무드 시안 전환. 씬 기본(현재)은 그대로 두고, 메뉴로 A/B 를 얹었다가 되돌린다.
/// - 처음 얹을 때 현재 값(태양·RenderSettings·Volume·월드 머티리얼)을 Library/PitCrewMoodSnapshot.json 에 찍는다.
/// - 월드 머티리얼은 공유 에셋이라 Play 를 멈춰도 남는다 → 반드시 "현재로 되돌리기" 로 복원(씬은 저장하지 않음).
/// - 시안 값은 BuildProfiles() 한 곳. 메뉴 "시안 에셋 다시 만들기" 가 Assets/Settings/Mood/ 에 덮어쓴다.
/// </summary>
public static class MoodSwitcher
{
    const string Dir = "Assets/Settings/Mood";
    const string SnapPath = "Library/PitCrewMoodSnapshot.json";
    public const string PathA = Dir + "/Mood_A_WarmMorning.asset";
    public const string PathB = Dir + "/Mood_B_FadedNoon.asset";

    [MenuItem("PitCrew/Art/Mood/A 따뜻한 아침 (원화)")] public static void MenuA() => Apply(PathA);
    [MenuItem("PitCrew/Art/Mood/B 바랜 한낮 (아말피)")] public static void MenuB() => Apply(PathB);
    [MenuItem("PitCrew/Art/Mood/현재로 되돌리기")] public static void MenuRevert() => Revert();
    [MenuItem("PitCrew/Art/Mood/시안 에셋 다시 만들기")] public static void MenuRebuild() => BuildProfiles(true);

    public static string Apply(string path)
    {
        BuildProfiles(false);
        var p = AssetDatabase.LoadAssetAtPath<MoodProfile>(path);
        if (p == null) return "no profile " + path;
        if (!File.Exists(SnapPath)) File.WriteAllText(SnapPath, JsonUtility.ToJson(Capture(), true));
        MoodController.ApplyProfile(p, null, FindGradeVolume());
        return "applied " + p.name;
    }

    public static string Revert()
    {
        if (!File.Exists(SnapPath)) return "no snapshot (already current)";
        var s = JsonUtility.FromJson<Snap>(File.ReadAllText(SnapPath));
        var l = FindSun();
        if (l != null) { l.transform.eulerAngles = s.sunRot; l.color = s.sunColor; l.intensity = s.sunInt; l.shadows = (LightShadows)s.shadows; l.shadowStrength = s.shadowStr; }
        RenderSettings.ambientMode = (AmbientMode)s.ambMode; RenderSettings.ambientLight = s.amb;
        RenderSettings.ambientSkyColor = s.ambSky; RenderSettings.ambientEquatorColor = s.ambEq; RenderSettings.ambientGroundColor = s.ambGnd;
        RenderSettings.ambientIntensity = s.ambInt;
        RenderSettings.fog = s.fog; RenderSettings.fogMode = (FogMode)s.fogMode; RenderSettings.fogColor = s.fogColor;
        RenderSettings.fogDensity = s.fogDens; RenderSettings.fogStartDistance = s.fogStart; RenderSettings.fogEndDistance = s.fogEnd;
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(s.skybox);
        var v = FindGradeVolume(); if (v != null) v.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(s.volumeProfile);
        // 머티리얼은 값을 다시 쓰면 float 반올림으로 .mat 가 바뀐다 → 찍어 둔 파일 원문을 그대로 돌려놓고 재임포트.
        foreach (var m in s.mats)
        {
            if (string.IsNullOrEmpty(m.text) || !File.Exists(m.path)) continue;
            if (File.ReadAllText(m.path) != m.text) File.WriteAllText(m.path, m.text);
            AssetDatabase.ImportAsset(m.path, ImportAssetOptions.ForceUpdate);
        }
        File.Delete(SnapPath);
        return "reverted " + s.mats.Count + " mats";
    }

    // ── 시안 값 (가설). 바꾸려면 여기 or 생성된 에셋 인스펙터 ──
    public static void BuildProfiles(bool force)
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Settings", "Mood");

        // A: 원화 02_warm_street — 낮은 아침 해(따뜻한 주황), 옅은 따뜻한 해무, 하늘은 위만 파랗고 지평선은 안개색.
        // v2: 해무를 베이지 → 옅은 하늘색으로(원화 먼 바다색). 따뜻함은 해 색·화이트밸런스가 맡는다. v1 은 화면 전체가 회베이지로 씻겼다.
        // v3(1638): A·B 가 한눈에 갈리게. A = 낮은 노란 해(22°)·긴 그림자·그늘에 따뜻한 반사광·회벽은 노란 필터+채도 빼서 바랜 색.
        // v4(1754, D-127 가을날): 낮은 해(18°)는 유지하되 빛을 서늘·맑게 — 해 색 주황→연한 금색, 해무 베이지→맑은 하늘빛, 안개를 멀리(70–600m, 탁 트인 시야).
        //   따뜻함은 해 색과 약한 색온도(+10)만. 하늘은 더 진한 가을 파랑.
        var hazeA = new Color(0.80f, 0.85f, 0.88f);
        Make(PathA, force, p =>
        {
            p.sunRotationEuler = new Vector3(18f, 300f, 0f); p.sunColor = new Color(1f, 0.86f, 0.66f); p.sunIntensity = 1.45f;
            p.shadowStrength = 0.75f;
            p.fogColor = hazeA; p.fogStart = 70f; p.fogEnd = 900f;   // 1827: 600 → 900 먼 섬이 하늘에 씻기지 않게 p.worldAmbientScale = 1.05f;
            p.railTint = new Color(0.80f, 0.70f, 0.46f, 1f);
            p.skybox = MakeSky(Dir + "/Mood_A_Sky.mat", new Color(0.20f, 0.44f, 0.78f), hazeA, hazeA);
            p.postProfile = MakePost(Dir + "/Mood_A_Post.asset", exposure: 0.05f, contrast: 16f, saturation: -4f, temperature: 10f, tint: 2f, lift: 0.01f,
                filter: new Color(1f, 0.97f, 0.92f));
        });

        // B = 머리 위 흰 해(74°)·짧고 진한 그림자(그늘 반사광 낮춤)·차가운 푸른 해무·채도 크게 빼고 대비 높여 "쨍한데 바랜".
        var hazeB = new Color(0.80f, 0.88f, 0.93f);
        Make(PathB, force, p =>
        {
            p.sunRotationEuler = new Vector3(74f, 325f, 0f); p.sunColor = new Color(1f, 1f, 0.97f); p.sunIntensity = 1.5f;
            p.shadowStrength = 1f;
            p.fogColor = hazeB; p.fogStart = 60f; p.fogEnd = 240f; p.worldAmbientScale = 0.8f;
            p.railTint = new Color(0.82f, 0.76f, 0.56f, 1f);
            p.skybox = MakeSky(Dir + "/Mood_B_Sky.mat", new Color(0.30f, 0.56f, 0.86f), hazeB, hazeB);
            p.postProfile = MakePost(Dir + "/Mood_B_Post.asset", exposure: 0.15f, contrast: 20f, saturation: -22f, temperature: -4f, tint: 0f, lift: 0.02f,
                filter: Color.white);
        });
        AssetDatabase.SaveAssets();
    }

    static void Make(string path, bool force, System.Action<MoodProfile> fill)
    {
        var p = AssetDatabase.LoadAssetAtPath<MoodProfile>(path);
        if (p != null && !force) return;
        bool isNew = p == null;
        if (isNew) p = ScriptableObject.CreateInstance<MoodProfile>();
        p.castShadows = true; p.ambientMode = AmbientMode.Flat; p.fog = true; p.fogMode = FogMode.Linear; p.syncWorld = true;
        fill(p);
        // 규칙 11: 주변광 = 안개와 같은 색. URP/Lit 은 Flat 앰비언트를 그대로 더하므로 0.7 로 줄여 씻김 방지(가설).
        p.ambientColor = p.fogColor * 0.7f; p.ambientIntensity = 1f;
        if (isNew) AssetDatabase.CreateAsset(p, path); else EditorUtility.SetDirty(p);
    }

    static Material MakeSky(string path, Color top, Color horizon, Color bottom)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var src = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(FindSkyGuid()));
            m = src != null ? new Material(src) : new Material(Shader.Find("PitCrew/GradientSky"));
            AssetDatabase.CreateAsset(m, path);
        }
        // 운전 시점 하늘은 지평선 위 ~10° 뿐이라 거의 _HorizonColor 만 보인다 → 지평선을 안개색에서 파랑 쪽으로 35% 당긴다(먼 건물은 여전히 안개색으로 녹음).
        m.SetColor("_TopColor", top); m.SetColor("_HorizonColor", Color.Lerp(horizon, top, 0.35f)); m.SetColor("_BottomColor", bottom);
        m.SetFloat("_HorizonBlur", 0.001f);
        m.SetFloat("_Exponent", 2.6f);   // 셰이더는 pow(x, 1/_Exponent). 운전 시점은 하늘 아랫단만 보인다 → 클수록 파랑이 낮게까지 내려온다(현재 1.15)
        EditorUtility.SetDirty(m);
        return m;
    }

    static string FindSkyGuid()
    {
        foreach (var g in AssetDatabase.FindAssets("GradientSky t:Material")) return g;
        return "";
    }

    static VolumeProfile MakePost(string path, float exposure, float contrast, float saturation, float temperature, float tint, float lift, Color filter)
    {
        var vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (vp == null) { vp = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(vp, path); }
        T Get<T>() where T : VolumeComponent
        {
            if (!vp.TryGet<T>(out var c)) { c = vp.Add<T>(true); c.name = typeof(T).Name; AssetDatabase.AddObjectToAsset(c, vp); }
            return c;
        }
        var ca = Get<ColorAdjustments>();
        ca.postExposure.Override(exposure); ca.contrast.Override(contrast); ca.saturation.Override(saturation); ca.colorFilter.Override(filter);
        var wb = Get<WhiteBalance>(); wb.temperature.Override(temperature); wb.tint.Override(tint);
        var lgg = Get<LiftGammaGain>(); lgg.lift.Override(new Vector4(1f, 1f, 1f, lift));   // 검정 들뜸 = "바랜"
        var bl = Get<Bloom>(); bl.intensity.Override(0.35f); bl.threshold.Override(0.95f);  // WarmGrade 와 같게
        EditorUtility.SetDirty(vp);
        return vp;
    }

    static Volume FindGradeVolume()
    {
        Volume best = null;
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (!v.isGlobal) continue;
            if (v.name == "PrototypeVolume") return v;
            if (best == null || v.priority > best.priority) best = v;
        }
        return best;
    }

    static Light FindSun()
    {
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) return l;
        return null;
    }

    static Snap Capture()
    {
        var s = new Snap();
        var l = FindSun();
        if (l != null) { s.sunRot = l.transform.eulerAngles; s.sunColor = l.color; s.sunInt = l.intensity; s.shadows = (int)l.shadows; s.shadowStr = l.shadowStrength; }
        s.ambMode = (int)RenderSettings.ambientMode; s.amb = RenderSettings.ambientLight; s.ambSky = RenderSettings.ambientSkyColor;
        s.ambEq = RenderSettings.ambientEquatorColor; s.ambGnd = RenderSettings.ambientGroundColor; s.ambInt = RenderSettings.ambientIntensity;
        s.fog = RenderSettings.fog; s.fogMode = (int)RenderSettings.fogMode; s.fogColor = RenderSettings.fogColor; s.fogDens = RenderSettings.fogDensity;
        s.fogStart = RenderSettings.fogStartDistance; s.fogEnd = RenderSettings.fogEndDistance;
        s.skybox = AssetDatabase.GetAssetPath(RenderSettings.skybox);
        var v = FindGradeVolume(); s.volumeProfile = v != null ? AssetDatabase.GetAssetPath(v.sharedProfile) : "";
        foreach (var m in MoodController.WorldMaterials())
        {
            var path = AssetDatabase.GetAssetPath(m);
            if (path.EndsWith(".mat") && File.Exists(path)) s.mats.Add(new MatSnap { path = path, text = File.ReadAllText(path) });
        }
        return s;
    }

    [System.Serializable] class MatSnap { public string path, text; }
    [System.Serializable] class Snap
    {
        public Vector3 sunRot; public Color sunColor; public float sunInt, shadowStr; public int shadows;
        public int ambMode; public Color amb, ambSky, ambEq, ambGnd; public float ambInt;
        public bool fog; public int fogMode; public Color fogColor; public float fogDens, fogStart, fogEnd;
        public string skybox, volumeProfile;
        public List<MatSnap> mats = new List<MatSnap>();
    }
}
