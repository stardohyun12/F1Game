// 과제 0433: 능선 구역(x 200~330, z -130~-20) 지형 사본 Mesh_TownV6_Terrain_0433 에
//  ① 건물 3채(정비소·보관소·우체국) 발밑 테라스 — 발자국+1.5 m 는 H 로 평평, 바깥 12 m 는 [H − e/3, H + e/2] 로 끼움(리서치 1안 연산자)
//  ② 모든 RoadSurface 길 띠: 내리기만 — 길 밑 = 면 −12 cm(0640, 예전 −15), 어깨 1 m = 면 −3 cm, 바깥 6 m 깎기 면 + (e−1)/2 (길 > 건물)
//  0616 고침(fb acb1 재오픈 — 보관소 앞 모서리 0.29 m 뜸): 지형 격자가 2 m 라 발자국 모서리 높이는 둘레 2 m 안 정점들의 보간이다.
//   그 정점을 길 어깨(−3 cm)가 깎아 모서리가 30.61 로 내려앉았다 → 판 평평 폭 1.5 → PADFLAT(=격자 2 m), 판 안에선 길 "밑"(e≤0)만 길이 이김(어깨는 판이 이김).
//   길 끝 너머는 캡슐 거리로 "길 밑 −15 cm" 가 돼 끝단이 23 cm 떠 보였다 → 끝 너머는 e = max(옆거리−반폭, 넘어간 길이) (어깨로 취급).
//  ③ 건물 y = H. 되돌리기 = TV6_Terrain 에 Mesh_TownV6_Terrain_ridge 다시 꽂기 + 건물 y 원래값(보고서).
var sb = new System.Text.StringBuilder(); bool dry = UnityEditor.SessionState.GetBool("p0433.dry", true);
var terrGo = GameObject.Find("TownV6_Gray/TownV6_Terrain"); var mf = terrGo.GetComponent<MeshFilter>(); var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_ridge.asset");   // 늘 _ridge 에서 다시 계산(두 번 돌려도 같은 결과)
//  0634 고침(fb acb1 2회차): 예전엔 씬 지형 콜라이더에 _ridge 를 꽂고 DRY 면 그대로 끝냈다 → 화면=_0433(테라스) / 콜라이더=_ridge(원지형) 로 어긋난 채 다른 작업이 씬을 저장,
//   차·레이캐스트가 건물 발밑 원지형(최대 2.9 m 위)을 밟았다. 이제 _ridge 는 숨긴 임시 콜라이더에만 꽂고 끝에 지운다 — 씬 콜라이더는 WET 저장 때만 바뀐다.
var tmpGo = new GameObject("p0433_tmp_ridge"); tmpGo.hideFlags = HideFlags.HideAndDontSave; tmpGo.transform.SetPositionAndRotation(terrGo.transform.position, terrGo.transform.rotation); tmpGo.transform.localScale = terrGo.transform.lossyScale;
var tmc = tmpGo.AddComponent<MeshCollider>(); tmc.sharedMesh = mesh;
try {
string dst = "Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_0433.asset"; string src = UnityEditor.AssetDatabase.GetAssetPath(mesh); sb.Append("src " + src + "\n");
var tr = terrGo.transform; var v = mesh.vertices; var w = new Vector3[v.Length]; for (int i = 0; i < v.Length; i++) w[i] = tr.TransformPoint(v[i]);
float x0 = 200, x1 = 330, z0 = -130, z1 = -20;
// ① 건물 발자국(로컬 축 사각형)
var root = GameObject.Find("TownV6_Gray/D198_Buildings").transform; var pads = new System.Collections.Generic.List<(Transform t, Vector3 c, Vector2 half, float H)>();
foreach (var nm in new[] { "D198_garage", "D198_depot", "D198_postoffice" }) {
  var t = root.Find(nm); var rs = t.GetComponentsInChildren<Renderer>(); Vector3 mn = Vector3.one * 1e9f, mx = -mn;
  foreach (var r in rs) { var b = r.bounds; for (int k = 0; k < 8; k++) { var p = t.InverseTransformPoint(new Vector3((k & 1) == 0 ? b.min.x : b.max.x, b.min.y, (k & 2) == 0 ? b.min.z : b.max.z)); mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); } }
  // 렌더러 AABB 를 로컬로 되돌리면 회전 때문에 커진다 → MeshFilter 로컬 bounds 로 다시
  mn = Vector3.one * 1e9f; mx = -mn;
  foreach (var f in t.GetComponentsInChildren<MeshFilter>()) { if (f.sharedMesh == null) continue; var b = f.sharedMesh.bounds; for (int k = 0; k < 8; k++) { var p = t.InverseTransformPoint(f.transform.TransformPoint(new Vector3((k & 1) == 0 ? b.min.x : b.max.x, (k & 4) == 0 ? b.min.y : b.max.y, (k & 2) == 0 ? b.min.z : b.max.z))); mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); } }
  var c = t.TransformPoint(new Vector3((mn.x + mx.x) * .5f, 0, (mn.z + mx.z) * .5f)); var half = new Vector2((mx.x - mn.x) * .5f, (mx.z - mn.z) * .5f);
  var hs = new System.Collections.Generic.List<float>();
  for (float a = -half.x + .5f; a < half.x; a += 1f) for (float b2 = -half.y + .5f; b2 < half.y; b2 += 1f) { var p = c + t.right * a + t.forward * b2; RaycastHit h; if (tmc.Raycast(new Ray(new Vector3(p.x, 200, p.z), Vector3.down), out h, 400)) hs.Add(h.point.y); }
  hs.Sort(); float H = hs[hs.Count / 2];
  // 3차: 보관소·우체국 정면(남) 2 m 앞이 ridge_up_c(30.49 / 31.43). 도현 03:56 높이(30.91 / 31.58)가 바로 그 길 높이+턱이라 그대로 두고 뒤쪽을 깎는다
  if (nm == "D198_depot") H = 30.91f; if (nm == "D198_postoffice") H = 31.58f; pads.Add((t, c, half, H));
  sb.Append(nm + " y " + t.position.y.ToString("F2") + " → H " + H.ToString("F2") + " (지형 " + hs[0].ToString("F2") + "~" + hs[hs.Count - 1].ToString("F2") + ") 발자국 " + (half * 2).ToString("F1") + "\n"); }
