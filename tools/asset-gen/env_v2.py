"""D-211: explicit small-house compositions on the SAME eight review plots.
No Unity calls. python env_v2.py prepare|build|check|sheet|all
"""
from pathlib import Path
import argparse, copy, hashlib, json, math, shutil, subprocess, sys
from shapely.geometry import Polygon, box
from shapely.affinity import affine_transform
R=Path(__file__).resolve().parents[2]
E=R/'art/export/env-first-v2'; V=R/'art/_review/2026-10-30-env-first-v2'
OLD=R/'art/export/env-first'; BL='D:/Tools/Blender/blender.exe'

# Hand chosen assemblies. Entries = floors, roof, wall, frontage offset, front treatment.
# Layouts are authored, not a seeded rule inventing architecture.
DESIGNS=[
 [(2,'gable','wall_cream',0,'gate')],
 [(2,'gable','wall_apricot',0,'gate'),(1,'hip','wall_cream',.55,'shop')],
 [(2,'gable','wall_cream',0,'balcony'),(3,'cross_gable','wall_apricot_B',.6,'gate'),(2,'hip','wall_mint',.1,'awning')],
 [(2,'hip','wall_apricot_B',0,'shop'),(2,'cross_gable','wall_cream',.7,'balcony')],
 [(1,'gable','wall_cream',0,'shop')],
 [(2,'gable','wall_apricot',0,'stairs'),(2,'cross_gable','wall_cream',.5,'awning')],
 [(2,'gable','wall_cream',0,'stairs'),(3,'hip','wall_apricot_B',.65,'balcony'),(2,'cross_gable','wall_mint',.15,'shop')],
 [(2,'hip','wall_apricot_B',0,'gate'),(1,'flat_parapet','wall_cream',.5,'awning')],
]
def write(p,x): p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(x,ensure_ascii=False,indent=2),encoding='utf8')
def prepare():
    from PIL import Image
    E.mkdir(parents=True,exist_ok=True);V.mkdir(parents=True,exist_ok=True)
    m=json.loads((OLD/'manifest.json').read_text(encoding='utf8'));m['schema']=2
    m['approval']='D-211 revised preview requested; NOT approved for game integration'
    m['ground_note']='Source review terrain only; raised stone plinths. Actual D-209 map requires QA ground check.'
    m['texture_root']='textures';m['palette_overrides']={'retain_stone':'stone-palette.gpl'}
    tx=E/'textures';tx.mkdir(exist_ok=True)
    for p in (R/'art/export/kit-shared-v2').glob('*.png'):shutil.copy2(p,tx/p.name)
    shutil.copy2(R/'art/export/kit-shared-v2/layout.json',tx/'layout.json')
    shutil.copy2(R/'art/export/kit-shared-v2/palette.gpl',tx/'palette.gpl')
    stone=R/'F1/Assets/Scenes/Map_TownV6_Mats/TV6_RetainStone.png'
    im=Image.open(stone).convert('RGB');im.resize((80,60),Image.Resampling.NEAREST).save(tx/'retain_stone.png')
    cols=sorted(set(Image.open(tx/'retain_stone.png').getdata()))
    (tx/'stone-palette.gpl').write_text('GIMP Palette\nName: Existing retaining stone\nColumns: 8\n#\n'+'\n'.join('%d %d %d c%d'%(*c,i) for i,c in enumerate(cols)),encoding='utf8')
    m['stone_source']={'path':str(stone.relative_to(R)),'sha256':hashlib.sha256(stone.read_bytes()).hexdigest(),'source_size':list(im.size),'export_size':[80,60],'tile_m':[2.5,1.875],'change':'nearest resize only; no generated texture or recoloring'}
    for b,design in zip(m['buildings'],DESIGNS):
        b['name']=b['name'].replace('_v01','_v02');b['houses']=[];b['source_poly']=copy.deepcopy(b['poly'])
        b['review_pad_y']=b['y'];b['y']=round(b['y']-.1,6)
        p=Polygon(b['poly']);rect=list(p.minimum_rotated_rectangle.exterior.coords)[:-1]
        ia=max(range(4),key=lambda i:math.dist(rect[i],rect[(i+1)%4]));a=rect[ia];z=rect[(ia+1)%4]
        L=math.dist(a,z);u=((z[0]-a[0])/L,(z[1]-a[1])/L)
        # Face toward original road-facing edge rather than toward arbitrary OSM winding.
        e=b['front_edge'];fa=b['poly'][e];fb=b['poly'][(e+1)%len(b['poly'])]
        front=((fa[0]+fb[0])/2-p.centroid.x,(fa[1]+fb[1])/2-p.centroid.y)
        n=(-u[1],u[0])
        if n[0]*front[0]+n[1]*front[1]<0:u=(-u[0],-u[1]);n=(-n[0],-n[1])
        cx,cz=p.centroid.x,p.centroid.y
        local=affine_transform(p,[u[0],u[1],n[0],n[1],-cx*u[0]-cz*u[1],-cx*n[0]-cz*n[1]])
        x0,y0,x1,y1=local.bounds;N=len(design)
        for j,(floors,roof,wall,offset,treatment) in enumerate(design):
            bay=(x1-x0)/N
            center=x0+bay*(j+.5)
            width=min(8.1,bay-1.06);depth=min(7.5,y1-y0-1.3)
            # Fit each predesigned rectangular house inside its source lot. Frontages stagger.
            yy=y1-.7-depth/2-offset
            if b['number']==1:width=4.5;depth=4.4;center=x0+width/2+.8;yy=(y0+y1)/2
            # Search translation/depth before shrinking both dimensions: concave OSM
            # corners must never silently fall through a fixed attempt count.
            best=None
            for dw in [k*.15 for k in range(17)]:
                ww=width-dw
                if ww<2.8:continue
                for dd in [k*.20 for k in range(20)]:
                    dep=depth-dd
                    if dep<2.8:continue
                    for ox in (0,-.25,.25):
                        for oy in (0,-.3,-.6,.3,.6,-1.0,1.0):
                            cc=center+ox;cy=yy+oy
                            fp=box(cc-ww/2,cy-dep/2,cc+ww/2,cy+dep/2)
                            if not local.buffer(.00001).covers(fp):continue
                            score=ww*dep-.2*abs(oy)-.3*abs(ox)
                            if best is None or score>best[0]:best=(score,ww,dep,cc,cy)
            assert best is not None,('No authored house fit',b['id'],j)
            _,width,depth,center,yy=best
            if b['number']==3 and j==2:
                # The long notched plot otherwise pushes the third frontage 2.9m
                # ahead. Use the same shallow domestic footprint and a 0.5m step.
                first=b['houses'][0];depth=4.2
                first_y=(first['center'][0]-cx)*n[0]+(first['center'][1]-cz)*n[1]
                yy=first_y+first['depth']/2-.5-depth/2
            assert local.buffer(.00001).covers(box(center-width/2,yy-depth/2,center+width/2,yy+depth/2))
            def world(x,y):return [round(cx+u[0]*x+n[0]*y,6),round(cz+u[1]*x+n[1]*y,6)]
            poly=[world(center-width/2,yy+depth/2),world(center+width/2,yy+depth/2),world(center+width/2,yy-depth/2),world(center-width/2,yy-depth/2)]
            b['houses'].append(dict(key=f'H{j+1}',poly=poly,center=world(center,yy),u=list(u),out=list(n),width=round(width,6),depth=round(depth,6),floors=floors,
                 roof_mode=roof,wall_cell=wall,roof_cell='roof_F3' if b['number']==5 else ('roof_E2' if j%2==0 else 'roof_E3'),front_offset=offset,treatment=treatment,
                 eaves=.45,foundation=.9+([0,.16,.05][j]),front_windows=2 if floors==1 else (4 if floors==3 else 3),budget_tris=6000))
        b['model_floors']=max(h['floors'] for h in b['houses']);b['budget_tris']=18000;b['roof_mode']='authored_house_group'
    write(E/'manifest.json',m);shutil.copy2(OLD/'review-terrain.json',E/'review-terrain.json')
    print('PREPARED',len(m['buildings']),'plots',sum(len(b['houses']) for b in m['buildings']),'houses')
