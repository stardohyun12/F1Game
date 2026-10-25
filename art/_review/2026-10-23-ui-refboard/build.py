"""자료 보드·수동 면적 계산·폰트 기준 렌더. Unity/게임 자산 변경 없음."""
from pathlib import Path
import json, html, hashlib, shutil, subprocess, re
from PIL import Image, ImageDraw, ImageFont, ImageChops

R=Path(__file__).resolve().parent
REPO=R.parents[2]
data=json.loads((R/'references.json').read_text(encoding='utf-8'))
sources=json.loads((R/'sources.json').read_text(encoding='utf-8'))
by={s['slug']:s for s in sources}
metrics=[]
E=html.escape

def pct(item):
    mask=Image.new('1',(960,540)); d=ImageDraw.Draw(mask)
    for x,y,w,h in item['rects']: d.rectangle((x,y,x+w-1,y+h-1),fill=1)
    for p in item.get('polygons',[]): d.polygon([tuple(q) for q in p],fill=1)
    return sum(mask.getdata())/518400*100

def outline(item):
    out=[]
    for x,y,w,h in item['rects']: out.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}"/>')
    for p in item.get('polygons',[]): out.append('<polygon points="'+' '.join(f'{x},{y}' for x,y in p)+'"/>')
    return '<svg class="mark" viewBox="0 0 960 540" preserveAspectRatio="none" aria-hidden="true">'+''.join(out)+'</svg>'

cards=[]; paragraphs=[]
for i,item in enumerate(data,1):
    src=by[item['slug']]; shot=next(s for s in src['shots'] if s['id']==item['shot']); percent=pct(item)
    im=Image.open(R/shot['local']).convert('RGB')
    if item.get('crop'): im=im.crop(tuple(item['crop']))
    display=f"images/{item['slug']}-display.png"
    im.resize((960,540),Image.Resampling.NEAREST).save(R/display)
    metric={'slug':item['slug'],'source_image':shot['local'],'source_sha256':shot['sha256'],'source_size':[shot['width'],shot['height']],'source_crop_xyxy':item.get('crop'),'normalized_size':[960,540],'kind':item['kind'],'rects_xywh':item['rects'],'polygons_xy':item.get('polygons',[]),'area_pct':round(percent,3),'state':item['state'],'limitation':item['limit']}
    metrics.append(metric)
    source_links=f'<a href="{E(src["source"])}" target="_blank" rel="noopener">Steam 원문</a> · <a href="{E(shot["url"])}" target="_blank" rel="noopener">스크린샷 #{shot["id"]}</a>'
    secondary=''
    if 'secondary' in item:
        s2=next(s for s in src['shots'] if s['id']==item['secondary'])
        secondary=f'<details><summary>다른 상태 보기 · #{s2["id"]} (면적 미계산)</summary><a href="{s2["local"]}" target="_blank"><img src="{s2["local"]}" alt="{E(src["name"])} 보조 스크린샷" loading="lazy"></a><a href="{E(s2["url"])}" target="_blank" rel="noopener">보조 이미지 원본 출처</a></details>'
    cards.append(f'''<article data-group="{item['group']}" id="{item['slug']}">
<div class="card-head"><span class="num">{i:02}</span><h3>{E(src['name'])}</h3><span class="tag">{item['group']}</span></div>
<div class="frame"><a href="{shot['local']}" target="_blank"><img src="{display}" alt="{E(src['name'])} — {E(item['state'])}; {E(item['visual'])}"></a>{outline(item)}</div>
<div class="card-body"><div class="measure"><strong>{percent:.1f}% <small>{item['kind']}</small></strong><span>{E(item['state'])}<br>수동 영역 근사 · 한 장면</span></div>
<p>{E(item['visual'])}</p><p class="sound">{E(item['motion'])} <a href="{E(item['motion_url'])}" target="_blank" rel="noopener">원문</a></p>
<p class="take">가져올 원리 · {E(item['take'])}</p><p class="limit">{E(item['limit'])}</p>{secondary}
<footer>{source_links}<br>{E(', '.join(src['developers']))} / {E(', '.join(src['publishers']))} · 게시 이미지, 직접 Play 캡처 아님</footer></div></article>''')
    extra=''
    if 'secondary' in item: extra=f" · [다른 상태 #{s2['id']}]({s2['url']})"
    paragraphs.append(f"### {i:02}. {src['name']} — {item['state']}\n\n- **폰트·색·테두리 [관찰]:** {item['visual']} 정확한 서체명은 확인한 경우 외에는 [미확인].\n- **움직임·소리:** {item['motion']} [원문]({item['motion_url']})\n- **화면 면적:** {percent:.1f}% **{item['kind']}**, {item['limit']} [등록 원문]({src['source']}) · [스크린샷 #{shot['id']}]({shot['url']}){extra}\n- **우리 적용 [제안]:** {item['take']}\n")