System.Func<Vector3, (int i, float e)> padE = p => { int bi = -1; float be = 1e9f; for (int i = 0; i < pads.Count; i++) { var d = p - pads[i].c; float lx = Mathf.Abs(Vector3.Dot(d, pads[i].t.right)) - pads[i].half.x, lz = Mathf.Abs(Vector3.Dot(d, pads[i].t.forward)) - pads[i].half.y; float e = Mathf.Max(lx, lz) > 0 ? new Vector2(Mathf.Max(lx, 0), Mathf.Max(lz, 0)).magnitude : Mathf.Max(lx, lz); if (e < be) { be = e; bi = i; } } return (bi, be); };
int padMoved = 0, roadMoved = 0, layerSkip = 0, fillMoved = 0; float padMax = 0, roadMax = 0, fillMax = 0; const float PADFLAT = 2f, UNDER = .12f;   // 0616: 격자 간격 — 발자국에 닿는 칸의 정점이 전부 H
for (int k = 0; k < w.Length; k++) { var p = w[k]; if (p.x < x0 || p.x > x1 || p.z < z0 || p.z > z1) continue;
  var pe = padE(p); if (pe.i < 0 || pe.e > PADFLAT + 12f) continue; float H = pads[pe.i].H; float e = pe.e - PADFLAT;
  float y = e <= 0 ? H : Mathf.Clamp(p.y, H - e / 3f, H + e / 2f); if (Mathf.Abs(y - p.y) > .001f) { padMax = Mathf.Max(padMax, Mathf.Abs(y - p.y)); w[k].y = y; padMoved++; } }
