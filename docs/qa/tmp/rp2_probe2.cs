var H = new UnityEngine.Vector2(185,-61); var G = new UnityEngine.Vector2(234,-54);
var sb = new System.Text.StringBuilder();
// 1) 반경 안 렌더러를 씬 루트(+2단계)별로 센다
var cnt = new System.Collections.Generic.Dictionary<string,int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None)) {
  var p = r.bounds.center; var q = new UnityEngine.Vector2(p.x,p.z);
  if ((q-H).magnitude>130 && (q-G).magnitude>130) continue;
  var t = r.transform; var path = new System.Collections.Generic.List<string>(); while (t!=null){ path.Insert(0,t.name); t=t.parent; }
  string key = string.Join("/", path.GetRange(0, System.Math.Min(3, path.Count-1>0?path.Count-1:1)));
  if (key.Contains("ROAD_V2")) key = path[0]+"/"+path[1]+"/ROAD_*";
  cnt[key] = (cnt.TryGetValue(key, out var c)? c:0)+1;
}
foreach (var kv in cnt) if (kv.Value>=1) sb.AppendLine(kv.Value+"\t"+kv.Key);
// 2) V2p 메시 정점 쌍 폭
var rv = UnityEngine.GameObject.Find("TownV6_Gray").transform.Find("RoadsV2");
var p0 = rv.Find("ROAD_V2p_camo_26348044_82_0"); var m = p0.GetComponent<UnityEngine.MeshFilter>().sharedMesh; var v = m.vertices;
sb.AppendLine("V2p verts="+v.Length+" tris="+m.triangles.Length/3+" topo="+m.GetTopology(0));
for (int i=0;i+1<System.Math.Min(v.Length,12);i+=2) sb.AppendLine("  pair "+i+" "+p0.TransformPoint(v[i]).ToString("F2")+" "+p0.TransformPoint(v[i+1]).ToString("F2")+" d="+(v[i]-v[i+1]).magnitude.ToString("F2"));
var tr = m.triangles; sb.AppendLine("  tri0 "+tr[0]+","+tr[1]+","+tr[2]+" tri1 "+tr[3]+","+tr[4]+","+tr[5]);
// 3) 문 이름
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None)) {
  var n=t.name.ToLower(); if (!(n.Contains("door")||n.Contains("gate"))) continue; var q=new UnityEngine.Vector2(t.position.x,t.position.z);
  if ((q-H).magnitude>130 && (q-G).magnitude>130) continue; sb.AppendLine("door: "+t.name+" "+t.position.ToString("F1")+" parent="+(t.parent!=null?t.parent.name:""));
}
System.IO.File.WriteAllText("D:/Github/F1Game/docs/qa/tmp/rp2_probe2.txt", sb.ToString());
return "ok";
