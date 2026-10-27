from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import numpy as np,json,hashlib,csv,sys,shutil
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-albedo-kit';V=R/'art/_review/2026-10-28-q0';stage=sys.argv[1] if len(sys.argv)>1 else 'r1';D=V/stage;names=['vending_G1','vending_G2','ac_G3','postbox_G1','tin_F3'];checks=[]
for n in names:
 for tag in ['before','after']:
  p=D/(n+'_'+tag+'_game.png');im=Image.open(p).convert('RGB');im.resize((1280,720),Image.Resampling.NEAREST).save(D/(n+'_'+tag+'_720.png'))
sheet=Image.new('RGB',(2500,2320),(234,230,218));dr=ImageDraw.Draw(sheet);font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
def label(x,y,t,n=24):dr.text((x,y),t,font=font(n),fill=(45,49,47))
def panel(p,box,nearest=False):
 im=Image.open(p).convert('RGB');im.thumbnail((box[2],box[3]),Image.Resampling.NEAREST if nearest else Image.Resampling.LANCZOS);sheet.paste(im,(box[0]+(box[2]-im.width)//2,box[1]+(box[3]-im.height)//2))
label(30,18,'Q0 · 작은 물건 정면 재도색 / F3 바랜 파랑',40);label(30,78,'좌: 이전 albedo / 우: 수정 albedo · 해 1 + 하늘빛은 렌더에만 · Unity 미실행',26)
for i,n in enumerate(names):
 x=i*500+12;label(x,140,n,28)
 for j,(view,y) in enumerate([('front',200),('three',720)]):
  label(x,y-25,'정면' if j==0 else '3/4',23)
  for k,tag in enumerate(['before','after']):panel(D/(n+'_'+tag+'_'+view+'.png'),(x+k*242,y,235,470))
 label(x,1230,'8m · 1280×720 전체 프레임',21)
 for k,tag in enumerate(['before','after']):panel(D/(n+'_'+tag+'_720.png'),(x,1270+k*180,475,170),True)
 label(x,1650,'같은 720 컷 중앙 크롭 · 원배율',20)
 for k,tag in enumerate(['before','after']):
  im=Image.open(D/(n+'_'+tag+'_720.png'));crop=im.crop((520,240,760,480)).resize((240,240),Image.Resampling.NEAREST);sheet.paste(crop,(x+k*242,1700))
 label(x,1980,{'vending_G1':'76×114 정면 / 64.0~64.4 texel·m','vending_G2':'76×114 정면 / 64.0~64.4 texel·m','ac_G3':'52×32 정면 / 64~65 texel·m','postbox_G1':'유지 · 비교용 / 기존 UV','tin_F3':'32×64 타일 / UV 유지'}[n],20)
 label(x,2020,'UV 교체 · FBX+PNG 함께' if n in names[:3] else '사진색 유지 / 조명 베이크 없음',20)
label(30,2130,'참고 게이트 시트 — 최종 PD/도현 승인 및 QA·프로그래밍 임포트 필요',27);label(30,2180,'정면은 2~4텍셀 덩어리로 재도색. F3 RGB 82/140/187 중심, 약 ±4% 재료 결.',24)
sheet.save(D/'sheet.png');shutil.copy2(D/'sheet.png',V/'sheet.png')
cards=''.join('<h2>'+n+'</h2>'+''.join('<p>'+v+'</p><img src="'+stage+'/'+n+'_before_'+v+'.png"><img src="'+stage+'/'+n+'_after_'+v+'.png">' for v in ['front','three','720']) for n in names)
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Q0 review</title><style>body{background:#eae6da;font:18px sans-serif;margin:24px}img{width:48%;image-rendering:pixelated}</style><h1>Q0 — 도현 승인 필요</h1><p>왼쪽 이전 / 오른쪽 수정. Blender 해+하늘빛 근사, Unity 미실행.</p><a href="sheet.png">전체 시트</a>'+cards,encoding='utf-8')
targets=names[:3]+['tin_F3','garage_tin_F3_atlas']
for n in targets:
 p=E/(n+'.png');a=np.array(Image.open(p).convert('RGB'));packed=(a[:,:,0].astype('uint32')<<16)|(a[:,:,1].astype('uint32')<<8)|a[:,:,2];pal=np.unique(packed);(E/(n+'.gpl')).write_text('GIMP Palette\nName: Q0 '+n+' sampled albedo\nColumns: 8\n#\n'+''.join(f'{int(v)>>16} {(int(v)>>8)&255} {int(v)&255} q0\n' for v in pal))
 checks.append(dict(file=p.name,size=[a.shape[1],a.shape[0]],colors=len(pal),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),albedo=True,baked_light=False,uv_changed=n in names[:3]))
f=np.array(Image.open(E/'tin_F3.png').convert('RGB'));assert np.array_equal(f[:,0],f[:,-1]) and np.array_equal(f[0],f[-1]);assert np.all(f[:,:,2]>f[:,:,0]+80)
assert (E/'postbox_G1.png').read_bytes()==(W/'before/postbox_G1.png').read_bytes()
for n in names[:3]:assert (E/(n+'.png')).read_bytes()==(E/(n+'.fbm')/(n+'.png')).read_bytes()
(E/'q0-validation.json').write_text(json.dumps(dict(stage=stage,textures=checks,postbox_png_identical=True,f3_blue=True,f3_tile_edges_identical=True,geometry=json.loads((W/'geometry-validation.json').read_text()),lighting=json.loads((D/'lighting.json').read_text())),indent=2))
# Refresh current hashes; keep historical D185 evidence clearly labelled as superseded.
p=E/'validation.json';old=json.loads(p.read_text());changed={c['file']:c for c in checks}
for c in old['files']:
 if c['file'] in changed:c.update(changed[c['file']]);c['q0_update']='2026-10-28';c['source']='art/source/q0/before/'+c['file'];c['source_sha256']=hashlib.sha256((W/'before'/c['file']).read_bytes()).hexdigest()
old['fbx_count']=4;old['q0_current']='q0-validation.json';p.write_text(json.dumps(old,indent=2))
p=E/'final-check.json';old=json.loads(p.read_text());old['superseded_targets_by_q0']=targets;old['current_q0_validation']='q0-validation.json';p.write_text(json.dumps(old,indent=2))
registry=R/'art/provenance.csv'
with registry.open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f));fields=list(rows[0])
fresh=[]
for n in targets:
 for ext in (['.png','.fbx'] if n in names[:3] else ['.png']):
  p=E/(n+ext);fresh.append(dict(asset_id='q0_'+n+ext,path=p.relative_to(R).as_posix(),source_url='art/source/q0/palette-samples.json',license='Derived from existing CC0 photo picks; code-drawn geometry/graphics',accessed='2026-10-28',tool='Pillow code painting + Blender UV export; no AI',ai_generated='false',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes='D186 Q0; sampled photo colours only; no lighting bake; previous asset in art/source/q0/before'))
ids={r['asset_id'] for r in fresh}
with registry.open('w',encoding='utf-8',newline='') as f:wr=csv.DictWriter(f,fieldnames=fields);wr.writeheader();wr.writerows([r for r in rows if r['asset_id'] not in ids]+fresh)
with (E/'q0-provenance.csv').open('w',encoding='utf-8',newline='') as f:wr=csv.DictWriter(f,fieldnames=fields);wr.writeheader();wr.writerows(fresh)
print('Q0 PACKAGE',stage,'5 modified PNG / 3 UV-updated FBX / postbox retained')
