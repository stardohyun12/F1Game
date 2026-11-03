// 1935: 메운 블록(A·B) 안 물체 중 ParcelBuild 가 새로 만든 것(Parcels_Plan)·지형·길 말고, 밑면이 지형보다 0.3 m 넘게 묻힌 것.
Physics.SyncTransforms();
var top = GameObject.Find("TownV6_Gray").transform; var tc = top.Find("TownV6_Terrain").GetComponent<MeshCollider>();
var boxes = new[] { new Rect(85, -5, 60, 60), new Rect(80, -80, 65, 65) };   // A (115,23) · B (112,-47) 대략
var sb = new System.Text.StringBuilder(); int n = 0, bad = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
    var b = r.bounds; var c = new Vector2(b.center.x, b.center.z); if (!boxes[0].Contains(c) && !boxes[1].Contains(c)) continue;
    var t = r.transform; var rootN = t.root.name; string path = t.name; for (var q = t.parent; q != null; q = q.parent) path = q.name + "/" + path;
    if (path.StartsWith("Parcels_Plan") || path.Contains("TownV6_Terrain") || path.Contains("RoadsV2") || b.size.x > 80) continue;
    n++;
    float g = tc.Raycast(new Ray(new Vector3(b.center.x, 400, b.center.z), Vector3.down), out var h, 800) ? h.point.y : float.NaN;
    if (float.IsNaN(g)) continue;
    if (b.max.y < g + .05f || b.min.y < g - .3f && b.size.y < 3) { bad++; if (bad <= 40) sb.Append($"{path} min {b.min.y:F2} max {b.max.y:F2} ground {g:F2}\n"); }
}
return $"checked {n} buried {bad}\n" + sb;
