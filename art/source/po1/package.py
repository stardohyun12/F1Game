from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,re
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-a2-kit-po1';K=R/'art/export/kart-v6e';V=R/'art/_review/2026-10-27-po1';stats=json.loads((E/'texture-validation.json').read_text());sources=json.loads((W/'sources.json').read_text());g=json.loads((K/'geometry-validation.json').read_text())
for name,p in sources.items():assert hashlib.sha256((R/p).read_bytes()).hexdigest()==stats[name]['source_sha256']
g['sha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [K/'kart_v6e.fbx',K/'kart_v6e.png']};(K/'validation.json').write_text(json.dumps(g,indent=2))
# Read-only material audit; record which slots QA must replace.
bindings={}
for p in (R/'F1/Assets/Art/SceneA6d/Mats').glob('*.mat'):
 if not any(n in p.stem for n in sources):continue
 s=p.read_text(encoding='utf-8-sig');bindings[p.name]={k:re.search(r'- '+re.escape(k)+r': ([^\r\n]+)',s).group(1) for k in ['_R2','_HasR2','_Sat','_Ramp','_R1Ok'] if re.search(r'- '+re.escape(k)+r': ([^\r\n]+)',s)}
(E/'material-audit.json').write_text(json.dumps(bindings,indent=2))
im=Image.new('RGB',(2400,2940),'#e6e1ce');d=ImageDraw.Draw(im)
def text(x,y,s,size=28):d.text((x,y),s,font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',size),fill='#343c3b')
def pic(name,x,y,w,h):
 a=Image.open(V/name).convert('RGBA');a.thumbnail((w,h),Image.Resampling.LANCZOS);im.paste(a,(x+(w-a.width)//2,y+(h-a.height)//2),a)
text(24,16,'도현 페인트오버 1  /  D1 · D3/D6 · D5 · D7 — 도현 승인 필요',40)
text(24,76,'모든 쌍 동일 형상·UV·카메라 / Blender 참고 / 실제 씬 색 보정·안개·그레인은 재현하지 않음',25)
for i,(asset,title) in enumerate([('transformer_01','D1 전기함'),('tires_01','D5 타이어 01')]):
 for j,tag in enumerate(['before','after']):x=i*1200+j*600;text(x+20,130,title+(' — 전' if j==0 else ' — 후'));pic(asset+'_'+tag+'.png',x,175,600,470)
for i,(asset,title) in enumerate([('tires_02','D5 타이어 02'),('kart','D7 카트')]):
 for j,tag in enumerate(['before','after']):
  x=i*1200+j*600;text(x+20,670,title+(' — 전' if j==0 else ' — 후'));pic((asset+'_'+tag+'.png') if asset!='kart' else ('v6d_close.png' if j==0 else 'v6e_close.png'),x,715,600,470)
text(24,1210,'D7 카메라 B 전 / 후 — 뒤 5.6m · 높이 2.8m · 수직 FOV 64° · 기존 정비소 미리보기 환경',27)
pic('v6d_camera_B.png',0,1260,1200,675);pic('v6e_camera_B.png',1200,1260,1200,675)
text(24,1955,'D3/D6 나무 5그루 — 각 칸 왼쪽 전 / 오른쪽 후 · 아래는 기둥 근접 · 잎/알파 보존',27)
for i in range(5):
 name=f'autumn_tree_{i+1:02d}';x=i*480;text(x+16,2005,f'나무 {i+1:02d}',26)
 for j,tag in enumerate(['before','after']):pic(name+'_'+tag+'.png',x+j*240,2050,240,360);pic(name+'_'+tag+'_trunk.png',x+j*240,2425,240,240)
text(24,2700,'검증: 나무 잎·알파 동일 / 타이어 RGB 40·70·104 / 카트 차체 채도 0.753→0.838',26)
text(24,2750,'차체 명도 P90−P10 87.99→101.13 (+14.9%) / 카트 1,980 tris·치수·바퀴·UV 유지',26)
text(24,2800,'적용 주의: 현재 나무는 BaseMapR2 활성. 새 PNG를 활성 슬롯에 연결해야 갈색 줄기가 반영됨.',26)
text(24,2850,'도현 승인 필요 · QA/프로그래밍 임포트 필요 · Unity 미실행 · 결함 완료 판정은 게임 컷/플레이 뒤',26)
im.save(V/'sheet.png');im.thumbnail((1200,1470));im.save(V/'sheet-small.png')
(V/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>페인트오버 1 수정</title><style>body{background:#e6e1ce;color:#343c3b;font:18px sans-serif;margin:24px}img{max-width:100%}a{color:inherit}</style><h1>페인트오버 1 — 도현 승인 필요</h1><p>Blender 참고 / Unity 미실행 / QA·프로그래밍 임포트 필요.</p><p><a href="../../../docs/team/reports/art-20261027-1902.md">결함별 보고</a> · <a href="../../export/scene-a2-kit-po1/APPLY.md">재질 슬롯 적용 메모</a> · <a href="v6e_camera_B.png">카트 B 1280×720</a></p><a href="sheet.png"><img src="sheet.png"></a></html>''',encoding='utf-8')
print('PACKAGED',g['tris'],bindings)
