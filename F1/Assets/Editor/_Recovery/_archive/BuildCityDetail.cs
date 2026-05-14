using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 세션 18 (c) — 도시 디테일 패스.
// "도시 느낌" 70~80% 달성 목표. 미사용 Kenney 모델 자동 활용.
//
// 단계:
//   1. URP Volume + Bloom + ColorAdjustments + Fog (skybox 분위기)
//   2. 가로수 (suburban tree-large/small) — 비도로 perimeter cell 자동
//   3. 가로등 (city-roads light-square) — 모든 도로 교차로 인접
//   4. 인도 (suburban path-stones) — 도로↔빌딩 경계
//   5. 주차 차량 (Kenny car-kit 일부) — 도로 옆 빈 cell
//
// 모두 OpenWorldLayout 마스크 기반 자동 배치. 결과는 GroundField 자식 "Detail/" 아래.
public static class BuildCityDetail
{
    const string LayoutPath = "Assets/Data/OpenWorldLayout.asset";
    const float CellSize = 6f;

    const string SuburbanRoot = "Assets/Model/kenney_city-kit-suburban_20/Models/FBX format/";
    const string RoadsRoot = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";
    const string CarKitRoot = "Assets/Model/kenney_car-kit/Models/FBX format/";

    [MenuItem("PitCrew/Layout/Build City Detail Pass")]
    public static void Execute()
    {
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(LayoutPath);
        if (layout == null) { Debug.LogError("OpenWorldLayout 못 찾음"); return; }

        // 1. URP Volume — Bloom + 컬러
        EnsureUrpVolume();

        // 2. Detail root
        var existing = GameObject.Find("Detail");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("Detail");
        Undo.RegisterCreatedObjectUndo(root, "Build City Detail");

        int trees = PlaceTrees(root, layout);
        int lights = PlaceStreetlights(root, layout);
        int paths = PlacePaths(root, layout);
        int cars = PlaceParkedCars(root, layout);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[CityDetail] trees={trees}, streetlights={lights}, paths={paths}, parked-cars={cars}");
    }

    static void EnsureUrpVolume()
    {
        var existing = GameObject.Find("URP Volume");
        if (existing != null) Object.DestroyImmediate(existing);

        var go = new GameObject("URP Volume", typeof(Volume));
        var vol = go.GetComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "OpenWorldVolume";

        // Bloom
        var bloom = profile.Add<Bloom>();
        bloom.intensity.Override(0.6f);
        bloom.threshold.Override(1.1f);
        bloom.scatter.Override(0.7f);

        // Color Adjustments — 따뜻한 톤
        var color = profile.Add<ColorAdjustments>();
        color.postExposure.Override(0.05f);
        color.contrast.Override(8f);
        color.colorFilter.Override(new Color(1.02f, 0.99f, 0.94f, 1f)); // 살짝 따뜻하게
        color.saturation.Override(8f);

        // Vignette — 가장자리 살짝 어둡게
        var vig = profile.Add<Vignette>();
        vig.intensity.Override(0.18f);
        vig.smoothness.Override(0.5f);

        // Tonemapping — ACES
        var tm = profile.Add<Tonemapping>();
        tm.mode.Override(TonemappingMode.ACES);

        vol.profile = profile;

        // 안개 (Render Settings)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.65f, 0.72f, 0.82f, 1f); // 약간 푸른 거리감
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 400f;

        // Ambient — 부드러운 라이트
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.46f, 0.48f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.25f, 1f);