(R/'measurements.json').write_text(json.dumps(metrics,ensure_ascii=False,indent=2),encoding='utf-8')

# 원형 12px -> nearest 2x/3x. 기존 공식 배포 파일만 재사용.
(R/'fonts').mkdir(exist_ok=True)
old=R.parent/'2026-10-23-d123'/'fonts'
for fn in ['Galmuri11.ttf','Galmuri11.woff2','LICENSE.txt']:
    shutil.copyfile(old/fn,R/'fonts'/fn)
basefont=ImageFont.truetype(str(R/'fonts/Galmuri11.ttf'),12)
def label(size=18): return ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',size)
proof=Image.new('RGB',(960,540),'#e5dfce'); d=ImageDraw.Draw(proof)
d.text((28,20),'Galmuri11 / 12px 원형 → 정수배 비교',font=label(25),fill='#273c3b')
d.text((28,60),'조사용 렌더 · 제품 UI 시안 아님 · 논리 화면 960×540',font=label(16),fill='#425955')
sample='배달 2/3   남은 시간 01:28   거리 320m'
mask=Image.new('1',(450,20)); md=ImageDraw.Draw(mask); md.fontmode='1'; md.text((0,0),sample,font=basefont,fill=1)
cropbox=mask.getbbox(); glyph=mask.crop(cropbox)
for y,k in [(116,1),(204,2),(306,3)]:
    d.text((28,y-25),f'{12*k}px / {k}×' + ('  (원형 진단용)' if k==1 else '  (후보)'),font=label(16),fill='#425955')
    up=glyph.resize((glyph.width*k,glyph.height*k),Image.Resampling.NEAREST)
    proof.paste('#263a39',(28,y),up)
d.line((28,383,930,383),fill='#8d9587',width=1)
test='볶음 · 값 · 흙 · 뺨    0/O 1/I 8/B    m km/h 초 원'
diag=Image.new('1',(450,22)); dd=ImageDraw.Draw(diag); dd.fontmode='1'; dd.text((0,0),test,font=basefont,fill=1)
diag=diag.crop(diag.getbbox()); proof.paste('#263a39',(28,403),diag.resize((diag.width*2,diag.height*2),Image.Resampling.NEAREST))
d.text((28,459),'원문: github.com/quiple/galmuri · Lee Minseo · SIL OFL 1.1',font=label(14),fill='#425955')
d.text((28,486),'Pillow 래스터 시험만 수행. Unity TMP·실제 주행 가독성은 미검증.',font=label(14),fill='#425955')
proof.save(R/'font-proof-960.png'); proof.resize((1920,1080),Image.Resampling.NEAREST).save(R/'font-proof-1920.png')
unknown=bytes(basefont.getmask('\u0378',mode='1')); missing=[c for c in sorted(set(sample+test)) if not c.isspace() and bytes(basefont.getmask(c,mode='1'))==unknown]
fontcheck={'ttf_sha256':hashlib.sha256((R/'fonts/Galmuri11.ttf').read_bytes()).hexdigest(),'source':'https://github.com/quiple/galmuri/releases/tag/v2.40.4','native_px':12,'scales':[2,3],'sample':sample+' / '+test,'missing_vs_undefined_glyph':missing,'native_mask_values':sorted(set(mask.getdata())),'nearest_2x_roundtrip_equal':ImageChops.difference(proof,Image.open(R/'font-proof-1920.png').resize((960,540),Image.Resampling.NEAREST)).getbbox() is None,'unity_tested':False,'human_readability_tested':False}
(R/'font-validation.json').write_text(json.dumps(fontcheck,ensure_ascii=False,indent=2),encoding='utf-8')

