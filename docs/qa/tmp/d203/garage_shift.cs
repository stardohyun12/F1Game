// 정비소를 북쪽 4m: 패드+여유가 북동 길 정점에서 4m 이상 떨어지게. 바닥은 원본 지형 평균으로 다시
var root = GameObject.Find("TownV6_Gray").transform; var terMC = root.Find("TownV6_Terrain").GetComponent<MeshCollider>();
var cur = terMC.sharedMesh; terMC.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_loopfix.asset"); Physics.SyncTransforms();
var g = root.Find("D198_Buildings/D198_garage"); g.position += new Vector3(0, 0, 4f);
var b = new Bounds(); bool f = true; foreach (var r in g.GetComponentsInChildren<Renderer>()) { if (f) { b = r.bounds; f = false; } else b.Encapsulate(r.bounds); }
float s = 0, lo = 999, hi = -999; int n = 0;
for (float x = b.min.x; x <= b.max.x; x += 1) for (float z = b.min.z; z <= b.max.z; z += 1) { terMC.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out var h, 600); s += h.point.y; n++; lo = Mathf.Min(lo, h.point.y); hi = Mathf.Max(hi, h.point.y); }
float fl = Mathf.Round(s / n * 10f) / 10f; g.position = new Vector3(g.position.x, fl, g.position.z);
terMC.sharedMesh = cur; Physics.SyncTransforms();
// 새 자리와 겹치는(아직 안 지운) OSM 폴리곤
var polys = System.IO.File.ReadAllLines("D:/Github/F1Game/docs/qa/tmp/d203/polys.txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return (id: a[0], pts: a.Skip(2).Select(q => { var p = q.Split(','); return new Vector2(float.Parse(p[0]), float.Parse(p[1])); }).ToArray()); }).ToList();
var rr = Rect.MinMaxRect(b.min.x - 1.5f, b.min.z - 1.5f, b.max.x + 1.5f, b.max.z + 1.5f);
var hit = polys.Where(p => p.pts.Any(q => rr.Contains(q))).Select(p => p.id);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(g.gameObject.scene);
return "garage pos " + g.position.ToString("F1") + " foot x" + b.min.x.ToString("F1") + ".." + b.max.x.ToString("F1") + " z" + b.min.z.ToString("F1") + ".." + b.max.z.ToString("F1") + " floor " + fl + " orig " + lo.ToString("F1") + ".." + hi.ToString("F1") + " osmInRect " + string.Join(",", hit);
