// 과제 0601 — Scene_A2b_Town: 마당 끝부터 내리막 길(아스팔트 폭 5 m + 양쪽 돌담), 길 너머 아래 비탈에 원경 지붕 층층이 → 바다 띠·수평선.
// 다시 돌리면 씬을 새로 만든다. 좌표는 Blender(카트 원점, +y = 마을 쪽) → Unity (−x, z, −y).
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
const string D = "Assets/Art/SceneA2/"; System.IO.Directory.CreateDirectory(D + "MatsB"); System.IO.Directory.CreateDirectory(D + "GenB");
var look = Shader.Find("PitCrew/SceneA/Look"); var log = new System.Text.StringBuilder();
Color fogC = new Color(.86f, .85f, .80f);
foreach (var tp in new[] { "asphalt_tile", "wall_tile", "grass_tile_soft", "yard_ground_soft", "pole_wood", "wire", "autumn_tree_01_soft", "autumn_tree_02_soft", "autumn_tree_03_soft", "autumn_tree_04_soft", "autumn_tree_05_soft" })
{ var p = D + "Gen/" + tp + ".png"; AssetDatabase.ImportAsset(p); var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 8192; ti.alphaIsTransparency = true; ti.wrapMode = tp.Contains("tree") || tp.Contains("yard") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat; ti.SaveAndReimport(); }
var mats = new System.Collections.Generic.Dictionary<string, Material>();
System.Func<string, float, Material> Mat = (png, fogMax) => {
  string key = png + fogMax; if (mats.TryGetValue(key, out var mm)) return mm;
  var m = new Material(look) { name = "SA2b_" + System.IO.Path.GetFileNameWithoutExtension(png) + (fogMax < .01f ? "_nofog" : "") };
  m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + png)); m.SetFloat("_LightAmt", 0); m.SetFloat("_Cutoff", 0.5f);
  m.SetColor("_FogColor", fogC); m.SetFloat("_FogStart", 12); m.SetFloat("_FogEnd", 140); m.SetFloat("_FogMax", fogMax);
  AssetDatabase.CreateAsset(m, D + "MatsB/" + m.name + ".mat"); mats[key] = m; return m; };
System.Func<float, float, float, Vector3> U = (x, y, z) => new Vector3(-x, z, -y);
var root = new GameObject("SceneA2b").transform;
System.Func<string, GameObject> Put = path => (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + path), root);
System.Action<GameObject, string> Paint = (go, png) => { foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
  string p = r.name.StartsWith("far_terrace") ? "far/far_terrace_baked32.png" : png; var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Mat(p, .65f); r.sharedMaterials = ms; } };
System.Func<string, int, int, System.Func<int, int, Vector3>, System.Func<int, int, Vector3, Vector2>, Mesh> Grid = (nm, nx, ny, f, uvf) => {
  var v = new Vector3[nx * ny]; var uv = new Vector2[nx * ny]; var t = new int[(nx - 1) * (ny - 1) * 6]; int k = 0;
  for (int j = 0; j < ny; j++) for (int i = 0; i < nx; i++) { var b = f(i, j); v[j * nx + i] = U(b.x, b.y, b.z); uv[j * nx + i] = uvf(i, j, b); }
  for (int j = 0; j < ny - 1; j++) for (int i = 0; i < nx - 1; i++) { int a = j * nx + i; t[k++] = a; t[k++] = a + nx; t[k++] = a + 1; t[k++] = a + 1; t[k++] = a + nx; t[k++] = a + nx + 1; }
  var m = new Mesh { name = nm, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, vertices = v, uv = uv, triangles = t }; m.RecalculateNormals(); m.RecalculateBounds();
  AssetDatabase.CreateAsset(m, D + "GenB/" + nm + ".asset"); return m; };
System.Func<string, Mesh, Material, GameObject> MeshGO = (nm, m, mat) => { var g = new GameObject(nm); g.transform.SetParent(root, false); g.AddComponent<MeshFilter>().sharedMesh = m; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return g; };