        Debug.Log("[CityDetail] URP Volume + Fog 설정 완료.");
    }

    static int PlaceTrees(GameObject parent, TrackLayout layout)
    {
        var treeLarge = AssetDatabase.LoadAssetAtPath<GameObject>(SuburbanRoot + "tree-large.fbx");
        var treeSmall = AssetDatabase.LoadAssetAtPath<GameObject>(SuburbanRoot + "tree-small.fbx");
        if (treeLarge == null && treeSmall == null) return 0;

        var sub = new GameObject("Trees");
        sub.transform.SetParent(parent.transform, false);

        int count = 0;
        for (int r = 0; r < layout.Height; r++)
        {
            for (int c = 0; c < layout.Width; c++)
            {
                if (layout.IsRoad(c, r)) continue;
                char b = layout.BuildingChar(c, r);
                if (b != '.') continue; // 빌딩 있는 셀 건드리지 X — 빈 셀에만

                // 도로 인접 셀에만 가로수 (외곽 perimeter 옆 우선)
                bool adjacent = layout.IsRoad(c - 1, r) || layout.IsRoad(c + 1, r) ||
                                layout.IsRoad(c, r - 1) || layout.IsRoad(c, r + 1);
                if (!adjacent) continue;

                // hash로 결정 — 같은 셀 항상 같은 결과
                int hash = c * 73856093 ^ r * 19349663;
                bool useLarge = (hash & 1) == 0;
                var src = (useLarge && treeLarge != null) ? treeLarge : treeSmall;
                if (src == null) continue;

                float h = layout.GetHeight(c, r);
                Vector3 pos = new Vector3(c * CellSize, h + 0.05f, r * CellSize);
                // 셀 안 살짝 흩뿌림
                pos += new Vector3(((hash >> 4) & 0xF) / 15f - 0.5f, 0f, ((hash >> 12) & 0xF) / 15f - 0.5f) * 1.5f;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
                inst.name = $"Tree_{c}_{r}";
                inst.transform.SetParent(sub.transform, false);
                inst.transform.localPosition = pos;
                inst.transform.localRotation = Quaternion.Euler(0f, (hash & 0xFF), 0f); // 랜덤 회전
                inst.transform.localScale = Vector3.one * (useLarge ? 4f : 3f);
                count++;
            }
        }
        return count;
    }

    static int PlaceStreetlights(GameObject parent, TrackLayout layout)
    {
        var lightSrc = AssetDatabase.LoadAssetAtPath<GameObject>(RoadsRoot + "light-square.fbx");
        if (lightSrc == null) return 0;

        var sub = new GameObject("Streetlights");
        sub.transform.SetParent(parent.transform, false);

        int count = 0;
        for (int r = 0; r < layout.Height; r++)
        {
            for (int c = 0; c < layout.Width; c++)
            {
                if (!layout.IsRoad(c, r)) continue;
                // 교차로(3+ exit) 인접 비-도로 셀 4 코너에 가로등
                int exits = 0;
                if (layout.IsRoad(c, r + 1)) exits++;
                if (layout.IsRoad(c, r - 1)) exits++;
                if (layout.IsRoad(c + 1, r)) exits++;
                if (layout.IsRoad(c - 1, r)) exits++;
                if (exits < 3) continue;

                // 4 corners 비-도로면 가로등
                int[] dx = { -1, 1, -1, 1 };
                int[] dy = { -1, -1, 1, 1 };
                for (int k = 0; k < 4; k++)
                {
                    int nc = c + dx[k], nr = r + dy[k];
                    if (nc < 0 || nc >= layout.Width || nr < 0 || nr >= layout.Height) continue;
                    if (layout.IsRoad(nc, nr)) continue;
                    if (layout.BuildingChar(nc, nr) != '.') continue;

                    Vector3 pos = new Vector3(c * CellSize + dx[k] * 2.8f, layout.GetHeight(nc, nr) + 0.05f, r * CellSize + dy[k] * 2.8f);
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(lightSrc);
                    inst.name = $"Light_{c}_{r}_{k}";
                    inst.transform.SetParent(sub.transform, false);
                    inst.transform.localPosition = pos;
                    inst.transform.localScale = Vector3.one * 6f;
                    count++;
                }
            }
        }
        return count;
    }

    static int PlacePaths(GameObject parent, TrackLayout layout)
    {
        // 인도 — 도로↔빌딩 경계 셀에 path-stones-long
        var pathSrc = AssetDatabase.LoadAssetAtPath<GameObject>(SuburbanRoot + "path-stones-long.fbx");
        if (pathSrc == null) return 0;

        var sub = new GameObject("Paths");
        sub.transform.SetParent(parent.transform, false);

        int count = 0;
        for (int r = 0; r < layout.Height; r++)
        {
            for (int c = 0; c < layout.Width; c++)
            {
                if (layout.IsRoad(c, r)) continue;
                char b = layout.BuildingChar(c, r);
                if (b == '.') continue; // 빌딩 있는 셀만 (그 입구쪽 인도)

                // 4방향 중 도로 쪽으로 인도 1조각
                int[] dx = { 0, 0, 1, -1 };
                int[] dy = { 1, -1, 0, 0 };
                int[] yaw = { 0, 180, 90, 270 };
                for (int k = 0; k < 4; k++)
                {
                    int nc = c + dx[k], nr = r + dy[k];
                    if (!layout.IsRoad(nc, nr)) continue;
                    Vector3 pos = new Vector3(c * CellSize + dx[k] * 2.4f, layout.GetHeight(c, r) + 0.06f, r * CellSize + dy[k] * 2.4f);
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(pathSrc);
                    inst.name = $"Path_{c}_{r}_{k}";
                    inst.transform.SetParent(sub.transform, false);
                    inst.transform.localPosition = pos;
                    inst.transform.localRotation = Quaternion.Euler(0f, yaw[k], 0f);
                    inst.transform.localScale = Vector3.one * 2.5f;
                    count++;
                    break; // 셀당 한 방향만
                }
            }
        }
        return count;
    }

    static int PlaceParkedCars(GameObject parent, TrackLayout layout)
    {
        // Kenny car-kit에서 몇 개 골라 도로 옆 빈 자리에 정적 배치
        string[] models = { "ambulance.fbx", "delivery.fbx", "police.fbx", "race.fbx", "sedan.fbx", "taxi.fbx", "truck.fbx" };
        var srcs = new List<GameObject>();
        foreach (var m in models)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(CarKitRoot + m);
            if (src != null) srcs.Add(src);
        }
        if (srcs.Count == 0) return 0;

        var sub = new GameObject("ParkedCars");
        sub.transform.SetParent(parent.transform, false);

        int count = 0;
        for (int r = 0; r < layout.Height; r++)
        {
            for (int c = 0; c < layout.Width; c++)
            {
                if (!layout.IsRoad(c, r)) continue;
                // 매 9번째 도로 셀에 1대 — 너무 많지 않게
                int hash = c * 73856093 ^ r * 19349663;
                if ((hash & 0xF) > 1) continue; // ~1/8 확률

                // 도로 가장자리 (perimeter 외곽 쪽) 위치
                bool perimSouth = !layout.IsRoad(c, r - 1);
                bool perimNorth = !layout.IsRoad(c, r + 1);
                if (!perimSouth && !perimNorth) continue;

                int idx = (hash >> 8) % srcs.Count;
                if (idx < 0) idx += srcs.Count;
                var src = srcs[idx];

                Vector3 pos = new Vector3(c * CellSize, layout.GetHeight(c, r) + 0.4f, r * CellSize + (perimSouth ? -2.0f : 2.0f));
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
                inst.name = $"ParkedCar_{c}_{r}";
                inst.transform.SetParent(sub.transform, false);
                inst.transform.localPosition = pos;
                inst.transform.localRotation = Quaternion.Euler(0f, perimSouth ? 0f : 180f, 0f);
                inst.transform.localScale = Vector3.one * 1.5f;
                count++;
            }
        }
        return count;
    }
}
