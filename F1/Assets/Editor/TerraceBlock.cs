using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// D-209 땅 시제품: 한 블록(반경 R)을 "사람이 깎은 계단 땅"으로.
// 지형 사본(Mesh_TownV6_Terrain_terrace1)을 만들어 꽂고, 축대·흙길은 TownV6_Gray/TerraceBlock 아래 새 메시로 둔다.
// 원본 v2 지형·길 메시·건물은 건드리지 않는다. 높이는 0.25 m 격자 위 함수 F(x,z) 하나로 매긴다:
//   터(건물 바닥·마당·밭·흙길 경사로) 안 = 평평 / 길 띠+여유 = 지금 값 그대로 / 나머지 = 원지형(0433)을 터·길 가장자리에서 1:2 원뿔로 묶음.
//   경계 높이차가 0.6 m 이상이면 원지형을 그대로 두고 축대가 차이를 받는다(0.45~0.6 은 이어지게 경사로 섞음).
public static class TerraceBlock
{
    const string Folder="Assets/Scenes/Map_TownV6_Mats/";
    const string SrcMesh=Folder+"Mesh_TownV6_Terrain_v2.asset",NatMesh=Folder+"Mesh_TownV6_Terrain_0433.asset";
    const string DstMesh=Folder+"Mesh_TownV6_Terrain_terrace1.asset",WallAsset=Folder+"Mesh_TerraceBlock_walls.asset",PathAsset=Folder+"Mesh_TerraceBlock_path.asset";
    const string StoneMat=Folder+"TV6_Stone.mat",RetainMat=Folder+"TV6_Retain.mat",CapMat=Folder+"TV6_RetainCap.mat",TerrainMat=Folder+"TV6_Terrain.mat",PathMat=Folder+"TV6_TerracePath.mat";
    // 2차: 돌 텍스처(H1 사진, house_home 기단과 같은 축척 2.5 m) · 흙길 가장자리 번짐 — docs/qa/tmp/terrace2/make_tex.py 가 만든다
    const string StoneTex=Folder+"TV6_RetainStone.png",PathEdgeTex=Folder+"TV6_TerracePathEdge.png";
    const string RootName="TerraceBlock";
    const float Cell=.25f,Blend=8f,CarMargin=1f,PathMargin=.8f,WallMin=.3f,Thick=.4f,Batter=.08f,CapW=.56f,CapH=.12f,Section=2.4f,MaxEdge=.38f;
    // 2차: 2.5 m 넘는 단차는 두 단(사이 1.6 m 화단 턱) · 마디 끝 겹침 0.15 m · 돌 한 장 2.5×1.88 m · 흙길 반폭 1.1 m(가운데 흙, 바깥으로 풀 번짐)
    const float TwoTier=2.5f,LedgeW=1.6f,LedgeRoadGap=.6f,SecOverlap=.15f,StoneU=2.5f,StoneV=1.88f,PathHalf=1.1f,SmoothRoadGap=2f;
    public static readonly Vector3 HouseCenter=new Vector3(185,0,-61);

    [MenuItem("PitCrew/Map/TownV6 — 계단 땅 시제품 (집 블록, D-209)")] static void BuildMenu()=>Debug.Log(Build(HouseCenter,60));
    [MenuItem("PitCrew/Map/TownV6 — 계단 땅 되돌리기")] static void RevertMenu()=>Debug.Log(Revert());

    static Transform Top()=>GameObject.Find("TownV6_Gray").transform;
    static Transform TerrainT()=>Top().Find("TownV6_Terrain");

    public static string Revert()
    {
        if(Application.isPlaying)throw new Exception("Play 중엔 안 됨");
        var terr=TerrainT();var src=AssetDatabase.LoadAssetAtPath<Mesh>(SrcMesh);
        terr.GetComponent<MeshFilter>().sharedMesh=src;var mc=terr.GetComponent<MeshCollider>();if(mc)mc.sharedMesh=src;
        var root=Top().Find(RootName);bool removed=root!=null;if(root)UnityEngine.Object.DestroyImmediate(root.gameObject);
        var scene=terr.gameObject.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "{\"revert\":{\"terrain\":\""+src.name+"\",\"rootRemoved\":"+(removed?"true":"false")+"}}";
    }

    // ---------- 격자 ----------
    class Grid
    {
        public float x0,z0;public int n;
        public int Idx(int i,int j)=>j*n+i;
        public Vector2 P(int i,int j)=>new Vector2(x0+i*Cell,z0+j*Cell);
        public Vector2 P(int idx)=>P(idx%n,idx/n);
        public float Sample(float[] a,float x,float z)
        {
            float fx=(x-x0)/Cell,fz=(z-z0)/Cell;int i=Mathf.Clamp((int)Mathf.Floor(fx),0,n-2),j=Mathf.Clamp((int)Mathf.Floor(fz),0,n-2);float tx=Mathf.Clamp01(fx-i),tz=Mathf.Clamp01(fz-j);
            return Mathf.Lerp(Mathf.Lerp(a[Idx(i,j)],a[Idx(i+1,j)],tx),Mathf.Lerp(a[Idx(i,j+1)],a[Idx(i+1,j+1)],tx),tz);
        }
        public int Near(float x,float z)=>Idx(Mathf.Clamp(Mathf.RoundToInt((x-x0)/Cell),0,n-1),Mathf.Clamp(Mathf.RoundToInt((z-z0)/Cell),0,n-1));
    }
    class Owner{public string name;public float h;public bool road;public List<Vector2> poly;public Func<Vector2,float> hf;public int cells;public float H(Vector2 p)=>hf!=null?hf(p):h;}