// ── 길 중심선(마당 안은 곧게, 마당 끝 y 4.5 부터 오른쪽으로 굽어 내려감) ──
var cl = new Vector2[] { new(0, -3), new(0, 4.5f), new(0.4f, 12), new(1.4f, 20), new(3.2f, 28), new(6, 36), new(10, 44), new(15, 52), new(20, 60), new(24, 70), new(26.5f, 82), new(27.5f, 94) };
var pts = new System.Collections.Generic.List<Vector2>(); for (int s = 0; s < cl.Length - 1; s++) for (int q = 0; q < 6; q++) pts.Add(Vector2.Lerp(cl[s], cl[s + 1], q / 6f)); pts.Add(cl[cl.Length - 1]);
var cum = new float[pts.Count]; for (int q = 1; q < pts.Count; q++) cum[q] = cum[q - 1] + (pts[q] - pts[q - 1]).magnitude;
System.Func<int, Vector2> Nrm = j => { var d = (pts[Mathf.Min(pts.Count - 1, j + 1)] - pts[Mathf.Max(0, j - 1)]).normalized; return new Vector2(d.y, -d.x); };   // 진행 방향 오른쪽
// 높이 표(y → z): 마당 0, 길 구간 10~11 %(8~12 % 요구), 마을 단(3.2 m 씩) 지나 바닷가 −22
var prof = new Vector2[] { new(-60, 0), new(4.5f, 0), new(12, -0.8f), new(20, -1.65f), new(28, -2.55f), new(36, -3.45f), new(44, -4.45f), new(52, -5.45f), new(59, -6.3f), new(70, -9.5f), new(81, -12.7f), new(92, -15.9f), new(103, -19f), new(112, -22.5f), new(600, -25) };   // y 59 부터는 원경 마을 단(11 m 마다 3.2 m)을 따라 급해짐
System.Func<float, float> Base = y => { for (int i = 0; i < prof.Length - 1; i++) if (y <= prof[i + 1].x) return Mathf.Lerp(prof[i].y, prof[i + 1].y, (y - prof[i].x) / (prof[i + 1].x - prof[i].x)); return prof[prof.Length - 1].y; };
System.Func<float, float> RoadX = y => { if (y <= 4.5f) return 0; for (int i = 0; i < cl.Length - 1; i++) if (y <= cl[i + 1].y) return Mathf.Lerp(cl[i].x, cl[i + 1].x, (y - cl[i].y) / (cl[i + 1].y - cl[i].y)); return cl[cl.Length - 1].x; };
System.Func<float, float, float> H = (x, y) => {
  float b = Base(y); if (y < 4.5f && Mathf.Abs(x) < 10.5f) return -0.02f;
  float d = x - RoadX(y), w = Mathf.SmoothStep(0, 1, (y - 3f) / 8f) * (1 - Mathf.SmoothStep(0, 1, (y - 18f) / 20f));   // 옆 둔덕은 y 18~38 에서 사라짐 → 카메라 B 에서 원경 마을 단을 안 가림(1차 58~82 는 왼쪽 둔덕이 마을을 덮었다)
  if (d < -5) b += w * Mathf.Min(3f, (-d - 5) * 0.3f);      // 왼쪽 = 비탈 위(언덕)
  if (d > 5) b -= w * Mathf.Min(3f, (d - 5) * 0.12f);          // 오른쪽 = 비탈 아래
  return b; };
log.Append($"경사: y4.5→12 {(0.8f / 7.5f) * 100:F1}% · 12→44 {(3.65f / 32f) * 100:F1}% · H(0,20)={H(1.4f, 20):F2}\n");

