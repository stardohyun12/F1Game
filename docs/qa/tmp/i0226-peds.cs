// 과제 0226: 행인 Animator 상태 확인 + 상점가 인도 행인 캡처 (Play 중)
var peds = UnityEngine.Object.FindObjectsByType<Pedestrian>(FindObjectsSortMode.None).OrderBy(p => p.name).ToArray();
var sb = new System.Text.StringBuilder();
foreach (var p in peds)
{
    var r = p.GetComponentInChildren<Resident>();
    var a = r != null ? r.anim : null;
    string st = a != null ? ("State=" + a.GetInteger("State") + " WalkSpeed=" + a.GetFloat("WalkSpeed").ToString("F2") + " clip=" + (a.GetCurrentAnimatorClipInfo(0).Length > 0 ? a.GetCurrentAnimatorClipInfo(0)[0].clip.name : "-")) : "no anim";
    sb.AppendLine(p.name + " " + p.transform.position.ToString("F0") + " " + st);
}
var target = peds.FirstOrDefault(p => { var r = p.GetComponentInChildren<Resident>(); return r != null && r.anim != null && r.anim.GetInteger("State") == 1; }) ?? peds.FirstOrDefault();
if (target != null)
{
    var go = new GameObject("i0226_cam"); var cam = go.AddComponent<Camera>();
    var main = Camera.main; if (main != null) { cam.CopyFrom(main); }
    cam.fieldOfView = 40f;
    var tp = target.transform.position + Vector3.up * 0.8f;
    // 차도 쪽(본선 중심선 가장 가까운 점)에서 행인을 본다 — 건물 안에 카메라가 박히지 않게
    var cl = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "main" && r.centerline != null && r.centerline.Length > 0).centerline;
    var rp = cl.OrderBy(c => (new Vector2(c.x - tp.x, c.z - tp.z)).sqrMagnitude).First();
    var away = rp - tp; away.y = 0; away = away.normalized;
    var along = Vector3.Cross(Vector3.up, away);
    go.transform.position = tp + away * 5.5f + along * 3f + Vector3.up * 1.0f;
    go.transform.LookAt(tp);
    var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render();
    RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
    System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath("../docs/qa/evidence/integrate-v1/peds_shopstreet.png"), tex.EncodeToPNG());
    RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(go);
    sb.AppendLine("shot " + target.name);
}
return sb.ToString();
