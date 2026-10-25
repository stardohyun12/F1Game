"""Actual-render contact sheets only; never synthesizes a background or geometry."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,math
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'art/_review/2026-10-25-bayloop-variety';O=ROOT/'art/models/bayloop_variety_v01'
assets=json.loads((O/'manifest.json').read_text(encoding='utf8'))['assets'];assets=sorted(assets,key=lambda a:(list(dict.fromkeys(a['type'] for a in assets)).index(a['type']),a['id']))
delivery=json.loads((R/'trial-delivery.json').read_text(encoding='utf8')) if (R/'trial-delivery.json').exists() else None
font=ImageFont.truetype(str(ROOT/'F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf'),18)
cols=9;cw=320;ch=202;rows=math.ceil(len(assets)/cols);board=Image.new('RGB',(cols*cw,rows*ch+40),(229,226,218));draw=ImageDraw.Draw(board)
draw.text((12,8),f'D-140 / {len(assets)} variants / actual Blender renders / 32 px per metre / 4 shared atlases',font=font,fill='#242424')
for j,a in enumerate(assets):
 x=j%cols*cw;y=j//cols*ch+40;im=Image.open(R/(a['id']+'_front.png')).convert('RGB').resize((320,180),Image.Resampling.NEAREST);board.paste(im,(x,y));draw.text((x+6,y+180),a['id'],font=font,fill='#242424')
board.save(R/'grid-full.png');board.resize((960,round(board.height*960/board.width)),Image.Resampling.NEAREST).save(R/'grid.png')
types={}
for a in assets:types.setdefault(a['type'],[]).append(a)
reps={};sections=[]
for typ,group in types.items():
 n=len(group);indices=[1,6,12] if n>=12 else [1,2,4] if n>=4 else [1]
 selected=[a for a in group if int(a['id'].rsplit('_',1)[1]) in indices];reps[typ]=[a['id'] for a in selected]
 strip=Image.new('RGB',(960*len(selected),575),(229,226,218));dr=ImageDraw.Draw(strip)
 for j,a in enumerate(selected):strip.paste(Image.open(R/(a['id']+'_front.png')).convert('RGB'),(j*960,35));dr.text((j*960+12,8),a['id'],font=font,fill='#242424')
 strip.save(R/(typ+'_representatives.png'))
 cards=[]
 for a in group:
  p=a['parameters'];links=' · '.join(f'<a href="{a["id"]}_{v}.png">{v}</a>' for v in ['front','back','side','top'] if (R/(a['id']+'_'+v+'.png')).exists())
  status=('시험 투입 · 인게임 승인 대기' if a['id'] in delivery['accepted'] else '보류 · F1 미투입') if delivery else '검토 중'
  cards.append(f'<article><h3>{a["id"]}</h3><strong>{status}</strong><img loading="lazy" src="{a["id"]}_front.png"><p>{a["tris"]} tris · {p["width"]}×{p["depth"]}m · {p["floors"]}F · {p["zone"]}<br>{p["roof"]} / {p["wall_color"]}<br>{links}</p></article>')
 sections.append(f'<h2>{typ} · {len(group)} variants</h2><a href="{typ}_representatives.png">대표 표본</a><div class="grid">'+''.join(cards)+'</div>')
(R/'representatives.json').write_text(json.dumps(reps,indent=2),encoding='utf8')
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><title>D140 — 같은 톤, 많은 변형</title><style>body{background:#e5e2da;color:#252b2c;font:16px/1.6 system-ui;max-width:1600px;margin:24px auto;padding:20px}img{max-width:100%}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:20px}article{border-top:1px solid #999}h3{font-size:17px}a{color:#2e5966}@media(max-width:900px){.grid{grid-template-columns:1fr}}</style><h1>D140 — 같은 톤, 많은 변형</h1><p>실제 bpy 형상·사진 기반 공용32색·세계32px/m·공유 아틀라스4장. AI 이미지0. Unity 캡처가 아닌 정적 렌더이며, 최종 게임 승인과 구별한다.</p><p><a href="../../../docs/team/reports/art-20261025-2023.md">보고서</a> · <a href="references.md">참고·라이선스·재현 범위</a> · <a href="../../../art/models/bayloop_variety_v01/SPEC.md">입력·피벗·구역 계약</a> · <a href="gate-summary.json">독립 type/톤 판정</a> · <a href="checks.json">GLB 전수검사</a> · <a href="fbx-checks.json">FBX 전수검사</a> · <a href="trial-delivery.json">시험 투입/보류</a></p><h2>전 변형 — 폭960 축소</h2><a href="grid-full.png"><img src="grid.png"></a><p>원본 격자를 클릭하면 상세 확인. 축소 격자 자체로 개별 창·문·마모까지 판정할 수 없다. 단일 시설은 같은 모델3개 복제 대신 실제1개+3방향으로 검토한다.</p>'''+''.join(sections)+'</html>'
(R/'index.html').write_text(html,encoding='utf8');print('REVIEW_GRID',len(assets),board.size)
