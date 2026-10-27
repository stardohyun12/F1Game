from pathlib import Path
import json,sys,importlib.util,shutil
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/q3-depot';V=R/'art/_review/2026-10-28-q3-depot';O=R/'art/_review/2026-10-28-kit-shared-v2'
attempt=int(sys.argv[1]) if len(sys.argv)>1 else 1
for n in ['15m','5m','inside']:Image.open(V/(n+'.png')).resize((1280,720),Image.Resampling.NEAREST).save(V/(n+'_720.png'))
ref=Image.open(O/'reference_720.png');ref.save(V/'reference_720.png')
# Selected visually before first measurement. Fixed for all iterations.
rois={'reference':[580,195,955,435],'depot_5m':[650,235,1025,475]}
ref.crop(rois['reference']).resize((640,410),Image.Resampling.NEAREST).save(V/'reference_architecture.png')
Image.open(V/'5m_720.png').crop(rois['depot_5m']).resize((640,410),Image.Resampling.NEAREST).save(V/'depot_architecture.png')
spec=importlib.util.spec_from_file_location('audit',R/'docs/qa/tmp/audit_lite.py');audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
rows=[]
for n in ['reference','depot']:
 m,_=audit.measure(n,(V/(n+'_architecture.png')).relative_to(R).as_posix());rows.append({k:m[k] for k in ['name','path','face_uniform_frac','face_std_median']});print(rows[-1],flush=True)
limits={'face_uniform_frac':[.4867,.5867],'face_std_median':[.83,1.25]};passed={k:lo<=rows[1][k]<=hi for k,(lo,hi) in limits.items()}
a={'attempt':attempt,'rows':rows,'limits':limits,'passed':passed,'rois_at_1280':rois,'source_roi_size':[375,240],'display_size':[640,410],'scope':'architecture only; bottom emptiness and brightness spread excluded'}
for n in ['audit.json',f'audit-attempt-{attempt}.json']:(V/n).write_text(json.dumps(a,indent=2))
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
s=Image.new('RGB',(2640,2680),(230,226,211));d=ImageDraw.Draw(s)
def txt(x,y,t,n=28):d.text((x,y),t,font=font(n),fill=(43,47,43))
txt(24,20,'Q3 · BAY PARCEL — 동네 택배 보관소 / 공용 키트 v2만 사용',40)
txt(24,80,'텍스처 새로 그리지 않음 · 간판 글자는 메시 · albedo만 / 빛·AO 굽기 없음',27)
for label,path,x,y in [('기준 게임 화면',V/'reference_720.png',20,140),('정면 15m · 높이 2.8m',V/'15m_720.png',1340,140),('셔터 앞 5m · 3/4',V/'5m_720.png',20,940),('셔터 안쪽',V/'inside_720.png',1340,940)]:
 txt(x,y,label);s.paste(Image.open(path),(x,y+50))
txt(24,1760,'건축 크롭: 같은 375×240 픽셀 → 640×410 표시 · 좌표 고정 · 노출 보정 없음',28)
s.paste(Image.open(V/'reference_architecture.png'),(20,1820));s.paste(Image.open(V/'depot_architecture.png'),(700,1820))
for i,m in enumerate(rows):
 txt(1390,1830+i*100,f"{m['name']}: 단색 면 {m['face_uniform_frac']:.2%}",28);txt(1390,1870+i*100,f"면 변화 {m['face_std_median']:.4f}",28)
txt(1390,2060,'범위: 48.67~58.67% / 0.83~1.25',26)
txt(1390,2120,'수치: '+('2/2 통과' if all(passed.values()) else '범위 밖 — 승인 보류'),30)
txt(24,2280,'본체 10×7×4m · 정면 +Z · 피벗 바닥 앞면 가운데 · 중앙 폭 5.2m × 앞 4m 비움',27)
txt(24,2330,'원본 PNG 5장 그대로 공유 / UV 32px·m / 표지·간판·소품은 기존 칸만 참조',27)
txt(24,2380,'Blender 해 1 + 하늘빛 근사 · 1600×900 → 1280×720 Nearest · Unity 미실행',27)
txt(24,2440,'수치와 시각 판정은 별도 · 도현 승인 필요 · QA/프로그래밍 임포트 필요',30)
s.save(V/'sheet.png')
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Q3 BAY PARCEL</title><style>body{background:#e6e2d3;font:18px sans-serif;margin:24px}img{max-width:100%;image-rendering:pixelated}</style><h1>Q3 택배 보관소 — 도현 승인 필요</h1><a href="audit.json">수치 기록</a><p><a href="sheet.png"><img src="sheet.png"></a>',encoding='utf-8')
print('PACKAGE',passed,flush=True)
