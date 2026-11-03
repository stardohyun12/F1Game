// 0141: 도현 캡처 자리 빈 구덩이 2곳이 무엇인지 + 길 이름(구역) 확인
var go = new UnityEngine.GameObject("_pr"){hideFlags=UnityEngine.HideFlags.HideAndDontSave}; string r="";
try { var cam = go.AddComponent<UnityEngine.Camera>(); cam.enabled=false; cam.fieldOfView=60; cam.aspect=16f/9f; cam.pixelRect=new UnityEngine.Rect(0,0,1600,900);
 go.transform.SetPositionAndRotation(new UnityEngine.Vector3(141.02f,30.22f,-62.69f), UnityEngine.Quaternion.Euler(16.5f,72f,0));
 foreach (var px in new[]{new UnityEngine.Vector2(460,900-195), new UnityEngine.Vector2(520,900-210), new UnityEngine.Vector2(790,900-215)}) {
  var ray = cam.ViewportPointToRay(new UnityEngine.Vector3(px.x/1600f, px.y/900f, 0)); var hs = UnityEngine.Physics.RaycastAll(ray, 2000); System.Array.Sort(hs,(a,b)=>a.distance.CompareTo(b.distance));
  r += px+": "; foreach (var h in hs) { r += h.collider.name+"@"+h.point.ToString("F1")+" "; break; } r+="\n";
  if (hs.Length>0) { var p=hs[0].point; var mr = UnityEngine.GameObject.Find("Parcels_Plan").transform.Find("Masses"); foreach (UnityEngine.Transform m in mr) { var d=new UnityEngine.Vector2(m.position.x-p.x,m.position.z-p.z).magnitude; if (d<14) r += "   mass "+m.name+" d="+d.ToString("F1")+" y="+m.position.y.ToString("F1")+" pos="+m.position.ToString("F1")+"\n"; } }
 }
} finally { UnityEngine.Object.DestroyImmediate(go); }
var rv = UnityEngine.GameObject.Find("TownV6_Gray").transform.Find("RoadsV2"); var names = new System.Collections.Generic.HashSet<string>();
foreach (UnityEngine.Transform ch in rv) { var parts = ch.name.Split('_'); names.Add(parts.Length>2? parts[2] : ch.name); }
r += "road name parts: " + string.Join(",", names);
int i=0; foreach (UnityEngine.Transform ch in rv) { if (i++%25==0) r += "\n  "+ch.name; }
return r;
