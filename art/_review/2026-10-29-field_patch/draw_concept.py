"""Approval drawings only. Pillow vector primitives; no AI image model, no mesh or albedo.
Run: python -X utf8 art/_review/2026-10-29-field_patch/draw_concept.py
"""
from pathlib import Path
from math import sin, cos, pi
import json
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
S = 2
W, H = 1680, 1430
im = Image.new('RGB', (W*S, H*S), '#f2f0e8')
d = ImageDraw.Draw(im)
FONT = 'C:/Windows/Fonts/malgun.ttf'
BOLD = 'C:/Windows/Fonts/malgunbd.ttf'
def txt(x,y,t,size=22,c='#30433c',bold=False):
    d.text((x*S,y*S),t,font=ImageFont.truetype(BOLD if bold else FONT,size*S),fill=c)
def line(pts,c='#8b968a',w=1): d.line([(round(x*S),round(y*S)) for x,y in pts],fill=c,width=round(w*S))
def poly(pts,c): d.polygon([(round(x*S),round(y*S)) for x,y in pts],fill=c)
def rect(b,c): d.rectangle(tuple(round(v*S) for v in b),fill=c)
def ellipse(b,c): d.ellipse(tuple(round(v*S) for v in b),fill=c)

txt(55,35,'FIELD PATCH',17,'#6e796a',True)
txt(55,65,'엄마가 가꾸는 작은 밭',40,bold=True)
txt(55,126,'형태·배치 승인용 도식  /  2026.10.29  /  원화 승인 대기',20)
txt(1120,69,'A  ·  5줄 안',27,bold=True)
txt(1120,111,'12.0 × 8.8 m   /   높이 1.35 m',21)
line([(55,170),(1625,170)])

# World drawing: Z up, house approach at -X. Painter ordering by y-x.
shapes=[]
LAYER=2
def pr(p):
    x,y,z=p
    return (800+49*x+32*y,487-15*x+24*y-65*z)
def face(p,c): shapes.append((LAYER, sum(v[1]-v[0]+v[2]*.5 for v in p)/len(p),p,c))
def box(x0,x1,y0,y1,z0,z1,cols):
    top,front,side=cols
    face([(x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)],top)
    face([(x0,y1,z0),(x1,y1,z0),(x1,y1,z1),(x0,y1,z1)],front)
    face([(x0,y0,z0),(x0,y1,z0),(x0,y1,z1),(x0,y0,z1)],side)
def leaf(x,y,z,a,r=.24):
    vx,vy=cos(a),sin(a); nx,ny=-vy,vx
    p=[(x,y,z),(x+vx*r*.55+nx*r*.3,y+vy*r*.55+ny*r*.3,z+r*.5),
       (x+vx*r,y+vy*r,z+r*.24),(x+vx*r*.55-nx*r*.3,y+vy*r*.55-ny*r*.3,z+r*.25)]
    face(p,['#47745a','#5a8057','#739262','#3d6551'][int(a*3)%4])
def low(x,y):
    for j in range(7): leaf(x,y,.22,j*2*pi/7,.32)
    for j in range(3): leaf(x,y,.3,j*2*pi/3,.18)
def tall(x,y):
    box(x+.13,x+.17,y-.02,y+.02,.12,1.35,('#aa9670','#8a795f','#786e59'))
    box(x-.018,x+.018,y-.018,y+.018,.15,1.04,('#597653','#44664b','#375744'))
    for j in range(7):
        leaf(x,y,.35+j*.095,j*2.4,.27)
    # Few late-season fruit; dark muted red, no ornamental abundance.
    face([(x-.17,y,.59),(x-.11,y,.61),(x-.12,y,.43),(x-.15,y,.41)],'#a15942')

LAYER=0
box(-6,6,-4.4,4.4,-.3,0,('#aaa17b','#82735d','#746b58'))
# Narrow, unframed soil ridges; 0.55m furrows. Empty row is visibly tilled.
rows=[-2.8,-1.4,0,1.4,2.8]
for row,y in enumerate(rows):
    LAYER=1
    face([(-3.15,y-.425,0),(4.25,y-.425,0),(4.1,y-.29,.19),(-3.0,y-.29,.19)],'#9b7c5c')
    face([(-3.,y-.29,.19),(4.1,y-.29,.19),(4.1,y+.29,.19),(-3.,y+.29,.19)],'#aa8963')
    face([(-3.,y+.29,.19),(4.1,y+.29,.19),(4.25,y+.425,0),(-3.15,y+.425,0)],'#806b52')
    face([(-3.15,y-.425,0),(-3.,y-.29,.19),(-3.,y+.29,.19),(-3.15,y+.425,0)],'#8e785a')
    if row==2:
        for j in range(20):
            x=-2.9+j*.35
            face([(x,y-.25,.194),(x+.06,y-.25,.194),(x+.1,y+.25,.194),(x+.04,y+.25,.194)],'#977b5a')
    else:
        LAYER=2
        for j in range(12):
            x=-2.75+j*.58
            (tall if row==0 else low)(x,y)
# Partial low timber boundary behind crops and one short return.
LAYER=2
for x in [0,1.25,2.5,3.75,5.0]:
    box(x-.045,x+.045,-3.9,-3.81,0,.62,('#b8a281','#9a896d','#8b806b'))
box(-.05,5.05,-3.88,-3.82,.38,.44,('#b8a281','#9a896d','#8b806b'))
for y in [-2.5,-1.2]: box(4.96,5.05,y-.045,y+.045,0,.62,('#b8a281','#9a896d','#8b806b'))
box(4.99,5.05,-3.8,-1.2,.38,.44,('#b8a281','#9a896d','#8b806b'))
for i in range(4):
    x=-5.55+i*.58; y=2.8-i*.27
    p=[(x-.24,y-.19,.035),(x+.19,y-.22,.035),(x+.27,y+.1,.035),(x+.11,y+.24,.035),(x-.25,y+.17,.035)]
    face(p,'#b4b3a0')