    static float[] SampleMesh(Mesh m,Grid g)
    {
        var go=new GameObject("_tb_col"){hideFlags=HideFlags.HideAndDontSave};
        try{
            var mc=go.AddComponent<MeshCollider>();mc.sharedMesh=m;var a=new float[g.n*g.n];
            for(int j=0;j<g.n;j++)for(int i=0;i<g.n;i++){var p=g.P(i,j);a[g.Idx(i,j)]=mc.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out var h,1000)?h.point.y:float.NaN;}
            for(int k=0;k<a.Length;k++)if(float.IsNaN(a[k]))throw new Exception("지형 레이 빗나감 "+g.P(k));
            return a;
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    // ---------- 2D 도형 ----------
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static float DistSeg(Vector2 p,Vector2 a,Vector2 b){var ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(1e-8f,ab.sqrMagnitude));return (a+ab*t-p).magnitude;}
    static bool InTri(Vector2 p,Vector2 a,Vector2 b,Vector2 c){float d1=Cross(b-a,p-a),d2=Cross(c-b,p-b),d3=Cross(a-c,p-c);bool neg=d1<0||d2<0||d3<0,pos=d1>0||d2>0||d3>0;return !(neg&&pos);}
    static bool InPoly(Vector2 p,List<Vector2> poly){bool r=false;for(int i=0,j=poly.Count-1;i<poly.Count;j=i++){var a=poly[i];var b=poly[j];if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)r=!r;}return r;}
    static List<Vector2> Rect(Vector3 origin,float yaw,float x0,float x1,float z0,float z1)
    {
        var q=Quaternion.Euler(0,yaw,0);var res=new List<Vector2>();
        foreach(var l in new[]{new Vector3(x0,0,z0),new Vector3(x1,0,z0),new Vector3(x1,0,z1),new Vector3(x0,0,z1)}){var w=origin+q*l;res.Add(new Vector2(w.x,w.z));}
        return res;
    }
    // 프리팹 건물: 바닥 근처(최저+0.5 m) 정점의 뿌리 방향 사각형
    static bool Footprint(Transform root,float pad,out List<Vector2> poly,out float minY,out float lx0,out float lx1,out float lz0,out float lz1)
    {
        var pts=new List<Vector3>();minY=float.MaxValue;poly=null;lx0=lz0=float.MaxValue;lx1=lz1=float.MinValue;
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>(false))
        {
            var mr=mf.GetComponent<MeshRenderer>();if(!mf.sharedMesh||!mr||!mr.enabled)continue;Vector3[] vs;try{vs=mf.sharedMesh.vertices;}catch{continue;}
            foreach(var v in vs){var w=mf.transform.TransformPoint(v);pts.Add(w);if(w.y<minY)minY=w.y;}
        }
        if(pts.Count==0)return false;
        var inv=Quaternion.Inverse(Quaternion.Euler(0,root.eulerAngles.y,0));
        foreach(var w in pts){if(w.y>minY+.5f)continue;var l=inv*(w-root.position);lx0=Mathf.Min(lx0,l.x);lx1=Mathf.Max(lx1,l.x);lz0=Mathf.Min(lz0,l.z);lz1=Mathf.Max(lz1,l.z);}
        poly=Rect(root.position,root.eulerAngles.y,lx0-pad,lx1+pad,lz0-pad,lz1+pad);return true;
    }

    // ---------- 본체 ----------
    public static string Build(Vector3 center,float radius)
    {
        if(Application.isPlaying)throw new Exception("Play 중엔 안 됨");
        var sw=System.Diagnostics.Stopwatch.StartNew();var js=new StringBuilder();var c2=new Vector2(center.x,center.z);
        var top=Top();var terr=TerrainT();
        var old=top.Find(RootName);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var src=AssetDatabase.LoadAssetAtPath<Mesh>(SrcMesh);var nat=AssetDatabase.LoadAssetAtPath<Mesh>(NatMesh);
        if(!src||!nat)throw new Exception("지형 원본 에셋 없음");
        float G=radius+Blend+4;var g=new Grid{x0=Mathf.Round(center.x-G),z0=Mathf.Round(center.z-G),n=Mathf.RoundToInt(2*G/Cell)+1};int N=g.n*g.n;
        var Hc=SampleMesh(src,g);var Hn=SampleMesh(nat,g);
        float Rad(Vector2 p)=>(p-c2).magnitude;
        bool Inside(List<Vector2> poly)=>poly.All(p=>Rad(p)<=radius-1f);

        // --- 터 목록 (낮은 우선순위부터) ---
        var owners=new List<Owner>();var skipped=new List<string>();
        // 집 + 마당(문 앞 = 뿌리 +z 쪽 5.5 m)
        var house=GameObject.Find("D198_Buildings/house_home");
        if(!house||!Footprint(house.transform,1.2f,out var hpoly,out float hMin,out float hx0,out float hx1,out float hz0,out float hz1))throw new Exception("house_home 못 찾음");
        float hHouse=hMin;var hyaw=house.transform.eulerAngles.y;var hpos=house.transform.position;
        var yardPoly=Rect(hpos,hyaw,hx0-1.2f,hx1+1.2f,hz0-1.2f,hz1+5.5f);
        // 밭(밭 목업 B 와 같은 외곽: 12×9 m 초타원 p=4, 중심 198,-57, 10°)
        var fq=Quaternion.Euler(0,10,0);var fc=new Vector3(198,0,-57);var fieldPoly=new List<Vector2>();
        for(int k=0;k<48;k++){float t=k*Mathf.PI*2/48,cs=Mathf.Cos(t),sn=Mathf.Sin(t);var l=new Vector3(6f*Mathf.Sign(cs)*Mathf.Pow(Mathf.Abs(cs),.5f),0,4.5f*Mathf.Sign(sn)*Mathf.Pow(Mathf.Abs(sn),.5f));var w=fc+fq*l;fieldPoly.Add(new Vector2(w.x,w.z));}
        float Median(List<Vector2> poly,float[] a){var v=new List<float>();for(int k=0;k<N;k++)if(InPoly(g.P(k),poly))v.Add(a[k]);v.Sort();return v.Count>0?v[v.Count/2]:float.NaN;}
        float hField=Median(fieldPoly,Hc);
        // 흙길(마당 동쪽 → 밭 남서): 경사로, 집 높이 → 밭 높이
        var qh=Quaternion.Euler(0,hyaw,0);var ps3=hpos+qh*new Vector3(hx0-.6f,0,hz1+3f);var pe3=fc+fq*new Vector3(-3f,0,-3.8f);
        Vector2 ps=new Vector2(ps3.x,ps3.z),pe=new Vector2(pe3.x,pe3.z);var pmid=(ps+pe)*.5f+new Vector2(.8f,-.8f);
        var pathLine=new List<Vector2>();for(int k=0;k<=16;k++){float t=k/16f;pathLine.Add((1-t)*(1-t)*ps+2*(1-t)*t*pmid+t*t*pe);}
        float PathT(Vector2 p){float best=1e9f,bt=0;for(int k=0;k<pathLine.Count-1;k++){var a=pathLine[k];var b=pathLine[k+1];var ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);float d=(a+ab*t-p).sqrMagnitude;if(d<best){best=d;bt=(k+t)/(pathLine.Count-1);}}return bt;}
        var pathPoly=new List<Vector2>();{var L=new List<Vector2>();var Rr=new List<Vector2>();for(int k=0;k<pathLine.Count;k++){var d=(pathLine[Mathf.Min(k+1,pathLine.Count-1)]-pathLine[Mathf.Max(k-1,0)]).normalized;var nn=new Vector2(-d.y,d.x)*.8f;L.Add(pathLine[k]+nn);Rr.Add(pathLine[k]-nn);}pathPoly.AddRange(L);Rr.Reverse();pathPoly.AddRange(Rr);}
        owners.Add(new Owner{name="path_house_field",poly=pathPoly,hf=p=>Mathf.Lerp(hHouse,hField,Mathf.SmoothStep(0,1,PathT(p)))});
        owners.Add(new Owner{name="field",poly=fieldPoly,h=hField});
        // OSM 건물(합친 벽 메시를 1 m 칸으로 묶어 덩어리별 최소 넓이 사각형)
        var wallsT=top.Find("Buildings/Walls_0");int osmIn=0;
        if(wallsT)
        {
            // 삼각형 연결 + 같은 자리(1 cm) 정점 용접으로 건물 한 채씩
            var wm=wallsT.GetComponent<MeshFilter>().sharedMesh;var wvs=wm.vertices;var wtr=wm.triangles;var par=new int[wvs.Length];for(int k=0;k<par.Length;k++)par[k]=k;
            int Find(int x){while(par[x]!=x){par[x]=par[par[x]];x=par[x];}return x;}void Union(int a,int b){a=Find(a);b=Find(b);if(a!=b)par[a]=b;}
            var weld=new Dictionary<(int,int,int),int>();var ww=new Vector3[wvs.Length];
            for(int k=0;k<wvs.Length;k++){var w=ww[k]=wallsT.TransformPoint(wvs[k]);var key=(Mathf.RoundToInt(w.x*100),Mathf.RoundToInt(w.y*100),Mathf.RoundToInt(w.z*100));if(weld.TryGetValue(key,out int f0))Union(k,f0);else weld[key]=k;}
            for(int t=0;t<wtr.Length;t+=3){Union(wtr[t],wtr[t+1]);Union(wtr[t+1],wtr[t+2]);}
            var groups=new Dictionary<int,List<Vector3>>();var near=new HashSet<int>();
            for(int k=0;k<wvs.Length;k++){int r0=Find(k);if(Rad(new Vector2(ww[k].x,ww[k].z))<=radius+6)near.Add(r0);if(!groups.TryGetValue(r0,out var l))groups[r0]=l=new List<Vector3>();l.Add(ww[k]);}
            foreach(var r0 in near)
            {
                var pts=groups[r0];
                if(pts.Count<8)continue;float minY=pts.Min(p=>p.y);float bestA=1e9f,bAng=0,bx0=0,bx1=0,bz0=0,bz1=0;var o=pts[0];
                for(int ang=0;ang<90;ang++){var iq=Quaternion.Euler(0,-ang,0);float a0=1e9f,a1=-1e9f,b0=1e9f,b1=-1e9f;foreach(var p in pts){var l=iq*(p-o);a0=Mathf.Min(a0,l.x);a1=Mathf.Max(a1,l.x);b0=Mathf.Min(b0,l.z);b1=Mathf.Max(b1,l.z);}float A=(a1-a0)*(b1-b0);if(A<bestA){bestA=A;bAng=ang;bx0=a0;bx1=a1;bz0=b0;bz1=b1;}}
                var poly=Rect(o,bAng,bx0-.5f,bx1+.5f,bz0-.5f,bz1+.5f);
                var oc=pts.Aggregate(Vector3.zero,(acc,p)=>acc+p)/pts.Count;
                if(bestA<6||bestA>600||!Inside(poly)){skipped.Add($"osm@({oc.x:F0},{oc.z:F0}) A={bestA:F0} in={Inside(poly)}");continue;}
                float minG=float.MaxValue;foreach(var p in poly)minG=Mathf.Min(minG,g.Sample(Hc,p.x,p.y));
                owners.Add(new Owner{name=$"osm_{osmIn++}@({oc.x:F0},{oc.z:F0})",poly=poly,h=Mathf.Max(minY,minG)});
            }
        }
        // 자산 자리 + D198 프리팹(집 제외) — 지금 바닥 높이 = 단 높이
        var others=new List<Transform>();
        var slots=GameObject.Find("TV6_Loop/AssetSlots");if(slots)foreach(Transform s in slots.transform)if(s.gameObject.activeInHierarchy)others.Add(s);
        var d198=GameObject.Find("D198_Buildings");if(d198)foreach(Transform s in d198.transform)if(s.gameObject.activeInHierarchy&&s.gameObject!=house)others.Add(s);
        foreach(var s in others)
        {
            if(!Footprint(s,1f,out var poly,out float minY,out _,out _,out _,out _))continue;
            if(!Inside(poly)){if(Rad(new Vector2(s.position.x,s.position.z))<radius+10)skipped.Add(s.name+" (반경 밖에 걸침)");continue;}
            owners.Add(new Owner{name=s.name,poly=poly,h=minY});
        }
        owners.Add(new Owner{name="house_home+yard",poly=yardPoly,h=hHouse});
        int ROAD=owners.Count;owners.Add(new Owner{name="roads(v2, 지금 값 유지)",road=true});

        // --- 라벨 ---
        var lab=new int[N];for(int k=0;k<N;k++)lab[k]=-1;
        for(int o=0;o<ROAD;o++)
        {
            var poly=owners[o].poly;float mnx=poly.Min(p=>p.x),mxx=poly.Max(p=>p.x),mnz=poly.Min(p=>p.y),mxz=poly.Max(p=>p.y);
            for(int j=Mathf.Max(0,Mathf.FloorToInt((mnz-g.z0)/Cell));j<=Mathf.Min(g.n-1,Mathf.CeilToInt((mxz-g.z0)/Cell));j++)
            for(int i=Mathf.Max(0,Mathf.FloorToInt((mnx-g.x0)/Cell));i<=Mathf.Min(g.n-1,Mathf.CeilToInt((mxx-g.x0)/Cell));i++)
                if(InPoly(g.P(i,j),poly))lab[g.Idx(i,j)]=o;
        }
        int roadMeshes=0;
        foreach(Transform ch in top.Find("RoadsV2"))
        {
            if(!ch.gameObject.activeInHierarchy)continue;var mf=ch.GetComponent<MeshFilter>();var mr=ch.GetComponent<MeshRenderer>();if(!mf||!mf.sharedMesh||!mr||!mr.enabled)continue;
            var b=mr.bounds;if(b.max.x<g.x0||b.min.x>g.x0+2*G||b.max.z<g.z0||b.min.z>g.z0+2*G)continue;
            float m=ch.name.StartsWith("ROAD_V2p")?PathMargin:CarMargin;roadMeshes++;
            var vs=mf.sharedMesh.vertices;var tr=mf.sharedMesh.triangles;var w=new Vector2[vs.Length];for(int k=0;k<vs.Length;k++){var p=ch.TransformPoint(vs[k]);w[k]=new Vector2(p.x,p.z);}
            for(int t=0;t<tr.Length;t+=3)
            {
                var a=w[tr[t]];var bb=w[tr[t+1]];var cc=w[tr[t+2]];
                float mnx=Mathf.Min(a.x,Mathf.Min(bb.x,cc.x))-m,mxx=Mathf.Max(a.x,Mathf.Max(bb.x,cc.x))+m,mnz=Mathf.Min(a.y,Mathf.Min(bb.y,cc.y))-m,mxz=Mathf.Max(a.y,Mathf.Max(bb.y,cc.y))+m;
                for(int j=Mathf.Max(0,Mathf.FloorToInt((mnz-g.z0)/Cell));j<=Mathf.Min(g.n-1,Mathf.CeilToInt((mxz-g.z0)/Cell));j++)
                for(int i=Mathf.Max(0,Mathf.FloorToInt((mnx-g.x0)/Cell));i<=Mathf.Min(g.n-1,Mathf.CeilToInt((mxx-g.x0)/Cell));i++)
                {
                    int idx=g.Idx(i,j);if(lab[idx]==ROAD)continue;var p=g.P(i,j);
                    if(InTri(p,a,bb,cc)||DistSeg(p,a,bb)<=m||DistSeg(p,bb,cc)<=m||DistSeg(p,cc,a)<=m)lab[idx]=ROAD;
                }
            }
        }
        for(int k=0;k<N;k++)if(lab[k]>=0)owners[lab[k]].cells++;

        // --- 터별 최근접 칸(8방향 2패스 거리 변환) ---
        int n=g.n;
        float D2(int s,int i,int j){int si=s%n,sj=s/n;return (si-i)*(si-i)+(sj-j)*(sj-j);}
        var feat=new int[owners.Count][];
        for(int o=0;o<owners.Count;o++)
        {
            var f=new int[N];for(int k=0;k<N;k++)f[k]=lab[k]==o?k:-1;
            void Try(int idx,int nb,int i,int j){int s=f[nb];if(s<0)return;if(f[idx]<0||D2(s,i,j)<D2(f[idx],i,j))f[idx]=s;}
            for(int j=0;j<n;j++)for(int i=0;i<n;i++){int idx=j*n+i;if(i>0)Try(idx,idx-1,i,j);if(j>0){Try(idx,idx-n,i,j);if(i>0)Try(idx,idx-n-1,i,j);if(i<n-1)Try(idx,idx-n+1,i,j);}}
            for(int j=n-1;j>=0;j--)for(int i=n-1;i>=0;i--){int idx=j*n+i;if(i<n-1)Try(idx,idx+1,i,j);if(j<n-1){Try(idx,idx+n,i,j);if(i<n-1)Try(idx,idx+n+1,i,j);if(i>0)Try(idx,idx+n-1,i,j);}}
            feat[o]=f;
        }

        // --- 높이 F ---
        // 2차: 1:2 원뿔을 "터마다 가장 가까운 칸 하나" 가 아니라 모든 경계 칸의 포락으로. 가까운 칸이 바뀌는 선마다 높이가 툭 끊겨
        //      축대 → 길 방향 세로 줄무늬가 생겼다(1차 f1_high·c2). 길(ROAD)은 따로 포락해 마지막에 건다(1차와 같은 우선순위).
        var Fcore=new float[N];var F=new float[N];
        void ComputeF()
        {
            var loO=new float[N];var upO=new float[N];var loR=new float[N];var upR=new float[N];
            for(int k=0;k<N;k++){loO[k]=loR[k]=float.NegativeInfinity;upO[k]=upR[k]=float.PositiveInfinity;}
            for(int j=0;j<n;j++)for(int i=0;i<n;i++)
            {
                int idx=j*n+i;int o=lab[idx];if(o<0)continue;
                bool edge=(i>0&&lab[idx-1]!=o)||(i<n-1&&lab[idx+1]!=o)||(j>0&&lab[idx-n]!=o)||(j<n-1&&lab[idx+n]!=o);if(!edge)continue;
                float e=o==ROAD?Hc[idx]:owners[o].H(g.P(idx));float delta=Hn[idx]-e,a=Mathf.Abs(delta);
                float W=Mathf.Clamp01((a-.45f)/.15f)*a;float cc=e+Mathf.Sign(delta)*W;
                if(o==ROAD){loR[idx]=cc+Cell*.25f;upR[idx]=cc-Cell*.25f;}else{loO[idx]=cc+Cell*.25f;upO[idx]=cc-Cell*.25f;}
            }
            Envelope(n,loO,upO);Envelope(n,loR,upR);
            for(int j=0;j<n;j++)for(int i=0;i<n;i++)
            {
                int idx=j*n+i;int l=lab[idx];float y;
                if(l==ROAD)y=Hc[idx];
                else if(l>=0)y=owners[l].H(g.P(idx));
                else{y=Mathf.Clamp(Hn[idx],loO[idx],upO[idx]);y=Mathf.Clamp(y,loR[idx],upR[idx]);}
                Fcore[idx]=y;
            }
            // 원뿔이 갈아타는 꺾임선(희미한 사선 띠) 누그러뜨리기: 터 밖 칸끼리만 3×3 평균 2번(터·길 칸은 섞지 않음 → 축대 밑 높이 그대로).
            // 길에서 2 m 안 칸은 빼둔다 — 섞으면 길 메시 밑 땅이 최대 2.7 cm 움직였다(검증 1차 0.7 cm).
            for(int pass=0;pass<2;pass++)
            {
                var tmp=(float[])Fcore.Clone();
                for(int j=1;j<n-1;j++)for(int i=1;i<n-1;i++)
                {
                    int idx=j*n+i;if(lab[idx]!=-1)continue;int qr=feat[ROAD][idx];if(qr>=0&&D2(qr,i,j)*Cell*Cell<SmoothRoadGap*SmoothRoadGap)continue;float sum=0;int c=0;
                    for(int dj=-1;dj<=1;dj++)for(int di=-1;di<=1;di++){int nb=idx+dj*n+di;if(lab[nb]!=-1)continue;sum+=tmp[nb];c++;}
                    Fcore[idx]=sum/c;
                }
            }
            for(int k=0;k<N;k++){float bt=Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius,radius+Blend,Rad(g.P(k))));F[k]=Mathf.Lerp(Fcore[k],Hc[k],bt);}
        }
        ComputeF();

        // --- 2단 축대: 터와 바깥 땅 차이가 2.5 m 넘는 곳은 바깥쪽 1.6 m 띠를 "화단 턱" 터로 만들어 중간 높이에 둔다 ---
        //     위 단 = 터 쪽, 아래 단 = 턱 바깥. 높이 U = 그 터 최대 단차의 절반(0.1 m 올림) → 두 단 다 2.5 m 이하. 총 높이는 그대로.
        //     턱은 길(차길+여유 1 m) 에서 0.6 m 더 떨어진 칸만 — 길가 축대가 차길 쪽으로 나오지 않게.
        int firstLedge=owners.Count;var ledgeInfo=new List<string>();
        for(int o=0;o<ROAD;o++)
        {
            foreach(int s in new[]{-1,1})  // -1: 바깥이 낮다(터가 위) · +1: 바깥이 높다(터를 깎아 들어감)
            {
                var cand=new List<int>();float jmax=0;
                for(int j=0;j<n;j++)for(int i=0;i<n;i++)
                {
                    int idx=j*n+i;if(lab[idx]!=-1)continue;int q=feat[o][idx];if(q<0||Mathf.Sqrt(D2(q,i,j))*Cell>LedgeW)continue;
                    if(Rad(g.P(idx))>radius-2)continue;int qr=feat[ROAD][idx];if(qr>=0&&Mathf.Sqrt(D2(qr,i,j))*Cell<LedgeRoadGap)continue;
                    float J=(Fcore[idx]-owners[o].H(g.P(q)))*s;if(J<=TwoTier)continue;cand.Add(idx);jmax=Mathf.Max(jmax,J);
                }
                if(cand.Count*Cell*Cell<3f)continue;
                float U=Mathf.Ceil(jmax*.5f/.1f)*.1f;var oo=owners[o];
                var led=new Owner{name="ledge_"+oo.name+(s<0?"_low":"_high"),hf=p=>oo.H(p)+s*U};int li=owners.Count;owners.Add(led);
                foreach(var idx in cand)lab[idx]=li;led.cells=cand.Count;
                ledgeInfo.Add($"{{\"name\":\"{led.name}\",\"cells\":{cand.Count},\"areaM2\":{cand.Count*Cell*Cell:F1},\"jMax\":{jmax:F2},\"upper\":"+U.ToString("F2")+"}");
            }
        }
        if(owners.Count>firstLedge)
        {
            // 턱 터의 최근접 칸 표(축대 방향 판정에 씀) + F 다시
            var featL=new int[owners.Count][];Array.Copy(feat,featL,feat.Length);feat=featL;
            for(int o=firstLedge;o<owners.Count;o++)
            {
                var f=new int[N];for(int k=0;k<N;k++)f[k]=lab[k]==o?k:-1;
                void Try(int idx,int nb,int i,int j){int s=f[nb];if(s<0)return;if(f[idx]<0||D2(s,i,j)<D2(f[idx],i,j))f[idx]=s;}
                for(int j=0;j<n;j++)for(int i=0;i<n;i++){int idx=j*n+i;if(i>0)Try(idx,idx-1,i,j);if(j>0){Try(idx,idx-n,i,j);if(i>0)Try(idx,idx-n-1,i,j);if(i<n-1)Try(idx,idx-n+1,i,j);}}
                for(int j=n-1;j>=0;j--)for(int i=n-1;i>=0;i--){int idx=j*n+i;if(i<n-1)Try(idx,idx+1,i,j);if(j<n-1){Try(idx,idx+n,i,j);if(i<n-1)Try(idx,idx+n+1,i,j);if(i>0)Try(idx,idx+n-1,i,j);}}
                feat[o]=f;
            }
            ComputeF();
        }

        // --- 축대: 터 경계(마칭 스퀘어) 를 따라 양쪽 높이차를 재서 WallMin(0.3 m) 이상·0.8 m 이상 이어지는 곳 ---
        var wv=new List<Vector3>();var wt=new List<int>();var ct=new List<int>();var wuv=new List<Vector2>();
        float wallLen=0,wallMax=0,wallSum=0,tallLen=0;int sections=0,stones=0;var perOwner=new Dictionary<string,float>();
        for(int o=0;o<owners.Count;o++)
        {
            foreach(var raw in Contours(g,lab,o))
            {
                if(raw.Count<3)continue;
                for(int it=0;it<6;it++)for(int k=1;k<raw.Count-1;k++)raw[k]=(raw[k-1]+raw[k]*2+raw[k+1])*.25f;
                var chain=Resample(Simplify(raw,.05f),.5f);
                int m=chain.Count;var J=new float[m];var Lo=new Vector2[m];var Fh=new float[m];var Fl=new float[m];var ok=new bool[m];
                for(int k=0;k<m;k++)
                {
                    var p=chain[k];if(Rad(p)>radius+Blend)continue;
                    var tg=(chain[Mathf.Min(k+1,m-1)]-chain[Mathf.Max(k-1,0)]).normalized;var nrm=new Vector2(tg.y,-tg.x);
                    if(lab[g.Near(p.x+nrm.x*.3f,p.y+nrm.y*.3f)]==o)nrm=-nrm;
                    int other=lab[g.Near(p.x+nrm.x*.4f,p.y+nrm.y*.4f)];if(other>=0&&other<o)continue;
                    float fin=g.Sample(F,p.x-nrm.x*.35f,p.y-nrm.y*.35f),fout=g.Sample(F,p.x+nrm.x*.35f,p.y+nrm.y*.35f);
                    J[k]=fin-fout;if(Mathf.Abs(J[k])<WallMin)continue;ok[k]=true;Lo[k]=J[k]>0?nrm:-nrm;Fh[k]=Mathf.Max(fin,fout);Fl[k]=Mathf.Min(fin,fout);
                }
                // 0.8 m 보다 짧은 토막은 버린다(길가 말뚝처럼 보임)
                for(int k=0;k<m;)
                {
                    if(!ok[k]){k++;continue;}int e=k;float rl=0;while(e+1<m&&ok[e+1]&&Mathf.Sign(J[e+1])==Mathf.Sign(J[k])){rl+=(chain[e+1]-chain[e]).magnitude;e++;}
                    if(rl<.8f)for(int q=k;q<=e;q++)ok[q]=false;k=e+1;
                }
                var S=new float[m];for(int k=1;k<m;k++)S[k]=S[k-1]+(chain[k]-chain[k-1]).magnitude;
                void Emit(int a,int b)
                {
                    if(b<=a)return;float l2=WallSection(wv,wt,ct,wuv,chain,S,Lo,Fh,Fl,a,b,ref stones,out float hgt);wallLen+=l2;wallMax=Mathf.Max(wallMax,hgt);if(hgt>TwoTier+.15f)tallLen+=l2;wallSum+=hgt*l2;sections++;
                    perOwner.TryGetValue(owners[o].name,out float pl);perOwner[owners[o].name]=pl+l2;
                }
                int s0=-1;float len=0,mn=0,mx=0;
                for(int k=0;k<m;k++)
                {
                    if(!ok[k]){if(s0>=0)Emit(s0,k-1);s0=-1;continue;}
                    if(s0<0||Mathf.Sign(J[k])!=Mathf.Sign(J[s0])){if(s0>=0)Emit(s0,k-1);s0=k;len=0;mn=mx=Fh[k];continue;}
                    len+=(chain[k]-chain[k-1]).magnitude;mn=Mathf.Min(mn,Fh[k]);mx=Mathf.Max(mx,Fh[k]);
                    if(len>=Section||mx-mn>.3f){Emit(s0,k);s0=k;len=0;mn=mx=Fh[k];}
                }
                if(s0>=0)Emit(s0,m-1);
            }
        }

        // --- 지형 사본: 날카로운 칸 근처만 0.38 m 까지 쪼갬(빨강-초록, T 이음 없음) 후 높이 다시 매김 ---
        var sharp=new bool[N];
        for(int j=0;j<n;j++)for(int i=0;i<n;i++){int idx=j*n+i;if(i<n-1&&Mathf.Abs(F[idx]-F[idx+1])>.2f){sharp[idx]=sharp[idx+1]=true;}if(j<n-1&&Mathf.Abs(F[idx]-F[idx+n])>.2f){sharp[idx]=sharp[idx+n]=true;}}
        {var s2=(bool[])sharp.Clone();for(int j=1;j<n-1;j++)for(int i=1;i<n-1;i++){int idx=j*n+i;if(s2[idx-1]||s2[idx+1]||s2[idx-n]||s2[idx+n])sharp[idx]=true;}}
        var V=new List<Vector3>(src.vertices);var tris=new List<int>(src.triangles);var uv=new List<Vector2>(src.uv);var uv2=new List<Vector2>(src.uv2);var col=new List<Color>(src.colors);var nor=new List<Vector3>(src.normals);
        int origCount=V.Count;bool hUV=uv.Count==origCount,hUV2=uv2.Count==origCount,hCol=col.Count==origCount,hNor=nor.Count==origCount;
        var canon=new int[origCount];{var map=new Dictionary<(long,long,long),int>();for(int k=0;k<origCount;k++){var p=V[k];var key=((long)Mathf.RoundToInt(p.x*1000),(long)Mathf.RoundToInt(p.y*1000),(long)Mathf.RoundToInt(p.z*1000));if(!map.TryGetValue(key,out int cn))map[key]=cn=k;canon[k]=cn;}}
        var canonL=new List<int>(canon);
        long EK(int a,int b){int x=canonL[a],y=canonL[b];return x<y?((long)x<<32)|(uint)y:((long)y<<32)|(uint)x;}
        float reach=radius+Blend+2;int passes=0;
        for(int pass=0;pass<5;pass++)
        {
            var region=new List<int>();var mark=new HashSet<long>();
            for(int t=0;t<tris.Count;t+=3)
            {
                var a=V[tris[t]];var b=V[tris[t+1]];var c=V[tris[t+2]];var cen=(a+b+c)/3f;if(Rad(new Vector2(cen.x,cen.z))>reach)continue;region.Add(t);
                float me=Mathf.Max((a-b).sqrMagnitude,Mathf.Max((b-c).sqrMagnitude,(c-a).sqrMagnitude));if(me<=MaxEdge*MaxEdge)continue;
                float mnx=Mathf.Min(a.x,Mathf.Min(b.x,c.x)),mxx=Mathf.Max(a.x,Mathf.Max(b.x,c.x)),mnz=Mathf.Min(a.z,Mathf.Min(b.z,c.z)),mxz=Mathf.Max(a.z,Mathf.Max(b.z,c.z));bool hit=false;
                for(int j=Mathf.Max(0,Mathf.FloorToInt((mnz-g.z0)/Cell));j<=Mathf.Min(n-1,Mathf.CeilToInt((mxz-g.z0)/Cell))&&!hit;j++)
                for(int i=Mathf.Max(0,Mathf.FloorToInt((mnx-g.x0)/Cell));i<=Mathf.Min(n-1,Mathf.CeilToInt((mxx-g.x0)/Cell));i++)if(sharp[j*n+i]){hit=true;break;}
                if(hit){mark.Add(EK(tris[t],tris[t+1]));mark.Add(EK(tris[t+1],tris[t+2]));mark.Add(EK(tris[t+2],tris[t]));}
            }
            if(mark.Count==0)break;passes++;
            for(bool changed=true;changed;){changed=false;foreach(var t in region){long e0=EK(tris[t],tris[t+1]),e1=EK(tris[t+1],tris[t+2]),e2=EK(tris[t+2],tris[t]);int cnt=(mark.Contains(e0)?1:0)+(mark.Contains(e1)?1:0)+(mark.Contains(e2)?1:0);if(cnt==2){mark.Add(e0);mark.Add(e1);mark.Add(e2);changed=true;}}}
            var mids=new Dictionary<long,int>();
            int Mid(int a,int b){long k=EK(a,b);if(mids.TryGetValue(k,out int r))return r;r=V.Count;mids[k]=r;V.Add((V[a]+V[b])*.5f);if(hUV)uv.Add((uv[a]+uv[b])*.5f);if(hUV2)uv2.Add((uv2[a]+uv2[b])*.5f);if(hCol)col.Add(Color.Lerp(col[a],col[b],.5f));if(hNor)nor.Add((nor[a]+nor[b]).normalized);canonL.Add(r);return r;}
            var nt=new List<int>(tris.Count+mark.Count*6);var inRegion=new HashSet<int>(region);
            for(int t=0;t<tris.Count;t+=3)
            {
                int a=tris[t],b=tris[t+1],c=tris[t+2];
                if(!inRegion.Contains(t)){nt.Add(a);nt.Add(b);nt.Add(c);continue;}
                bool m0=mark.Contains(EK(a,b)),m1=mark.Contains(EK(b,c)),m2=mark.Contains(EK(c,a));
                if(m0&&m1&&m2){int ab=Mid(a,b),bc=Mid(b,c),ca=Mid(c,a);nt.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});}
                else if(m0){int ab=Mid(a,b);nt.AddRange(new[]{a,ab,c,ab,b,c});}
                else if(m1){int bc=Mid(b,c);nt.AddRange(new[]{a,b,bc,a,bc,c});}
                else if(m2){int ca=Mid(c,a);nt.AddRange(new[]{a,b,ca,b,c,ca});}
                else{nt.Add(a);nt.Add(b);nt.Add(c);}
            }
            tris=nt;
        }
        int changedV=0,keptRoad=0;float maxCut=0,maxFill=0;
        for(int k=0;k<V.Count;k++)
        {
            var p=V[k];var p2=new Vector2(p.x,p.z);float r=Rad(p2);if(r>radius+Blend)continue;
            if(lab[g.Near(p.x,p.z)]==ROAD){keptRoad++;continue;}
            float y=Mathf.Lerp(g.Sample(Fcore,p.x,p.z),p.y,Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius,radius+Blend,r)));
            if(Mathf.Abs(y-p.y)>.001f){changedV++;maxCut=Mathf.Max(maxCut,p.y-y);maxFill=Mathf.Max(maxFill,y-p.y);}
            V[k]=new Vector3(p.x,y,p.z);
        }
        int tFixed=StitchTJunctions(V,tris,p=>Rad(p)<=radius+Blend,radius+Blend+2,Rad);
        var mesh=new Mesh{name="Mesh_TownV6_Terrain_terrace1",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(V);mesh.SetTriangles(tris,0);if(hUV)mesh.SetUVs(0,uv);if(hUV2)mesh.SetUVs(1,uv2);if(hCol)mesh.SetColors(col);
        mesh.RecalculateNormals();
        if(hNor){var rn=mesh.normals;for(int k=0;k<origCount;k++){var p=V[k];if(Rad(new Vector2(p.x,p.z))>radius+Blend+1)rn[k]=nor[k];}mesh.normals=rn;}
        mesh.RecalculateBounds();
        if(AssetDatabase.LoadAssetAtPath<Mesh>(DstMesh))AssetDatabase.DeleteAsset(DstMesh);AssetDatabase.CreateAsset(mesh,DstMesh);

        // --- 재질 사본 ---
        Material CopyMat(string from,string to,Color? tint){var m=AssetDatabase.LoadAssetAtPath<Material>(to);if(!m){AssetDatabase.CopyAsset(from,to);m=AssetDatabase.LoadAssetAtPath<Material>(to);}if(tint.HasValue&&m.HasProperty("_Tint")){m.SetColor("_Tint",tint.Value);EditorUtility.SetDirty(m);}return m;}
        // 2차: 축대 = H1 돌 사진 텍스처(빛 안 구움, 틴트 거의 흰색) · 갓돌 = 밝은 회백 단색 · 흙길 = 풀 틴트 × 가장자리 번짐 텍스처(바깥 끝 = 풀색 그대로)
        Texture2D Tex(string path,bool clampU)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            if(ti==null)throw new Exception("텍스처 없음 "+path+" — make_tex.py 먼저");
            ti.wrapModeU=clampU?TextureWrapMode.Clamp:TextureWrapMode.Repeat;ti.wrapModeV=TextureWrapMode.Repeat;ti.filterMode=FilterMode.Bilinear;ti.mipmapEnabled=true;ti.sRGBTexture=true;ti.textureCompression=TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        var stoneTex=Tex(StoneTex,false);var edgeTex=Tex(PathEdgeTex,true);
        var stone=AssetDatabase.LoadAssetAtPath<Material>(StoneMat);var st0=stone.HasProperty("_Tint")?stone.GetColor("_Tint"):Color.white;
        var retain=CopyMat(StoneMat,RetainMat,new Color(1.05f,1.02f,.96f,st0.a));retain.SetTexture("_MainTex",stoneTex);retain.SetTextureScale("_MainTex",new Vector2(1/StoneU,1/StoneV));EditorUtility.SetDirty(retain);
        var cap=CopyMat(StoneMat,CapMat,new Color(.86f,.83f,.76f,st0.a));
        var terrMat=AssetDatabase.LoadAssetAtPath<Material>(TerrainMat);var pathMat=CopyMat(TerrainMat,PathMat,terrMat.GetColor("_Tint"));pathMat.SetTexture("_MainTex",edgeTex);pathMat.SetTextureScale("_MainTex",Vector2.one);EditorUtility.SetDirty(pathMat);

        // --- 흙길 띠(최종 F 위 +4 cm) ---
        // 2차: 반폭 1.1 m(11칸), 정점 높이 = 새 지형 메시 레이 +3 cm(지형 삼각형과 어긋나 뜨지 않게), UV u = 가로 0..1, v = 길이/4 m
        var pv=new List<Vector3>();var pt=new List<int>();var puv=new List<Vector2>();float pathLen=0;const int PC=11;
        {
            var tgo=new GameObject("_tb_path_col"){hideFlags=HideFlags.HideAndDontSave};
            try{
                var tcol=tgo.AddComponent<MeshCollider>();tcol.sharedMesh=mesh;
                var pts=new List<Vector2>();for(int k=0;k<pathLine.Count-1;k++){var a=pathLine[k];var b=pathLine[k+1];int s=Mathf.Max(1,Mathf.CeilToInt((b-a).magnitude/.25f));for(int q=0;q<s;q++)pts.Add(Vector2.Lerp(a,b,q/(float)s));pathLen+=(b-a).magnitude;}pts.Add(pathLine[pathLine.Count-1]);
                float along=0;
                for(int k=0;k<pts.Count;k++)
                {
                    if(k>0)along+=(pts[k]-pts[k-1]).magnitude;var d=(pts[Mathf.Min(k+1,pts.Count-1)]-pts[Mathf.Max(k-1,0)]).normalized;var nn=new Vector2(-d.y,d.x);
                    for(int q=0;q<PC;q++){float u=q/(float)(PC-1);var p=pts[k]+nn*((u*2-1)*PathHalf);float y=tcol.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out var hit,1000)?hit.point.y:g.Sample(F,p.x,p.y);pv.Add(new Vector3(p.x,y+.03f,p.y));puv.Add(new Vector2(u,along/4f));}
                }
                for(int k=0;k<pts.Count-1;k++)for(int q=0;q<PC-1;q++){int a=k*PC+q,b=a+1,c=a+PC,d=a+PC+1;var tri=new[]{a,c,b,b,c,d};var nrm=Vector3.Cross(pv[c]-pv[a],pv[b]-pv[a]);if(nrm.y<0)tri=new[]{a,b,c,b,d,c};pt.AddRange(tri);}
            }finally{UnityEngine.Object.DestroyImmediate(tgo);}
        }
        var pmesh=new Mesh{name="Mesh_TerraceBlock_path"};pmesh.SetVertices(pv);pmesh.SetUVs(0,puv);pmesh.SetTriangles(pt,0);pmesh.RecalculateNormals();pmesh.RecalculateBounds();
        if(AssetDatabase.LoadAssetAtPath<Mesh>(PathAsset))AssetDatabase.DeleteAsset(PathAsset);AssetDatabase.CreateAsset(pmesh,PathAsset);
        var wmesh=new Mesh{name="Mesh_TerraceBlock_walls",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=2};wmesh.SetVertices(wv);wmesh.SetUVs(0,wuv);wmesh.SetTriangles(wt,0);wmesh.SetTriangles(ct,1);wmesh.RecalculateNormals();wmesh.RecalculateBounds();
        if(AssetDatabase.LoadAssetAtPath<Mesh>(WallAsset))AssetDatabase.DeleteAsset(WallAsset);AssetDatabase.CreateAsset(wmesh,WallAsset);
        AssetDatabase.SaveAssets();

        // --- 씬에 꽂기 ---
        var dst=AssetDatabase.LoadAssetAtPath<Mesh>(DstMesh);terr.GetComponent<MeshFilter>().sharedMesh=dst;var tmc=terr.GetComponent<MeshCollider>();if(tmc)tmc.sharedMesh=dst;
        var root=new GameObject(RootName);root.transform.SetParent(top,false);
        var wg=new GameObject("RetainingWalls");wg.transform.SetParent(root.transform,false);wg.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(WallAsset);wg.AddComponent<MeshRenderer>().sharedMaterials=new[]{retain,cap};wg.AddComponent<MeshCollider>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(WallAsset);
        var pg=new GameObject("DirtPath_house_field");pg.transform.SetParent(root.transform,false);pg.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(PathAsset);pg.AddComponent<MeshRenderer>().sharedMaterial=pathMat;
        var scene=terr.gameObject.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);

        // --- 기록(JSON) ---
        js.Append("{\"build\":{\"center\":["+center.x+","+center.z+"],\"radius\":"+radius+",\"blend\":"+Blend+",\"ms\":"+sw.ElapsedMilliseconds+",");
        js.Append("\"owners\":["+string.Join(",",owners.Select(o=>"{\"name\":\""+o.name+"\",\"h\":"+(o.road?"null":o.H(o.poly!=null?o.poly[0]:Vector2.zero).ToString("F2"))+",\"cells\":"+o.cells+"}"))+"],");
        js.Append("\"skipped\":["+string.Join(",",skipped.Select(s=>"\""+s+"\""))+"],\"roadMeshes\":"+roadMeshes+",");
        string f2(float x)=>x.ToString("F2");
        js.Append("\"terrain\":{\"vertsBefore\":"+origCount+",\"vertsAfter\":"+V.Count+",\"refinePasses\":"+passes+",\"changed\":"+changedV+",\"keptRoadZone\":"+keptRoad+",\"tJunctionsStitched\":"+tFixed+",\"maxCut\":"+f2(maxCut)+",\"maxFill\":"+f2(maxFill)+"},");
        js.Append("\"walls\":{\"sections\":"+sections+",\"lengthM\":"+wallLen.ToString("F1")+",\"maxH\":"+f2(wallMax)+",\"over2_65mLen\":"+tallLen.ToString("F1")+",\"meanH\":"+f2(wallLen>0?wallSum/wallLen:0)+",\"copingStones\":"+stones+",\"verts\":"+wv.Count+",\"byOwner\":{"+string.Join(",",perOwner.Select(kv=>"\""+kv.Key+"\":"+kv.Value.ToString("F1")))+"}},");
        js.Append("\"ledges\":["+string.Join(",",ledgeInfo)+"],");
        js.Append("\"path\":{\"lengthM\":"+pathLen.ToString("F1")+",\"hStart\":"+f2(hHouse)+",\"hEnd\":"+f2(hField)+"}}}");
        return js.ToString();
    }

    // 한 마디(s0..e) 축대: 앞면은 낮은 쪽으로 약간 눕힘, 윗면 평평(0.1 m 올림), 갓돌 한 줄. 반환 = 길이.
    // 2차: UV = (마디 줄 따라 잰 거리 m, 높이 m) → 재질 타일 2.5×1.88 m 로 돌 0.3~0.5 m. 마디 양 끝을 줄 방향으로 0.15 m 늘려 이웃 마디와 겹침(꺾인 모서리 검은 틈).
    static float WallSection(List<Vector3> v,List<int> wt,List<int> ct,List<Vector2> uv,List<Vector2> Pc,float[] S,Vector2[] L,float[] Fh,float[] Fl,int s0,int e,ref int stones,out float height)
    {
        float maxH=float.MinValue,minH=float.MaxValue,minL=float.MaxValue;for(int k=s0;k<=e;k++){maxH=Mathf.Max(maxH,Fh[k]);minH=Mathf.Min(minH,Fh[k]);minL=Mathf.Min(minL,Fl[k]);}
        float yTop=Mathf.Ceil(maxH/.1f)*.1f,yBot=minL-.3f,yBack=minH-.3f;height=yTop-minL;float bat=Thick*.5f+Batter*(yTop-yBot);
        Vector3 V3(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
        // 끝 겹침: 이 마디 안에서만 쓰는 점 사본
        var P=new Dictionary<int,Vector2>();for(int k=s0;k<=e;k++)P[k]=Pc[k];
        P[s0]=Pc[s0]+(Pc[s0]-Pc[s0+1]).normalized*SecOverlap;P[e]=Pc[e]+(Pc[e]-Pc[e-1]).normalized*SecOverlap;
        float U(int k)=>k==s0?S[k]-SecOverlap:k==e?S[k]+SecOverlap:S[k];
        Vector2 uvW(int k,float y)=>new Vector2(U(k),y);
        float len=0;
        for(int k=s0;k<e;k++)
        {
            var p0=P[k];var p1=P[k+1];var l0=L[k];var l1=L[k+1];len+=(Pc[k+1]-Pc[k]).magnitude;var hint=V3((l0+l1)*.5f,0);
            Quad(v,wt,uv,V3(p0+l0*Thick*.5f,yTop),V3(p1+l1*Thick*.5f,yTop),V3(p1+l1*bat,yBot),V3(p0+l0*bat,yBot),hint+Vector3.up*.1f,uvW(k,yTop),uvW(k+1,yTop),uvW(k+1,yBot),uvW(k,yBot));
            Quad(v,wt,uv,V3(p0-l0*Thick*.5f,yTop),V3(p1-l1*Thick*.5f,yTop),V3(p1+l1*Thick*.5f,yTop),V3(p0+l0*Thick*.5f,yTop),Vector3.up,uvW(k,0),uvW(k+1,0),uvW(k+1,Thick),uvW(k,Thick));
            Quad(v,wt,uv,V3(p0-l0*Thick*.5f,yBack),V3(p1-l1*Thick*.5f,yBack),V3(p1-l1*Thick*.5f,yTop),V3(p0-l0*Thick*.5f,yTop),-hint,uvW(k,yBack),uvW(k+1,yBack),uvW(k+1,yTop),uvW(k,yTop));
        }
        foreach(var (k,dir) in new[]{(s0,Pc[s0]-Pc[s0+1]),(e,Pc[e]-Pc[e-1])})
            Quad(v,wt,uv,V3(P[k]-L[k]*Thick*.5f,yBack),V3(P[k]-L[k]*Thick*.5f,yTop),V3(P[k]+L[k]*Thick*.5f,yTop),V3(P[k]+L[k]*bat,yBot),V3(dir.normalized,0),
                new Vector2(0,yBack),new Vector2(0,yTop),new Vector2(Thick,yTop),new Vector2(bat+Thick*.5f,yBot));
        // 갓돌: 0.55 m 돌, 4 cm 틈, 높이 ±1.5 cm 흔들림
        Vector2 At(float d){for(int k=s0;k<e;k++){float sl=(Pc[k+1]-Pc[k]).magnitude;if(d<=sl||k==e-1)return Vector2.Lerp(Pc[k],Pc[k+1],sl>1e-6f?Mathf.Clamp01(d/sl):0);d-=sl;}return Pc[e];}
        int seed=s0*7919+e*104729;int cnt=Mathf.Max(1,Mathf.RoundToInt(len/.59f));float step=len/cnt;
        for(int k=0;k<cnt;k++)
        {
            float jit=((seed=(seed*1103515245+12345)&0x7fffffff)%1000)/1000f*.03f-.015f;
            var q0=At(k*step+.02f);var q1=At((k+1)*step-.02f);if((q1-q0).sqrMagnitude<1e-4f)continue;
            Box(v,ct,uv,q0,q1,CapW,yTop-.02f,yTop+CapH+jit);stones++;
        }
        return len;
    }
    static void Quad(List<Vector3> v,List<int> t,List<Vector2> uv,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 hint,Vector2 ua=default,Vector2 ub=default,Vector2 uc=default,Vector2 ud=default)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),hint)<0){var tmp=b;b=d;d=tmp;var tu=ub;ub=ud;ud=tu;}
        int i=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);uv.Add(ua);uv.Add(ub);uv.Add(uc);uv.Add(ud);t.Add(i);t.Add(i+1);t.Add(i+2);t.Add(i);t.Add(i+2);t.Add(i+3);
    }
    static void Box(List<Vector3> v,List<int> t,List<Vector2> uv,Vector2 p0,Vector2 p1,float w,float y0,float y1)
    {
        var dir=(p1-p0).normalized;var nn=new Vector2(-dir.y,dir.x)*(w*.5f);Vector3 P(Vector2 q,float y)=>new Vector3(q.x,y,q.y);
        var a0=p0-nn;var a1=p0+nn;var b0=p1-nn;var b1=p1+nn;var ctr=P((p0+p1)*.5f,(y0+y1)*.5f);
        void F(Vector3 a,Vector3 b,Vector3 c,Vector3 d)=>Quad(v,t,uv,a,b,c,d,(a+b+c+d)*.25f-ctr);
        F(P(a0,y1),P(a1,y1),P(b1,y1),P(b0,y1));F(P(a0,y0),P(a0,y1),P(b0,y1),P(b0,y0));F(P(a1,y0),P(a1,y1),P(b1,y1),P(b1,y0));
        F(P(a0,y0),P(a1,y0),P(a1,y1),P(a0,y1));F(P(b0,y0),P(b1,y0),P(b1,y1),P(b0,y1));
    }

    // 원본 v2 지형엔 타일 이음(T자: 한쪽은 잘게, 한쪽은 굵게 쪼갠 모서리)이 반경 68 m 안에만 1,669개 있다. 평평할 땐 안 벌어지지만
    // 높이를 바꾸면 잘게 쪼갠 쪽 정점이 굵은 모서리 직선에서 떠서 1px 틈(뒤 건물·하늘이 비침)이 된다. 그 정점을 상대 모서리 직선 위로 내린다.
    static int StitchTJunctions(List<Vector3> V,List<int> tris,System.Func<Vector2,bool> editable,float reach,System.Func<Vector2,float> rad)
    {
        (int,int) XZ(Vector3 p)=>(Mathf.RoundToInt(p.x*1000),Mathf.RoundToInt(p.z*1000));
        var cnt=new Dictionary<((int,int),(int,int)),(int a,int b,int n)>();
        for(int t=0;t<tris.Count;t+=3)
        {
            var cen=(V[tris[t]]+V[tris[t+1]]+V[tris[t+2]])/3f;if(rad(new Vector2(cen.x,cen.z))>reach)continue;
            for(int e=0;e<3;e++)
            {
                int a=tris[t+e],b=tris[t+(e+1)%3];var ka=XZ(V[a]);var kb=XZ(V[b]);
                var key=ka.CompareTo(kb)<0?(ka,kb):(kb,ka);cnt[key]=cnt.TryGetValue(key,out var c)?(c.a,c.b,c.n+1):(a,b,1);
            }
        }
        var open=new List<(int a,int b)>();var verts=new Dictionary<(int,int),List<int>>();
        foreach(var kv in cnt){if(kv.Value.n!=1)continue;open.Add((kv.Value.a,kv.Value.b));verts[kv.Key.Item1]=null;verts[kv.Key.Item2]=null;}
        if(open.Count==0)return 0;
        for(int k=0;k<V.Count;k++){var kk=XZ(V[k]);if(verts.TryGetValue(kk,out var l)){if(l==null)verts[kk]=l=new List<int>();l.Add(k);}}
        var grid=new Dictionary<(int,int),List<int>>();
        for(int e=0;e<open.Count;e++)
        {
            Vector3 A=V[open[e].a],B=V[open[e].b];
            for(int j=Mathf.FloorToInt(Mathf.Min(A.z,B.z));j<=Mathf.FloorToInt(Mathf.Max(A.z,B.z));j++)
            for(int i=Mathf.FloorToInt(Mathf.Min(A.x,B.x));i<=Mathf.FloorToInt(Mathf.Max(A.x,B.x));i++)
            {if(!grid.TryGetValue((i,j),out var l))grid[(i,j)]=l=new List<int>();l.Add(e);}
        }
        int fixedN=0;
        for(int it=0;it<3;it++)   // 이음 끝점이 또 다른 이음 위에 있는 사슬 → 3바퀴
        foreach(var kv in verts)
        {
            var P=V[kv.Value[0]];var p2=new Vector2(P.x,P.z);if(!editable(p2))continue;
            if(!grid.TryGetValue((Mathf.FloorToInt(P.x),Mathf.FloorToInt(P.z)),out var es))continue;
            foreach(int e in es)
            {
                Vector3 A=V[open[e].a],B=V[open[e].b];var ab=new Vector2(B.x-A.x,B.z-A.z);float L2=ab.sqrMagnitude;if(L2<1e-6f)continue;
                float s=Vector2.Dot(p2-new Vector2(A.x,A.z),ab)/L2;if(s<=.001f||s>=.999f)continue;
                if((new Vector2(A.x,A.z)+ab*s-p2).sqrMagnitude>4e-6f)continue;   // 2 mm 안 = 그 모서리 위
                float y=Mathf.Lerp(A.y,B.y,s);if(Mathf.Abs(y-P.y)<1e-4f)break;
                foreach(int k in kv.Value)V[k]=new Vector3(V[k].x,y,V[k].z);if(it==0)fixedN++;break;
            }
        }
        return fixedN;
    }

    // 1:2 원뿔 포락: lo(x) = max(lo(이웃) − 거리/2), up(x) = min(up(이웃) + 거리/2). 5×5 체임퍼(1, √2, √5 칸) 앞뒤 2바퀴 → 원 거리와 오차 ~2%, 끊김 없음.
    static readonly (int di,int dj)[] ChamF={(-1,0),(-2,-1),(-1,-1),(0,-1),(1,-1),(2,-1),(-1,-2),(1,-2)};
    static void Envelope(int n,float[] lo,float[] up)
    {
        var w=ChamF.Select(d=>Mathf.Sqrt(d.di*d.di+d.dj*d.dj)*Cell*.5f).ToArray();
        for(int round=0;round<2;round++)
        {
            for(int j=0;j<n;j++)for(int i=0;i<n;i++)
            {
                int idx=j*n+i;float l=lo[idx],u=up[idx];
                for(int t=0;t<ChamF.Length;t++){int ii=i+ChamF[t].di,jj=j+ChamF[t].dj;if(ii<0||ii>=n||jj<0)continue;int nb=jj*n+ii;l=Mathf.Max(l,lo[nb]-w[t]);u=Mathf.Min(u,up[nb]+w[t]);}
                lo[idx]=l;up[idx]=u;
            }
            for(int j=n-1;j>=0;j--)for(int i=n-1;i>=0;i--)
            {
                int idx=j*n+i;float l=lo[idx],u=up[idx];
                for(int t=0;t<ChamF.Length;t++){int ii=i-ChamF[t].di,jj=j-ChamF[t].dj;if(ii<0||ii>=n||jj>=n)continue;int nb=jj*n+ii;l=Mathf.Max(l,lo[nb]-w[t]);u=Mathf.Min(u,up[nb]+w[t]);}
                lo[idx]=l;up[idx]=u;
            }
        }
    }

    static List<Vector2> Simplify(List<Vector2> p,float tol)
    {
        if(p.Count<3)return p;var keep=new bool[p.Count];keep[0]=keep[p.Count-1]=true;var st=new Stack<(int,int)>();st.Push((0,p.Count-1));
        while(st.Count>0){var (a,b)=st.Pop();float best=0;int bi=-1;for(int k=a+1;k<b;k++){float d=DistSeg(p[k],p[a],p[b]);if(d>best){best=d;bi=k;}}if(bi>=0&&best>tol){keep[bi]=true;st.Push((a,bi));st.Push((bi,b));}}
        var r=new List<Vector2>();for(int k=0;k<p.Count;k++)if(keep[k])r.Add(p[k]);return r;
    }
    static List<Vector2> Resample(List<Vector2> p,float step)
    {
        var r=new List<Vector2>();for(int k=0;k<p.Count-1;k++){int s=Mathf.Max(1,Mathf.CeilToInt((p[k+1]-p[k]).magnitude/step));for(int q=0;q<s;q++)r.Add(Vector2.Lerp(p[k],p[k+1],q/(float)s));}r.Add(p[p.Count-1]);return r;
    }
    // 라벨 o 의 경계선(마칭 스퀘어, 칸 변 가운데를 이은 선). 열린 선/닫힌 고리 모두.
    static List<List<Vector2>> Contours(Grid g,int[] lab,int o)
    {
        int n=g.n;var adj=new Dictionary<long,List<long>>();
        void Link(long a,long b){if(!adj.TryGetValue(a,out var la))adj[a]=la=new List<long>();la.Add(b);if(!adj.TryGetValue(b,out var lb))adj[b]=lb=new List<long>();lb.Add(a);}
        long H(int i,int j)=>(long)(j*n+i)*2;long Vt(int i,int j)=>(long)(j*n+i)*2+1;
        for(int j=0;j<n-1;j++)for(int i=0;i<n-1;i++)
        {
            int cs=(lab[j*n+i]==o?1:0)|(lab[j*n+i+1]==o?2:0)|(lab[(j+1)*n+i+1]==o?4:0)|(lab[(j+1)*n+i]==o?8:0);if(cs==0||cs==15)continue;
            long e0=H(i,j),e1=Vt(i+1,j),e2=H(i,j+1),e3=Vt(i,j);
            switch(cs){case 1:case 14:Link(e3,e0);break;case 2:case 13:Link(e0,e1);break;case 3:case 12:Link(e3,e1);break;case 4:case 11:Link(e1,e2);break;
                case 6:case 9:Link(e0,e2);break;case 7:case 8:Link(e3,e2);break;case 5:Link(e3,e0);Link(e1,e2);break;case 10:Link(e0,e1);Link(e2,e3);break;}
        }
        Vector2 Pos(long id){int idx=(int)(id/2);var p=g.P(idx);return (id&1)==0?p+new Vector2(Cell*.5f,0):p+new Vector2(0,Cell*.5f);}
        var used=new HashSet<long>();var res=new List<List<Vector2>>();
        foreach(var s in adj.Keys.OrderBy(k=>adj[k].Count).ToList())
        {
            if(used.Contains(s))continue;var line=new List<Vector2>{Pos(s)};used.Add(s);long prev=-1,cur=s;
            while(true)
            {
                long nx=-1;foreach(var q in adj[cur])if(q!=prev&&!used.Contains(q)){nx=q;break;}
                if(nx<0){if(line.Count>2&&adj[cur].Contains(s))line.Add(Pos(s));break;}
                used.Add(nx);line.Add(Pos(nx));prev=cur;cur=nx;
            }
            res.Add(line);
        }
        return res;
    }
}
