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
    // 4차: 텃밭(집 뿌리 좌표, 집 동쪽 옆) 6×5.5 m · 말뚝 틈(흙길 들어오는 곳) z · 흙길 반폭 0.55 m(한 사람 길)
    const float GX0=-12.6f,GX1=-6.6f,GZ0=-2.5f,GZ1=3.0f,GateZ=1.6f,YardPathHalf=.55f;
    const float TwoTier=2.5f,LedgeW=1.6f,LedgeRoadGap=.6f,SecOverlap=.15f,StoneU=2.5f,StoneV=1.88f,PathHalf=1.1f,SmoothRoadGap=2f;
    // 3차: 사람 길. 연석 높이 0.13·폭 0.15 / 보도 큰길 1.8·마을길 1.5·좁으면 0.8 / 길 띠 0.4 m 안 땅은 그대로(길 밑 지형 안 움직이게)
    //      계단 단 높이 목표 0.165 / 밭 단 = 밭 안 지형 중앙값 +0.5(앞에 낮은 돌담) / 포장 텍스처 한 장 = 1.6 m
    const float Curb=.13f,CurbW=.15f,RoadCore=.4f,WalkMain=1.8f,WalkVillage=1.5f,WalkNarrow=.8f,Rise=.165f,FieldRaise=.5f,PaveTile=1.6f;
    const string PaveTex=Folder+"TV6_Paving.png",PaveMat=Folder+"TV6_Paving.mat",WalkAsset=Folder+"Mesh_TerraceBlock_walk.asset",GardenAsset=Folder+"Mesh_TerraceBlock_garden.asset";
    // 5차(도현 8b98 "PS1 질감"): 0.6 m 까지는 담 대신 풀 비탈 · 담은 0.45 m 부터 · 계단의 낮은 쪽(채움)은 늘 비탈 → 계단 옆 담은 높은 쪽 하나
    //   돌 = B 방식 한 장 20×4 m(벽 길이 × 밑동부터 높이, 32 px/m, Point — 집과 같은 필터) · 갓돌 = 이어진 띠(가장 밝은 돌보다 한 단 밝게)
    //   벽 밑동·계단 옆 풀 덤불 · 계단 챌판 = 디딤보다 어두운 재질
    const float SlopeMax=.6f,WallMinB=.45f,StoneBU=20f,StoneBV=4f;const bool PlaceGarden=false;static bool TwoStairs=false;
    const string StoneTexB=Folder+"TV6_RetainStoneB.png",CapBMat=Folder+"TV6_RetainCapB.mat",RiserMat=Folder+"TV6_StairRiser.mat",TuftAsset=Folder+"Mesh_TerraceBlock_tufts.asset";
    static readonly Color CapB=new Color(.6392f,.5765f,.4706f);
    // 6차(D-213 「차고지로도 자연스럽게 걸어갈 수 있도록」): 큰길 북쪽 보도 → 곧은 돌계단(x≈214.5) → 차고 남쪽 벽 따라 포장 골목(차고 바닥 높이) → 차고 앞 마당 남쪽 가장자리.
    //   골목 = 차고 뿌리 +x(남쪽) 벽에서 AlleyOut m 까지 평평 · 포장 띠 = 벽에서 AlleyPave0..1 m · 서쪽 끝 = 계단 머리 서쪽 1.2 m · 동쪽 끝 = 차고 사각형 끝(앞 마당)
    const bool GarageWalk=true;const float GarageStairX=214.5f,AlleyOut=2.6f,AlleyPave0=.2f,AlleyPave1=1.6f;
    // 7차(PD 20261030-2120, D-206 방식 「건물은 그대로, 길이 올라간다」): 차 진입로. 큰길(j_245 판 북쪽 가장자리, x≈245.8 z≈-61.3)에서 북북동(방위 20°)으로 갈라져
    //   R 11 m 원호로 110° 왼쪽으로 돌아 서쪽을 보며 차고 앞 마당(자갈 땅) 북동 모서리 안 x≈232.3 z -46.5 까지 들어간다(x 234.3 에서 끊으면 풀 띠 2 m 가 남았다). 곧게 오르면 어디서 출발해도 17 m 안에 2 m(>11%).
    //   높이 = 판 +2 cm → 마당(26.64) +2 cm, 경사 사다리꼴(양끝 3 m 완화 · 마지막 1 m 평평) → 최대 8.7%(길이 26.3 m). 폭 4.5 m.
    const bool GarageDrive=true;const float DriveW=4.5f,DriveR=11f,DriveB0=20f,DriveLead=2f,DriveTail=3.2f,DriveEase=3f,DriveFlat=1f,DriveY1=26.66f,DriveBot=24.3f;
    static readonly Vector2 DriveC=new Vector2(235.5f,-57.5f);const string DriveAsset=Folder+"Mesh_TerraceBlock_drive.asset";
    static readonly List<(Vector3 p,Vector2 n,float s)> Foot=new List<(Vector3,Vector2,float)>();
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
    // hard = 경계에서 원지형을 그대로 둔다(높이차를 비탈 대신 담이 받음) · late = 길 라벨 뒤에 덮어 쓰는 사람 길(보도·계단)
    class Walk{public string name;public List<Vector2> e=new List<Vector2>(),n=new List<Vector2>();public List<float> w=new List<float>(),y=new List<float>();}
    class Stair{public string name;public List<Vector2> line;public float hl,hh,width,L,r,t;public int N;}
    class Owner{public string name;public float h;public bool road,hard,late;public List<Vector2> poly;public Func<Vector2,float> hf;public int cells;public float H(Vector2 p)=>hf!=null?hf(p):h;}

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
    static Vector2 PolyAt(List<Vector2> line,float s){for(int k=0;k<line.Count-1;k++){float l=(line[k+1]-line[k]).magnitude;if(s<=l||k==line.Count-2)return Vector2.Lerp(line[k],line[k+1],l>1e-6f?Mathf.Clamp01(s/l):0);s-=l;}return line[line.Count-1];}
    // 선 위 가장 가까운 점:반환 = 거리, s = 시작부터 잰 길이, cap = 양 끝 밖으로 투영됨
    static float PolyNear(List<Vector2> line,Vector2 p,out float s,out bool cap)
    {
        float best=1e9f,acc=0;s=0;cap=false;
        for(int k=0;k<line.Count-1;k++)
        {
            var a=line[k];var ab=line[k+1]-a;float l=ab.magnitude;float t=Vector2.Dot(p-a,ab)/Mathf.Max(1e-8f,l*l);
            bool c=(k==0&&t<0)||(k==line.Count-2&&t>1);float tc=Mathf.Clamp01(t);float d=(a+ab*tc-p).magnitude;
            if(d<best){best=d;s=acc+tc*l;cap=c;}acc+=l;
        }
        return best;
    }
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
        var sw=System.Diagnostics.Stopwatch.StartNew();var js=new StringBuilder();var c2=new Vector2(center.x,center.z);Foot.Clear();
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
        // 4차(D-210 도현 「밭은 집에 딸린 텃밭」): 따로 올린 밭 단·돌담·계단을 없앤다. 마당을 집 동쪽(뿌리 -x)으로 넓혀(L자) 그 안 같은 높이에 텃밭.
        //   마당 = 집 둘레 1.2 m + 문 앞 5.5 m + 동쪽 날개(뿌리 x GX0-0.8..집 옆, z GZ0-0.8..GZ1+3.2). 둘레 축대는 마당 것 하나.
        var qh=Quaternion.Euler(0,hyaw,0);
        Vector2 HL(float lx,float lz){var w=hpos+qh*new Vector3(lx,0,lz);return new Vector2(w.x,w.z);}
        float yx0=hx0-1.2f,yx1=hx1+1.2f,yz0=hz0-1.2f,yz1=hz1+5.5f;
        var yardPoly=new List<Vector2>{HL(GX0-.8f,GZ0-.8f),HL(yx0,GZ0-.8f),HL(yx0,yz0),HL(yx1,yz0),HL(yx1,yz1),HL(yx0,yz1),HL(yx0,GZ1+3.2f),HL(GX0-.8f,GZ1+3.2f)};
        // 흙길 한 줄: 앞문(문 앞 바닥 끝) → 집 동쪽 모퉁이를 돌아 → 텃밭 서쪽 말뚝 틈. 집 프리팹엔 옆문·뒷문이 없어(DOOR_01 하나) 앞문에서 나간다.
        Vector2 ps=HL(-.7f,hz1+.6f),pmid=HL(GX1+.6f,hz1+.9f),pe=HL(GX1+.25f,GateZ);
        var pathLine=new List<Vector2>();for(int k=0;k<=16;k++){float t=k/16f;pathLine.Add((1-t)*(1-t)*ps+2*(1-t)*t*pmid+t*t*pe);}
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
        // pcore: 이미 있는 보행 리본(V2p) 위 — 3차 보도가 덮지 않는다
        int roadMeshes=0;var core=new bool[N];var pcore=new bool[N];var roadCols=new List<Collider>();var chains=new List<(string name,string kind,float w,List<Vector2> p)>();
        foreach(Transform ch in top.Find("RoadsV2"))
        {
            if(!ch.gameObject.activeInHierarchy)continue;var mf=ch.GetComponent<MeshFilter>();var mr=ch.GetComponent<MeshRenderer>();if(!mf||!mf.sharedMesh||!mr||!mr.enabled)continue;
            var b=mr.bounds;if(b.max.x<g.x0||b.min.x>g.x0+2*G||b.max.z<g.z0||b.min.z>g.z0+2*G)continue;
            bool car=!ch.name.StartsWith("ROAD_V2p");float m=car?CarMargin:PathMargin;roadMeshes++;
            if(car){var rc=ch.GetComponent<Collider>();if(rc)roadCols.Add(rc);var rs=ch.GetComponent<RoadSurface>();if(rs!=null&&ch.name.StartsWith("ROAD_V2e")&&rs.centerline!=null&&rs.centerline.Length>=2)chains.Add((ch.name.Substring(9),rs.kind,rs.width,rs.centerline.Select(q=>new Vector2(q.x,q.z)).ToList()));}
            var vs=mf.sharedMesh.vertices;var tr=mf.sharedMesh.triangles;var w=new Vector2[vs.Length];for(int k=0;k<vs.Length;k++){var p=ch.TransformPoint(vs[k]);w[k]=new Vector2(p.x,p.z);}
            for(int t=0;t<tr.Length;t+=3)
            {
                var a=w[tr[t]];var bb=w[tr[t+1]];var cc=w[tr[t+2]];
                float mnx=Mathf.Min(a.x,Mathf.Min(bb.x,cc.x))-m,mxx=Mathf.Max(a.x,Mathf.Max(bb.x,cc.x))+m,mnz=Mathf.Min(a.y,Mathf.Min(bb.y,cc.y))-m,mxz=Mathf.Max(a.y,Mathf.Max(bb.y,cc.y))+m;
                for(int j=Mathf.Max(0,Mathf.FloorToInt((mnz-g.z0)/Cell));j<=Mathf.Min(g.n-1,Mathf.CeilToInt((mxz-g.z0)/Cell));j++)
                for(int i=Mathf.Max(0,Mathf.FloorToInt((mnx-g.x0)/Cell));i<=Mathf.Min(g.n-1,Mathf.CeilToInt((mxx-g.x0)/Cell));i++)
                {
                    int idx=g.Idx(i,j);if(lab[idx]==ROAD&&(car?core[idx]:pcore[idx]))continue;var p=g.P(i,j);
                    float dm=InTri(p,a,bb,cc)?0:Mathf.Min(DistSeg(p,a,bb),Mathf.Min(DistSeg(p,bb,cc),DistSeg(p,cc,a)));
                    if(dm<=m)lab[idx]=ROAD;if(car&&dm<=RoadCore)core[idx]=true;if(!car&&dm<=RoadCore)pcore[idx]=true;
                }
            }
        }

        // --- 3차: 사람 길(보도·연석·골목 계단) — 길 라벨 위에 덮어 쓴다(길 띠 0.4 m 안 = core 는 그대로) ---
        float RoadY(Vector2 p,out bool hit)
        {
            float best=float.NegativeInfinity;
            foreach(var c in roadCols){var bb=c.bounds;if(p.x<bb.min.x||p.x>bb.max.x||p.y<bb.min.z||p.y>bb.max.z)continue;if(c.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out var h,1000)&&h.point.y>best)best=h.point.y;}
            hit=!float.IsNegativeInfinity(best);return best;
        }
        var blockers=owners.Where(o=>o.name!="house_home+yard"&&!o.road).Select(o=>o.poly).ToList();blockers.Add(hpoly);
        // 갈림목을 사이에 두고 끊긴 같은 폭 차길(예: 큰길 165_0 → 165_1)은 이어서 보도를 한 줄로(갈림목 북쪽 담 쪽 보도가 끊기지 않게)
        for(bool merged=true;merged;)
        {
            merged=false;
            for(int a=0;a<chains.Count&&!merged;a++)for(int b=0;b<chains.Count&&!merged;b++)
            {
                if(a==b||Mathf.Abs(chains[a].w-chains[b].w)>.01f)continue;
                foreach(bool rev in new[]{false,true})
                {
                    var A=chains[a].p;var B=rev?Enumerable.Reverse(chains[b].p).ToList():chains[b].p;
                    var pa=A[A.Count-1];var pb=B[0];float d=(pb-pa).magnitude;if(d>16||d<.01f)continue;
                    var ta=(A[A.Count-1]-A[Mathf.Max(0,A.Count-3)]).normalized;var tb=(B[Mathf.Min(B.Count-1,2)]-B[0]).normalized;
                    if(Vector2.Dot(ta,(pb-pa)/d)<.8f||Vector2.Dot(tb,(pb-pa)/d)<.8f)continue;
                    var J2=new List<Vector2>(A);int sN=Mathf.CeilToInt(d);
                    for(int s=1;s<sN;s++){float t=s/(float)sN,t2=t*t,t3=t2*t;J2.Add((2*t3-3*t2+1)*pa+(t3-2*t2+t)*d*ta+(-2*t3+3*t2)*pb+(t3-t2)*d*tb);}
                    J2.AddRange(B);chains[a]=(chains[a].name+"+"+chains[b].name,chains[a].kind,chains[a].w,J2);chains.RemoveAt(b);merged=true;break;
                }
            }
        }
        var walks=new List<Walk>();
        bool Clear(Vector2 q){if(Rad(q)>radius-2)return false;RoadY(q,out bool h);if(h)return false;
            int ci=Mathf.RoundToInt((q.x-g.x0)/Cell),cj=Mathf.RoundToInt((q.y-g.z0)/Cell);if(ci>=0&&cj>=0&&ci<g.n&&cj<g.n&&pcore[g.Idx(ci,cj)])return false;foreach(var bp in blockers)if(InPoly(q,bp))return false;return true;}
        foreach(var ch in chains)
        {
            var cp=Resample(ch.p,.5f);if(!cp.Any(q=>Rad(q)<radius))continue;int m=cp.Count;float pref=ch.kind=="main"?WalkMain:WalkVillage;
            foreach(int side in new[]{1,-1})
            {
                var E=new Vector2[m];var Nn=new Vector2[m];var Y=new float[m];var Wr=new float[m];
                for(int k=0;k<m;k++)
                {
                    var tg=(cp[Mathf.Min(k+2,m-1)]-cp[Mathf.Max(k-2,0)]).normalized;var nn=new Vector2(-tg.y,tg.x)*side;Nn[k]=nn;E[k]=cp[k]+nn*(ch.w*.5f);
                    Y[k]=RoadY(E[k]-nn*.15f,out bool hit);if(!hit){Wr[k]=0;continue;}
                    foreach(var wd in new[]{pref,WalkNarrow}){if(new[]{.3f,CurbW+wd*.5f,CurbW+wd,CurbW+wd+.3f}.All(t=>Clear(E[k]+nn*t))){Wr[k]=wd;break;}}
                }
                // 폭: 앞뒤 1.5 m 최소 → 1.5 m 평균(장애물 앞에서 미리 좁아짐), 높이: 1 m 평균
                var We=new float[m];for(int k=0;k<m;k++){float mn=9;for(int q=Mathf.Max(0,k-3);q<=Mathf.Min(m-1,k+3);q++)mn=Mathf.Min(mn,Wr[q]);We[k]=mn;}
                var Ws=new float[m];for(int k=0;k<m;k++){float s=0;int c=0;for(int q=Mathf.Max(0,k-3);q<=Mathf.Min(m-1,k+3);q++){s+=We[q];c++;}Ws[k]=We[k]<=0?0:s/c;}
                for(int k=0;k<m;)
                {
                    // (3차 고침: 평균이 0 이웃을 섞어 끝이 0.6~0.8 m 로 가늘어지던 것 → 갓길 최소 0.8 m 아래는 자른다)
                    if(Ws[k]<WalkNarrow-.01f){k++;continue;}int e=k;while(e+1<m&&Ws[e+1]>=WalkNarrow-.01f)e++;
                    if((e-k)*.5f>=3f)
                    {
                        var wk=new Walk{name="walk_"+ch.name.Substring(0,Mathf.Min(24,ch.name.Length))+(side>0?"_L":"_R")+walks.Count};
                        for(int q=k;q<=e;q++){float s=0;int c=0;for(int r=Mathf.Max(k,q-2);r<=Mathf.Min(e,q+2);r++){s+=Y[r];c++;}wk.e.Add(E[q]);wk.n.Add(Nn[q]);wk.w.Add(Ws[q]);wk.y.Add(s/c);}
                        walks.Add(wk);
                    }
                    k=e+1;
                }
            }
        }
        int WalkNear(Walk wk,Vector2 p){int bi=0;float bd=1e9f;for(int k=0;k<wk.e.Count;k++){float d=(wk.e[k]-p).sqrMagnitude;if(d<bd){bd=d;bi=k;}}return bi;}
        int firstLate=owners.Count;
        foreach(var wk in walks)
        {
            var poly=new List<Vector2>();for(int k=0;k<wk.e.Count;k++)poly.Add(wk.e[k]+wk.n[k]*CurbW);for(int k=wk.e.Count-1;k>=0;k--)poly.Add(wk.e[k]+wk.n[k]*(CurbW+wk.w[k]));
            var w0=wk;owners.Add(new Owner{name=wk.name,poly=poly,late=true,hf=p=>w0.y[WalkNear(w0,p)]+Curb-.04f});
        }
        // 보도 뒤 가장자리로 붙이기: 앵커에서 가장 가까운 보도 점의 뒤 가장자리(+0.1 m 안쪽) 와 보도 윗면 높이
        bool WalkBack(Vector2 anchor,out Vector2 back,out float top)
        {
            back=anchor;top=0;float bd=9;bool ok=false;
            foreach(var wk in walks)for(int k=0;k<wk.e.Count;k++){var b=wk.e[k]+wk.n[k]*(CurbW+wk.w[k]-.1f);float d=(b-anchor).magnitude;if(d<bd){bd=d;back=b;top=wk.y[k]+Curb;ok=true;}}
            return ok;
        }
        // 골목 계단: 낮은 끝 → 높은 끝. 단 수 = 높이차/0.165 반올림, 디딤 = 길이/단 수. 계단 밑 땅 = 디딤 코보다 낮은 경사(디딤이 덮음).
        var stairs=new List<Stair>();var stairNote=new List<string>();
        void AddStair(string name,List<Vector2> line,float hl,float hh,float width)
        {
            float L=0;for(int k=1;k<line.Count;k++)L+=(line[k]-line[k-1]).magnitude;int Nst=Mathf.Max(1,Mathf.RoundToInt((hh-hl)/Rise));
            var st=new Stair{name=name,line=line,hl=hl,hh=hh,width=width,L=L,N=Nst,r=(hh-hl)/Nst,t=L/Nst};stairs.Add(st);
            owners.Add(new Owner{name=name,late=true,poly=line,hf=p=>{PolyNear(st.line,p,out float s,out _);return st.hl+s/st.L*(st.hh-st.hl)-.05f;}});
            stairNote.Add($"{{\"name\":\"{name}\",\"steps\":{Nst},\"rise\":{st.r:F3},\"tread\":{st.t:F2},\"lengthM\":{L:F1},\"width\":{width:F1},\"hLow\":{hl:F2},\"hHigh\":{hh:F2},\"low\":[{line[0].x:F1},{line[0].y:F1}],\"high\":[{line[line.Count-1].x:F1},{line[line.Count-1].y:F1}]}}");
        }
        {
            // ① 집 대문(마당 남동 모서리) → 큰길 북쪽 보도. 축대 사이를 비스듬히 내려간다.
            // (3차 고침: 머리를 흙길 출발점(≈189.4,-69.5)에서 남서로 1.2 m 떼고 곧게 — 같은 점에서 나가면 계단 홈이 흙길 머리를 1.36 m 끊었고,
            //  서쪽 2.5 m(187,-71)로 빼면 마당 앞 축대 면을 따라 내려가 남쪽 벽이 4.36 m 로 한 단이 됐다)
            var top0=new Vector2(188.4f,-70.4f);for(int it=0;it<20&&!InPoly(top0,yardPoly);it++)top0=Vector2.MoveTowards(top0,new Vector2(hpos.x,hpos.z),.3f);
            // 5차(PD 8b98 진단 1 "계단은 담 사이 짧은 돌계단 한 줄"): 사선 계단 둘 → 직선 하나. 마당 끝→보도 직각 거리가 x=186~194 에선 1.5~5 m 라
            //   4.4 m 를 못 오르고(27단×0.28 = 7.6 m 필요), x≈197 에서만 8~9 m 가 나온다 → 거기 하나만. 걸어서 도는 고리는 없어진다(왕복).
            if(TwoStairs&&WalkBack(new Vector2(202f,-76.3f),out var b1,out float y1))AddStair("stair_house_walk",new List<Vector2>{b1,Vector2.Lerp(b1,top0,.5f),top0},y1,hHouse,1.6f);
            // ② 큰길 북쪽 보도(3차와 같은 앵커) → 마당 동쪽 날개 남동 모서리(4차: 밭 단 대신 마당 높이). 걸어서 도는 고리가 유지되게.
            var yTop=HL(GX0+.2f,GZ1+2.5f);
            if(TwoStairs){if(WalkBack(new Vector2(210f,-74.8f),out var b2,out float y2))AddStair("stair_walk_yard",new List<Vector2>{b2,Vector2.Lerp(b2,yTop,.5f)+new Vector2(.5f,-.3f),yTop},y2,hHouse,1.6f);}
            else if(WalkBack(new Vector2(yTop.x+.8f,-76f),out var b2,out float y2))AddStair("stair_walk_yard",new List<Vector2>{b2,Vector2.Lerp(b2,yTop,.5f),yTop},y2,hHouse,1.6f);
        }
        // 6차: 보도는 Q_MART 자리(225,-71.5, 길 가장자리까지 차지)에서 x≈220 에 끊긴다 → 그 서쪽에서 차고 쪽으로 올라간다. 집·차고 둘 다 큰길 북쪽이라 길 건널 곳은 없음.
        List<Vector2> alleyPave=null;float alleyH=0;string alleyNote="null";
        if(GarageWalk)
        {
            var gar=GameObject.Find("D198_Buildings/D198_garage");
            if(gar&&Footprint(gar.transform,0f,out _,out float gMin,out _,out float gx1,out _,out float gz1))
            {
                float gyaw=gar.transform.eulerAngles.y;var gq=Quaternion.Euler(0,gyaw,0);var gpos=gar.transform.position;
                if(Vector3.Dot(gq*Vector3.right,Vector3.back)>.9f)   // 뿌리 +x 가 남쪽일 때만(yaw≈85)
                {
                    alleyH=gMin;
                    Vector2 GL(float lx,float lz){var w=gpos+gq*new Vector3(lx,0,lz);return new Vector2(w.x,w.z);}
                    float lzStair=(Quaternion.Inverse(gq)*(new Vector3(GarageStairX,0,gpos.z)-gpos)).z;
                    var alley=Rect(gpos,gyaw,gx1-.2f,gx1+AlleyOut,lzStair-2f,gz1);
                    owners.Add(new Owner{name="alley_garage",late=true,poly=alley,h=alleyH});
                    alleyPave=Rect(gpos,gyaw,gx1+AlleyPave0,gx1+AlleyPave1,lzStair-1.2f,gz1);
                    // 계단 머리 = 포장 판 바깥(남쪽) 모서리. 판 가운데로 들이면 마지막 두 디딤이 판(+3 cm, 0.3 m 두께) 밑으로 들어가 0.36 m 턱이 됐다.
                    var sTop=GL(gx1+AlleyPave1,lzStair);
                    if(WalkBack(new Vector2(sTop.x,-74f),out var b3,out float y3))AddStair("stair_walk_garage",new List<Vector2>{b3,Vector2.Lerp(b3,sTop,.5f),sTop},y3,alleyH,1.6f);
                    alleyNote=$"{{\"h\":{alleyH:F2},\"lenM\":{(GL(0,lzStair-1.2f)-GL(0,gz1)).magnitude:F1},\"paveW\":{AlleyPave1-AlleyPave0:F1},\"west\":[{alleyPave[0].x:F1},{alleyPave[0].y:F1}],\"east\":[{alleyPave[2].x:F1},{alleyPave[2].y:F1}]}}";
                }
            }
        }
        // 7차: 차 진입로 중심선(0.5 m 안팎 간격) + 높이 + 땅 터(반폭 +0.5 m, 진입로 윗면 -3 cm). 판(core) 위 칸은 안 덮으니 판 위에선 진입로가 판 위에 얹힌 둑이 된다(옆 = 치마 축대).
        List<Vector2> driveC=null,driveD=null;List<float> driveY=null;float driveHalf=DriveW*.5f;string driveNote="null";
        if(GarageDrive)
        {
            driveC=new List<Vector2>();driveD=new List<Vector2>();driveY=new List<float>();
            float La=DriveR*Mathf.Deg2Rad*(DriveB0+90),s0=-DriveLead,s1=La+DriveTail;int ns=Mathf.CeilToInt((s1-s0)/.5f);
            for(int k=0;k<=ns;k++)
            {
                float s=s0+(s1-s0)*k/ns;Vector2 p,d;
                if(s<0){float b0=DriveB0*Mathf.Deg2Rad;d=new Vector2(Mathf.Sin(b0),Mathf.Cos(b0));p=DriveC+new Vector2(Mathf.Cos(b0),-Mathf.Sin(b0))*DriveR+d*s;}
                else if(s>La){d=Vector2.left;p=DriveC+new Vector2(-(s-La),DriveR);}
                else{float b=(DriveB0-s/DriveR*Mathf.Rad2Deg)*Mathf.Deg2Rad;d=new Vector2(Mathf.Sin(b),Mathf.Cos(b));p=DriveC+new Vector2(Mathf.Cos(b),-Mathf.Sin(b))*DriveR;}
                driveC.Add(p);driveD.Add(d);
            }
            float y0=RoadY(driveC[0],out bool hit0)+.02f;if(!hit0)y0=g.Sample(Hc,driveC[0].x,driveC[0].y)+.02f;
            // 사다리꼴 경사: 오르는 길이 Lc 안에서 경사 비중 w(u) = min(1, u/e, (Lc-u)/e), 누적 비중으로 높이를 나눔
            float Lt=s1-s0,Lc=Lt-DriveFlat;var cum=new float[ns+1];
            for(int k=1;k<=ns;k++){float u=Lt*(k-.5f)/ns;float w=Mathf.Clamp01(Mathf.Min(u/DriveEase,(Lc-u)/DriveEase));cum[k]=cum[k-1]+w;}
            for(int k=0;k<=ns;k++)driveY.Add(y0+(DriveY1-y0)*cum[k]/cum[ns]);
            float gmax=0;for(int k=1;k<=ns;k++)gmax=Mathf.Max(gmax,(driveY[k]-driveY[k-1])/(driveC[k]-driveC[k-1]).magnitude);
            var dl=driveC;var dy=driveY;float ds=(s1-s0)/ns;
            // 터 = 반폭 +0.5 m: 옆 담 갓돌(폭 0.56)이 진입로 가장자리를 안 덮게(+0.25 일 땐 s=16 에서 0.24 m 걸침)
            var poly=new List<Vector2>();for(int k=0;k<=ns;k++){var nn=new Vector2(-driveD[k].y,driveD[k].x);poly.Add(driveC[k]+nn*(driveHalf+.5f));}
            for(int k=ns;k>=0;k--){var nn=new Vector2(-driveD[k].y,driveD[k].x);poly.Add(driveC[k]-nn*(driveHalf+.5f));}
            owners.Add(new Owner{name="drive_garage",late=true,poly=poly,hf=p=>{PolyNear(dl,p,out float s,out _);float t=s/ds;int a=Mathf.Clamp(Mathf.FloorToInt(t),0,dy.Count-1),b=Mathf.Min(a+1,dy.Count-1);return Mathf.Lerp(dy[a],dy[b],t-a)-.03f;}});
            driveNote=$"{{\"widthM\":{DriveW:F1},\"R\":{DriveR:F1},\"lengthM\":{Lt:F1},\"y0\":{y0:F2},\"y1\":{DriveY1:F2},\"maxGrade\":{gmax:F3},\"start\":[{driveC[0].x:F1},{driveC[0].y:F1}],\"end\":[{driveC[ns].x:F1},{driveC[ns].y:F1}]}}";
        }
        // 진입로 띠 안(중심선에서 r 안)인가
        bool InDrive(Vector2 p,float r){if(driveC==null)return false;return PolyNear(driveC,p,out _,out bool cp)<r&&!cp;}
        // 라벨 덮기(보도 먼저, 계단이 위). 길 core·건물 바닥은 안 덮는다.
        for(int o=firstLate;o<owners.Count;o++)
        {
            var ow=owners[o];var st=stairs.FirstOrDefault(s=>s.name==ow.name);
            var bpts=st!=null?st.line:ow.poly;float pad=st!=null?st.width:0;
            float mnx=bpts.Min(p=>p.x)-pad,mxx=bpts.Max(p=>p.x)+pad,mnz=bpts.Min(p=>p.y)-pad,mxz=bpts.Max(p=>p.y)+pad;
            for(int j=Mathf.Max(0,Mathf.FloorToInt((mnz-g.z0)/Cell));j<=Mathf.Min(g.n-1,Mathf.CeilToInt((mxz-g.z0)/Cell));j++)
            for(int i=Mathf.Max(0,Mathf.FloorToInt((mnx-g.x0)/Cell));i<=Mathf.Min(g.n-1,Mathf.CeilToInt((mxx-g.x0)/Cell));i++)
            {
                int idx=g.Idx(i,j);if(core[idx]||pcore[idx])continue;var p=g.P(i,j);bool inside;
                if(st!=null){float dd=PolyNear(st.line,p,out float _s,out bool endCap);inside=!endCap&&dd<=st.width*.5f;}else inside=InPoly(p,ow.poly);
                if(!inside)continue;if(blockers.Any(bp=>InPoly(p,bp)))continue;lab[idx]=o;
            }
        }
        // (late owner 는 ROAD 뒤 인덱스 → 아래 화단 턱 loop(o<ROAD) 에서 빠진다: 보도·계단 옆엔 턱을 만들지 않음)
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
                // 5차: 터(길 제외)는 0.6 m 까지 비탈로 받음. 계단은 원지형이 더 낮은 쪽(delta<0, 채움)을 늘 비탈로 → 담은 깎는 쪽 하나.
                bool stairO=o!=ROAD&&owners[o].name.StartsWith("stair_");float a0=o==ROAD?.45f:SlopeMax;
                float W=o!=ROAD&&owners[o].hard?a:(stairO&&delta<0)?0:Mathf.Clamp01((a-a0)/.15f)*a;float cc=e+Mathf.Sign(delta)*W;
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
                    // 7차: 진입로 위를 가로지르는 담은 안 세운다(판 북쪽 담·진입로 끝 가로 경계). 진입로 옆 경계(반폭+0.5)는 남김.
                    if(InDrive(p,owners[o].name=="drive_garage"?driveHalf-.1f:driveHalf+.1f))continue;
                    var tg=(chain[Mathf.Min(k+1,m-1)]-chain[Mathf.Max(k-1,0)]).normalized;var nrm=new Vector2(tg.y,-tg.x);
                    if(lab[g.Near(p.x+nrm.x*.3f,p.y+nrm.y*.3f)]==o)nrm=-nrm;
                    int other=lab[g.Near(p.x+nrm.x*.4f,p.y+nrm.y*.4f)];if(other>=0&&other<o)continue;
                    float fin=g.Sample(F,p.x-nrm.x*.35f,p.y-nrm.y*.35f),fout=g.Sample(F,p.x+nrm.x*.35f,p.y+nrm.y*.35f);
                    J[k]=fin-fout;if(Mathf.Abs(J[k])<(o==ROAD?WallMin:WallMinB))continue;ok[k]=true;Lo[k]=J[k]>0?nrm:-nrm;Fh[k]=Mathf.Max(fin,fout);Fl[k]=Mathf.Min(fin,fout);
                }
                // 0.8 m 보다 짧은 토막은 버린다(길가 말뚝처럼 보임)
                for(int k=0;k<m;)
                {
                    if(!ok[k]){k++;continue;}int e=k;float rl=0;while(e+1<m&&ok[e+1]&&Mathf.Sign(J[e+1])==Mathf.Sign(J[k])){rl+=(chain[e+1]-chain[e]).magnitude;e++;}
                    if(rl<.8f)for(int q=k;q<=e;q++)ok[q]=false;k=e+1;
                }
                var S=new float[m];for(int k=1;k<m;k++)S[k]=S[k-1]+(chain[k]-chain[k-1]).magnitude;
                // 4차: 이어지는 마디끼리의 이음엔 끝 마개면을 안 붙인다(옆을 보는 좁은 면이 햇빛을 받아 가는 밝은 세로 선이 됐다). 사슬 끝·끊긴 곳만 막음.
                bool joined=false;
                void Emit(int a,int b,bool cont=false)
                {
                    bool capS=!joined;joined=cont;
                    if(b<=a)return;float l2=WallSection(wv,wt,ct,wuv,chain,S,Lo,Fh,Fl,a,b,capS,!cont,ref stones,out float hgt);wallLen+=l2;wallMax=Mathf.Max(wallMax,hgt);if(hgt>TwoTier+.15f)tallLen+=l2;wallSum+=hgt*l2;sections++;
                    perOwner.TryGetValue(owners[o].name,out float pl);perOwner[owners[o].name]=pl+l2;
                }
                int s0=-1;float len=0,mn=0,mx=0;
                for(int k=0;k<m;k++)
                {
                    if(!ok[k]){if(s0>=0)Emit(s0,k-1);s0=-1;continue;}
                    if(s0<0||Mathf.Sign(J[k])!=Mathf.Sign(J[s0])){if(s0>=0)Emit(s0,k-1);s0=k;len=0;mn=mx=Fh[k];continue;}
                    len+=(chain[k]-chain[k-1]).magnitude;mn=Mathf.Min(mn,Fh[k]);mx=Mathf.Max(mx,Fh[k]);
                    if(len>=Section||mx-mn>.3f){Emit(s0,k,true);s0=k;len=0;mn=mx=Fh[k];}
                }
                if(s0>=0)Emit(s0,m-1);
            }
        }

        // --- 지형 사본: 날카로운 칸 근처만 0.38 m 까지 쪼갬(빨강-초록, T 이음 없음) 후 높이 다시 매김 ---
        var sharp=new bool[N];
        for(int j=0;j<n;j++)for(int i=0;i<n;i++){int idx=j*n+i;if(i<n-1&&Mathf.Abs(F[idx]-F[idx+1])>.2f){sharp[idx]=sharp[idx+1]=true;}if(j<n-1&&Mathf.Abs(F[idx]-F[idx+n])>.2f){sharp[idx]=sharp[idx+n]=true;}}
        // 3차: 보도·계단·밭(late/hard) 경계도 잘게 — 큰 삼각형이 길 띠 core 와 보도 높이를 잇다가 길 위로 솟지 않게
        bool Fine(int l)=>l>=0&&l!=ROAD&&(owners[l].late||owners[l].hard);
        for(int j=0;j<n;j++)for(int i=0;i<n;i++){int idx=j*n+i;int l=lab[idx];if(i<n-1&&lab[idx+1]!=l&&(Fine(l)||Fine(lab[idx+1])))sharp[idx]=sharp[idx+1]=true;if(j<n-1&&lab[idx+n]!=l&&(Fine(l)||Fine(lab[idx+n])))sharp[idx]=sharp[idx+n]=true;}
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
        // 5차: 잔디 각진 면 — 반경 안 정점 법선을 같은 자리 정점끼리 합치고, 이웃 정점 법선과 2번 섞는다(60° 넘게 꺾인 이웃은 안 섞음 → 축대 밑 단차 면은 그대로).
        int smoothedN=0;
        {
            var rn=mesh.normals;var key=new Dictionary<(int,int,int),int>();var cid=new int[V.Count];var cl=new List<Vector3>();
            for(int k=0;k<V.Count;k++){var p=V[k];if(Rad(new Vector2(p.x,p.z))>radius+Blend){cid[k]=-1;continue;}var kk=(Mathf.RoundToInt(p.x*1000),Mathf.RoundToInt(p.y*1000),Mathf.RoundToInt(p.z*1000));if(!key.TryGetValue(kk,out int c)){c=cl.Count;key[kk]=c;cl.Add(Vector3.zero);}cid[k]=c;cl[c]+=rn[k];}
            var cn=cl.Select(x=>x.normalized).ToArray();var adj=new HashSet<long>();
            for(int t=0;t<tris.Count;t+=3)for(int q=0;q<3;q++){int a=cid[tris[t+q]],b=cid[tris[t+(q+1)%3]];if(a<0||b<0||a==b)continue;adj.Add(((long)a<<32)|(uint)b);adj.Add(((long)b<<32)|(uint)a);}
            var nb=new List<int>[cn.Length];foreach(var e2 in adj){int a=(int)(e2>>32),b=(int)(e2&0xffffffff);(nb[a]??=new List<int>()).Add(b);}
            for(int pass=0;pass<2;pass++){var nx=(Vector3[])cn.Clone();for(int c=0;c<cn.Length;c++){if(nb[c]==null)continue;var s=cn[c];foreach(var o in nb[c])if(Vector3.Dot(cn[o],cn[c])>.5f)s+=cn[o]*.5f;nx[c]=s.normalized;}cn=nx;}
            for(int k=0;k<V.Count;k++){if(cid[k]<0)continue;var sN=cn[cid[k]];if(Vector3.Dot(sN,rn[k])>.5f){rn[k]=sN;smoothedN++;}}
            mesh.normals=rn;
        }
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
            ti.wrapModeU=clampU?TextureWrapMode.Clamp:TextureWrapMode.Repeat;ti.wrapModeV=TextureWrapMode.Repeat;ti.filterMode=path==StoneTexB?FilterMode.Point:FilterMode.Bilinear;ti.mipmapEnabled=true;ti.sRGBTexture=true;ti.textureCompression=TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        // 5차: 축대 = B 방식 한 장(docs/qa/tmp/terrace5/make_wall_tex.py), 틴트 흰색(팔레트 색 그대로) · 갓돌 띠 = 별도 재질 CapB(연석은 옛 CapMat 그대로)
        var stoneTex=Tex(StoneTexB,false);var edgeTex=Tex(PathEdgeTex,true);
        var stone=AssetDatabase.LoadAssetAtPath<Material>(StoneMat);var st0=stone.HasProperty("_Tint")?stone.GetColor("_Tint"):Color.white;
        var retain=CopyMat(StoneMat,RetainMat,new Color(1f,1f,1f,st0.a));retain.SetTexture("_MainTex",stoneTex);retain.SetTextureScale("_MainTex",new Vector2(1/StoneBU,1/StoneBV));EditorUtility.SetDirty(retain);
        var cap=CopyMat(StoneMat,CapMat,new Color(.86f,.83f,.76f,st0.a));
        var capB=CopyMat(StoneMat,CapBMat,new Color(CapB.r,CapB.g,CapB.b,st0.a));
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
                    for(int q=0;q<PC;q++){float u=q/(float)(PC-1);var p=pts[k]+nn*((u*2-1)*YardPathHalf);float y=tcol.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out var hit,1000)?hit.point.y:g.Sample(F,p.x,p.y);pv.Add(new Vector3(p.x,y+.03f,p.y));puv.Add(new Vector2(u,along/4f));}
                }
                for(int k=0;k<pts.Count-1;k++)for(int q=0;q<PC-1;q++){int a=k*PC+q,b=a+1,c=a+PC,d=a+PC+1;var tri=new[]{a,c,b,b,c,d};var nrm=Vector3.Cross(pv[c]-pv[a],pv[b]-pv[a]);if(nrm.y<0)tri=new[]{a,b,c,b,d,c};pt.AddRange(tri);}
            }finally{UnityEngine.Object.DestroyImmediate(tgo);}
        }
        var pmesh=new Mesh{name="Mesh_TerraceBlock_path"};pmesh.SetVertices(pv);pmesh.SetUVs(0,puv);pmesh.SetTriangles(pt,0);pmesh.RecalculateNormals();pmesh.RecalculateBounds();
        if(AssetDatabase.LoadAssetAtPath<Mesh>(PathAsset))AssetDatabase.DeleteAsset(PathAsset);AssetDatabase.CreateAsset(pmesh,PathAsset);
        // --- 3차: 사람 길 메시. 서브메시 0 = 포장(보도 윗면·뒤 치마·계단 디딤), 1 = 연석(밝은 갓돌 색) ---
        var av=new List<Vector3>();var at=new List<int>();var ac=new List<int>();var auv=new List<Vector2>();float walkLen=0,walkArea=0;var walkNote=new List<string>();
        Vector3 W3(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
        foreach(var wk in walks)
        {
            int m=wk.e.Count;float len=0;
            for(int k=0;k<m-1;k++)
            {
                float y0=wk.y[k]+Curb,y1=wk.y[k+1]+Curb;var e0=wk.e[k];var e1=wk.e[k+1];var n0=wk.n[k];var n1=wk.n[k+1];var up=Vector3.up;
                var i0=e0+n0*CurbW;var i1=e1+n1*CurbW;var o0=e0+n0*(CurbW+wk.w[k]);var o1=e1+n1*(CurbW+wk.w[k+1]);float sl=(e1-e0).magnitude;len+=sl;walkArea+=sl*(wk.w[k]+wk.w[k+1])*.5f;
                Quad(av,at,auv,W3(i0,y0),W3(i1,y1),W3(o1,y1),W3(o0,y0),up);                                                      // 보도 윗면
                Quad(av,at,auv,W3(o0,y0),W3(o1,y1),W3(o1,y1-.45f),W3(o0,y0-.45f),W3((n0+n1)*.5f,0));                           // 뒤 치마
                var f0=e0-n0*.02f;var f1=e1-n1*.02f;
                Quad(av,ac,auv,W3(f0,y0),W3(f1,y1),W3(i1,y1),W3(i0,y0),up);                                                       // 연석 윗면
                Quad(av,ac,auv,W3(f0,wk.y[k]-.12f),W3(f1,wk.y[k+1]-.12f),W3(f1,y1),W3(f0,y0),W3(-(n0+n1)*.5f,0));               // 연석 앞면(차길 쪽)
            }
            foreach(var (k,dir) in new[]{(0,wk.e[0]-wk.e[1]),(m-1,wk.e[m-1]-wk.e[m-2])})                                         // 끝 막음
            {
                float y=wk.y[k]+Curb;var e0=wk.e[k]-wk.n[k]*.02f;var o0=wk.e[k]+wk.n[k]*(CurbW+wk.w[k]);
                Quad(av,at,auv,W3(e0,y),W3(o0,y),W3(o0,wk.y[k]-.45f),W3(e0,wk.y[k]-.12f),W3(dir.normalized,0));
            }
            walkLen+=len;walkNote.Add($"{{\"name\":\"{wk.name}\",\"lengthM\":{len:F1},\"wMin\":{wk.w.Min():F2},\"wMax\":"+wk.w.Max().ToString("F2")+"}");
        }
        // 5차: 계단 = 디딤(윗면, 포장) + 챌판·옆면(서브메시 2, 더 어두운 재질). 멀리서 경사 판이 아니라 줄무늬 단으로 읽히게. 디딤 코 2 cm 내밂.
        var ar=new List<int>();
        foreach(var st in stairs)
        {
            var pts=new List<Vector2>();var ss=new List<float>();
            for(int i=0;i<=st.N;i++){float s=Mathf.Min(st.L,i*st.t);ss.Add(s);pts.Add(PolyAt(st.line,s));}
            for(int i=0;i<st.N;i++)
            {
                float yTop=st.hl+(i+1)*st.r,yBot=st.hl+ss[i]/st.L*(st.hh-st.hl)-.05f-.3f;
                var p0=pts[i];var p1=pts[i+1];var d=(p1-p0).normalized;if(i>0)p0-=d*.03f;if(i==st.N-1)p1+=d*.25f;   // 디딤끼리 3 cm 겹침, 마지막 디딤은 위 땅으로 0.25 m 더
                int t0=at.Count;Box(av,at,auv,p0-d*.02f,p1,st.width,yBot,yTop);
                // Box 면 순서: 윗면 · 옆 둘 · 앞(p0 쪽 = 챌판) · 뒤 → 윗면(6 인덱스)만 디딤, 나머지는 챌판 서브메시로
                ar.AddRange(at.GetRange(t0+6,at.Count-t0-6));at.RemoveRange(t0+6,at.Count-t0-6);
            }
            // 계단 양옆 풀 덤불 자리(디딤 가장자리 밖 0.1 m, 땅 높이)
            for(float s=.3f;s<st.L-.3f;s+=.35f){var c=PolyAt(st.line,s);var dd=(PolyAt(st.line,Mathf.Min(st.L,s+.2f))-PolyAt(st.line,Mathf.Max(0,s-.2f))).normalized;var nn=new Vector2(-dd.y,dd.x);
                foreach(int sd in new[]{1,-1}){var q=c+nn*sd*(st.width*.5f+.1f);Foot.Add((new Vector3(q.x,g.Sample(F,q.x,q.y),q.y),nn*sd,.8f));}}
        }
        // 6차: 차고 골목 포장 판(윗면 = 차고 바닥 +3 cm, 0.3 m 두께) — 차고 앞 마당 남쪽 가장자리까지 이어져 「사람 다니는 띠」가 된다
        if(alleyPave!=null)Box(av,at,auv,(alleyPave[0]+alleyPave[1])*.5f,(alleyPave[2]+alleyPave[3])*.5f,AlleyPave1-AlleyPave0,alleyH-.3f,alleyH+.03f);
        // 포장 UV: 윗면 = 월드 xz / 1.6 m, 옆면 = (가로, 높이) / 1.6 m
        var amesh=new Mesh{name="Mesh_TerraceBlock_walk",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=3};amesh.SetVertices(av);amesh.SetTriangles(at,0);amesh.SetTriangles(ac,1);amesh.SetTriangles(ar,2);amesh.RecalculateNormals();
        {var an=amesh.normals;for(int k=0;k<av.Count;k++){var p=av[k];var q=an[k];auv[k]=Mathf.Abs(q.y)>.5f?new Vector2(p.x,p.z)/PaveTile:(Mathf.Abs(q.x)>Mathf.Abs(q.z)?new Vector2(p.z,p.y):new Vector2(p.x,p.y))/PaveTile;}}
        amesh.SetUVs(0,auv);amesh.RecalculateBounds();
        if(AssetDatabase.LoadAssetAtPath<Mesh>(WalkAsset))AssetDatabase.DeleteAsset(WalkAsset);AssetDatabase.CreateAsset(amesh,WalkAsset);
        var paveTex=Tex(PaveTex,false);var pave=CopyMat(StoneMat,PaveMat,new Color(1f,.98f,.94f,st0.a));pave.SetTexture("_MainTex",paveTex);pave.SetTextureScale("_MainTex",Vector2.one);EditorUtility.SetDirty(pave);
        var riser=CopyMat(StoneMat,RiserMat,new Color(.70f,.66f,.60f,st0.a));riser.SetTexture("_MainTex",paveTex);riser.SetTextureScale("_MainTex",Vector2.one);EditorUtility.SetDirty(riser);
        // 5차 풀 덤불: 벽 밑동·윗단 뒤·계단 옆 0.3~0.4 m 마다 낮은 덩어리(납작 반타원체, 링 3·옆 7). 크기·색 2종 무작위, 채도 낮은 풀색.
        var tv=new List<Vector3>();var tA=new List<int>();var tB=new List<int>();int tufts=0;
        {
            int seed=777;float R(){seed=(seed*1103515245+12345)&0x7fffffff;return (seed%10000)/10000f;}
            Vector3 last=new Vector3(1e9f,0,0);
            foreach(var (p,nd,sc) in Foot)
            {
                if((p-last).sqrMagnitude<.3f*.3f*sc)continue;if(R()<.18f){last=p;continue;}last=p;   // 18% 빈 자리
                float rad=(.16f+R()*.2f)*sc,h=(.10f+R()*.18f)*sc;var ctr=p+new Vector3(nd.x,0,nd.y)*rad*.35f+new Vector3((R()-.5f)*.1f,-.03f,(R()-.5f)*.1f);
                int c0=tv.Count;const int RG=3,SD=7;float rot=R()*6.28f;var list=R()<.5f?tA:tB;
                for(int i=0;i<=RG;i++){float ph=i/(float)RG*Mathf.PI*.5f;for(int j=0;j<SD;j++){float th=j/(float)SD*Mathf.PI*2+rot,jn=1+(R()-.5f)*.35f;
                    tv.Add(ctr+new Vector3(Mathf.Cos(ph)*Mathf.Cos(th)*rad*jn,Mathf.Sin(ph)*h*(i==RG?1:jn),Mathf.Cos(ph)*Mathf.Sin(th)*rad*jn*.8f));}}
                for(int i=0;i<RG;i++)for(int j=0;j<SD;j++){int a=c0+i*SD+j,b=c0+i*SD+(j+1)%SD,c=a+SD,d=b+SD;var nn2=Vector3.Cross(tv[c]-tv[a],tv[b]-tv[a]);var o=(tv[a]+tv[d])*.5f-ctr;
                    if(Vector3.Dot(nn2,o)>=0)list.AddRange(new[]{a,c,b,b,c,d});else list.AddRange(new[]{a,b,c,b,d,c});}
                tufts++;
            }
        }
        var tmesh=new Mesh{name="Mesh_TerraceBlock_tufts",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=2};tmesh.SetVertices(tv);tmesh.SetTriangles(tA,0);tmesh.SetTriangles(tB,1);tmesh.RecalculateNormals();tmesh.RecalculateBounds();
        if(AssetDatabase.LoadAssetAtPath<Mesh>(TuftAsset))AssetDatabase.DeleteAsset(TuftAsset);AssetDatabase.CreateAsset(tmesh,TuftAsset);
        var tuftMats=new[]{CopyMat(StoneMat,Folder+"TV6_TuftA.mat",new Color(.50f,.55f,.34f,st0.a)),CopyMat(StoneMat,Folder+"TV6_TuftB.mat",new Color(.43f,.48f,.30f,st0.a))};
        var wmesh=new Mesh{name="Mesh_TerraceBlock_walls",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=2};wmesh.SetVertices(wv);wmesh.SetUVs(0,wuv);wmesh.SetTriangles(wt,0);wmesh.SetTriangles(ct,1);wmesh.RecalculateNormals();wmesh.RecalculateBounds();
        if(AssetDatabase.LoadAssetAtPath<Mesh>(WallAsset))AssetDatabase.DeleteAsset(WallAsset);AssetDatabase.CreateAsset(wmesh,WallAsset);
        // 7차: 진입로 메시. 서브메시 0 = 윗면(마을길 재질, UV = 월드 xz), 1 = 양옆 치마(축대 돌, UV = 길이·높이 m) — 판 위에선 둑의 옆면, 판 밖에선 땅에 묻힘.
        Mesh dmesh=null;
        if(driveC!=null)
        {
            var dv=new List<Vector3>();var dt=new List<int>();var dk=new List<int>();var duv=new List<Vector2>();float along=0;
            for(int k=0;k<driveC.Count-1;k++)
            {
                var n0=new Vector2(-driveD[k].y,driveD[k].x)*driveHalf;var n1=new Vector2(-driveD[k+1].y,driveD[k+1].x)*driveHalf;float y0=driveY[k],y1=driveY[k+1];
                var l0=driveC[k]+n0;var r0=driveC[k]-n0;var l1=driveC[k+1]+n1;var r1=driveC[k+1]-n1;float sl=(driveC[k+1]-driveC[k]).magnitude;
                Quad(dv,dt,duv,W3(l0,y0),W3(l1,y1),W3(r1,y1),W3(r0,y0),Vector3.up,new Vector2(l0.x,l0.y),new Vector2(l1.x,l1.y),new Vector2(r1.x,r1.y),new Vector2(r0.x,r0.y));
                foreach(var (a,b,o) in new[]{(l0,l1,n0+n1),(r0,r1,-(n0+n1))})
                    Quad(dv,dk,duv,W3(a,y0),W3(b,y1),W3(b,DriveBot),W3(a,DriveBot),W3(o,0),new Vector2(along,y0-DriveBot),new Vector2(along+sl,y1-DriveBot),new Vector2(along+sl,0),new Vector2(along,0));
                along+=sl;
            }
            dmesh=new Mesh{name="Mesh_TerraceBlock_drive",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=2};dmesh.SetVertices(dv);dmesh.SetUVs(0,duv);dmesh.SetTriangles(dt,0);dmesh.SetTriangles(dk,1);dmesh.RecalculateNormals();dmesh.RecalculateBounds();
            if(AssetDatabase.LoadAssetAtPath<Mesh>(DriveAsset))AssetDatabase.DeleteAsset(DriveAsset);AssetDatabase.CreateAsset(dmesh,DriveAsset);
        }
        AssetDatabase.SaveAssets();

        // --- 씬에 꽂기 ---
        var dst=AssetDatabase.LoadAssetAtPath<Mesh>(DstMesh);terr.GetComponent<MeshFilter>().sharedMesh=dst;var tmc=terr.GetComponent<MeshCollider>();if(tmc)tmc.sharedMesh=dst;
        var root=new GameObject(RootName);root.transform.SetParent(top,false);
        var wg=new GameObject("RetainingWalls");wg.transform.SetParent(root.transform,false);wg.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(WallAsset);wg.AddComponent<MeshRenderer>().sharedMaterials=new[]{retain,capB};wg.AddComponent<MeshCollider>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(WallAsset);
        var pg=new GameObject("DirtPath_door_garden");pg.transform.SetParent(root.transform,false);pg.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(PathAsset);pg.AddComponent<MeshRenderer>().sharedMaterial=pathMat;
        // 4차 텃밭: 흙판·이랑 4줄·작물(낮은 잎 2줄/높은 잎 2줄)·낮은 나무 말뚝 둘레. 단색 씬 전용 재질(밭 목업 field_build 색).
        var gmesh=BuildGarden(hpos,hyaw,hHouse,out string gardenNote);
        if(AssetDatabase.LoadAssetAtPath<Mesh>(GardenAsset))AssetDatabase.DeleteAsset(GardenAsset);AssetDatabase.CreateAsset(gmesh,GardenAsset);AssetDatabase.SaveAssets();
        var gmats=new[]{("Soil",new Color(.60f,.50f,.37f)),("Ridge",new Color(.64f,.54f,.40f)),("CropLow",new Color(.42f,.50f,.31f)),("CropTall",new Color(.47f,.55f,.34f)),("Wood",new Color(.46f,.41f,.34f))}
            .Select(c=>CopyMat(StoneMat,Folder+"TV6_Garden"+c.Item1+".mat",new Color(c.Item2.r,c.Item2.g,c.Item2.b,st0.a))).ToArray();
        // 5차(도현 8b98 "밭 모델링·질감 안 맞음" → 아트 field_patch 정식 에셋으로 따로, artlive-20261030-2003): 임시 텃밭 메시는 씬에 안 꽂는다. 마당 안 평평한 칸만 남김.
        if(PlaceGarden){var gg=new GameObject("Garden");gg.transform.SetParent(root.transform,false);gg.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(GardenAsset);gg.AddComponent<MeshRenderer>().sharedMaterials=gmats;}
        var ag=new GameObject("Walkways");ag.transform.SetParent(root.transform,false);ag.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(WalkAsset);ag.AddComponent<MeshRenderer>().sharedMaterials=new[]{pave,cap,riser};ag.AddComponent<MeshCollider>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(WalkAsset);
        if(dmesh!=null)
        {
            // 7차: 진입로 = 차가 타는 면(MeshCollider) + RoadSurface(마을길, 1 m 중심선) → TownPathBot 이 길로 잡는다
            var dg=new GameObject("Driveway");dg.transform.SetParent(root.transform,false);dg.AddComponent<MeshFilter>().sharedMesh=dmesh;
            dg.AddComponent<MeshRenderer>().sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>(Folder+"TV6_Village.mat"),retain};dg.AddComponent<MeshCollider>().sharedMesh=dmesh;
            var rs=dg.AddComponent<RoadSurface>();rs.roadId="drive_garage";rs.kind="village";rs.width=DriveW;
            float Lt=0;for(int k=1;k<driveC.Count;k++)Lt+=(driveC[k]-driveC[k-1]).magnitude;int nc=Mathf.Max(2,Mathf.RoundToInt(Lt)+1);
            rs.centerline=new Vector3[nc];for(int k=0;k<nc;k++){float s=Lt*k/(nc-1);var p=PolyAt(driveC,s);float t=s/Lt*(driveC.Count-1);int a=Mathf.Min(Mathf.FloorToInt(t),driveC.Count-2);rs.centerline[k]=W3(p,Mathf.Lerp(driveY[a],driveY[a+1],t-a));}
        }
        var tuftGo=new GameObject("GrassTufts");tuftGo.transform.SetParent(root.transform,false);tuftGo.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(TuftAsset);tuftGo.AddComponent<MeshRenderer>().sharedMaterials=tuftMats;
        var scene=terr.gameObject.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);

        // --- 기록(JSON) ---
        js.Append("{\"build\":{\"center\":["+center.x+","+center.z+"],\"radius\":"+radius+",\"blend\":"+Blend+",\"ms\":"+sw.ElapsedMilliseconds+",");
        js.Append("\"owners\":["+string.Join(",",owners.Select(o=>"{\"name\":\""+o.name+"\",\"h\":"+(o.road?"null":o.H(o.poly!=null?o.poly[0]:Vector2.zero).ToString("F2"))+",\"cells\":"+o.cells+"}"))+"],");
        js.Append("\"skipped\":["+string.Join(",",skipped.Select(s=>"\""+s+"\""))+"],\"roadMeshes\":"+roadMeshes+",");
        string f2(float x)=>x.ToString("F2");
        js.Append("\"terrain\":{\"vertsBefore\":"+origCount+",\"vertsAfter\":"+V.Count+",\"refinePasses\":"+passes+",\"changed\":"+changedV+",\"keptRoadZone\":"+keptRoad+",\"tJunctionsStitched\":"+tFixed+",\"maxCut\":"+f2(maxCut)+",\"maxFill\":"+f2(maxFill)+"},");
        js.Append("\"walls\":{\"sections\":"+sections+",\"lengthM\":"+wallLen.ToString("F1")+",\"maxH\":"+f2(wallMax)+",\"over2_65mLen\":"+tallLen.ToString("F1")+",\"meanH\":"+f2(wallLen>0?wallSum/wallLen:0)+",\"copingStones\":"+stones+",\"verts\":"+wv.Count+",\"byOwner\":{"+string.Join(",",perOwner.Select(kv=>"\""+kv.Key+"\":"+kv.Value.ToString("F1")))+"}},");
        js.Append("\"alleyGarage\":"+alleyNote+",\"driveGarage\":"+driveNote+",");
        js.Append("\"ledges\":["+string.Join(",",ledgeInfo)+"],\"tufts\":"+tufts+",\"footPts\":"+Foot.Count+",\"smoothedNormals\":"+smoothedN+",\"risersTri\":"+ar.Count/3+",");
        js.Append("\"path\":{\"lengthM\":"+pathLen.ToString("F1")+",\"halfW\":"+YardPathHalf+",\"h\":"+f2(hHouse)+"},");
        js.Append("\"garden\":{"+gardenNote+",\"h\":"+f2(hHouse)+"},\"chains\":["+string.Join(",",chains.Select(c=>"\""+c.name+"\""))+"],");
        js.Append("\"walk\":{\"curb\":"+Curb+",\"lengthM\":"+walkLen.ToString("F1")+",\"areaM2\":"+walkArea.ToString("F1")+",\"runs\":["+string.Join(",",walkNote)+"]},\"stairs\":["+string.Join(",",stairNote)+"]}}");
        return js.ToString();
    }

    // 4차 텃밭 메시(집 뿌리 좌표 → 월드). 서브메시 0 흙판 · 1 이랑 · 2 낮은 잎 · 3 높은 잎 · 4 나무 말뚝. 밭 목업(tools/art/bin/field_build.cs) 치수 그대로:
    //   이랑 폭 0.56·높이 0.19 반원통(가로 8·길이 16 조각, 끝 0.3 m 에서 낮아짐) / 작물 0.38×(0.19|0.40)×0.39 울퉁 타원체(링 6·옆 12) / 말뚝 반지름 0.048, 여기선 낮게 0.38 m.
    static Mesh BuildGarden(Vector3 hpos,float hyaw,float y0,out string note)
    {
        var q=Quaternion.Euler(0,hyaw,0);Vector3 W(float lx,float y,float lz)=>hpos+q*new Vector3(lx,0,lz)+Vector3.up*(y-hpos.y);
        var v=new List<Vector3>();var subs=new[]{new List<int>(),new List<int>(),new List<int>(),new List<int>(),new List<int>()};
        int seed=12345;float R(){seed=(seed*1103515245+12345)&0x7fffffff;return (seed%10000)/10000f;}
        float cx=(GX0+GX1)*.5f,cz=(GZ0+GZ1)*.5f,ax=(GX1-GX0)*.5f,az=(GZ1-GZ0)*.5f;
        // 흙판: 초타원(p=4) 둘레 48점, 가장자리 ±6 cm 울퉁, 마당 위 +2.5 cm
        {
            int c0=v.Count;v.Add(W(cx,y0+.025f,cz));
            for(int k=0;k<48;k++){float t=k*Mathf.PI*2/48,cs=Mathf.Cos(t),sn=Mathf.Sin(t),j=1+(R()-.5f)*.04f;v.Add(W(cx+ax*j*Mathf.Sign(cs)*Mathf.Pow(Mathf.Abs(cs),.5f),y0+.025f,cz+az*j*Mathf.Sign(sn)*Mathf.Pow(Mathf.Abs(sn),.5f)));}
            for(int k=0;k<48;k++){int a=c0+1+k,b=c0+1+(k+1)%48;var nn=Vector3.Cross(v[a]-v[c0],v[b]-v[c0]);if(nn.y>0)subs[0].AddRange(new[]{c0,a,b});else subs[0].AddRange(new[]{c0,b,a});}
        }
        // 이랑 4줄: 길이 방향 = 뿌리 x, 줄 간격 = 판 폭/4
        int ridges=4,crops=0,stakes=0;float rl0=GX0+.55f,rl1=GX1-.55f,rw=.56f,rh=.19f;
        var tops=new List<(float z,int row)>();
        for(int r=0;r<ridges;r++)
        {
            float z=GZ0+(r+.5f)*(GZ1-GZ0)/ridges;tops.Add((z,r));int c0=v.Count;const int A=16,B=8;
            for(int i=0;i<=A;i++){float s=i/(float)A,x=Mathf.Lerp(rl0,rl1,s),d=Mathf.Min(x-rl0,rl1-x),taper=Mathf.SmoothStep(0,1,Mathf.Clamp01(d/.3f)),wob=Mathf.Sin(s*7+r)*.03f;
                for(int jj=0;jj<=B;jj++){float a=jj/(float)B*Mathf.PI;v.Add(W(x,y0+.03f+Mathf.Sin(a)*rh*(.25f+.75f*taper),z+wob+Mathf.Cos(a)*rw*.5f*(.6f+.4f*taper)));}}
            for(int i=0;i<A;i++)for(int jj=0;jj<B;jj++){int a=c0+i*(B+1)+jj,b=a+1,c=a+B+1,d=c+1;var nn=Vector3.Cross(v[c]-v[a],v[b]-v[a]);if(nn.y>=0)subs[1].AddRange(new[]{a,c,b,b,c,d});else subs[1].AddRange(new[]{a,b,c,b,d,c});}
        }
        // 작물: 이랑마다 0.45 m 간격, 앞 두 줄 낮은 잎(0.19) · 뒤 두 줄 높은 잎(0.40)
        foreach(var (z,row) in tops)
        {
            bool tall=row>=2;float sy=tall?.40f:.19f;
            for(float x=rl0+.3f;x<=rl1-.25f;x+=.45f)
            {
                var ctr=W(x+(R()-.5f)*.06f,y0+.03f+rh+sy*.38f,z+(R()-.5f)*.05f);int c0=v.Count;const int RG=6,SD=12;float rot=R()*6.28f;
                for(int i=0;i<=RG;i++){float ph=i/(float)RG*Mathf.PI;for(int jj=0;jj<SD;jj++){float th=jj/(float)SD*Mathf.PI*2+rot,n=1+(R()-.5f)*.22f;
                    v.Add(ctr+q*new Vector3(Mathf.Sin(ph)*Mathf.Cos(th)*.19f*n,-Mathf.Cos(ph)*sy*.5f*n,Mathf.Sin(ph)*Mathf.Sin(th)*.195f*n));}}
                for(int i=0;i<RG;i++)for(int jj=0;jj<SD;jj++){int a=c0+i*SD+jj,b=c0+i*SD+(jj+1)%SD,c=a+SD,d=b+SD;var nn=Vector3.Cross(v[c]-v[a],v[b]-v[a]);var o=(v[a]+v[d])*.5f-ctr;
                    if(Vector3.Dot(nn,o)>=0)subs[tall?3:2].AddRange(new[]{a,c,b,b,c,d});else subs[tall?3:2].AddRange(new[]{a,b,c,b,d,c});}
                crops++;
            }
        }
        // 말뚝 둘레: 판 밖 0.25 m 사각 둘레에 0.6 m 간격, 흙길 들어오는 서쪽(GX1 쪽) GateZ ±0.6 m 는 비움
        {
            float x0=GX0-.25f,x1=GX1+.25f,z0=GZ0-.25f,z1=GZ1+.25f;var ring=new List<Vector2>{new Vector2(x0,z0),new Vector2(x1,z0),new Vector2(x1,z1),new Vector2(x0,z1)};
            for(int e=0;e<4;e++)
            {
                var a=ring[e];var b=ring[(e+1)%4];int cnt=Mathf.Max(1,Mathf.RoundToInt((b-a).magnitude/.6f));
                for(int k=0;k<cnt;k++)
                {
                    var p=Vector2.Lerp(a,b,k/(float)cnt);if(Mathf.Abs(p.x-x1)<.01f&&Mathf.Abs(p.y-GateZ)<.6f)continue;
                    float h=.38f+(R()-.5f)*.08f,rad=.048f;int c0=v.Count;const int S=8;
                    for(int jj=0;jj<S;jj++){float th=jj*Mathf.PI*2/S;var o=new Vector3(Mathf.Cos(th)*rad,0,Mathf.Sin(th)*rad);v.Add(W(p.x,y0-.05f,p.y)+o);v.Add(W(p.x,y0+h,p.y)+o);}
                    v.Add(W(p.x,y0+h+.02f,p.y));int top=v.Count-1;var axis=W(p.x,y0,p.y);
                    for(int jj=0;jj<S;jj++){int a0=c0+jj*2,a1=a0+1,b0=c0+((jj+1)%S)*2,b1=b0+1;var nn=Vector3.Cross(v[a1]-v[a0],v[b0]-v[a0]);var o=v[a0]-axis;o.y=0;
                        if(Vector3.Dot(nn,o)>=0)subs[4].AddRange(new[]{a0,a1,b0,b0,a1,b1});else subs[4].AddRange(new[]{a0,b0,a1,b0,b1,a1});
                        var tn=Vector3.Cross(v[b1]-v[a1],v[top]-v[a1]);if(tn.y>=0)subs[4].AddRange(new[]{a1,b1,top});else subs[4].AddRange(new[]{a1,top,b1});}
                    stakes++;
                }
            }
        }
        var m=new Mesh{name="Mesh_TerraceBlock_garden",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=5};m.SetVertices(v);for(int s=0;s<5;s++)m.SetTriangles(subs[s],s);m.RecalculateNormals();m.RecalculateBounds();
        note=$"\"local\":[{GX0},{GX1},{GZ0},{GZ1}],\"sizeM\":[{GX1-GX0:F1},{GZ1-GZ0:F1}],\"ridges\":{ridges},\"crops\":{crops},\"stakes\":{stakes},\"verts\":{v.Count}";
        return m;
    }

    // 한 마디(s0..e) 축대: 앞면은 낮은 쪽으로 약간 눕힘, 윗면 평평(0.1 m 올림), 갓돌 한 줄. 반환 = 길이.
    // 2차: UV = (마디 줄 따라 잰 거리 m, 높이 m) → 재질 타일 2.5×1.88 m 로 돌 0.3~0.5 m. 마디 양 끝을 줄 방향으로 0.15 m 늘려 이웃 마디와 겹침(꺾인 모서리 검은 틈).
    static float WallSection(List<Vector3> v,List<int> wt,List<int> ct,List<Vector2> uv,List<Vector2> Pc,float[] S,Vector2[] L,float[] Fh,float[] Fl,int s0,int e,bool capS,bool capE,ref int stones,out float height)
    {
        // 5차: 윗면 높이를 마디 하나(올림 0.1 m)가 아니라 점마다 — 앞뒤 2점 평균 +0.1. 계단 옆 담의 톱니(마디마다 한 칸씩 올라감)가 매끈한 비탈 윗선이 된다.
        //   이어지는 마디는 이음점 k 가 같아 같은 높이 → 갓돌 띠도 끊김 없이 이어진다.
        int mm=Fh.Length;
        float TY(int k){float s=0;int c=0;for(int j=Mathf.Max(0,k-2);j<=Mathf.Min(mm-1,k+2);j++)if(Fh[j]>0){s+=Fh[j];c++;}return Mathf.Max(s/Mathf.Max(1,c)+.1f,Fh[k]+.05f);}
        float minL=float.MaxValue,maxT=float.MinValue;for(int k=s0;k<=e;k++){minL=Mathf.Min(minL,Fl[k]);maxT=Mathf.Max(maxT,TY(k));}
        height=maxT-minL;
        // 4차: 앞면 아래 끝(높이·눕힘)을 마디가 아니라 점마다 정한다 → 이어지는 두 마디가 이음점에서 같은 아래 정점을 쓴다.
        //  (3차까지는 마디마다 눕힘이 달라 0.15 m 겹친 두 앞면이 비스듬히 교차 → 가는 밝은 세로 선)
        float YB(int k)=>Fl[k]-.3f;float BT(int k)=>Thick*.5f+Batter*(TY(k)-YB(k));float YK(int k)=>Fh[k]-.3f;
        Vector3 V3(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
        // 끝 겹침: 사슬 끝·끊긴 곳(cap)만. 이어지는 이음은 겹치지 않고 점을 공유.
        var P=new Dictionary<int,Vector2>();for(int k=s0;k<=e;k++)P[k]=Pc[k];
        float oS=capS?SecOverlap:0,oE=capE?SecOverlap:0;
        P[s0]=Pc[s0]+(Pc[s0]-Pc[s0+1]).normalized*oS;P[e]=Pc[e]+(Pc[e]-Pc[e-1]).normalized*oE;
        float U(int k)=>k==s0?S[k]-oS:k==e?S[k]+oE:S[k];
        // 5차 UV: v = 그 점 앞면 밑동(YB)부터 잰 높이 m → 텍스처 아래 0.3~0.8 m 흙빛 띠가 늘 땅 언저리에 온다
        Vector2 uvW(int k,float y)=>new Vector2(U(k),y-YB(k));
        float len=0;
        for(int k=s0;k<e;k++)
        {
            var p0=P[k];var p1=P[k+1];var l0=L[k];var l1=L[k+1];len+=(Pc[k+1]-Pc[k]).magnitude;var hint=V3((l0+l1)*.5f,0);float t0=TY(k),t1=TY(k+1);
            Quad(v,wt,uv,V3(p0+l0*Thick*.5f,t0),V3(p1+l1*Thick*.5f,t1),V3(p1+l1*BT(k+1),YB(k+1)),V3(p0+l0*BT(k),YB(k)),hint+Vector3.up*.1f,uvW(k,t0),uvW(k+1,t1),uvW(k+1,YB(k+1)),uvW(k,YB(k)));
            Quad(v,wt,uv,V3(p0-l0*Thick*.5f,t0),V3(p1-l1*Thick*.5f,t1),V3(p1+l1*Thick*.5f,t1),V3(p0+l0*Thick*.5f,t0),Vector3.up,uvW(k,YB(k)+1),uvW(k+1,YB(k+1)+1),uvW(k+1,YB(k+1)+1+Thick),uvW(k,YB(k)+1+Thick));
            Quad(v,wt,uv,V3(p0-l0*Thick*.5f,YK(k)),V3(p1-l1*Thick*.5f,YK(k+1)),V3(p1-l1*Thick*.5f,t1),V3(p0-l0*Thick*.5f,t0),-hint,uvW(k,YK(k)),uvW(k+1,YK(k+1)),uvW(k+1,t1),uvW(k,t0));
        }
        // 끝 마개: 사슬 끝은 통째로, 이어지는 이음은 윗단 0.45 m 만(이웃 마디 윗면이 최대 ~0.4 m 낮을 때 드러나는 옆구리만 막음)
        foreach(var (k,dir,cap) in new[]{(s0,Pc[s0]-Pc[s0+1],capS),(e,Pc[e]-Pc[e-1],capE)})
        {
            float yT=TY(k),yb=cap?YB(k):Mathf.Max(YB(k),yT-.45f),yk=cap?YK(k):yb,bt=Thick*.5f+Batter*(yT-yb);
            Quad(v,wt,uv,V3(P[k]-L[k]*Thick*.5f,yk),V3(P[k]-L[k]*Thick*.5f,yT),V3(P[k]+L[k]*Thick*.5f,yT),V3(P[k]+L[k]*(cap?BT(k):bt),yb),V3(dir.normalized,0),
                new Vector2(0,yk-YB(k)),new Vector2(0,yT-YB(k)),new Vector2(Thick,yT-YB(k)),new Vector2((cap?BT(k):bt)+Thick*.5f,yb-YB(k)));
        }
        // 5차 갓돌: 따로 놓인 돌(이빨) → 이어진 띠 하나. 폭 0.56·두께 0.12, 점마다 벽 윗선을 따라감. 끝(사슬 끝·끊긴 곳)만 막음.
        {
            Vector3 C(int k,float side,float y)=>V3(P[k]+L[k]*(CapW*.5f*side),y);
            for(int k=s0;k<e;k++)
            {
                float a=TY(k),b=TY(k+1);var hint=V3((L[k]+L[k+1])*.5f,0);
                Quad(v,ct,uv,C(k,-1,a+CapH),C(k+1,-1,b+CapH),C(k+1,1,b+CapH),C(k,1,a+CapH),Vector3.up);
                Quad(v,ct,uv,C(k,1,a+CapH),C(k+1,1,b+CapH),C(k+1,1,b-.03f),C(k,1,a-.03f),hint);
                Quad(v,ct,uv,C(k,-1,a+CapH),C(k+1,-1,b+CapH),C(k+1,-1,b-.03f),C(k,-1,a-.03f),-hint);
            }
            foreach(var (k,dir,capEnd) in new[]{(s0,Pc[s0]-Pc[s0+1],capS),(e,Pc[e]-Pc[e-1],capE)})
            {
                if(!capEnd)continue;float a=TY(k);
                Quad(v,ct,uv,C(k,-1,a-.03f),C(k,-1,a+CapH),C(k,1,a+CapH),C(k,1,a-.03f),V3(dir.normalized,0));
            }
            stones++;
        }
        // 풀 덤불 자리: 앞면이 땅(Fl)과 만나는 선(바깥 법선 L) · 윗단 뒤(높은 쪽 땅 Fh, 작은 덤불)
        for(int k=s0;k<=e;k++)
        {
            float off=Thick*.5f+Batter*(TY(k)-Fl[k]);Foot.Add((V3(P[k]+L[k]*off,Fl[k]),L[k],1f));
            Foot.Add((V3(P[k]-L[k]*(CapW*.5f),Fh[k]),-L[k],.55f));
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
