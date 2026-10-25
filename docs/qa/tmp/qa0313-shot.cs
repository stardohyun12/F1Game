// QA 0313: Play 중 카메라를 고정하고 ScreenCapture(게임뷰+포스트+UI). SessionState "qs.at" = "kind,name" kind=ped|hill|top|restore
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/qa-night"));
System.IO.Directory.CreateDirectory(outDir);
var a = UnityEditor.SessionState.GetString("qs.at", "restore,x").Split(',');
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var cam = fc.GetComponent<Camera>();
if (a[0] == "restore") { cam.orthographic = false; fc.enabled = true; return "restored"; }
System.Func<float, float, float> H = (x, z) => Physics.Raycast(new Vector3(x, 400f, z), Vector3.down, out var h, 800f) ? h.point.y : 0f;
fc.enabled = false; cam.orthographic = false;
string info = "";
if (a[0] == "ped")
{
    var peds = UnityEngine.Object.FindObjectsByType<Pedestrian>(FindObjectsSortMode.None).OrderBy(p => p.name).ToArray();
    int i = int.Parse(a[2]);
    var t = peds[i].transform.position;
    var fw = peds[i].transform.forward; fw.y = 0;
    var side = Vector3.Cross(Vector3.up, fw.normalized);
    var cp = t + side * 6f + fw * 4f + Vector3.up * 2.2f;
    cam.fieldOfView = 50f; cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(t + Vector3.up * 0.8f - cp));
    var anim = peds[i].GetComponentInChildren<Animator>();
    info = peds[i].name + " at " + t.ToString("F1") + " ground=" + H(t.x, t.z).ToString("F2") + " anim=" + (anim ? anim.GetInteger("State") + "/" + (anim.GetCurrentAnimatorClipInfo(0).Length > 0 ? anim.GetCurrentAnimatorClipInfo(0)[0].clip.name : "-") : "none") + " n=" + peds.Length;
}
else if (a[0] == "hill")
{
    float x = 128, z = 136; var t = new Vector3(x, H(x, z), z); var cp = t + new Vector3(-28f, 18f, -22f);
    cam.fieldOfView = 55f; cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(t - cp));
}
else if (a[0] == "top")
{
    cam.orthographic = true; cam.orthographicSize = 250f; cam.transform.SetPositionAndRotation(new Vector3(0, 400f, 20f), Quaternion.Euler(90f, 0, 0));
}
ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(outDir, a[1] + ".png"));
return "shot " + a[1] + " " + info;
