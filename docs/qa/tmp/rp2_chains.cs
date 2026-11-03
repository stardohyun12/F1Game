var top = UnityEngine.GameObject.Find("TownV6_Gray").transform; var rv = top.Find("RoadsV2");
var H = new UnityEngine.Vector2(185,-61); var G = new UnityEngine.Vector2(234,-54);
var sb = new System.Text.StringBuilder(); int nE=0,nP=0,nJ=0;
foreach (UnityEngine.Transform ch in rv) {
  if (!ch.gameObject.activeInHierarchy) continue;
  var mr = ch.GetComponent<UnityEngine.MeshRenderer>(); if (mr==null) continue; var b = mr.bounds;
  var c2 = new UnityEngine.Vector2(b.center.x,b.center.z); float ext = new UnityEngine.Vector2(b.extents.x,b.extents.z).magnitude;
  if ((c2-H).magnitude > 120+ext && (c2-G).magnitude > 120+ext) continue;
  var rs = ch.GetComponent<RoadSurface>(); var col = ch.GetComponent<UnityEngine.Collider>();
  string pre = ch.name.Length>8? ch.name.Substring(0,8):ch.name; if (pre=="ROAD_V2e") nE++; else if (pre=="ROAD_V2p") nP++; else if (pre=="ROAD_V2j") nJ++;
  sb.AppendLine(ch.name + " | " + (rs!=null? (rs.roadId+" "+rs.kind+" w"+rs.width+" cl"+(rs.centerline!=null?rs.centerline.Length:0)) : "noRS") + " col=" + (col!=null?col.GetType().Name+":"+col.gameObject.layer:"none") + " c=" + b.center.ToString("F0"));
}
sb.Insert(0, "E="+nE+" P="+nP+" J="+nJ+"\n");
var d = UnityEngine.GameObject.Find("D198_Buildings"); if (d!=null) foreach (UnityEngine.Transform s in d.transform) sb.AppendLine("D198/"+s.name+" "+s.position.ToString("F1"));
System.IO.File.WriteAllText("D:/Github/F1Game/docs/qa/tmp/rp2_chains.txt", sb.ToString());
return sb.ToString().Substring(0, System.Math.Min(600, sb.Length));
