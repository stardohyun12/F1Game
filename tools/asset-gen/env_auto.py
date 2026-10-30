"""D-207: eight review-only OSM buildings. No Unity API or F1 writes.

py -3.13 tools/asset-gen/env_auto.py select|build|check|sheet|all
Blender owns geometry; existing check_rules.py owns surface checks.
"""
from pathlib import Path
import argparse, hashlib, json, math, subprocess, sys

R = Path(__file__).resolve().parents[2]
E = R / 'art/export/env-first'
V = R / 'art/_review/2026-10-30-env-first'
K = R / 'art/export/kit-shared-v2'
BL = 'D:/Tools/Blender/blender.exe'
# Selection is deliberately limited, stable, and reviewable; no random town fill.
PICKS = [
 ('camo_204696009', '집 동쪽 24m, 정비소로 나갈 때 가장 큰 인접 회벽', 3),
 ('camo_204693578', '집 북쪽 33m, 집 뒤 실루엣', 3),
 ('camo_204701091', '집 북동쪽 37m, 동선 뒤 연립주택', 4),
 ('camo_204695467', '집 남쪽 49m, 돌아오는 길 바깥 회벽', 3),
 ('tomo_1355595398', '집 서쪽 39m, 큰길에 붙은 낮은 부속채', 1),
 ('camo_204697918', '정비소 북동쪽 37m, 보관소 방향에서 보이는 집', 3),
 ('camo_204695159', '정비소 남동쪽 35m, 길 건너 큰 연립주택', 4),
 ('camo_204694937', '보관소 북쪽 31m, 우체국 앞에서 보는 배경 집', 3),
]

def write(path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(obj, ensure_ascii=False, indent=2), encoding='utf-8')

