// D-203 조사: 씬, 핵심 건물 인스턴스, 능선 주변 지형 높이·길·OSM 집
var sb = new System.Text.StringBuilder();
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
sb.Append("scene " + sc.path + " dirty " + sc.isDirty + "\n");
foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
  var p = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
  if (p.Contains("Library/Buildings")) {
    var b = new Bounds(go.transform.position, Vector3.zero); foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
    sb.Append(go.name + " path=" + GetPath(go.transform) + " pos " + go.transform.position.ToString("F1") + " yaw " + go.transform.eulerAngles.y.ToString("F0") + " scale " + go.transform.localScale.ToString("F2") + " bsize " + b.size.ToString("F1") + " bcenterOff " + (b.center - go.transform.position).ToString("F1") + " src " + p + "\n");
  }
}
string GetPath(Transform t) { return t.parent ? GetPath(t.parent) + "/" + t.name : t.name; }
var root = GameObject.Find("TownV6_Gray").transform;
var tc = root.Find("TownV6_Terrain").GetComponent<Collider>();
sb.Append("terrain mesh " + root.Find("TownV6_Terrain").GetComponent<MeshFilter>().sharedMesh.name + " verts " + root.Find("TownV6_Terrain").GetComponent<MeshFilter>().sharedMesh.vertexCount + "\n");
sb.Append("height grid x 140..240 step10, z -110..-10 step10 (terrain only):\n");
for (float z = -10; z >= -110; z -= 10) { sb.Append("z" + z + ": "); for (float x = 140; x <= 240; x += 10) { if (tc.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out var h, 600)) sb.Append(h.point.y.ToString("F1") + " "); else sb.Append("--- "); } sb.Append("\n"); }
sb.Append("roads children: "); foreach (Transform r in root.Find("Roads")) sb.Append(r.name + " "); sb.Append("\n");
// 능선 주변 반경 60m 안 도로 위 점(위에서 쏴서 Roads 맞는 곳)
var roadPts = new List<string>();
for (float x = 130; x <= 250; x += 4) for (float z = -120; z <= 0; z += 4) { var hs = Physics.RaycastAll(new Vector3(x, 300, z), Vector3.down, 600); if (hs.Length == 0) continue; var top = hs.OrderBy(h => h.distance).First(); var n = top.collider.transform; if (n.parent && n.parent.name == "Roads") roadPts.Add(x + "," + z); }
sb.Append("road pts(4m grid) " + roadPts.Count + ": " + string.Join(" ", roadPts) + "\n");
// OSM polys near
var polys = System.IO.File.ReadAllLines("D:/Github/F1Game/docs/qa/tmp/hill/polys.txt").Where(l => l.Trim() != "").ToList();
foreach (var l in polys) { var a = l.Split(' '); var pts = a.Skip(2).Select(s => { var q = s.Split(','); return new Vector2(float.Parse(q[0]), float.Parse(q[1])); }).ToArray(); var c = new Vector2(pts.Average(q => q.x), pts.Average(q => q.y)); if (Vector2.Distance(c, new Vector2(185, -61)) < 70) sb.Append("osm " + a[0] + " " + a[1] + " c " + c.ToString("F1") + "\n"); }
return sb.ToString();