// 땅: x −120..140, y −30..140, 2 m 격자, 대비 낮춘 풀 타일
MeshGO("Terrain", Grid("b_terrain", 131, 86, (i, j) => { float x = -120 + i * 2, y = -30 + j * 2; return new Vector3(x, y, H(x, y) - 0.03f); }, (i, j, b) => new Vector2(b.x / 32f, b.y / 29.6f)), Mat("Gen/grass_tile_soft.png", .65f));
// 길: 폭 5 m 아스팔트(2 m 타일), 마당 안(y<4.5)은 마당 흙 그대로 두려고 y 4.2 부터
int j0 = pts.FindIndex(p => p.y >= 4.2f);
System.Func<int, float> RZ = j => Base(pts[j].y) + 0.05f;
MeshGO("Road", Grid("b_road", 6, pts.Count - j0, (i, j) => { int jj = j + j0; var p = pts[jj] + Nrm(jj) * (i - 2.5f); return new Vector3(p.x, p.y, RZ(jj)); }, (i, j, b) => new Vector2(i / 2f, (cum[j + j0] - cum[j0]) / 2f)), Mat("Gen/asphalt_tile.png", .65f));
// 돌담: 왼쪽 옹벽(길 위 1.3 m, y 5~66), 오른쪽 낮은 담(0.55 m, y 5~80). 단면 4점(안쪽 아래·안쪽 위·바깥 위·바깥 아래)
System.Action<string, float, float, float, float, float> Wall = (nm, off, h, th, y0, y1) => {
  int a = pts.FindIndex(p => p.y >= y0), b = pts.FindLastIndex(p => p.y <= y1); float sg = Mathf.Sign(off);
  MeshGO(nm, Grid("b_" + nm, 4, b - a + 1, (i, j) => { int jj = j + a; float o = off + (i >= 2 ? sg * th : 0); var p = pts[jj] + Nrm(jj) * o; float z0 = RZ(jj) - 0.4f; return new Vector3(p.x, p.y, (i == 1 || i == 2) ? RZ(jj) + h : z0); },
    (i, j, bb) => new Vector2(new[] { 0f, h + .4f, h + .4f + th, 2 * h + .8f + th }[i] / 2f, (cum[j + a] - cum[a]) / 2f)), Mat("Gen/wall_tile.png", .65f)); };
Wall("Wall_L", -2.75f, 1.3f, 0.45f, 4.6f, 66); Wall("Wall_R", 2.75f, 0.55f, 0.35f, 4.6f, 80);

// 하늘 반구(그라데이션, 안개 없음) — A2 텍스처 재사용
MeshGO("Sky", Grid("b_sky", 33, 12, (i, j) => { float a = i / 32f * Mathf.PI * 2, e = Mathf.Lerp(-0.25f, Mathf.PI / 2, j / 11f); return new Vector3(Mathf.Cos(a) * Mathf.Cos(e) * 1500, Mathf.Sin(a) * Mathf.Cos(e) * 1500, Mathf.Sin(e) * 1500); }, (i, j, b) => new Vector2(0.5f, Mathf.Clamp01(b.z / 1500f))), Mat("Gen/sky_grad.png", 0));
// 곶 2개(바다 양끝, 낮게) — 풀 타일
foreach (var hc in new[] { new Vector4(-130, 230, 110, 26), new Vector4(170, 260, 130, 30) })
{ var c = hc; MeshGO("Headland", Grid("b_head" + (int)c.x, 25, 9, (i, j) => { float a = i / 24f * Mathf.PI * 2, r = j / 8f; return new Vector3(c.x + Mathf.Cos(a) * r * c.z / 2, c.y + Mathf.Sin(a) * r * c.z * 0.35f, -23 + c.w * Mathf.Pow(1 - r * r, 0.8f)); }, (i, j, b) => new Vector2(b.x / 32f, b.y / 29.6f)), Mat("Gen/grass_tile_soft.png", .65f)); }

