// 에디트 모드 캡처: s 지점에서 FollowCamera(원화 프리셋) 자세를 흉내내 960×540 으로 렌더. 인자는 아래 변수.
var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BuildPostcardSlice")).First(x => x != null);
var preset = UnityEditor.AssetDatabase.LoadAssetAtPath<CameraFeelPreset>("Assets/Settings/Camera/CameraFeel_4_Postcard.asset");
var cam = Camera.main;
var outs = new System.Collections.Generic.List<string>();
float[] ss = { 5f, 68f, 95f };
string[] names = { "slice-start", "slice-sea-open", "slice-after" };
var car = GameObject.Find("Race");
var rend = car.GetComponentsInChildren<Renderer>();
for (int i = 0; i < ss.Length; i++)
{
    var args = new object[] { ss[i], null, null, null };
    t.GetMethod("Frame").Invoke(null, args);
    var c = (Vector3)args[1]; var f = (Vector3)args[2]; var r = (Vector3)args[3];
    var carPos = c + r * 1.2f + Vector3.up * 0.05f;
    car.transform.position = carPos; car.transform.rotation = Quaternion.LookRotation(f);
    var yawRot = Quaternion.LookRotation(f);
    cam.transform.position = carPos + yawRot * new Vector3(0, preset.height, -preset.distance);
    var look = carPos + Vector3.up * preset.lookHeight + f * preset.lookAhead;
    cam.transform.rotation = Quaternion.LookRotation(look - cam.transform.position);
    cam.fieldOfView = preset.baseFov;
    var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.Render();
    RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply();
    cam.targetTexture = null; RenderTexture.active = null;
    System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/" + names[i] + ".png", tex.EncodeToPNG());
    // 차 화면 점유율: 렌더러 바운드 8꼭짓점 투영 사각형 / 화면
    var b = rend[0].bounds; foreach (var x in rend) b.Encapsulate(x.bounds);
    float x0 = 1, x1 = 0, y0 = 1, y1 = 0;
    for (int k = 0; k < 8; k++) { var p = b.center + Vector3.Scale(b.extents, new Vector3((k & 1) * 2 - 1, (k & 2) - 1, (k & 4) / 2 - 1)); var v = cam.WorldToViewportPoint(p); x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y); }
    outs.Add(names[i] + " carW=" + (x1 - x0).ToString("F2") + " carH=" + (y1 - y0).ToString("F2") + " area=" + ((x1 - x0) * (y1 - y0)).ToString("F3") + " carTop=" + y1.ToString("F2"));
}
// 차 원위치
var a0 = new object[] { 5f, null, null, null }; t.GetMethod("Frame").Invoke(null, a0);
car.transform.position = (Vector3)a0[1] + (Vector3)a0[3] * 1.2f + Vector3.up * .6f; car.transform.rotation = Quaternion.LookRotation((Vector3)a0[2]);
return string.Join(" | ", outs);
