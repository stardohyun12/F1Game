using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// D-227 노을 낚시 자리. 메뉴:
//  · PitCrew/Look/노을          — 켬/끔(SunsetLook.SetOn). 에디트 모드면 씬 dirty(저장은 사람이), Play 중이면 그 Play 동안만.
//  · PitCrew/노을/자리 설치      — Map_TownV6 에 Sunset_D227 루트(노을 값·Cam_Sunset·낚시 자리·소리 3개)를 만든다. 다시 누르면 지우고 새로.
//  · PitCrew/노을/낚시 자리로 가기 — Play 중 걷기 모드에서 낚시 자리 앞으로 순간이동(개발용).
public static class PitCrewSunsetMenu
{
    const string Toggle = "PitCrew/Look/노을";
    const string MatDir = "Assets/Scenes/Map_TownV6_Mats/";
    const string AmbDir = "Assets/Audio/Ambience/";
    public const string RootName = "Sunset_D227";

    // 자리: 손가락 부두(-8.3,-0.4,-287.2) 남쪽 끝(윗면 y≈1.3) — 앉은 눈 = 윗면 + 1.0 m. 왼쪽 마을·가운데 방파제·오른쪽 먼바다와 해(방위 186°)
    public static readonly Vector3 SeatPos = new Vector3(-9.0f, 2.3f, -291.0f);
    public static readonly Vector3 SeatEuler = new Vector3(3f, 151f, 0f);

    [MenuItem(Toggle, false, 20)]
    static void ToggleSunset()
    {
        var s = SunsetLook.Find();
        if (s == null) { Debug.LogWarning("[노을] 이 씬엔 SunsetLook 이 없다 — PitCrew/노을/자리 설치 먼저."); return; }
        s.SetOn(!s.IsOn);
        if (!Application.isPlaying) { EditorUtility.SetDirty(s); EditorSceneManager.MarkSceneDirty(s.gameObject.scene); }
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        Debug.Log("[노을] " + (s.IsOn ? "켬" : "끔"));
    }

    [MenuItem(Toggle, true)]
    static bool ToggleCheck() { var s = SunsetLook.Find(); Menu.SetChecked(Toggle, s != null && s.IsOn); return true; }

