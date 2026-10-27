from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps
import json,csv,re,hashlib,shutil
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-a6-photo-kit';V=R/'art/_review/2026-10-27-photo-kit';items=json.loads((W/'manifest.json').read_text());metrics=json.loads((E/'texture-validation.json').read_text());geo=json.loads((E/'geometry-validation.json').read_text())
for d in items[:4]:
 folder=E/(d['name']+'.fbm');folder.mkdir(exist_ok=True);shutil.copyfile(E/(d['name']+'.png'),folder/(d['name']+'.png'))
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
s=Image.new('RGB',(2400,2710),(232,227,212));dr=ImageDraw.Draw(s)
def label(x,y,t,n=25):dr.text((x,y),t,font=font(n),fill=(45,49,45))
def panel(path,box):
 im=Image.open(path).convert('RGBA');im.thumbnail((box[2],box[3]),Image.Resampling.LANCZOS);s.paste(im,(box[0]+(box[2]-im.width)//2,box[1]+(box[3]-im.height)//2),im)
label(35,22,'도현 선택 사진 → 생활 소품 · 함석 · 돌담',43)
label(35,82,'원근 보정 / 날짜 제외 / 빛 평탄화 / 약 32 texel·m / 늦가을 빛 베이크 / 디더 없음',25)
label(35,126,'Blender 참고 미리보기 — 도현 승인 필요 · 실제 Unity 720 컷은 QA/프로그래밍 임포트 필요',24)
for i,d in enumerate(items[:4]):
 x=30+i*595;n=d['name'];label(x,190,n,29);label(x,232,'보정한 원본 크롭',22);panel(W/'crops'/(n+'.png'),(x,265,565,225));label(x,502,'사진 정면 적용',22);panel(V/(n+'_photo.png'),(x,530,565,335));label(x,872,'결과 · 같은 카메라',22);panel(V/(n+'_result.png'),(x,905,565,335));label(x,1250,' × '.join(map(str,d['size']))+' m',22);label(x,1285,f"{geo[i]['tris']} tris / {metrics[n]['colors']}색 / 32 texel·m",22)
label(35,1345,'벽·지붕 타일 — 아래 돌담 미리보기는 높이 0.9m, 길이 6m',30)
for i,d in enumerate(items[4:]):
 x=25+i*475;n=d['name'];label(x,1395,n,26);panel(W/'crops'/(n+'.png'),(x,1440,450,190));label(x,1636,'원본 사진 적용',21);panel(V/(n+'_photo.png'),(x,1660,450,235));label(x,1900,'결과 · 같은 카메라',21);panel(V/(n+'_result.png'),(x,1930,450,235));label(x,2168,f"{d['source_pixels'][0]}×{d['source_pixels'][1]}px / {metrics[n]['colors']}색",22);label(x,2200,f"{d['tile_m'][0]}×{d['tile_m'][1]}m · 양 끝 RGB 차이 0",21)
label(35,2250,'정비소 교체용 아틀라스 — UV 유지, 함석 밖 픽셀 동일',29)
for i,tag in enumerate(['original','F2','F3']):
 x=25+i*790;label(x,2295,{'original':'기존 정비소','F2':'F2 붉은 함석','F3':'F3 회색 함석'}[tag],25);panel(V/('garage_'+tag+'.png'),(x,2335,760,305))
label(35,2660,'CC0 사진 8장 · 생성형 이미지 0 · 재현 스크립트/검증 JSON/적용 메모 동봉',23);s.save(V/'sheet.png')
links=''.join(f'<li><a href="{p.name}">{p.name}</a></li>' for p in sorted(V.glob('*.png')) if p.name!='sheet.png')
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Photo kit review</title><style>body{background:#e8e3d4;font:18px sans-serif;margin:24px}img{width:100%;max-width:2400px}a{color:#28565d}</style><h1>사진 기반 소품·함석·돌담 — 도현 승인 필요</h1><p>Blender 참고. QA/프로그래밍 임포트 필요. Unity 게임 컷 미확인.</p><img src="sheet.png"><p><a href="../../export/scene-a6-photo-kit/APPLY.md">적용 메모</a></p><ul>'+links+'</ul>',encoding='utf-8')
# Source rows retain the documented CC0 verification; derivatives retain source associations.
source={}
for line in (R/'art/source/photo-picks/SOURCES.md').read_text(encoding='utf-8').splitlines():
 if not line.startswith('| '):continue
 c=[x.strip() for x in line.split('|')]
 if c[1] in {d['pick'] for d in items}:source[c[1]]=dict(path=R/'art/source/photo-picks'/c[2].strip('`'),url=re.search(r'https://\S+',c[3]).group(),author=c[5])
rows=[]
def record(id,p,url,note):rows.append(dict(asset_id=id,path=p.relative_to(R).as_posix(),source_url=url,license='CC0-1.0',accessed='2026-10-27',tool='Pillow/OpenCV/Blender 5.2.2',ai_generated='false',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes=note))
for k,a in source.items():record('photo_kit_source_'+k,a['path'],a['url'],'Existing SOURCES.md CC0 verification; author '+a['author'])
for d in items:
 for suffix in (['.png','.fbx'] if d['kind']=='prop' else ['.png']):record('photo_kit_'+d['name']+suffix,E/(d['name']+suffix),source[d['pick']]['url'],'Rectified crop; 32 texel/m; V5 autumn bake; no AI; crop coordinates in art/source/photo-kit/manifest.json')
for k in ['F2','F3']:record('photo_kit_garage_'+k,E/('garage_tin_'+k+'_atlas.png'),source[k]['url'],'Original garage UV preserved; exterior tin pixels only')
with (E/'provenance.csv').open('w',newline='',encoding='utf-8') as f:
 wr=csv.DictWriter(f,fieldnames=list(rows[0]));wr.writeheader();wr.writerows(rows)
registry=R/'art/provenance.csv'
with registry.open(encoding='utf-8-sig',newline='') as f:old=list(csv.DictReader(f));fields=list(old[0])
ids={r['asset_id'] for r in rows};keep=[r for r in old if r['asset_id'] not in ids]
with registry.open('w',encoding='utf-8',newline='') as f:
 wr=csv.DictWriter(f,fieldnames=fields);wr.writeheader();wr.writerows(keep+rows)
print('PACKAGED',len(rows),'provenance records')