def select():
    import yaml
    from shapely.geometry import Polygon, Point, LineString
    from PIL import Image, ImageDraw, ImageFont
    src = json.loads((R/'docs/map/env-buildings.json').read_text(encoding='utf-8'))
    mp = json.loads((R/'docs/map/town-v6b.json').read_text(encoding='utf-8'))
    pl = yaml.safe_load((R/'docs/design/tutorial/placements.yaml').read_text(encoding='utf-8'))['places']
    by = {b['id']: b for b in src['buildings']}
    selected = []
    for i,(bid,reason,floors) in enumerate(PICKS):
        b = dict(by[bid]); p=Polygon(b['poly'])
        b.update(number=i+1, name=f'env_{bid}_v01', selection_reason=reason,
                 source_floors=b['floors'], model_floors=floors,
                 home_distance_m=round(p.distance(Point(*pl['house_home']['world'])),2),
                 wall_cell=['wall_cream','wall_apricot_B','wall_mint','wall_apricot','wall_cream','wall_apricot','wall_apricot_B','wall_cream'][i],
                 roof_cell='roof_F3' if b['type']=='annex' else ('roof_E3' if i%2 else 'roof_E2'),
                 budget_tris=12000, approval='pending')
        # Actual footprint bounds centre, not OSM vertex mean: bottom-centred export pivot.
        x0,z0,x1,z1=p.bounds; b['pivot_xz']=[(x0+x1)/2,(z0+z1)/2]
        b['roof_mode']='shed' if b['type']=='annex' else ('gable' if b['simple_rect'] and b['type'] not in ('apartment','terrace_house') else 'flat_parapet')
        if b['roof_mode']=='flat_parapet':
            from shapely import constrained_delaunay_triangles
            inner=p.buffer(-.22,join_style=2)
            inners=list(inner.geoms) if hasattr(inner,'geoms') else [inner]
            b['parapet_inner']=[list(pp.exterior.coords)[:-1] for pp in inners if not pp.is_empty]
            ring=p.difference(inner)
            b['parapet_caps']=[list(t.exterior.coords)[:3] for t in constrained_delaunay_triangles(ring).geoms]
        assert b['osm_way'] not in mp['removed']['osm_ways']
        selected.append(b)
    manifest=dict(schema=1, approval='D-207 preview only; director approval pending',
                  coordinates='Unity world x,y,z; each FBX local bottom-centred; placement in manifest',
                  ground_note='OSM source y retained; D-209 terrain is being edited separately. No live-map ground clearance claim.',
                  source_floor_note='Classifier floors are old builder hash 2..4, not surveyed OSM levels. Model floors follow ENV-PLAN type ranges.',
                  kit_sha256={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in K.glob('*.png')},
                  input_sha256={s:hashlib.sha256((R/s).read_bytes()).hexdigest() for s in ['docs/map/env-buildings.json','docs/map/town-v6b.json','docs/design/tutorial/placements.yaml']},
                  landmarks={n:pl[n] for n in ('house_home','garage','depot','postoffice')},buildings=selected)
    write(E/'manifest.json',manifest)
    # Neutral review terrain: original grid, flattened under each fixed-height building.
    # This is disposable context, never a replacement for the live D-209 map terrain.
    grid={(p[0],p[1]):p[2] for p in mp['terrain']['controlPoints']};step=mp['terrain']['step']
    gx=min(p[0] for p in mp['terrain']['controlPoints']);gz=min(p[1] for p in mp['terrain']['controlPoints'])
    def height(x,z):
        xx=gx+math.floor((x-gx)/step)*step;zz=gz+math.floor((z-gz)/step)*step
        u=(x-xx)/step;v=(z-zz)/step
        return sum(grid.get((xx+dx*step,zz+dz*step),25)*(u if dx else 1-u)*(v if dz else 1-v) for dx in (0,1) for dz in (0,1))
    plots=[(Polygon(b['poly']),b['y']) for b in selected]
    from shapely.geometry import box as shapely_box
    for n in ('house_home','garage','depot','postoffice'):
        x,z=pl[n]['world'];w,d={'house_home':(12,12),'garage':(16,14),'depot':(16,12),'postoffice':(14,12)}[n]
        plots.append((shapely_box(x-w/2,z-d/2,x+w/2,z+d/2),pl[n]['y']))
    def reviewheight(x,z):
        h=height(x,z);p=Point(x,z);ds=sorted((p.distance(poly),yy) for poly,yy in plots)
        dist,yy=ds[0];weight=max(0,min(1,(10-dist)/7))
        weight=weight*weight*(3-2*weight)
        return h*(1-weight)+(yy-.08)*weight
    nx,nz=111,81
    terrain={'note':'review context only: original grid + local pads; NOT live D-209 terrain','nx':nx,'nz':nz,
             'vertices':[[120+ix*2,-130+iz*2,reviewheight(120+ix*2,-130+iz*2)] for iz in range(nz) for ix in range(nx)],'roads':[]}
    for rd in mp['roads']:
        if rd['id'] in mp['removed']['roads']:continue
        # Preserve horizontal source centreline, resample to ground and miter the joins.
        points=[]
        for a,b in zip(rd['knots'],rd['knots'][1:]):
            n=max(1,math.ceil(math.hypot(b[0]-a[0],b[1]-a[1])/2))
            for j in range(n):
                t=j/n;x=a[0]+t*(b[0]-a[0]);z=a[1]+t*(b[1]-a[1])
                if 122<x<338 and -128<z<28:points.append([x,z,reviewheight(x,z)+.035])
        if len(points)>1:terrain['roads'].append(dict(width=rd['width'],points=points))
    # Clip the road surface into terrain triangles. Elevating only its centreline
    # produces folded ribbons on a slope; this reproduces a conforming overlay instead.
    from shapely.ops import unary_union, triangulate
    road_area=unary_union([LineString([(p[0],p[1]) for p in rd['knots']]).buffer(rd['width']/2,join_style=2)
                          for rd in mp['roads'] if rd['id'] not in mp['removed']['roads']])
    terrain['road_triangles']=[]
    for j in range(nz-1):
        for i in range(nx-1):
            q=j*nx+i
            for ids in [(q,q+1,q+nx+1),(q,q+nx+1,q+nx)]:
                a,b,c=[terrain['vertices'][ii] for ii in ids]
                tri=Polygon([(p[0],p[1]) for p in (a,b,c)])
                if not road_area.intersects(tri):continue
                cut=road_area.intersection(tri)
                polys=list(cut.geoms) if hasattr(cut,'geoms') else [cut]
                for poly in polys:
                    if poly.geom_type!='Polygon' or poly.area<1e-7:continue
                    for tt in triangulate(poly):
                        if not poly.covers(tt.representative_point()):continue
                        ps=[]
                        for x,z in list(tt.exterior.coords)[:3]:
                            det=(b[0]-a[0])*(c[1]-a[1])-(c[0]-a[0])*(b[1]-a[1])
                            u=((x-a[0])*(c[1]-a[1])-(z-a[1])*(c[0]-a[0]))/det
                            v=((z-a[1])*(b[0]-a[0])-(x-a[0])*(b[1]-a[1]))/det
                            ps.append([x,z,a[2]+u*(b[2]-a[2])+v*(c[2]-a[2])+.04])
                        terrain['road_triangles'].append(ps)
    write(E/'review-terrain.json',terrain)
    V.mkdir(parents=True,exist_ok=True)
    W,H=1500,1000; im=Image.new('RGB',(W,H),'#eee9de'); dr=ImageDraw.Draw(im)
    fontpath='C:/Windows/Fonts/malgun.ttf'
    f=ImageFont.truetype(fontpath,19); fs=ImageFont.truetype(fontpath,15); title=ImageFont.truetype(fontpath,28)
    # North up, equal horizontal scale; right column lists numbered OSM identities.
    scale=4.6
    def xy(p):return (50+(p[0]-110)*scale,830-(p[1]+125)*scale)
    dr.text((40,20),'집 주변·튜토리얼 배경 건물 첫 판 / 8채',font=title,fill='#263e42')
    dr.text((40,60),'OSM 평면 위치 · 원본 높이 유지 · 집 중심 반경 60m · 실제 지형/가림 검증 전',font=f,fill='#52635c')
    for b in src['buildings']:
        if 105<b['center'][0]<310 and -130<b['center'][1]<20:
            dr.polygon([xy(p) for p in b['poly']],fill='#d8d1c5',outline='#b8b3a7')
    for rd in mp['roads']:
        if rd['id'] in mp['removed']['roads']:continue
        p=rd['knots']
        # crop rather than drawing distant map across legend
        for a,b in zip(p,p[1:]):
            if all(105<q[0]<310 and -130<q[1]<20 for q in (a,b)):
                dr.line([xy(a),xy(b)],fill='#aeb7b3',width=max(2,int(rd['width']*scale)))
    cx,cy=xy(pl['house_home']['world']); rr=60*scale
    dr.ellipse((cx-rr,cy-rr,cx+rr,cy+rr),outline='#8a9e83',width=2)
    for b in selected:
        col='#b17d59' if b['type']=='annex' else '#a4b59c' if b['type']=='apartment' else '#d6a994'
        dr.polygon([xy(p) for p in b['poly']],fill=col,outline='#5f6761',width=2)
        x,z=xy(b['pivot_xz']);dr.ellipse((x-13,z-13,x+13,z+13),fill='#314b4c');dr.text((x-6,z-11),str(b['number']),font=fs,fill='white')
    for n,tag in [('house_home','H 집'),('garage','G 정비소'),('depot','D 보관소'),('postoffice','P 우체국')]:
        x,y=xy(pl[n]['world']);dr.rectangle((x-7,y-7,x+7,y+7),fill='#b55143')
        tx,ty=(x-28,y-32) if n=='depot' else (x+10,y+6) if n=='postoffice' else (x+10,y-12)
        dr.text((tx,ty),tag,font=fs,fill='#6a3430')
    # Excluded overlaps are explicit rather than silently replacing story buildings.
    for bid in ['camo_204700995','camo_204694107','camo_204698626']:
        b=by[bid];dr.line([xy(b['poly'][0]),xy(b['poly'][len(b['poly'])//2])],fill='#b55143',width=3)
    for i,b in enumerate(selected):
        x,y=1090,120+i*91
        dr.text((x,y),f"{i+1}. {b['id']}",font=fs,fill='#263e42')
        dr.text((x,y+23),f"{b['type']} / {b['model_floors']}층",font=fs,fill='#53685a')
        dr.text((x,y+46),f"x {b['center'][0]:.1f}, z {b['center'][1]:.1f}",font=fs,fill='#53685a')
    dr.text((45,925),'N ↑  /  회색: OSM 건물·길  /  색: 생성 8채  /  빨강: 이야기 건물  /  사선: 겹침 제외 후보',font=f,fill='#263e42')
    dr.text((45,960),'© OpenStreetMap contributors · ODbL / SRTM · docs/map/ATTRIBUTION.md',font=fs,fill='#52635c')
    dr.line([(70,890),(70+20*scale,890)],fill='#263e42',width=4);dr.text((70,860),'20m',font=fs,fill='#263e42')
    im.save(R/'docs/map/env-first.png');im.save(V/'plan.png')
    lines=['# ENV 첫 판 선정 8채','', '현재 placements.yaml을 읽었으며 OSM 외곽·x/z·원본 y는 보존한다. 층수는 조사값이 아닌 기존 해시값이므로 ENV-PLAN 유형 범위로 명시적으로 바꿨다.', '', '|#|OSM|유형|원본→제작 층|x,z|선정 이유|','|---|---|---|---|---|---|']
    for b in selected:lines.append(f"|{b['number']}|{b['id']}|{b['type']}|{b['source_floors']}→{b['model_floors']}|{b['center']}|{b['selection_reason']}|")
    lines += ['','제외: camo_204700995(집 자리), camo_204694107·camo_204698626(정비소 자리/진입부). 원본 데이터의 removed 목록도 제외한다.','카메라 크기는 거리·정면 길이 기준의 추정이며 실제 맵 지형·가림을 검증한 가시성 결과는 아니다.','주변 5채 + 정비소/보관소/우체국 앞 배경 3채. 나머지 5개 유형을 억지로 채우지 않는다.']
    (R/'docs/map/env-first.md').write_text('\n'.join(lines),encoding='utf-8')
    print('SELECT 8',E/'manifest.json')

def build():
    p=subprocess.run([BL,'-b','-t','6','--factory-startup','--python',str(R/'tools/asset-gen/env_auto_bpy.py')],cwd=R,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace')
    (V/'build.log').write_text(p.stdout,encoding='utf-8');print(p.stdout[-1600:])
    if p.returncode or 'Traceback' in p.stdout:raise SystemExit('Blender build failed; see build.log')

def check():
    cmd=[sys.executable,'-X','utf8',str(R/'tools/art/check_rules.py'),'--env-dir',str(E),'--only']
    cmd += [b['name'] for b in json.loads((E/'manifest.json').read_text(encoding='utf-8'))['buildings']]
    cmd += ['--gate','--json',str(V/'rules.json'),'--md',str(V/'rules.md')]
    p=subprocess.run(cmd,cwd=R,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace')
    (V/'check.log').write_text(p.stdout,encoding='utf-8');print(p.stdout[-2200:]);return p.returncode

def sheet():
    from PIL import Image, ImageDraw, ImageFont
    files=['cart_home.png','cart_route.png','top.png']
    files=[f for f in files if (V/f).exists()]
    if files:
        out=Image.new('RGB',(1280,80+760*len(files)),'#ece7dc');d=ImageDraw.Draw(out)
        font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',22)
        d.text((24,22),'ENV 첫 판 · Blender 참고 렌더 / A7 Unity 판정 전',font=font,fill='#263e42')
        for i,f in enumerate(files):
            d.text((24,84+760*i),f,font=font,fill='#263e42');out.paste(Image.open(V/f).convert('RGB').resize((1280,720)),(0,120+760*i))
        out.save(V/'sheet.png')
    html='''<!doctype html><html lang="ko"><meta charset="utf-8"><title>ENV 첫 판 · 도현 승인 필요</title><style>body{max-width:1300px;margin:36px auto;background:#eee9df;color:#283e40;font:18px sans-serif}img{max-width:100%;height:auto}article{margin:32px 0}p{line-height:1.6}a{color:#46736c}</style><h1>집 주변·튜토리얼 배경 건물 8채</h1><p>도현 승인 필요. 공용 키트 원본 재질만 사용. 본 맵 미적용.<br>아래 Blender 렌더는 형태 참고이며 Unity A7 게임 화면 판정이 아닙니다. 지형은 원본 OSM 높이의 검토용 받침이며 D-209 실제 지형을 재현하지 않습니다.</p><p><a href="../../export/env-first/manifest.json">좌표·선정 목록</a> · <a href="check.log">기존 규칙 검사 원문</a> · <a href="geometry.json">기하 측정</a> · <a href="rules.md">재질 검사표</a></p>'''
    for f in files+['plan.png']:html+=f'<article><h2>{f}</h2><a href="{f}"><img src="{f}"></a></article>'
    html+='<p>문·간판은 자리만. 최종 문양·글자는 제작하지 않음. QA/프로그래밍 임포트 필요.</p><p><a href="ux-review.md">독립 화면 판정</a> · <a href="readback.json">FBX 재수입 검사</a> · <a href="reproducibility.json">재생성 비교</a> · <a href="STUDIO-HANDOFF.md">A7 촬영 인계</a></p><p>© OpenStreetMap contributors · ODbL / SRTM. 지도 출처: docs/map/ATTRIBUTION.md. 재료 출처: art/export/kit-shared-v2/provenance.csv.</p></html>'
    (V/'index.html').write_text(html,encoding='utf-8')

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('step',choices=['select','build','check','sheet','all']);a=ap.parse_args()
    E.mkdir(parents=True,exist_ok=True);V.mkdir(parents=True,exist_ok=True)
    if a.step in ('select','all'):select()
    if a.step in ('build','all'):build()
    rc=check() if a.step in ('check','all') else 0
    if a.step in ('sheet','all'):sheet()
    sys.exit(rc)
