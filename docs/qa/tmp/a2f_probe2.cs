var sb = new System.Text.StringBuilder();
foreach (var n in new[] { "Road", "Terrain", "Headland" }) { var g = GameObject.Find(n); if (!g) continue; sb.AppendLine(n + " col " + (g.GetComponent<Collider>() ? g.GetComponent<Collider>().GetType().Name : "none") + " parent " + (g.transform.parent ? g.transform.parent.name : "-") + " pos " + g.transform.position + " rot " + g.transform.eulerAngles + " scale " + g.transform.lossyScale); }
var road = GameObject.Find("Road").GetComponent<MeshFilter>().sharedMesh; var v = road.vertices; var u = road.uv;
for (int i = 0; i < 6; i++) sb.AppendLine(i + " " + v[i] + " " + u[i]);
sb.AppendLine("last " + v[v.Length - 1] + " " + u[u.Length - 1] + " asset " + AssetDatabase.GetAssetPath(road));
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (r.sharedMaterial && !r.sharedMaterial.name.StartsWith("SA2d_")) sb.AppendLine("other " + r.name + " " + r.sharedMaterial.name);
return sb.ToString();
