using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PitCrew.TownGen.Tests
{
    /// <summary>곡선 큰길(스펙 2026-10-20-spline-town) 코어 — 엔진 없이.</summary>
    public class SplineTownTests
    {
        static OsmWay W(string hw, params double[] xz)
        {
            var w = new OsmWay { highway = hw };
            for (int i = 0; i < xz.Length; i += 2) w.points.Add(new Vec2(xz[i], xz[i + 1]));
            return w;
        }

        static SplineTownSettings S()
        {
            var s = new SplineTownSettings { boundsXMeters = 240, boundsZMeters = 200, heightScale = 1f, seaLevelMeters = -1e6f, seamZMin = -1, seamZMax = -1 };
            return s;
        }

        static (bool pass, string detail) Check(SplineTownResult r, string id)
        {
            var c = r.report.checks.LastOrDefault(x => x.id == id);
            Assert.True(c.id != null, $"검사 {id} 없음\n{r.report.ToText()}");
            return (c.pass, c.detail);
        }

        [Fact]
        public void Selection_main_tags_and_long_residential_touching_main_footway_dropped()
        {
            var ways = new List<OsmWay>
            {
                W("primary", 10, 100, 60, 100, 120, 100, 230, 100),   // 실제 OSM 처럼 교차 지점이 같은 노드
                W("residential", 120, 100, 120, 190),     // 90m, 큰길에 닿음 → 큰길
                W("residential", 60, 100, 60, 140),       // 40m → 큰길 아님(골목 후보)
                W("footway", 200, 100, 200, 180),          // 버림
            };
            var r = SplineTown.Build(ways, new FlatHeightField(5), S());
            double total = r.Mains.Sum(e => e.Length);
            Assert.InRange(total, 300, 315);               // 220 + 90 (곡선 다듬기 오차 조금)
            var alley = r.Alleys.Single();                 // 40m 주거 도로 → 골목(큰길 쪽 끝에서 시작)
            Assert.InRange(alley.Length, 30, 41);
            Assert.Contains("1개", r.report.MetricValue("alley_candidates"));
            Assert.True(Check(r, "M6").pass, r.report.ToText());
        }

        [Fact]
        public void Isolated_long_piece_connected_short_piece_dropped()
        {
            var ways = new List<OsmWay>
            {
                W("primary", 10, 50, 230, 50),
                W("primary", 10, 150, 230, 150),          // 220m 떨어진 조각 → 잇기
                W("primary", 100, 100, 150, 100),         // 50m 조각 (< 80) → 버림
            };
            var r = SplineTown.Build(ways, new FlatHeightField(5), S());
            Assert.True(Check(r, "M6").pass, r.report.ToText());
            Assert.StartsWith("1개", r.report.MetricValue("connectors"));
            Assert.StartsWith("1개", r.report.MetricValue("dropped_isolated"));
            Assert.Single(r.edges.Where(e => e.connector));
            Assert.True(r.edges.Sum(e => e.Length) > 400, r.report.ToText());   // 두 줄(220+220)이 모두 남음 = 실제로 이어짐
        }

        [Fact]
        public void Connector_winds_on_steep_slope_instead_of_going_straight_up()
        {
            // 아래 줄(z=40)과 위 줄(z=180) 사이가 30% 비탈 — 곧장 오르면 140m 직선, 8% 로 오르려면 굽이 길이 훨씬 길어야 한다.
            var ways = new List<OsmWay> { W("primary", 10, 40, 230, 40), W("primary", 10, 180, 230, 180) };
            var s = S(); s.maxCutFillMeters = 100;
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => Math.Max(0, Math.Min(140, z - 40)) * 0.3), s);
            var c = r.edges.Single(e => e.connector);
            Assert.True(c.Length > 140 * 1.8, $"잇기 {c.Length:0}m " + r.report.ToText());
        }

        [Fact]
        public void Grade_clamped_to_8_percent_and_cut_fill_reported()
        {
            // 남→북 20% 오르막 위를 곧게 가는 길: 종단은 8% 로 잘리고, 그만큼 깎기·메우기가 생긴다.
            var ways = new List<OsmWay> { W("primary", 120, 10, 120, 190) };
            var s = S(); s.maxCutFillMeters = 100;
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => z * 0.2), s);
            Assert.True(Check(r, "M2").pass, r.report.ToText());
            var e = r.edges.Single();
            for (int i = 1; i < e.pts.Count - 1; i++)
                Assert.True(Math.Abs(e.y[i] - e.y[i - 1]) / SplineEdge.Dist(e.pts[i - 1], e.pts[i]) <= 0.0801);
        }

        [Fact]
        public void Road_section_is_flush_and_terrain_never_pokes_through_on_side_slope()
        {
            // 동서로 가는 길, 지형은 북쪽으로 10% 기울어짐(옆 경사). 길 밑을 깎고 메워 평평하게.
            var ways = new List<OsmWay> { W("primary", 10, 100, 230, 100) };
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => (z - 100) * 0.1 + 20), S());
            Assert.True(Check(r, "M1").pass, r.report.ToText());
            Assert.True(Check(r, "M1-buried").pass, r.report.ToText());
            Assert.True(Check(r, "M7").pass, r.report.ToText());
            // 옆 경사 10% × 어깨 6m = 0.6m — 옹벽 기준(1.5m) 아래라 옹벽 없음
            Assert.Empty(r.walls);
        }

        [Fact]
        public void Steep_side_slope_gets_retaining_walls()
        {
            var ways = new List<OsmWay> { W("primary", 10, 100, 230, 100) };
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => (z - 100) * 0.4 + 40), S());
            Assert.NotEmpty(r.walls);                       // 40% × 6m = 2.4m > 1.5m
            Assert.True(Check(r, "M1-buried").pass, r.report.ToText());
        }

        [Fact]
        public void Sharp_corner_is_smoothed_to_min_radius()
        {
            var ways = new List<OsmWay> { W("primary", 20, 100, 120, 100, 120, 190) }; // 직각
            var r = SplineTown.Build(ways, new FlatHeightField(5), S());
            Assert.True(Check(r, "M3").pass, r.report.ToText());
        }

        [Fact]
        public void Roads_never_on_sea()
        {
            var ways = new List<OsmWay> { W("primary", 120, 2, 120, 190), W("primary", 10, 4, 230, 4) };
            var s = S(); s.seaLevelMeters = 0.15f;
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => z < 24 ? 0 : 5), s);
            Assert.True(Check(r, "M5").pass, r.report.ToText());
            Assert.All(r.edges, e => Assert.All(e.pts, p => Assert.True(p.z > 16)));
        }

        [Fact]
        public void Deterministic_hash()
        {
            var ways = new List<OsmWay> { W("primary", 20, 100, 120, 100, 120, 190), W("residential", 120, 140, 220, 160) };
            var hf = new FuncHeightField((x, z) => Math.Sin(x / 30) * 4 + z * 0.05);
            var a = SplineTown.Build(ways, hf, S());
            var b = SplineTown.Build(ways, hf, S());
            Assert.Equal(a.report.MetricValue("hash"), b.report.MetricValue("hash"));
        }
            [Fact]
        public void Houses_line_both_sides_never_on_road_never_overlapping_and_capped()
        {
            var ways = new List<OsmWay> { W("primary", 10, 100, 230, 100) };
            var s = S(); s.houseCap = 1000;
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => 5 + (z - 100) * 0.05), s);
            Assert.True(Check(r, "M4-overlap").pass, r.report.ToText());
            Assert.True(r.houses.Count >= 30, $"집 {r.houses.Count}\n" + r.report.ToText());   // 220m × 양쪽 / 필지 ~11m ≈ 40
            Assert.Contains(r.houses, h => h.center.z > 100);
            Assert.Contains(r.houses, h => h.center.z < 100);
            for (int a = 0; a < r.houses.Count; a++)
                for (int b = a + 1; b < r.houses.Count; b++)
                    Assert.False(SplineTown.RectOverlap(r.houses[a].Corners(), r.houses[b].Corners()));
            // 집 정면이 길을 본다: 북쪽 줄은 남쪽(yaw 180), 남쪽 줄은 북쪽(yaw 0)
            Assert.All(r.houses.Where(h => h.center.z > 100), h => Assert.InRange(Math.Abs(Math.Abs(h.yawDeg) - 180), 0, 5));
            Assert.All(r.houses.Where(h => h.center.z < 100), h => Assert.InRange(Math.Abs(h.yawDeg), 0, 5));
            var s2 = S(); s2.houseCap = 10;
            var r2 = SplineTown.Build(ways, new FlatHeightField(5), s2);
            Assert.Equal(10, r2.houses.Count);
        }

        [Fact]
        public void Steep_plot_moves_back_a_row_or_is_dropped_and_plinth_never_exceeds_limit()
        {
            var ways = new List<OsmWay> { W("primary", 10, 100, 230, 100) };
            var s = S(); s.maxCutFillMeters = 100;
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => 20 + (z - 100) * 0.3 + Math.Sin(x / 7) * 1.5), s);
            // §11.1(2026-10-20): 드러나는 높이(바닥 − 최저 지형) ≤ 3m, 오르막 쪽 깎는 깊이 ≤ 3m — 넘으면 한 단 뒤로 / 비움 (옛 "받침대 ≤ 2.5m" 대체)
            Assert.All(r.houses, h => Assert.True(h.plinthY - h.terrainMin <= s.seatTerraceMaxMeters + 1e-6 && h.terrainMax - h.plinthY <= s.seatTerraceMaxMeters + 1e-6));
            Assert.True(Check(r, "M4-overlap").pass, r.report.ToText());
        }

        [Fact]
        public void Seat_front_row_floor_is_one_step_above_sidewalk_and_flat_lots_sit_on_levelled_ground()
        {
            // §11.1 / H2: 앞줄 큰길 집 바닥 = 앞 보도 + 0.15~0.30m. 드러나는 높이 ≤ 0.3m 집은 바닥 밑 지형을 바닥 높이로 고른다(받침대 없음)
            var ways = new List<OsmWay> { W("primary", 10, 100, 230, 100) };
            var s = S(); s.houseModelWidths = new[] { 8f }; s.houseModelDepths = new[] { 7f }; s.houseModelCenterX = new[] { 0f }; s.houseModelCenterZ = new[] { 0f };
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => 5 + Math.Sin(x / 9) * 0.2 + (z - 100) * 0.04), s);
            var road = r.Mains.Single();
            Assert.Contains(r.houses, h => h.row == 0);
            foreach (var h in r.houses.Where(h => h.row == 0))
            {
                double side = road.pts.Min(q => SplineEdge.Dist(q, h.center));
                int i = Enumerable.Range(0, road.pts.Count).OrderBy(k => SplineEdge.Dist(road.pts[k], h.center)).First();
                Assert.InRange(h.plinthY - (road.y[i] + s.curbHeightMeters), 0.15 - 0.05, 0.30 + 0.05);
            }
            // 땅에 바로(0): 바닥 밑 지형이 바닥 높이 / 벽 내림·반지하(1·2): 밑단이 둘레 지형보다 아래 — 어느 쪽이든 바닥 밑 틈 0
            foreach (var h in r.houses)
                foreach (var c in h.Corners())
                {
                    double g = r.terrain.Surface((c.x * 3 + h.center.x) / 4, (c.z * 3 + h.center.z) / 4);
                    if (h.seat == 0) Assert.InRange(g - h.plinthY, -0.08, 0.001);
                    else Assert.True(h.seatBottom <= g + 1e-6 && g <= h.plinthY + 1e-6, $"밑단 {h.seatBottom:0.00} 지형 {g:0.00} 바닥 {h.plinthY:0.00}");
                }
        }
            // ---------------------------------------------------------------- 게이트 FAIL 회귀 (BUG-003 · BUG-007)
        static bool TerrainAboveAnyRoad(SplineTownResult r, out string where)
        {
            var s = new SplineTownSettings();
            foreach (var e in r.edges)
                for (int i = 0; i < e.pts.Count; i++)
                {
                    var (nx, nz) = SplineTown.Normal(e.pts, i);
                    double half = e.alley ? 1.5 : 4.0;
                    for (double off = -half; off <= half + 1e-9; off += 0.5)
                    {
                        double ty = r.terrain.Surface(e.pts[i].x + nx * off, e.pts[i].z + nz * off);
                        double ry = e.y[i] + (!e.alley && Math.Abs(off) > 3 ? 0.15 : 0);
                        if (ty > ry + 0.05) { where = $"({e.pts[i].x:0},{e.pts[i].z:0}) off {off} terr {ty:0.00} road {ry:0.00}"; return true; }
                    }
                }
            where = ""; return false;
        }

        [Fact]
        public void BUG003_two_close_roads_at_different_heights_never_have_terrain_over_either()
        {
            // 높이가 15m 다른 두 큰길이 중심선 9m 거리로 나란히(어깨 영향권이 겹침) — 가장 가까운 길 하나에만 맞추면 높은 길 밑 지형이 낮은 길 보도를 덮던 경우
            var ways = new List<OsmWay> { W("primary", 10, 100, 230, 100), W("primary", 10, 109, 230, 109), W("primary", 120, 100, 120, 20) };
            var s = S(); s.maxCutFillMeters = 100; s.conflictHeightMeters = 1e6f; // 충돌 정리를 끄고 지형 상한만으로 막는지 본다
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => z < 104 ? 5 : 20), s);
            Assert.False(TerrainAboveAnyRoad(r, out var w), w + "\n" + r.report.ToText());
        }

        [Fact]
        public void BUG003_junction_area_has_no_terrain_over_roads_or_disc()
        {
            var ways = new List<OsmWay> { W("primary", 10, 100, 120, 100, 230, 100), W("primary", 120, 100, 120, 190), W("primary", 120, 100, 120, 10) };
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => 10 + Math.Sin(x / 9) * 2 + (z - 100) * 0.06), S());
            Assert.NotEmpty(r.junctions);
            Assert.False(TerrainAboveAnyRoad(r, out var w), w);
            foreach (var n in r.junctions)
                for (double a = 0; a < Math.PI * 2; a += 0.3)
                    for (double rad = 0; rad <= 6; rad += 1)
                        Assert.True(r.terrain.Surface(r.nodes[n].x + Math.Cos(a) * rad, r.nodes[n].z + Math.Sin(a) * rad) <= r.nodeY[n] + 0.03);
        }

        [Fact]
        public void BUG007_off_center_model_pivot_never_puts_house_body_on_road()
        {
            // 모델 바닥이 기준점에서 앞쪽(+z, 길 쪽)으로 0.6m 치우친 경우(CornerShopB 실측) — 몸체 사각형이 길 단면에 안 들어와야 한다
            var ways = new List<OsmWay> { W("primary", 10, 100, 230, 100), W("residential", 120, 100, 120, 150) };
            var s = S();
            s.houseModelWidths = new[] { 6.6f }; s.houseModelDepths = new[] { 7.0f };
            s.houseModelCenterX = new[] { -0.05f }; s.houseModelCenterZ = new[] { 0.6f };
            var r = SplineTown.Build(ways, new FlatHeightField(5), s);
            Assert.NotEmpty(r.houses);
            foreach (var h in r.houses)
            {
                // 실제 모델 몸체 = 기준점에서 로컬 (cx, cz) 만큼 옮긴 자리의 사각형
                double rr = h.yawDeg * Math.PI / 180, fx = Math.Sin(rr), fz = Math.Cos(rr), rx = fz, rz = -fx;
                var body = new HousePlot { center = new Vec2(h.pivot.x + rx * -0.05 + fx * 0.6, h.pivot.z + rz * -0.05 + fz * 0.6), yawDeg = h.yawDeg, width = 6.6, depth = 7.0 };
                Assert.True(Math.Abs(body.center.x - h.center.x) < 1e-6 && Math.Abs(body.center.z - h.center.z) < 1e-6);
                foreach (var e in r.edges)
                {
                    double lim = e.alley ? 1.5 : 6.0;
                    foreach (var c in body.Corners())
                        for (int i = 0; i + 1 < e.pts.Count; i++)
                            Assert.True(SplineTown.DistToPoly(c, new List<Vec2> { e.pts[i], e.pts[i + 1] }) >= lim - 1e-6, $"집 모서리 ({c.x:0.0},{c.z:0.0}) 이 {e.highway} 단면 안");
                }
            }
        }
            // ---------------------------------------------------------------- 필렛 (§2.9, BUG-006)
        static double MinRadius(List<Vec2> p)
        {
            double m = double.PositiveInfinity;
            for (int i = 1; i + 1 < p.Count; i++) m = Math.Min(m, SplineTown.RadiusAt(p, i, 1));
            return m;
        }

        [Fact]
        public void Fillet_sharp_150deg_hairpin_split_into_two_bends_radius_8_and_deviation_within_10()
        {
            // 150° 꺾임: R=8m 한 원호면 이탈 22.9m(§2.9) → 두 굽이로 나눠야 이탈 ≤ 10m
            double a = 150 * Math.PI / 180;
            var raw = new List<Vec2> { new Vec2(0, 0), new Vec2(100, 0), new Vec2(100 + Math.Cos(a) * 100, Math.Sin(a) * 100) };
            var st = new SplineTown.FilletStats();
            var s = new SplineTownSettings();
            var p = SplineTown.Resample(SplineTown.Fillet(raw, 12, 8, 0.5, s, st), 2);
            Assert.True(st.split >= 1, "두 굽이로 나뉘지 않음");
            Assert.True(MinRadius(p) >= 8 - 0.05, $"최소 반경 {MinRadius(p):0.00}");
            double dev = p.Max(v => SplineTown.DistToPoly(v, raw));
            Assert.True(dev <= 10 + 0.5, $"이탈 {dev:0.0}m");
        }

        [Fact]
        public void Fillet_two_close_same_direction_corners_merge_into_one_arc_without_shrinking_radius()
        {
            // 4m 떨어진 두 45° 꺾임(합 90°): R=12 접선 길이 각 5m > 선분 4m → 한 원호로 합쳐 R 을 지킨다
            var raw = new List<Vec2> { new Vec2(0, 0), new Vec2(60, 0), new Vec2(60 + 4 / Math.Sqrt(2), 4 / Math.Sqrt(2)), new Vec2(60 + 4 / Math.Sqrt(2), 60) };
            var st = new SplineTown.FilletStats();
            var p = SplineTown.Resample(SplineTown.Fillet(raw, 12, 8, 0.1, new SplineTownSettings(), st), 2);
            Assert.True(st.merged >= 1, "합쳐지지 않음");
            Assert.Equal(0, st.reduced);
            Assert.True(MinRadius(p) >= 12 - 0.05, $"최소 반경 {MinRadius(p):0.00}");
        }
            [Fact]
        public void Profile_backward_pass_uses_the_limit_of_the_segment_it_clamps()
        {
            // 역방향 다듬기가 구간 (i→i+1) 을 G(i) 로 자르면 패드 첫 구간에 10% 가 샌다(실데이터 18구간).
            var e = new SplineEdge { connector = true };
            for (int i = 0; i <= 50; i++) e.pts.Add(new Vec2(0, i * 2));
            // 가운데 패드(헤어핀 20~25) · 양 끝 고정 · 끝 높이를 경사 용량(43구간×10%×2m + 7구간×5%×2m = 9.3m) 가까이 —
            // 끝에서 밀어 올리는 역방향 다듬기가 패드 경계까지 닿아야 결함이 드러난다
            e.hairpin = Enumerable.Range(0, e.pts.Count).Select(i => i >= 20 && i <= 25).ToList();
            e.pad = Enumerable.Repeat(false, e.pts.Count).ToList();
            var s = new SplineTownSettings();
            var y = SplineTown.Profile(e, new FuncHeightField((x, z) => z * 0.3), 0, 9.1, s);
            for (int i = 1; i < y.Count; i++)
            {
                double g = Math.Abs(y[i] - y[i - 1]) / 2.0;
                Assert.True(g <= SplineTown.GradeLimit(e, i, s) + 1e-6, $"구간 {i - 1}→{i}: {g * 100:0.00}% > {SplineTown.GradeLimit(e, i, s) * 100:0}%");
            }
        }
            // ---------------------------------------------------------------- 3차(2026-10-20): 교차점 판·골목 입구·진입로·매듭·S자
        [Fact]
        public void Alley_mouth_starts_at_host_sidewalk_edge_at_sidewalk_height()
        {
            // 골목이 큰길 중심선에서 시작하면 밝은 돌이 차도 위에 깔린다(그래픽 결함 1) → 보도 바깥 끝·보도 높이에서 시작
            var ways = new List<OsmWay> { W("primary", 10, 100, 120, 100, 230, 100), W("service", 120, 100, 120, 135) };
            var s = S();
            var r = SplineTown.Build(ways, new FlatHeightField(5), s);
            Assert.True(r.Alleys.Count() == 1, r.report.ToText());
            var al = r.Alleys.Single();
            var host = r.Mains.OrderBy(e => SplineTown.DistToPoly(al.pts[0], e.pts)).First();
            double d = SplineTown.DistToPoly(al.pts[0], host.pts);
            Assert.InRange(d, s.HalfSection - 0.05, s.HalfSection + 0.3);
            for (int i = 1; i < al.pts.Count; i++) Assert.True(SplineTown.DistToPoly(al.pts[i], host.pts) >= s.HalfSection - 0.05, "골목 점이 큰길 단면 안");
            Assert.Equal(5 + s.curbHeightMeters, al.y[0], 3);
        }

        [Fact]
        public void Junctions_closer_than_merge_distance_collapse_into_one()
        {
            // 10m 떨어진 두 T 교차점(매듭) → 한 교차점(가지 4개)
            var ways = new List<OsmWay> { W("primary", 10, 100, 120, 100, 130, 100, 230, 100), W("primary", 120, 100, 120, 190), W("primary", 130, 100, 130, 10) };
            var r = SplineTown.Build(ways, new FlatHeightField(5), S());
            Assert.Single(r.junctions);
            Assert.Equal(4, r.junctionLayouts.Single().arms.Count);
            Assert.Contains("1 ", r.report.MetricValue("junction_merged"));
        }

        [Fact]
        public void Junction_plate_on_steep_acute_junction_terrain_never_above_plate_and_sidewalks_do_not_overlap()
        {
            // 약 37° 로 갈라지는 교차점 + 가파른 비탈 — 판(부채꼴) 아래 지형이 판 위로 솟지 않고, 끊는 점에서 이웃 길 보도가 겹치지 않는다
            var ways = new List<OsmWay> { W("primary", 10, 100, 120, 100, 230, 100), W("primary", 120, 100, 200, 160) };
            var s = S(); s.maxCutFillMeters = 100;
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => 5 + z * 0.08 + x * 0.03), s);
            var jl = r.junctionLayouts.Single();
            var node = r.nodes[jl.node];
            var ring = jl.ringP;
            Assert.True(ring.Count >= 3);
            for (int k = 0; k < ring.Count; k++)
                for (double u = 0; u <= 1; u += 0.1)
                    for (double v = 0; u + v <= 1; v += 0.1)
                    {
                        var a = ring[k]; var b = ring[(k + 1) % ring.Count];
                        var q = new Vec2(node.x + (a.x - node.x) * u + (b.x - node.x) * v, node.z + (a.z - node.z) * u + (b.z - node.z) * v);
                        var surf = SplineTown.PlateSurfaceAt(r, jl, q);
                        if (!surf.HasValue) continue;
                        Assert.True(r.terrain.Surface(q.x, q.z) <= surf.Value + 1e-6, $"판 안 ({q.x:0.0},{q.z:0.0}) 지형 {r.terrain.Surface(q.x, q.z):0.00} > 판 면 {surf.Value:0.00}");
                    }
            foreach (var A in jl.arms)
            {
                Assert.True(A.clip >= SplineTown.JunctionClip(s) - 1e-9);
                foreach (var B in jl.arms.Where(b => b != A))
                    foreach (int sg in new[] { 1, -1 })
                    {
                        var p = new Vec2(A.c.x + A.ccw.x * s.HalfSection * sg, A.c.z + A.ccw.z * s.HalfSection * sg);
                        Assert.True(SplineTown.DistToPoly(p, r.edges[B.edge].pts) >= s.HalfSection - 0.3, $"끊는 점 보도 끝 ({p.x:0.0},{p.z:0.0}) 이 이웃 길 단면 안");
                    }
            }
        }

        [Fact]
        public void Every_house_gets_a_driveway_from_sidewalk_edge_to_its_front_door()
        {
            var ways = new List<OsmWay> { W("primary", 10, 100, 230, 100) };
            var s = S(); s.houseModelWidths = new[] { 8f }; s.houseModelDepths = new[] { 7f }; s.houseModelCenterX = new[] { 0f }; s.houseModelCenterZ = new[] { 0f };
            var r = SplineTown.Build(ways, new FlatHeightField(5), s);
            Assert.NotEmpty(r.houses);
            var road = r.Mains.Single();
            foreach (var h in r.houses)
            {
                Assert.True(h.hasDrive, $"진입로 없음 ({h.center.x:0},{h.center.z:0})");
                Assert.InRange(SplineTown.DistToPoly(h.driveA, road.pts), s.HalfSection - 0.05, s.HalfSection + 0.05);
                Assert.InRange(SplineEdge.Dist(h.driveB, h.center), h.depth / 2 - 0.05, h.depth / 2 + 0.05);
                Assert.Equal(5 + s.curbHeightMeters, h.driveAY, 3);
                Assert.Equal(h.plinthY, h.driveBY, 6);
                Assert.False(h.driveSteps);   // 평지
            }
        }

        [Fact]
        public void Fillet_tight_S_bend_is_straightened_instead_of_shrinking_radius()
        {
            // 5m 사이 두 반대 굽이(각 37°) — R12 접선 4m 씩이라 자리가 모자람. 원래 선 이탈 ≤ 6m 면 꼭짓점을 빼서 편다(반경 줄이지 않음)
            var raw = new List<Vec2> { new Vec2(0, 0), new Vec2(60, 0), new Vec2(64, 3), new Vec2(124, 3) };
            var st = new SplineTown.FilletStats();
            var p = SplineTown.Resample(SplineTown.Fillet(raw, 12, 8, 0.1, new SplineTownSettings(), st), 2);
            Assert.True(st.straightened >= 1, "펴지 않음");
            Assert.Equal(0, st.reduced);
            Assert.True(MinRadius(p) >= 12 - 0.05, $"최소 반경 {MinRadius(p):0.00}");
        }
            [Fact]
        public void Duplicate_parallel_edges_between_same_junctions_are_dropped()
        {
            // 같은 두 점을 잇는 길이 두 번 들어온 데이터(실데이터 항구 매듭) → 하나만 남아 겹친 단면이 없다
            var ways = new List<OsmWay> { W("primary", 10, 100, 100, 100, 160, 100, 230, 100), W("primary", 100, 100, 130, 101, 160, 100), W("primary", 100, 100, 100, 190), W("primary", 160, 100, 160, 10) };
            var r = SplineTown.Build(ways, new FlatHeightField(5), S());
            Assert.Contains("중복 길 뺌 1", r.report.MetricValue("junction_merged"));
            foreach (var e in r.Mains)
                foreach (var o in r.Mains.Where(o => o != e))
                    Assert.False(((e.a == o.a && e.b == o.b) || (e.a == o.b && e.b == o.a)) && e.pts.All(q => SplineTown.DistToPoly(q, o.pts) < 3), "겹친 중복 길이 남음");
        }
            [Fact]
        public void BUG005_junction_plate_faces_never_steeper_than_pad_grade()
        {
            // 교차점 판(부채꼴) 각 면의 기울기 ≤ 패드 5%(+0.5%p 재표본 여유). 3차는 둘레 높이를 점 순서로 보간해 81% 면이 생겼다.
            // 판이 평면으로 성립하는 교차점(붙은 길이 경사 상한 안에서 평면 높이를 받아냄)만 대상 — 비탈에서 성립 못 하는 교차점은 원뿔꼴(4차 보고 SPLINE-4TH.md)
            var cases = new[]
            {
                new List<OsmWay> { W("primary", 10, 100, 120, 100, 230, 100), W("primary", 120, 100, 200, 160) },                    // 37° Y
                new List<OsmWay> { W("primary", 10, 100, 120, 100, 230, 100), W("primary", 120, 100, 120, 190), W("primary", 120, 100, 120, 10) }, // +
            };
            foreach (var ways in cases)
            {
                var s = S(); s.maxCutFillMeters = 100;
                var r = SplineTown.Build(ways, new FuncHeightField((x, z) => 5 + z * 0.12 + x * 0.04), s);
                Assert.True(r.junctionPlane.Count == r.junctionLayouts.Count, r.report.MetricValue("junction_planes"));
                foreach (var jl in r.junctionLayouts)
                {
                    var c = r.nodes[jl.node];
                    for (int k = 0; k < jl.ringP.Count; k++)
                    {
                        var a = jl.ringP[k]; var b = jl.ringP[(k + 1) % jl.ringP.Count];
                        double ya = jl.ringY[k], yb = jl.ringY[(k + 1) % jl.ringP.Count];
                        double ux = a.x - c.x, uy = ya - jl.y, uz = a.z - c.z, vx = b.x - c.x, vy = yb - jl.y, vz = b.z - c.z;
                        double nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
                        if (Math.Abs(ny) < 0.02) continue;   // 면적 0.01㎡ 미만 면은 기울기 정의가 불안정
                        double g = Math.Sqrt(nx * nx + nz * nz) / Math.Abs(ny);
                        Assert.True(g <= s.hairpinMaxGrade + 0.005, $"판 면 ({a.x:0.0},{a.z:0.0})-({b.x:0.0},{b.z:0.0}) 기울기 {g * 100:0.0}%");
                    }
                }
            }
        }
            [Fact]
        public void BUG010_driveway_entry_edge_matches_sloped_sidewalk_on_both_sides()
        {
            // 6% 오르막 길: 보도가 길 방향으로 기울어 평평한 입구는 양 끝이 ±(경사×반폭) 어긋났다 → 입구 디딤판 두 끝 = 그 자리 보도 높이
            var ways = new List<OsmWay> { W("primary", 10, 100, 230, 100) };
            var s = S(); s.houseModelWidths = new[] { 8f }; s.houseModelDepths = new[] { 7f }; s.houseModelCenterX = new[] { 0f }; s.houseModelCenterZ = new[] { 0f };
            var r = SplineTown.Build(ways, new FuncHeightField((x, z) => 5 + x * 0.06), s);
            var road = r.Mains.Single();
            double RoadY(Vec2 q)
            {
                double best = double.MaxValue, y = 0;
                for (int i = 0; i + 1 < road.pts.Count; i++)
                {
                    double d = SplineTown.DistToPoly(q, new List<Vec2> { road.pts[i], road.pts[i + 1] });
                    if (d < best) { best = d; double L = SplineEdge.Dist(road.pts[i], road.pts[i + 1]); double t = L > 1e-9 ? ((q.x - road.pts[i].x) * (road.pts[i + 1].x - road.pts[i].x) + (q.z - road.pts[i].z) * (road.pts[i + 1].z - road.pts[i].z)) / (L * L) : 0; t = Math.Max(0, Math.Min(1, t)); y = road.y[i] + (road.y[i + 1] - road.y[i]) * t; }
                }
                return y + s.curbHeightMeters;
            }
            int n = 0;
            foreach (var h in r.houses.Where(h => h.hasDrive))
            {
                double len = SplineEdge.Dist(h.driveA, h.driveB), ux = (h.driveB.x - h.driveA.x) / len, uz = (h.driveB.z - h.driveA.z) / len, px = -uz, pz = ux;
                var L0 = new Vec2(h.driveA.x + px * h.driveW / 2, h.driveA.z + pz * h.driveW / 2); var R0 = new Vec2(h.driveA.x - px * h.driveW / 2, h.driveA.z - pz * h.driveW / 2);
                Assert.InRange(h.driveAYL - RoadY(L0), -0.01, 0.01);
                Assert.InRange(h.driveAYR - RoadY(R0), -0.01, 0.01);
                Assert.True(Math.Abs(h.driveAYL - h.driveAYR) > 0.05, "기운 보도인데 입구 두 끝 높이가 같음");
                n++;
            }
            Assert.True(n > 5);
        }
    }
}
