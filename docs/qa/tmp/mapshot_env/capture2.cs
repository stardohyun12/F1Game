// mapshot 2020 (v2, capture.cs 1856 판 기반):  Map_TownV6(사본 에디터) 에 배경 건물 8채를 임시 배치 → 없음/있음 쌍 촬영 → 전부 원복. 씬 저장 없음.
// OSM 회색 건물은 Walls_0/Roofs_0 한 덩어리 메시라서, 8채 터 안의 삼각형만 뺀 임시 메시로 바꿔 끼웠다가 되돌린다.
var log = new System.Text.StringBuilder();
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Game_shot/F1/Assets")) return "FAIL: 사본 에디터가 아님";
if (Application.isPlaying) return "FAIL: play";
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.name != "Map_TownV6") return "FAIL: scene " + scene.name;
const string D = "Assets/_TmpEnv";
string repo = "D:/Github/F1Game/";
string outDir = repo + "art/_review/2026-10-30-env-first-v2/unity/";
System.IO.Directory.CreateDirectory(outDir);
var man = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(repo + "art/export/env-first-v2/manifest.json"));
var terrain = new System.Collections.Generic.List<Collider>();
foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)) if (c.enabled && c.gameObject.activeInHierarchy && (c is TerrainCollider || c.name.Contains("Terrain"))) terrain.Add(c);
System.Func<float, float, float> Ground = (x, z) => { float best = float.NaN; var ray = new Ray(new Vector3(x, 500, z), Vector3.down); foreach (var c in terrain) { RaycastHit h; if (c.Raycast(ray, out h, 1000) && (float.IsNaN(best) || h.point.y > best)) best = h.point.y; } if (float.IsNaN(best)) throw new System.Exception("Ground miss " + x + "," + z); return best; };

// ---- 카메라 정의 (카메라 B: 카트 뒤 5.6m · 위 2.8m · FOV 64, 카트 위 1.2m + 앞 4m 를 본다)
var views = new System.Collections.Generic.List<(string name, Vector3 pos, Vector3 tgt, bool ortho, float size)>();
// 1판과 같은 5 카메라: art/_review/2026-10-30-env-first/unity/evidence.txt 의 절대 pos/tgt
views.Add(("cart_home_1", new Vector3(200.0f, 22.5f, -79.0f), new Vector3(212.0f, 30.6f, -57.0f), false, 0));
views.Add(("cart_home_2", new Vector3(185.0f, 29.8f, -52.0f), new Vector3(184.0f, 36.1f, -28.0f), false, 0));
views.Add(("cart_route_1", new Vector3(235.0f, 26.5f, -76.0f), new Vector3(263.0f, 27.2f, -73.5f), false, 0));
views.Add(("cart_route_2", new Vector3(268.0f, 27.7f, -50.0f), new Vector3(252.0f, 38.8f, -20.0f), false, 0));
views.Add(("top", new Vector3(210, 300, -55), new Vector3(210, 0, -55), true, 56f));

