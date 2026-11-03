// 0141 캡처(parcels3): 2판 cap.cs 와 같은 카메라 + hill_eye(반지하·2단 많은 언덕 눈높이, hill.txt 의 x,z,tx,tz)
if (UnityEditor.EditorApplication.isPlaying) return "FAIL play";
var TAG = "3pan";
var dir = "D:/Github/F1Game/docs/qa/evidence/parcels3/"; System.IO.Directory.CreateDirectory(dir);
var sv = UnityEditor.SceneView.lastActiveSceneView; var src = sv != null ? sv.camera : UnityEngine.Camera.main;
System.Func<float,float,float> gy = (x,z) => { var hs = UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(x,400,z), UnityEngine.Vector3.down, 800); float b = float.NegativeInfinity; foreach (var h in hs) { var rn = h.collider.transform.root.name; if (rn=="Roadside"||rn=="Parcels_Plan") continue; if (h.point.y>b) b=h.point.y; } return b; };
var rv = UnityEngine.GameObject.Find("TownV6_Gray").transform.Find("RoadsV2");
RoadSurface main = null;
foreach (UnityEngine.Transform ch in rv) { if (!ch.gameObject.activeInHierarchy || !ch.name.StartsWith("ROAD_V2e")) continue; var rs = ch.GetComponent<RoadSurface>(); if (rs==null||rs.centerline==null||rs.centerline.Length<4) continue; if (ch.name.Contains("camo_309378634_165_1")) main = rs; }
var shots = new System.Collections.Generic.List<(string n, UnityEngine.Vector3 p, UnityEngine.Vector3 t, float fov)>();
{ var p = new UnityEngine.Vector3(141.02f,30.22f,-62.69f); var q = UnityEngine.Quaternion.Euler(16.5f,72f,0); shots.Add(("dohyun", p, p + q*UnityEngine.Vector3.forward*50f, 60)); }
shots.Add(("8b98", new UnityEngine.Vector3(183.249f,37.967f,-79.95f), new UnityEngine.Vector3(277.145f,-125.962f,76.22f), 60));
System.Func<RoadSurface,float,float,(UnityEngine.Vector3,UnityEngine.Vector3)> along = (rs, frac, h) => { var c = rs.centerline; int i = UnityEngine.Mathf.Clamp((int)(c.Length*frac),0,c.Length-2); int j = UnityEngine.Mathf.Min(c.Length-1, i+ System.Math.Max(2,c.Length/4)); var a=c[i]; var b=c[j]; var p=new UnityEngine.Vector3(a.x, gy(a.x,a.z)+h, a.z); var t=new UnityEngine.Vector3(b.x, gy(b.x,b.z)+h*.6f, b.z); return (p,t); };
string info = "";
if (main != null) { var (p,t) = along(main, .15f, 1.0f); shots.Add(("main_kart", p, t, 65)); info += " main="+p.ToString("F1"); }
{ float yaw = 85.15f * UnityEngine.Mathf.Deg2Rad; var f = new UnityEngine.Vector2(UnityEngine.Mathf.Sin(yaw), UnityEngine.Mathf.Cos(yaw)); var g = new UnityEngine.Vector2(234.1f,-53.79f); var e = g + f*14f; var p = new UnityEngine.Vector3(e.x, gy(e.x,e.y)+1.6f, e.y); shots.Add(("garage", p, new UnityEngine.Vector3(g.x, gy(g.x,g.y)+2.5f, g.y), 70)); }
shots.Add(("top_all", new UnityEngine.Vector3(0f, 620f, -430f), new UnityEngine.Vector3(0f, 20f, 0f), 60));
shots.Add(("top_home", new UnityEngine.Vector3(170f, 200f, -170f), new UnityEngine.Vector3(170f, 15f, -50f), 60));
{ float x=146, z=-80; var p = new UnityEngine.Vector3(x, gy(x,z)+1.6f, z); float tx=160, tz=-52; shots.Add(("slope_eye", p, new UnityEngine.Vector3(tx, gy(tx,tz)+3f, tz), 65)); info += " slope="+p.ToString("F1"); }
var hp = "D:/Github/F1Game/docs/qa/tmp/parcels3/hill.txt";
if (System.IO.File.Exists(hp)) { var a = System.IO.File.ReadAllText(hp).Trim().Split(','); float x=float.Parse(a[0]), z=float.Parse(a[1]), tx=float.Parse(a[2]), tz=float.Parse(a[3]); var p = new UnityEngine.Vector3(x, gy(x,z)+1.6f, z); shots.Add(("hill_eye", p, new UnityEngine.Vector3(tx, gy(tx,tz)+4f, tz), 65)); info += " hill="+p.ToString("F1"); }
var go = new UnityEngine.GameObject("_pccap") { hideFlags = UnityEngine.HideFlags.HideAndDontSave }; string res = "";
try { var cam = go.AddComponent<UnityEngine.Camera>(); cam.CopyFrom(src); cam.cameraType = UnityEngine.CameraType.Game; cam.enabled = false; cam.nearClipPlane = .1f; cam.farClipPlane = 3000;
  foreach (var s in shots) { cam.fieldOfView = s.fov; go.transform.SetPositionAndRotation(s.p, UnityEngine.Quaternion.LookRotation(s.t - s.p)); bool ok = PitCrewPlan.PlanIO.RenderPng(cam, dir + s.n + "_" + TAG + ".png", 1600, 900, null); res += s.n + (ok?" ok ":" FAIL "); }
} finally { UnityEngine.Object.DestroyImmediate(go); }
return res + info;
