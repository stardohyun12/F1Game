// 에디트 모드: 지형만(길·옹벽·건물 끔) 높은 시점 렌더 → hilltop-1-terrain.png
var root = GameObject.Find("Hilltop");
var hide = new[] { "Road", "RetainingWalls", "Buildings" }.Select(n => root.transform.Find(n)).Where(t => t != null).Select(t => t.gameObject).ToList();
foreach (var g in hide) g.SetActive(false);
var camGo = new GameObject("TmpCap"); var cam = camGo.AddComponent<Camera>();
var src = UnityEngine.Object.FindFirstObjectByType<FollowCamera>().GetComponent<Camera>();
cam.CopyFrom(src); cam.farClipPlane = 2000f; cam.fieldOfView = 55f;
camGo.transform.position = new Vector3(-170f, 120f, 380f); camGo.transform.LookAt(new Vector3(30f, 0f, 160f));
var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
RenderTexture.active = rt; var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply(); RenderTexture.active = null;
System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/hilltop-1-terrain.png", tex.EncodeToPNG());
cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(camGo); UnityEngine.Object.DestroyImmediate(rt);
foreach (var g in hide) g.SetActive(true);
return "saved terrain cap";