intro='''# UI 조사 1/3 — 레퍼런스

2026-10-23 · PitCrew 아트팀 UI · PD 과제 20261023-1657 · **조사 완료 / 방향 제안, 원화·게임 적용 승인 아님**

**제안:** 갈 때는 고정된 작은 시간·거리 HUD, 돌아올 때는 목표 HUD를 접고, 읽을 내역은 정차 후 종이 화면으로 넘긴다. 기준은 D-123(바랜 해안 동네), D-124(960×540 Point), D-125(갈 때 급하게 / 귀환이 보상), D-126(차 주변 속도감 / 먼 세계 규모감)이다. D-121의 Jua·장난감 톤은 폐기 상태로 유지한다.

- [레퍼런스 보드](../../art/_review/2026-10-23-ui-refboard/index.html) · [보드 PNG](../../art/_review/2026-10-23-ui-refboard/refboard.png) · [폰트 960 원형](../../art/_review/2026-10-23-ui-refboard/font-proof-960.png) · [1920 정수 확대](../../art/_review/2026-10-23-ui-refboard/font-proof-1920.png)
- 근거 등급: **[관찰]** 직접 연 공식 이미지·정적 코드, **[제작자 설명]** 원문에 적힌 사실/평가(실행 검증 아님), **[제안/추론]** 우리 적용 판단, **[미확인]** 접근·청취·실행 못 한 부분.
- 10개 요청 게임 + Crazy Taxi·Pacific Drive를 비교했다. Mini Motorways·Unpacking·Papers, Please는 3D 대표작이 아니라 **2D UI 대조군**이다. 모든 게임을 로파이 3D로 묶지 않는다.

## 1. 게임별 관찰 — 각 4줄

면적은 **선정한 한 장면**에서 960×540 좌표로 수동 그린 묶음 영역의 합집합 / 518,400이다. 글자 사이 여백·패널 바탕을 포함하므로 알파 불투명 픽셀 비율이 아니다. 소수점은 계산 출력이며 경계 판정의 정확도를 보증하지 않는다. **O**=겹친 HUD/메뉴, **D**=시계·종이·계기 같은 읽을 면. D의 조작 면적과 O의 풍경 가림을 서로 순위 비교하지 않는다. Papers, Please만 검은 바깥 여백을 빼고 계산했다. 보드의 ‘영역 표시’와 [좌표 장부](../../art/_review/2026-10-23-ui-refboard/measurements.json)로 재확인 가능하다.

스크린샷은 개발사/배급사가 Steam에 등록한 **공식 게시 이미지**다. 직접 플레이 캡처가 아니며 PC 스토어 게시 컷이지 버전 전체를 대표하지 않는다. [출처·원본 URL·해시 장부](../../art/_review/2026-10-23-ui-refboard/sources.json)에 등록자·조회일·해상도를 남겼다. 권리는 각 권리자에게 있고 조사 비교용이며, 게임 출시 자산으로 쓰지 않는다. 애니메이션·소리는 정지 화면에서 추측하지 않았다.

'''
doc=intro+'\n'.join(paragraphs)+'\n'+(R/'notes.md').read_text(encoding='utf-8')
docpath=REPO/'docs/research/ui-01-references.md'; docpath.parent.mkdir(parents=True,exist_ok=True); docpath.write_text(doc,encoding='utf-8')