System.Func<Vector2, Vector2[], float, bool> NearP = (q, poly, tol) => { bool inside = false; float md = float.MaxValue; for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++) { var a = poly[i]; var c = poly[j]; if (((a.y > q.y) != (c.y > q.y)) && (q.x < (c.x - a.x) * (q.y - a.y) / (c.y - a.y) + a.x)) inside = !inside; var ab = c - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); md = Mathf.Min(md, (q - (a + ab * t)).magnitude); } return inside || md <= tol; };
// ---- 임시 오브젝트
var owned = new System.Collections.Generic.List<UnityEngine.Object>();
var root = new GameObject("_ENVSHOT") { hideFlags = HideFlags.DontSave }; owned.Add(root); root.SetActive(false);
var tris = 0; var saved = new System.Collections.Generic.List<(MeshFilter mf, Mesh orig)>();
Camera cam = null;
try {
  // 건물 배치
  foreach (var b in man["buildings"]) {
    string nm = (string)b["name"]; var asset = AssetDatabase.LoadAssetAtPath<GameObject>(D + "/" + nm + ".fbx"); if (!asset) throw new System.Exception("missing " + nm);
    float px = (float)b["pivot_xz"][0], pz = (float)b["pivot_xz"][1];
        var go = UnityEngine.Object.Instantiate(asset); go.name = nm; go.hideFlags = HideFlags.DontSave; go.transform.SetParent(root.transform, false);
    // FBX 좌우 부호: 바닥(y<5cm) 점들이 터 다각형 안/근처(1m)에 더 많이 들어가는 부호
    var lot = new System.Collections.Generic.List<Vector2>(); foreach (var p in b["poly"]) lot.Add(new Vector2((float)p[0], (float)p[1]));
    var lotA = lot.ToArray();
    var pts = new System.Collections.Generic.List<Vector2>(); float minY = float.MaxValue;
    foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) foreach (var p0 in mf.sharedMesh.vertices) minY = Mathf.Min(minY, mf.transform.TransformPoint(p0).y);
    foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) foreach (var p0 in mf.sharedMesh.vertices) { var p = mf.transform.TransformPoint(p0); if (p.y < minY + .05f) pts.Add(new Vector2(p.x, p.z)); }
    float best = -1, second = -1; var sg = Vector2.one;
    foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f }) { int ok = 0; foreach (var p in pts) if (NearP(new Vector2(p.x * sx + px, p.y * sz + pz), lotA, 1.0f)) ok++; float fr = pts.Count > 0 ? (float)ok / pts.Count : 0; if (fr > best) { second = best; best = fr; sg = new Vector2(sx, sz); } else if (fr > second) second = fr; }
    { float frDef = 0; int okd = 0; foreach (var p in pts) if (NearP(new Vector2(-p.x + px, p.y + pz), lotA, 1.0f)) okd++; frDef = pts.Count > 0 ? (float)okd / pts.Count : 0;
      if (frDef >= best - .02f) { sg = new Vector2(-1f, 1f); best = frDef; } } // FBX 임포트 관례 (-1,1): 대칭 터에서 동점이면 이쪽
    if (pts.Count == 0 || best < .85f) throw new System.Exception("footprint check failed " + nm + " inside-frac " + best + " pts " + pts.Count);
    var ls0 = go.transform.localScale; go.transform.localScale = new Vector3(ls0.x * sg.x, ls0.y, ls0.z * sg.y); // FBX 루트 스케일 100 유지(전에는 덮어써서 1/100 크기였다)
    // 접지: 터 꼭짓점·가운데 지면 높이 → 가장 낮은 곳에 바닥(윗쪽은 묻힘)
    float gmin = float.MaxValue, gmax = float.MinValue; foreach (var q in lot) { float g = Ground(q.x, q.y); gmin = Mathf.Min(gmin, g); gmax = Mathf.Max(gmax, g); }
    go.transform.position = new Vector3(px, gmin, pz);
    foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { var old = r.sharedMaterials; var mm = new Material[old.Length];
      for (int i = 0; i < old.Length; i++) { string mn = old[i] ? old[i].name : ""; string slot = new[] { "retain_stone", "wall", "trim", "roof", "floor" }.FirstOrDefault(s => mn.Contains(s)); if (slot == null) throw new System.Exception("unmapped material " + mn + " on " + nm); mm[i] = AssetDatabase.LoadAssetAtPath<Material>(D + "/Env_" + slot + ".mat"); }
      r.sharedMaterials = mm; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
    // 집마다 자기 중심 지면에 앉힌다(루트 하나를 최저점에 맞추면 경사 터에서 윗집 문이 2~3m 묻힌다). 루트 y=지면일 때 설계 의도: 층바닥 = 지면 + 기단 0.9m
    foreach (var h in b["houses"]) { float cx = 0, cz = 0; int n4 = 0; foreach (var p in h["poly"]) { cx += (float)p[0]; cz += (float)p[1]; n4++; } cx /= n4; cz /= n4; string key = (string)h["key"];
      float dy = Ground(cx, cz) - gmin; foreach (var tr in go.GetComponentsInChildren<Transform>(true)) if (tr != go.transform && tr.name.Contains("_" + key + "_") && tr.GetComponent<MeshFilter>() != null) tr.position += Vector3.up * dy; }
    { var hs = new System.Text.StringBuilder(); foreach (var h in b["houses"]) { float cx = 0, cz = 0; int n4 = 0; foreach (var p in h["poly"]) { cx += (float)p[0]; cz += (float)p[1]; n4++; } cx /= n4; cz /= n4; string key = (string)h["key"];
        float fy = float.NaN; foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) if (mf.name.EndsWith("_" + key + "_BODY")) { fy = float.MaxValue; foreach (var p0 in mf.sharedMesh.vertices) fy = Mathf.Min(fy, mf.transform.TransformPoint(p0).y); }
        hs.Append(" " + key + ":floor-ground=" + (fy - Ground(cx, cz)).ToString("F2")); }
      log.AppendLine("  houses(층바닥-지면 중심, +뜸/-묻힘)" + hs); }
    log.AppendLine(nm + " signs " + sg + " insideFrac " + best.ToString("F3") + " (차점 " + second.ToString("F3") + ")" + " ground " + gmin.ToString("F2") + ".." + gmax.ToString("F2") + " (spread " + (gmax - gmin).ToString("F2") + ") manifest y " + (float)b["y"]);
  }
  // OSM 터 구멍 메시
  var polysW = new System.Collections.Generic.List<Vector2[]>();
  foreach (var b in man["buildings"]) { var l = new System.Collections.Generic.List<Vector2>(); foreach (var p in b["poly"]) l.Add(new Vector2((float)p[0], (float)p[1])); polysW.Add(l.ToArray()); }
  System.Func<Vector2, Vector2[], float, bool> Near = (q, poly, tol) => { bool inside = false; float md = float.MaxValue; for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++) { var a = poly[i]; var c = poly[j]; if (((a.y > q.y) != (c.y > q.y)) && (q.x < (c.x - a.x) * (q.y - a.y) / (c.y - a.y) + a.x)) inside = !inside; var ab = c - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); md = Mathf.Min(md, (q - (a + ab * t)).magnitude); } return inside || md <= tol; };
  var holeMesh = new System.Collections.Generic.Dictionary<MeshFilter, Mesh>();
  foreach (var nmo in new[] { "Walls_0", "Roofs_0" }) {
    var mfo = GameObject.Find("TownV6_Gray/Buildings/" + nmo).GetComponent<MeshFilter>(); var om = mfo.sharedMesh; var vs = om.vertices; var ix = om.triangles; var keep = new System.Collections.Generic.List<int>(); int removed = 0;
    for (int i = 0; i < ix.Length; i += 3) { var cen = (vs[ix[i]] + vs[ix[i + 1]] + vs[ix[i + 2]]) / 3f; var q = new Vector2(cen.x, cen.z); bool hit = false; foreach (var pl in polysW) if (Near(q, pl, 0.12f)) { hit = true; break; } if (hit) removed++; else { keep.Add(ix[i]); keep.Add(ix[i + 1]); keep.Add(ix[i + 2]); } }
    var nm2 = new Mesh() { name = nmo + "_hole", hideFlags = HideFlags.DontSave, indexFormat = om.indexFormat }; nm2.vertices = vs; nm2.normals = om.normals; nm2.uv = om.uv; nm2.colors = om.colors; nm2.triangles = keep.ToArray(); nm2.RecalculateBounds(); owned.Add(nm2);
    holeMesh[mfo] = nm2; saved.Add((mfo, om)); log.AppendLine("OSM " + nmo + " 삼각형 " + (ix.Length / 3) + " → 제거 " + removed);
  }
  // 카메라
  var sview = UnityEditor.SceneView.lastActiveSceneView; var src = sview != null ? sview.camera : Camera.main;
  var camGo = new GameObject("_ENVSHOT_CAM") { hideFlags = HideFlags.HideAndDontSave }; owned.Add(camGo); cam = camGo.AddComponent<Camera>(); cam.CopyFrom(src); cam.cameraType = CameraType.Game; cam.enabled = false;
  cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.56f, .72f, .90f, 1); cam.nearClipPlane = 0.1f; cam.farClipPlane = 900f; // 1판 촬영과 같은 하늘(씬 뷰 카메라는 skybox 없음 → 회색)
  log.AppendLine("cam clearFlags=" + cam.clearFlags + " bg=" + cam.backgroundColor + " near=" + cam.nearClipPlane + " far=" + cam.farClipPlane + " fog=" + RenderSettings.fog + " skybox=" + (RenderSettings.skybox ? RenderSettings.skybox.name : "none") + " srcIsSceneView=" + (sview != null));
  log.AppendLine("A7Shade=" + Shader.GetGlobalColor("_A7Shade") + " _A7T=" + Shader.GetGlobalVector("_A7T") + " _A7Mid=" + Shader.GetGlobalFloat("_A7Mid") + " aShadeObj=" + (UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>() != null));
  foreach (var v in views) {
    cam.orthographic = v.ortho; if (v.ortho) cam.orthographicSize = v.size; else cam.fieldOfView = 64f;
    cam.transform.position = v.pos; cam.transform.rotation = v.ortho ? Quaternion.LookRotation(Vector3.down, Vector3.forward) : Quaternion.LookRotation(v.tgt - v.pos, Vector3.up);
    // 없음
    foreach (var kv in holeMesh) kv.Key.sharedMesh = saved.First(s => s.mf == kv.Key).orig; root.SetActive(false);
    if (!PitCrewPlan.PlanIO.RenderPng(cam, outDir + v.name + "_without.png", 1280, 720, null)) throw new System.Exception("capture fail " + v.name);
    // 있음
    foreach (var kv in holeMesh) kv.Key.sharedMesh = kv.Value; root.SetActive(true);
    if (!PitCrewPlan.PlanIO.RenderPng(cam, outDir + v.name + "_with.png", 1280, 720, null)) throw new System.Exception("capture fail " + v.name);
    log.AppendLine(v.name + " pos=" + v.pos.ToString("F1") + " tgt=" + v.tgt.ToString("F1") + (v.ortho ? " ortho " + v.size : " fov 64") + " 1280x720");
  }
  return log.ToString();
} finally {
  foreach (var s in saved) if (s.mf) s.mf.sharedMesh = s.orig;
  for (int i = owned.Count - 1; i >= 0; i--) if (owned[i]) UnityEngine.Object.DestroyImmediate(owned[i]);
  int rem = 0; foreach (var o in Resources.FindObjectsOfTypeAll<GameObject>()) if (o.name.StartsWith("_ENVSHOT")) rem++;
  var sc2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  System.IO.File.WriteAllText(outDir + "evidence.txt", log.ToString() + "cleanup remaining=" + rem + " dirty=" + sc2.isDirty + "\n");
}
