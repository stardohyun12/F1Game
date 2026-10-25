// 에디트 모드 미리보기: 게임 카메라 자세(Postcard: 뒤 4.2·위 1.9·8m 앞 1m 높이를 봄, FOV 50)를 길 위 지점에 놓고 렌더 → docs/qa/tmp/prev-*.png
// 3차: 꼭대기 카메라 구역(CameraFramingZone_Crest)의 drop 을 거리로 계산해 시선을 내린다(실게임은 rate 로 늦게 따라옴 — 근사).
var c = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Splines.SplineContainer>();
var src = UnityEngine.Object.FindFirstObjectByType<FollowCamera>().GetComponent<Camera>();
var zone = UnityEngine.Object.FindFirstObjectByType<CameraFramingZone>();
var path = new System.Collections.Generic.List<Vector3>();
for (int i = 0; i <= 3000; i++) { var q = UnityEngine.Splines.SplineUtility.EvaluatePosition(c.Splines[0], i / 3000f); var v = new Vector3(q.x, q.y, q.z); if (path.Count == 0 || Vector3.Distance(path[path.Count - 1], v) >= 1f) path.Add(v); }
var pts = new System.Collections.Generic.Dictionary<string, int>();
int iCorner = 0; float bc = 1e9f;
for (int i = 0; i < path.Count; i++) { float d = Vector2.Distance(new Vector2(path[i].x, path[i].z), new Vector2(12f, 167f)); if (d < bc) { bc = d; iCorner = i; } }
pts["z150"] = path.FindIndex(p => p.z >= 150f); pts["z155"] = path.FindIndex(p => p.z >= 155f); pts["z159"] = path.FindIndex(p => p.z >= 159f); pts["z162"] = path.FindIndex(p => p.z >= 162f); pts["c0"] = iCorner;
var camGo = new GameObject("TmpCap"); var cam = camGo.AddComponent<Camera>(); cam.CopyFrom(src); cam.fieldOfView = 50f; cam.farClipPlane = 2000f;
var rt = new RenderTexture(1280, 720, 24); var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); string o = "";
foreach (var kv in pts)
{
  int i = Mathf.Clamp(kv.Value, 3, path.Count - 4); var car = path[i] + Vector3.up * 0.5f; var f = path[i + 3] - path[i - 3]; f.y = 0; f.Normalize();
  float drop = 0f; if (zone != null) { var dd = car - zone.transform.position; dd.y = 0; drop = zone.drop * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(zone.radius * 0.6f, zone.radius, dd.magnitude))); }
  camGo.transform.position = car - f * 4.2f + Vector3.up * 1.9f; camGo.transform.LookAt(car + f * 8f + Vector3.up * (1.0f - drop));
  cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
  int tot = 0, hit = 0, miss = 0; var mk = new Texture2D(96, 54, TextureFormat.RGB24, false); for (int gy = 0; gy < 54; gy++) for (int gx = 0; gx < 96; gx++) { float u = (gx + .5f) / 96f, w = (gy + .5f) / 54f; float t = 1f - w; bool hud = (u < .19f && t > .64f && t < .74f) || (u < .31f && t > .83f && t < .91f) || (u > .74f && u < .955f && t > .15f && t < .49f) || (u > .875f && t > .72f && t < .9f) || t < .12f || (u > .33f && u < .68f && t > .58f); if (hud) { mk.SetPixel(gx, gy, Color.gray); continue; } tot++; var ray = cam.ViewportPointToRay(new Vector3(u, w, 0)); if (Physics.Raycast(ray, out var rh, 1500f)) { if (rh.point.y < 8.5f) { hit++; mk.SetPixel(gx, gy, Color.green); } else mk.SetPixel(gx, gy, Color.black); } else if (ray.direction.y < 0) { hit++; miss++; mk.SetPixel(gx, gy, Color.blue); } else mk.SetPixel(gx, gy, Color.white); }
  System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/tmp/mask-" + kv.Key + ".png", mk.EncodeToPNG());
  o += "[screen " + (100f * hit / 5184f).ToString("F0") + "% / nonHUD " + (100f * hit / tot).ToString("F0") + "% miss" + miss + " of " + tot + "] ";
  System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/tmp/pz-" + kv.Key + ".png", tex.EncodeToPNG()); o += kv.Key + "@" + path[i].ToString("F0") + " drop" + drop.ToString("F1") + " ";
}
cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(camGo); UnityEngine.Object.DestroyImmediate(rt);
return o;