def build(no_render=False):
    cmd=[BL,'-b','-t','6','--factory-startup','--python',str(R/'tools/asset-gen/env_auto_bpy.py'),'--','--v2']
    if no_render:cmd+=['--no-render']
    p=subprocess.run(cmd,capture_output=True,text=True,encoding='utf8',errors='replace');(V/('repeat-build.log' if no_render else 'build.log')).write_text(p.stdout+p.stderr,encoding='utf8')
    print((p.stdout+p.stderr)[-1200:]);assert not p.returncode and 'Traceback' not in p.stdout+p.stderr
def check():
    m=json.loads((E/'manifest.json').read_text(encoding='utf8'))
    p=subprocess.run([sys.executable,'-X','utf8',str(R/'tools/art/check_rules.py'),'--env-dir',str(E),'--only']+[b['name'] for b in m['buildings']]+['--gate','--json',str(V/'rules.json'),'--md',str(V/'rules.md')],capture_output=True,text=True,encoding='utf8',errors='replace')
    (V/'check.log').write_text(p.stdout+p.stderr,encoding='utf8');print(p.stdout[-1600:]);return p.returncode
def sheet():
    from PIL import Image,ImageDraw,ImageFont
    files=['cart_home','cart_route','top'];f=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',24)
    out=Image.new('RGB',(1920,850),'#e9e4d8');d=ImageDraw.Draw(out)
    d.text((20,12),'D-211 / 위 1판 · 아래 2판 — 동일 Blender 카메라·빛·검토 지형 (Unity 아님)',font=f,fill='#293e42')
    for row,folder in enumerate([R/'art/_review/2026-10-30-env-first',V]):
        for col,key in enumerate(files):
            d.text((col*640+12,55+row*390),f'{row+1}판 / {key}',font=f,fill='#293e42')
            out.paste(Image.open(folder/(key+'.png')).convert('RGB').resize((640,360)),(col*640,88+row*390))
    out.save(V/'sheet.png')
    details=Image.new('RGB',(1920,800),'#e9e4d8');dd=ImageDraw.Draw(details)
    for i in range(8):
        p=V/f'detail_{i+1:02d}.png'
        if p.exists():
            x=(i%4)*480;y=(i//4)*400;dd.text((x+12,y+10),f'터 {i+1}',font=f,fill='#293e42')
            details.paste(Image.open(p).convert('RGB').resize((480,360)),(x,y+40))
    details.save(V/'details.png')
    (V/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>D-211 배경 집 2판</title><style>body{margin:28px;background:#eee9df;color:#263d42;font:18px sans-serif}img{max-width:100%}a{color:#356a77}p{line-height:1.7}</style><h1>배경 집 2판 · 도현 승인 필요</h1><p>같은 8개 터, 작은 집 16채. 위 1판·아래 2판. 같은 Blender 카메라·빛·검토 지형이며 실제 Unity 맵 촬영은 다음 과제입니다.<br>house_home 창·덧창·문·24° 지붕을 재사용·변형. 본 맵 미적용. QA/프로그래밍 임포트 필요.</p><a href="sheet.png"><img src="sheet.png"></a><p><a href="geometry.json">기하 측정</a> · <a href="readback.json">FBX 검사</a> · <a href="reproducibility.json">재생성</a> · <a href="rules.md">재질 검사</a></p><p>© OpenStreetMap contributors · ODbL / SRTM. 기존 지도 및 키트 출처 유지.</p></html>''',encoding='utf8')
    page=(V/'index.html').read_text(encoding='utf8').replace('</html>','<h2>8개 터 앞면</h2><a href="details.png"><img src="details.png"></a><p>1층 3채 · 2층 11채 · 3층 2채 / 평지붕 1채 / 처마45cm. 돌 원본은 현재 축대와 같으며 32px/m에 맞춰 80×60으로 축소했습니다.</p><p>재질 전체 게이트는 잔결 실패가 남아 있습니다. 기하 PASS와 적용 승인은 별개입니다.</p><h2>이음매 확인</h2><img src="seams_roof.png" style="width:48%"><img src="seams_stairs.png" style="width:48%"><p><a href="site-audit.json">터·정비소·검토 지형 접지 검사</a> · <a href="ux-review.md">독립 비교 리뷰</a> · <a href="STUDIO-HANDOFF.md">촬영 인계</a></p></html>')
    (V/'index.html').write_text(page,encoding='utf8')
if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('step',choices=['prepare','build','repeat','check','sheet','all']);a=ap.parse_args()
    if a.step in ('prepare','all'):prepare()
    if a.step in ('build','all'):build()
    if a.step=='repeat':build(True)
    rc=check() if a.step in ('check','all') else 0
    if a.step in ('sheet','all'):sheet()
    sys.exit(rc)