// ② 길 — 구역 안 RoadSurface 선분
var segs = new System.Collections.Generic.List<(Vector3 a, Vector3 b, float hw, bool s0, bool s1, int ri)>();   // s0/s1 = 이 선분 시작/끝이 길의 끝, ri = 길 번호(0616)
int nRoad = 0; foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) { var c = r.centerline; if (c == null) continue; for (int i = 1; i < c.Length; i++) if (c[i].x > x0 - 10 && c[i].x < x1 + 10 && c[i].z > z0 - 10 && c[i].z < z1 + 10) segs.Add((c[i - 1], c[i], r.width * .5f, i == 1, i == c.Length - 1, nRoad)); nRoad++; }
var endSeg = new int[nRoad]; var bestD = new float[nRoad];
for (int k = 0; k < w.Length; k++) { var p = w[k]; if (p.x < x0 || p.x > x1 || p.z < z0 || p.z > z1) continue;
  // 0616: 길마다 가장 가까운 선분이 "길 끝 너머로 클램프" 면 그 선분 하나만 끝 규칙으로 본다(선분이 1 m 라 끝 직전 선분 캡슐이 끝 너머 2 m 를 길 밑으로 깎았다)
  for (int i = 0; i < nRoad; i++) { endSeg[i] = -1; bestD[i] = 1e9f; }
  for (int si = 0; si < segs.Count; si++) { var s = segs[si]; var ab = s.b - s.a; ab.y = 0; var ap = p - s.a; ap.y = 0; float tr0 = Vector3.Dot(ap, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude); var d = p - Vector3.Lerp(s.a, s.b, Mathf.Clamp01(tr0)); d.y = 0; float dm = d.magnitude;
    if (dm < bestD[s.ri]) { bestD[s.ri] = dm; endSeg[s.ri] = ((s.s0 && tr0 < 0) || (s.s1 && tr0 > 1)) ? si : -1; } }
  bool inPad = padE(p).e <= PADFLAT;   // 1차 .3 → 보관소 앞 판 어깨가 길 깎기에 먹혀 1.26 m 뜸. 판(+1.5 m)에선 깎기 비탈 안 함, 길 띠+어깨 1 m 는 길이 이김(ridge_up_c 끝이 우체국 판에 1.1 m 묻혔음)   // 건물 발자국 안 = 건물이 덮으니 테라스 높이 유지(밑으로 지나는 길은 건물에 가려짐)
  float lim = 1e9f, lo = -1e9f; bool layer = false; float oE = 1e9f, oY = 0f;   // 0622(fb 4607): 띠·어깨 높이는 "주인 길"(e 가장 작은 길) 하나만 정한다
  for (int si = 0; si < segs.Count; si++) { var s = segs[si]; if (endSeg[s.ri] >= 0 && endSeg[s.ri] != si) continue; var ab = s.b - s.a; ab.y = 0; var ap = p - s.a; ap.y = 0; float tr0 = Vector3.Dot(ap, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude), t = Mathf.Clamp01(tr0); var q = Vector3.Lerp(s.a, s.b, t); var d = p - q; d.y = 0; float e = d.magnitude - s.hw;
    if ((s.s0 && tr0 < 0) || (s.s1 && tr0 > 1)) { float over = (tr0 < 0 ? -tr0 : tr0 - 1) * ab.magnitude; float side = Mathf.Sqrt(Mathf.Max(0, d.sqrMagnitude - over * over)); e = Mathf.Max(side - s.hw, over); }   // 0616: 길 끝 너머 = 어깨
    if (e > 7f || (inPad && e > 0f)) continue;   // 0616: 판(격자 2 m) 안에선 어깨도 판이 이김 — 길 밑만 길
    if (p.y - q.y > 3f && e <= 1f) { layer = true; continue; }   // 3 m 넘게 위 = 다른 층(다리·겹친 길) → 손대지 않음
    if (e <= 1f) { if (e < oE) { oE = e; oY = q.y; } continue; }
    lim = Mathf.Min(lim, padE(p).e <= 0 ? 1e9f : q.y + (e - 1f) / 2f);
    if (!inPad) lo = Mathf.Max(lo, q.y - .03f - (e - 1f) / 3f); }   // 0616: 어깨 메우기 3:1 — 원지형이 길보다 낮으면 길 끝·가장자리가 떠 보였다(291,-37 +0.23 m)
  // 0622: 예전엔 모든 길의 min 이라, 높이가 다른 두 길이 붙은 곳(250,-63 camo 24.75 옆 ridge_up_b 24.06 / 287,-65 갈림목)에서
  //  낮은 길의 "길 밑 −15 cm" 가 높은 길 띠 밑을 0.3~0.8 m 파서 가장자리가 떴다. 띠·어깨 안 정점은 주인 길 높이에 딱 맞춘다(올리기·내리기 둘 다).
  //  0640(fb 4607 3회차): 길 밑 −15 cm 가 센서 허용치(0.15)와 같아서, 격자 2 m 정점이 길 밑으로 잡힌 갈림목 바깥 모서리(285.4,−65.8)에서 가장자리가 +0.16 떴다 → 길 밑 −12 cm (−10 은 길 끝단(269.4,−63.0)에서 지형이 길을 2 cm 덮음 — 뜸·덮임 양쪽 여유 2~3 cm)
  if (oE <= 1f) { lim = lo = oE <= 0 ? oY - UNDER : oY - .03f; }
  if (layer) layerSkip++;
  if (!layer && w[k].y < Mathf.Min(lo, lim) - .001f) { fillMax = Mathf.Max(fillMax, Mathf.Min(lo, lim) - w[k].y); w[k].y = Mathf.Min(lo, lim); fillMoved++; }
  if (w[k].y > lim + .001f) { roadMax = Mathf.Max(roadMax, w[k].y - lim); w[k].y = lim; roadMoved++; } }
sb.Append("테라스 정점 " + padMoved + " (최대 " + padMax.ToString("F2") + " m) · 길 내림 " + roadMoved + " (최대 " + roadMax.ToString("F2") + " m) · 길 메우기 " + fillMoved + " (최대 " + fillMax.ToString("F2") + " m) · 층 건너뜀 " + layerSkip + "\n");
if (dry) return "DRY\n" + sb;
var m2 = UnityEngine.Object.Instantiate(mesh); m2.name = "TownV6_Terrain_0433"; var v2 = new Vector3[w.Length]; for (int i = 0; i < w.Length; i++) v2[i] = tr.InverseTransformPoint(w[i]);
m2.vertices = v2; m2.RecalculateNormals(); m2.RecalculateBounds();
if (UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(dst) != null) UnityEditor.AssetDatabase.DeleteAsset(dst);
UnityEditor.AssetDatabase.CreateAsset(m2, dst); mf.sharedMesh = m2; var mc = terrGo.GetComponent<MeshCollider>(); mc.sharedMesh = null; mc.sharedMesh = m2;
UnityEditor.EditorUtility.SetDirty(mf); UnityEditor.EditorUtility.SetDirty(mc);
foreach (var pd in pads) { UnityEditor.Undo.RecordObject(pd.t, "0433"); var pp = pd.t.position; pp.y = pd.H; pd.t.position = pp; UnityEditor.EditorUtility.SetDirty(pd.t); }
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); UnityEditor.AssetDatabase.SaveAssets();
return "SAVED\n" + sb;
} finally { UnityEngine.Object.DestroyImmediate(tmpGo); }
