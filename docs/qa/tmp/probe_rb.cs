var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var c = car.GetComponentInChildren<BoxCollider>(); var s = new System.Text.StringBuilder();
s.Append($"kin {rb.isKinematic} sleep {rb.IsSleeping()} cons {rb.constraints} mass {rb.mass} v {rb.linearVelocity} interp {rb.interpolation} box ctr {c.center} size {c.size} local {c.transform.localPosition} | ");
foreach (var o in Physics.OverlapBox(c.bounds.center, c.bounds.extents + Vector3.one * .1f))
  if (o != c && Physics.ComputePenetration(c, c.transform.position, c.transform.rotation, o, o.transform.position, o.transform.rotation, out var d, out var dist)) s.Append($"{o.name} {d} {dist:F3}; ");
var mc = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(x => x.roadId == "camo_309369480_161").GetComponent<MeshCollider>();
s.Append($" convex {mc.convex} tris {mc.sharedMesh.triangles.Length / 3} verts {mc.sharedMesh.vertexCount} cook {mc.cookingOptions}");
s.Append($" | others: {string.Join(",", car.GetComponents<MonoBehaviour>().Select(m => m.GetType().Name))}");
return s.ToString();
