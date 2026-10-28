// D-198 터 고르기 2판: 디스크 원본 지형으로 되돌린 뒤, 정점마다 가장 가까운 건물 하나만 따르고, 길 면보다 높이 올리지 않음
var sb = new System.Text.StringBuilder();
var gray = GameObject.Find("TownV6_Gray").transform; var terr = gray.Find("TownV6_Terrain");
var tmf = terr.GetComponent<MeshFilter>(); var tmc = terr.GetComponent<MeshCollider>();
var path = UnityEditor.AssetDatabase.GetAssetPath(tmf.sharedMesh);
Resources.UnloadAsset(tmf.sharedMesh);
var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path); tmf.sharedMesh = mesh; tmc.sharedMesh = null; tmc.sharedMesh = mesh;
float TY(float x, float z) { RaycastHit h; return tmc.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out h, 600) ? h.point.y : float.NaN; }
sb.Append("reloaded; garage back corner terrain=" + TY(-62.5f, 18f).ToString("F2") + " (원본이면 8.32 아님)\n");
var P = new (string id, float x, float z, float yaw, Vector4 b)[] {
 ("room", -117.69f, -10.3f, 0.9f, new Vector4(-1.75f,-3.16f,1.75f,0f)),
 ("garage", -63.2f, -6.7f, -174.8f, new Vector4(-8f,-24.91f,8f,0f)),
 ("depot", -43.77f, -28.2f, 2.7f, new Vector4(-5.01f,-7f,5f,1.01f)),
 ("postoffice", -57.51f, -27.64f, -5.3f, new Vector4(-5.34f,-8.86f,5.1f,1.08f)) };
var pads = P.Select(p => gray.Find("D198_Buildings/D198_" + p.id).position.y).ToArray();
var v = mesh.vertices; int moved = 0, capped = 0;
for (int i = 0; i < v.Length; i++) {
  var w = terr.TransformPoint(v[i]); float bestD = 1e9f; int bi = -1;
  for (int k = 0; k < P.Length; k++) {
    var p = P[k]; var l = Quaternion.Euler(0, -p.yaw, 0) * new Vector3(w.x - p.x, 0, w.z - p.z);
    float dx = Mathf.Max(p.b.x - l.x, 0, l.x - p.b.z), dz = Mathf.Max(p.b.y - l.z, 0, l.z - p.b.w); float d = Mathf.Sqrt(dx * dx + dz * dz);
    if (d < bestD) { bestD = d; bi = k; }
  }
  if (bestD > 6f) continue;
  float t = bestD <= 2f ? 1f : Mathf.SmoothStep(1f, 0f, (bestD - 2f) / 4f);
  float ny = Mathf.Lerp(w.y, pads[bi] - 0.02f, t);
  if (ny > w.y) {   // 올릴 때만: 길 면(콜라이더) 6 cm 아래까지만
    var hs = Physics.RaycastAll(new Vector3(w.x, 300, w.z), Vector3.down, 600).Where(h => h.collider.name.StartsWith("ROAD_") || h.collider.name.StartsWith("BUMP_")).ToList();
    if (hs.Count > 0) { float ry = hs.Min(h => h.point.y) - 0.06f; if (ny > ry) { ny = Mathf.Max(w.y, ry); capped++; } }
  }
  if (Mathf.Abs(ny - w.y) < 0.001f) continue;
  w.y = ny; v[i] = terr.InverseTransformPoint(w); moved++;
}
mesh.vertices = v; mesh.RecalculateNormals(); mesh.RecalculateBounds(); UnityEditor.EditorUtility.SetDirty(mesh);
tmc.sharedMesh = null; tmc.sharedMesh = mesh;
sb.Append("moved " + moved + " capped(road) " + capped + "\n");
// 바다가 보이는 최소 눈높이(방 바닥 기준) — 남쪽 반원
var eye0 = gray.Find("D198_Buildings/D198_room").TransformPoint(new Vector3(0, 0, -1.58f));
foreach (var yaw in new[] { 135f, 158f, 180f, 202f, 225f, 250f, 270f }) {
  string res = "";
  foreach (var hgt in new[] { 3.5f, 6.5f, 9.5f, 12.5f, 15.5f, 20f, 25f, 30f }) {
    var e = eye0 + Vector3.up * hgt; bool sea = false;
    for (float pi = 0.25f; pi <= 8f && !sea; pi += 0.25f) { var dir = Quaternion.Euler(pi, yaw, 0) * Vector3.forward; if (!Physics.Raycast(e, dir, 3000)) sea = true; }
    if (sea) { res = "+" + hgt + "m"; break; }
  }
  sb.Append("yaw " + yaw + " sea first visible at floor" + (res == "" ? " >30m" : res) + "\n");
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
return sb.ToString();
