from pathlib import Path
import json,importlib.util,sys
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kit-shared-v1';V=R/'art/_review/2026-10-28-kit-shared-v1'
mode=sys.argv[1] if len(sys.argv)>1 else 'final'
def module(path,name):
 s=importlib.util.spec_from_file_location(name,R/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
audit=module('docs/qa/tmp/audit_lite.py' if mode=='final' else 'docs/research/audit_compare.py','audit')
src=Image.open(R/'docs/qa/evidence/scene-a7b/street_trio.png')
# Source is 2880x570, three 960px columns, black label header 31px.
ref=src.crop((0,31,960,570)).resize((1280,720),Image.Resampling.NEAREST);ref.save(V/'reference_720.png')
for name in ['assembly','front','close']:
 Image.open(V/(name+'.png')).resize((1280,720),Image.Resampling.NEAREST).save(V/(name+'_720.png'))
# Fixed architecture ROIs selected before measuring. No exposure/gamma matching.
rois={'reference':[580,195,955,435],'proof':[452,220,827,460]}
ref.crop(rois['reference']).resize((640,410),Image.Resampling.NEAREST).save(V/'reference_architecture.png')
Image.open(V/'front_720.png').crop(rois['proof']).resize((640,410),Image.Resampling.NEAREST).save(V/'proof_architecture.png')
rows=[]
for name,file in [('reference full','reference_720.png'),('kit full','assembly_720.png'),('reference architecture','reference_architecture.png'),('kit architecture','proof_architecture.png')]:
 m,_=audit.measure(name,(V/file).relative_to(R).as_posix());rows.append({k:m[k] for k in ['name','path','face_uniform_frac','face_std_median']});print('AUDIT',rows[-1],flush=True)
base,test=rows[2:];limits={'face_uniform_frac':[max(0,base['face_uniform_frac']-.05),min(1,base['face_uniform_frac']+.05)],'face_std_median':[base['face_std_median']*.8,base['face_std_median']*1.2]}
passed={k:lo<=test[k]<=hi for k,(lo,hi) in limits.items()}
result=dict(measure_tool='audit_lite.py' if mode=='final' else 'audit_compare.py (equivalent diagnostic)',reference_crop_original=[0,31,960,570],architecture_rois_at_1280=rois,architecture_crop_size=[375,240],display_size=[640,410],same_crop_scale=True,rows=rows,provisional_limits=limits,passed=passed,tolerance_status='artist proposal: +/-5 percentage points and +/-20%; no PD reply yet',excluded=['empty_cell_frac_bottom','val_p5_p95'])
(V/('audit.json' if mode=='final' else 'audit-diagnostic.json')).write_text(json.dumps(result,indent=2),encoding='utf-8')
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
s=Image.new('RGB',(2640,2780),(230,226,211));d=ImageDraw.Draw(s)
def txt(x,y,t,n=25):d.text((x,y),t,font=font(n),fill=(43,47,43))
txt(24,18,'공용 재질 키트 v1 · 사진 albedo / 32px·m / 4장 + 알파 데칼',37)
txt(24,75,'왼쪽: 제공 게임 첫 칸 · 오른쪽: 키트만 사용한 6×5×7m 시험 집 · Unity 미실행')
s.paste(ref,(20,120));s.paste(Image.open(V/'assembly_720.png'),(1340,120))
txt(24,860,'재료 4장(512²) + 데칼(512² RGBA) · 같은 팔레트 · 타일은 지정 칸 안에서 반복',27)
for i,name in enumerate(['wall','trim','floor','roof','decals']):
 x=20+i*524;im=Image.open(E/(name+'.png')).convert('RGBA');s.paste(im,(x,920),im if name=='decals' else None);txt(x,1440,name,24)
txt(24,1490,'건축 영역 확대 · 노출 변경 없음 · 면 지표만 비교(크롭 좌표는 audit.json)',27)
s.paste(Image.open(V/'reference_architecture.png'),(20,1540));s.paste(Image.open(V/'proof_architecture.png'),(700,1540))
s.paste(Image.open(V/'close_720.png').resize((900,506),Image.Resampling.NEAREST),(1400,1540))
for i,m in enumerate(rows):txt(25,2080+i*44,f"{m['name']}: 단색 면 {m['face_uniform_frac']:.2%} / 면 변화 {m['face_std_median']:.3f}")
txt(25,2280,'임시 범위: 단색 면 ±5%p / 면 변화 ±20% · PD 확인 전 잠정 판정',26)
txt(25,2330,'수치 결과: '+('2/2 범위 안' if all(passed.values()) else '범위 밖 — 통과 보류'),28)
txt(25,2390,'빛·AO 굽기 없음. 해 1 + 주변광 근사 렌더. 실제 Unity 조명·동일 카메라 판정은 미실행.',25)
txt(25,2440,'회벽: A2 → 살구/크림/연초록 · 지붕: E2/E3/F2/F3 · F3 파랑 보존',25)
txt(25,2490,'트림: 처마/창틀/문틀/걸레받이/홈통/셔터 + 유리/나무 · 데칼: 국소 보수/간판/포스터/번지',25)
txt(25,2550,'도현 승인 필요 · QA/프로그래밍 임포트 필요 · 규칙: art/export/kit-shared-v1/APPLY.md',25)
s.save(V/'sheet.png')
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>공용 재질 키트 v1</title><style>body{background:#e6e2d3;font:18px sans-serif;margin:24px}img{max-width:100%;image-rendering:pixelated}</style><h1>공용 재질 키트 v1 — 도현 승인 필요</h1><p>수치: audit.json / 원근·조명·크롭 차이는 보고서 참조</p><a href="sheet.png"><img src="sheet.png"></a>',encoding='utf-8')
print('PACKAGE DONE',mode,passed,flush=True)
