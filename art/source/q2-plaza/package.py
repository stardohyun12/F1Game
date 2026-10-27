from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,csv,sys,numpy as np
W=Path(__file__).parent;R=W.parents[2];E=R/'art/export/q2-plaza';V=R/'art/_review/2026-10-28-q2-plaza';stage=sys.argv[1] if len(sys.argv)>1 else 'r1';D=V/stage
for n in ['corner','game15','sign20','top','side']:Image.open(D/(n+'.png')).resize((1280,720),Image.Resampling.NEAREST).save(D/(n+'_720.png'))
sheet=Image.new('RGB',(2640,2000),(232,228,214));d=ImageDraw.Draw(sheet);font=lambda s:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',s)
def label(x,y,s,size=24):d.text((x,y),s,font=font(size),fill=(46,50,45))
label(30,18,'Q2 · 항구 광장 / 바닥 · 벤치 · 가로등 · 표지판 · 화분',38)
label(30,78,'Albedo only · 빛 굽기 없음 · Blender 해 1 + 하늘빛 근사 · 도현 승인 필요',24)
for n,x,y,title in [('corner',20,160,'광장 모퉁이'),('game15',1340,160,'게임 거리 15m · 카메라 높이 2.8m'),('sign20',20,950,'표지판 20m · 720 원본'),('top',1340,950,'톱다운 · 진입로 개방 / 바닥 화살표 없음')]:
 label(x,y-36,title);sheet.paste(Image.open(D/(n+'_720.png')),(x,y))
label(30,1710,'돌 포석 4×4m / 연석 1m 모듈 · 소품 바닥 중심 피벗 · Y-up FBX · 실물 치수',25)
for i,n in enumerate(['plaza_paving','plaza_curb','plaza_props','plaza_sign']):
 im=Image.open(E/(n+'.png'));im.thumbnail((220,180),Image.Resampling.NEAREST);sheet.paste(im,(30+i*650,1790));label(270+i*650,1800,n,21)
sheet.save(D/'sheet.png');sheet.save(V/'sheet.png')
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Q2 항구 광장</title><style>body{background:#e8e4d6;font:18px sans-serif;margin:24px}img{max-width:100%;image-rendering:pixelated}</style><h1>Q2 · 도현 승인 필요</h1><p>Blender 참고 촬영. Unity 미실행. 15m/20m 사진은 1280×720 Nearest.</p><a href="sheet.png">시트</a>'+''.join('<h2>'+n+'</h2><img src="'+stage+'/'+n+'_720.png">' for n in ['corner','game15','sign20','top','side']),encoding='utf-8')
checks=[]
for p in E.glob('*.png'):
 a=np.array(Image.open(p).convert('RGB'));item=dict(file=p.name,size=[a.shape[1],a.shape[0]],colors=len(np.unique(a.reshape(-1,3),axis=0)),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),baked_light=False)
 if p.stem in ['plaza_paving','plaza_curb']:
  item['opposite_edges_identical']=bool(np.array_equal(a[0],a[-1]) and np.array_equal(a[:,0],a[:,-1]));assert item['opposite_edges_identical']
 checks.append(item)
(E/'texture-validation.json').write_text(json.dumps(checks,indent=2))
sources='art/source/photo-picks/SOURCES.md#H2-H3; art/source/q2-plaza/photo-samples.json'
rows=[]
for p in sorted(E.iterdir()):
 if p.suffix not in ['.fbx','.png']:continue
 rows.append(dict(asset_id='q2_'+p.name,path=p.relative_to(R).as_posix(),source_url=sources,license='Original code geometry/bitmap lettering; CC0 photo colours reference only',accessed='2026-10-28',tool='Blender bpy + Pillow; albedo only',ai_generated='false',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes='D186 Q2; no AI images; no lighting bake; source art/source/q2-plaza'))
p=R/'art/provenance.csv'
with p.open(encoding='utf-8-sig',newline='') as f:old=list(csv.DictReader(f));fields=list(old[0])
ids={r['asset_id'] for r in rows}
for p,data in [(p,[r for r in old if r['asset_id'] not in ids]+rows),(E/'provenance.csv',rows)]:
 with p.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(data)
print('Q2 PACKAGE',stage,len(rows),'deliverables')
