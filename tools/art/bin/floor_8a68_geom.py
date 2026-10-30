"""Studio horizontal floor overlap sensor. Pure geometry; no scene edits."""
import itertools

def cross(a,b,p):
    return (b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0])

def area(p):
    return abs(sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(p,p[1:]+p[:1])))/2

def overlap(a,b):
    if cross(b[0],b[1],b[2])<0: b=b[::-1]
    for v,w in zip(b,b[1:]+b[:1]):
        out=[]
        for p,q in zip(a,a[1:]+a[:1]):
            dp,dq=cross(v,w,p),cross(v,w,q)
            if dp>=0: out.append(p)
            if (dp>=0)!=(dq>=0):
                t=dp/(dp-dq);out.append(tuple(x+t*(y-x) for x,y in zip(p,q)))
        a=out
        if len(a)<3:return 0.
    return area(a)

def judge(rows):
    tris=[]
    for r in rows:
        if r[0]!='F':continue
        v=list(map(float,r[3:])); tris.append((r[1]+':'+r[2],sum(v[1::3])/3,list(zip(v[0::3],v[2::3]))))
    pairs=[]
    for a,b in itertools.combinations(tris,2):
        if abs(a[1]-b[1])>.002:continue
        ar=overlap(a[2],b[2])
        if ar>1e-5:pairs.append((a[0],b[0],round(ar,6)))
    floor=[t for t in tris if t[0].startswith('HOUSE_shell:') and abs(t[1])<.002]
    coverage=sum(overlap(t[2],[(-4.3,-3),(4.3,-3),(4.3,3)])+overlap(t[2],[(-4.3,-3),(4.3,3),(-4.3,3)]) for t in floor)
    ok=not pairs and abs(coverage-51.6)<.001
    return [{'name':'Studio floor surfaces','problem':None if ok else '바닥 겹침/누락','value':{'triangles':len(tris),'coplanar_pairs':pairs,'interior_area':round(coverage,6),'expected_area':51.6},'fix':None if ok else '생성기의 바닥 중복/누락 수정'}]

if __name__=='__main__':
    a=[(0,0),(1,0),(0,1)]
    assert abs(overlap(a,a)-.5)<1e-9
    assert overlap(a,[(1,0),(1,1),(0,1)])<1e-9
    assert abs(overlap(a,[(0,0),(1,0),(1,1)])-.25)<1e-9
    def row(i,pts): return ['F','HOUSE_shell',str(i)]+[str(v) for x,z in pts for v in (x,0,z)]
    base=[row(0,[(-4.3,-3),(4.3,-3),(4.3,3)]),row(1,[(-4.3,-3),(4.3,3),(-4.3,3)])]
    assert judge(base)[0]['problem'] is None
    assert judge(base+[base[0]])[0]['problem'] is not None
    assert judge(base[:1])[0]['problem'] is not None
    print('triangle clipping controls PASS (duplicate / shared edge / partial overlap)')
    print('floor sensor controls PASS (complete floor / duplicate FAIL / missing half FAIL)')
