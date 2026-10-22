using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace PitCrew.TownGen.Tests
{
    public class TownBackendTests
    {
        static RoadNetwork Load(string name)
        {
            var errors = new List<string>(); var warnings = new List<string>();
            var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "samples", name));
            var net = RoadNetwork.Parse(json, errors, warnings);
            Assert.True(net != null, string.Join("; ", errors));
            return net;
        }

        /// <summary>바다 끈 기본 설정 — 시험 지형이 높이 0 평지라 해수면(0.15m) 규칙이 전부 바다로 만들지 않게.</summary>
        static TownGenSettings Land() => new TownGenSettings { seaLevelMeters = -1e6f };

        static TownGenSettings Small(float bx, float bz)
        {
            // 작은 시험 맵용: 배달지 3곳, 구간 축소. 나머지는 기본값(칸 8m·경사 12%·5%·2%).
            var s = Land(); s.boundsXMeters = bx; s.boundsZMeters = bz;
            s.nearMinMeters = 0; s.nearMaxMeters = 60; s.midMaxMeters = 150;
            s.nearCount = 1; s.midCount = 1; s.farCount = 1;
            s.deliveryLabels = new[] { "A", "B", "C" };
            s.nearFixedLabel = "A"; s.midFixedLabel = "B"; s.farFixedLabel = "C";
            s.deliveryMinSpacingCells = 1;
            return s;
        }

        static (bool pass, string detail) CheckOf(TownGenResult r, string id)
        {
            var c = r.report.checks.LastOrDefault(x => x.id == id);
            Assert.True(c.id != null, $"검사 {id} 없음\n{r.report.ToText()}");
            return (c.pass, c.detail);
        }

        static int MetricInt(TownGenResult r, string key) => int.Parse(r.report.MetricValue(key));

        // ---------------------------------------------------------------- 형식
        [Fact]
        public void Parse_rejects_unknown_class_and_bad_points()
        {
            var errors = new List<string>(); var warnings = new List<string>();
            var net = RoadNetwork.Parse(@"{ ""source"": ""osm"", ""bounds_m"": [100, 100],
                ""segments"": [ { ""id"": 1, ""class"": ""motorway"", ""points_m"": [[0,0],[10,0]] },
                                { ""id"": 2, ""class"": ""minor"", ""points_m"": [[0,0]] } ] }", errors, warnings);
            Assert.Null(net);
            Assert.Contains(errors, e => e.Contains("motorway"));
            Assert.Contains(errors, e => e.Contains("2개 미만"));
        }

        [Fact]
        public void Parse_warns_points_outside_bounds_and_roundtrips()
        {
            var errors = new List<string>(); var warnings = new List<string>();
            var net = RoadNetwork.Parse(@"{ ""source"": ""roadgen"", ""seed"": 7, ""bounds_m"": [100, 50],
                ""segments"": [ { ""id"": 1, ""class"": ""major"", ""points_m"": [[-5,10],[120,10]] } ] }", errors, warnings);
            Assert.NotNull(net);
            Assert.Single(warnings);
            var again = RoadNetwork.Parse(net.ToJson(), new List<string>(), new List<string>());
            Assert.Equal(7, again.seed);
            Assert.Equal(RoadClass.Major, again.segments[0].cls);
        }

        // ---------------------------------------------------------------- 래스터화
        [Fact]
        public void Rasterize_diagonal_is_4_connected_and_ordered()
        {
            var cells = TownBackend.Rasterize(new List<Vec2> { new Vec2(4, 4), new Vec2(76, 52) }, 8, 20, 20);
            Assert.Equal(0, cells[0]);                 // (0,0)
            Assert.Equal(6 * 20 + 9, cells[cells.Count - 1]); // (9,6)
            for (int k = 1; k < cells.Count; k++)
            {
                int a = cells[k - 1], b = cells[k];
                int dc = Math.Abs(a % 20 - b % 20), dr = Math.Abs(a / 20 - b / 20);
                Assert.Equal(1, dc + dr);              // 대각선 점프 없음
            }
        }

        // ---------------------------------------------------------------- 전체 흐름 (평지 격자, 기본 설정 480×400·칸 8m·배달지 11)
        [Fact]
        public void GridFlat_full_pipeline_passes_hard_checks()
        {
            var r = TownBackend.Run(Load("grid_flat.json"), new FlatHeightField(0), Land());
            var text = r.report.ToText();
            Assert.True(CheckOf(r, "A1").pass, text);
            Assert.True(CheckOf(r, "A2").pass, text);
            Assert.True(CheckOf(r, "A2-fixed-bands").pass, text);
            Assert.True(CheckOf(r, "D100-1").pass, text);
            Assert.True(CheckOf(r, "D100-2").pass, text);
            Assert.True(CheckOf(r, "D100-3").pass, text); // 평지 → 대상 아님(통과)
            Assert.Equal(11, r.grid.deliveries.Count);
            Assert.Equal(60, r.grid.W); Assert.Equal(50, r.grid.H);
            Assert.Equal(new[] { "CityHall", "Plaza", "Depot", "PierEnd", "CargoDock", "Warehouse", "FactoryA", "Station", "ContainerYard", "FishMarket", "Mall" }.OrderBy(x => x),
                         r.grid.deliveries.Select(d => d.label).OrderBy(x => x));
            // 출발점: 큰길·바다에서 2칸 이상
            Assert.Equal(CellKind.Major, r.grid.kind[r.grid.start]);
            Assert.True(r.grid.Row(r.grid.start) >= 2);
            // 계단은 도로가 아니고 장식 후보, path 는 버림
            Assert.True(r.grid.stepsDecor.Count(b => b) > 0);
            int stepCell = r.grid.Index(300 / 8, 376 / 8);
            Assert.True(r.grid.stepsDecor[stepCell]);
            Assert.False(r.grid.IsRoad(stepCell));
        }

        [Fact]
        public void Deterministic_same_input_same_hash()
        {
            var a = TownBackend.Run(Load("grid_flat.json"), new FlatHeightField(0), Land());
            var b = TownBackend.Run(Load("grid_flat.json"), new FlatHeightField(0), Land());
            Assert.Equal(a.grid.RoadMaskHash(), b.grid.RoadMaskHash());
            Assert.Equal(a.grid.deliveries.Select(d => d.label + d.cell), b.grid.deliveries.Select(d => d.label + d.cell));
        }

        [Fact]
        public void Settings_change_cell_size_without_code_change()
        {
            var s = Land(); s.cellSizeMeters = 6f;
            var r = TownBackend.Run(Load("grid_flat.json"), new FlatHeightField(0), s);
            Assert.Equal(80, r.grid.W); Assert.Equal(67, r.grid.H);
            Assert.True(CheckOf(r, "A1").pass, r.report.ToText());
        }

        // ---------------------------------------------------------------- 승격 (a) / A* 잇기
        [Fact]
        public void Footway_bridging_two_pieces_is_promoted_and_island_joined_by_astar()
        {
            var r = TownBackend.Run(Load("islands_footway.json"), new FlatHeightField(0), Small(240, 96));
            var text = r.report.ToText();
            Assert.Equal(1, MetricInt(r, "promoted_footway_a"));       // 3번만(두 조각을 잇는 것). 5번은 한 조각에만 닿음
            Assert.True(MetricInt(r, "connector_cells_astar") > 0, text); // 위쪽 섬(4번)은 A* 로
            Assert.Equal(0, MetricInt(r, "dropped_isolated_cells"));
            Assert.True(CheckOf(r, "A1").pass, text);
            for (int c = 13; c <= 16; c++) Assert.Equal(CellOrigin.PromotedA, r.grid.origin[r.grid.Index(c, 2)]);
        }

        // ---------------------------------------------------------------- 경사 끊기
        [Fact]
        public void Steep_road_is_cut_and_isolated_rest_dropped()
        {
            var json = @"{ ""source"": ""test"", ""bounds_m"": [240, 96], ""segments"": [
                { ""id"": 1, ""class"": ""major"", ""points_m"": [[4, 20], [236, 20]] },
                { ""id"": 2, ""class"": ""major"", ""points_m"": [[124, 20], [124, 92]] } ] }";
            var net = RoadNetwork.Parse(json, new List<string>(), new List<string>());
            var hf = new FuncHeightField((x, z) => Math.Max(0, (z - 30) * 0.25)); // 북쪽으로 25% 오르막
            var r = TownBackend.Run(net, hf, Small(240, 96));
            var text = r.report.ToText();
            Assert.True(CheckOf(r, "D100-1").pass, text);
            Assert.True(CheckOf(r, "A1").pass, text);
            Assert.True(MetricInt(r, "steep_cut_cells") > 0, text);
            for (int row = 5; row < r.grid.H; row++) Assert.False(r.grid.IsRoad(15, row), $"row {row}\n{text}");
        }

        // ---------------------------------------------------------------- 정리
        [Fact]
        public void One_cell_stub_removed_two_cell_branch_kept()
        {
            var json = @"{ ""source"": ""test"", ""bounds_m"": [240, 96], ""segments"": [
                { ""id"": 1, ""class"": ""major"", ""points_m"": [[4, 28], [236, 28]] },
                { ""id"": 2, ""class"": ""minor"", ""points_m"": [[60, 28], [60, 36]] },
                { ""id"": 3, ""class"": ""minor"", ""points_m"": [[180, 28], [180, 44]] } ] }";
            var net = RoadNetwork.Parse(json, new List<string>(), new List<string>());
            var s = Small(240, 96); s.startCol = 1; s.startRow = 3;
            var r = TownBackend.Run(net, new FlatHeightField(0), s);
            Assert.False(r.grid.IsRoad(7, 4), r.report.ToText());   // 1칸 가지 → 지움
            Assert.True(r.grid.IsRoad(22, 4)); Assert.True(r.grid.IsRoad(22, 5)); // 2칸 가지 → 남김
        }

        [Fact]
        public void Parallel_two_wide_strip_is_thinned_to_one()
        {
            var json = @"{ ""source"": ""test"", ""bounds_m"": [240, 96], ""segments"": [
                { ""id"": 1, ""class"": ""major"", ""points_m"": [[4, 28], [236, 28]] },
                { ""id"": 2, ""class"": ""minor"", ""points_m"": [[4, 36], [236, 36]] } ] }";
            var net = RoadNetwork.Parse(json, new List<string>(), new List<string>());
            var r = TownBackend.Run(net, new FlatHeightField(0), Small(240, 96));
            var g = r.grid;
            for (int c = 0; c + 1 < g.W; c++)
                Assert.False(g.IsRoad(c, 3) && g.IsRoad(c + 1, 3) && g.IsRoad(c, 4) && g.IsRoad(c + 1, 4), $"2x2 at {c}\n{r.report.ToText()}");
            Assert.True(MetricInt(r, "thinned_cells") > 0);
            for (int c = 0; c < g.W; c++) Assert.Equal(CellKind.Major, g.kind[g.Index(c, 3)]); // 큰길은 남기고 골목을 지운다
            Assert.True(CheckOf(r, "A1").pass);
        }

        // ---------------------------------------------------------------- D-100 ② 우회로 / 승격 (b)
        static IHeightField Bump() => new FuncHeightField((x, z) => 3.0 * Math.Exp(-((x - 124) * (x - 124) + (z - 60) * (z - 60)) / 576.0));

        static TownGenSettings HillSettings()
        {
            var s = Small(240, 96);
            s.startCol = 1; s.startRow = 7;
            s.detourMaxRatio = 3f; // 시험 맵이 작아서 우회 길이 예산만 키움(규칙 논리 검증용)
            return s;
        }

        static RoadNetwork HillNet(string loopClass)
        {
            var net = Load("hill_detour.json");
            if (loopClass == null) net.segments.RemoveAll(sg => sg.id == 2);
            else net.segments.First(sg => sg.id == 2).cls = loopClass == "minor" ? RoadClass.Minor : RoadClass.Footway;
            return net;
        }

        [Fact]
        public void Hill_with_flat_road_loop_has_detour()
        {
            var r = TownBackend.Run(HillNet("minor"), Bump(), HillSettings());
            Assert.True(CheckOf(r, "D100-2").pass, r.report.ToText());
            Assert.Contains("우회 있음 1", r.report.MetricValue("d100_2_hill_runs"));
        }

        [Fact]
        public void Hill_without_alternative_fails_detour_check()
        {
            var r = TownBackend.Run(HillNet(null), Bump(), HillSettings());
            Assert.False(CheckOf(r, "D100-2").pass, r.report.ToText());
            Assert.Contains("우회 없음 1", r.report.MetricValue("d100_2_hill_runs"));
        }

        [Fact]
        public void Hill_with_footway_alternative_promotes_it_b()
        {
            var r = TownBackend.Run(HillNet("footway"), Bump(), HillSettings());
            var text = r.report.ToText();
            Assert.Equal(0, MetricInt(r, "promoted_footway_a")); // 한 조각에만 닿음 → (a) 아님
            Assert.Equal(1, MetricInt(r, "promoted_footway_b"));
            Assert.True(CheckOf(r, "D100-2").pass, text);
            Assert.Equal(CellOrigin.PromotedB, r.grid.origin[r.grid.Index(15, 2)]);
        }

        [Fact]
        public void Monotonic_climb_is_counted_as_climb_not_failure()
        {
            var json = @"{ ""source"": ""test"", ""bounds_m"": [240, 96], ""segments"": [
                { ""id"": 1, ""class"": ""major"", ""points_m"": [[4, 20], [236, 20]] } ] }";
            var net = RoadNetwork.Parse(json, new List<string>(), new List<string>());
            var hf = new FuncHeightField((x, z) => Math.Max(0, (x - 40) * 0.08)); // 동쪽으로 8% 오르막
            var s = Small(240, 96); s.startCol = 0; s.startRow = 2; s.harborMaxHeightMeters = 1000;
            var r = TownBackend.Run(net, hf, s);
            Assert.True(CheckOf(r, "D100-2").pass, r.report.ToText());
            Assert.Contains("오르막(평지 우회 원리상 불가) 1", r.report.MetricValue("d100_2_hill_runs"));
        }

        // ---------------------------------------------------------------- 랜드마크
        [Fact]
        public void Landmark_slot_next_to_highest_upper_tier_road()
        {
            var json = @"{ ""source"": ""test"", ""bounds_m"": [240, 96], ""segments"": [
                { ""id"": 1, ""class"": ""major"", ""points_m"": [[4, 20], [236, 20]] } ] }";
            var net = RoadNetwork.Parse(json, new List<string>(), new List<string>());
            var hf = new FuncHeightField((x, z) => x * 0.05); // 동쪽 끝이 제일 높음 (5% — 경사 끊기 없음)
            var s = Small(240, 96); s.harborMaxHeightMeters = 1; s.startCol = 0; s.startRow = 2;
            var r = TownBackend.Run(net, hf, s);
            Assert.True(CheckOf(r, "D100-3").pass, r.report.ToText());
            Assert.Single(r.grid.landmarks);
            int lm = r.grid.landmarks[0];
            Assert.True(Math.Abs(r.grid.Col(lm) - 29) <= 1 && Math.Abs(r.grid.Row(lm) - 2) <= 1);
            Assert.False(r.grid.IsRoad(lm));
        }
            // ---------------------------------------------------------------- 바다 (D-110)
        [Fact]
        public void Roads_are_never_on_south_connected_sea_and_inland_low_ground_is_not_sea()
        {
            // 남쪽 20m 는 높이 0(바다), 그 위는 3m 땅. 가운데 내륙 웅덩이(높이 0)는 바다가 아님.
            var json = @"{ ""source"": ""test"", ""bounds_m"": [240, 96], ""segments"": [
                { ""id"": 1, ""class"": ""major"", ""points_m"": [[4, 4], [236, 4]] },
                { ""id"": 2, ""class"": ""major"", ""points_m"": [[4, 44], [236, 44]] },
                { ""id"": 3, ""class"": ""minor"", ""points_m"": [[124, 4], [124, 92]] },
                { ""id"": 4, ""class"": ""minor"", ""points_m"": [[60, 76], [180, 76]] } ] }";
            var net = RoadNetwork.Parse(json, new List<string>(), new List<string>());
            var hf = new FuncHeightField((x, z) => z < 20 ? 0 : (Math.Abs(x - 124) < 12 && Math.Abs(z - 76) < 12 ? 0 : 3));
            var s = Small(240, 96); s.seaLevelMeters = 0.15f;
            var r = TownBackend.Run(net, hf, s);
            var g = r.grid;
            Assert.True(CheckOf(r, "SEA").pass, r.report.ToText());
            for (int c = 0; c < g.W; c++) { Assert.True(g.sea[g.Index(c, 0)]); Assert.False(g.IsRoad(c, 0)); }
            Assert.True(int.Parse(r.report.MetricValue("sea_cut_raster_cells")) > 0);
            Assert.False(g.sea[g.Index(15, 9)]);            // 내륙 웅덩이는 바다 아님
            Assert.Equal(2, g.seaTopRow);                    // 행 0~1 + 해안선(z=20)에 모서리가 닿는 행 2 (칸 최저점 기준)
            for (int i = 0; i < g.kind.Length; i++) Assert.False(g.sea[i] && g.IsRoad(i));
        }
    }
}
