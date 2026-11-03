// 0029 비탈 집 가까이: 덩어리 앞(길 쪽) 14 m · 옆 5 m · 눈높이 2.5 m 에서 덩어리 가운데를 본다
if (UnityEditor.EditorApplication.isPlaying) return "FAIL play";
var dir = "D:/Github/F1Game/docs/qa/evidence/parcels2/";
System.Func<float,float,float> gy = (x,z) => { var hs = UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(x,400,z), UnityEngine.Vector3.down, 800); float b = float.NegativeInfinity; foreach (var h in hs) { var rn = h.collider.transform.root.name; if (rn=="Roadside"||rn=="Parcels_Plan") continue; if (h.point.y>b) b=h.point.y; } return b; };
var list = new (string n, float x, float y, float z, float yaw, float d, float side)[] {
  ("C1096_stepped", 358.5f, 35.02f, -50.09f, -64f, 6.88f, 6f),
  ("C602_stepped", 312.99f, 8.36f, -167.73f, 7.7f, 3.96f, -6f),
  ("B666_pad", 160.45f, 10.66f, -129.05f, 156.6f, 7.75f, 6f),
  ("B943_pad", 348.67f, 46.87f, 14.05f, 90f, 7.2f, -6f) };
var sv = UnityEditor.SceneView.lastActiveSceneView; var src = sv != null ? sv.camera : UnityEngine.Camera.main;
var go = new UnityEngine.GameObject("_pccap") { hideFlags = UnityEngine.HideFlags.HideAndDontSave }; string res = "";
try { var cam = go.AddComponent<UnityEngine.Camera>(); cam.CopyFrom(src); cam.cameraType = UnityEngine.CameraType.Game; cam.enabled = false; cam.nearClipPlane = .1f; cam.farClipPlane = 3000; cam.fieldOfView = 60;
  foreach (var s in list) {
    float r = s.yaw * UnityEngine.Mathf.Deg2Rad; var f = new UnityEngine.Vector2(UnityEngine.Mathf.Sin(r), UnityEngine.Mathf.Cos(r)); var t = new UnityEngine.Vector2(f.y, -f.x);
    var c = new UnityEngine.Vector2(s.x, s.z); var e = c + f * 14f + t * s.side * 2f; var p = new UnityEngine.Vector3(e.x, s.y + 16f, e.y);
    var tc = c - f * (s.d * .5f); var tg = new UnityEngine.Vector3(tc.x, s.y + 2f, tc.y);
    go.transform.SetPositionAndRotation(p, UnityEngine.Quaternion.LookRotation(tg - p));
    bool ok = PitCrewPlan.PlanIO.RenderPng(cam, dir + s.n + "_after.png", 1600, 900, null); res += s.n + (ok ? " ok " : " FAIL ") + p.ToString("F1") + " | ";
  }
} finally { UnityEngine.Object.DestroyImmediate(go); }
return res;
