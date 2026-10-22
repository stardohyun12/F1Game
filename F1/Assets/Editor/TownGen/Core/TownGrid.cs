using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PitCrew.TownGen
{
    /// <summary>칸 종류. Major=큰길(2차로), Minor=골목(1차로: minor·alley·승격 footway·A* 잇기).</summary>
    public enum CellKind : byte { Empty = 0, Major = 1, Minor = 2 }

    /// <summary>골목 칸이 어디서 왔는지 (지표·그림용).</summary>
    public enum CellOrigin : byte { None = 0, Source = 1, PromotedA = 2, PromotedB = 3, Connector = 4 }

    public enum DistanceBand { Near, Mid, Far }

    public class Delivery
    {
        public string label;
        public int cell;
        public int col, row;
        public double distanceMeters;
        public DistanceBand band;
        /// <summary>목표 구간 안에서 못 찾아 다른 구간에서 가져왔으면 true.</summary>
        public bool outOfBand;
    }

    /// <summary>뒷단 결과 — 칸 격자 + 출발점·배달지·랜드마크. row 0 = 남쪽(바다 쪽), TrackLayout 과 같은 아래→위 순서.</summary>
    public class TownGrid
    {
        public readonly int W, H;
        public readonly double cell;
        public readonly CellKind[] kind;
        public readonly CellOrigin[] origin;
        public readonly bool[] stepsDecor;
        public readonly double[] height;
        public readonly int[] tier;
        /// <summary>바다 칸(남쪽 변에서 이어진 해수면 이하) — 도로 금지. D-110.</summary>
        public readonly bool[] sea;
        /// <summary>바다 칸이 있는 가장 북쪽 행(없으면 -1). 씬 바다 평면을 여기까지 덮는다.</summary>
        public int seaTopRow = -1;
        public int start = -1;
        public float startYawDeg;
        public readonly List<Delivery> deliveries = new List<Delivery>();
        public readonly List<int> landmarks = new List<int>();

        public TownGrid(int w, int h, double cell)
        {
            W = w; H = h; this.cell = cell;
            kind = new CellKind[w * h];
            origin = new CellOrigin[w * h];
            stepsDecor = new bool[w * h];
            height = new double[w * h];
            tier = new int[w * h];
            sea = new bool[w * h];
        }

        public int Index(int c, int r) => r * W + c;
        public int Col(int i) => i % W;
        public int Row(int i) => i / W;
        public bool InBounds(int c, int r) => c >= 0 && r >= 0 && c < W && r < H;
        public bool IsRoad(int i) => kind[i] != CellKind.Empty;
        public bool IsRoad(int c, int r) => InBounds(c, r) && kind[Index(c, r)] != CellKind.Empty;

        static readonly int[] DC = { 0, 1, 0, -1 };
        static readonly int[] DR = { 1, 0, -1, 0 };

        /// <summary>4방향 이웃 (북·동·남·서 순서 고정 — 결정론).</summary>
        public IEnumerable<int> Neighbors4(int i)
        {
            int c = Col(i), r = Row(i);
            for (int d = 0; d < 4; d++)
            {
                int nc = c + DC[d], nr = r + DR[d];
                if (InBounds(nc, nr)) yield return Index(nc, nr);
            }
        }

        public int RoadDegree(int i)
        {
            int n = 0;
            foreach (var j in Neighbors4(i)) if (IsRoad(j)) n++;
            return n;
        }

        /// <summary>두 이웃 칸 사이 경사(비율, 부호 없음) = |높이차| / 칸 크기. 연속 고도 기준.</summary>
        public double Slope(int a, int b) => Math.Abs(height[a] - height[b]) / cell;

        public int CountRoad()
        {
            int n = 0;
            for (int i = 0; i < kind.Length; i++) if (IsRoad(i)) n++;
            return n;
        }

        /// <summary>roadMask (아래→위 행, '#'=도로 '.'=빈칸) — TrackLayout.roadMask 와 같은 형식.</summary>
        public string[] RoadMask()
        {
            var rows = new string[H];
            var sb = new StringBuilder(W);
            for (int r = 0; r < H; r++)
            {
                sb.Clear();
                for (int c = 0; c < W; c++) sb.Append(IsRoad(Index(c, r)) ? '#' : '.');
                rows[r] = sb.ToString();
            }
            return rows;
        }

        /// <summary>heightMask (아래→위 행, '0'~'2' = 고도 단계).</summary>
        public string[] HeightMask()
        {
            var rows = new string[H];
            var sb = new StringBuilder(W);
            for (int r = 0; r < H; r++)
            {
                sb.Clear();
                for (int c = 0; c < W; c++) sb.Append((char)('0' + tier[Index(c, r)]));
                rows[r] = sb.ToString();
            }
            return rows;
        }

        /// <summary>A7 결정성 비교용 — roadMask 의 SHA1 앞 12자리.</summary>
        public string RoadMaskHash()
        {
            var joined = string.Join("\n", RoadMask());
            using (var sha = SHA1.Create())
            {
                var h = sha.ComputeHash(Encoding.UTF8.GetBytes(joined));
                var sb = new StringBuilder();
                for (int i = 0; i < 6; i++) sb.Append(h[i].ToString("X2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        /// <summary>출발점에서 도로 칸 BFS 거리(칸 수). 도달 못 하면 -1. parent 는 최단 경로 되짚기용.</summary>
        public int[] BfsFrom(int src, out int[] parent)
        {
            var dist = new int[kind.Length];
            parent = new int[kind.Length];
            for (int i = 0; i < dist.Length; i++) { dist[i] = -1; parent[i] = -1; }
            if (src < 0 || !IsRoad(src)) return dist;
            var q = new Queue<int>();
            dist[src] = 0; q.Enqueue(src);
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                foreach (var v in Neighbors4(u))
                {
                    if (!IsRoad(v) || dist[v] >= 0) continue;
                    dist[v] = dist[u] + 1; parent[v] = u; q.Enqueue(v);
                }
            }
            return dist;
        }

        /// <summary>도로 칸 연결 요소 번호(-1 = 도로 아님). 번호는 가장 작은 칸 번호 순.</summary>
        public int[] Components(out List<int> sizes)
        {
            var comp = new int[kind.Length];
            for (int i = 0; i < comp.Length; i++) comp[i] = -1;
            sizes = new List<int>();
            var q = new Queue<int>();
            for (int i = 0; i < comp.Length; i++)
            {
                if (!IsRoad(i) || comp[i] >= 0) continue;
                int id = sizes.Count, n = 0;
                comp[i] = id; q.Enqueue(i);
                while (q.Count > 0)
                {
                    int u = q.Dequeue(); n++;
                    foreach (var v in Neighbors4(u))
                        if (IsRoad(v) && comp[v] < 0) { comp[v] = id; q.Enqueue(v); }
                }
                sizes.Add(n);
            }
            return comp;
        }

        public JObject ToJson()
        {
            var dl = new JArray();
            foreach (var d in deliveries)
                dl.Add(new JObject
                {
                    ["label"] = d.label, ["col"] = d.col, ["row"] = d.row,
                    ["distance_m"] = Math.Round(d.distanceMeters, 1), ["band"] = d.band.ToString().ToLowerInvariant(),
                    ["out_of_band"] = d.outOfBand,
                });
            var lm = new JArray();
            foreach (var i in landmarks) lm.Add(new JArray(Col(i), Row(i)));
            var hs = new JArray();
            foreach (var h in height) hs.Add(Math.Round(h, 2));
            var kinds = new StringBuilder();
            for (int r = H - 1; r >= 0; r--)
            {
                for (int c = 0; c < W; c++)
                {
                    int i = Index(c, r);
                    char ch = kind[i] == CellKind.Major ? 'M' : kind[i] == CellKind.Minor
                        ? (origin[i] == CellOrigin.Connector ? 'a' : origin[i] == CellOrigin.PromotedA || origin[i] == CellOrigin.PromotedB ? 'f' : 'm')
                        : (stepsDecor[i] ? 's' : '.');
                    kinds.Append(ch);
                }
                kinds.Append('\n');
            }
            return new JObject
            {
                ["cols"] = W, ["rows"] = H, ["cell_m"] = cell,
                ["start"] = new JArray(Col(start), Row(start)), ["start_yaw_deg"] = startYawDeg,
                ["deliveries"] = dl, ["landmarks"] = lm,
                ["road_mask_bottom_to_top"] = new JArray(RoadMask()),
                ["height_mask_bottom_to_top"] = new JArray(HeightMask()),
                ["heights_m_row_major_from_south"] = hs,
                ["legend"] = "M=큰길 m=골목 f=승격 footway a=A*잇기 s=계단 장식 후보 .=빈칸 (위=북)",
                ["cells_top_to_bottom"] = kinds.ToString(),
                ["road_mask_sha1_12"] = RoadMaskHash(),
            };
        }
    }
}
