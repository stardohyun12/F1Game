using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class BuildBayLoop
{
    // Geometry design adapted from Acid3croco/berat-racer, tools/roads/junction.py
    // ff79911c14930a3d099f61dec2254d638bb061b2 (MIT): facing-kerb trims,
    // clear mouths, swallow short links, union stubs, shared mouth coordinates, one junction plane.
    // Independent C# implementation; polygon operations use vendored Clipper2 (Boost-1.0).
    sealed class V2J
    {
        public int n;
        public List<int> nodes = new List<int>();
        public List<(V2L c, bool st, Vector3 u)> inc = new List<(V2L, bool, Vector3)>();
        public List<V2L> internalLinks = new List<V2L>();
    }
    static Vector3 V2At(List<Vector3> p, float d)
    {
        for(int i=1;i<p.Count;i++) { float l=Fl(p[i]-p[i-1]).magnitude; if(d<=l) return Vector3.Lerp(p[i-1],p[i],d/Mathf.Max(l,1e-6f)); d-=l; }
        return p[p.Count-1];
    }
    static List<Vector3> V2Outward(V2L c,bool st) { var p=c.p.ToList(); if(!st)p.Reverse(); return p; }
    static Clipper2Lib.PathD V2Path(IEnumerable<Vector3> points)
    {
        var p=new Clipper2Lib.PathD(points.Select(q=>new Clipper2Lib.PointD(q.x,q.z)));
        if(Clipper2Lib.Clipper.Area(p)<0)p.Reverse(); return p;
    }
    static List<Vector3> V2Outline(List<Vector3> band)
    {
        var p=new List<Vector3>(); for(int i=0;i<band.Count;i+=2)p.Add(band[i]);
        for(int i=band.Count-1;i>=0;i-=2)p.Add(band[i]); return p;
    }
    static List<V2J> V2PrepareJunctions(List<V2L> chains,List<Vector3> nodes,Dictionary<int,float> heights,List<string> log)
    {
        var fans=new Dictionary<int,List<(V2L c,bool st,Vector3 u)>>();
        foreach(var c in chains) foreach(bool st in new[]{true,false}) {
            int n=st?c.n0:c.n1; if(!fans.TryGetValue(n,out var fan))fans[n]=fan=new List<(V2L,bool,Vector3)>();
            fan.Add((c,st,V2Dir(c.p,st)));
        }
        var outlines=chains.ToDictionary(c=>c,c=>V2Outline(V2BandVerts(c.p,c.w*.5f,0)));
        var distance=chains.ToDictionary(c=>c,c=>V2Len(c.p));
        foreach(var pair in fans) {
            var fan=pair.Value; if(fan.Count<2||!V2In(nodes[pair.Key]))continue;
            fan.Sort((a,b)=>Mathf.Atan2(a.u.z,a.u.x).CompareTo(Mathf.Atan2(b.u.z,b.u.x)));
            var paths=fan.Select(a=>V2Outward(a.c,a.st)).ToList();
            var bands=fan.Select((a,i)=>V2BandVerts(paths[i],a.c.w*.5f,0)).ToList();
            var trims=Enumerable.Repeat(2f,fan.Count).ToArray();
            for(int i=0;i<fan.Count;i++) {
                int j=(i+1)%fan.Count; float sa=0; bool found=false;
                for(int a=0;a+3<bands[i].Count&&!found;a+=2) {
                    float sb=0;
                    for(int b=1;b+2<bands[j].Count;b+=2) {
                        if(V2SegX(bands[i][a],bands[i][a+2],bands[j][b],bands[j][b+2],out float t,out float u)) {
                            var da=Fl(paths[i][a/2+1]-paths[i][a/2]); var db=Fl(paths[j][b/2+1]-paths[j][b/2]);
                            float theta=Vector3.Angle(da,db)*Mathf.Deg2Rad;
                            float tangent=Mathf.Clamp(V2Rc/Mathf.Max(.001f,Mathf.Tan(theta*.5f)),1f,8f);
                            trims[i]=Mathf.Max(trims[i],sa+da.magnitude*t+tangent);
                            trims[j]=Mathf.Max(trims[j],sb+db.magnitude*u+tangent); found=true; break;
                        }
                        sb+=Fl(paths[j][b/2+1]-paths[j][b/2]).magnitude;
                    }
                    sa+=Fl(paths[i][a/2+1]-paths[i][a/2]).magnitude;
                }
            }
            for(int i=0;i<fan.Count;i++) {
                float limit=distance[fan[i].c]-.5f; trims[i]=Mathf.Min(trims[i],limit);
                for(;trims[i]<limit;trims[i]+=.5f) {
                    var q=V2At(paths[i],trims[i]); var u=Fl(V2At(paths[i],trims[i]+.1f)-V2At(paths[i],Mathf.Max(0,trims[i]-.1f))).normalized;
                    var left=new Vector3(-u.z,0,u.x)*fan[i].c.w*.5f; bool blocked=false;
                    for(int j=0;j<fan.Count&&!blocked;j++) if(j!=i&&fan[i].c!=fan[j].c)
                        for(int s=0;s<=4;s++) if(V2PolyE(outlines[fan[j].c],q+left*(s*.5f-1f),out _) < -.02f) {blocked=true;break;}
                    if(!blocked)break;
                }
                if(fan[i].st)fan[i].c.d0=trims[i];else fan[i].c.d1=trims[i];
            }
        }
        var parent=nodes.Select((_,i)=>i).ToArray();
        int Root(int n){while(parent[n]!=n)n=parent[n]=parent[parent[n]];return n;}
        foreach(var c in chains) if(c.d0>0&&c.d1>0&&c.d0+c.d1>=distance[c]-1f) parent[Root(c.n1)]=Root(c.n0);
        // Nearby junction footprints can overlap even without a short connecting link.
        // Solve these as one local plane instead of stamping two incompatible heights into the same terrain cells.
        var activeNodes=fans.Keys.Where(n=>fans[n].Count>=2&&V2In(nodes[n])).ToList(); int nearbyMerged=0;
        for(int a=0;a<activeNodes.Count;a++)for(int b=a+1;b<activeNodes.Count;b++) {
            int na=activeNodes[a],nb=activeNodes[b];
            float reach=(fans[na].Max(x=>x.c.w)+fans[nb].Max(x=>x.c.w))*.5f+1.5f;
            if(Root(na)!=Root(nb)&&Fl(nodes[na]-nodes[nb]).magnitude<reach&&Mathf.Abs(heights[na]-heights[nb])<3f) {parent[Root(nb)]=Root(na);nearbyMerged++;}
        }
        var groups=new Dictionary<int,V2J>();
        foreach(var pair in fans) if(pair.Value.Count>=2&&V2In(nodes[pair.Key])) {
            int r=Root(pair.Key); if(!groups.TryGetValue(r,out var j))groups[r]=j=new V2J{n=r}; j.nodes.Add(pair.Key);
        }
        foreach(var j in groups.Values) {
            float y=j.nodes.Average(n=>heights[n]); foreach(int n in j.nodes)heights[n]=y;
            foreach(var c in chains) {
                bool a=j.nodes.Contains(c.n0),b=j.nodes.Contains(c.n1);
                if(a&&b)j.internalLinks.Add(c);
                else {if(a)j.inc.Add((c,true,V2Dir(c.p,true)));if(b)j.inc.Add((c,false,V2Dir(c.p,false)));}
            }
        }
        var swallowed=groups.Values.SelectMany(j=>j.internalLinks).Distinct().ToList();
        foreach(var c in swallowed) chains.Remove(c);
        log.Add($"입구 겹침 제거: 갈림목 {groups.Count}, 짧은 연결로 흡수 {swallowed.Count}, 인접 갈림목 병합 {nearbyMerged}, 최대 자름 {chains.Max(c=>Mathf.Max(c.d0,c.d1)):F1}m");
        return groups.Values.ToList();
    }
    // only: 그 길들 몫(토막·흡수 연결로·둘 다 only 인 이웃 사이 모서리)만 — 1501 갈림목 두 재질 나누기용
    static Clipper2Lib.PathsD V2JunctionSurface(V2J junction,Dictionary<V2L,List<Vector3>> original,Dictionary<V2L,List<Vector3>> bands,List<Vector3> nodes,bool subtractRoads=true,System.Func<V2L,bool> only=null,float y=float.NaN)
    {
        var pieces=new Clipper2Lib.PathsD(); var cuts=new Clipper2Lib.PathsD();
        foreach(var c in junction.internalLinks)if(only==null||only(c))pieces.Add(V2Path(V2Outline(V2BandVerts(original[c],c.w*.5f,0))));
        foreach(var a in junction.inc) cuts.Add(V2Path(V2Outline(bands[a.c])));
        foreach(int node in junction.nodes) {
            var fan=new List<(Vector3 A,Vector3 B,Vector3 u,List<Vector3> left,List<Vector3> right,bool k)>();
            foreach(var c in original.Keys) foreach(bool st in new[]{true,false}) {
                if((st?c.n0:c.n1)!=node)continue;
                var path=original[c].ToList(); if(!st)path.Reverse();
                bool internalLink=junction.internalLinks.Contains(c); if(!internalLink&&!junction.inc.Any(a=>a.c==c&&a.st==st))continue;
                float trim=internalLink?Mathf.Min(3f,V2Len(path)*.5f):(st?c.d0:c.d1);
                var full=V2BandVerts(path,c.w*.5f,0); var left=new List<Vector3>();var right=new List<Vector3>();float run=0;
                for(int i=0;i<path.Count;i++) {if(i>0)run+=Fl(path[i]-path[i-1]).magnitude;if(run>=trim-.01f)break;left.Add(full[i*2]);right.Add(full[i*2+1]);}
                Vector3 A,B,u;
                if(internalLink) {
                    var mid=V2At(path,trim);u=Fl(V2At(path,trim+.1f)-V2At(path,trim-.1f)).normalized;
                    var normal=new Vector3(-u.z,0,u.x)*c.w*.5f;A=mid+normal;B=mid-normal;
                } else {
                    var band=bands[c]; int m=band.Count; A=st?band[0]:band[m-1];B=st?band[1]:band[m-2];
                    u=st?Fl(band[2]+band[3]-band[0]-band[1]).normalized:Fl(band[m-4]+band[m-3]-band[m-2]-band[m-1]).normalized;
                }
                bool k=only==null||only(c);
                left.Add(A);right.Add(B);var stub=new List<Vector3>(right);stub.Reverse();stub.AddRange(left);if(k)pieces.Add(V2Path(stub));
                fan.Add((A,B,u,left,right,k));
            }
            fan=fan.OrderBy(a=>Mathf.Atan2((a.A+a.B).z*.5f-nodes[node].z,(a.A+a.B).x*.5f-nodes[node].x)).ToList();
            for(int i=0;i<fan.Count;i++) {
                var a=fan[i];var b=fan[(i+1)%fan.Count];if(!a.k||!b.k)continue;var poly=new List<Vector3>(a.left); var control=(a.A+b.B)*.5f;
                if(V2LineX(a.A,a.u,b.B,b.u,out float t,out float s)&&t<=0&&s<=0&&t>-30&&s>-30)control=a.A+a.u*t;
                for(int k=1;k<8;k++){float t0=k/8f;poly.Add((1-t0)*(1-t0)*a.A+2*(1-t0)*t0*control+t0*t0*b.B);}
                poly.AddRange(b.right.AsEnumerable().Reverse());pieces.Add(V2Path(poly));
            }
        }
        var union=Clipper2Lib.Clipper.Union(pieces,null,Clipper2Lib.FillRule.NonZero,4);
        // 1601: 팔 조각끼리 맞닿기만 하고 안 겹친 쐐기(폭 수 cm~0.5 m)가 구멍으로 남아 큰길 위 검은 가시로 보임(j_245 @250,−58, 4143·4100)
        //  → 닫기(+0.4 → −0.4)로 메운다. 입구 모서리는 아래 cuts(팔 띠) 빼기로 다시 정해지므로 안 움직인다
        if(union.Count>0)union=Clipper2Lib.Clipper.InflatePaths(Clipper2Lib.Clipper.InflatePaths(union,.4,Clipper2Lib.JoinType.Miter,Clipper2Lib.EndType.Polygon,2,4),-.4,Clipper2Lib.JoinType.Miter,Clipper2Lib.EndType.Polygon,2,4);
        //  0.8 m 넘는 작은 섬 구멍(6 m² 미만, j_245 @251,−57)은 통째로 메운다 — 둘러싼 길 안쪽이라 잔디가 보일 자리가 아님
        union.RemoveAll(p=>Clipper2Lib.Clipper.Area(p)<0&&Clipper2Lib.Clipper.Area(p)>-6);
        // 1501: 큰길에 바짝 붙어 나란히 달리는 남의 길(ridge_up_c @257,−50)을 갈림목 모서리가 덮어 겹침 14쌍 → 같은 높이(±2.5 m)로 지나는 남의 띠도 뺀다
        if(subtractRoads&&!float.IsNaN(y)&&union.Count>0) {
            var r=Clipper2Lib.Clipper.GetBounds(union);
            // 1601: 남의 띠를 딱 맞게 빼면 두 길 높이차(0.6 m)를 지형이 0.5 m 칸 하나로 못 넘어 갈림목 가장자리를 덮음(j_245 @264,−50, ridge_up_c_1)
            //  → 높이가 0.3 m 넘게 다른 남의 띠는 0.4 m 여유를 두고 뺀다(팔 입구는 그대로)
            var other=new Clipper2Lib.PathsD();
            foreach(var kv in bands) {
                if(junction.internalLinks.Contains(kv.Key)||junction.inc.Any(a=>a.c==kv.Key))continue;
                var near=kv.Value.Where(q=>q.x>=r.left&&q.x<=r.right&&q.z>=r.top&&q.z<=r.bottom&&Mathf.Abs(q.y-y)<2.5f).ToList();
                if(near.Count==0)continue;
                if(near.Any(q=>Mathf.Abs(q.y-y)>.3f))other.Add(V2Path(V2Outline(kv.Value)));else cuts.Add(V2Path(V2Outline(kv.Value)));
            }
            if(other.Count>0)cuts.AddRange(Clipper2Lib.Clipper.InflatePaths(Clipper2Lib.Clipper.Union(other,null,Clipper2Lib.FillRule.NonZero,4),.4,Clipper2Lib.JoinType.Miter,Clipper2Lib.EndType.Polygon,2,4));
        }
        return subtractRoads?Clipper2Lib.Clipper.Difference(union,cuts,Clipper2Lib.FillRule.NonZero,4):union;
    }
    static float V2ProjectDistance(List<Vector3> p,Vector3 q)
    {
        float best=1e9f,result=0,run=0;
        for(int i=1;i<p.Count;i++) {
            var ab=Fl(p[i]-p[i-1]);float length=ab.magnitude;
            float t=Mathf.Clamp01(Vector3.Dot(Fl(q-p[i-1]),ab)/Mathf.Max(1e-8f,ab.sqrMagnitude));
            float d=Fl(q-Vector3.Lerp(p[i-1],p[i],t)).sqrMagnitude;
            if(d<best){best=d;result=run+t*length;}run+=length;
        }
        return result;
    }
    static void V2ClearJunctionSurfaces(List<V2L> chains,List<V2J> junctions,Dictionary<V2L,List<Vector3>> original,List<Vector3> nodes,Dictionary<int,float> heights,List<string> log)
    {
        int moves=0,merged=0;
        for(int pass=0;pass<40;pass++) {  // 과제 1501: 마을길 굽이가 노선을 따라가며 12 회로 모자람(자름은 늘기만 해 결국 수렴)
            var bands=new Dictionary<V2L,List<Vector3>>();
            foreach(var c in chains) {
                var copy=new V2L{p=original[c].ToList(),fix=c.fix.ToList(),d0=c.d0,d1=c.d1};V2Trim(copy);
                bands[c]=V2BandVerts(copy.p,c.w*.5f,0);
            }
            bool changed=false;
            foreach(var junction in junctions) {
                var surface=V2JunctionSurface(junction,original,bands,nodes,false);
                foreach(var arm in junction.inc) {
                    var c=arm.c;var road=new Clipper2Lib.PathsD{V2Path(V2Outline(bands[c]))};
                    var overlap=Clipper2Lib.Clipper.Intersect(surface,road,Clipper2Lib.FillRule.NonZero,4);
                    if(System.Math.Abs(Clipper2Lib.Clipper.Area(overlap))<.005)continue;
                    float length=V2Len(original[c]),old=arm.st?c.d0:c.d1,required=old;
                    foreach(var polygon in overlap)foreach(var p in polygon) {
                        float along=V2ProjectDistance(original[c],new Vector3((float)p.x,0,(float)p.y));
                        required=Mathf.Max(required,(arm.st?along:length-along)+.25f);
                    }
                    required=Mathf.Min(length,required);
                    if(required>old+.05f){if(arm.st)c.d0=required;else c.d1=required;changed=true;moves++;}
                }
            }
            // Clearance may consume the last piece of a link: merge its planes instead of clamping trims back into an overlap.
            foreach(var c in chains.ToList())if(c.d0+c.d1>=V2Len(original[c])-.6f) {
                var a=junctions.FirstOrDefault(j=>j.nodes.Contains(c.n0));var b=junctions.FirstOrDefault(j=>j.nodes.Contains(c.n1));
                if(a==null&&b==null)continue;if(a==null){a=b;b=null;}
                if(b!=null&&b!=a){a.nodes.AddRange(b.nodes);a.internalLinks.AddRange(b.internalLinks);junctions.Remove(b);merged++;}
                if(!a.nodes.Contains(c.n0))a.nodes.Add(c.n0);if(!a.nodes.Contains(c.n1))a.nodes.Add(c.n1);
                a.internalLinks.Add(c);chains.Remove(c);changed=true;
            }
            foreach(var junction in junctions) {
                junction.inc.Clear();float y=junction.nodes.Average(n=>heights[n]);foreach(int n in junction.nodes)heights[n]=y;
                foreach(var c in chains){if(junction.nodes.Contains(c.n0))junction.inc.Add((c,true,V2Dir(c.p,true)));if(junction.nodes.Contains(c.n1))junction.inc.Add((c,false,V2Dir(c.p,false)));}
            }
            if(!changed)break;
            if(pass==39)throw new System.InvalidOperationException("Junction mouth clearance did not converge");
        }
        log.Add($"합집합 입구 재검사: 추가 절단 {moves}, 추가 병합 {merged}");
    }
    static void V2MakeUnionJunction(Transform parent,string name,Clipper2Lib.PathsD surface,float y,V2L main,List<Mesh> meshes,List<(int obj,Vector3 a,Vector3 b,Vector3 c)> topTris,int obj)
    {
        if(System.Math.Abs(Clipper2Lib.Clipper.Area(surface))<.01)return;
        var result=Clipper2Lib.Clipper.Triangulate(surface,4,out var triangles);
        if(result!=Clipper2Lib.TriangulateResult.success)throw new System.InvalidOperationException(name+" triangulation "+result);
        var v=new List<Vector3>();var t=new List<int>();var map=new Dictionary<(int,int),int>();
        int Add(Clipper2Lib.PointD p){var key=(Mathf.RoundToInt((float)p.x*10000),Mathf.RoundToInt((float)p.y*10000));if(!map.TryGetValue(key,out int k)){map[key]=k=v.Count;v.Add(new Vector3((float)p.x,y,(float)p.y));}return k;}
        foreach(var tri in triangles){int a=Add(tri[0]),b=Add(tri[1]),c=Add(tri[2]);if(Vector3.Cross(v[b]-v[a],v[c]-v[a]).y<0)(b,c)=(c,b);if(Vector3.Cross(v[b]-v[a],v[c]-v[a]).y<=1e-7f)continue;t.AddRange(new[]{a,b,c});topTris.Add((obj,v[a],v[b],v[c]));}
        var go=V2Go(parent,name,v,t,v,t,main.mat,meshes);
        var rs=go.AddComponent<RoadSurface>();rs.roadId="v2_"+name;rs.kind=main.kind;rs.roughness=main.rough;rs.width=main.w;
        rs.centerline=new[]{v[0],v[0]+Vector3.forward*.1f};rs.closed=false;
    }

    static void V2FitParallelWidths(List<V2L> chains,List<Vector3> nodes,List<string> log)
    {
        var proposed=chains.ToDictionary(c=>c,c=>c.w);
        for(int i=0;i<chains.Count;i++)for(int j=i+1;j<chains.Count;j++) {
            var a=chains[i];var b=chains[j];if(a.kind!=b.kind||a.kind=="main"||!V2BBX(V2BB(a.p,a.w),V2BB(b.p,b.w)))continue;
            var common=new[]{a.n0,a.n1}.Intersect(new[]{b.n0,b.n1}).ToList();
            float closest=1e9f;int parallel=0;
            for(int k=1;k+1<a.p.Count;k++) {
                var q=a.p[k];if(common.Any(n=>Fl(q-nodes[n]).magnitude<25f))continue;
                float d=V2Dist(b.p,q,out var near);if(d>=(a.w+b.w)*.5f||d<4.75f||Mathf.Abs(q.y-near.y)>3f)continue;
                var dir=Fl(a.p[k+1]-a.p[k-1]).normalized;
                V2Dist(b.p,q+dir,out var n1);V2Dist(b.p,q-dir,out var n0);
                if(Mathf.Abs(Vector3.Dot(Fl(n1-n0).normalized,dir))<.9f)continue;
                closest=Mathf.Min(closest,d);parallel++;
            }
            if(parallel<8)continue;
            float factor=(closest-.25f)/((a.w+b.w)*.5f);
            if(a.w*factor<4.5f||b.w*factor<4.5f)continue;
            proposed[a]=Mathf.Min(proposed[a],a.w*factor);proposed[b]=Mathf.Min(proposed[b],b.w*factor);
        }
        foreach(var c in chains)if(proposed[c]<c.w-.01f){log.Add($"평행 길 폭 여유: {c.id} {c.w:F2}→{proposed[c]:F2}m (차도 사이 0.25m)");c.w=proposed[c];}
    }
    static Mesh V2RefineTerrain(Mesh source,Transform transform,System.Func<Vector3,bool> near)
    {
        var v=source.vertices.ToList();var triangles=source.triangles.ToList();var uv=source.uv.ToList();var uv2=source.uv2.ToList();var colors=source.colors.ToList();
        bool hasUV=uv.Count==v.Count,hasUV2=uv2.Count==v.Count,hasColors=colors.Count==v.Count;
        var cache=new Dictionary<(int,int),int>();
        int Mid(int a,int b) {
            var key=a<b?(a,b):(b,a);if(cache.TryGetValue(key,out int k))return k;cache[key]=k=v.Count;
            v.Add((v[a]+v[b])*.5f);if(hasUV)uv.Add((uv[a]+uv[b])*.5f);if(hasUV2)uv2.Add((uv2[a]+uv2[b])*.5f);if(hasColors)colors.Add(Color.Lerp(colors[a],colors[b],.5f));return k;
        }
        for(int pass=0;pass<2;pass++) {
            var refined=new List<int>();
            for(int i=0;i<triangles.Count;i+=3) {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                if(!near(transform.TransformPoint((v[a]+v[b]+v[c])/3f))){refined.AddRange(new[]{a,b,c});continue;}
                int ab=Mid(a,b),bc=Mid(b,c),ca=Mid(c,a);
                refined.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});
            }
            triangles=refined;
        }
        var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);
        if(hasUV)mesh.SetUVs(0,uv);if(hasUV2)mesh.SetUVs(1,uv2);if(hasColors)mesh.SetColors(colors);return mesh;
    }
    // Clip the walking-road footprint against the actual terrain triangles.
    // Both surfaces then use exactly the same planes; no per-row lowering or guessed bump is needed.
    sealed class V2Draper
    {
        readonly Vector3[] terrain;
        readonly int[] triangles;
        readonly Dictionary<(int, int), List<int>> cells = new Dictionary<(int, int), List<int>>();
        const float Cell = 1f;
        public V2Draper(Vector3[] vertices, int[] indices)
        {
            terrain = vertices; triangles = indices;
            for (int i = 0; i < indices.Length; i += 3) {
                var a = vertices[indices[i]]; var b = vertices[indices[i+1]]; var c = vertices[indices[i+2]];
                for (int x = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x,c.x))/Cell); x <= Mathf.FloorToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))/Cell); x++)
                for (int z = Mathf.FloorToInt(Mathf.Min(a.z, Mathf.Min(b.z,c.z))/Cell); z <= Mathf.FloorToInt(Mathf.Max(a.z,Mathf.Max(b.z,c.z))/Cell); z++) {
                    if (!cells.TryGetValue((x,z),out var list)) cells[(x,z)] = list = new List<int>(); list.Add(i);
                }
            }
        }
        static double Cross(Vector3 a, Vector3 b, Vector3 p) => ((double)b.x-a.x)*(p.z-a.z)-((double)b.z-a.z)*(p.x-a.x);
        static List<Vector3> Clip(List<Vector3> input, Vector3 a, Vector3 b, double sign)
        {
            var output = new List<Vector3>(); if (input.Count == 0) return output;
            var prev=input[input.Count-1]; double pd=Cross(a,b,prev)*sign;
            foreach(var cur in input) {
                double cd=Cross(a,b,cur)*sign;
                if ((pd >= -1e-8) != (cd >= -1e-8)) output.Add(Vector3.LerpUnclamped(prev,cur,(float)(pd/(pd-cd))));
                if (cd >= -1e-8) output.Add(cur);
                prev=cur; pd=cd;
            }
            return output;
        }
        public GameObject Make(Transform parent, string name, List<Vector3> band, Material material, List<Mesh> meshes)
        {
            var v=new List<Vector3>(); var t=new List<int>(); var index=new Dictionary<(int,int),int>();
            int Vertex(Vector3 q) {
                var key=(Mathf.RoundToInt(q.x*10000),Mathf.RoundToInt(q.z*10000));
                if (!index.TryGetValue(key,out int k)) { index[key]=k=v.Count; v.Add(q); } return k;
            }
            void Triangle(Vector3 a, Vector3 b, Vector3 c) {
                var candidates=new HashSet<int>();
                for(int x=Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))/Cell);x<=Mathf.FloorToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))/Cell);x++)
                for(int z=Mathf.FloorToInt(Mathf.Min(a.z,Mathf.Min(b.z,c.z))/Cell);z<=Mathf.FloorToInt(Mathf.Max(a.z,Mathf.Max(b.z,c.z))/Cell);z++)
                    if(cells.TryGetValue((x,z),out var list)) foreach(int k in list) candidates.Add(k);
                foreach(int k in candidates) {
                    var A=terrain[triangles[k]]; var B=terrain[triangles[k+1]]; var C=terrain[triangles[k+2]];
                    double det=Cross(A,B,C); if(System.Math.Abs(det)<1e-8) continue;
                    double sign=det>0 ? 1 : -1;
                    var poly=Clip(Clip(Clip(new List<Vector3>{a,b,c},A,B,sign),B,C,sign),C,A,sign);
                    if(poly.Count<3) continue;
                    for(int j=0;j<poly.Count;j++) {
                        var q=poly[j]; double u=Cross(A,q,C)/det, s=Cross(A,B,q)/det;
                        q.y=(float)(A.y+u*(B.y-A.y)+s*(C.y-A.y))+V2PathLift; poly[j]=q;
                    }
                    int first=Vertex(poly[0]);
                    for(int j=1;j+1<poly.Count;j++) {
                        float area=Vector3.Cross(poly[j]-poly[0],poly[j+1]-poly[0]).y;
                        if(Mathf.Abs(area)<1e-7f) continue;
                        int b0=Vertex(poly[j]),c0=Vertex(poly[j+1]);
                        if(first==b0||b0==c0||c0==first) continue;
                        t.Add(first); t.Add(area>0?b0:c0); t.Add(area>0?c0:b0);
                    }
                }
            }
            for(int i=0;i+3<band.Count;i+=2) { Triangle(band[i],band[i+2],band[i+1]); Triangle(band[i+1],band[i+2],band[i+3]); }
            return V2Go(parent,name,v,t,null,null,material,meshes);
        }
    }

}
