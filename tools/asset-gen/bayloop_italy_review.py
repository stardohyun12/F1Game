"""Static, attributable review board assembled exclusively from photos and actual bpy renders."""
from pathlib import Path
import json,html
from PIL import Image,ImageDraw,ImageFont,ImageOps
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'art/_review/2026-10-25-bayloop-italy'
font=ImageFont.truetype(str(ROOT/'F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf'),25)
g=json.loads((R/'gate-summary.json').read_text());assets=[]
for folder in ['bayloop_italy_v01','bayloop_italy_props_v01']:assets+=json.loads((ROOT/'art/models'/folder/'manifest.json').read_text())['assets']
board=Image.new('RGB',(1920,1160),(235,232,222));d=ImageDraw.Draw(board)
for x,src,label in [(0,'machiya_same_camera.png','이전 마치야 — 동일 카메라·원래 크기'),(960,'shop_house_a_front.png','D-137 상점 — 동일 카메라·원래 크기')]:
 d.text((x+16,8),label,font=font,fill='#303030');board.paste(Image.open(R/src).convert('RGB'),(x,45))
d.text((16,595),'비례 참고 실사진 — Procida / Atrani / Positano · 게임 텍스처에 사용하지 않음',font=font,fill='#303030')
photo=ImageOps.contain(Image.open(R/'references/reference-contact.jpg').convert('RGB'),(1920,470));board.paste(photo,((1920-photo.width)//2,638))
d.text((16,1115),'사진 저자·CC BY/SA 라이선스: references/REFERENCES.md · 모든 모델/배경은 실제 bpy 렌더',font=font,fill='#303030');board.save(R/'compare.png')
sections=[]
for title,subset in [('건물',sorted([a for a in assets if a.get('asset_kind')!='prop'],key=lambda a:a['id'])),('생활 소품',sorted([a for a in assets if a.get('asset_kind')=='prop'],key=lambda a:a['id']))]:
 cards=[]
 for a in subset:
  name=a['id'];gate=g[name];passed=gate['yes'] and gate['score']>=7;prefix='props/' if a.get('asset_kind')=='prop' else '';views=['front','back'] if prefix else ['front','back','side','top']
  links=' · '.join(f'<a href="{prefix}{name}_{view}.png">{view}</a>' for view in views)
  cards.append(f'<article><h3>{name}</h3><p>{a["tris"]} tris · {gate["score"]}/10 · <strong>{"시험 투입" if passed else "보류"}</strong></p><a href="{prefix}{name}_front.png"><img src="{prefix}{name}_front.png" alt="{name}"></a><p>{links} · <a href="{gate["report"]}">독립 판정 원문</a></p></article>')
 sections.append(f'<h2>{title}</h2><div class="grid">'+''.join(cards)+'</div>')
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><title>D-137 기억 속 항구 — 건물·생활 소품</title><style>body{background:#ebe8df;color:#272b2b;font:17px/1.6 system-ui;max-width:1550px;margin:32px auto;padding:0 22px}h1,h2,h3{line-height:1.3}a{color:#315e6b}img{max-width:100%;height:auto}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:22px}article{border-top:1px solid #999}article h3{font-size:18px}p{margin:12px 0}.wide{width:100%;display:block;margin:20px 0}@media(max-width:900px){.grid{grid-template-columns:1fr}}</style><h1>D-137 — 어디인지 모를 기억 속 항구</h1><p>이탈리아 해안의 회벽·아치·덧창·발코니·옥상 형태에 작은 한국어 간판·자판기·배관 등 길가 생활 층을 더한 정적 시험 에셋. 지리적 고증 재현이나 실제 게임 캡처가 아니다.</p><p>세계32px/m · 공용32색 albedo · AI 이미지0 · Unity/씬/커밋 미실행. 7점 미만은 복사하지 않는다. 최종 승인은 인게임960×540 캡처로 받는다.</p><p><a href="../../../docs/team/reports/art-20261025-1929.md">보고서</a> · <a href="references/REFERENCES.md">사진 크레딧·라이선스</a> · <a href="building-checks.json">건물 수치검사</a> · <a href="prop-checks.json">소품 수치검사</a> · <a href="building-fbx-checks.json">건물 FBX</a> · <a href="prop-fbx-checks.json">소품 FBX</a> · <a href="trial-delivery.json">시험 복사 목록</a></p><a href="compare.png"><img class="wide" src="compare.png" alt="마치야와 새 상점 및 실물 사진 비교"></a><h2>실제 Blender 배치 시험</h2><img class="wide" src="street-study-960.png" alt="새 건물과 통과 소품을 metre 스케일로 배치한 Blender 렌더"><p>건물·소품 형상과 크기 관계를 보기 위한 배치. Unity 플레이 거리·충돌·길 연결은 미검증.</p>'''+''.join(sections)+'''<h2>기존 항구 창고 — 유지</h2><p>작업용 건물의 바랜 함석·셔터를 유지했다. 기존 FBX/아틀라스는 변경하지 않았다. 주거용 테라코타 지붕으로 일괄 바꾸지 않는 제작 판단이며, 실사진 고증을 주장하지 않는다.</p><img class="wide" src="../2026-10-25-bayloop-buildings/harbor_warehouse_pair.png" alt="유지한 기존 창고 두 채"><p>미통과 판은 iterations/에 보존. 각 정면 이미지를 눌러960×540 원본 확인. 알베도 팔레트 검사와 최종 조명 화면 색수는 별개다.</p></html>'''
(R/'index.html').write_text(page,encoding='utf8');print('REVIEW_READY')
