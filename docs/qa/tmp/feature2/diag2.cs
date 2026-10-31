var roads = new System.Collections.Generic.List<RoadSurface>();
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
  if (r.centerline != null && r.centerline.Length >= 2 && r.kind != "stairs" && (r.kind == null || !r.kind.StartsWith("walk"))) roads.Add(r);
roads.Sort((a,b)=>string.CompareOrdinal(a.roadId,b.roadId));
var nodes=new System.Collections.Generic.List<Vector3>(); var road=new System.Collections.Generic.List<int>();
for(int ri=0;ri<roads.Count;ri++) foreach(var p in roads[ri].centerline){nodes.Add(p);road.Add(ri);}
int n=nodes.Count; string s="";
var g = roads.Find(r=>r.roadId=="drive_garage");
s+="drive_garage ends: "+g.centerline[0]+" -> "+g.centerline[g.centerline.Length-1]+" n="+g.centerline.Length+" width="+g.width+"\n";
foreach(var end in new[]{g.centerline[0], g.centerline[g.centerline.Length-1]}){
  var list=new System.Collections.Generic.List<(float,int)>();
  for(int i=0;i<n;i++){ if(roads[road[i]]==g) continue; var d=nodes[i]-end; list.Add((d.magnitude,i)); }
  list.Sort((a,b)=>a.Item1.CompareTo(b.Item1));
  s+="near "+end+": ";
  for(int k=0;k<4;k++) s+=list[k].Item1.ToString("F1")+"m "+roads[road[list[k].Item2]].roadId+":"+roads[road[list[k].Item2]].kind+" "+nodes[list[k].Item2]+" | ";
  s+="\n";
}
return s;
