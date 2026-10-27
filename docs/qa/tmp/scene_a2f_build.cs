// 과제 1110 — Scene_A2f_Town = A2d 복사. 배치·카메라·렌더링(720 Nearest·그림자 없음·unlit·안개) 그대로, A2d 에서 남은 것만:
// (1) 길: 새 텍스처(GenF/Gen/road_a2f.png) + 길 메시 UV 를 폭 0..1·20 m 반복으로 다시 매김  (2) 풀: 연두 3단 + 길가 마른 풀 띠(새 메시 DryVerge)
// (3) 나무·드럼·타이어: GenF 3단 텍스처  (4) 원경 집: 위를 보는 면을 둘째 서브메시로 떼어 주황 지붕 재질. 텍스처 = docs/qa/tmp/a2f_tex.py. 값 = 가설.
const string D = "Assets/Art/SceneA2/", F = "Assets/Art/SceneA2/GenF/", S = "Assets/Scenes/Scene_A2f_Town.unity";
Color FLAT = Color.white; float ROOF_FOG = 0.40f;
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A2d_Town.unity", S);
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
AssetDatabase.DeleteAsset(D + "MatsF"); AssetDatabase.CreateFolder("Assets/Art/SceneA2", "MatsF");
System.Func<string, TextureWrapMode, TextureWrapMode, Texture2D> Tex = (p, wu, wv) => {
  AssetDatabase.ImportAsset(p); var ti = (TextureImporter)AssetImporter.GetAtPath(p);
  ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 8192; ti.alphaIsTransparency = true;
  ti.wrapModeU = wu; ti.wrapModeV = wv; ti.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(p); };
var R = TextureWrapMode.Repeat; var C = TextureWrapMode.Clamp;
var map = new System.Collections.Generic.Dictionary<Material, Material>(); var log = new System.Text.StringBuilder();
System.Action<Material> Neutral = m => { m.SetFloat("_Sat", 1f); m.SetColor("_WB", FLAT); m.SetColor("_Tint", FLAT); };
System.Func<Material, Material> Conv = src => {
  if (src == null || !src.name.StartsWith("SA2d_")) return src; if (map.TryGetValue(src, out var mm)) return mm;
  var m = new Material(src) { name = "SA2f_" + src.name.Substring(5) }; string n = m.name;
  if (n.Contains("asphalt")) { m.SetTexture("_BaseMap", Tex(F + "Gen/road_a2f.png", C, R)); Neutral(m); m.SetFloat("_Lift", 0f); }          // 따뜻한 회색이 텍스처에 있음 — 채도·WB·청록 들어올림 끔(청 끼 금지)
  else if (n.Contains("grass_tile")) { m.SetTexture("_BaseMap", Tex(F + "Gen/grass_tile_soft.png", R, R)); Neutral(m); m.SetFloat("_Lift", 0f); }
  else if (n.Contains("autumn_tree")) { string k = n.Substring(n.IndexOf("autumn_tree")); m.SetTexture("_BaseMap", Tex(F + "Gen/" + k + ".png", R, R)); Neutral(m); }
  else if (n.Contains("drum_") || n.Contains("tires_")) { string k = n.Substring(5); m.SetTexture("_BaseMap", Tex(F + "kit/" + k + ".png", R, R)); if (n.Contains("drum_")) Neutral(m); else m.SetFloat("_Sat", 1.3f); }   // 드럼은 색이 텍스처에 정해짐, 타이어 ×2.6 이면 형광(1차 캡처)
  AssetDatabase.CreateAsset(m, D + "MatsF/" + n + ".mat"); map[src] = m; return m; };
int rn = 0; foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{ var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Conv(ms[i]); r.sharedMaterials = ms; rn++; }
var root = GameObject.Find("SceneA2d"); if (root) root.name = "SceneA2f";

// (1) 길 UV: u 0..2.5 → 0..1 (폭 한 장), v 2 m/칸 → 20 m/장
var roadGo = GameObject.Find("Road"); var rmf = roadGo.GetComponent<MeshFilter>();
var rm = UnityEngine.Object.Instantiate(rmf.sharedMesh); rm.name = "f_road"; var ruv = rm.uv;
for (int i = 0; i < ruv.Length; i++) ruv[i] = new Vector2(ruv[i].x / 2.5f, ruv[i].y / 10f);
rm.uv = ruv; AssetDatabase.CreateAsset(rm, D + "MatsF/f_road.asset"); rmf.sharedMesh = rm;

// (2) 마른 풀 띠: 길 양 가장자리에서 바깥으로 1.8 m, 땅 높이에 붙임(임시 MeshCollider 로 내려 쏨, 저장 전 지움)
var temp = new System.Collections.Generic.List<MeshCollider>();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
  if (r.sharedMaterial && r.sharedMaterial.name.Contains("grass_tile")) temp.Add(r.gameObject.AddComponent<MeshCollider>());
