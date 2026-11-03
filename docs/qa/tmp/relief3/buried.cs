// 2253: 맵 전체 — ParcelBuild 가 새로 만든 것(Parcels_Plan)·지형·길 말고, 밑면이 지형보다 0.3 m 넘게 묻히거나 통째로 묻힌 물체(1935 buried.cs 를 맵 전체로).
Physics.SyncTransforms();
var top = GameObject.Find("TownV6_Gray").transform; var tc = top.Find("TownV6_Terrain").GetComponent<MeshCollider>();
var sb = new System.Text.StringBuilder(); int n = 0, bad = 0, whole = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
    var b = r.bounds; var t = r.transform; string path = t.name; for (var q = t.parent; q != null; q = q.parent) path = q.name + "/" + path;
    if (path.StartsWith("Parcels_Plan") || path.Contains("TownV6_Terrain") || path.Contains("RoadsV2") || b.size.x > 80 || b.size.z > 80) continue;
    float g = tc.Raycast(new Ray(new Vector3(b.center.x, 400, b.center.z), Vector3.down), out var h, 800) ? h.point.y : float.NaN;
    if (float.IsNaN(g)) continue; n++;
    bool w = b.max.y < g + .05f; if (w) whole++;
    if (w || b.min.y < g - .3f && b.size.y < 3) { bad++; if (bad <= 60) sb.Append($"{path} c({b.center.x:F0},{b.center.z:F0}) min {b.min.y:F2} max {b.max.y:F2} ground {g:F2}\n"); }
}
return $"checked {n} buried {bad} (whole {whole})\n" + sb;