    [MenuItem("PitCrew/노을/자리 설치", false, 0)]
    public static void Install()
    {
        if (Application.isPlaying) { Debug.LogWarning("[노을] Play 중엔 설치 안 함"); return; }
        var old = GameObject.Find(RootName);
        if (old != null) { var ol = old.GetComponent<SunsetLook>(); if (ol != null && ol.IsOn) ol.SetOn(false); Object.DestroyImmediate(old); }

        var root = new GameObject(RootName);
        root.transform.position = SeatPos;   // 갈매기(CoastAmbience)는 이 둘레에서 운다 — 자식은 아래에서 월드 좌표로 놓음
        var look = root.AddComponent<SunsetLook>();
        look.sun = RenderSettings.sun != null ? RenderSettings.sun : FindSun();
        var seaGo = GameObject.Find("TownV6_Gray/Sea");
        look.sea = seaGo != null ? seaGo.GetComponent<Renderer>() : null;
        look.skyMaterial = MakeMat(MatDir + "TV6_SkyDusk.mat", "PitCrew/Look/DuskSky");
        look.seaMaterial = MakeMat(MatDir + "TV6_SeaDusk.mat", "PitCrew/Look/DuskSea");
        if (look.seaMaterial != null && look.sea != null && look.sea.sharedMaterial != null && look.sea.sharedMaterial.HasProperty("_Tint"))
            look.seaMaterial.SetColor("_DayTint", look.sea.sharedMaterial.GetColor("_Tint"));

        // Cam_Sunset — 꺼둔 카메라(위치·방향 표시용, Cam_FB_* 와 같은 꼴)
        var camGo = new GameObject("Cam_Sunset"); camGo.transform.SetParent(root.transform, false);
        camGo.transform.SetPositionAndRotation(SeatPos, Quaternion.Euler(SeatEuler));
        var cam = camGo.AddComponent<Camera>(); cam.enabled = false; cam.fieldOfView = 60f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 1500f;

        var spotGo = new GameObject("FishingSpot"); spotGo.transform.SetParent(root.transform, false);
        spotGo.transform.position = SeatPos - Vector3.up * 1.0f;
        var spot = spotGo.AddComponent<FishingSpot>(); spot.seat = camGo.transform;

        // 소리 — 바람(WindAmbience 재사용, 3D 아님: 사방에서 부는 바람), 파도(3D, 물가 가까울수록 큼), 갈매기(가끔, 3D 머리 위)
        var wind = new GameObject("Amb_Wind"); wind.transform.SetParent(root.transform, false); wind.transform.position = SeatPos;
        var wsrc = wind.AddComponent<AudioSource>(); wsrc.spatialBlend = 0.3f; wsrc.rolloffMode = AudioRolloffMode.Linear; wsrc.minDistance = 20f; wsrc.maxDistance = 220f;
        var wa = wind.AddComponent<WindAmbience>(); wa.clip = Clip("amb_wind_01.wav"); wa.ignoreRun = true; wa.gust = 0.35f;
        wa.lowY = -4f; wa.highY = 6f; wa.quietVolume = 0.10f; wa.loudVolume = 0.30f;

        var waves = new GameObject("Amb_Waves"); waves.transform.SetParent(root.transform, false);
        waves.transform.position = new Vector3(-8.6f, 0.2f, -296f);   // 자리 바로 앞 물가
        var vsrc = waves.AddComponent<AudioSource>(); vsrc.clip = Clip("amb_waves_01.wav"); vsrc.loop = true; vsrc.playOnAwake = true;
        vsrc.spatialBlend = 1f; vsrc.rolloffMode = AudioRolloffMode.Logarithmic; vsrc.minDistance = 4f; vsrc.maxDistance = 60f; vsrc.volume = 0.55f;

        var gulls = new GameObject("Amb_Gulls"); gulls.transform.SetParent(root.transform, false); gulls.transform.position = SeatPos + Vector3.up * 12f;
        var gsrc = gulls.AddComponent<AudioSource>(); gsrc.playOnAwake = false; gsrc.spatialBlend = 1f; gsrc.rolloffMode = AudioRolloffMode.Logarithmic; gsrc.minDistance = 6f; gsrc.maxDistance = 120f;
        var lp = gulls.AddComponent<AudioLowPassFilter>(); lp.cutoffFrequency = 3800f;   // 가까이서 녹음된 갈매기 → 멀게
        var coast = root.AddComponent<CoastAmbience>(); coast.waves = vsrc; coast.gulls = gsrc;
        coast.gullClips = new[] { Clip("amb_gull_01.wav"), Clip("amb_gull_02.wav"), Clip("amb_gull_03.wav"), Clip("amb_gull_04.wav"), Clip("amb_gull_05.wav") };

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("[노을] 설치: sun=" + (look.sun != null) + " sea=" + (look.sea != null) + " sky=" + (look.skyMaterial != null) + " seaMat=" + (look.seaMaterial != null));
    }

    [MenuItem("PitCrew/노을/낚시 자리로 가기", false, 1)]
    static void GoToSpot()
    {
        var d = Object.FindFirstObjectByType<OnFootDirector>();
        if (!Application.isPlaying || d == null || !d.GoToSpot()) Debug.LogWarning("[노을] Play 중 걷기 모드에서만");
    }

    static Light FindSun()
    {
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) return l;
        return null;
    }

    static AudioClip Clip(string file)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>(AmbDir + file);
        if (c == null) Debug.LogWarning("[노을] 소리 없음: " + AmbDir + file + " — 슬롯만 남김");
        return c;
    }

    static Material MakeMat(string path, string shader)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        var sh = Shader.Find(shader);
        if (sh == null) { Debug.LogError("[노을] 셰이더 없음: " + shader); return null; }
        m = new Material(sh);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
