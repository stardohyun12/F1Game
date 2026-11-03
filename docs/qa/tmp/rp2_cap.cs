// 2300 캡처: TAG = before/after. 4장 — 8b98 도현 카메라 · 마당 눈높이 · 큰길 카트 높이 · 골목
if (UnityEditor.EditorApplication.isPlaying) return "FAIL play";
var dir = "D:/Github/F1Game/docs/qa/evidence/roadpaint2/"; System.IO.Directory.CreateDirectory(dir);
var sv = UnityEditor.SceneView.lastActiveSceneView; var src = sv != null ? sv.camera : UnityEngine.Camera.main;
System.Func<float,float,float> gy = (x,z) => { var hs = UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(x,400,z), UnityEngine.Vector3.down, 800); float b = float.NegativeInfinity; foreach (var h in hs) { if (h.collider.transform.root.name=="Roadside") continue; if (h.point.y>b) b=h.point.y; } return b; };
var rv = UnityEngine.GameObject.Find("TownV6_Gray").transform.Find("RoadsV2");
RoadSurface main = null, lane = null; float dm = 1e9f, dl = 1e9f; var H = new UnityEngine.Vector2(185,-61);
foreach (UnityEngine.Transform ch in rv) { if (!ch.gameObject.activeInHierarchy || !ch.name.StartsWith("ROAD_V2e")) continue; var rs = ch.GetComponent<RoadSurface>(); if (rs==null||rs.centerline==null||rs.centerline.Length<4) continue;
  float d = 1e9f; foreach (var q in rs.centerline) d = System.Math.Min(d, (new UnityEngine.Vector2(q.x,q.z)-H).magnitude);
  if (ch.name.Contains("camo_309378634_165_1")) { main = rs; dm = 0; }
  if (rs.kind!="main" && rs.width < 4 && d < dl && d > 8) { dl = d; lane = rs; } }
var shots = new System.Collections.Generic.List<(string n, UnityEngine.Vector3 p, UnityEngine.Vector3 t, float fov)>();
shots.Add(("8b98", new UnityEngine.Vector3(183.249f,37.967f,-79.95f), new UnityEngine.Vector3(277.145f,-125.962f,76.22f), 60));
{ float x=185, z=-66.5f; var p = new UnityEngine.Vector3(x, gy(x,z)+1.6f, z); float tx=212, tz=-84; shots.Add(("yard", p, new UnityEngine.Vector3(tx, gy(tx,tz)+1.0f, tz), 60)); }
System.Func<RoadSurface,float,float,(UnityEngine.Vector3,UnityEngine.Vector3)> along = (rs, frac, h) => { var c = rs.centerline; int i = UnityEngine.Mathf.Clamp((int)(c.Length*frac),0,c.Length-2); int j = UnityEngine.Mathf.Min(c.Length-1, i+ System.Math.Max(2,c.Length/4)); var a=c[i]; var b=c[j]; var p=new UnityEngine.Vector3(a.x, gy(a.x,a.z)+h, a.z); var t=new UnityEngine.Vector3(b.x, gy(b.x,b.z)+h*.6f, b.z); return (p,t); };
string info = "";
if (main != null) { var (p,t) = along(main, .15f, 1.0f); shots.Add(("main_kart", p, t, 65)); info += " main="+main.name+" "+p.ToString("F1"); }
if (lane != null) { var (p,t) = along(lane, .2f, 1.6f); shots.Add(("lane", p, t, 65)); info += " lane="+lane.name+" w"+lane.width+" "+p.ToString("F1"); }
var rsd = UnityEngine.GameObject.Find("Roadside"); bool hide = "_TAG" == "_before"; if (rsd != null && hide) rsd.SetActive(false);
var go = new UnityEngine.GameObject("_rp2cap") { hideFlags = UnityEngine.HideFlags.HideAndDontSave }; string res = "";
try { var cam = go.AddComponent<UnityEngine.Camera>(); cam.CopyFrom(src); cam.cameraType = UnityEngine.CameraType.Game; cam.enabled = false; cam.nearClipPlane = .1f;
  foreach (var s in shots) { cam.fieldOfView = s.fov; go.transform.SetPositionAndRotation(s.p, UnityEngine.Quaternion.LookRotation(s.t - s.p)); bool ok = PitCrewPlan.PlanIO.RenderPng(cam, dir + s.n + "_TAG.png", 1600, 900, null); res += s.n + (ok?" ok ":" FAIL "); }
} finally { UnityEngine.Object.DestroyImmediate(go); if (rsd != null) rsd.SetActive(true); }
return res + info;
