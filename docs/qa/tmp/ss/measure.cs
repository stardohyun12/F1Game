// 과제 1428: 계획 자리(json)가 씬 V2 길 표면 위에 떨어지는지 센다. 소품 = 발 중심 1점, 연석 = path 점마다.
var race = GameObject.Find("Race"); var proxy = GameObject.Find("_SS_PROXY");
System.Func<float, float, float, string> surf = (x, y, z) => {
  RaycastHit best = default; bool any = false;
  foreach (var h in Physics.RaycastAll(new Vector3(x, y + 4f, z), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore)) {
    if (h.transform.root == race.transform || (proxy && h.transform.root == proxy.transform)) continue;
    if (!any || h.point.y > best.point.y) { best = h; any = true; } }
  if (!any) return "none"; var n = best.collider.name; return n.StartsWith("ROAD_V2") ? "road" : n.Contains("Terrain") ? "terrain" : "other:" + n; };
var ci = System.Globalization.CultureInfo.InvariantCulture; string seg = ""; var sb = new System.Text.StringBuilder("{");
var res = new System.Collections.Generic.SortedDictionary<string, int>(); var onRoad = new System.Text.StringBuilder();
foreach (var ln in System.IO.File.ReadAllLines(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/ss/data.txt")))) {
  var p = ln.Split(' ');
  if (p[0] == "SEG") { seg = p[1]; continue; }
  if (p[0] != "E") continue;
  float y = float.Parse(p[4], ci);
  if (p[2] == "curb_segment") { for (int i = 9; i < p.Length; i++) { var a = p[i].Split(','); var s = surf(float.Parse(a[0], ci), y, float.Parse(a[1], ci)); s = s.StartsWith("other") ? "other" : s;
      var k = seg + ".curb_pt." + s; res[k] = res.ContainsKey(k) ? res[k] + 1 : 1; } }
  else { var s = surf(float.Parse(p[3], ci), y, float.Parse(p[5], ci)); var k = seg + ".item." + (s.StartsWith("other") ? "other" : s); res[k] = res.ContainsKey(k) ? res[k] + 1 : 1;
    if (s == "road") onRoad.Append(seg + ":" + p[1] + ":" + p[2] + " "); if (s.StartsWith("other")) onRoad.Append("[" + seg + ":" + p[2] + " on " + s.Substring(6) + "] "); }
}
foreach (var kv in res) sb.Append("\"" + kv.Key + "\":" + kv.Value + ",");
sb.Append("\"on_road_items\":\"" + onRoad.ToString().Trim() + "\"}");
return sb.ToString();
