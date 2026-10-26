// 1514: zoom_bw 화면의 큰 회색 삼각형(픽셀 750,200 = 아래쪽 / 600,420)을 그리는 렌더러 찾기 — 렌더러 이름 앞부분별로 끄고 픽셀 변화
var go = new GameObject("c1514cam"); var cam = go.AddComponent<Camera>(); cam.nearClipPlane = 0.3f; cam.farClipPlane = 1500f;
var s = new Vector3(-0.7f, 0, -0.7f).normalized; var p = new Vector3(-96, 3, -111) + s * 7 + new Vector3(s.z, 0, -s.x) * 14; var cp = p + s * 16 + Vector3.up * 9;
cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(p - cp)); cam.fieldOfView = 55;
var rt = new RenderTexture(1280, 720, 24); var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
var px = new[] { (760, 180), (600, 300), (520, 160) };   // 텍스처 좌표(아래가 0)
System.Func<string> grab = () => { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null; return string.Join(" ", px.Select(q => ColorUtility.ToHtmlStringRGB(tex.GetPixel(q.Item1, q.Item2)))); };
var baseC = grab(); var sb = new System.Text.StringBuilder("base " + baseC + "\n");
var all = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.bounds.SqrDistance(p) < 900f).ToList();
foreach (var grp in all.GroupBy(r => r.name.Split('_')[0] + "_" + (r.name.Split('_').Length > 1 ? r.name.Split('_')[1] : "")))
{
    foreach (var r in grp) r.enabled = false; var c = grab(); foreach (var r in grp) r.enabled = true;
    if (c != baseC) sb.Append(grp.Key + " x" + grp.Count() + " -> " + c + " e.g. " + grp.First().name + " mat " + grp.First().sharedMaterial.name + "\n");
}
UnityEngine.Object.DestroyImmediate(go);
return sb.ToString();