// ── 큰 덩어리 ──
var yard = Put("far/yard_ground.fbx"); yard.transform.localPosition = U(0, 0, 0.02f); Paint(yard, "Gen/yard_ground_soft.png");
// 원경 마을: 원본은 뒤 단일수록 3.2 m 높다 → 180° 돌려 가까운 단이 높고 먼 단이 낮게(아래 비탈에 층층이). 단 y 59/70/81/92 · z −6.3/−9.5/−12.7/−15.9 (1차 y 92~125 는 100 m 밖이라 화면에서 40 px 띠였다)
var far = Put("far/far_town.fbx"); far.transform.localPosition = U(12, 92, -15.9f);   // 가운데를 길이 굽어 가는 쪽(x 12)으로 — 왼쪽 둔덕 뒤에 숨지 않게
 far.transform.localRotation = Quaternion.Euler(0, 180, 0) * far.transform.localRotation;
int farOff = 0; foreach (var r in far.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Mat(r.name.StartsWith("far_terrace") ? "far/far_terrace_baked32.png" : "far/far_town_baked32.png", .4f); r.sharedMaterials = ms;   // 원경 지붕 안개 최대 40 %(가설)
  var bc = r.bounds.center; if (!r.name.StartsWith("far_terrace") && Mathf.Abs(-bc.x - RoadX(-bc.z)) < 8f) { r.enabled = false; farOff++; } }   // 길 위에 걸치는 집은 끔
var gar = Put("v5/garage_v5_combo.fbx"); gar.transform.localRotation = Quaternion.Euler(0, 180, 0); gar.transform.localPosition = new Vector3(0.7f, 0, 11.5f); Paint(gar, "v5/garage_baked32.png");
var sea = Put("v5/sea_v5_combo.fbx"); sea.transform.localScale *= 1.6f; Paint(sea, "v5/sea_baked32.png"); foreach (var r in sea.GetComponentsInChildren<Renderer>()) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Mat("v5/sea_baked32.png", .3f); r.sharedMaterials = ms; }   // 바다 띠가 하늘과 갈리게 안개 30 %
{ var sb0 = sea.GetComponentInChildren<Renderer>().bounds; var want = U(20, 112 + sb0.size.z / 2, -20f); sea.transform.position += want - sb0.center; log.Append("sea " + sea.GetComponentInChildren<Renderer>().bounds.ToString() + "\n"); }   // 바다 앞 끝을 y 112(땅이 −22.5 로 내려가는 곳)에
var kart = Put("kart/kart_a2.fbx"); kart.name = "Kart"; Paint(kart, "kart/kart_a2_baked32.png");

// ── 소품 무리(흩뿌리지 않음): 마당 왼쪽 가장자리 · 오른쪽 가장자리 · 길 들머리 · 길가 ──
int placed = 0;
System.Action<string, float, float, float, float, float> K = (kit, x, y, rot, tilt, hgt) => {
  var g = Put("kit/" + kit + ".fbx"); g.name = kit + "_" + placed; float z = (y < 4.5f && Mathf.Abs(x) < 10.5f) ? 0 : H(x, y);
  g.transform.localPosition = U(x, y, z); g.transform.localRotation = Quaternion.Euler(0, -rot, tilt) * g.transform.localRotation;
  if (hgt > 0) g.transform.localScale *= hgt / g.GetComponentInChildren<Renderer>().bounds.size.y;
  Paint(g, kit.StartsWith("autumn_tree") ? "Gen/" + kit + "_soft.png" : "kit/" + kit + ".png"); placed++; };
