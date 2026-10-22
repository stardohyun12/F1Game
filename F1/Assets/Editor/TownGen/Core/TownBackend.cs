using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PitCrew.TownGen
{
    /// <summary>뒷단 실행 결과 — 격자 + 검사 보고.</summary>
    public class TownGenResult
    {
        public TownGrid grid;
        public TownReport report;
        public string polylinesJson;
    }

    /// <summary>
    /// 공통 뒷단(스펙 §2 ①~⑧): 등급 매핑 → 래스터화 → 경사 끊기 → 승격(a) → 출발점 → 정리(막다른 가지·평행선)
    /// → A* 잇기 → 고립 버림 → 배달지 11 → 고도 단계·랜드마크 → D-100 검사(+승격 b) → 수용 기준 숫자.
    /// 두 앞단(OSM·roadgen)에 똑같이 한 벌로 적용한다 — 앞단별 분기 없음.
    /// 결정론: 반복 순서는 전부 칸 번호/세그먼트 순서. 해시 집합 순회 없음.
    /// </summary>
    public static class TownBackend
    {
        // 칸 우선순위(지울 때 낮은 것 먼저): A*잇기 < 승격 footway < 골목 < 큰길
        static int Rank(TownGrid g, int i)
        {
            if (g.kind[i] == CellKind.Major) return 4;
            switch (g.origin[i])
            {
                case CellOrigin.Connector: return 1;
                case CellOrigin.PromotedA:
                case CellOrigin.PromotedB: return 2;
                default: return 3;
            }
        }

        public static TownGenResult Run(RoadNetwork net, IHeightField hf, TownGenSettings s)
        {
            var sw = Stopwatch.StartNew();
            var rep = new TownReport { source = net.source, seed = net.seed };
            int W = s.Columns, H = s.Rows;
            var g = new TownGrid(W, H, s.cellSizeMeters);
            rep.Metric("grid", $"{W}x{H} 칸, 칸 {s.cellSizeMeters}m, 땅 {s.boundsXMeters}x{s.boundsZMeters}m");
            if (Math.Abs(net.boundsX - s.boundsXMeters) > 0.5 || Math.Abs(net.boundsZ - s.boundsZMeters) > 0.5)
                rep.Warn($"도로망 bounds_m [{net.boundsX},{net.boundsZ}] ≠ 설정 땅 [{s.boundsXMeters},{s.boundsZMeters}] — 설정 기준으로 래스터화");

            // 0) 연속 고도: 칸 중심 샘플
            for (int r = 0; r < H; r++)
                for (int c = 0; c < W; c++)
                    g.height[g.Index(c, r)] = hf.HeightAt((c + 0.5) * s.cellSizeMeters, (r + 0.5) * s.cellSizeMeters) * s.heightScale;

            // 0-b) 바다 칸: 칸 최저 높이(중심+네 모서리) ≤ 해수면 인 칸 중 남쪽 변(행 0)에서 이어진 것 (D-110).
            //      내륙 저지대(예: 잘라 붙인 포지타노 해변)는 바다 아님 — 연못을 만들지 않는다.
            MarkSea(g, hf, s);
            rep.Metric("sea_cells", $"{g.sea.Count(b => b)} (해수면 {s.seaLevelMeters}m, 북쪽 끝 행 {g.seaTopRow})");

            // ① 등급 매핑 + ③-1 래스터화
            var footways = new List<KeyValuePair<int, List<int>>>(); // (segment id, 칸들)
            int pathDropped = 0, stepsSegs = 0, alleyCells = 0, seaCut = 0;
            foreach (var seg in net.segments)
            {
                var cells = Rasterize(seg.points, s.cellSizeMeters, W, H);
                switch (seg.cls)
                {
                    case RoadClass.Major:
                        foreach (var i in cells) { if (g.sea[i]) { seaCut++; continue; } g.kind[i] = CellKind.Major; }
                        break;
                    case RoadClass.Minor:
                    case RoadClass.Alley:
                        foreach (var i in cells)
                            if (g.sea[i]) seaCut++;
                            else if (g.kind[i] == CellKind.Empty) { g.kind[i] = CellKind.Minor; g.origin[i] = CellOrigin.Source; if (seg.cls == RoadClass.Alley) alleyCells++; }
                        break;
                    case RoadClass.Footway:
                        footways.Add(new KeyValuePair<int, List<int>>(seg.id, cells));
                        break;
                    case RoadClass.Steps:
                        stepsSegs++;
                        foreach (var i in cells) g.stepsDecor[i] = true;
                        break;
                    default:
                        pathDropped++;
                        break;
                }
            }
            for (int i = 0; i < g.kind.Length; i++) if (g.kind[i] == CellKind.Major) g.origin[i] = CellOrigin.Source;
            rep.Metric("segments", $"{net.segments.Count}개 (footway {footways.Count} · steps {stepsSegs} · path 버림 {pathDropped})");
            rep.Metric("raster_road_cells", g.CountRoad().ToString(CultureInfo.InvariantCulture));
            rep.Metric("alley_cells", alleyCells.ToString(CultureInfo.InvariantCulture));
            rep.Metric("sea_cut_raster_cells", seaCut.ToString(CultureInfo.InvariantCulture));

            var protectedCells = new bool[W * H];

            // ③-2 경사 끊기 (출발점 정하기 전 — 보호 칸 없음)
            int steepCut = SteepCut(g, s, protectedCells, rep);

            // ② 승격 (a) 연결성
            var promoted = new HashSet<int>();
            int promotedA = PromoteForConnectivity(g, s, footways, promoted);

            // 출발점
            if (!PickStart(g, s, rep)) { rep.Fail("start", "출발점 후보 없음 (도로 칸 0)"); return Finish(g, rep, net, sw, s); }
            protectedCells[g.start] = true;

            // ③-3·4 정리, ③-5 잇기 — 반복
            int deadEnd = 0, thinned = 0, connector = 0, droppedIsolated = 0;
            for (int pass = 0; pass < s.maxRepairPasses; pass++)
            {
                deadEnd += PruneDeadEnds(g, s, protectedCells);
                thinned += ThinParallel(g, protectedCells);
                connector += Reconnect(g, s, rep);
                int cut = SteepCut(g, s, protectedCells, rep);
                steepCut += cut;
                g.Components(out var sizes);
                if (cut == 0 && sizes.Count <= 1) break;
            }
            droppedIsolated = DropNonMain(g);
            deadEnd += PruneDeadEnds(g, s, protectedCells);

            // ⑦ 배달지 11
            PlaceDeliveries(g, s, rep);
            foreach (var d in g.deliveries) protectedCells[d.cell] = true;

            // 고도 단계 + ③ 랜드마크
            AssignTiers(g, s, rep);
            PlaceLandmark(g, net, rep);

            // ⑥ D-100 ② 검사 (+ 승격 b)
            int promotedB = CheckDetours(g, s, footways, promoted, rep);

            rep.Metric("steep_cut_cells", steepCut.ToString(CultureInfo.InvariantCulture));
            rep.Metric("promoted_footway_a", promotedA.ToString(CultureInfo.InvariantCulture));
            rep.Metric("promoted_footway_b", promotedB.ToString(CultureInfo.InvariantCulture));
            rep.Metric("connector_cells_astar", connector.ToString(CultureInfo.InvariantCulture));
            rep.Metric("dead_end_removed_cells", deadEnd.ToString(CultureInfo.InvariantCulture));
            rep.Metric("thinned_cells", thinned.ToString(CultureInfo.InvariantCulture));
            rep.Metric("dropped_isolated_cells", droppedIsolated.ToString(CultureInfo.InvariantCulture));

            return Finish(g, rep, net, sw, s);
        }

        static TownGenResult Finish(TownGrid g, TownReport rep, RoadNetwork net, Stopwatch sw, TownGenSettings s)
        {
            if (g.start >= 0)
            {
                int roadOnSea = 0;
                for (int i = 0; i < g.kind.Length; i++) if (g.sea[i] && g.IsRoad(i)) roadOnSea++;
                rep.Check("SEA", roadOnSea == 0, $"바다 칸 위 도로 {roadOnSea}");
                CheckD100Slope(g, s, rep);
                CheckConnectivity(g, rep);
                CheckDeliveries(g, s, rep);
                Shape(g, rep);
                rep.Metric("road_mask_sha1_12", g.RoadMaskHash());
            }
            sw.Stop();
            rep.Metric("backend_ms", sw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture));
            return new TownGenResult { grid = g, report = rep, polylinesJson = net.ToJson() };
        }

        // ------------------------------------------------------------------ 바다
        /// <summary>바다 칸만 계산한 격자 (도로 없음). 곡선 큰길(SplineTown)이 같은 바다 규칙(D-110)을 쓰도록 공개.</summary>
        public static TownGrid SeaGrid(IHeightField hf, TownGenSettings s)
        {
            var g = new TownGrid(s.Columns, s.Rows, s.cellSizeMeters);
            for (int r = 0; r < g.H; r++)
                for (int c = 0; c < g.W; c++)
                    g.height[g.Index(c, r)] = hf.HeightAt((c + 0.5) * s.cellSizeMeters, (r + 0.5) * s.cellSizeMeters) * s.heightScale;
            MarkSea(g, hf, s);
            return g;
        }

        static void MarkSea(TownGrid g, IHeightField hf, TownGenSettings s)
        {
            double cs = s.cellSizeMeters;
            var low = new bool[g.W * g.H];
            for (int r = 0; r < g.H; r++)
                for (int c = 0; c < g.W; c++)
                {
                    double x0 = c * cs, z0 = r * cs, m = g.height[g.Index(c, r)];
                    foreach (var (x, z) in new[] { (x0, z0), (x0 + cs, z0), (x0, z0 + cs), (x0 + cs, z0 + cs) })
                        m = Math.Min(m, hf.HeightAt(x, z) * s.heightScale);
                    low[g.Index(c, r)] = m <= s.seaLevelMeters;
                }
            var q = new Queue<int>();
            for (int c = 0; c < g.W; c++) { int i = g.Index(c, 0); if (low[i]) { g.sea[i] = true; q.Enqueue(i); } }
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                g.seaTopRow = Math.Max(g.seaTopRow, g.Row(u));
                foreach (var v in g.Neighbors4(u)) if (low[v] && !g.sea[v]) { g.sea[v] = true; q.Enqueue(v); }
            }
        }

        // ------------------------------------------------------------------ 래스터화
        /// <summary>
        /// 폴리라인 → 4방향 연결 칸 목록 (Amanatides–Woo 격자 순회 — 선이 지나는 칸을 빠짐없이, 대각선은 계단식 2칸).
        /// 땅 밖 칸은 버린다. 연속 중복 제거, 순서 = 선 진행 순서.
        /// </summary>
        public static List<int> Rasterize(List<Vec2> pts, double cell, int W, int H)
        {
            var outCells = new List<int>();
            int last = -1;
            void Add(int c, int r)
            {
                if (c < 0 || r < 0 || c >= W || r >= H) return;
                int i = r * W + c;
                if (i != last) { outCells.Add(i); last = i; }
            }
            for (int k = 0; k + 1 < pts.Count; k++)
            {
                double x0 = pts[k].x / cell, z0 = pts[k].z / cell, x1 = pts[k + 1].x / cell, z1 = pts[k + 1].z / cell;
                int c = (int)Math.Floor(x0), r = (int)Math.Floor(z0);
                int c1 = (int)Math.Floor(x1), r1 = (int)Math.Floor(z1);
                double dx = x1 - x0, dz = z1 - z0;
                int stepC = dx > 0 ? 1 : (dx < 0 ? -1 : 0), stepR = dz > 0 ? 1 : (dz < 0 ? -1 : 0);
                double tDeltaX = stepC != 0 ? Math.Abs(1.0 / dx) : double.PositiveInfinity;
                double tDeltaZ = stepR != 0 ? Math.Abs(1.0 / dz) : double.PositiveInfinity;
                double tMaxX = stepC > 0 ? (Math.Floor(x0) + 1 - x0) * tDeltaX : (stepC < 0 ? (x0 - Math.Floor(x0)) * tDeltaX : double.PositiveInfinity);
                double tMaxZ = stepR > 0 ? (Math.Floor(z0) + 1 - z0) * tDeltaZ : (stepR < 0 ? (z0 - Math.Floor(z0)) * tDeltaZ : double.PositiveInfinity);
                Add(c, r);
                int guard = Math.Abs(c1 - c) + Math.Abs(r1 - r) + 2;
                while ((c != c1 || r != r1) && guard-- > 0)
                {
                    if (tMaxX < tMaxZ) { tMaxX += tDeltaX; c += stepC; }
                    else { tMaxZ += tDeltaZ; r += stepR; }   // 모서리 정확히 지나도 한 축씩 → 4방향 연결 보장
                    Add(c, r);
                }
            }
            return outCells;
        }

        // ------------------------------------------------------------------ ③-2 경사 끊기
        /// <summary>이웃한 두 도로 칸 사이 경사 &gt; 상한이면 한 칸을 지운다(우선순위 낮은 쪽, 보호 칸 제외). 지운 칸 수.</summary>
        static int SteepCut(TownGrid g, TownGenSettings s, bool[] prot, TownReport rep)
        {
            int removed = 0;
            for (int guard = 0; guard < g.kind.Length; guard++)
            {
                bool any = false;
                for (int i = 0; i < g.kind.Length; i++)
                {
                    if (!g.IsRoad(i)) continue;
                    int c = g.Col(i), r = g.Row(i);
                    foreach (var j in new[] { c + 1 < g.W ? i + 1 : -1, r + 1 < g.H ? i + g.W : -1 })
                    {
                        if (j < 0 || !g.IsRoad(i) || !g.IsRoad(j) || g.Slope(i, j) <= s.maxSlope + 1e-9) continue;
                        int victim = PickVictim(g, i, j, prot);
                        if (victim < 0) { rep.Warn($"보호 칸 두 개 사이 경사 {g.Slope(i, j) * 100:0.#}% — 못 끊음 ({g.Col(i)},{g.Row(i)})"); continue; }
                        g.kind[victim] = CellKind.Empty; g.origin[victim] = CellOrigin.None;
                        removed++; any = true;
                    }
                }
                if (!any) break;
            }
            return removed;
        }

        static int PickVictim(TownGrid g, int a, int b, bool[] prot)
        {
            if (prot[a] && prot[b]) return -1;
            if (prot[a]) return b;
            if (prot[b]) return a;
            int ra = Rank(g, a), rb = Rank(g, b);
            if (ra != rb) return ra < rb ? a : b;
            int da = g.RoadDegree(a), db = g.RoadDegree(b);
            if (da != db) return da < db ? a : b;
            return Math.Max(a, b);
        }

        /// <summary>칸 i 를 도로로 만들었을 때 이웃 도로(또는 extra 집합)와 경사 상한을 지키는가.</summary>
        static bool AdjacencyOk(TownGrid g, TownGenSettings s, int i, ICollection<int> extra)
        {
            foreach (var j in g.Neighbors4(i))
                if ((g.IsRoad(j) || (extra != null && extra.Contains(j))) && g.Slope(i, j) > s.maxSlope + 1e-9) return false;
            return true;
        }

        // ------------------------------------------------------------------ ② 승격 (a)
        /// <summary>footway 가 서로 다른 도로 조각 2개 이상에 닿으면(= 승격 안 하면 고립) 승격. 길이 ≥ footwayMinCells.</summary>
        static int PromoteForConnectivity(TownGrid g, TownGenSettings s, List<KeyValuePair<int, List<int>>> footways, HashSet<int> promoted)
        {
            int count = 0;
            bool changed = true;
            while (changed)
            {
                changed = false;
                var comp = g.Components(out _);
                foreach (var fw in footways)
                {
                    if (promoted.Contains(fw.Key) || fw.Value.Count < s.footwayMinCells) continue;
                    if (fw.Value.Any(i => g.sea[i])) continue; // 바다를 지나는 보행로는 승격 안 함
                    var newCells = fw.Value.Where(i => !g.IsRoad(i)).Distinct().ToList();
                    if (newCells.Count == 0) continue;
                    var touched = new SortedSet<int>();
                    foreach (var i in fw.Value)
                    {
                        if (g.IsRoad(i)) touched.Add(comp[i]);
                        foreach (var j in g.Neighbors4(i)) if (g.IsRoad(j)) touched.Add(comp[j]);
                    }
                    if (touched.Count < 2) continue;
                    var set = new HashSet<int>(newCells);
                    if (!newCells.All(i => AdjacencyOk(g, s, i, set))) continue;
                    foreach (var i in newCells) { g.kind[i] = CellKind.Minor; g.origin[i] = CellOrigin.PromotedA; }
                    promoted.Add(fw.Key); count++; changed = true;
                    break; // 요소 번호 다시 계산
                }
            }
            return count;
        }

        // ------------------------------------------------------------------ 출발점
        /// <summary>항구 평지(항구 단계 이하) 안, 바다(남쪽 변)에서 startMinRowFromSea 칸 이상 떨어진 큰길 칸. 가장 큰 도로 조각 우선.</summary>
        static bool PickStart(TownGrid g, TownGenSettings s, TownReport rep)
        {
            var comp = g.Components(out var sizes);
            if (sizes.Count == 0) return false;
            int best = -1; long bestKey = long.MaxValue; string how = "";
            if (s.startCol >= 0 && s.startRow >= 0)
            {
                if (g.IsRoad(s.startCol, s.startRow)) { best = g.Index(s.startCol, s.startRow); how = "직접 지정"; }
                else rep.Warn($"지정 출발점 ({s.startCol},{s.startRow}) 이 도로가 아님 — 자동 선택");
            }
            for (int pass = 0; pass < 3 && best < 0; pass++)
            {
                for (int i = 0; i < g.kind.Length; i++)
                {
                    if (!g.IsRoad(i)) continue;
                    int r = g.Row(i), c = g.Col(i);
                    bool major = g.kind[i] == CellKind.Major, harbor = g.height[i] <= s.harborMaxHeightMeters, offSea = r >= s.startMinRowFromSea;
                    bool ok = pass == 0 ? (major && harbor && offSea) : pass == 1 ? (harbor && offSea) : true;
                    if (!ok) continue;
                    // 키: 조각 크기 큰 순 → 남쪽 행 → 가운데 열에 가까움 → 칸 번호
                    long key = ((long)(int.MaxValue - sizes[comp[i]]) << 32) | ((long)r << 20) | ((long)Math.Abs(c - g.W / 2) << 10) | (uint)c;
                    if (key < bestKey) { bestKey = key; best = i; how = pass == 0 ? "큰길·항구·바다 2칸+" : pass == 1 ? "골목 대체(항구 큰길 없음)" : "아무 도로(항구 조건 불충족)"; }
                }
            }
            if (best < 0) return false;
            g.start = best;
            if (how != "큰길·항구·바다 2칸+" && how != "직접 지정") rep.Warn("출발점 조건 완화: " + how);
            // 진행 방향: 북쪽 도로 이웃 우선, 아니면 첫 도로 이웃
            int sc = g.Col(best), sr = g.Row(best);
            int[] dc = { 0, 1, -1, 0 }, dr = { 1, 0, 0, -1 };
            for (int d = 0; d < 4; d++)
                if (g.IsRoad(sc + dc[d], sr + dr[d])) { g.startYawDeg = (float)(Math.Atan2(dc[d], dr[d]) * 180 / Math.PI); break; }
            rep.Metric("start", $"({sc},{sr}) h={g.height[best]:0.0}m yaw={g.startYawDeg:0} [{how}]");
            return true;
        }

        // ------------------------------------------------------------------ ③-3 막다른 가지
        static int PruneDeadEnds(TownGrid g, TownGenSettings s, bool[] prot)
        {
            int removed = 0;
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < g.kind.Length; i++)
                {
                    if (!g.IsRoad(i) || prot[i] || g.RoadDegree(i) != 1) continue;
                    // 끝 칸에서 사슬을 따라가다 교차로(이웃 3+)를 만나면, 그때까지 칸 수가 가지 길이.
                    var path = new List<int> { i };
                    int prev = -1, cur = i;
                    bool reachedJunction = false;
                    while (true)
                    {
                        int next = -1;
                        foreach (var j in g.Neighbors4(cur)) if (g.IsRoad(j) && j != prev) { next = j; break; }
                        if (next < 0) break;
                        if (g.RoadDegree(next) >= 3) { reachedJunction = true; break; }
                        if (path.Count >= s.minDeadEndCells) break;           // 충분히 긴 가지 — 남긴다
                        if (prot[next] || g.RoadDegree(next) != 2) break;     // 보호 칸 / 반대쪽도 끝(고립 조각) — 이 규칙 대상 아님
                        prev = cur; cur = next; path.Add(cur);
                    }
                    if (reachedJunction && path.Count < s.minDeadEndCells)
                    {
                        foreach (var p in path) { g.kind[p] = CellKind.Empty; g.origin[p] = CellOrigin.None; removed++; }
                        changed = true;
                    }
                }
            }
            return removed;
        }

        // ------------------------------------------------------------------ ③-4 평행선 합치기
        /// <summary>붙어서 2칸 폭이 된 띠(2×2 도로 블록)를 1칸으로 — 지워도 주변 연결이 유지되는 칸만 지운다.</summary>
        static int ThinParallel(TownGrid g, bool[] prot)
        {
            int removed = 0;
            for (int rankPass = 1; rankPass <= 4; rankPass++)
            {
                bool changed = true;
                while (changed)
                {
                    changed = false;
                    for (int i = 0; i < g.kind.Length; i++)
                    {
                        if (!g.IsRoad(i) || prot[i] || Rank(g, i) != rankPass || !In2x2(g, i)) continue;
                        if (!SafeToRemove(g, i)) continue;
                        g.kind[i] = CellKind.Empty; g.origin[i] = CellOrigin.None; removed++; changed = true;
                    }
                }
            }
            return removed;
        }

        static bool In2x2(TownGrid g, int i)
        {
            int c = g.Col(i), r = g.Row(i);
            for (int oc = -1; oc <= 0; oc++)
                for (int or = -1; or <= 0; or++)
                    if (g.IsRoad(c + oc, r + or) && g.IsRoad(c + oc + 1, r + or) && g.IsRoad(c + oc, r + or + 1) && g.IsRoad(c + oc + 1, r + or + 1)) return true;
            return false;
        }

        /// <summary>3×3 창 안에서, 칸 i 를 빼도 i 의 도로 이웃들이 서로 4방향으로 이어져 있는가 (단순점 판정).</summary>
        static bool SafeToRemove(TownGrid g, int i)
        {
            int c = g.Col(i), r = g.Row(i);
            var nbrs = new List<(int, int)>();
            if (g.IsRoad(c, r + 1)) nbrs.Add((c, r + 1));
            if (g.IsRoad(c + 1, r)) nbrs.Add((c + 1, r));
            if (g.IsRoad(c, r - 1)) nbrs.Add((c, r - 1));
            if (g.IsRoad(c - 1, r)) nbrs.Add((c - 1, r));
            if (nbrs.Count <= 1) return false; // 끝 칸은 막다른 가지 규칙 몫
            var seen = new HashSet<(int, int)> { nbrs[0] };
            var q = new Queue<(int, int)>(); q.Enqueue(nbrs[0]);
            while (q.Count > 0)
            {
                var (x, z) = q.Dequeue();
                foreach (var (dx, dz) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
                {
                    int nx = x + dx, nz = z + dz;
                    if (Math.Abs(nx - c) > 1 || Math.Abs(nz - r) > 1 || (nx == c && nz == r)) continue;
                    if (!g.IsRoad(nx, nz) || !seen.Add((nx, nz))) continue;
                    q.Enqueue((nx, nz));
                }
            }
            return nbrs.All(seen.Contains);
        }

        // ------------------------------------------------------------------ ③-5 A* 잇기
        /// <summary>출발점 조각이 아닌 조각마다, 출발점 조각까지 경사 비용 최단 경로(다중 출발 Dijkstra)로 잇는다. 새로 깐 칸 수.</summary>
        static int Reconnect(TownGrid g, TownGenSettings s, TownReport rep)
        {
            int added = 0;
            for (int guard = 0; guard < 64; guard++)
            {
                var comp = g.Components(out var sizes);
                int main = comp[g.start];
                if (sizes.Count <= 1) break;
                // 큰 조각부터
                int pick = -1;
                for (int k = 0; k < sizes.Count; k++)
                    if (k != main && (pick < 0 || sizes[k] > sizes[pick])) pick = k;
                var path = Dijkstra(g, s, comp, pick, main);
                if (path == null)
                {
                    rep.Warn($"조각 {sizes[pick]}칸: 경사 {s.maxSlope * 100:0}% 안에서 잇는 길 없음 — 버림 대상");
                    // 이 조각은 버린다(무한 반복 방지)
                    for (int i = 0; i < comp.Length; i++) if (comp[i] == pick) { g.kind[i] = CellKind.Empty; g.origin[i] = CellOrigin.None; }
                    continue;
                }
                foreach (var i in path)
                    if (!g.IsRoad(i)) { g.kind[i] = CellKind.Minor; g.origin[i] = CellOrigin.Connector; added++; }
            }
            return added;
        }

        static List<int> Dijkstra(TownGrid g, TownGenSettings s, int[] comp, int from, int to)
        {
            int n = g.kind.Length;
            var dist = new double[n];
            var prev = new int[n];
            for (int i = 0; i < n; i++) { dist[i] = double.PositiveInfinity; prev[i] = -1; }
            var pq = new SortedSet<(double, int)>();
            for (int i = 0; i < n; i++)
                if (comp[i] == from) { dist[i] = 0; pq.Add((0, i)); }
            while (pq.Count > 0)
            {
                var top = pq.Min; pq.Remove(top);
                int u = top.Item2;
                if (comp[u] == to)
                {
                    var path = new List<int>();
                    for (int v = u; v >= 0; v = prev[v]) path.Add(v);
                    path.Reverse();
                    return path;
                }
                foreach (var v in g.Neighbors4(u))
                {
                    double sl = g.Slope(u, v);
                    if (sl > s.maxSlope + 1e-9) continue;
                    // 빈칸에 새로 깔 때: 그 칸이 다른 도로 칸과 가파르게 붙으면 안 됨(다시 끊기는 것 방지)
                    if (g.sea[v]) continue;                               // 바다 칸으로 잇지 않음 (D-110)
                    if (!g.IsRoad(v) && !AdjacencyOk(g, s, v, null)) continue;
                    double nd = dist[u] + s.cellSizeMeters * (1 + s.aStarSlopeK * sl * sl) * (g.IsRoad(v) ? 0.5 : 1.0);
                    if (nd < dist[v] - 1e-9)
                    {
                        if (!double.IsPositiveInfinity(dist[v])) pq.Remove((dist[v], v));
                        dist[v] = nd; prev[v] = u; pq.Add((nd, v));
                    }
                }
            }
            return null;
        }

        static int DropNonMain(TownGrid g)
        {
            var comp = g.Components(out _);
            int main = comp[g.start], dropped = 0;
            for (int i = 0; i < comp.Length; i++)
                if (comp[i] >= 0 && comp[i] != main) { g.kind[i] = CellKind.Empty; g.origin[i] = CellOrigin.None; dropped++; }
            return dropped;
        }

        // ------------------------------------------------------------------ ⑦ 배달지
        static void PlaceDeliveries(TownGrid g, TownGenSettings s, TownReport rep)
        {
            var dist = g.BfsFrom(g.start, out _);
            var cands = new List<int>();
            for (int i = 0; i < dist.Length; i++) if (dist[i] > 0) cands.Add(i);
            cands.Sort((a, b) => dist[a] != dist[b] ? dist[a].CompareTo(dist[b]) : a.CompareTo(b));
            double M(int i) => dist[i] * s.cellSizeMeters;

            var chosen = new List<int>();
            bool Spaced(int i)
            {
                foreach (var j in chosen)
                    if (Math.Max(Math.Abs(g.Col(i) - g.Col(j)), Math.Abs(g.Row(i) - g.Row(j))) < s.deliveryMinSpacingCells) return false;
                return true;
            }

            var picks = new Dictionary<DistanceBand, List<(int cell, bool outOfBand)>>();
            var order = new[] { (DistanceBand.Far, s.farCount, s.midMaxMeters, double.MaxValue),
                                (DistanceBand.Mid, s.midCount, s.nearMaxMeters, (double)s.midMaxMeters),
                                (DistanceBand.Near, s.nearCount, s.nearMinMeters, (double)s.nearMaxMeters) };
            foreach (var (band, count, lo, hi) in order)
            {
                var list = cands.Where(i => M(i) >= lo && M(i) < hi).ToList();
                var got = new List<(int, bool)>();
                for (int k = 0; k < count; k++)
                {
                    int found = -1;
                    if (list.Count > 0)
                    {
                        int t = Math.Min(list.Count - 1, (int)((k + 0.5) / count * list.Count));
                        for (int off = 0; off < list.Count && found < 0; off++)
                            foreach (var idx in new[] { t + off, t - off })
                                if (idx >= 0 && idx < list.Count && !chosen.Contains(list[idx]) && Spaced(list[idx])) { found = list[idx]; break; }
                    }
                    bool oob = false;
                    if (found < 0)
                    {
                        // 구간 안에 자리가 없으면 구간 경계에 가장 가까운 남은 칸
                        double target = band == DistanceBand.Far ? lo : band == DistanceBand.Near ? Math.Max(lo, 0) : (lo + hi) / 2;
                        foreach (var i in cands.OrderBy(i => Math.Abs(M(i) - target)).ThenBy(i => i))
                            if (!chosen.Contains(i) && Spaced(i)) { found = i; oob = true; break; }
                    }
                    if (found < 0) { rep.Fail("A2", $"배달지 자리 부족 ({band} {k + 1}/{count})"); continue; }
                    chosen.Add(found); got.Add((found, oob));
                }
                got.Sort((a, b) => dist[a.Item1].CompareTo(dist[b.Item1]));
                picks[band] = got;
            }

            // 라벨: 고정 3개는 구간 가운데 자리, 나머지는 입력 순서대로 근→중→원
            var labels = s.deliveryLabels ?? new string[0];
            var fixedByBand = new Dictionary<DistanceBand, string> { { DistanceBand.Near, s.nearFixedLabel }, { DistanceBand.Mid, s.midFixedLabel }, { DistanceBand.Far, s.farFixedLabel } };
            var others = new Queue<string>(labels.Where(l => !fixedByBand.ContainsValue(l)));
            foreach (var band in new[] { DistanceBand.Near, DistanceBand.Mid, DistanceBand.Far })
            {
                if (!picks.TryGetValue(band, out var list)) continue;
                int mid = list.Count / 2;
                for (int k = 0; k < list.Count; k++)
                {
                    string label = k == mid && labels.Contains(fixedByBand[band]) ? fixedByBand[band] : (others.Count > 0 ? others.Dequeue() : $"Extra{g.deliveries.Count}");
                    var (cell, oob) = list[k];
                    g.deliveries.Add(new Delivery { label = label, cell = cell, col = g.Col(cell), row = g.Row(cell), distanceMeters = M(cell), band = band, outOfBand = oob });
                }
            }
            int oobCount = g.deliveries.Count(d => d.outOfBand);
            if (oobCount > 0) rep.Warn($"배달지 {oobCount}곳이 목표 거리 구간 밖에서 뽑힘 (맵이 그 구간을 못 채움)");
        }

        // ------------------------------------------------------------------ 고도 단계 + 랜드마크
        static void AssignTiers(TownGrid g, TownGenSettings s, TownReport rep)
        {
            double hmax = double.MinValue;
            for (int i = 0; i < g.kind.Length; i++) if (g.IsRoad(i)) hmax = Math.Max(hmax, g.height[i]);
            double upper = s.harborMaxHeightMeters + Math.Max(0, hmax - s.harborMaxHeightMeters) * s.upperTierFraction;
            for (int i = 0; i < g.kind.Length; i++)
            {
                double h = g.height[i];
                g.tier[i] = h <= s.harborMaxHeightMeters ? 0 : (h <= upper ? 1 : 2);
            }
            rep.Metric("tiers", $"0: ≤{s.harborMaxHeightMeters:0.#}m · 1: ≤{upper:0.#}m · 2: 그 위 (도로 최고 {hmax:0.#}m)");
        }

        /// <summary>D-100 ③: 위 마을 단계 최고 도로 칸 옆(1칸) 빈칸에 랜드마크 자리. 위 마을 단계가 없으면(평지) 대상 아님.</summary>
        static void PlaceLandmark(TownGrid g, RoadNetwork net, TownReport rep)
        {
            int top = -1;
            for (int i = 0; i < g.kind.Length; i++)
                if (g.IsRoad(i) && g.tier[i] == 2 && (top < 0 || g.height[i] > g.height[top])) top = i;
            if (top < 0) { rep.Check("D100-3", true, "위 마을 단계(2) 도로 없음 — 평지라 대상 아님"); return; }
            int c = g.Col(top), r = g.Row(top);
            int slot = -1;
            foreach (var (dc, dr) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0), (1, 1), (-1, 1), (1, -1), (-1, -1) })
            {
                int nc = c + dc, nr = r + dr;
                if (!g.InBounds(nc, nr)) continue;
                int j = g.Index(nc, nr);
                if (g.IsRoad(j) || g.stepsDecor[j]) continue;
                slot = j; break;
            }
            if (slot >= 0) g.landmarks.Add(slot);
            rep.Check("D100-3", slot >= 0, slot >= 0
                ? $"꼭대기 도로 ({c},{r}) h={g.height[top]:0.0}m 옆 ({g.Col(slot)},{g.Row(slot)}) 에 랜드마크 자리"
                : $"꼭대기 도로 ({c},{r}) 주변 1칸에 빈칸 없음");
            if (net.landmarksHint.Count > 0) rep.Metric("landmarks_hint", $"{net.landmarksHint.Count}개 받음 — 이번 판은 미사용(꼭대기 규칙 우선)");
        }

        // ------------------------------------------------------------------ ⑥ D-100 ② 우회로 (+ 승격 b)
        /// <summary>
        /// 배달지마다 출발점→배달지 최단 도로 경로에서 경사 &gt; hillSlope 인 연속 구간을 찾고,
        /// 그 구간 양 끝을 모든 연결 ≤ flatSlope 로 잇는 길(길이 ≤ 구간 × detourMaxRatio)이 있는지 본다.
        /// 도로만으로 없으면 footway 후보 칸까지 써서 찾고, 찾으면 쓴 칸을 승격(b).
        /// 양 끝 높이차 때문에 평지 우회가 원리적으로 불가능한 구간(= 오르막 자체)은 "오르막"으로 따로 센다.
        /// </summary>
        static int CheckDetours(TownGrid g, TownGenSettings s, List<KeyValuePair<int, List<int>>> footways, HashSet<int> promoted, TownReport rep)
        {
            var dist = g.BfsFrom(g.start, out var parent);
            var candidateOwner = new Dictionary<int, int>(); // 빈칸 → footway id
            foreach (var fw in footways)
            {
                if (promoted.Contains(fw.Key) || fw.Value.Count < s.footwayMinCells) continue;
                foreach (var i in fw.Value) if (!g.IsRoad(i) && !candidateOwner.ContainsKey(i)) candidateOwner[i] = fw.Key;
            }
            var seenRuns = new HashSet<(int, int)>();
            int hills = 0, ok = 0, fail = 0, climbs = 0, promotedSegs = 0;
            var failList = new List<string>();
            foreach (var d in g.deliveries)
            {
                if (dist[d.cell] < 0) continue;
                var path = new List<int>();
                for (int v = d.cell; v >= 0; v = parent[v]) path.Add(v);
                path.Reverse();
                // 언덕 구간 = 가파른 연결(> hillSlope) 묶음. 사이 평지 틈이 hillMergeGapCells 이하면 한 묶음
                // (혹 = 올라가 꼭대기 잠깐 평평 → 내려옴 → 한 구간). u = 묶음 직전 칸, v = 묶음 직후 칸.
                // u·v 높이차가 평지 우회로(≤ flatSlope, 길이 ≤ 예산)로 원리상 못 메우면 = 오르막(진행 단계, D-099) → 따로 센다.
                // 양 끝은 평지(≤ flatSlope)가 시작되는 곳까지 넓힌다 — 언덕 어깨(2~5%)에 걸린 끝점에선 평지 우회로가 원리상 못 붙는다.
                bool Steep(int e) => g.Slope(path[e], path[e + 1]) > s.hillSlope + 1e-9;
                bool NotFlat(int e) => g.Slope(path[e], path[e + 1]) > s.flatSlope + 1e-9;
                int k = 0;
                while (k + 1 < path.Count)
                {
                    if (!Steep(k)) { k++; continue; }
                    int a = k, last = k;
                    for (int e = k + 1; e + 1 < path.Count && e - last - 1 <= s.hillMergeGapCells; e++)
                        if (Steep(e)) last = e;
                    while (a > 0 && NotFlat(a - 1)) a--;
                    while (last + 2 < path.Count && NotFlat(last + 1)) last++;
                    k = last + 1;
                    int u = path[a], v = path[last + 1], runLen = last + 1 - a;
                    bool toEnd = last + 1 == path.Count - 1;          // 배달지에서 끝남 → 배달지마다 다른 v. 시작 칸으로만 중복 판정
                    if (!seenRuns.Add((u, toEnd ? -1 : v))) continue;
                    hills++;
                    int budget = (int)Math.Floor(runLen * s.detourMaxRatio);
                    double minFlatLen = Math.Abs(g.height[u] - g.height[v]) / (s.flatSlope * s.cellSizeMeters);
                    if (minFlatLen > budget + 1e-9) { climbs++; continue; }
                    var det = FlatPath(g, s, u, v, budget, null);
                    if (det != null) { ok++; continue; }
                    det = FlatPath(g, s, u, v, budget, candidateOwner);
                    if (det != null)
                    {
                        var used = new SortedSet<int>();
                        foreach (var i in det)
                            if (!g.IsRoad(i) && candidateOwner.TryGetValue(i, out var owner)) { g.kind[i] = CellKind.Minor; g.origin[i] = CellOrigin.PromotedB; used.Add(owner); }
                        foreach (var id in used) if (promoted.Add(id)) promotedSegs++;
                        ok++; continue;
                    }
                    fail++;
                    failList.Add($"{d.label}:({g.Col(u)},{g.Row(u)})→({g.Col(v)},{g.Row(v)}) {runLen}칸");
                }
            }
            rep.Metric("d100_2_hill_runs", $"언덕 구간 {hills} = 우회 있음 {ok} + 우회 없음 {fail} + 오르막(평지 우회 원리상 불가) {climbs}");
            rep.Check("D100-2", fail == 0, fail == 0 ? $"우회 없는 언덕 구간 0 (오르막 {climbs} 별도)" : $"우회 없는 언덕 구간 {fail}: " + string.Join(", ", failList.Take(8)));
            return promotedSegs;
        }

        /// <summary>u→v, 모든 연결 ≤ flatSlope, 길이(연결 수) ≤ budget 인 BFS 경로. candidates 가 있으면 그 빈칸도 지나갈 수 있음.</summary>
        static List<int> FlatPath(TownGrid g, TownGenSettings s, int u, int v, int budget, Dictionary<int, int> candidates)
        {
            var dist = new Dictionary<int, int> { { u, 0 } };
            var prev = new Dictionary<int, int>();
            var q = new Queue<int>(); q.Enqueue(u);
            while (q.Count > 0)
            {
                int x = q.Dequeue();
                if (x == v) break;
                if (dist[x] >= budget) continue;
                foreach (var y in g.Neighbors4(x))
                {
                    bool passable = g.IsRoad(y) || (candidates != null && candidates.ContainsKey(y) && !g.sea[y] && AdjacencyOk(g, s, y, null));
                    if (!passable || dist.ContainsKey(y) || g.Slope(x, y) > s.flatSlope + 1e-9) continue;
                    dist[y] = dist[x] + 1; prev[y] = x; q.Enqueue(y);
                }
            }
            if (!dist.ContainsKey(v)) return null;
            var path = new List<int>();
            for (int x = v; x != u; x = prev[x]) path.Add(x);
            path.Add(u); path.Reverse();
            return path;
        }

        // ------------------------------------------------------------------ 수용 기준 숫자
        static void CheckD100Slope(TownGrid g, TownGenSettings s, TownReport rep)
        {
            int bad = 0; double worst = 0;
            for (int i = 0; i < g.kind.Length; i++)
            {
                if (!g.IsRoad(i)) continue;
                int c = g.Col(i), r = g.Row(i);
                foreach (var j in new[] { c + 1 < g.W ? i + 1 : -1, r + 1 < g.H ? i + g.W : -1 })
                {
                    if (j < 0 || !g.IsRoad(j)) continue;
                    double sl = g.Slope(i, j); worst = Math.Max(worst, sl);
                    if (sl > s.maxSlope + 1e-9) bad++;
                }
            }
            rep.Check("D100-1", bad == 0, $"도로 칸 연결 최대 경사 {worst * 100:0.0}% (상한 {s.maxSlope * 100:0}%), 초과 {bad}");
        }

        static void CheckConnectivity(TownGrid g, TownReport rep)
        {
            var dist = g.BfsFrom(g.start, out _);
            int total = g.CountRoad(), reach = 0;
            for (int i = 0; i < dist.Length; i++) if (g.IsRoad(i) && dist[i] >= 0) reach++;
            rep.Check("A1", reach == total && total > 0, $"도로 칸 {total}, 출발점에서 도달 {reach}, 고립 {total - reach}");
        }

        static void CheckDeliveries(TownGrid g, TownGenSettings s, TownReport rep)
        {
            var dist = g.BfsFrom(g.start, out _);
            int onRoad = g.deliveries.Count(d => g.IsRoad(d.cell) && dist[d.cell] >= 0);
            int minSpace = int.MaxValue;
            for (int a = 0; a < g.deliveries.Count; a++)
                for (int b = a + 1; b < g.deliveries.Count; b++)
                    minSpace = Math.Min(minSpace, Math.Max(Math.Abs(g.deliveries[a].col - g.deliveries[b].col), Math.Abs(g.deliveries[a].row - g.deliveries[b].row)));
            int want = s.nearCount + s.midCount + s.farCount;
            bool a2 = g.deliveries.Count == want && onRoad == want && minSpace >= s.deliveryMinSpacingCells;
            rep.Check("A2", a2, $"배달지 {g.deliveries.Count}/{want}, 도로·도달 {onRoad}, 최소 간격 {(minSpace == int.MaxValue ? 0 : minSpace)}칸 (≥{s.deliveryMinSpacingCells})");

            var ds = g.deliveries.Select(d => d.distanceMeters).OrderBy(x => x).ToList();
            if (ds.Count == 0) { rep.Check("A3", false, "배달지 없음"); return; }
            double min = ds[0], max = ds[ds.Count - 1];
            double med = ds.Count % 2 == 1 ? ds[ds.Count / 2] : (ds[ds.Count / 2 - 1] + ds[ds.Count / 2]) / 2;
            bool a3 = min >= 80 && med >= 200 && med <= 350 && max >= 450;
            rep.Check("A3", a3, $"[가설] 최소 {min:0}m (≥80) · 중앙값 {med:0}m (200~350) · 최대 {max:0}m (≥450)");
            var sb = new StringBuilder();
            foreach (var d in g.deliveries.OrderBy(x => x.distanceMeters))
                sb.Append($"{d.label}={d.distanceMeters:0}m/{d.band.ToString().ToLowerInvariant()}{(d.outOfBand ? "*" : "")} ");
            rep.Metric("deliveries", sb.ToString().Trim());
            var fixedOk = g.deliveries.Any(d => d.label == s.farFixedLabel && d.band == DistanceBand.Far)
                && g.deliveries.Any(d => d.label == s.midFixedLabel && d.band == DistanceBand.Mid)
                && g.deliveries.Any(d => d.label == s.nearFixedLabel && d.band == DistanceBand.Near);
            rep.Check("A2-fixed-bands", fixedOk, $"{s.nearFixedLabel}=가까움 · {s.midFixedLabel}=중간 · {s.farFixedLabel}=멂 고정 라벨");
        }

        static void Shape(TownGrid g, TownReport rep)
        {
            int major = 0, minor = 0, deadEnds = 0, junctions = 0;
            for (int i = 0; i < g.kind.Length; i++)
            {
                if (!g.IsRoad(i)) continue;
                if (g.kind[i] == CellKind.Major) major++; else minor++;
                int deg = g.RoadDegree(i);
                if (deg == 1) deadEnds++;
                if (deg >= 3) junctions++;
            }
            int steps = g.stepsDecor.Count(b => b);
            rep.Metric("road_cells", $"{major + minor} (큰길 {major} / 골목 {minor}, 큰길 비율 {(major + minor == 0 ? 0 : 100.0 * major / (major + minor)):0}%)");
            rep.Metric("dead_ends", deadEnds.ToString(CultureInfo.InvariantCulture));
            rep.Metric("junction_cells", junctions.ToString(CultureInfo.InvariantCulture));
            rep.Metric("steps_decor_cells", steps.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>뒷단 보고 — 검사(통과/실패) + 지표 + 경고. 순서 보존.</summary>
    public class TownReport
    {
        public string source; public long seed;
        public readonly List<(string id, bool pass, string detail)> checks = new List<(string, bool, string)>();
        public readonly List<(string key, string value)> metrics = new List<(string, string)>();
        public readonly List<string> warnings = new List<string>();

        public void Check(string id, bool pass, string detail) => checks.Add((id, pass, detail));
        public void Fail(string id, string detail) => checks.Add((id, false, detail));
        public void Metric(string key, string value) => metrics.Add((key, value));
        public void Warn(string w) => warnings.Add(w);
        public bool AllPass => checks.All(c => c.pass);
        public string MetricValue(string key) => metrics.LastOrDefault(m => m.key == key).value;

        public string ToText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[TownGen] source={source} seed={seed} — 검사 {checks.Count(c => c.pass)}/{checks.Count} 통과");
            foreach (var c in checks) sb.AppendLine($"  {(c.pass ? "PASS" : "FAIL")} {c.id}: {c.detail}");
            foreach (var m in metrics) sb.AppendLine($"  · {m.key}: {m.value}");
            foreach (var w in warnings) sb.AppendLine($"  ⚠ {w}");
            return sb.ToString();
        }

        public JObject ToJson()
        {
            var cs = new JArray();
            foreach (var c in checks) cs.Add(new JObject { ["id"] = c.id, ["pass"] = c.pass, ["detail"] = c.detail });
            var ms = new JObject();
            foreach (var m in metrics) ms[m.key] = m.value;
            return new JObject { ["source"] = source, ["seed"] = seed, ["all_pass"] = AllPass, ["checks"] = cs, ["metrics"] = ms, ["warnings"] = new JArray(warnings) };
        }
    }
}
