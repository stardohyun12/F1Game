from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,csv,hashlib,importlib.util,sys,numpy as np
W=Path(__file__).parent;R=W.parents[2];E=R/'art/export/q2-garage';V=R/'art/_review/2026-10-28-q2-garage';stage=sys.argv[1] if len(sys.argv)>1 else 'r1';D=V/stage
views=['outside15','front5','inside','sign20']
for tag in ['before','after']:
 for n in views:Image.open(D/(tag+'_'+n+'.png')).resize((1280,720),Image.Resampling.NEAREST).save(D/(tag+'_'+n+'_720.png'))
spec=importlib.util.spec_from_file_location('audit_lite_q2',R/'docs/qa/tmp/audit_lite.py');audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
paths=[('Unity left reference',V/'game_reference_left.png'),('Blender before inside',D/'before_inside_720.png'),('Blender after inside',D/'after_inside_720.png'),('Blender after 15m',D/'after_outside15_720.png'),('Blender after 5m',D/'after_front5_720.png')]
metrics=[];keys=['face_uniform_frac','face_std_median','empty_cell_frac_bottom','val_p5_p95']
for n,p in paths:
 m,_=audit.measure(n,p.relative_to(R).as_posix());m['gate']=[m[keys[0]]<=.50,m[keys[1]]>=1.5,m[keys[2]]<=.20,m[keys[3]]>=.55];metrics.append(m);print('AUDIT',n,[round(m[k],4) for k in keys],m['gate'],flush=True)
(D/'audit.json').write_text(json.dumps(dict(threshold_source='docs/qa/tmp/scene_a_audit.py',thresholds=dict(zip(keys,['<=0.50','>=1.5','<=0.20','>=0.55'])),frames=metrics),indent=2))
sheet=Image.new('RGB',(2640,3800),(232,228,214));dr=ImageDraw.Draw(sheet);font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
def label(x,y,t,n=25):dr.text((x,y),t,font=font(n),fill=(43,47,43))
label(28,20,'Q2 정비소 · 기존 albedo → 사장의 흔적 / 빛 굽기 없음',37);label(28,75,'좌: 현재 / 우: 다듬은 판 · 같은 Blender 카메라·해 1·하늘빛 · Unity 미실행')
for row,n in enumerate(views):
 y=155+row*770;label(20,y-36,{'outside15':'셔터 밖 15m','front5':'셔터 앞 5m','inside':'안쪽 — 천장·작업대·등록 게시판','sign20':'추가: 간판 20m'}[n])
 for col,tag in enumerate(['before','after']):sheet.paste(Image.open(D/(tag+'_'+n+'_720.png')),(20+col*1320,y))
# Same display scale/aspect for supplied Unity panel and Blender interior, no colour adjustment.
y=3260;label(20,y-35,'D-189 · 제공 게임 왼쪽 칸 / 수정 실내 — 동일 640×360 표시 배율')
for i,p in enumerate([V/'game_reference_left.png',D/'before_inside_720.png',D/'after_inside_720.png']):
 sheet.paste(Image.open(p).resize((640,360),Image.Resampling.NEAREST),(20+i*870,y));m=metrics[i];label(20+i*870,y+365,'단색면 / 면변화 / 아래빈칸 / 밝기폭',20);label(20+i*870,y+395,f"{m[keys[0]]:.1%} / {m[keys[1]]:.2f} / {m[keys[2]]:.1%} / {m[keys[3]]:.3f}  ({sum(m['gate'])}/4)",23)
label(20,3730,'기준: ≤50% / ≥1.5 / ≤20% / ≥0.55 · 수치·새 눈 게이트 모두 확인 후 도현 승인',23);sheet.save(D/'sheet.png');sheet.save(V/'sheet.png')
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Q2 garage</title><style>body{background:#e8e4d6;font:18px sans-serif;margin:24px}img{max-width:49%;image-rendering:pixelated}</style><h1>정비소 · 도현 승인 필요</h1><a href="sheet.png">전체 시트</a>'+''.join('<h2>'+n+'</h2><img src="'+stage+'/before_'+n+'_720.png"><img src="'+stage+'/after_'+n+'_720.png">' for n in views)+'<h2>게임 기준</h2><img src="game_reference_left.png"><p>측정: '+stage+'/audit.json</p>',encoding='utf-8')
registry=R/'art/provenance.csv'
with registry.open(encoding='utf-8-sig',newline='') as f:old=list(csv.DictReader(f));fields=list(old[0])
rows=[]
for p in sorted(E.iterdir()):
 if p.suffix not in ['.fbx','.png']:continue
 rows.append(dict(asset_id='q2_garage_'+p.name,path=p.relative_to(R).as_posix(),source_url='art/source/q2-garage/material-sources.json; art/reference/kart-v6/provenance-rows.csv; original scene-albedo-kit provenance.csv',license='CC0 materials; CC0/US public domain race photographs; original geometry/lettering',accessed='2026-10-28',tool='Blender bpy/Pillow; no AI; no lighting bake',ai_generated='false',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes='D189 photographic material albedo; original garage structure/origin retained'))
ids={r['asset_id'] for r in rows}
for p,data in [(registry,[r for r in old if r['asset_id'] not in ids]+rows),(E/'provenance.csv',rows)]:
 with p.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(data)
validation=[]
for p in E.glob('*.png'):
 im=Image.open(p).convert('RGB');a=np.array(im);validation.append(dict(file=p.name,size=list(im.size),colors=len(np.unique(a.reshape(-1,3),axis=0)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
(E/'texture-validation.json').write_text(json.dumps(validation,indent=2));print('PACKAGE COMPLETE',stage,flush=True)
