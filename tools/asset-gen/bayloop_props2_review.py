from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,math
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bayloop_props2_v01';R=ROOT/'art/_review/2026-10-25-bayloop-props2'
m=json.loads((O/'manifest.json').read_text(encoding='utf8'));assets=m['assets'];font=ImageFont.truetype(str(ROOT/'F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf'),19)
gate=json.loads((R/'gates.json').read_text()) if (R/'gates.json').exists() else {}
cols=4;cw=480;ch=300;board=Image.new('RGB',(cols*cw,math.ceil(len(assets)/cols)*ch+45),(226,223,214));d=ImageDraw.Draw(board);d.text((14,10),'D141 / actual Blender renders / 32px per metre',font=font,fill='#222222')
blind=Image.new('RGB',board.size,(226,223,214));bd=ImageDraw.Draw(blind)
for i,a in enumerate(assets):
 x=i%cols*cw;y=i//cols*ch+45;board.paste(Image.open(R/(a['id']+'_front.png')).convert('RGB').resize((480,270),Image.Resampling.NEAREST),(x,y));g=gate.get(a['id'],{});status='PASS' if g.get('yes') and g.get('score',0)>=7 else 'HOLD';d.text((x+10,y+273),f"{a['id']} / {a['tris']} / {status}",font=font,fill='#222222')
 blind.paste(Image.open(R/(a['id']+'_front.png')).convert('RGB').resize((480,270),Image.Resampling.NEAREST),(x,y));bd.text((x+10,y+273),a['id'],font=font,fill='#222222')
board.save(R/'grid-full.png');board.resize((960,round(board.height/2)),Image.Resampling.NEAREST).save(R/'grid.png')
blind.resize((960,round(blind.height/2)),Image.Resampling.NEAREST).save(R/'grid-blind.png')
gate=json.loads((R/'gates.json').read_text()) if (R/'gates.json').exists() else {}
cards=[]
for a in assets:
 name=a['id'];row=gate.get(name);status='검토 중' if not row else f"{'통과' if row['yes'] and row['score']>=7 else '보류'} · {row['score']}/10"
 cards.append(f'<article><h2>{name}</h2><b>{status}</b><a href="{name}_front.png"><img src="{name}_front.png"></a><p>{a["tris"]} tris · {a["dimensions_m"]}m</p><a href="{name}_side.png">side</a> · <a href="{name}_top.png">top</a></article>')
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><title>D141 길가 소품2</title><style>body{background:#e2dfd6;color:#252923;font:16px/1.6 system-ui;max-width:1500px;margin:30px auto;padding:20px}img{max-width:100%}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:20px}article{border-top:1px solid #aaa}h2{font-size:19px}a{color:#305b69}</style><h1>D141 길가 생활 소품2</h1><p>실제 bpy 렌더. AI 이미지0. 정적 검토이며 인게임 승인과 다름. 거리 장면에는 보류된 자동차·삼륜차·빨래도 비교용으로 포함되며, 시험 투입 목록과 구분한다.</p><p><a href="../../../docs/team/reports/art-20261025-2201.md">보고서</a> · <a href="REFERENCES.md">참고·출처</a> · <a href="glb-checks.json">GLB 검사</a> · <a href="fbx-checks.json">FBX 검사</a> · <a href="gates.json">독립 게이트</a> · <a href="delivery.json">시험 투입</a></p><h2>960 격자</h2><a href="grid-full.png"><img src="grid.png"></a><h2>실제 거리 배치 · 960×540 (보류 시제품 포함)</h2><a href="street-960.png"><img src="street-960.png"></a><div class="grid">'''+''.join(cards)+'</div></html>'
(R/'index.html').write_text(html,encoding='utf8');print('REVIEW',len(assets))
