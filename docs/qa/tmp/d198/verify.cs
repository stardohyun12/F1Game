// D-198 검증: 접지·길 침범·차 자리·발코니 바다 가시·해 방향·캡처
var sb = new System.Text.StringBuilder();
var gray = GameObject.Find("TownV6_Gray").transform; var tmc = gray.Find("TownV6_Terrain").GetComponent<MeshCollider>();
float TY(float x, float z) { RaycastHit h; return tmc.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out h, 600) ? h.point.y : float.NaN; }
var B = new (string id, Vector4 b)[] { ("room", new Vector4(-1.75f,-3.16f,1.75f,0f)), ("garage", new Vector4(-8f,-24.91f,8f,0f)), ("depot", new Vector4(-5.01f,-7f,5f,1.01f)), ("postoffice", new Vector4(-5.34f,-8.86f,5.1f,1.08f)) };
var roadCols = gray.Find("Roads").GetComponentsInChildren<Collider>(false);
foreach (var p in B) {
  var t = gray.Find("D198_Buildings/D198_" + p.id); float worst = 0; int n = 0, poke = 0, rn = 0;
  for (float u = p.b.x; u <= p.b.z + 0.01f; u += 0.5f) for (float w = p.b.y; w <= p.b.w + 0.01f; w += 0.5f) {
    var wp = t.TransformPoint(new Vector3(u, 0, w)); float d = TY(wp.x, wp.z) - t.position.y; if (Mathf.Abs(d) > Mathf.Abs(worst)) worst = d; n++; }
  // 터 녹임 범위(발자국+6 m)에서 지형이 길 위로 솟은 곳
  for (float u = p.b.x - 6; u <= p.b.z + 6; u += 0.5f) for (float w = p.b.y - 6; w <= p.b.w + 6; w += 0.5f) {
    var wp = t.TransformPoint(new Vector3(u, 0, w)); var ray = new Ray(new Vector3(wp.x, 300, wp.z), Vector3.down);
    foreach (var c in roadCols) { RaycastHit h; if (c.Raycast(ray, out h, 600)) { rn++; if (TY(wp.x, wp.z) > h.point.y + 0.005f) poke++; break; } } }
  sb.Append(p.id + " floor y=" + t.position.y.ToString("F2") + " terrain-floor worst=" + (worst * 100).ToString("F1") + "cm (n=" + n + ") roadSamples=" + rn + " terrainAboveRoad=" + poke + "\n");
}
var spot = GameObject.Find("TUT_GarageCarSpot").transform;
var ov = Physics.OverlapBox(spot.position + Vector3.up * 0.7f, new Vector3(0.8f, 0.5f, 1.4f), spot.rotation).Where(c => c.name != "TownV6_Terrain").Select(c => c.name).ToList();
sb.Append("carSpot overlaps: " + (ov.Count == 0 ? "none" : string.Join(",", ov)) + "\n");
// 발코니: 방 가운데, 바닥+3.5 m
var room = gray.Find("D198_Buildings/D198_room"); var eye = room.TransformPoint(new Vector3(0, 3.5f, -1.58f));
sb.Append("eye " + eye.ToString("F2") + "\n");
var dirs = new (string n, float yaw)[] { ("N", 0), ("NE", 45), ("E", 90), ("SE", 135), ("S", 180), ("SW", 225), ("W", 270), ("NW", 315) };
foreach (var d in dirs) {
  string res = "";
  for (int pi = 0; pi <= 10; pi += 2) {
    var dir = Quaternion.Euler(pi, d.yaw, 0) * Vector3.forward;
    var hs = Physics.RaycastAll(eye, dir, 1500).Where(h => !h.collider.transform.IsChildOf(room)).OrderBy(h => h.distance).ToList();
    if (hs.Count == 0) res += " p-" + pi + ":" + (pi == 0 ? "sky" : "SEA@" + (eye.y / Mathf.Sin(pi * Mathf.Deg2Rad)).ToString("F0") + "m");
    else res += " p-" + pi + ":" + hs[0].collider.name.Replace("TownV6_", "") + "@" + hs[0].distance.ToString("F0");
  }
  sb.Append(d.n + res + "\n");
}
var sun = GameObject.Find("Directional Light").transform;
sb.Append("sun euler=" + sun.eulerAngles.ToString("F0") + " light travels yaw " + sun.eulerAngles.y.ToString("F0") + " -> sun sits at yaw " + ((sun.eulerAngles.y + 180) % 360).ToString("F0") + " elev " + sun.eulerAngles.x.ToString("F0") + "\n");
// 캡처
var dirOut = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/d198")); System.IO.Directory.CreateDirectory(dirOut);
var cg = new GameObject("tmpCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 60; cam.nearClipPlane = 0.3f; cam.farClipPlane = 2000;
var mc = Camera.main; if (mc) { cam.clearFlags = mc.clearFlags; cam.backgroundColor = mc.backgroundColor; }
var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
foreach (var d in dirs) {
  cg.transform.SetPositionAndRotation(eye, Quaternion.Euler(5, d.yaw, 0)); cam.Render();
  RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
  System.IO.File.WriteAllBytes(System.IO.Path.Combine(dirOut, "room_view_" + d.n + ".png"), tex.EncodeToPNG());
}
cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(cg);
return sb.ToString();