K("tires_01", -4.3f, 1.6f, 10, 0, 0); K("tires_02", -3.5f, 0.5f, 0, 6, 0); K("pallet_01", -4.9f, -0.7f, 18, 0, 0); K("drum_03", -3.2f, 2.5f, 0, 0, 0);   // 왼쪽 무리
K("drum_01", 3.4f, 1.2f, 0, 0, 0); K("drum_02", 4.1f, 2.0f, 0, 4, 0); K("pot_01", 3.0f, 3.1f, 0, 0, 0); K("pallet_02", 4.7f, 0.1f, -12, 0, 0);       // 오른쪽 무리
K("cone_01", 2.9f, 4.9f, 0, 0, 0); K("cone_02", 3.4f, 5.5f, 30, 0, 0); K("pot_02", -3.1f, 4.7f, 0, 0, 0); K("bench_01", 5.8f, 3.4f, -90, 0, 0);      // 길 들머리
K("old_car_01", 7.4f, 0.4f, 65, 0, 0);                                         // 정비소 손님 차, 마당 오른쪽
K("bicycle_01", -2.35f, 9.5f, 3, 10, 0);                                       // 왼쪽 옹벽에 기댐
K("streetlight_01", 3.55f, 15.5f, 90, 2, 0);                                   // 오른쪽 담 위
K("transformer_01", -4.6f, 18.6f, 0, 0, 0);
// 판잣집 1채: 길 오른쪽 중경(정면 중앙은 비움)
K("background_building_03", 12.5f, 33, -35, 0, 4.6f);
// 나무: 화면 양옆 틀 + 비탈 위·아래에 하나씩
K("autumn_tree_01", -7.2f, 5.5f, 0, 0, 10); K("autumn_tree_02", 7.8f, 7.5f, 0, 0, 9.5f); K("autumn_tree_03", -10.5f, 24, 0, 0, 9); K("autumn_tree_04", 13, 18, 0, 0, 8.5f); K("autumn_tree_05", -12, 44, 0, 0, 9);

// ── 전신주 6개: 길 왼쪽 한 줄(옹벽 위), 기울기 3~11°, 전선 3가닥 처짐 0.5~0.9 m ──
float[] poleY = { 7, 18, 30, 43, 57, 72 }; float[] poleTilt = { 8, -4, 11, 3, -6, 5 }; float[] sag = { .6f, .8f, .5f, .9f, .7f };
var tops = new System.Collections.Generic.List<Vector3[]>();
var wood = Mat("Gen/pole_wood.png", .65f); var wireM = Mat("Gen/wire.png", .65f);
System.Func<Vector3, Vector3, Vector3, Vector3, int, Vector3> P = (o, ax, sd, up, k) => o + ax * 0 + sd * 0 + up * 0;
for (int i = 0; i < poleY.Length; i++)
{
  float y = poleY[i]; int j = pts.FindIndex(p => p.y >= y); var n = Nrm(j); var p2 = pts[j] - n * 3.6f; float z = H(p2.x, p2.y) - 0.3f;
  var go = new GameObject("Pole_" + i); go.transform.SetParent(root, false); go.transform.localPosition = U(p2.x, p2.y, z);
  var dir = (pts[Mathf.Min(pts.Count - 1, j + 1)] - pts[Mathf.Max(0, j - 1)]).normalized; float yaw = Mathf.Atan2(-dir.x, -dir.y) * Mathf.Rad2Deg;   // Unity: 길 방향 = (−dx, 0, −dy)
  go.transform.localRotation = Quaternion.Euler(0, yaw, poleTilt[i]);
  var post = GameObject.CreatePrimitive(PrimitiveType.Cube); UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>()); post.transform.SetParent(go.transform, false); post.transform.localScale = new Vector3(0.24f, 7.6f, 0.24f); post.transform.localPosition = new Vector3(0, 3.8f, 0); post.GetComponent<Renderer>().sharedMaterial = wood;
  var arm = GameObject.CreatePrimitive(PrimitiveType.Cube); UnityEngine.Object.DestroyImmediate(arm.GetComponent<Collider>()); arm.transform.SetParent(go.transform, false); arm.transform.localScale = new Vector3(1.7f, 0.13f, 0.13f); arm.transform.localPosition = new Vector3(0, 7.0f, 0); arm.GetComponent<Renderer>().sharedMaterial = wood;
  foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
  tops.Add(new[] { go.transform.TransformPoint(new Vector3(-0.75f, 7.08f, 0)), go.transform.TransformPoint(new Vector3(0.75f, 7.08f, 0)), go.transform.TransformPoint(new Vector3(0, 7.65f, 0)) });
}
// 전선: 가닥마다 십자 리본(세로·가로, 폭 4 cm) 16 마디 포물선
int wires = 0;
for (int i = 0; i < tops.Count - 1; i++) for (int w = 0; w < 3; w++)
{
  var a = tops[i][w]; var b = tops[i + 1][w]; float sg = sag[i] * (w == 2 ? 0.8f : 1f);
  for (int x2 = 0; x2 < 2; x2++)
  {
    var off = x2 == 0 ? Vector3.up * 0.02f : Vector3.Cross((b - a).normalized, Vector3.up).normalized * 0.02f;
    var vs = new Vector3[34]; var tr = new int[16 * 6]; var uvs = new Vector2[34];
    for (int k = 0; k <= 16; k++) { float t = k / 16f; var c = Vector3.Lerp(a, b, t) - Vector3.up * sg * 4 * t * (1 - t); vs[k * 2] = c - off; vs[k * 2 + 1] = c + off; uvs[k * 2] = uvs[k * 2 + 1] = new Vector2(0.5f, 0.5f); }
    for (int k = 0; k < 16; k++) { int q = k * 2, o = k * 6; tr[o] = q; tr[o + 1] = q + 1; tr[o + 2] = q + 2; tr[o + 3] = q + 1; tr[o + 4] = q + 3; tr[o + 5] = q + 2; }
    var m = new Mesh { name = $"wire_{i}_{w}_{x2}", vertices = vs, triangles = tr, uv = uvs }; m.RecalculateBounds(); AssetDatabase.CreateAsset(m, D + "GenB/" + m.name + ".asset");
    var g = new GameObject("Wire"); g.transform.SetParent(root, false); g.AddComponent<MeshFilter>().sharedMesh = m; var rr = g.AddComponent<MeshRenderer>(); rr.sharedMaterial = wireM; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rr.receiveShadows = false;
  }
  wires++;
}
log.Append($"원경 집 끔 {farOff} · 소품·나무·건물 {placed} · 전신주 {tops.Count} · 전선 {wires}가닥\n");

