using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TrackLayout roadMask의 구조적 건전성 자동 검증.
/// 세션 13에서 "외곽 main과 끊긴 고립 End 4개" 버그 발견 → 재발 방지용 인프라.
///
/// 체크:
///   1. Reachability: startCell에서 BFS로 모든 road cell 도달 가능한가?
///   2. Orphan Ends: 도달 불가능한 End 타일이 있는가?
///   3. Isolated cells: 이웃 exits 0인 road cell (TileMapAuto가 이미 경고하지만 여기서도)
///   4. Dead-end sanity: End 타일이 TrackLayout에 의도된 것인지 (선택적 allowlist)
///
/// BuildingPlacer.VerifyPlacements (D-026)와 동일한 "침묵 실패 방지" 원칙.
/// </summary>
public static class TrackConnectivityVerifier
{
    [MenuItem("PitCrew/Verify Track Connectivity")]
    public static void Execute()
    {
        var layoutGuids = AssetDatabase.FindAssets("t:TrackLayout ComplexTrackLayout");
        if (layoutGuids.Length == 0) { Debug.LogError("[Verify] ComplexTrackLayout 못 찾음"); return; }
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(layoutGuids[0]));
        Verify(layout, logPrefix: "Verify Menu");
    }

    public struct Result
    {
        public bool ok;
        public int totalRoad;
        public int reachable;
        public List<Vector2Int> orphans;       // 도달 불가능한 road cell
        public List<Vector2Int> ends;          // 모든 End 타일 위치
        public List<Vector2Int> endsUnreachable; // 도달 불가능한 End (심각)
        public List<Vector2Int> isolated;      // 이웃 exits = 0
    }

    public static Result Verify(TrackLayout layout, string logPrefix = "Verify")
    {
        var r = new Result { orphans = new(), ends = new(), endsUnreachable = new(), isolated = new() };
        if (layout == null || layout.roadMask == null || layout.roadMask.Length == 0)
        {
            Debug.LogError($"[{logPrefix}] layout 또는 roadMask 비어있음");
            return r;
        }

        int h = layout.roadMask.Length;
        int w = layout.roadMask[0].Length;
        bool IsRoad(int c, int rr)
        {
            if (c < 0 || c >= w || rr < 0 || rr >= h) return false;
            char ch = layout.roadMask[rr][c];
            return ch == '#' || ch == 'X' || ch == 'D';
        }
        int Exits(int c, int rr)
        {
            int n = 0;
            if (IsRoad(c, rr + 1)) n++;
            if (IsRoad(c, rr - 1)) n++;
            if (IsRoad(c + 1, rr)) n++;
            if (IsRoad(c - 1, rr)) n++;
            return n;
        }

        // 모든 road cell 수집 + End/Isolated 기록
        var allRoad = new List<Vector2Int>();
        for (int rr = 0; rr < h; rr++)
        for (int c = 0; c < w; c++)
        {
            if (!IsRoad(c, rr)) continue;
            allRoad.Add(new Vector2Int(c, rr));
            int e = Exits(c, rr);
            if (e == 0) r.isolated.Add(new Vector2Int(c, rr));
            if (e == 1) r.ends.Add(new Vector2Int(c, rr));
        }
        r.totalRoad = allRoad.Count;

        // startCell에서 BFS
        var startCell = layout.startCell;
        if (!IsRoad(startCell.x, startCell.y))
        {
            Debug.LogError($"[{logPrefix}] startCell ({startCell.x},{startCell.y})이 road가 아님. roadMask 확인 필요.");
            return r;
        }

        var reached = new HashSet<Vector2Int> { startCell };
        var q = new Queue<Vector2Int>();
        q.Enqueue(startCell);
        while (q.Count > 0)
        {
            var p = q.Dequeue();
            Vector2Int[] nbrs = {
                new(p.x, p.y + 1), new(p.x, p.y - 1),
                new(p.x + 1, p.y), new(p.x - 1, p.y)
            };
            foreach (var n in nbrs)
            {
                if (!IsRoad(n.x, n.y)) continue;
                if (reached.Add(n)) q.Enqueue(n);
            }
        }
        r.reachable = reached.Count;

        foreach (var cell in allRoad)
            if (!reached.Contains(cell)) r.orphans.Add(cell);

        foreach (var end in r.ends)
            if (!reached.Contains(end)) r.endsUnreachable.Add(end);

        r.ok = r.orphans.Count == 0 && r.isolated.Count == 0 && r.endsUnreachable.Count == 0;

        // 리포트
        string header = $"[{logPrefix}] {layout.Width}×{layout.Height} mask, {r.totalRoad} road cells, {r.reachable} reachable from start {startCell}. Ends={r.ends.Count}";
        if (r.ok)
        {
            Debug.Log(header + "  ✓ 연결성 OK");
        }
        else
        {
            Debug.LogWarning(header);
            if (r.isolated.Count > 0) Debug.LogError($"  ⚠ 고립(exits=0): {CellList(r.isolated)}");
            if (r.orphans.Count > 0) Debug.LogError($"  ⚠ 도달불가 road cell: {CellList(r.orphans)}");
            if (r.endsUnreachable.Count > 0) Debug.LogError($"  ⚠ 도달불가 End (shortcut 고립 가능성): {CellList(r.endsUnreachable)}");
        }

        return r;
    }

    static string CellList(List<Vector2Int> cells)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < cells.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append($"({cells[i].x},{cells[i].y})");
        }
        return sb.ToString();
    }
}