take10=[line[3:] for line in (R/'notes.md').read_text(encoding='utf-8').split('## 5. 우리가 가져올 것 10개')[1].split('## 6. 하지 말 것 5개')[0].strip().splitlines() if re.match(r'\d+\.',line)]
avoid5=[re.sub(r'^\d+\. ','',line) for line in (R/'notes.md').read_text(encoding='utf-8').split('## 6. 하지 말 것 5개')[1].strip().splitlines() if re.match(r'\d+\.',line)]
take10=[re.sub(r'^\d+\. ','',line) for line in (R/'notes.md').read_text(encoding='utf-8').split('## 5. 우리가 가져올 것 10개')[1].split('## 6. 하지 말 것 5개')[0].strip().splitlines() if re.match(r'\d+\.',line)]
def li(items): return ''.join('<li>'+E(t).replace('**','')+'</li>' for t in items)
css='''@font-face{font-family:Galmuri;src:url(fonts/Galmuri11.woff2)}*{box-sizing:border-box}body{margin:0;background:#e8e5da;color:#243c3b;font:15px/1.65 "Malgun Gothic",sans-serif}a{color:#225e66;text-underline-offset:3px}header{background:#243c3b;color:#faf4df;padding:32px max(28px,calc((100vw - 1360px)/2))}header a{color:#efc479}.eyebrow{font:12px Galmuri,monospace;letter-spacing:2px;color:#b5c6ba}h1{font:36px Galmuri,monospace;line-height:1.4;margin:14px 0}header p{max-width:980px;margin:6px 0;color:#e2e5d9}main{max-width:1416px;padding:24px 28px 36px;margin:auto}h2{font:24px Galmuri,monospace;line-height:1.45;margin:8px 0 14px}.notice{border-left:4px solid #ad6d3c;padding:12px 18px;background:#f6f2e6;margin:0 0 20px}.toolbar{display:flex;gap:12px;flex-wrap:wrap;align-items:center;margin:18px 0}select,button{padding:8px 12px;background:#fffbed;border:1px solid #7d9187;color:#243c3b;font:inherit;cursor:pointer}button[aria-pressed=true]{background:#243c3b;color:#fffbed}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:20px}article{background:#f8f5eb;border:1px solid #bac3b5;min-width:0}.card-head{display:flex;align-items:center;gap:10px;padding:12px 14px}.num{font:24px Galmuri;color:#68827c}h3{font-size:18px;line-height:1.4;margin:0;flex:1}.tag{font-size:11px;white-space:nowrap;border-bottom:1px solid #bd8052}.frame{position:relative;background:#182b2c}.frame img{display:block;width:100%;aspect-ratio:16/9;object-fit:fill}.mark{position:absolute;inset:0;width:100%;height:100%;fill:#ffe1852b;stroke:#ffe185;stroke-width:3;display:none;pointer-events:none}.show .mark{display:block}.card-body{padding:14px}.measure{display:flex;justify-content:space-between;align-items:center;border-bottom:1px solid #d8dccc;padding-bottom:9px;gap:10px}.measure strong{font:24px Galmuri}.measure small{font-size:12px}.measure span{font-size:12px;text-align:right}.card-body p{margin:10px 0}.sound{color:#52635c;font-size:13px}.take{border-left:2px solid #a96d45;padding-left:9px}.limit{font-size:12px;color:#5c655f}footer{font-size:11px;border-top:1px solid #d8dccc;padding-top:10px;overflow-wrap:anywhere}details{font-size:12px;margin:10px 0}details img{width:100%;height:auto;margin-top:8px}summary{cursor:pointer}.bottom{margin-top:30px;display:grid;grid-template-columns:1fr 1fr;gap:24px}.paper{padding:22px;background:#f8f5eb;border-top:3px solid #70867c}.paper ol{padding-left:23px;margin:0}.paper li{margin:7px 0}.font img{max-width:100%;height:auto;image-rendering:pixelated}.font{margin-top:26px}.links{margin-top:20px;border-top:1px solid #aebaae;padding-top:16px;font-size:13px}.hidden{display:none}@media(max-width:1050px){.grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:680px){.grid,.bottom{grid-template-columns:1fr}h1{font-size:24px}main{padding:18px}.tag{display:none}}@media print{.toolbar{display:none}body{font-size:11px}.grid{grid-template-columns:repeat(3,1fr)}article{break-inside:avoid}.bottom{break-before:page}}'''
board=f'''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>PitCrew · UI 레퍼런스 01</title><style>{css}</style></head><body>
<header><div class="eyebrow">PITCREW / ART UI / RESEARCH 01 OF 03 / 2026.10.23</div><h1>갈 때는 급하게,<br>돌아오는 길이 보상.</h1><p>풍경을 남기는 HUD · 읽기 쉬운 종이 · 한글 픽셀 격자</p><p>D-123~126을 기준으로 12개 게임을 비교한 조사 보드. 신규 원화·출시 UI가 아니며, 적용 방향은 도현 승인 필요.</p><a href="../../../docs/research/ui-01-references.md">전체 조사 문서</a> · <a href="sources.json">출처 장부</a> · <a href="measurements.json">면적 좌표</a></header>
<main><div class="notice"><b>무엇을 확인했나</b><br>공식 등록 스크린샷과 제작자 기록. 각 비율은 한 장면의 수동 영역 근사다. O=겹친 안내, D=물건의 읽을 면. 서로 같은 지표로 순위를 매기지 않는다. 소리 청취·원작 Play·Unity 검증은 하지 않았다. 게임 이미지 권리는 각 권리자에게 있다.</div>
<div class="toolbar"><label for="filter">비교 관점</label><select id="filter"><option value="all">전체 12개</option>풍경</option><option>경계 HUD</option><option>물건 UI</option><option>시간 압박</option></select><button id="mark" aria-pressed="false">면적 영역 표시</button><span id="count" aria-live="polite">12개 표시</span></div>
<section class="grid" aria-label="게임별 공식 스크린샷 비교">{''.join(cards)}</section>
<section class="bottom"><div class="paper"><h2>물건 UI의 경계</h2><p>Papers, Please 모바일은 작은 책상·겹친 서류를 그대로 유지하지 못했다. 제작자는 큰 문서 캐러셀과 별도 도장 책상으로 읽기와 조작을 나눴다.</p><p><b>우리 적용:</b> 전표는 정차 화면. 시간은 화면 구석. 종이 질감 때문에 글자를 작게 만들거나 새 드래그 단계를 넣지 않는다.</p><a href="https://dukope.com/devlogs/papers-please/mobile/">실패 시안과 해결 과정 원문</a></div><div class="paper"><h2>압박과 풍경의 예산</h2><p><b>갈 때 ≤8% / 귀환 ≤3%</b>는 우리 첫 시안의 가설이다. 960×540에서 각각 41,472 / 15,552px². 검증된 업계 기준이 아니다.</p><p>시간·거리 고정 위치, 큰 중앙 마커 제외, 단발 경고. 배달 종료 후 목표 HUD를 접는다. D-126 속도감은 카메라·차 주변·소리가 맡는다.</p><a href="https://store.steampowered.com/manual/71230">Crazy Taxi 원 설명서</a></div></section>
<section class="font paper"><h2>12는 원형, 24·36은 후보.</h2><p>Galmuri11의 공식 기준은 12px. 아래는 실제 TTF의 원형을 nearest로 확대한 조사용 렌더다. 읽는 속도와 Unity TMP 결과는 미검증.</p><a href="font-proof-960.png" target="_blank"><img src="font-proof-960.png" width="960" height="540" alt="Galmuri11 12, 24, 36픽셀 비교와 복잡한 받침·숫자 진단 문장"></a><p><a href="font-proof-960.png">960×540 원형</a> · <a href="font-proof-1920.png">1920×1080 정수 확대</a> · <a href="font-validation.json">렌더 검사</a> · <a href="https://github.com/quiple/galmuri">폰트 원문</a> · <a href="fonts/LICENSE.txt">OFL</a></p><p>2560×1440은 960의 정수배가 아니다. Point 필터와 정수배 배치는 별개. 현재 UIRoot 기준은 1920×1080이므로 프로그래밍·QA 확인 필요.</p></section>
<section class="bottom"><div class="paper"><h2>우리가 가져올 것 10개</h2><ol>{li(take10)}</ol></div><div class="paper"><h2>하지 말 것 5개</h2><ol>{li(avoid5)}</ol></div></section>
<div class="links">조사 1/3 완료 · 원화/스프라이트 제작 0 · QA/프로그래밍 임포트 필요(후속 제작 시) · <a href="../../../docs/team/reports/artui-20261023-1657.md">PD 보고</a><br>원본 이미지는 클릭해서 확인. 영역 표시는 연구 주석이며 원작 UI가 아님. 단순 미확인 항목을 실패로 판정하지 않는다.</div></main>
<script>const cards=[...document.querySelectorAll('article')];document.getElementById('filter').addEventListener('change',e=>{{let n=0;cards.forEach(c=>{{const show=e.target.value==='all'||c.dataset.group===e.target.value;c.classList.toggle('hidden',!show);n+=show}});document.getElementById('count').textContent=n+'개 표시'}});document.getElementById('mark').addEventListener('click',e=>{{const on=document.body.classList.toggle('show');e.target.setAttribute('aria-pressed',on);e.target.textContent=on?'면적 영역 숨기기':'면적 영역 표시'}});window.addEventListener('load',()=>{{document.fonts.ready.then(()=>{{document.documentElement.dataset.ready='true';document.documentElement.dataset.height=document.documentElement.scrollHeight}})}});</script></body></html>'''
board=board.replace('<option>풍경</option>','<option>풍경</option>')
# 위 템플릿의 선택 항목도 단순 정적 HTML로 유지한다.
board=board.replace('</option>풍경</option>','</option><option>풍경</option>')
(R/'index.html').write_text(board,encoding='utf-8')
print(json.dumps({'games':len(cards),'measurements':[{m['slug']:round(m['area_pct'],1)} for m in metrics],'font_missing':missing,'research':str(docpath)},ensure_ascii=False))
