from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, shutil, math

ROOT=Path(__file__).resolve().parent
REPO=ROOT.parents[2]
P={'ink':'#23303B','paper':'#F3E9D2','lemon':'#F2C230','terracotta':'#C8553D','tyrrhenian':'#1F7A9C','signal':'#D93A2B','olive':'#6A9A3A','stone':'#A69F93'}
for d in ['sprites','guides','fonts','reference']: (ROOT/d).mkdir(exist_ok=True)
for family,file in [('Jua','Jua-Regular.ttf'),('Pretendard','Pretendard-Regular.ttf'),('Pretendard','Pretendard-Bold.ttf')]:
    shutil.copyfile(REPO/'F1/Assets/UI/Fonts'/family/file,ROOT/'fonts'/file)
for family in ['Jua','Pretendard']: shutil.copyfile(REPO/'F1/Assets/UI/Fonts'/family/'OFL.txt',ROOT/'fonts'/f'{family}-OFL.txt')
for name in ['proto-d119-board-call.png','proto-d118-packing.png','proto-d118-result.png']:
    shutil.copyfile(REPO/'docs/qa/evidence'/name,ROOT/'reference'/name)

# UI geometry is authored directly, not AI raster retouching. Border order follows Unity Vector4 L,B,R,T.
specs=[('UITheme','paperPanel',256,256,[16,24,16,16],'original'),('UITheme','paperCard',256,320,[16,24,16,40],'original'),('UITheme','slipHeader',256,64,[16,16,16,16],'white'),('UITheme','stampRing',256,256,[0,0,0,0],'white'),('UITheme','buttonPlate',256,80,[16,16,16,16],'white'),('UITheme','enamelPlate',256,128,[20,20,20,20],'original'),('UITheme','gaugeFill',128,24,[8,8,8,8],'white'),('PackingScreen','stampSprite',300,300,[0,0,0,0],'original'),('PackingScreen','gaugeTrackSprite',256,32,[12,12,12,12],'white'),('PackingScreen','gaugeFillSprite',128,24,[8,8,8,8],'white')]
font=lambda n:ImageFont.truetype(str(ROOT/'fonts/Pretendard-Regular.ttf'),n)
manifest=[]
for owner,name,w,h,border,tint in specs:
    im=Image.new('RGBA',(w,h)); d=ImageDraw.Draw(im)
    if name in ['stampRing','stampSprite']:
        color='#FFFFFF' if tint=='white' else P['signal']
        # Breaks deliberately sit in rings; the center stays empty for runtime labels.
        for pad,stroke in [(9,7),(23,3)]:
            for start,end in [(3,78),(84,174),(178,259),(266,356)]: d.arc((pad,pad,w-pad-1,h-pad-1),start,end,fill=color,width=stroke)
    elif name in ['gaugeFill','gaugeFillSprite']:
        d.rounded_rectangle((0,0,w-1,h-1),radius=7,fill='white')
    elif name=='gaugeTrackSprite':
        d.rounded_rectangle((0,0,w-1,h-1),radius=8,fill='#FFFFFF',outline='#707070',width=3)
    else:
        base=P['paper'] if tint=='original' else '#FFFFFF'
        outline=P['ink'] if tint=='original' else '#727272'
        d.rounded_rectangle((4,4,w-1,h-1),radius=8,fill=(35,48,59,102) if tint=='original' else (60,60,60,102))
        d.rounded_rectangle((0,0,w-5,h-5),radius=8,fill=base,outline=outline,width=3)
        if name=='paperCard':
            # Perforation confined to fixed corner regions so sliced center does not elongate dashes.
            for x in [7,w-14]: d.ellipse((x,23,x+4,27),fill=P['stone'])
            d.line((16,26,w-17,26),fill=P['stone'],width=1)
        if name in ['paperPanel','paperCard']:
            # Receipt edge corner notches; long repeating teeth belong to a separate future tiled strip.
            for x in [7,w-14]: d.polygon([(x,h-5),(x+3,h-10),(x+6,h-5)],fill=(0,0,0,0))
        if name=='slipHeader': d.line((16,h-13,w-17,h-13),fill='#A0A0A0',width=2)
        if name=='enamelPlate':
            d.rounded_rectangle((9,9,w-14,h-14),radius=5,outline=P['tyrrhenian'],width=2)
            for x in [16,w-21]: d.ellipse((x-2,h//2-2,x+2,h//2+2),fill=P['ink'])
    im.save(ROOT/'sprites'/f'{name}.png')
    guide=Image.new('RGB',(max(w,440)+48,h+112),'#E4D5B3');guide.paste(im,(24,72),im)
    gd=ImageDraw.Draw(guide);gd.text((24,12),f'{owner}.{name}',font=font(19),fill=P['ink'])
    gd.text((24,40),f'{w} × {h}  |  L B R T = {border}',font=font(16),fill=P['ink'])
    l,b,r,t=border
    if any(border):
        for x in [24+l,24+w-r]:gd.line((x,72,x,72+h),fill='#D93A2B',width=1)
        for y in [72+t,72+h-b]:gd.line((24,y,24+w,y),fill='#D93A2B',width=1)
    else: gd.text((24,h+80), 'Simple · 정비율 · 9-slice 없음',font=font(15),fill=P['ink'])
    guide.save(ROOT/'guides'/f'{name}-guide.png')
    manifest.append(dict(owner=owner,slot=name,size=[w,h],border_LBRT=border,tint=tint,mode='Sliced' if any(border) else 'Simple'))
(ROOT/'sprites.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')

# A deterministic 9-slice reference implementation: corners are copied exactly.
def sliced(im,sz,b):
    w,h=im.size; W,H=sz;l,bot,r,t=b
    a=[0,l,w-r,w];c=[0,t,h-bot,h];aa=[0,l,W-r,W];cc=[0,t,H-bot,H]
    out=Image.new('RGBA',sz)
    for i in range(3):
        for j in range(3):
            patch=im.crop((a[i],c[j],a[i+1],c[j+1]))
            out.paste(patch.resize((aa[i+1]-aa[i],cc[j+1]-cc[j]),Image.Resampling.NEAREST),(aa[i],cc[j]))
    return out
proof=Image.new('RGB',(1200,520),P['paper']);pd=ImageDraw.Draw(proof)
pd.text((24,18),'9-slice · 동일 모서리 / 가로·세로 확장',font=font(26),fill=P['ink'])
checks=[]
for x,sz in [(24,(256,320)),(320,(500,180)),(850,(320,400))]:
    im=Image.open(ROOT/'sprites/paperCard.png'); out=sliced(im,sz,[16,24,16,40]);proof.paste(out,(x,80),out)
    checks.append(out.crop((0,0,16,40)).tobytes()==im.crop((0,0,16,40)).tobytes())
proof.save(ROOT/'slice-proof.png')
(ROOT/'validation.json').write_text(json.dumps({'slots':len(manifest),'rgba_pngs':all(Image.open(ROOT/'sprites'/f'{s[1]}.png').mode=='RGBA' for s in specs),'slice_corner_exact':checks,'unity_tested':False,'model_triangles_measured':False},indent=2),encoding='utf-8')

css='''@font-face{font-family:Jua;src:url(fonts/Jua-Regular.ttf)}@font-face{font-family:Pretendard;src:url(fonts/Pretendard-Regular.ttf)}@font-face{font-family:Pretendard;src:url(fonts/Pretendard-Bold.ttf);font-weight:700}*{box-sizing:border-box}body{margin:0;background:#e4d5b3;color:#23303b;font-family:Pretendard}h1,h2,h3,.display{font-family:Jua;font-weight:400}header{padding:42px 5vw;background:#23303b;color:#f3e9d2}h1{font-size:48px;margin:8px 0}h2{font-size:32px}section{padding:24px 5vw}img{max-width:100%}.badge{display:inline-block;padding:7px 14px;background:#f2c230;color:#23303b;border-radius:4px}.grid{display:grid;grid-template-columns:repeat(2,1fr);gap:24px}.tile{background:#f3e9d2;padding:18px;border:2px solid #23303b;border-radius:8px}table{border-collapse:collapse;width:100%;background:#f3e9d2}td,th{text-align:left;padding:12px;border-bottom:1px solid #a69f93}a{color:#1f7a9c}.scene{position:relative;width:1280px;height:720px;overflow:hidden;background-size:cover}.scene:before{content:'';position:absolute;inset:0;background:#23303b44}.paper{background:#f3e9d2;border:3px solid #23303b;border-radius:8px;box-shadow:4px 4px #23303b66}.panel{position:absolute;left:130px;top:82px;width:1020px;height:568px;padding:28px}.panel h2{font-size:46px;margin:0 0 10px}.kicker{letter-spacing:3px;font-size:16px}.cards{display:flex;gap:20px;margin-top:18px}.card{position:relative;flex:1;height:340px;padding:18px;background:#f3e9d2;border:3px solid #23303b;border-radius:8px}.card h3{font-size:32px;margin:12px 0}.rule{border-top:2px dashed #a69f93;margin:12px 0}.row{display:flex;justify-content:space-between;align-items:center;margin:10px 0;font-size:22px}.large{font-size:36px;font-weight:700}.button{display:inline-block;background:#c8553d;border:3px solid #23303b;border-radius:8px;padding:12px 24px;color:#f3e9d2;font:28px Jua;box-shadow:4px 4px #23303b66}.button.alt{background:#f3e9d2;color:#23303b}.timer{position:absolute;right:28px;top:20px;background:#f3e9d2;border:5px double #23303b;padding:10px 24px;font-size:32px;font-weight:700}.caption{position:absolute;bottom:0;left:0;right:0;padding:8px 24px;background:#23303b;color:#f3e9d2;font-size:14px}.cargo-icon{height:96px;display:flex;align-items:center;justify-content:center}.parcel{width:120px;height:82px;background:#bd8849;border:3px solid #23303b;position:relative;transform:rotate(-4deg);border-radius:8px}.parcel:before{content:'';position:absolute;left:48px;width:20px;height:76px;background:#f2c230}.parcel:after{content:'앞 ↑';position:absolute;right:7px;bottom:6px;background:#f3e9d2;padding:3px;font-size:15px}.cake{width:120px;height:64px;border:3px solid #23303b;border-radius:50% 50% 15px 15px;background:#c8553d;border-top:24px solid #f3e9d2;box-shadow:0 0 0 3px #a69f93}.call{left:300px;top:100px;width:680px;height:514px;padding:32px}.call h2{font-size:48px;margin:10px 0}.call p{font-size:25px;line-height:1.55}.stamp{border:7px double #d93a2b;border-radius:50%;color:#d93a2b;text-align:center;font-family:Jua;transform:rotate(-7deg)}.receipt{left:390px;top:40px;width:500px;height:636px;padding:30px}.receipt h2{text-align:center;margin:16px 0;font-size:44px}.receipt .row{font-size:21px}.receipt:after{content:'';position:absolute;left:0;right:0;bottom:-10px;height:10px;background:linear-gradient(135deg,#f3e9d2 25%,transparent 25%) -10px 0,linear-gradient(225deg,#f3e9d2 25%,transparent 25%) -10px 0;background-size:20px 20px}.mono{font-variant-numeric:tabular-nums}'''
(ROOT/'style.css').write_text(css,encoding='utf-8')
def doc(body):return '<!doctype html><html lang="ko"><meta charset="utf-8"><link rel="stylesheet" href="style.css"><body>'+body+'</body></html>'
scenes=[]
cards=''
for idx,(place,icon,sec,reward) in enumerate([('쇼핑몰','parcel','1분 30초','₩900'),('시청','cake','50초','₩1,050'),('부두','parcel','1분 10초','₩800')]):
    cards+=f'<div class="card"><span class="kicker">배달 전표 / 0{idx+1}</span><div class="rule"></div><div class="cargo-icon"><div class="{icon}"></div></div><h3>{place}</h3><div class="row"><span>제한 시간</span><b>{sec}</b></div><div class="row"><span>배달 보상</span><b>{reward}</b></div></div>'
scenes.append(('orders','주문 카드',f'<div class="panel paper"><div class="kicker">PIT CREW / 배달 사무소</div><h2>오늘은 어디로 갈까요?</h2><div class="cards">{cards}</div><div style="display:flex;justify-content:space-between;align-items:center;margin-top:18px"><span>최대 2건 선택 · 화물은 조심히!</span><span class="button">선택한 주문 포장하기 →</span></div></div>','proto-d119-board-call.png'))
scenes.append(('call','긴급 전화','<div class="panel paper call"><div class="kicker">전화 쪽지 / 쇼핑몰 택배 데스크</div><h2>따르릉, 급한 배달이에요!</h2><div style="position:absolute;right:25px;top:18px;color:#d93a2b;font:28px Jua;border:4px solid #d93a2b;transform:rotate(8deg);padding:4px 9px">긴급</div><div class="rule"></div><p>“오늘 마감인 반품 상자예요.<br>튼튼하니까 마음껏 밟으셔도 돼요!”</p><div class="row"><span>상자 1개 → 쇼핑몰</span><b>₩600 + 긴급 ₩300</b></div><div class="row"><span>남은 시간</span><b class="large">1:30</b></div><div style="display:flex;justify-content:flex-end;gap:20px;margin-top:28px"><span class="button alt">못 가요</span><span class="button">받을게요!</span></div></div>','proto-d119-board-call.png'))
scenes.append(('packing','포장 도장','<div class="panel paper" style="left:16px;top:18px;width:1248px;height:666px"><h2>흔들려도 든든하게</h2><div class="rule"></div><div style="position:absolute;left:28px;top:140px;width:220px"><h3 style="font-size:30px">포장 재료</h3><p style="font-size:24px">1　뽁뽁이　₩30/m</p><p style="font-size:24px">2　완충재　₩70/m</p><p style="font-size:24px">3　단단한 케이스</p><div class="rule"></div><p>랩 폭　━━━━●━━</p></div><div style="position:absolute;left:330px;top:180px;width:450px;height:360px;background:#e4d5b3;border:3px solid #a69f93;border-radius:12px"><div class="parcel" style="position:absolute;width:270px;height:230px;left:82px;top:70px;transform:none"></div><span style="position:absolute;left:180px;top:25px;font:28px Jua">앞 ↑</span></div><div style="position:absolute;right:30px;top:150px;width:290px"><h3 style="font-size:30px">포장 비용</h3><div class="row"><span>뽁뽁이</span><b>₩120</b></div><div class="row"><span>완충재</span><b>₩70</b></div><div class="rule"></div><div class="row"><b>합계</b><b>₩190</b></div><div class="row"><span>평균 커버</span><b>92%</b></div><div style="height:24px;background:#a69f93;border:3px solid #23303b;border-radius:8px"><div style="height:100%;width:92%;background:#6a9a3a"></div></div></div><div class="stamp" style="position:absolute;left:475px;top:235px;width:250px;height:250px;padding-top:33px;background:#f3e9d2e8"><div style="font-size:28px">포장 완료</div><div style="font-size:96px;line-height:1.1">A</div><div style="font-size:24px">보호 92%</div></div><div class="timer">0:40</div></div>','proto-d118-packing.png'))
scenes.append(('receipt','정산 영수증','<div class="panel paper receipt"><div class="kicker" style="text-align:center">PIT CREW / 배달 영수증</div><h2>무사히 도착!</h2><div class="rule"></div><div class="row"><span>주문</span><b>생일 케이크</b></div><div class="row"><span>배달 완료</span><b>1 / 1곳</b></div><div class="row"><span>소요 / 목표</span><b>17초 / 50초</b></div><div class="row"><span>화물 상태</span><b>100% · 완벽</b></div><div class="rule"></div><div class="row"><span>배달 보상</span><b>₩800</b></div><div class="row"><span>시간 보너스</span><b>₩250</b></div><div class="row"><strong>합계</strong><strong class="large">₩1,050</strong></div><div style="color:#6a9a3a;font:30px Jua;text-align:center;margin:20px 0">✓ 한 점 흠 없이 도착했어요</div><div class="rule"></div><div style="display:flex;gap:18px;justify-content:center;margin-top:25px"><span class="button">다시 배달</span><span class="button alt">차고로</span></div></div>','proto-d118-result.png'))
for slug,title,body,bg in scenes:
    if slug=='receipt': body='<div style="position:absolute;left:106px;top:27px;width:1068px;height:666px;background:#23303b"></div>'+body
    if slug=='call': body='<div style="position:absolute;left:273px;top:143px;width:734px;height:436px;background:#23303b"></div>'+body
    scene=f'<div class="scene" style="background-image:url(reference/{bg})">{body}<div class="caption">D-121 원화 시안 · {title} · 기존 QA 캡처 위 HTML 합성 / 실제 게임 적용 아님 · 수치는 예시</div></div>'
    (ROOT/f'mockup-{slug}.html').write_text(doc(scene),encoding='utf-8')
overview='<header><span class="badge">도현 승인 필요 · 원화/시안 단계</span><h1>배달 사무소의 종이와 물건</h1><p>D-121 / 2026.10.23 · Jua + Pretendard · 따뜻한 한낮 해안</p></header><section><h2>01 / 화면에 얹어 보기</h2><p>기존 QA 캡처 위 합성 목업. 게임 적용·기능 변경을 뜻하지 않습니다.</p><div class="grid">'
for slug,title,_,_ in scenes:overview+=f'<div class="tile"><h3>{title}</h3><a href="mockup-{slug}.html"><img src="mockup-{slug}.png" alt="{title} 목업"></a></div>'
overview+='</div></section><section><h2>02 / 슬롯 10종 · 크기와 고정 여백</h2><p>PNG는 글자 없는 바탕입니다. 빨간 안내선은 guide 파일에만 있습니다. L/B/R/T = 왼쪽/아래/오른쪽/위 px. 흰색 원본은 코드 틴트용.</p><div class="grid">'
for owner,name,w,h,b,tint in specs:overview+=f'<div class="tile"><a href="sprites/{name}.png"><img src="guides/{name}-guide.png" alt="{name}"></a><p>{"원색 유지 / 흰 틴트" if tint=="original" else "흰색·회색 / 상태색 틴트"} · {"정비율 Simple" if not any(b) else "Sliced"}</p></div>'
overview+='</div><h3>확장 확인 · PNG 참조 구현</h3><img src="slice-proof.png"></section><section><h2>03 / 장난감 화물 · 위 / 앞 / 옆</h2><p>형태·색 원화입니다. 생성 그림의 약한 원근은 제작 치수의 근거가 아니며, 아래 치수표를 제작 기준으로 씁니다. 산호색 화살표 = 포장 앞면 표식 위치.</p><img src="cargo-concepts-v02.png"><table><tr><th>화물</th><th>목표 치수 W×D×H (m)</th><th>예상 tris</th><th>앞면(+Z) 표시</th></tr>'
cargo=[('상자','0.40 × 0.30 × 0.28','360','오른쪽 아래 송장'),('케이크','0.30 × 0.30 × 0.24','1,320','받침 테두리 중앙'),('계란판','0.24 × 0.16 × 0.14','1,560','긴 변 중앙; 뚜껑은 뒤'),('와인','0.12 × 0.12 × 0.30','720','병의 정면 타원 라벨'),('생선+상자','0.50 × 0.22 × 0.19','1,680','상자 긴 변 중앙')]
for row in cargo:overview+='<tr>'+''.join(f'<td>{v}</td>' for v in row)+'</tr>'
overview+='</table><p>모두 ≤2,000 tris 계획값이며 실측 아님. 바닥 중심 피벗 / Y-up / m / 단색·플랫 셰이딩. 생선의 과장된 머리, 케이크의 굵은 크림, 와인의 넓은 어깨를 유지합니다.</p></section><section><h2>승인 항목</h2><p>① 종이·법랑·도장 형태와 4개 화면 목업 ② 화물 5종의 실루엣·색·앞면 위치</p><p>승인 후 Blender 제작 → 미리보기 승인 → QA/프로그래밍 임포트 필요.</p><p><a href="NOTES.md">참고·변경·검증·남은 이슈</a> · <a href="sprites.json">슬롯 명세 JSON</a> · <a href="validation.json">정적 검사</a></p></section>'
overview=overview.replace('01 / ??? ?? ??</h2>', '01 / ??? ?? ??</h2><p><a href="mockup-overview.png">4?? ?? PNG</a> ? <a href="cargo-sheet.png">?? ?? ?? PNG</a></p>')
(ROOT/'index.html').write_text(doc(overview),encoding='utf-8')
print('Built 10 sprites, 10 guides, 4 mockup HTML, index, manifest and slice proof.')
