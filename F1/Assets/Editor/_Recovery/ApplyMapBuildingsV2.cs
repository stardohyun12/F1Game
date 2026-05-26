using UnityEditor;
using UnityEngine;

// 일회성: 아트 디렉터(병렬 에이전트)가 설계한 새 buildingMask 적용 — 지구 정체성 강화
// (다운타운 H고층 중앙 클러스터 / 공업 F+T / 항구 W+T 물가 라인). row3은 통합 시 30자로 수정.
// BuildingPlacer는 도로 셀 자동 스킵 + "Buildings" 루트만 교체(시스템 안전).
public static class ApplyMapBuildingsV2
{
    static readonly string[] NewBuildingMask =
    {
        "..............................",
        ".F..T....F.T.T..F..T....F.T...",
        "..............................",
        ".T.F.....F...T..T.F....F..T...",
        "..............................",
        ".....T...T......T....T........",
        "..............................",
        "..............................",
        "K..K..K..HH.HH..HH.HH.K..K..K.",
        ".........HH.HH..HH.HH.........",
        "..............................",
        "..............................",
        "HH.HH.K..HH.K...K..HH.K..HH.HH",
        "HH.HH....HH........HH....HH.HH",
        "..............................",
        "..............................",
        "TWWW.WWTW..WT...WWWTW..TWWWWTW",
        "WWWT.WWWWT.TW...WTWWWW.WWWWTW.",
        "WTWW.TWTW..WW...WWWWTW.WTTWWWW",
        "TWTWTWTWTWTWTW..WTWTWTWTWTWTWT",
        "TWTWTWTWTWTWTWTWTWTWTWTWTWTWTW",
        "WTWTWTWTWTWTWTWTWTWTWTWTWTWTWT",
    };

    [MenuItem("PitCrew/Map/Apply Buildings V2")]
    public static void Execute()
    {
        var guids = AssetDatabase.FindAssets("t:TrackLayout");
        if (guids.Length == 0) { Debug.LogError("[BuildingsV2] TrackLayout 못 찾음"); return; }
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(path);

        // 안전망: 모든 행 30자 강제 + 도로 셀에 올라간 건물 문자 제거(BuildingPlacer도 스킵하지만 데이터도 정리).
        var road = layout.roadMask;
        var clean = new string[NewBuildingMask.Length];
        int blanked = 0;
        for (int r = 0; r < NewBuildingMask.Length; r++)
        {
            var row = NewBuildingMask[r];
            if (row.Length < 30) row = row.PadRight(30, '.');
            else if (row.Length > 30) row = row.Substring(0, 30);
            var chars = row.ToCharArray();
            if (road != null && r < road.Length)
                for (int c = 0; c < 30 && c < road[r].Length; c++)
                    if (road[r][c] == '#' && chars[c] != '.') { chars[c] = '.'; blanked++; }
            clean[r] = new string(chars);
        }
        layout.buildingMask = clean;
        EditorUtility.SetDirty(layout);
        AssetDatabase.SaveAssets();

        var go = BuildingPlacer.Build(layout);
        Debug.Log($"[BuildingsV2] ✓ buildingMask 적용+재배치. 도로충돌 정리 {blanked}셀. Buildings root={(go != null)}");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
