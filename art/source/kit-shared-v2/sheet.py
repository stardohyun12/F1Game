from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[3];E=R/'art/export/kit-shared-v2';V=R/'art/_review/2026-10-28-kit-shared-v2';O=R/'art/_review/2026-10-28-kit-shared-v1'
a=json.loads((V/'audit.json').read_text());old=json.loads((O/'audit.json').read_text());rows=[a['rows'][2],old['rows'][3],a['rows'][3]]
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
s=Image.new('RGB',(2640,2400),(230,226,211));d=ImageDraw.Draw(s)
def text(x,y,t,n=28):d.text((x,y),t,font=font(n),fill=(43,47,43))
text(24,20,'공용 재질 키트 v2 — 사진 결 복원 / 빛·AO 굽기 없음',40)
text(24,80,'기준 / v1 / v2 · 같은 표시 크기 · v1과 v2는 형태·UV·조명·카메라 동일',26)
for i,(label,path) in enumerate([('기준 게임 화면',O/'reference_720.png'),('v1',O/'assembly_720.png'),('v2',V/'assembly_720.png')]):
 x=20+880*i;text(x,130,label);s.paste(Image.open(path).resize((860,484),Image.Resampling.NEAREST),(x,180))
text(24,695,'고정 건축 크롭 — 원본 375×240 → 640×410, 노출 보정 없음',28)
for i,path in enumerate([V/'reference_architecture.png',O/'proof_architecture.png',V/'proof_architecture.png']):s.paste(Image.open(path),(20+880*i,750))
for i,m in enumerate(rows):
 x=20+880*i;text(x,1190,f"단색 면 {m['face_uniform_frac']:.2%}",27);text(x,1230,f"면 변화 {m['face_std_median']:.4f}",27)
text(24,1300,'확정 범위: 단색 면 48.67~58.67% / 면 변화 0.8300~1.2451',28)
text(24,1350,'수치 판정: '+('2/2 통과' if all(a['passed'].values()) else '범위 밖 — 통과 보류'),30)
text(24,1420,'v2 아틀라스 · 512×512 · 회벽/트림/바닥만 갱신 · 지붕/데칼은 v1 유지',27)
for i,name in enumerate(['wall','trim','floor','roof','decals']):
 im=Image.open(E/(name+'.png')).convert('RGBA');s.paste(im,(20+i*524,1480),im if name=='decals' else None);text(20+i*524,2005,name,25)
text(24,2070,'큰 그늘 제거 sigma 1m / 사진 중간 결 유지 / 명도 변화 상한 ±4% / 디더 없음',27)
text(24,2120,'창틀 청록 한 단계 바램 · 32px/m · 원본 사진과 UV 배치 유지',27)
text(24,2180,'Blender: 해 1 + 하늘빛 근사. 실제 Unity 조명·카메라 최종 판정은 미실행.',27)
text(24,2240,'도현 승인 필요 · QA/프로그래밍 임포트 필요',30)
s.save(V/'sheet.png')
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>공용 재질 키트 v2</title><style>body{background:#e6e2d3;font:18px sans-serif;margin:24px}img{max-width:100%;image-rendering:pixelated}</style><h1>공용 재질 키트 v2 — 도현 승인 필요</h1><a href="audit.json">수치 기록</a><p><a href="sheet.png"><img src="sheet.png"></a>',encoding='utf-8')
