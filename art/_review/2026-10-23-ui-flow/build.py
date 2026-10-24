"""UI investigation 02. Research annotations + code-drawn review layouts. No Unity writes."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, shutil, html, hashlib

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
FONTS = OUT / 'fonts'
FONTS.mkdir(exist_ok=True)
for src, dest in [
    ('art/_review/2026-10-23-ui-refboard/fonts/Galmuri11.ttf', 'Galmuri11.ttf'),
    ('art/_review/2026-10-23-ui-refboard/fonts/Galmuri11.woff2', 'Galmuri11.woff2'),
    ('art/_review/2026-10-23-ui-refboard/fonts/LICENSE.txt', 'Galmuri-OFL.txt'),
    ('F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf', 'Pretendard-Regular.ttf'),
    ('F1/Assets/UI/Fonts/Pretendard/OFL.txt', 'Pretendard-OFL.txt'),
]:
    shutil.copyfile(ROOT / src, FONTS / dest)
P = json.loads((ROOT / 'art/source/palette/pitcrew_d123_32.json').read_text('utf-8'))
COLORS = {x['name']: x['hex'] for x in P['entries']}
PAPER, INK, DARK, MUTED, TEAL, AMBER, STAMP = [COLORS[x] for x in ['label','ink','scene_06','scene_04','scene_18','carton_light','stamp']]
G = FONTS / 'Galmuri11.ttf'
PR = FONTS / 'Pretendard-Regular.ttf'

def text(im, xy, value, size=24, color=INK, font='g', shadow=False):
    """Monochrome native glyph mask -> exact 2x/3x nearest. Integer coordinates only."""
    scale = 3 if font == 'g' and size == 36 else 2
    base = 12 if font == 'g' else size // scale
    f = ImageFont.truetype(str(G if font == 'g' else PR), base)
    box = f.getbbox(value)
    mask = Image.new('1', (max(1, box[2]-box[0]), max(1, box[3]-box[1])))
    ImageDraw.Draw(mask).text((-box[0], -box[1]), value, font=f, fill=1)
    mask = mask.resize((mask.width*scale, mask.height*scale), Image.Resampling.NEAREST)
    x,y = map(int,xy)
    if shadow:
        im.paste(DARK,(x+2,y+2),mask)
    im.paste(color,(x,y),mask)
    return [x,y,mask.width+(2 if shadow else 0),mask.height+(2 if shadow else 0)]

def rect(im, box, fill=None, outline=None, width=2):
    x,y,w,h=box
    ImageDraw.Draw(im).rectangle((x,y,x+w-1,y+h-1),fill=fill,outline=outline,width=width)

def line(im, pts, fill=INK, width=2): ImageDraw.Draw(im).line(pts,fill=fill,width=width)

def button(im, box, label, primary=False):
    rect(im,box,fill=INK if primary else PAPER,outline=INK)
    text(im,(box[0]+16,box[1]+14),label,24,PAPER if primary else INK)

def save(im,name):
    im.save(OUT/name)
    return name

AUDITS = [
 {'id':'board','title':'주문판 — 전화 아래 노출 부분만 감사','source':'20261023-qa-0-board.png','issues':[
   {'box':[205,108,550,324],'label':'V · 짙은 보드 + 밝은 카드 + 전화가 겹쳐 단계 구분이 흐림. 종이 전표 한 층으로 정리.'},
   {'box':[232,151,58,203],'label':'V · 카드가 전화에 가림. 전체 카드의 위계·줄바꿈은 [미확인]; 전화 없는 캡처 필요.'},
   {'box':[365,409,305,19],'label':'V/C · 하단 안내가 작고 “출발”은 실제 포장 진입과 다름. “포장하러”로 행동 명시.'},
   {'box':[818,18,125,45],'label':'C · 주문판부터 90초가 흐르지만 시작 규칙이 보이지 않음. 공용 타이머에 단계 안내.'},
 ]},
 {'id':'call','title':'긴급전화 — 같은 시간은 한 군데만','source':'20261023-qa-0-board.png','issues':[
   {'box':[308,344,158,22],'extra':[[818,18,125,45]],'label':'V/C · 1:22 / 1:21 중복. 전화 CeilToInt, HUD FloorToInt. 한 데이터·한 표기로 통일.'},
   {'box':[310,279,275,39],'label':'V/C · 배달지·화물·보상이 작고 옅음. 코드 22px → 960 기준 약 11px em.'},
   {'box':[290,140,380,35],'label':'V · 넓은 빨강 띠와 둥근 제목이 D-123과 불일치. 얇은 잉크선·짧은 긴급 표식으로.'},
   {'box':[553,336,105,40],'label':'C · “받을게요!”는 주문 선택 후 보드 복귀. “이 주문 추가”와 선택 결과를 명시.'},
 ]},
 {'id':'packing','title':'포장 — 작업면과 판단 정보의 위계','source':'20261023-1551-card-Order_04_WeddingCake.png','issues':[
   {'box':[8,56,165,401],'extra':[[675,75,270,435]],'label':'V · 짙은 남색 패널이 화면을 분할. 작업면 주변을 바랜 작업 전표로 통합.'},
   {'box':[692,120,235,105],'label':'V · 세 재료 비용·합계와 좌측 예상 비용이 동시에 경쟁. 합계 우선, 내역 접기.'},
   {'box':[689,467,234,14],'label':'V/C · 면 설명은 코드 16px → 960 기준 약 8px. 작은 주석 대신 24px 핵심 행동.'},
   {'box':[448,146,69,88],'label':'V · 큰 빨강 “앞”이 작업 화물을 덮음. 면 외곽의 작은 방향 라벨로 이동.'},
   {'box':[800,0,160,66],'label':'V/C · 이 캡처에 “배치하기”가 안 보임. 코드 버튼·타이머 위치 충돌/배선은 QA 재현 필요.'},
 ]},
 {'id':'driving','title':'주행 — 풍경 위 UI 판과 마커','source':'20261023-flow-3-driving.png','issues':[
   {'box':[711,82,205,183],'label':'V · 트렁크 화면만 약 7.2%. 기본은 상태 1줄, 상세 화면은 정차/요청 시로 제안.'},
   {'box':[232,0,208,226],'label':'V · 노란 월드 마커가 지붕·하늘을 가림. 화면 구석 방향 정보로 축소.'},
   {'box':[348,18,265,45],'extra':[[818,18,125,45]],'label':'V · 목적지 중앙·시간 우측·화물 좌하단으로 시선 왕복. 상단 좌우 고정, 상태는 한 묶음.'},
   {'box':[18,343,165,61],'extra':[[840,390,101,101]],'label':'V · 0 km/h와 N 판이 큼. 핵심 3정보보다 속도·기어가 강함; 배경 판 제거.'},
   {'box':[18,448,283,45],'label':'V/C · 큰 초록 상태바 + 별도 경고 5Hz 코드. 상태 단어·숫자, 경고 한 번 전환으로 제안.'},
 ]},
 {'id':'result','title':'정산 — 손에 남은 돈과 돌아갈 행동','source':'20261023-1536-result-profit.png','issues':[
   {'box':[79,4,803,517],'label':'V · 큰 종이가 화면 약 80%를 차지하지만 아래 절반은 비어 있음. 세로 영수증으로 집중.'},
   {'box':[110,106,741,143],'label':'V · 라벨과 값이 양끝. 순이익도 같은 행 위계. 순이익을 첫 줄, 보상·비용을 바로 아래.'},
   {'box':[588,150,265,13],'label':'V/C · 긴 시간 문장·자동 축소(18~32 코드px). 960에서 약 9~16px; 두 항목으로 분리.'},
   {'box':[328,469,304,39],'label':'V/C · “다시 / 차고로”만 있음. 정산 뒤 느린 복귀가 없음. “돌아가기”를 주 행동으로.'},
 ]},
]
for a in AUDITS:
    src=ROOT/'docs/qa/evidence'/a['source']
    original=Image.open(src).convert('RGB')
    im=Image.new('RGB',(1920,1450),PAPER); im.paste(original,(0,70))
    text(im,(34,22),a['title'],36)
    d=ImageDraw.Draw(im)
    for n,it in enumerate(a['issues'],1):
        for b in [it['box']]+it.get('extra',[]):
            x,y,w,h=[v*2 for v in b];y+=70
            rect(im,(x,y,w,h),outline=STAMP,width=4)
            rect(im,(max(0,x),max(70,y),36,36),fill=STAMP)
            text(im,(max(0,x)+8,max(70,y)+6),str(n),24,PAPER)
        text(im,(34,1172+(n-1)*44),f"{n:02d}  {it['label']}",24)
    text(im,(34,1416),'원본: docs/qa/evidence/'+a['source']+'   |   V=화면 관찰 / C=코드 근거 / 960 좌표를 2배 표시',18,font='p')
    a['image']=save(im,'audit-'+a['id']+'.png')
(OUT/'audits.json').write_text(json.dumps(AUDITS,ensure_ascii=False,indent=2),encoding='utf-8')

def basewire(title,clock=None):
    im=Image.new('RGB',(960,540),PAPER)
    text(im,(28,24),title,36)
    line(im,[(28,76),(932,76)],MUTED)
    if clock:text(im,(766,28),clock,36)
    return im

WIRES=[]
def wire(name,title,essentials,im,note):
    file=save(im,'wire-'+name+'.png')
    WIRES.append({'id':name,'title':title,'essentials':essentials,'image':file,'note':note})

im=basewire('01  주문 전표','1:30')
text(im,(30,94),'고르기부터 배달까지 같은 시계',24)
for y,name,meta in [(142,'시청 · 케이크','보상 1,500원 / 흔들림 주의'),(248,'부두 · 활어','보상 1,200원 / 신선도 주의')]:
    rect(im,(30,y,552,94),outline=INK)
    text(im,(48,y+16),name,24)
    text(im,(48,y+56),meta,24,font='p')
rect(im,(614,142,316,200),outline=MUTED)
text(im,(638,162),'선택 1 / 3',24)
text(im,(638,204),'시청 · 케이크',24)
text(im,(638,252),'총 보상 1,500원',24,font='p')
text(im,(30,380),'전표 선택 → 포장 → 배치 → 출발',24)
button(im,(664,446,266,62),'포장하러',True)
wire('board','주문판',['남은 제한 시간','목적지·화물 묶음','선택한 주문과 보상'],im,'전화 수락/거절 뒤 이 화면으로 복귀. 포장 진입 버튼을 출발과 구분.')

im=basewire('02  긴급전화','1:22')
rect(im,(170,110,620,318),outline=INK)
text(im,(198,132),'긴급  /  시청 행사팀',24,STAMP)
line(im,[(198,170),(762,170)],MUTED)
text(im,(198,196),'곧 행사가 시작해요.',24,font='p')
text(im,(198,234),'케이크를 먼저 부탁드릴게요.',24,font='p')
text(im,(198,292),'시청 · 케이크',24)
text(im,(198,338),'기본 1,000원 + 긴급 500원',24,font='p')
button(im,(170,448,226,62),'거절')
button(im,(450,448,340,62),'이 주문 추가',True)
wire('call','긴급전화',['누가·무엇을 요청하는가','기본 보상·긴급 추가금','공용 남은 시간'],im,'위 타이머 하나 유지. 별도 전화 타이머 없음. 수락은 선택 갱신이며 자동 출발 아님.')

im=basewire('03  포장','1:08')
rect(im,(28,98,226,316),outline=INK)
text(im,(44,116),'1 버블랩',24)
text(im,(44,162),'2 폼',24)
text(im,(44,208),'3 하드케이스',24)
text(im,(44,278),'합계 230원',24)
text(im,(44,326),'보상 중 15%',24,font='p')
rect(im,(276,98,378,316),outline=MUTED)
text(im,(318,208),'화물 작업 뷰',24)
text(im,(320,254),'[면 회전]',24,MUTED)
rect(im,(678,98,254,316),outline=INK)
text(im,(696,116),'보호 예상 B',24)
for y,t in [(174,'앞 85%'),(218,'뒤 50%'),(262,'좌 45%'),(306,'우 45%')]:text(im,(696,y),t,24)
text(im,(28,446),'WASD 회전 / 드래그 감기',24,font='p')
text(im,(28,484),'배치 후 출발 · 시계는 계속',24,font='p')
button(im,(678,450,254,60),'배치하기',True)
wire('packing','포장',['남은 제한 시간','현재 면·보호 상태','누적 포장비 / 보상'],im,'포장 안에 배치 하위 단계 유지. 배치하기 → 적재 확인 → 출발. 화물 모델 제작 아님.')

def roadwire(im):
    line(im,[(190,430),(430,194),(530,194),(770,430)],MUTED)
    line(im,[(30,194),(930,194)],MUTED)
    for y in range(236,430,56):line(im,[(480,y),(480,y+22)],MUTED)
    text(im,(370,276),'풍경 / 시야',24,MUTED)

im=basewire('04  갈 때')
roadwire(im)
text(im,(30,98),'1/3  시청 →',24)
text(im,(30,134),'직선 240m',24,font='p')
text(im,(786,98),'0:42',36)
text(im,(30,464),'케이크 96%',24)
text(im,(792,476),'82 km/h',24,font='p')
wire('outbound','주행 · 갈 때',['남은 시간','다음 목적지·방향','화물 상태'],im,'속도는 보조 정보. F1 속도감은 카메라·근경·소리 담당. 중앙 60%×60%에는 HUD 없음.')

im=basewire('05  정산')
rect(im,(244,92,472,424),outline=INK)
text(im,(270,114),'배달 완료 · 시청',24)
text(im,(270,162),'순이익 +1,270원',36)
line(im,[(270,216),(690,216)],MUTED)
text(im,(270,238),'보상       1,500원',24,font='p')
text(im,(270,280),'포장비       230원',24,font='p')
text(im,(270,328),'화물 96% · 1/1 완료',24)
text(im,(270,370),'상세 내역 보기',24,MUTED,font='p')
button(im,(270,438,420,60),'돌아가기',True)
wire('result','정산',['성공·실패와 화물 결과','실수령 순이익','다음 행동: 돌아가기'],im,'도착 시 보상·비용 확정 1회. 타이머 종료. 시간 초과도 정산 후 복귀, 즉시 재주문 없음.')

im=basewire('06  돌아오는 길')
roadwire(im)
text(im,(30,104),'시간 제한 없음',24,MUTED)
text(im,(30,456),'돌아오는 길',24)
text(im,(632,480),'[Tab] 차고 길 안내',24,font='p')
wire('return','주행 · 돌아올 때',['시간 제한이 끝났다는 확인','차고 방향: 요청할 때','도착 후 다음 주문 행동'],im,'3정보 동시 상시 표시 아님. 진입 문구 4초 후 숨김 / 안내 키를 누를 때 방향 / 차고에서 주문 버튼.')
(OUT/'wireframes.json').write_text(json.dumps(WIRES,ensure_ascii=False,indent=2),encoding='utf-8')

flow=Image.new('RGB',(1980,2200),PAPER)
text(flow,(34,26),'한 판의 흐름  /  바쁜 배달 → 정산 → 느린 복귀',36)
text(flow,(34,82),'공용 시계: 주문판 · 전화 · 포장 · 갈 때  |  정산에서 종료  |  돌아올 때 새 주문·시간 압박 없음',24,font='p')
for i,w in enumerate(WIRES):
    x=(i%2)*980;y=140+(i//2)*680
    flow.paste(Image.open(OUT/w['image']),(x+20,y))
    for j,s in enumerate(w['essentials']):
        text(flow,(x+48,y+554+j*30),f'{j+1}. {s}',24)
text(flow,(34,2170),'전화는 선택 분기. 포장 안에 적재 단계 포함. 분기·예외는 index.html / ui-02-flow.md.',18,font='p')
save(flow,'flow-sheet.png')

HUD=[]
def group_bounds(rects):
    x=min(r[0] for r in rects); y=min(r[1] for r in rects)
    return [x,y,max(r[0]+r[2] for r in rects)-x,max(r[1]+r[3] for r in rects)-y]

for name,font,returning in [('outbound-a','g',False),('outbound-b','p',False),('return-a','g',True),('return-b','p',True)]:
    source='mood-A.png' if returning else 'mood-B.png'
    bg=Image.open(ROOT/'docs/qa/evidence'/source).convert('RGB').resize((960,540),Image.Resampling.NEAREST)
    overlay=Image.new('RGBA',(960,540),(0,0,0,0)); groups=[]
    def t(pos,value,size=24,color=PAPER):return text(overlay,pos,value,size,color,font,True)
    if not returning:
        groups.append(group_bounds([t((24,24),'1/3  시청 →'),t((24,58),'직선 240m')]))
        groups.append(group_bounds([t((808,24),'0:42' if font=='g' else '0:18',36),t((808,66),'남은 시간')]))
        cargo=t((24,466),'케이크 96%' if font=='g' else '화물 62%')
        if font=='g':
            rect(overlay,(24,500,172,4),DARK);rect(overlay,(24,500,164,4),PAPER)
            groups.append(group_bounds([cargo,[24,500,172,4]]))
        else:
            groups.append(group_bounds([cargo,t((24,500),'급제동 주의',24,AMBER)]))
            rect(overlay,(796,24,4,54),AMBER);groups[-2]=group_bounds([groups[-2],[796,24,4,54]])
        groups.append(group_bounds([t((814,494),'82 km/h' if font=='g' else '104 km/h')]))
        title='갈 때 A · Galmuri / 보통 상태' if font=='g' else '갈 때 B · Pretendard / 마감·화물 경고'
        note='상시 시간·행선지·상태. 속도는 보조.' if font=='g' else '경고는 글자+작은 호박색 선. 전면 점멸 없음. Pretendard는 가독성 비교용, 채택은 승인 필요.'
    else:
        if font=='g':
            groups.append(group_bounds([t((24,492),'돌아오는 길')]))
            title='돌아올 때 A · Galmuri / 진입 4초'
            note='이 문구도 4초 뒤 숨김 → 기본 HUD 0%. 제한 시간·화물·보상 숫자 없음.'
        else:
            groups.append(group_bounds([t((24,28),'차고 →'),t((24,62),'길 안내',24)]))
            title='돌아올 때 B · Pretendard / 길 안내 요청'
            note='Tab 누름 예시. 떼고 2초 뒤 숨김. 키 배정·시간은 제안이며 미구현.'
    composed=Image.alpha_composite(bg.convert('RGBA'),overlay).convert('RGB')
    save(overlay,name+'-overlay.png');save(composed,name+'.png')
    mask=Image.new('1',(960,540));md=ImageDraw.Draw(mask)
    for x,y,w,h in groups:md.rectangle((x,y,x+w-1,y+h-1),fill=1)
    count=sum(mask.getdata()); visible=sum(a>0 for a in overlay.getchannel('A').getdata())
    center=overlay.getchannel('A').crop((192,108,768,432))
    assert center.getbbox() is None, name+' center occluded'
    used=set(overlay.convert('RGB').getdata())-{(0,0,0)}
    palette={tuple(c) for c in P['colors']}
    assert used<=palette, (name,used-palette)
    assert all(x>=0 and y>=0 and x+w<=960 and y+h<=540 for x,y,w,h in groups)
    HUD.append({'id':name,'title':title,'note':note,'image':name+'.png','overlay':name+'-overlay.png','background':source,'groups':groups,'envelope_pixels':count,'envelope_percent':round(count/518400*100,2),'ink_percent':round(visible/518400*100,2),'center_clear':True,'palette_outside':0,'size':[960,540]})
(OUT/'mockups.json').write_text(json.dumps(HUD,ensure_ascii=False,indent=2),encoding='utf-8')

sheet=Image.new('RGB',(1980,1280),PAPER)
text(sheet,(28,20),'갈 때 급하게, 돌아오는 길이 보상  /  HUD 2 + 2',36)
for i,h in enumerate(HUD):
    x=20+i%2*980;y=82+i//2*590
    text(sheet,(x,y),h['title'],24)
    sheet.paste(Image.open(OUT/h['image']),(x,y+32))
save(sheet,'hud-sheet.png')

def esc(s): return html.escape(s)
def img(src,alt,cls=''):return f'<a href="{src}" target="_blank"><img class="{cls}" src="{src}" alt="{esc(alt)}"></a>'
audithtml=''.join(f'<article class="audit"><h3>{esc(a["title"])}</h3>{img(a["image"],a["title"])}<p class="source">原 캡처: <a href="../../../docs/qa/evidence/{a["source"]}">{a["source"]}</a></p><ol>'+''.join(f'<li>{esc(it["label"])}</li>' for it in a['issues'])+'</ol></article>' for a in AUDITS)
wirehtml=''.join(f'<article class="wire"><h3>{i+1:02d} · {esc(w["title"])}</h3>{img(w["image"],w["title"])}<ol>'+''.join(f'<li>{esc(s)}</li>' for s in w['essentials'])+f'</ol><p>{esc(w["note"])}</p></article>' for i,w in enumerate(WIRES))
hudhtml=''.join(f'<article class="hud"><div class="hudtitle"><h3>{esc(h["title"])}</h3><span>UI {h["envelope_percent"]}%</span></div><div class="pixel-scroll">{img(h["image"],h["title"],"native")}</div><p>{esc(h["note"])}</p><p class="source">배경: <a href="../../../docs/qa/evidence/{h["background"]}">{h["background"]}</a> · HUD 합성 목업 / 가상 수치 / 실제 새 Play 아님 · <a href="{h["overlay"]}">투명 HUD</a></p></article>' for h in HUD)
css='''@font-face{font-family:P;src:url(fonts/Pretendard-Regular.ttf)}@font-face{font-family:G;src:url(fonts/Galmuri11.woff2)}*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#e4d6bf;color:#30362f;font-family:P,sans-serif;line-height:1.6}a{color:inherit;text-underline-offset:4px}header,main,footer{max-width:1280px;margin:auto;padding:40px}header{border-bottom:2px solid #30362f;padding-top:62px}.kicker{letter-spacing:.14em;font-size:13px;color:#795244}h1{font-family:G;font-size:36px;line-height:1.45;margin:14px 0 22px}h2{font-size:29px;font-weight:400;margin:14px 0}h3{font-size:20px;margin:0 0 18px}p{margin:8px 0 16px}.lead{font-size:21px;max-width:900px}.badge{display:inline-block;border:1px solid #8a4631;color:#8a4631;padding:4px 12px;font-size:14px}nav{display:flex;gap:22px;flex-wrap:wrap;margin-top:28px}section{padding:35px 0;border-bottom:1px solid #97826a}.sectionnum{font:24px G;color:#795244}.notice{border-left:4px solid #8a4631;padding:10px 20px;background:#c4a687;margin:24px 0}.summary{display:grid;grid-template-columns:repeat(3,1fr);gap:18px}.summary div{border-top:2px solid #30362f;padding:16px 0}.summary strong{display:block;font:24px G;margin-bottom:10px}img{max-width:100%;display:block;height:auto;image-rendering:pixelated}article{margin:34px 0 52px}.audit img{border:1px solid #97826a}.audit ol{columns:2;column-gap:42px;padding-left:22px}.audit li{break-inside:avoid;margin:10px 0}.source{font-size:13px;color:#525350;overflow-wrap:anywhere}.grid{display:grid;grid-template-columns:1fr 1fr;gap:26px}.wire{margin:16px 0}.wire img{border:1px solid #97826a}.wire ol{padding-left:22px}.wire p{font-size:15px}.route{background:#30362f;color:#e4d6bf;padding:26px;font:24px/1.9 G}.hud{border-top:1px solid #97826a;padding-top:24px}.hudtitle{display:flex;justify-content:space-between;align-items:baseline}.hudtitle span{font:24px G}.pixel-scroll{overflow-x:auto;background:#231f1b;padding:12px}.native{width:960px;max-width:none;image-rendering:pixelated}.pixel-scroll a{display:block;width:960px;margin:auto}.swatches{display:flex;gap:12px;flex-wrap:wrap}.swatch{width:132px;border:1px solid #97826a;font-size:13px;padding:10px}.swatch i{display:block;height:42px;margin-bottom:8px}table{width:100%;border-collapse:collapse;font-size:15px}td,th{border-bottom:1px solid #97826a;padding:12px;text-align:left}th{font-weight:400;color:#795244}footer{font-size:14px}button{font:inherit;background:#30362f;color:#e4d6bf;border:0;padding:8px 14px;cursor:pointer}body.scaled .native{max-width:100%;width:960px}body.scaled .pixel-scroll a{max-width:100%}@media(max-width:760px){header,main,footer{padding:24px}.grid,.summary{grid-template-columns:1fr}.audit ol{columns:1}h1{font-size:28px}.route{font-size:20px}.hudtitle{display:block}}'''
swatches=''.join(f'<div class="swatch"><i style="background:{COLORS[k]}"></i>{esc(k)}<br>{COLORS[k]}</div>' for k in ['label','ink','scene_06','scene_18','carton_light','stamp'])
page=f'''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>PitCrew · UI 02 / 한 판과 두 박자</title><style>{css}</style><body>
<header><div class="kicker">PITCREW / ART UI RESEARCH 02 / 2026.10.23</div><h1>갈 때 급하게.<br>돌아오는 길이 보상.</h1><p class="lead">현재 화면의 읽는 순서를 줄이고, 정산 뒤 풍경을 돌려주는 화면 흐름.</p><span class="badge">검토 시안 · 도현 승인 필요</span><nav><a href="#audit">01 현재 감사</a><a href="#flow">02 한 판 흐름</a><a href="#hud">03 두 박자 HUD</a><a href="#spec">04 적용 조건</a><a href="../../../docs/research/ui-02-flow.md">근거 문서</a></nav></header>
<main><div class="summary"><div><strong>5 화면</strong>실제 QA 캡처 위 번호로 문제 표시.<br>화면마다 최대 5개.</div><div><strong>6 단계</strong>긴급전화는 선택 분기.<br>포장 안에 적재 하위 단계.</div><div><strong>960 × 540</strong>HUD 목업 4장, 실제 픽셀.<br>Galmuri / Pretendard, 공용 팔레트.</div></div>
<div class="notice">정적 조사·합성 목업입니다. Unity 실행·임포트·주행 검증은 하지 않았습니다. 주문판은 전화에 가려져 전체 판독이 불가능합니다. 배경은 프로그래밍팀의 기존 무드 비교 캡처이며 조명 승인이나 F1 속도감 검증을 뜻하지 않습니다.</div>
<section id="audit"><div class="sectionnum">01</div><h2>현재 UI 감사</h2><p>V는 화면 관찰, C는 코드 근거. 원본 캡처를 바꾸지 않고 별도 파일에 주석을 겹쳤습니다. 작은 글자 환산은 1920 UI를 960으로 줄였을 때의 조건부 em 크기입니다.</p>{audithtml}<p>별도 재현 필요: <a href="../../../docs/qa/evidence/20261023-qa-6-packing-all.png">전화가 포장 위에 남은 캡처</a>. 현재 소스는 보드에서 전화를 띄웁니다. 호출/종료 경로는 Play로 확인하지 않았습니다.</p></section>
<section id="flow"><div class="sectionnum">02</div><h2>한 판의 화면 흐름</h2><div class="route">주문판 → (긴급전화 ↩ 선택 갱신) → 포장 → 갈 때<br>→ 정산 → 돌아오는 길 → 차고에서 다음 주문</div><p>전화 거절도 주문판으로 복귀. 전화 없이 포장 가능. 공용 시계는 주문판에서 시작하고 정산에서 끝납니다. 0초 도달은 어느 작업 단계에서도 정산으로 보내는 변경안입니다. 기존 출발 시 실패 처리와 다릅니다.</p><div class="grid">{wirehtml}</div><p>각 와이어프레임의 번호 목록이 필수 정보 3개입니다. 돌아오는 길의 3개는 진입 / 요청 / 도착 시 나눠 보입니다. <a href="flow-sheet.png">흐름 PNG 시트</a></p></section>
<section id="hud"><div class="sectionnum">03</div><h2>같은 동네, 두 박자</h2><p>기본 표시는 1 이미지 픽셀 = 1 CSS 픽셀. 960px보다 좁으면 가로 스크롤합니다. 운영체제·브라우저 확대는 100%에서 판정하세요.</p><button id="scale" aria-pressed="false">축소 비교 보기 전환</button><p class="source">축소 비교는 전체 구성 확인용입니다. 글자 크기·정수배 판정에는 원본 PNG를 사용하세요.</p>{hudhtml}<p>면적 = 텍스트·선의 묶음별 최소 사각형 합집합 / 518,400px². 투명 여백 포함, 배경 제외. 중앙 576×324 영역 침범 0. 기본 복귀 문구가 사라지면 0%. <a href="hud-sheet.png">4장 PNG 시트</a> / <a href="mockups.json">좌표·측정값</a></p></section>
<section id="spec"><div class="sectionnum">04</div><h2>적용 조건과 아직 남은 확인</h2><div class="swatches">{swatches}</div><p>UI 색은 <a href="../../source/palette/pitcrew_d123_32.json">D-123 공용 32색</a>에서만 사용. 원본 풍경 캡처는 재양자화하지 않았습니다. 흰 글자 역할은 순백 대신 label 크림색. 작은 잉크 그림자로 명도를 확보했습니다.</p>
<table><tr><th>항목</th><th>이번 시안</th><th>이후 확인</th></tr><tr><td>글자</td><td>Galmuri11 12px 원형 → 24/36px. Pretendard 12/18px 마스크 → 24/36px.</td><td>960 원본·2×·3×에서 한국어 읽기. TMP 재현 별도.</td></tr><tr><td>상태 전이</td><td>Result 뒤 Return 신규 단계. 시간 종료·보상 확정 후 진입.</td><td>기존 DeliveryManager.Returning은 정산 전 복귀. 별도 분리 필요.</td></tr><tr><td>목적지 거리</td><td>“직선 240m”는 예시 수치. 도로 경로 거리로 주장하지 않음.</td><td>실제 경로 거리 제공 시 문구 변경. 긴 이름 줄바꿈·최장값 확인.</td></tr><tr><td>스프라이트</td><td>UITheme 7 + Packing 3 슬롯 유지. 이번 제작은 목업만.</td><td>출시용 스프라이트 0. 슬롯별 크기·9-slice 제안은 문서 참조.</td></tr><tr><td>승인</td><td>도현 승인 필요: 위계·HUD A/B·복귀 흐름.</td><td>QA/프로그래밍 임포트 필요. 동작·소리·F1 속도감은 [미확인].</td></tr></table>
<p><a href="../../../docs/research/ui-01-references.md">1/3 근거</a> → <a href="../../../docs/research/ui-02-flow.md">2/3 상세 문서</a> · <a href="sources.json">원본·코드 SHA256</a> · <a href="validation.json">검증 기록</a> · <a href="build.py">재생성 스크립트</a></p></section></main>
<footer>UI 아트팀 / 과제 20261023-1723 · Galmuri / Pretendard OFL: <a href="fonts/Galmuri-OFL.txt">Galmuri 라이선스</a>, <a href="fonts/Pretendard-OFL.txt">Pretendard 라이선스</a> · UI 구조·텍스트 코드 제작, AI 생성 이미지 0. 3D 화물 변경 없음.</footer>
<script>document.getElementById('scale').onclick=function(){{document.body.classList.toggle('scaled');this.setAttribute('aria-pressed',document.body.classList.contains('scaled'));}};</script></body></html>'''
(OUT/'index.html').write_text(page,encoding='utf-8')
print(json.dumps({'audits':len(AUDITS),'wireframes':len(WIRES),'hud':[{k:h[k] for k in ['id','envelope_percent','ink_percent']} for h in HUD]},ensure_ascii=False,indent=2))
