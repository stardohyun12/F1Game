var roads = new System.Collections.Generic.List<RoadSurface>();
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
  if (r.centerline != null && r.centerline.Length >= 2 && r.kind != "stairs" && (r.kind == null || !r.kind.StartsWith("walk"))) roads.Add(r);
roads.Sort((a,b)=>string.CompareOrdinal(a.roadId,b.roadId));
var nodes=new System.Collections.Generic.List<Vector3>(); var road=new System.Collections.Generic.List<int>();
for(int ri=0;ri<roads.Count;ri++) foreach(var p in roads[ri].centerline){nodes.Add(p);road.Add(ri);}
int n=nodes.Count;
Vector3 from=new Vector3(245.19f,24.69f,-67.33f), to=new Vector3(93.79f,4.55f,-288.05f);
int Near(Vector3 q){int bi=0;float bd=1e9f;for(int i=0;i<n;i++){var d=nodes[i]-q;d.y=0;if(d.sqrMagnitude<bd){bd=d.sqrMagnitude;bi=i;}}return bi;}
int na=Near(from), nb=Near(to);
string s="";
foreach(float R in new float[]{2.5f,4f,6f,8f,10f,12f,15f,20f}){
  var par=new int[n]; for(int i=0;i<n;i++)par[i]=i;
  int F(int x){while(par[x]!=x){par[x]=par[par[x]];x=par[x];}return x;}
  for(int i=0;i+1<n;i++) if(road[i]==road[i+1]) par[F(i)]=F(i+1);
  var grid=new System.Collections.Generic.Dictionary<(int,int),System.Collections.Generic.List<int>>();
  float cs=R;
  for(int i=0;i<n;i++){var k=((int)Mathf.Floor(nodes[i].x/cs),(int)Mathf.Floor(nodes[i].z/cs)); if(!grid.TryGetValue(k,out var l))grid[k]=l=new System.Collections.Generic.List<int>(); l.Add(i);}
  for(int i=0;i<n;i++){int gx=(int)Mathf.Floor(nodes[i].x/cs),gz=(int)Mathf.Floor(nodes[i].z/cs);
    for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++) if(grid.TryGetValue((gx+dx,gz+dz),out var l)) foreach(var j in l) if(j>i&&road[j]!=road[i]&&Vector3.Distance(nodes[i],nodes[j])<R && Mathf.Abs(nodes[i].y-nodes[j].y)<3f) par[F(i)]=F(j);}
  var cc=new System.Collections.Generic.HashSet<int>(); for(int i=0;i<n;i++)cc.Add(F(i));
  s+="R="+R+" comps="+cc.Count+" connected="+(F(na)==F(nb))+"\n";
}
return s;
