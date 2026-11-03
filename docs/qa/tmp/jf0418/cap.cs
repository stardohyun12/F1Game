// 0418 전후 캡처: 도현 8b98 코멘트 카메라 + 정비소 진입로(DriveC 235.5,-57.5). 태그 = fp_tag.txt. 에디트 모드, 임시 카메라 → PNG
var dir = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/evidence/junction-fit");
System.IO.Directory.CreateDirectory(dir);
var tag = System.IO.File.ReadAllText(dir + "/../../tmp/jf0418/fp_tag.txt").Trim();
var shots = new (string n, Vector3 pos, Vector3 look)[] {
  ("8b98", new Vector3(183.249f, 37.967f, -79.95f), new Vector3(193.952f, 22.162f, -69.747f)),
  ("garage_drive", new Vector3(222f, 30f, -76f), new Vector3(236f, 22f, -58f)),
  ("garage_drive_low", new Vector3(226f, 24.5f, -68f), new Vector3(240f, 22.5f, -52f)),
};
var go = new GameObject("_cap0418"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 60; cam.nearClipPlane = .1f; cam.farClipPlane = 2000;
var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; var outs = "";
foreach (var s in shots)
{
  go.transform.position = s.pos; go.transform.LookAt(s.look); cam.Render();
  RenderTexture.active = rt; var tx = new Texture2D(1600, 900, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tx.Apply();
  var p = dir + "/" + s.n + "_" + tag + ".png"; System.IO.File.WriteAllBytes(p, tx.EncodeToPNG()); outs += p + " ";
  UnityEngine.Object.DestroyImmediate(tx);
}
RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
return outs;
