var sb = new System.Text.StringBuilder();
var roads = new System.Collections.Generic.List<RoadSurface>();
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(UnityEngine.FindObjectsSortMode.None))
  if (r.centerline != null && r.centerline.Length >= 2) roads.Add(r);
roads.Sort((a, b) => string.CompareOrdinal(a.roadId, b.roadId));
sb.AppendLine("roads=" + roads.Count);
var nodes = new System.Collections.Generic.List<UnityEngine.Vector3>(); var rid = new System.Collections.Generic.List<int>();
for (int i = 0; i < roads.Count; i++) foreach (var p in roads[i].centerline) { nodes.Add(p); rid.Add(i); }
var adj = new System.Collections.Generic.List<(int, float)>[nodes.Count];
for (int i = 0; i < nodes.Count; i++) adj[i] = new System.Collections.Generic.List<(int, float)>();
for (int i = 0; i + 1 < nodes.Count; i++) if (rid[i] == rid[i + 1]) { float d = UnityEngine.Vector3.Distance(nodes[i], nodes[i + 1]); adj[i].Add((i + 1, d)); adj[i + 1].Add((i, d)); }
for (int i = 0; i < nodes.Count; i++) for (int j = i + 1; j < nodes.Count; j++) if (rid[i] != rid[j]) { float d = UnityEngine.Vector3.Distance(nodes[i], nodes[j]); if (d < 10f && UnityEngine.Mathf.Abs(nodes[i].y - nodes[j].y) < 3f) { adj[i].Add((j, d * 2f)); adj[j].Add((i, d * 2f)); } }
System.Func<UnityEngine.Vector3, int> near = q => { int bi = 0; float bd = float.MaxValue; for (int i = 0; i < nodes.Count; i++) { var v = nodes[i] - q; float d = v.sqrMagnitude; if (d < bd) { bd = d; bi = i; } } return bi; };
var from = new UnityEngine.Vector3(197.3f, 24.5f, -77.9f);
var to = new UnityEngine.Vector3(277f, 30f, -37f);
int na = near(from), nb = near(to);
sb.AppendLine("start node " + nodes[na].ToString("F1") + " road=" + roads[rid[na]].roadId + "/" + roads[rid[na]].kind + "  end node " + nodes[nb].ToString("F1") + " road=" + roads[rid[nb]].roadId + "/" + roads[rid[nb]].kind);
var dist = new float[nodes.Count]; var prev = new int[nodes.Count];
for (int i = 0; i < nodes.Count; i++) { dist[i] = float.MaxValue; prev[i] = -1; }
var pq = new System.Collections.Generic.SortedSet<(float, int)>(); dist[na] = 0; pq.Add((0, na));
while (pq.Count > 0) { var (d, u) = pq.Min; pq.Remove(pq.Min); if (u == nb) break; if (d > dist[u]) continue; foreach (var (w, c) in adj[u]) { float nd = d + c; if (nd < dist[w]) { if (dist[w] < float.MaxValue) pq.Remove((dist[w], w)); dist[w] = nd; prev[w] = u; pq.Add((nd, w)); } } }
if (prev[nb] < 0) { sb.AppendLine("NO PATH"); return sb.ToString(); }
var ids = new System.Collections.Generic.List<int>(); for (int x = nb; x >= 0; x = prev[x]) ids.Add(x); ids.Reverse();
sb.AppendLine("cost=" + dist[nb].ToString("F0") + " nodes=" + ids.Count);
string lastRoad = ""; UnityEngine.Vector3 last = nodes[ids[0]];
for (int k = 0; k < ids.Count; k++) { int i = ids[k]; string rn = roads[rid[i]].roadId + "/" + roads[rid[i]].kind; if (true) { sb.AppendLine(nodes[i].ToString("F1") + " " + rn); lastRoad = rn; last = nodes[i]; } }
return sb.ToString();