// ── 카메라: B = 카트 뒤 5.6 m·높이 2.8 m·FOV 64. 보는 점 = 앞 6 m 길바닥 높이(LOOKH) — 내리막을 내려다보게(가설, kart_size.json 의 look_h 1.0 대신)
float LOOKH = 0.1f;   // 1차 −0.3(숙임 15°) → 하늘 14.7 % → 0.1(13.1°) 로 1회 조정
RenderSettings.fog = false; RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white;
var camDefs = new (string n, Vector3 pos, Vector3 at, float fov)[] {
  ("Cam_B_Game", U(0, -5.6f, 2.8f), U(0, 6, LOOKH), 64), ("Cam_D", U(0, -3.9f, 2.0f), U(0, 5, 0.3f), 64), ("Cam_Intro", U(5.5f, 7.5f, 4.5f), U(-0.5f, -5f, 1.2f), 55) };
for (int c = 0; c < camDefs.Length; c++)
{
  var cg = new GameObject(camDefs[c].n); var cam = cg.AddComponent<Camera>(); if (c == 0) cg.tag = "MainCamera";
  cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = fogC; cam.nearClipPlane = 0.1f; cam.farClipPlane = 2000; cam.fieldOfView = camDefs[c].fov;
  cg.transform.position = camDefs[c].pos; cg.transform.LookAt(camDefs[c].at);
  var lr = cg.AddComponent<LowResRenderTest>(); lr.targetHeight = 720; lr.integerScale = false; lr.smooth = false;
  cg.SetActive(c == 0); log.Append($"{cg.name} pos={cg.transform.position:F2} rot={cg.transform.eulerAngles:F1}\n");
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Scenes/Scene_A2b_Town.unity");
AssetDatabase.SaveAssets();
return log + "mats " + mats.Count;