# Watering can and hand hoe as separate pictograms in scale.
box(-4.15,-3.8,.94,1.24,0,.4,('#719398','#527a7d','#42676b'))
face([(-3.81,1.02,.16),(-3.46,1.04,.36),(-3.45,1.1,.42),(-3.81,1.13,.26)],'#648b8c')
box(-4.42,-4.37,.45,1.2,.035,.085,('#b49c78','#9a8266','#827359'))
box(-4.6,-4.22,.44,.58,.02,.065,('#7d8a8a','#637575','#526969'))
for _,__,p,c in sorted(shapes,key=lambda s:(s[0],s[1])): poly([pr(v) for v in p],c)
# Can handle after solids; illustrative line only, not mesh.
a=pr((-4.1,1.08,.38)); b=pr((-4.1,1.08,.57)); c=pr((-3.86,1.08,.57)); e=pr((-3.86,1.08,.38))
line([a,b,c,e],'#527a7d',3)

txt(76,224,'낮은 나무 울타리는 뒤와 옆 일부만',21,bold=True)
line([(460,244),pr((2,-3.85,.62))],'#7b8777',1.5)
txt(1090,234,'고추형 작물 1줄',21,bold=True)
txt(1090,269,'지지대 1.35m · 수확 끝물',19)
line([(1100,310),pr((2.5,-2.8,1.0))],'#7b8777',1.5)
txt(83,644,'집 쪽 진입',23,bold=True)
txt(83,682,'디딤돌 4개 / 물뿌리개·호미',18)
line([(355,679),pr((-4.5,2.3,.03))],'#7b8777',1.5)
txt(1180,654,'빈 이랑은 1줄',22,bold=True)
txt(1180,691,'막 갈아엎은 흙만 남김',19)
line([(1170,674),pr((3.8,0,.2))],'#7b8777',1.5)
txt(610,773,'흙 스커트 0.30m  ·  지표면 중심 피벗',19)
txt(56,825,'색·덩어리만 설명하는 코드 도식입니다. 실제 재질·게임 조명·현장 배치는 아직 검증하지 않았습니다.',18,'#6e796a')
line([(55,870),(1625,870)])

txt(55,905,'평면 / 집 쪽을 비워 둔 다섯 이랑',24,bold=True)
ox,oy,sc=85,970,34
rect((ox,oy,ox+12*sc,oy+8.8*sc),'#aaa17b')
for i,y in enumerate(rows):
    yy=oy+(y+4.4)*sc
    rect((ox+2.85*sc,yy-.425*sc,ox+10.25*sc,yy+.425*sc),'#a58968')
    if i!=2:
        for j in range(12):
            xx=ox+(3.25+j*.58)*sc
            ellipse((xx-6,yy-6,xx+6,yy+6),'#41684f' if i==0 else '#61825a')
    txt(510,yy-11,['지지대 작물','잎채소','빈 흙','잎채소','잎채소'][i],16)
for i in range(4):
    xx=ox+(.45+i*.58)*sc; yy=oy+(7.2-i*.27)*sc
    ellipse((xx-7,yy-6,xx+7,yy+6),'#b7b7a6')
line([(ox+6*sc,oy+.5*sc),(ox+11*sc,oy+.5*sc),(ox+11*sc,oy+3.2*sc)],'#756c57',3)
txt(90,1280,'12.0m × 8.8m / 이랑 7.4 × 0.85m / 고랑 0.55m',16)
txt(90,1310,'집 ← 열린 접근로 2.7m     |     지지대 작물은 반대쪽',16)

txt(710,905,'형태와 표면 기준',24,bold=True)
notes=['낮은 잎채소: 0.30~0.40m, 잎 5~7장 덩어리',
       '지지대 작물: 1.0m 내외, 소량의 어두운 붉은 열매',
       '흙 두둑: 0.19m, 나무 테두리 없는 낮은 이랑',
       '흙·목재·돌은 CC0 사진 재료로 제작 예정',
       '사진 결 ±8~12%, 에셋 전용 팔레트 재양자화',
       '윤곽선·구운 빛·녹·광범위한 때 없음']
for i,t in enumerate(notes): txt(710,955+i*36,t,20)
colors=[('#aa8963','흙'),('#47745a','잎'),('#b8a281','목재'),('#b4b3a0','돌'),('#527a7d','물뿌리개')]
for i,(co,label) in enumerate(colors):
    x=710+i*165;rect((x,1193,x+133,1235),co);txt(x,1245,label,17)
txt(710,1305,'대안 B: 잎채소 1줄을 빼 4줄로 축소하고 여백 확대',20)
txt(55,1380,'도현 승인 필요  ·  제작/Unity 임포트/검사/뷰어 갱신/맵 배치 전',19,'#885d43',True)
im.resize((W,H),Image.Resampling.LANCZOS).save(OUT/'concept.png')

spec={'status':'concept_pending_director_approval','size_m':[12,8.8],
      'ground_skirt_m':.3,'pivot':'ground_surface_center','rows':5,
      'row_length_m':7.4,'row_width_m':.85,'furrow_m':.55,
      'crops':{'low_leaf_rows':3,'staked_pepper_rows':1,'empty_rows':1},
      'max_height_m':1.35,'stepping_stones':4,'props':['watering_can','hand_hoe'],
      'target_tri_budget':6000,'actual_triangles':None,
      'production_run':False,'unity_run':False,'map_placement':False}
(OUT/'concept-spec.json').write_text(json.dumps(spec,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(OUT/'concept.png')
