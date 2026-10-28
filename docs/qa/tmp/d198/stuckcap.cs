// 끼임 자리 내려다보기 캡처 + 경로 점마다 좌우 3 m 안 벽(Walls_0) 거리
var go = new GameObject("tmpCap"); var cam = go.AddComponent<Camera>();
cam.transform.position = new Vector3(93, 50, -15); cam.transform.rotation = Quaternion.Euler(90, 0, 0); cam.orthographic = true; cam.orthographicSize = 16;
var rt = new RenderTexture(1024, 1024, 24); cam.targetTexture = rt; cam.Render();
RenderTexture.active = rt; var tx = new Texture2D(1024, 1024, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); tx.Apply();
// 경로 점 찍기(주황)
var lines = System.IO.File.ReadAllLines("D:/Github/F1Game/docs/qa/tmp/d198/loop_pts.txt");
var sb = new System.Text.StringBuilder();
foreach (var l in lines) { var s = l.Split(' '); if (s.Length < 2) continue; float x = float.Parse(s[0]), z = float.Parse(s[1]);
  int px = (int)((x - 93 + 16) / 32 * 1024), py = (int)((z + 15 + 16) / 32 * 1024);
  for (int a = -3; a <= 3; a++) for (int b = -3; b <= 3; b++) if (px + a >= 0 && px + a < 1024 && py + b >= 0 && py + b < 1024) tx.SetPixel(px + a, py + b, new Color(1, .45f, .1f));
  if (Mathf.Abs(x - 93) < 10 && Mathf.Abs(z + 15) < 10 && Physics.Raycast(new Vector3(x, 200, z), Vector3.down, out var g, 400)) {
    float mind = 99; for (int k = 0; k < 16; k++) { var dir = Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward; if (Physics.Raycast(g.point + Vector3.up * .4f, dir, out var h, 5f) && h.collider.name == "Walls_0") mind = Mathf.Min(mind, h.distance); }
    sb.Append(x + "," + z + " wall " + mind.ToString("F1") + "\n"); } }
tx.Apply(); System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/d198/bot_stuck_93_-15.png", tx.EncodeToPNG());
RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(rt);
return sb.ToString();
