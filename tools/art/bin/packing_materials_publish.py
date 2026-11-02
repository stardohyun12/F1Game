"""Compose directly authored Blender render sheets; no generative image tools."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageStat
import json,csv,hashlib,html,re
R=Path(__file__).resolve().parents[3];S=R/'art/source/packing-materials';V=R/'art/_review/2026-11-02-packing-materials'
def font(n):return ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
names={'box_s':'골판지 상자 S','box_m':'골판지 상자 M','box_l':'골판지 상자 L','bubble_roll':'뽁뽁이 롤','bubble_sheet':'풀린 뽁뽁이 시트','newspaper_stack':'신문지 묶음','newspaper_ball_a':'구긴 신문지 A','newspaper_ball_b':'구긴 신문지 B','egg_tray':'달걀 한 판 · 30알','tape_roll':'박스테이프 롤','label_stack':'주소 라벨 묶음','address_label':'붙이는 주소 라벨'}
notes={'box_s':'4날개 별도 힌지 · 닫힘 34.4×12.4×34.4cm','box_m':'4날개 별도 힌지 · 닫힘 33.9×17.4×33.9cm','box_l':'4날개 별도 힌지 · 닫힘 52.4×32.4×52.4cm','bubble_roll':'지름16×축 길이32cm · 중심 피벗','bubble_sheet':'폭30×참고 길이60cm · 실제 게임은 동적 메시 유지','newspaper_stack':'30×6×22cm · 글자가 아닌 흐린 열 무늬','newspaper_ball_a':'단위 지름1 FBX · 전시 지름11cm','newspaper_ball_b':'단위 지름1 FBX · 전시 지름11cm','egg_tray':'30×6.5×30cm · 콜라이더 중심 피벗','tape_roll':'지름10×높이2.5cm · 단일 렌더 메시','label_stack':'10×2×7cm · 공급용','address_label':'10×0.2×7cm · 부착용'}
metrics={n:json.loads((S/f'{n}_metrics.json').read_text()) for n in names};files=[];cards=[]
overview=Image.new('RGB',(1400,4*420+160),'#ece7db');od=ImageDraw.Draw(overview);od.text((35,25),'보관소 포장 재료 / 12개 후보',font=font(35),fill='#2f4947');od.text((35,85),'D-259 · 도현 승인 필요 · Blender 근사 조명 · 게임 미투입',font=font(22),fill='#60746b')
for idx,(aid,title) in enumerate(names.items()):
 m=metrics[aid];views=['hero','side','top']+(['closed'] if aid.startswith('box_') else [])
 for view in views:
  p=V/f'{aid}_{view}.png';im=Image.open(p).convert('RGB');assert im.size==(1280,720)
  assert max(ImageStat.Stat(im).stddev)>3,(p,'suspected blank failed render')
  im.resize((640,360),Image.Resampling.LANCZOS).save(V/f'{aid}_{view}_360.png');files.append(dict(path=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),size=im.size))
 x=30+(idx%3)*460;y=160+(idx//3)*420;od.text((x,y),title,font=font(22),fill='#2f4947');overview.paste(Image.open(V/f'{aid}_hero.png').resize((440,248)),(x,y+42));od.text((x,y+303),f"{m['triangles']} tris / {'중심' if not aid.startswith('box_') else '바닥 중앙'} 피벗",font=font(18),fill='#61766c')
 lines=['']
 for word in notes[aid].split():
  candidate=(lines[-1]+' '+word).strip()
  if od.textlength(candidate,font=font(16))>430:lines.append(word)
  else:lines[-1]=candidate
 for line,note in enumerate(lines):od.text((x,y+337+line*26),note,font=font(16),fill='#61766c')
 sheet=Image.new('RGB',(1400,1500 if aid.startswith('box_') else 1340),'#ece7db');d=ImageDraw.Draw(sheet);d.text((40,25),title,font=font(36),fill='#2f4947');d.text((40,85),notes[aid]+f" / {m['triangles']} tris",font=font(22),fill='#61766c');sheet.paste(Image.open(V/f'{aid}_hero.png'),(60,140))
 for j,view in enumerate(['side','top']):sheet.paste(Image.open(V/f'{aid}_{view}_360.png'),(40+j*680,910));d.text((40+j*680,870),'측면' if view=='side' else '윗면',font=font(20),fill='#61766c')
 if aid.startswith('box_'):
  # Three primary views remain full-size elsewhere; closed pose is an additional reference.
  sheet=Image.new('RGB',(1400,1740),'#ece7db');d=ImageDraw.Draw(sheet);d.text((40,25),title+' / 열림과 닫힘',font=font(36),fill='#2f4947');d.text((40,85),notes[aid],font=font(22),fill='#61766c');sheet.paste(Image.open(V/f'{aid}_hero.png'),(60,140));sheet.paste(Image.open(V/f'{aid}_side_360.png'),(40,900));sheet.paste(Image.open(V/f'{aid}_top_360.png'),(720,900));sheet.paste(Image.open(V/f'{aid}_closed_360.png'),(380,1320));d.text((40,1280),'추가 컷 · 닫힘 (FBX 기본 자세)',font=font(22),fill='#61766c')
 sheet.save(V/f'{aid}_sheet.png')
 previews=''.join(f'<a href="{aid}_{v}.png"><img loading="lazy" src="{aid}_{v}_360.png" alt="{title} {v}"><span>{dict(side="측면",top="윗면",closed="닫힘")[v]}</span></a>' for v in views[1:])
 cards.append(f'''<article id="{aid}"><header><span>{idx+1:02d} / 12</span><h2>{title}</h2><p>{notes[aid]}</p></header><a href="{aid}_hero.png"><img src="{aid}_hero_360.png" width="640" height="360" alt="{title} 3/4"></a><p class="metric">{m['triangles']} tris · {m['spec']['pivot']}<br>도현 승인 필요</p><div class="details">{previews}</div><p class="downloads"><a href="{aid}_sheet.png">PNG 시트</a><a href="../../source/packing-materials/packing_{aid}_v01.blend">.blend</a><a href="../../source/packing-materials/packing_{aid}_v01.fbx">.fbx</a></p></article>''')
overview.save(V/'overview.png')
css='''*{box-sizing:border-box}body{margin:0;background:#ece7db;color:#2f4947;font:16px/1.65 "Malgun Gothic",sans-serif}main{max-width:1440px;padding:40px;margin:auto}h1{font-size:44px;line-height:1.25;letter-spacing:-2px}h2{font-size:25px;margin:5px 0}a{color:inherit;text-underline-offset:4px}nav,.downloads{display:flex;flex-wrap:wrap;gap:18px}nav{padding:20px 0;border-block:1px solid #b5c0b4;margin:30px 0}.tag{display:inline-block;background:#335856;color:#eee7d7;padding:7px 14px}.notice{border-left:4px solid #9a7855;padding:16px 22px;background:#e1dfd0;max-width:1100px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:28px;margin-top:45px}article{background:#f4f0e5;border:1px solid #c4c7b9}header{padding:22px}header p{margin:5px 0;color:#62796c}header span{font-size:12px;letter-spacing:2px}img{display:block;max-width:100%;height:auto}.metric,.downloads{padding:0 22px;font-size:14px}.details{display:grid;grid-template-columns:1fr 1fr;gap:12px;padding:0 22px}.details span{display:block;padding:5px 0 12px;font-size:13px}.details a{text-decoration:none}footer{border-top:1px solid #b5c0b4;padding:30px 0;margin-top:50px}.intro{max-width:950px}@media(max-width:800px){main{padding:20px}.grid{grid-template-columns:1fr}h1{font-size:32px}}'''
concepts=''.join(f'<a href="concept/{n}_concept.png">{t}</a>' for n,t,*_ in json.loads((S/'design.json').read_text(encoding='utf8')))
page=f'''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>D-259 포장 재료 후보</title><style>{css}</style><main><div class="intro"><span class="tag">도현 승인 필요 · D-259</span><h1>상자를 접고,<br>물건을 감싸는 재료들</h1><p>골판지 · 반투명 필름 · 신문지 · 종이 달걀판 · 테이프 · 주소 라벨.<br>기존 포장 시제품의 치수와 피벗에 맞춘 12개 모델입니다.</p></div><nav><a href="overview.png">전체 PNG</a><a href="../../source/packing-materials/README.md">치수·연결·출처</a><a href="../../../docs/team/reports/art-20261102-2052.md">아트 보고</a><a href="../2026-11-02-depot-props/index.html">① 보관소 소품</a></nav><div class="notice"><b>게임 미투입.</b> 원화와 미리보기 일괄 확인용 후보입니다. 상자 4날개 동기화와 뽁뽁이 UV·투명 재질은 프로그래밍 연결이 필요합니다. 기존 물리 콜라이더는 수정하지 않았습니다. 모든 이미지는 Blender 근사 조명이며 Unity 검증을 하지 않았습니다.</div><p>직접 그린 원화</p><nav>{concepts}</nav><div class="grid">{''.join(cards)}</div><footer><p>Blender bpy 직접 모델링 / CC0 사진 기반 / 생성형 이미지·3D 도구 사용 없음 / 실제 기사·로고·주소·임의 글자 없음.</p><p><b>파일 주의:</b> 신문공 FBX는 지름1 정규화 메시입니다. <a href="../../source/packing-materials/adapters/">어댑터 5개</a>는 기존 primitive의 스케일 유지용이며 실크기 원본과 구분합니다. 뽁뽁이 시트 FBX는 참고본으로 실제 유연 메시를 대체하지 않습니다.</p><p><a href="evidence/fbx_readback.json">FBX 치수·피벗·힌지·어댑터 검사</a> · <a href="evidence/delivery_check.json">납품 파일 검사</a> · <a href="../../source/packing-materials/texture_manifest.json">사진 출처·가공 기록</a></p><p>QA/프로그래밍 임포트 필요. 원본1280×720, 화면 축소본640×360.</p></footer></main></html>'''
(V/'index.html').write_text(page,encoding='utf8')
prov=R/'art/provenance.csv';existing=prov.read_text(encoding='utf-8-sig');lic=json.loads((S/'reference/license.json').read_text());manifest=json.loads((S/'texture_manifest.json').read_text(encoding='utf8'))
with prov.open('a',encoding='utf8',newline='') as f:
 w=csv.writer(f);rows=[['packing_20261102_bubble_source','art/source/packing-materials/reference/bubble_wrap_cc0.jpg',lic['cc0_confirmation'],'CC0-1.0','2026-11-02','urllib download','no',lic['sha256'],'Original photo via CC0 page linked source; license.json/source_page.html saved']]
 for p in sorted((S/'textures').glob('*.png')):
  m=manifest[-1] if p.stem.startswith('bubble') else next(m for m in manifest if m['name']==p.stem)
  rows.append(['packing_20261102_'+p.stem,str(p.relative_to(R)).replace('\\','/'),m['source_url'],'CC0-1.0','2026-11-02','packing_materials_prepare.py; authored bpy','no',hashlib.sha256(p.read_bytes()).hexdigest(),m['processing']])
 for row in rows:
  if row[0]+',' not in existing:w.writerow(row)
links=re.findall(r'(?:href|src)="([^"]+)"',page);missing=[s for s in links if s!='evidence/delivery_check.json' and not s.startswith(('#','http')) and not (V/s).exists()] # this result file is written immediately below
check=dict(model_count=len(metrics),blend_count=len(list(S.glob('*.blend'))),fbx_count=len(list(S.glob('*.fbx'))),adapter_count=len(list((S/'adapters').glob('*.fbx'))),concept_count=len(list((V/'concept').glob('*.png'))),render_count=len(files),small_count=len(files),missing_links=missing,files=files)
check['pass']=len(metrics)==12 and check['blend_count']==12 and check['fbx_count']==12 and check['adapter_count']==5 and len(files)==39 and not missing
(V/'evidence/delivery_check.json').write_text(json.dumps(check,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in check.items() if k!='files'},ensure_ascii=False))
