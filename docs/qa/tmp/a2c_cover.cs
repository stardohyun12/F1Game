// 과제 0619 — Cam_B 화면에서 나무(autumn_tree_*)가 덮는 면적: 위 절반(모서리 쪽) 비율 · 전체 비율. 나무 렌더러만 마젠타로 바꿔 1280×720 렌더 후 되돌림. 하늘 비율도 같이.
Camera cam = null; foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (c.name == "Cam_B_Game") cam = c;
var mag = new Material(Shader.Find("PitCrew/SceneA/Look")); mag.SetTexture("_BaseMap", Texture2D.whiteTexture); mag.SetColor("_Tint", Color.magenta); mag.SetFloat("_FogMax", 0); mag.SetFloat("_LightAmt", 0);
string Count(System.Func<Renderer, bool> pick) {
  var saved = new System.Collections.Generic.List<(Renderer, Material[])>();
  foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (pick(r)) { saved.Add((r, r.sharedMaterials)); var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) { ms[i] = new Material(mag); ms[i].SetTexture("_BaseMap", r.sharedMaterials[i].GetTexture("_BaseMap")); ms[i].SetFloat("_Cutoff", 0.5f); ms[i].SetColor("_Tint", new Color(1, 0, 1, 1)); } r.sharedMaterials = ms; }
  var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
  foreach (var s in saved) s.Item1.sharedMaterials = s.Item2;
  RenderTexture.active = rt; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); RenderTexture.active = null; rt.Release();
  var px = tx.GetPixels32(); int top = 0, all = 0;   // ReadPixels 의 0 행 = 화면 아래
  for (int y = 0; y < 720; y++) for (int x = 0; x < 1280; x++) { var p = px[y * 1280 + x]; bool m = p.g == 0 && p.r + p.b > 0; /* 틴트 (1,0,1) × 원래 텍스처 → 초록 채널만 0 */ if (m) { all++; if (y >= 360) top++; } }
  return $"위 절반 {(100f * top / (1280 * 360)):F1}% · 전체 {(100f * all / (1280 * 720)):F1}%"; }
var tree = Count(r => r.transform.root.name.StartsWith("SceneA2") && r.gameObject.name.Length > 0 && FindTop(r.transform).StartsWith("autumn_tree"));
var sky = Count(r => r.name == "Sky");
return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " 나무: " + tree + " | 하늘: " + sky;
string FindTop(Transform t) { while (t.parent && !t.parent.name.StartsWith("SceneA2")) t = t.parent; return t.name; }