Physics.SyncTransforms();
var rv = rmf.sharedMesh.vertices; var ouv = rmf.sharedMesh.uv; var lw = roadGo.transform.localToWorldMatrix; int rows = rv.Length / 6;
var vs = new System.Collections.Generic.List<Vector3>(); var us = new System.Collections.Generic.List<Vector2>(); var ts = new System.Collections.Generic.List<int>();
float[] dist = { -0.05f, 0.6f, 1.2f, 1.8f }; int hits = 0, miss = 0;
foreach (int side in new[] { 0, 5 }) {
  int b0 = vs.Count;
  for (int j = 0; j < rows; j++) {
    Vector3 e = lw.MultiplyPoint3x4(rv[j * 6 + side]), o = lw.MultiplyPoint3x4(rv[j * 6 + 5 - side]);
    Vector3 dir = e - o; dir.y = 0; dir.Normalize();
    for (int c = 0; c < dist.Length; c++) {
      Vector3 p = e + dir * dist[c]; float y = e.y;
      var hs = Physics.RaycastAll(p + Vector3.up * 40f, Vector3.down, 120f); float best = float.NegativeInfinity;
      foreach (var h in hs) if (h.collider is MeshCollider && temp.Contains((MeshCollider)h.collider)) best = Mathf.Max(best, h.point.y);
      if (best > float.NegativeInfinity) { y = best; hits++; } else miss++;
      vs.Add(new Vector3(p.x, y + 0.04f, p.z)); us.Add(new Vector2(c / (float)(dist.Length - 1), ouv[j * 6].y * 2f / 8f)); } }
  for (int j = 0; j < rows - 1; j++) for (int c = 0; c < dist.Length - 1; c++) {
    int a = b0 + j * dist.Length + c, bq = a + dist.Length;
    Vector3 nrm = Vector3.Cross(vs[bq] - vs[a], vs[a + 1] - vs[a]);
    if (nrm.y >= 0) { ts.Add(a); ts.Add(bq); ts.Add(a + 1); ts.Add(a + 1); ts.Add(bq); ts.Add(bq + 1); }
    else { ts.Add(a); ts.Add(a + 1); ts.Add(bq); ts.Add(a + 1); ts.Add(bq + 1); ts.Add(bq); } } }
foreach (var mc in temp) UnityEngine.Object.DestroyImmediate(mc);
var dm = new Mesh { name = "f_dry_verge" }; dm.SetVertices(vs); dm.SetUVs(0, us); dm.SetTriangles(ts, 0); dm.RecalculateNormals(); dm.RecalculateBounds();
AssetDatabase.CreateAsset(dm, D + "MatsF/f_dry_verge.asset");
var grassMat = AssetDatabase.LoadAssetAtPath<Material>(D + "MatsF/SA2f_grass_tile_soft.mat");
var dryMat = new Material(grassMat) { name = "SA2f_dry_strip" }; dryMat.SetTexture("_BaseMap", Tex(F + "Gen/dry_strip.png", C, R)); dryMat.SetFloat("_Cutoff", 0.5f);
AssetDatabase.CreateAsset(dryMat, D + "MatsF/SA2f_dry_strip.mat");
var dg = new GameObject("DryVerge"); dg.transform.SetParent(root ? root.transform : null, false);
dg.AddComponent<MeshFilter>().sharedMesh = dm; var dr = dg.AddComponent<MeshRenderer>(); dr.sharedMaterial = dryMat; dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

// (4) 원경 집 지붕: 월드에서 위를 보는(법선 y > 0.35) 삼각형 → 서브메시 1, 주황 지붕 재질
var farMat = AssetDatabase.LoadAssetAtPath<Material>(D + "MatsF/SA2f_far_town_baked32_farfog.mat");
var roofMat = new Material(farMat) { name = "SA2f_far_roof_farfog" }; roofMat.SetTexture("_BaseMap", Tex(F + "Gen/roof_far.png", R, R)); Neutral(roofMat); roofMat.SetFloat("_Lift", 0f); roofMat.SetFloat("_FogMax", ROOF_FOG);
AssetDatabase.CreateAsset(roofMat, D + "MatsF/SA2f_far_roof_farfog.mat");
var meshCache = new System.Collections.Generic.Dictionary<Mesh, Mesh>(); int roofTris = 0, houses = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  if (r.sharedMaterial != farMat) continue; var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
  var src = mf.sharedMesh; var vtx = src.vertices; var tri = src.GetTriangles(0); var l2w = r.transform.localToWorldMatrix;
  var wall = new System.Collections.Generic.List<int>(); var top = new System.Collections.Generic.List<int>();
  for (int i = 0; i < tri.Length; i += 3) {
    Vector3 a = l2w.MultiplyPoint3x4(vtx[tri[i]]), b = l2w.MultiplyPoint3x4(vtx[tri[i + 1]]), c = l2w.MultiplyPoint3x4(vtx[tri[i + 2]]);
    var nn = Vector3.Cross(b - a, c - a).normalized; var lst = Mathf.Abs(nn.y) > 0.35f && (a.y + b.y + c.y) / 3f > r.bounds.min.y + 1.2f ? top : wall;   // 바닥판 제외
    lst.Add(tri[i]); lst.Add(tri[i + 1]); lst.Add(tri[i + 2]); }
  if (top.Count == 0) continue;
  var nm = UnityEngine.Object.Instantiate(src); nm.name = "f_" + r.name; nm.subMeshCount = 2; nm.SetTriangles(wall, 0); nm.SetTriangles(top, 1);
  AssetDatabase.CreateAsset(nm, D + "MatsF/f_" + r.name + ".asset"); mf.sharedMesh = nm; r.sharedMaterials = new[] { farMat, roofMat };
  roofTris += top.Count / 3; houses++; }

int shadows = 0; foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (l.shadows != LightShadows.None) shadows++;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
return "renderers " + rn + " · 재질 " + map.Count + " · 길 행 " + rows + " · 마른 풀 띠 정점 " + vs.Count + " (땅 맞음 " + hits + " / 못 맞음 " + miss + ") · 지붕 집 " + houses + " 삼각형 " + roofTris + " · 그림자 켠 빛 " + shadows;
