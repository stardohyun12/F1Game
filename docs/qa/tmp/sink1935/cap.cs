// 1935 캡처: shots.txt 줄마다 "이름 px py pz lx ly lz [fov]" → docs/qa/evidence/terrain-sink/<이름>.png. 에디트 모드, 임시 카메라(씬 저장 안 함).
var dir = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/evidence/terrain-sink");
System.IO.Directory.CreateDirectory(dir);
var lines = System.IO.File.ReadAllLines(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/sink1935/shots.txt"));
var inv = System.Globalization.CultureInfo.InvariantCulture;
var go = new GameObject("_cap1935"); var cam = go.AddComponent<Camera>(); cam.nearClipPlane = .1f; cam.farClipPlane = 2000;
var main = Camera.main; if (main != null) { cam.clearFlags = main.clearFlags; cam.backgroundColor = main.backgroundColor; }
var rt = new RenderTexture(1600, 1080, 24); cam.targetTexture = rt; var outs = "";
foreach (var ln in lines)
{
  var a = ln.Trim().Split(' '); if (a.Length < 7 || a[0].StartsWith("#")) continue;
  float f(int i) => float.Parse(a[i], inv);
  cam.fieldOfView = a.Length > 7 ? f(7) : 60;
  go.transform.position = new Vector3(f(1), f(2), f(3)); go.transform.LookAt(new Vector3(f(4), f(5), f(6))); cam.Render();
  RenderTexture.active = rt; var tx = new Texture2D(1600, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1600, 1080), 0, 0); tx.Apply();
  var p = dir + "/" + a[0] + ".png"; System.IO.File.WriteAllBytes(p, tx.EncodeToPNG()); outs += a[0] + " ";
  UnityEngine.Object.DestroyImmediate(tx);
}
RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
return outs;
