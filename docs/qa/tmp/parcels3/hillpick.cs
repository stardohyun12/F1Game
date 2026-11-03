// 0141: hill_eye 자리 고르기 — 반지하(C1)/2단(C2) 집이 가장 많이 보이는 길 위 점
var root = UnityEngine.GameObject.Find("Parcels_Plan"); if (root == null) return "no Parcels_Plan";
var cs = new System.Collections.Generic.List<UnityEngine.Vector3>();
foreach (UnityEngine.Transform t in root.GetComponentsInChildren<UnityEngine.Transform>()) { if (t.parent == null) continue; var n = t.name; if (n.StartsWith("P") && (n.EndsWith("_C1") || n.EndsWith("_C2"))) cs.Add(t.position); }
var rv = UnityEngine.GameObject.Find("TownV6_Gray").transform.Find("RoadsV2");
float best = -1; UnityEngine.Vector3 bp = default, bt = default;
foreach (UnityEngine.Transform ch in rv) { if (!ch.gameObject.activeInHierarchy) continue; var rs = ch.GetComponent<RoadSurface>(); if (rs == null || rs.centerline == null) continue;
  foreach (var c in rs.centerline) { int k = 0; UnityEngine.Vector3 s = default; foreach (var m in cs) { var d = new UnityEngine.Vector2(m.x - c.x, m.z - c.z).magnitude; if (d > 8 && d < 35) { k++; s += m; } }
    if (k > best) { best = k; bp = c; bt = s / UnityEngine.Mathf.Max(1, k); } } }
System.IO.File.WriteAllText("D:/Github/F1Game/docs/qa/tmp/parcels3/hill.txt", bp.x.ToString("F1") + "," + bp.z.ToString("F1") + "," + bt.x.ToString("F1") + "," + bt.z.ToString("F1"));
return "C1/C2 " + cs.Count + " best " + best + " at " + bp.ToString("F1") + " -> " + bt.ToString("F1");
