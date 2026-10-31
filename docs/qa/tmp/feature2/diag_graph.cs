var roads = new System.Collections.Generic.List<RoadSurface>();
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
  if (r.centerline != null && r.centerline.Length >= 2 && r.kind != "stairs" && (r.kind == null || !r.kind.StartsWith("walk"))) roads.Add(r);
roads.Sort((a,b)=>string.CompareOrdinal(a.roadId,b.roadId));
var nodes=new System.Collections.Generic.List<Vector3>(); var road=new System.Collections.Generic.List<int>();
for(int ri=0;ri<roads.Count;ri++) foreach(var p in roads[ri].centerline){nodes.Add(p);road.Add(ri);}
int n=nodes.Count; var par=new int[n]; for(int i=0;i<n;i++)par[i]=i;
int F(int x){while(par[x]!=x){par[x]=par[par[x]];x=par[x];}return x;}
void U(int a,int b){par[F(a)]=F(b);}
float R = 2.5f; int ga=roads.FindIndex(r=>r.roadId=="drive_garage"); for(int i=0;i<n;i++) if(road[i]==ga) for(int j=0;j<n;j++) if(road[j]!=ga && Vector3.Distance(nodes[i],nodes[j])<6f) U(i,j);
for(int i=0;i+1<n;i++) if(road[i]==road[i+1]) U(i,i+1);
for(int i=0;i<n;i++) for(int j=i+1;j<n;j++) if(road[i]!=road[j] && Vector3.Distance(nodes[i],nodes[j])<R) U(i,j);
Vector3 from=new Vector3(245.33f,24.66f,-61.71f), to=new Vector3(93.79f,4.55f,-288.05f);
int Near(Vector3 q){int bi=0;float bd=1e9f;for(int i=0;i<n;i++){var d=nodes[i]-q;d.y=0;if(d.sqrMagnitude<bd){bd=d.sqrMagnitude;bi=i;}}return bi;}
int na=Near(from), nb=Near(to);
var comps=new System.Collections.Generic.Dictionary<int,int>(); for(int i=0;i<n;i++){int f=F(i);comps[f]=comps.ContainsKey(f)?comps[f]+1:1;}
string s="roads="+roads.Count+" comps="+comps.Count+" start comp "+comps[F(na)]+" nodes road="+roads[road[na]].roadId+":"+roads[road[na]].kind+" end comp "+comps[F(nb)]+" road="+roads[road[nb]].roadId+":"+roads[road[nb]].kind+"\n";
float best=1e9f; int ba=-1,bb=-1;
for(int i=0;i<n;i++) if(F(i)==F(na)) for(int j=0;j<n;j++) if(F(j)==F(nb)){float d=Vector3.Distance(nodes[i],nodes[j]); if(d<best){best=d;ba=i;bb=j;}}
s+="closest dist="+best+" A="+nodes[ba]+" "+roads[road[ba]].roadId+":"+roads[road[ba]].kind+" B="+nodes[bb]+" "+roads[road[bb]].roadId+":"+roads[road[bb]].kind+"\n";
var ka=new System.Collections.Generic.HashSet<string>(); for(int i=0;i<n;i++) if(F(i)==F(na)) ka.Add(roads[road[i]].roadId+":"+roads[road[i]].kind);
s+="startcomp roads: "+string.Join(",",ka);
return s;
