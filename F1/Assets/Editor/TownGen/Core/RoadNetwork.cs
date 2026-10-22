using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace PitCrew.TownGen
{
    /// <summary>스펙 §4.1 도로 등급. OSM·roadgen 앞단이 이 6개로 매핑해 내보낸다.</summary>
    public enum RoadClass { Major, Minor, Alley, Footway, Steps, Path }

    public struct Vec2
    {
        public double x, z;
        public Vec2(double x, double z) { this.x = x; this.z = z; }
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:0.##},{1:0.##})", x, z);
    }

    public class RoadSegment
    {
        public int id;
        public RoadClass cls;
        public List<Vec2> points = new List<Vec2>();
    }

    /// <summary>
    /// 선형 도로망 — 두 앞단의 공통 출력(§4.1).
    /// 좌표는 미터, 원점 = 땅 남서 모서리, +z = 북(바다 반대).
    /// </summary>
    public class RoadNetwork
    {
        public string source = "";
        public long seed;
        public double boundsX, boundsZ;
        public List<RoadSegment> segments = new List<RoadSegment>();
        public List<Vec2> landmarksHint = new List<Vec2>();

        static readonly Dictionary<string, RoadClass> ClassNames = new Dictionary<string, RoadClass>
        {
            { "major", RoadClass.Major }, { "minor", RoadClass.Minor }, { "alley", RoadClass.Alley },
            { "footway", RoadClass.Footway }, { "steps", RoadClass.Steps }, { "path", RoadClass.Path },
        };

        public static string ClassName(RoadClass c) => c.ToString().ToLowerInvariant();

        /// <summary>JSON 문자열을 읽는다. 형식 오류는 errors 에, 경고(범위 밖 점 등)는 warnings 에. 오류가 하나라도 있으면 null.</summary>
        public static RoadNetwork Parse(string json, List<string> errors, List<string> warnings)
        {
            JObject root;
            try { root = JObject.Parse(json); }
            catch (Exception ex) { errors.Add("JSON 파싱 실패: " + ex.Message); return null; }

            var net = new RoadNetwork();
            var src = root["source"]?.ToString();
            if (src != "osm" && src != "roadgen" && src != "test") errors.Add($"source 는 osm|roadgen|test 여야 함: '{src}'");
            net.source = src ?? "";
            net.seed = root["seed"]?.Type == JTokenType.Integer ? root["seed"].Value<long>() : 0;

            var b = root["bounds_m"] as JArray;
            if (b == null || b.Count != 2) errors.Add("bounds_m 는 [x, z] 두 수");
            else { net.boundsX = b[0].Value<double>(); net.boundsZ = b[1].Value<double>(); }
            if (net.boundsX <= 0 || net.boundsZ <= 0) errors.Add($"bounds_m 양수 아님: [{net.boundsX}, {net.boundsZ}]");

            var segs = root["segments"] as JArray;
            if (segs == null) { errors.Add("segments 배열 없음"); return null; }
            var ids = new HashSet<int>();
            int outside = 0;
            foreach (var t in segs)
            {
                var s = new RoadSegment();
                if (t["id"]?.Type != JTokenType.Integer) { errors.Add("segment id 정수 아님: " + t["id"]); continue; }
                s.id = t["id"].Value<int>();
                if (!ids.Add(s.id)) errors.Add("segment id 중복: " + s.id);
                var cname = t["class"]?.ToString() ?? "";
                if (!ClassNames.TryGetValue(cname, out s.cls)) { errors.Add($"segment {s.id} class 모름: '{cname}'"); continue; }
                var pts = t["points_m"] as JArray;
                if (pts == null || pts.Count < 2) { errors.Add($"segment {s.id} points_m 2개 미만"); continue; }
                foreach (var p in pts)
                {
                    var pa = p as JArray;
                    if (pa == null || pa.Count != 2) { errors.Add($"segment {s.id} 점 형식 [x,z] 아님: {p}"); break; }
                    var v = new Vec2(pa[0].Value<double>(), pa[1].Value<double>());
                    if (v.x < 0 || v.z < 0 || v.x > net.boundsX || v.z > net.boundsZ) outside++;
                    s.points.Add(v);
                }
                net.segments.Add(s);
            }
            if (outside > 0) warnings.Add($"땅 밖 점 {outside}개 — 칸 래스터화에서 잘림");

            if (root["landmarks_hint"] is JArray lh)
                foreach (var p in lh)
                    if (p is JArray pa && pa.Count == 2) net.landmarksHint.Add(new Vec2(pa[0].Value<double>(), pa[1].Value<double>()));

            return errors.Count == 0 ? net : null;
        }

        /// <summary>§4.1 형식으로 다시 쓴다 (polylines.json 보존용).</summary>
        public string ToJson()
        {
            var root = new JObject
            {
                ["source"] = source,
                ["seed"] = seed,
                ["bounds_m"] = new JArray(boundsX, boundsZ),
            };
            var segs = new JArray();
            foreach (var s in segments)
            {
                var pts = new JArray();
                foreach (var p in s.points) pts.Add(new JArray(Math.Round(p.x, 3), Math.Round(p.z, 3)));
                segs.Add(new JObject { ["id"] = s.id, ["class"] = ClassName(s.cls), ["points_m"] = pts });
            }
            root["segments"] = segs;
            var lh = new JArray();
            foreach (var p in landmarksHint) lh.Add(new JArray(p.x, p.z));
            root["landmarks_hint"] = lh;
            return root.ToString(Newtonsoft.Json.Formatting.Indented);
        }
    }
}
