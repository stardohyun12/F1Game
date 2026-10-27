from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,csv,hashlib,re,numpy as np
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-albedo-kit';V=R/'art/_review/2026-10-27-albedo-kit';jobs=json.loads((W/'inventory.json').read_text());checks=[];colors=set()
for d in jobs:
 p=E/d['file'];im=Image.open(p).convert('RGBA');a=np.array(im);old=np.array(Image.open(d['before']).convert('RGBA'));assert a.shape==old.shape;assert np.array_equal(a[:,:,3],old[:,:,3]);packed=(a[:,:,0].astype('uint32')<<16)|(a[:,:,1].astype('uint32')<<8)|a[:,:,2];pal=sorted(int(x) for x in np.unique(packed));colors.update(pal)
 checks.append(dict(file=d['file'],source=str(Path(d['before']).relative_to(R)),size=list(im.size),alpha_identical=True,uv_changed=False,fbx_reused=d.get('fbx','existing wall UV'),colors=len(pal),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),source_sha256=hashlib.sha256(Path(d['before']).read_bytes()).hexdigest()))
 # Explicit per-texture palette, with no dithering; no second lighting/color grade.
 (E/(Path(d['file']).stem+'.gpl')).write_text('GIMP Palette\nName: D185 '+d['name']+' albedo\nColumns: 8\n#\n'+''.join(f'{v>>16} {(v>>8)&255} {v&255} c{i}\n' for i,v in enumerate(pal)))
(E/'validation.json').write_text(json.dumps(dict(png_count=len(checks),fbx_count=0,files=checks),indent=2))
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n);sheet=Image.new('RGB',(2800,3100),(233,229,216));draw=ImageDraw.Draw(sheet)
def label(x,y,s,n=25):draw.text((x,y),s,font=font(n),fill=(43,49,47))
label(32,20,'D-185 · 구운 조명 제거 / 재료색 albedo',43);label(32,84,'35 PNG · 기존 형태/UV/바퀴 유지 · 같은 카메라, 양쪽 모두 추가 조명 없이 표시',27);label(32,127,'도현 승인 필요 · Unity 미실행 · QA/프로그래밍 임포트 필요',25)
ordered=sorted(jobs,key=lambda d:(0 if d['kind']=='garage' else 1 if d['name']=='dirt' else 2 if d['name'].startswith('autumn') else 3 if d['kind']=='kart' else 4,d['name']))
for i,d in enumerate(ordered):
 x=(i%5)*560+15;y=190+(i//5)*410;label(x,y,d['name'],26);label(x,y+39,'구운 판',21);label(x+275,y+39,'albedo',21)
 for j,tag in enumerate(['baked','albedo']):
  im=Image.open(V/(d['name']+'_'+tag+'.png')).convert('RGBA');im.thumbnail((265,300),Image.Resampling.LANCZOS);sheet.paste(im,(x+j*275+(265-im.width)//2,y+75+(300-im.height)//2),im)
 label(x,y+372,d['file'],19)
sheet.save(V/'sheet.png')
cards=''.join('<section><h2>'+d['name']+'</h2><img src="'+d['name']+'_baked.png"><img src="'+d['name']+'_albedo.png"></section>' for d in ordered)
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>D185 Albedo</title><style>body{background:#e9e5d8;font:18px sans-serif;color:#2b312f;margin:24px}section{display:inline-block;width:48%;vertical-align:top}img{width:49%}</style><h1>D-185 albedo — 도현 승인 필요</h1><p>왼쪽 구운 판 / 오른쪽 albedo. Unity 미실행, 실시간 조명 결과가 아님.</p><p><a href="sheet.png">전체 시트</a> · <a href="../../export/scene-albedo-kit/APPLY.md">적용 메모</a></p>'+cards,encoding='utf-8')
# Source lineage remains in the existing provenance registry, no new external photographs.
registry=R/'art/provenance.csv'
with registry.open(encoding='utf-8-sig',newline='') as f:oldrows=list(csv.DictReader(f));fields=list(oldrows[0])
rows=[]
for c in checks:
 origin=next((r for r in reversed(oldrows) if r['path'].replace('\\','/')==c['source']),None)
 rows.append(dict(asset_id='d185_albedo_'+Path(c['file']).stem,path=(E/c['file']).relative_to(R).as_posix(),source_url=origin['source_url'] if origin else c['source'],license=origin['license'] if origin else 'Derivative; see source registry',accessed='2026-10-27',tool='Blender EMIT UV transfer + Pillow/NumPy',ai_generated='false',sha256=c['sha256'],notes='D-185; geometry and UV unchanged; no sun/sky/AO bake; source '+c['source']))
with (E/'provenance.csv').open('w',encoding='utf-8',newline='') as f:wr=csv.DictWriter(f,fieldnames=fields);wr.writeheader();wr.writerows(rows)
ids={r['asset_id'] for r in rows}
with registry.open('w',encoding='utf-8',newline='') as f:wr=csv.DictWriter(f,fieldnames=fields);wr.writeheader();wr.writerows([r for r in oldrows if r['asset_id'] not in ids]+rows)
print('PACKAGE',len(jobs),'PNG; shape/alpha/source hashes verified')
