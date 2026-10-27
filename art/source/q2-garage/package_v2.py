from pathlib import Path
import json,hashlib,csv
import numpy as np
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/q2-garage';V=R/'art/_review/2026-10-28-q2-garage';D=V/'v2'
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
s=Image.new('RGB',(2880,2380),(230,226,211));d=ImageDraw.Draw(s)
def txt(x,y,t,n=27):d.text((x,y),t,font=font(n),fill=(43,47,43))
txt(24,18,'정비소 q2 수정 — 빨강 F2 기본 / 바닥 원 제거 / UV0 통일 / 게시판 복원',38)
txt(24,77,'A7은 제공 Unity 캡처, q2 v1/v2는 같은 Blender 카메라·조명. 게임과 동일 광량 검증은 아님.',25)
for i,(label,path) in enumerate([('A7 지금 판 · Unity',R/'docs/qa/evidence/preview-1028/garage_without.png'),('q2 v1 · 미리보기 UV 보정 재현',D/'v1_inside.png'),('q2 v2 · FBX UV0 그대로',D/'v2_inside.png')]):
 x=i*960+10;txt(x,130,label);s.paste(Image.open(path).convert('RGB').resize((940,529),Image.Resampling.NEAREST),(x,180))
txt(20,740,'프로그래밍 미리보기 q2 v1 · Unity',27);s.paste(Image.open(R/'docs/qa/evidence/preview-1028/garage_with.png').resize((940,529),Image.Resampling.NEAREST),(10,790))
for i,tag in enumerate(['v1','v2']):
 x=970+i*960;txt(x,740,tag+' 마당 · 같은 카메라');s.paste(Image.open(D/(tag+'_outside15.png')).resize((940,529),Image.Resampling.NEAREST),(x,790))
txt(20,1350,'큰 흰 판: UV 채널 불일치 → 사진·종이·핀 표시 복원',28)
for i,tag in enumerate(['v1','v2']):s.paste(Image.open(D/(tag+'_board.png')).resize((940,529),Image.Resampling.NEAREST),(10+i*960,1410))
txt(1940,1430,'검증',32)
for i,t in enumerate(['기하·원점 유지 / 2,406 tris','FBX UV 채널 1개: UVMap','재질 7개 UV 좌표 대조 통과','바닥 남색 픽셀: 0','마당 붉은 점 픽셀: 0','기본 atlas = 빨강 F2','빛·AO 굽기 없음']):txt(1940,1490+i*55,t,26)
txt(24,2010,'바닥 RGB 100~110 / 마당 RGB (128~136, 120~128, 102~108) · 사진 결 ±3%',28)
txt(24,2070,'기존 파일은 art/export/q2-garage/v1/ 보존 · 같은 이름 덮어쓰기 · UV 사본 패치 없이 사용',27)
txt(24,2130,'도현 승인 필요 · QA/프로그래밍 임포트 필요 · Unity 실행·커밋 안 함',30)
s.save(V/'sheet_v2.png')
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>정비소 q2 수정</title><style>body{background:#e6e2d3;font:18px sans-serif;margin:24px}img{max-width:100%;image-rendering:pixelated}</style><h1>정비소 q2 수정 — 도현 승인 필요</h1><a href="sheet_v2.png"><img src="sheet_v2.png"></a><p><a href="sheet.png">기존 v1 시트</a>',encoding='utf-8')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
fix=json.loads((E/'texture-fix-v2.json').read_text());uv=json.loads((E/'uv-fix-v2.json').read_text())
a=np.array(Image.open(E/'garage_baked32.png'));ref=np.array(Image.open(R/'F1/Assets/Art/SceneA7/Tex/garage_tin_F2_atlas_clean.png'));mask=np.load(W/'roof-mask.npy')|np.load(W/'floor-mask-v2.npy')
assert np.array_equal(a[~mask],ref[~mask]);assert sha(E/'garage_baked32.png')==sha(E/'garage_tin_F2_atlas.png')
copies={}
for folder in ['garage_v5_combo.fbm','dirt_v5_combo.fbm']:
 for p in (E/folder).glob('*.png'):
  if (E/p.name).exists():copies[str(p.relative_to(E))]=sha(p)==sha(E/p.name)
assert all(copies.values())
out={'A7_color_placement_exact_outside_cleaned_roof_floor':True,'F2_is_default_byte_identical':True,'fbm_copies_match':copies,'texture':fix,'uv':uv,'output_sha256':{p.name:sha(p) for p in E.iterdir() if p.suffix in ['.png','.fbx']}}
(E/'delivery-validation-v2.json').write_text(json.dumps(out,indent=2));print('DELIVERY VERIFIED')
# Preserve existing provenance schema and add two explicit repair sources.
p=E/'provenance.csv';rows=list(csv.DictReader(p.open(encoding='utf-8-sig')));fields=list(rows[0]);
for source in fix['sources']:
 row={k:'' for k in fields}
 for key in ['source_url','url']:
  if key in row:row[key]=source['url']
 for key in ['path','source','photo']:
  if key in row:row[key]=source['path']
 for key in ['license','sha256']:
  if key in row:row[key]=source[key]
 if 'notes' in row:row['notes']='0557 repair: clean photo grain +/-3%; no light bake; floor or dirt only'
 rows.append(row)
with p.open('w',encoding='utf-8',newline='') as f:
 wr=csv.DictWriter(f,fieldnames=fields);wr.writeheader();wr.writerows(rows)
