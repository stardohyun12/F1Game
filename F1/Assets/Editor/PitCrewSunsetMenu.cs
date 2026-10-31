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

    // 자리: 손가락 부두(-8.3,-0.4,-287.2) 남쪽 끝(윗면 y≈1.3) — 앉은 눈 = 윗면 + 1.0 m.
    // D-229: 방향 151° → 180°(해 방위 186° 가 가운데 조금 오른쪽, 왼쪽 1/3 에 방파제 끝). 마을(방위 106~125°)이 안 들어오게 둘러보기 왼쪽 8°·오른쪽 35°.
    public static readonly Vector3 SeatPos = new Vector3(-9.0f, 2.3f, -291.0f);
    public static readonly Vector3 SeatEuler = new Vector3(3f, 180f, 0f);
    public const float LookLeft = 8f;

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
        var spot = spotGo.AddComponent<FishingSpot>(); spot.seat = camGo.transform; spot.yawLeft = LookLeft;

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

    // D-229 물가 거품용 물 밑 깊이 — 깊이 텍스처는 이 RT 카메라에서 비어 나와서(2판 실측) 레이로 굽는다.
    // 자리 둘레 DepthSize m 정사각을 DepthTexel m 칸으로: 위에서 아래로 레이 → 처음 맞은 것(지형·부두 윗면·방파제)의 높이.
    // R = 물 밑 깊이 / 8 m (물 위로 나온 곳 = 0). 부두는 윗면이 맞으니 거품은 부두 테두리를 두른다.
    public const float DepthSize = 320f, DepthTexel = 0.5f, DepthMax = 8f, ShoreDistMax = 400f;   // ShoreDistMax = DuskSea.shader 의 400 과 같이
    [MenuItem("PitCrew/노을/물 밑 깊이 굽기", false, 2)]
    public static void BakeSeaDepth()
    {
        var look = SunsetLook.Find();
        if (look == null || look.seaMaterial == null) { Debug.LogWarning("[노을] 자리 설치 먼저"); return; }
        int n = Mathf.RoundToInt(DepthSize / DepthTexel);
        float x0 = SeatPos.x - DepthSize * .5f, z0 = SeatPos.z - DepthSize * .5f;
        var tex = new Texture2D(n, n, TextureFormat.RGB24, false, true);
        var px = new Color32[n * n];
        // 3판(D-230): 부두 윗면 밑은 물로 본다(윗면에서 다시 아래로 쏴 바닥이 물 밑이면 물) + 물 높이에 박힌 기둥·바위는 CheckBox 로 땅.
        //  G = 물가(땅)까지 거리 — 2-패스 챔퍼 거리, sqrt(거리/ShoreDistMax) 로 담아 가까운 곳 정밀도를 남긴다.
        //  상한이 커야 하는 이유: 먼 물가(150 m)는 화면 1px 가 물 28 m 라 4~5px 거품 띠 = 물가에서 140 m
        var depth = new float[n * n]; var land = new bool[n * n];
        var half = new Vector3(DepthTexel * .5f, .12f, DepthTexel * .5f);
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float wx = x0 + (i + .5f) * DepthTexel, wz = z0 + (j + .5f) * DepthTexel;
                float d = DepthMax; bool isLand = false;
                float y0 = 40f;
                for (int k = 0; k < 4; k++)
                {
                    if (!Physics.Raycast(new Vector3(wx, y0, wz), Vector3.down, out var hit, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    break;   // 밑에 아무것도 없음 = 깊은 물
                    if (hit.point.y <= 0f) { d = Mathf.Clamp(-hit.point.y, 0f, DepthMax); break; }
                    // 물 위로 나온 면: 그 밑이 비었으면(지형 속) 땅, 밑에 또 면이 있으면 다시
                    if (!Physics.Raycast(hit.point - Vector3.up * .05f, Vector3.down, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) { isLand = true; break; }
                    y0 = hit.point.y - .05f;
                    if (k == 3) isLand = true;
                }
                if (!isLand && Physics.CheckBox(new Vector3(wx, 0f, wz), half, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) isLand = true;
                if (isLand) d = 0f;
                depth[j * n + i] = d; land[j * n + i] = isLand || d <= 0.02f;
            }
        var dist = new float[n * n];
        const float Big = 1e6f, DiagW = 1.41421f;
        for (int q = 0; q < dist.Length; q++) dist[q] = land[q] ? 0f : Big;
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                int q = j * n + i; float v = dist[q];
                if (i > 0) v = Mathf.Min(v, dist[q - 1] + 1f);
                if (j > 0) { v = Mathf.Min(v, dist[q - n] + 1f); if (i > 0) v = Mathf.Min(v, dist[q - n - 1] + DiagW); if (i < n - 1) v = Mathf.Min(v, dist[q - n + 1] + DiagW); }
                dist[q] = v;
            }
        for (int j = n - 1; j >= 0; j--)
            for (int i = n - 1; i >= 0; i--)
            {
                int q = j * n + i; float v = dist[q];
                if (i < n - 1) v = Mathf.Min(v, dist[q + 1] + 1f);
                if (j < n - 1) { v = Mathf.Min(v, dist[q + n] + 1f); if (i < n - 1) v = Mathf.Min(v, dist[q + n + 1] + DiagW); if (i > 0) v = Mathf.Min(v, dist[q + n - 1] + DiagW); }
                dist[q] = v;
            }
        int landCount = 0;
        for (int q = 0; q < px.Length; q++)
        {
            if (land[q]) landCount++;
            byte r = (byte)Mathf.RoundToInt(depth[q] / DepthMax * 255f);
            float m = Mathf.Min(dist[q] * DepthTexel, ShoreDistMax);
            byte g = (byte)Mathf.RoundToInt(Mathf.Sqrt(m / ShoreDistMax) * 255f);
            px[q] = new Color32(r, g, 0, 255);
        }
        tex.SetPixels32(px); tex.Apply();
        string path = MatDir + "TV6_SeaDepth.png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.sRGBTexture = false; ti.mipmapEnabled = false; ti.wrapMode = TextureWrapMode.Clamp; ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 1024; ti.npotScale = TextureImporterNPOTScale.None; ti.SaveAndReimport();
        AssetDatabase.Refresh();
        var dt = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (dt == null) Debug.LogWarning("[노을] 깊이 텍스처를 못 읽음 — 메뉴를 한 번 더 누를 것");
        look.seaMaterial.SetTexture("_DepthTex", dt);
        look.seaMaterial.SetVector("_DepthRect", new Vector4(x0, z0, DepthSize, DepthMax));
        EditorUtility.SetDirty(look.seaMaterial); AssetDatabase.SaveAssets();
        Debug.Log("[노을] 물 밑 깊이·물가 거리 " + n + "×" + n + " 땅 칸 " + landCount + " → " + path);
    }

    // D-229 이 장면에서 보이는 옛 대역 나무(LofiPilot CoastalTree)만 숲 나무 F1·F2 로. 원본은 끄기만 하고(SunsetLook.hiddenTrees) 새 나무는 Sunset_D227/Trees_F 아래.
    // 범위 = 앉은 눈에서 수평 TreeRadius m 안, 방위 TreeBearing.x~y°(둘러보기 범위 + 화면 반폭). 다시 누르면 되돌린 뒤 새로.
    public const float TreeRadius = 450f;
    public static readonly Vector2 TreeBearing = new Vector2(115f, 275f);
    const string OldTree = "Assets/Art/LofiPilot/CoastalTree.prefab";
    static readonly string[] NewTrees = { "Assets/Art/Library/Trees/forest_F1/forest_F1.prefab", "Assets/Art/Library/Trees/forest_F2/forest_F2.prefab" };
    [MenuItem("PitCrew/노을/보이는 나무 → 숲 나무", false, 3)]
    public static void SwapTrees()
    {
        var look = SunsetLook.Find(); if (look == null) { Debug.LogWarning("[노을] 자리 설치 먼저"); return; }
        RestoreTrees();
        var news = new GameObject[NewTrees.Length];
        for (int k = 0; k < news.Length; k++) news[k] = AssetDatabase.LoadAssetAtPath<GameObject>(NewTrees[k]);
        if (news[0] == null || news[1] == null) { Debug.LogWarning("[노을] 숲 나무 프리팹 없음"); return; }
        var holder = new GameObject("Trees_F").transform; holder.SetParent(look.transform, true);
        var hidden = new System.Collections.Generic.List<GameObject>();
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
            if (root == null || hidden.Contains(root)) continue;
            var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(root);
            if (src == null || AssetDatabase.GetAssetPath(src) != OldTree) continue;
            Vector3 d = root.transform.position - SeatPos; d.y = 0f;
            float brg = Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f);
            if (d.magnitude > TreeRadius || brg < TreeBearing.x || brg > TreeBearing.y) continue;
            int pick = (Mathf.Abs(root.transform.position.GetHashCode()) % 3 == 0) ? 1 : 0;   // F1 2 : F2 1
            var t = (GameObject)PrefabUtility.InstantiatePrefab(news[pick], holder);
            t.name = news[pick].name + "_" + hidden.Count;
            t.transform.SetPositionAndRotation(root.transform.position, Quaternion.Euler(0f, Mathf.Repeat(root.transform.position.x * 37f + root.transform.position.z * 11f, 360f), 0f));
            root.SetActive(false); hidden.Add(root);
        }
        look.hiddenTrees = hidden.ToArray();
        EditorUtility.SetDirty(look); EditorSceneManager.MarkSceneDirty(look.gameObject.scene);
        Debug.Log("[노을] 나무 바꿈 " + hidden.Count + "그루");
    }

    [MenuItem("PitCrew/노을/나무 되돌리기", false, 4)]
    public static void RestoreTrees()
    {
        var look = SunsetLook.Find(); if (look == null) return;
        if (look.hiddenTrees != null) foreach (var g in look.hiddenTrees) if (g != null) g.SetActive(true);
        look.hiddenTrees = null;
        var old = look.transform.Find("Trees_F"); if (old != null) Object.DestroyImmediate(old.gameObject);
        EditorUtility.SetDirty(look); EditorSceneManager.MarkSceneDirty(look.gameObject.scene);
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
