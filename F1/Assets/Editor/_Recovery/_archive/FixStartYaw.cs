using UnityEditor;
using UnityEngine;

/// <summary>
/// startCell 위치의 roadMask 인접 셀을 분석해 startYawDeg 자동 추론.
/// Yaw convention: 0=+Z(북, row 증가), 90=+X(동, col 증가), 180=-Z(남), 270=-X(서).
///
/// 도현 요청 (세션 15): "차량 방향이 길과 같게 배치좀".
/// 기존: 단순 90 고정 → 강화: roadMask 분석 후 자동 결정.
/// </summary>
public static class FixStartYaw
{
    const string ASSET_PATH = "Assets/Data/ComplexTrackLayout.asset";

    [MenuItem("PitCrew/Layout/Fix Start Yaw (Auto-Align)")]
    public static void Execute()
    {
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(ASSET_PATH);
        if (layout == null) { Debug.LogError($"layout missing: {ASSET_PATH}"); return; }

        int c = layout.startCell.x;
        int r = layout.startCell.y;

        // 4방향 도로 검사 ('#' 또는 'X' 등 '.' 아닌 모든 char를 도로로 간주).
        bool roadE = IsAnyRoad(layout, c + 1, r);
        bool roadW = IsAnyRoad(layout, c - 1, r);
        bool roadN = IsAnyRoad(layout, c, r + 1);
        bool roadS = IsAnyRoad(layout, c, r - 1);

        float yaw = layout.startYawDeg; // fallback
        string reason;

        // 우선순위: 더 멀리 이어지는 직선 도로 방향 선택.
        // 양쪽 도로면 동/북 우선 (코스 진행 방향).
        if (roadE && roadW) { yaw = 90f; reason = "horizontal road (E↔W) → East"; }
        else if (roadN && roadS) { yaw = 0f; reason = "vertical road (N↕S) → North"; }
        else if (roadE) { yaw = 90f; reason = "dead-end West → face East"; }
        else if (roadW) { yaw = 270f; reason = "dead-end East → face West"; }
        else if (roadN) { yaw = 0f; reason = "dead-end South → face North"; }
        else if (roadS) { yaw = 180f; reason = "dead-end North → face South"; }
        else { reason = "isolated cell — startYawDeg unchanged"; }

        Debug.Log($"[FixStartYaw] startCell=({c},{r}) E={roadE} W={roadW} N={roadN} S={roadS} → yaw={yaw}° ({reason})");

        layout.startYawDeg = yaw;
        EditorUtility.SetDirty(layout);
        AssetDatabase.SaveAssets();

        // 씬에 즉시 반영.
        SetupRaceInScene.PlaceInCurrent();
    }

    static bool IsAnyRoad(TrackLayout layout, int c, int r)
    {
        if (layout.roadMask == null) return false;
        if (r < 0 || r >= layout.roadMask.Length) return false;
        var row = layout.roadMask[r];
        if (c < 0 || c >= row.Length) return false;
        return row[c] != '.';
    }
}
