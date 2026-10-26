// 0444-2b: Walls_0 이 뭔지 + 길별로 몇 m 겹치나(2 m 표본 수), 폭을 길 폭의 60% 로 좁혀서(가장자리 옹벽 제외)
var s = new System.Text.StringBuilder();
foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
  if (c.name == "Walls_0") s.Append($"Walls_0 parent={(c.transform.parent ? c.transform.parent.name : "-")} bounds={c.bounds.size} verts={(c is MeshCollider mc && mc.sharedMesh ? mc.sharedMesh.vertexCount : -1)}\n");
var per = new System.Collections.Generic.Dictionary<string, int>(); var at = new System.Collections.Generic.Dictionary<string, string>();
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
{
  var cl = r.centerline; if (cl == null || cl.Length < 2) continue;
  for (int i = 0; i + 1 < cl.Length; i += 2)
  {
    var a = cl[i]; var d = cl[i + 1] - a; d.y = 0; if (d.sqrMagnitude < 1e-4f) continue;
    var hs = Physics.OverlapBox(a + Vector3.up * 1.4f, new Vector3(r.width * .3f, 1.1f, .5f), Quaternion.LookRotation(d.normalized));
    foreach (var h in hs)
    {
      var nm = h.name; if (nm.StartsWith("ROAD_") || nm.StartsWith("BUMP_") || nm.Contains("Terrain") || h.isTrigger) continue;
      var k = r.roadId + " × " + nm; per[k] = per.TryGetValue(k, out var q) ? q + 1 : 1; if (!at.ContainsKey(k)) at[k] = $"({a.x:F0},{a.z:F0})";
    }
  }
}
var l = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>(per); l.Sort((x, y) => y.Value.CompareTo(x.Value));
s.Append($"중앙 60% 폭 겹침: {l.Count}쌍\n");
foreach (var kv in l) s.Append($"{kv.Key} 표본{kv.Value}(≈{kv.Value * 2}m) 첫 {at[kv.Key]}\n");
return s.ToString();
