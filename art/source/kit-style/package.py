from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,shutil
import numpy as np
R=Path(__file__).resolve().parents[3];V=R/'art/_review/2026-10-27-kit-style';E=R/'art/export/scene-a2-kit-style';S=R/'art/export/scene-a2-kit'
font=lambda s:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',s)
ink='#29464a';paper='#e6e1cf'; rows=[]
tree=Image.open(V/'autumn_tree_01_before.png').convert('RGBA')
def paste(c,im,x,y):c.paste(im,(int(x),int(y)),im)
def label(c,xy,t,s=20):ImageDraw.Draw(c).text(xy,t,font=font(s),fill=ink)
def row(h):return Image.new('RGB',(2400,h),paper)
names=[m['name'] for m in json.loads((E/'metrics.json').read_text()) if 'triangles' in m and m['name'] not in ['fence_01','pole_01']]
c=row(max(1280,tree.height+100));paste(c,tree,12,c.height-60-tree.height);label(c,(15,c.height-45),'기준 나무 · 원본 13.5m / 동일 64px/m',21)
for j,name in enumerate(names):
 x=950+(j%3)*475; y=25+(j//3)*300;label(c,(x,y),name,23)
 for z,var in enumerate(['before','after']):
  im=Image.open(V/(name+'_'+var+'.png')).convert('RGBA');paste(c,im,x+z*220,y+230-im.height);label(c,(x+z*220,y+245),'전' if z==0 else '후',19)
rows.append(c)
for name in ['fence_01','pole_01']:
 ims=[Image.open(V/(name+'_'+v+'.png')).convert('RGBA') for v in ['before','after']]
 if name=='fence_01':
  c=row(340);label(c,(30,15),'fence_01 · 원형 길이 유지',23)
  for j,im in enumerate(ims):paste(c,im,30+j*1200,270-im.height);label(c,(30+j*1200,295),'전' if j==0 else '후')
  rows.append(c)
 else:
  for j,im in enumerate(ims):
   c=row(max(600,im.height+80));label(c,(20,15),'pole_01 · 전신주 3개 + 연결 전선 원형 전체 / '+('전' if j==0 else '후'),23);paste(c,im,20,60);rows.append(c)
c=row(420);label(c,(20,15),'덤불 · 둔덕 · 바위 — 적용 방식 미리보기 (Unity 원본 메시가 아닌 동일 대용 메시 전/후)',25)
for j,name in enumerate(['bush','mound','rock']):
 x=30+j*790
 for z,var in enumerate(['before','after']):
  im=Image.open(V/(name+'_proxy_'+var+'.png')).convert('RGBA');paste(c,im,x+z*330,95);label(c,(x+z*330,320),name+' '+('전' if z==0 else '후'),20)
 tex=Image.open(E/(name+'_style.png')).convert('RGBA').resize((64,64),Image.Resampling.NEAREST);paste(c,tex,x,350)
 label(c,(x+80,360),'32×32 / 3색 / 반복 2.909m',18)
rows.append(c)
c=row(280)
for i,t in enumerate(['수치 표 | 대상                           텍셀 크기                      색 수                 명암 / 접지',
 '소품 14종                                9.09cm 월드 셀               각 4색                3단 / 1단',
 '덤불·둔덕·바위 3장                     32px ÷ 2.909m = 11/m        각 3색                3단 / 적용 메모 참고',
 '나무 기준                               원본 UV·PNG 유지             기존 팔레트            원본 구운 명암 유지',
 '공통: 실제 FBX · 전/후 동일 정사영 카메라 · 64px/m · Unity 조명·안개 미포함 · 도현 승인 필요']):label(c,(24,15+i*48),t,22)
rows.append(c)
sheet=row(145+sum(c.height for c in rows));label(sheet,(24,15),'환경 소품·덤불 그림체 비교 — D-175 / 아트 1',34);label(sheet,(24,70),'공용 style32 · 사진 무늬 제거 · 원형/UV/알파 유지 · 위/왼쪽 전 → 아래/오른쪽 후',23)
y=145
for c in rows:sheet.paste(c,(0,y));y+=c.height
sheet.save(V/'sheet.png')
small=sheet.copy();small.thumbnail((1000,2000));small.save(V/'sheet-small.png')
# Separate enlarged evidence; never confused with the same-scale master sheet.
detail=Image.new('RGB',(1800,1500),paper);label(detail,(20,15),'소품 확대 확인 · 위 전 / 아래 후 · 각 렌더 3배 확대',28)
for i,name in enumerate(['drum_01','drum_03','tires_02','pot_01','bicycle_01','bench_01']):
 x=20+(i%3)*600;y=80+(i//3)*690;label(detail,(x,y),name,23)
 for j,var in enumerate(['before','after']):
  im=Image.open(V/(name+'_'+var+'.png')).convert('RGBA');im=im.resize((im.width*3,im.height*3),Image.Resampling.NEAREST);paste(detail,im,x,y+45+j*305)
detail.save(V/'detail.png')
(V/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>환경 소품 그림체 검토</title><style>body{background:#e6e1cf;color:#29464a;font:18px sans-serif;margin:24px}img{max-width:100%}a{color:#29464a}</style><h1>환경 소품·덤불 — 도현 승인 필요</h1><p>같은 FBX·같은 카메라·64px/m. 이미지를 클릭하면 원본 크기. 자연 3종은 대용 메시이며 Unity 적용 결과가 아닙니다.</p><p><a href="detail.png">소품 확대 전/후</a> · <a href="tree-game-reference.png">게임 컷의 기준 나무 (별도 화면 축척)</a></p><a href="sheet.png"><img src="sheet.png"></a><p><a href="../../export/scene-a2-kit-style/APPLY.md">적용 메모</a></p></html>''',encoding='utf-8')
# Verify every delivered pixel belongs to the frozen shared palette and originals still match.
pal={tuple(map(int,l.split()[:3])) for l in (E/'style32-snapshot.gpl').read_text(encoding='utf-8-sig').splitlines() if l and l[0].isdigit()};checks=[]
for m in json.loads((E/'metrics.json').read_text()):
 p=E/(m['name']+'.png');a=np.array(Image.open(p).convert('RGBA'));rgb={tuple(x) for x in np.unique(a[:,:,:3].reshape(-1,3),axis=0)}
 check=dict(name=m['name'],palette_pass=rgb<=pal,no_black=(0,0,0) not in rgb,colors=len(rgb),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
 if 'fbx_sha256' in m:
  old=np.array(Image.open(S/p.name).convert('RGBA'));check.update(alpha_identical=bool(np.array_equal(a[:,:,3],old[:,:,3])),size_identical=a.shape==old.shape,source_png_unchanged=hashlib.sha256((S/p.name).read_bytes()).hexdigest()==m['source_png_sha256'],source_fbx_unchanged=hashlib.sha256((S/(m['name']+'.fbx')).read_bytes()).hexdigest()==m['fbx_sha256'])
 # Isolated RGBA sample: pixel unlike all four direct neighbours. Atlas borders excluded.
 valid=(a[:,:,3]>127);same=np.zeros(valid.shape,bool);interior=valid.copy()
 for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
  b=np.roll(a,(dy,dx),(0,1));same|=(a[:,:,:3]==b[:,:,:3]).all(2);interior&=b[:,:,3]>127
 interior[[0,-1],:]=False;interior[:,[0,-1]]=False
 check['isolated_opaque_pixel_percent']=float(100*np.sum(interior&~same)/max(1,np.sum(interior)))
 checks.append(check)
(E/'validation.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copyfile(S/'CREDITS.md',E/'CREDITS.md')
with (E/'CREDITS.md').open('a',encoding='utf-8') as f:f.write('\n## STYLE derivative (2026-10-27)\n14 prop textures repainted with deterministic world-grid raster painting; existing geometry, UV and alpha retained. Shared style32 palette. No generative AI image tool used. New bush/mound/rock tiles are deterministic procedural artwork. Original model credits and licenses above remain applicable.\n')
print('sheet',sheet.size,'checks',len(checks),'max isolated',max(c['isolated_opaque_pixel_percent'] for c in checks))
