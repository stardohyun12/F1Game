"""Build review pages from actual Blender renders; no generated imagery."""
from pathlib import Path
import json,html
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/_review/2026-10-25-bayloop-buildings'
SRC=ROOT/'art/models/bayloop_buildings_v01'
font=ImageFont.truetype(str(ROOT/'F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf'),25)
manifest=json.loads((SRC/'manifest.json').read_text(encoding='utf8'))
gates=json.loads((OUT/'gate-summary.json').read_text(encoding='utf8'))
blocks=[]
for typ,old in [('shop_house','shop'),('terrace_house','terrace'),('harbor_warehouse','warehouse')]:
 im=Image.new('RGB',(1920,590),(235,232,223));d=ImageDraw.Draw(im)
 d.text((20,10),'기존 LofiPilot — 원래 크기 유지',font=font,fill='#333333')
 d.text((980,10),typ+' a — 실제 bpy 렌더',font=font,fill='#333333')
 for x,path in [(0,OUT/f'legacy_{old}.png'),(960,OUT/f'{typ}_a_front.png')]:im.paste(Image.open(path).convert('RGB'),(x,50))
 im.save(OUT/f'{typ}_compare.png')
 cards=[]
 for a in [a for a in manifest['assets'] if a['type']==typ]:
  name=a['id'];g=gates[name]
  views=''.join(f'<a href="{name}_{v}.png"><img src="{name}_{v}.png" alt="{name} {v}"></a>' for v in ['front','back','side','top'])
  cards.append(f'<article><h3>{name} · {a["tris"]} tris · {g["score"]}/10</h3><p>본체 {a["footprint_m"]} m · {a["storeys"]}층 · <a href="{g["report"]}">독립 판정 원문</a></p><div class="views">{views}</div></article>')
 blocks.append(f'<section><h2>{typ}</h2><img class="wide" src="{typ}_compare.png" alt="기존·새 모델 같은 조명과 카메라 비교"><img class="wide" src="{typ}_pair.png" alt="변형 나란히">'+''.join(cards)+'</section>')
sheet=Image.new('RGB',(1920,3*580),(235,232,223));d=ImageDraw.Draw(sheet)
for i,a in enumerate(manifest['assets']):
 x=(i%2)*960;y=(i//2)*580;g=gates[a['id']]
 d.text((x+16,y+7),f'{a["id"]} / {a["tris"]} tris / {g["score"]}/10',font=font,fill='#333333')
 sheet.paste(Image.open(OUT/f'{a["id"]}_front.png').convert('RGB'),(x,y+40))
sheet.save(OUT/'all-buildings.png')
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><title>BayLoop 건물 3종 · 시험 투입 검토</title><style>body{margin:32px auto;padding:0 20px;max-width:1440px;background:#eae7df;color:#292c29;font:17px/1.6 system-ui}a{color:#315d6b}h1,h2,h3{line-height:1.3}section{border-top:1px solid #888;margin-top:40px}.wide{display:block;max-width:100%;height:auto;margin:16px auto}.views{display:grid;grid-template-columns:1fr 1fr;gap:12px}.views img{width:100%}article{margin-bottom:28px}code{font-size:14px}</style><h1>BayLoop 건물 3종 / 6변형</h1><p>시험 투입용 실제 Blender 렌더. AI 이미지·배경 0. Unity 실행·인게임 배치는 미실행이며, 최종 승인은 프로그래밍 임포트 후 인게임 캡처로 받는다.</p><p>세계 32px/m · 공용 32색 팔레트(조명 전 albedo 검사) · 바닥 앞면 가운데 피벗 · 앞면 +Z · 1단위 1m. 비교는 같은 조명·카메라이며 기존 키트도 원래 크기 그대로다. 레거시 모델의 확대 보정은 하지 않았다.</p><p><a href="checks.json">GLB 수치 검사</a> · <a href="fbx-checks.json">FBX 재임포트 검사</a> · <a href="references/REFERENCES.md">실물 사진·출처</a> · <a href="../../models/bayloop_buildings_v01/SPEC.md">인계 규격</a> · <a href="../../../docs/team/reports/art-20261025-1645.md">완료 보고</a></p><img class="wide" src="all-buildings.png" alt="6채 전체">'''+''.join(blocks)+'''<p>960×540 개별 PNG는 클릭해 원본 확인. 단독 렌더는 실제 게임 카메라 거리·가림·빛 환경을 입증하지 않는다. 모든 독립 게이트의 잔여 결함은 원문과 보고서에 보존한다.</p></html>'''
(OUT/'index.html').write_text(page,encoding='utf8')
print('REVIEW_READY',OUT)
