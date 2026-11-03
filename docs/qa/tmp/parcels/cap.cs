// 2356 캡처: TAG = before/after. 5장 — 8b98 도현 카메라 · 집 앞 큰길 카트 높이 · 정비소 앞 · 120 m 위에서 · 마당 눈높이
if (UnityEditor.EditorApplication.isPlaying) return "FAIL play";
var dir = "D:/Github/F1Game/docs/qa/evidence/parcels1/"; System.IO.Directory.CreateDirectory(dir);
var sv = UnityEditor.SceneView.lastActiveSceneView; var src = sv != null ? sv.camera : UnityEngine.Camera.main;
System.Func<float,float,float> gy = (x,z) => { var hs = UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(x,400,z), UnityEngine.Vector3.down, 800); float b = float.NegativeInfinity; foreach (var h in hs) { var rn = h.collider.transform.root.name; if (rn=="Roadside"||rn=="Parcels_Plan") continue; if (h.point.y>b) b=h.point.y; } return b; };
var rv = UnityEngine.GameObject.Find("TownV6_Gray").transform.Find("RoadsV2");
RoadSurface main = null;
foreach (UnityEngine.Transform ch in rv) { if (!ch.gameObject.activeInHierarchy || !ch.name.StartsWith("ROAD_V2e")) continue; var rs = ch.GetComponent<RoadSurface>(); if (rs==null||rs.centerline==null||rs.centerline.Length<4) continue; if (ch.name.Contains("camo_309378634_165_1")) main = rs; }
var shots = new System.Collections.Generic.List<(string n, UnityEngine.Vector3 p, UnityEngine.Vector3 t, float fov)>();
shots.Add(("8b98", new UnityEngine.Vector3(183.249f,37.967f,-79.95f), new UnityEngine.Vector3(277.145f,-125.962f,76.22f), 60));
System.Func<RoadSurface,float,float,(UnityEngine.Vector3,UnityEngine.Vector3)> along = (rs, frac, h) => { var c = rs.centerline; int i = UnityEngine.Mathf.Clamp((int)(c.Length*frac),0,c.Length-2); int j = UnityEngine.Mathf.Min(c.Length-1, i+ System.Math.Max(2,c.Length/4)); var a=c[i]; var b=c[j]; var p=new UnityEngine.Vector3(a.x, gy(a.x,a.z)+h, a.z); var t=new UnityEngine.Vector3(b.x, gy(b.x,b.z)+h*.6f, b.z); return (p,t); };
string info = "";
if (main != null) { var (p,t) = along(main, .15f, 1.0f); shots.Add(("main_kart", p, t, 65)); info += " main="+p.ToString("F1"); }
{ float yaw = 85.15f * UnityEngine.Mathf.Deg2Rad; var f = new UnityEngine.Vector2(UnityEngine.Mathf.Sin(yaw), UnityEngine.Mathf.Cos(yaw)); var g = new UnityEngine.Vector2(234.1f,-53.79f); var e = g + f*14f; var p = new UnityEngine.Vector3(e.x, gy(e.x,e.y)+1.6f, e.y); shots.Add(("garage", p, new UnityEngine.Vector3(g.x, gy(g.x,g.y)+2.5f, g.y), 70)); info += " garage="+p.ToString("F1"); }
shots.Add(("top120", new UnityEngine.Vector3(209.5f, 250f, -150f), new UnityEngine.Vector3(209.5f, 25f, -57f), 60));
{ float x=185, z=-66.5f; var p = new UnityEngine.Vector3(x, gy(x,z)+1.6f, z); float tx=212, tz=-84; shots.Add(("yard", p, new UnityEngine.Vector3(tx, gy(tx,tz)+1.0f, tz), 60)); }
var go = new UnityEngine.GameObject("_pccap") { hideFlags = UnityEngine.HideFlags.HideAndDontSave }; string res = "";
try { var cam = go.AddComponent<UnityEngine.Camera>(); cam.CopyFrom(src); cam.cameraType = UnityEngine.CameraType.Game; cam.enabled = false; cam.nearClipPlane = .1f; cam.farClipPlane = 2000;
  foreach (var s in shots) { cam.fieldOfView = s.fov; go.transform.SetPositionAndRotation(s.p, UnityEngine.Quaternion.LookRotation(s.t - s.p)); bool ok = PitCrewPlan.PlanIO.RenderPng(cam, dir + s.n + "_TAG.png", 1600, 900, null); res += s.n + (ok?" ok ":" FAIL "); }
} finally { UnityEngine.Object.DestroyImmediate(go); }
return res + info;
