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
  System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/tmp/pz-" + kv.Key + ".png", tex.EncodeToPNG()); o += kv.Key + "@" + path[i].ToString("F0") + " drop" + drop.ToString("F1") + " ";
}
cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(camGo); UnityEngine.Object.DestroyImmediate(rt);
return o;
