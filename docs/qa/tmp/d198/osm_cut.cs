// D-198 — 합쳐진 OSM 건물 메시(Walls_*/Roofs_*)에서 osm_polys.txt 건물의 삼각형을 뺌
var sb = new System.Text.StringBuilder();
string f = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/d198/osm_polys.txt"));
var polys = System.IO.File.ReadAllLines(f).Where(l => l.Trim() != "").Select(l => l.Split(' ').Select(s => { var a = s.Split(','); return new Vector2(float.Parse(a[0]), float.Parse(a[1])); }).ToList()).ToList();
bool OnPoly(Vector3 w) { foreach (var p in polys) foreach (var q in p) if (Mathf.Abs(q.x - w.x) < 0.01f && Mathf.Abs(q.y - w.z) < 0.01f) return true; return false; }
var bld = GameObject.Find("TownV6_Gray").transform.Find("Buildings");
foreach (Transform c in bld) {
  var mf = c.GetComponent<MeshFilter>(); var m = mf.sharedMesh; var path = UnityEditor.AssetDatabase.GetAssetPath(m);
  var vs = m.vertices; var tr = m.triangles; var keep = new List<int>(); int cut = 0;
  for (int i = 0; i < tr.Length; i += 3) {
    if (OnPoly(c.TransformPoint(vs[tr[i]])) && OnPoly(c.TransformPoint(vs[tr[i + 1]])) && OnPoly(c.TransformPoint(vs[tr[i + 2]]))) { cut++; continue; }
    keep.Add(tr[i]); keep.Add(tr[i + 1]); keep.Add(tr[i + 2]);
  }
  if (cut == 0) { sb.Append(c.name + " cut 0\n"); continue; }
  if (string.IsNullOrEmpty(path)) { m = UnityEngine.Object.Instantiate(m); m.name = mf.sharedMesh.name; mf.sharedMesh = m; }
  m.SetTriangles(keep, 0); m.RecalculateBounds(); UnityEditor.EditorUtility.SetDirty(m);
  var mc = c.GetComponent<MeshCollider>(); if (mc) { mc.sharedMesh = null; mc.sharedMesh = m; }
  sb.Append(c.name + " cut " + cut + " tris, asset=" + (path == "" ? "(scene)" : path) + "\n");
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
return sb.ToString();
