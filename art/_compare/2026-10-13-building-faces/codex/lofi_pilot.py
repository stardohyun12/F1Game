"""Original low-poly / painted-pixel coastal kit. Blender Python, no external assets.
Run: blender -b -P tools/asset-gen/lofi_pilot.py -- --root D:/Github/F1Game
Produces shared atlas, FBX and editable .blend; deterministic seed, metre units.
"""
import bpy, math, random, json, sys, argparse
from pathlib import Path
from mathutils import Vector

ap = argparse.ArgumentParser()
ap.add_argument('--root', required=True)
a = ap.parse_args(sys.argv[sys.argv.index('--')+1:])
root = Path(a.root)
out = root/'F1/Assets/Art/LofiPilot'
source = root/'art/source/lofi-pilot'
out.mkdir(parents=True, exist_ok=True)
source.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
rng = random.Random(413)
N, T = 512, 64
pixels = [0.0]*(N*N*4)
tiles = {}
pal = [
 ('plaster','#b8ac91'),('concrete','#91958b'),('teal','#426963'),('roof','#505a59'),
 ('wood','#80684c'),('metal','#697775'),('dark','#293337'),('glass','#394f55'),
 ('warmglass','#b99454'),('orange','#c47c35'),('red','#b64935'),('cream','#d5c9a8'),
 ('asphalt','#666c70'),('grass','#777f51'),('leaves','#68734a'),('tire','#343b3b'),
 ('mart','#456d69'),('service','#b8ac91'),('shelf','#4d4c3d'),('crate','#5a7873'),
 ('shutter','#858a81'),('stripe','#99874e'),('carton','#a88858'),('water','#6e9caa'),
 ('moss','#636e4a'),('brick','#97755e'),('paintred','#b34f3f'),('slate','#76878b'),
 # --- 2026-10-13 Claude: 소품·도로 디테일용 추가 칸 (기존 칸 뒤에만 붙여 기존 UV·rng 순서 보존) ---
 ('awning','#d5c9a8'),('sign_tea','#426963'),('sign_ice','#b64935'),('sign_rest','#80684c'),
 ('zebra','#666c70'),('grate','#293337'),('terracotta','#b5674a'),('cloth','#d5c9a8'),
 # --- 2026-10-13 Claude 3차(바닥·원경): 연석·흙·모래·꽃·얼룩·부두석·짙은 잔디 ---
 ('curb','#8d9492'),('dirt','#8a7658'),('sand','#c2b391'),('flowers','#777f51'),
 ('stain','#5b6165'),('quaystone','#9a9a8e'),('grass2','#5c683a'),
 ('asphalt2','#61676c'),   # 3차-b: 아스팔트 덩어리 변화(균열·얼룩) — 게이트 1순위 '도로 민무늬'
 # Building faces: append only; existing tile indices and RNG calls stay intact.
 ('roof_clay','#956955'),('roof_green','#536b60'),('awning_rust','#c4ac85')]

def rgb(h): return tuple(int(h[i:i+2],16)/255 for i in (1,3,5))
def rect(tile,x,y,w,h,c):
    tx=(tile%8)*T; ty=(tile//8)*T
    for yy in range(max(0,y),min(T,y+h)):
        for xx in range(max(0,x),min(T,x+w)):
            k=((ty+yy)*N+tx+xx)*4
            pixels[k:k+4]=[*c,1]

font={
 'M':['10001','11011','10101','10001','10001','10001','10001'],
 'A':['01110','10001','10001','11111','10001','10001','10001'],
 'R':['11110','10001','10001','11110','10100','10010','10001'],
 'T':['11111','00100','00100','00100','00100','00100','00100'],
 'S':['01111','10000','10000','01110','00001','00001','11110'],
 'E':['11111','10000','10000','11110','10000','10000','11111'],
 'V':['10001','10001','10001','10001','10001','01010','00100'],
 'I':['111','010','010','010','010','010','111'],
 'C':['01111','10000','10000','10000','10000','10000','01111']}
def word(tile,text,y,scale=1,c=(.85,.8,.66)):
    width=sum(len(font[ch][0])+1 for ch in text)*scale
    x=(64-width)//2
    for ch in text:
        for row,line in enumerate(font[ch]):
            for col,v in enumerate(line):
                if v=='1':rect(tile,x+col*scale,y+(6-row)*scale,scale,scale,c)
        x+=(len(font[ch][0])+1)*scale

for i,(name,hexv) in enumerate(pal):
    tiles[name]=i; c=rgb(hexv); rect(i,0,0,T,T,c)
    # Broad sparse tonal islands, not independent per-pixel noise.
    for k in range(65 if name in ('plaster','concrete','wood','roof') else 20):
        delta=rng.choice([-.025,-.012,.012,.025])
        if name in ('asphalt','grass'):delta*=.35
        color=tuple(max(0,min(1,v+delta)) for v in c)
        x,y=rng.randrange(3,57),rng.randrange(3,57)
        w,h=rng.randrange(2,10),rng.randrange(2,7)
        rect(i,x,y,w,h,color);rect(i,x+1,y-1,max(1,w-3),h+2,color)
    if name=='concrete':
        for y in (16,32,48):rect(i,0,y,64,1,tuple(v*.86 for v in c))
        for x,y in [(16,1),(48,1),(32,17),(16,33),(48,33),(32,49)]:rect(i,x,y,1,15,tuple(v*.88 for v in c))
    if name in ('roof','wood','shutter'):
        for y in range(4,64,8 if name=='roof' else 5):rect(i,0,y,64,1,tuple(v*.82 for v in c))
    if name=='glass':
        rect(i,5,6,2,48,(.36,.44,.44));rect(i,9,8,9,46,(.27,.37,.39))
        rect(i,3,0,58,4,(.17,.23,.25))
    if name=='warmglass':
        rect(i,6,5,22,52,(.81,.65,.35));rect(i,32,5,25,52,(.87,.72,.41))
    if name=='mart':word(i,'MART',25,2)
    if name=='service':
        word(i,'SERVICE',8,1,(.27,.36,.36))
        rect(i,19,34,27,5,(.29,.37,.37));rect(i,13,31,10,12,(.29,.37,.37))
        rect(i,42,31,10,12,(.29,.37,.37));rect(i,12,35,6,4,c);rect(i,47,35,6,4,c)
    if name=='shelf':
        for yy in (8,27,46):
            rect(i,0,yy,64,2,(.64,.49,.3))
            for xx in range(4,58,9):
                cc=rng.choice([(.69,.55,.3),(.7,.4,.26),(.38,.56,.45),(.74,.7,.5)])
                hh=rng.randrange(8,15);rect(i,xx,yy+2,6,hh,cc);rect(i,xx+1,yy+5,4,3,(.8,.75,.6))
    if name=='crate':
        for yy in (12,28,44):
            for xx in range(6,58,10):rect(i,xx,yy,7,6,(.18,.29,.28))
        rect(i,0,59,64,4,(.44,.55,.48))
    if name=='stripe':
        for yy in range(64):
            for xx in range(64):
                if ((xx+yy)//12)%2==0:rect(i,xx,yy,1,1,(.19,.23,.23))
    if name=='carton':
        rect(i,29,0,7,64,(.7,.59,.41));rect(i,5,18,17,12,(.73,.68,.54))
    if name=='tire':
        for xx in range(3,64,8):rect(i,xx,0,1,64,(.16,.19,.19))
    # --- 추가 칸 무늬 ---
    if name=='awning':   # 크림 바탕 + 청록 세로 줄무늬 (차양)
        for xx in range(4,64,12):rect(i,xx,0,6,64,(.26,.41,.39))
    if name in ('sign_tea','sign_ice','sign_rest'):
        rect(i,2,2,60,60,tuple(v*.9 for v in c));rect(i,4,4,56,56,c)
        word(i,{'sign_tea':'TEA','sign_ice':'ICE','sign_rest':'REST'}[name],26,2,(.9,.87,.76))
        rect(i,6,50,52,2,(.9,.87,.76));rect(i,6,12,52,2,(.9,.87,.76))
    if name=='zebra':    # 횡단보도: 아스팔트 위 크림 띠 (살짝 닳음)
        for xx in range(2,64,16):
            rect(i,xx,0,9,64,(.8,.77,.64))
            for k in range(6):rect(i,xx+rng.randrange(0,8),rng.randrange(0,60),2,3,(.64,.66,.6))
    if name=='grate':    # 배수구 쇠살
        for yy in range(6,64,10):rect(i,4,yy,56,3,(.45,.49,.5))
    if name=='terracotta':
        for yy in (14,30,46):rect(i,0,yy,64,2,tuple(v*.85 for v in c))
    if name=='cloth':    # 빨래: 흰·빨강·청록 조각
        rect(i,4,10,16,44,(.91,.88,.8));rect(i,24,14,16,40,(.71,.29,.21));rect(i,44,8,16,46,(.3,.45,.43))
    # --- 3차 칸 무늬 ---
    if name=='curb':     # 연석: 16px 마다 이음매, 위쪽 밝은 모서리
        for xx in range(0,64,16):rect(i,xx,0,1,64,tuple(v*.78 for v in c))
        rect(i,0,60,64,4,tuple(min(1,v*1.08) for v in c))
    if name in ('dirt','sand'):   # 자갈 점 + 희미한 바큇자국
        for k in range(26):
            x,y=rng.randrange(1,62),rng.randrange(1,62);dk=rng.choice([-.07,-.04,.05])
            rect(i,x,y,2,2,tuple(max(0,min(1,v+dk)) for v in c))
        if name=='dirt':
            for yy in (18,22,44,48):rect(i,0,yy,64,1,tuple(v*.9 for v in c))
    if name=='flowers':  # 잔디 위 꽃 점: 빨강·크림·주황, 잎 짙은 점
        for k in range(22):
            x,y=rng.randrange(2,61),rng.randrange(2,61)
            rect(i,x,y,2,2,rng.choice([(.78,.33,.27),(.86,.8,.62),(.8,.5,.22),(.4,.45,.27)]))
    if name=='stain':    # 아스팔트 얼룩: 가운데 살짝 짙게 (3차-b: 너무 짙어 '그림자 오류'로 읽혀 톤 올림)
        rect(i,12,10,40,44,tuple(v*.95 for v in c));rect(i,22,18,22,26,tuple(v*.9 for v in c))
        for k in range(10):
            x,y=rng.randrange(4,58),rng.randrange(4,58);rect(i,x,y,5,3,tuple(v*.86 for v in c))
    if name=='quaystone':  # 큰 석재 블록 32×16, 어긋난 줄눈
        for yy in range(0,64,16):
            rect(i,0,yy,64,1,tuple(v*.76 for v in c))
            for xx in range(16 if (yy//16)%2 else 0,64,32):rect(i,xx,yy,1,16,tuple(v*.76 for v in c))
        rect(i,0,62,64,2,tuple(v*.7 for v in c))
    if name=='asphalt2': # 균열 폴리라인 2개 + 밝은 자갈 점 + 짙은 얼룩 1 (칸 1.5m 반복이라 선은 짧게)
        for (x0,y0,steps) in ((6,4,14),(40,30,11)):
            x,y=x0,y0
            for k in range(steps):
                rect(i,x,y,1,1,tuple(v*.72 for v in c));x+=rng.choice([1,1,2]);y+=rng.choice([0,1,1,2,-1])
        for k in range(12):
            x,y=rng.randrange(2,62),rng.randrange(2,62);rect(i,x,y,2,1,tuple(min(1,v*1.12) for v in c))
        rect(i,20,40,16,10,tuple(v*.9 for v in c))
    if name=='grass2':   # 짙은 잔디: 성긴 톤 섬 (기존 grass 처럼 약하게)
        for k in range(14):
            x,y=rng.randrange(3,57),rng.randrange(3,57);rect(i,x,y,rng.randrange(3,9),rng.randrange(2,6),tuple(v*.93 for v in c))
    if name in ('roof_clay','roof_green'):
        for yy in range(4,64,8):rect(i,0,yy,64,1,tuple(v*.82 for v in c))
    if name=='awning_rust':
        for xx in range(4,64,12):rect(i,xx,0,6,64,(.60,.32,.24))
img=bpy.data.images.new('LofiAtlas',width=N,height=N,alpha=True)
img.pixels.foreach_set(pixels);img.filepath_raw=str(out/'LofiAtlas.png');img.file_format='PNG';img.save()
mat=bpy.data.materials.new('LofiAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=1
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img;tex.interpolation='Closest'
mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])

verts=[];faces=[];uvs=[]
building_mode=False
def face(points,tile,split=True):
    if split and len(points)==4 and (tile in ('plaster','concrete','slate','roof','asphalt','grass','wood','metal','shutter','quaystone','curb','dirt','sand') or (building_mode and tile not in ('mart','service','sign_tea','glass','warmglass','shelf'))):
        p=[Vector(v) for v in points]
        nu=max(1,math.ceil((p[1]-p[0]).length/2.0));nv=max(1,math.ceil((p[3]-p[0]).length/2.0))
        if nu>1 or nv>1:
            sample=lambda u,v:tuple(p[0].lerp(p[1],u).lerp(p[3].lerp(p[2],u),v))
            for j in range(nv):
                for i in range(nu):face([sample(i/nu,j/nv),sample((i+1)/nu,j/nv),sample((i+1)/nu,(j+1)/nv),sample(i/nu,(j+1)/nv)],tile,False)
            return
    start=len(verts);verts.extend(points);faces.append(tuple(range(start,start+len(points))))
    i=tiles[tile];x=(i%8)*T;y=(i//8)*T
    # inset inside atlas tile: no neighboring material bleed
    y0,y1=(23,41) if tile=='mart' or (building_mode and tile=='sign_tea') else ((4,49) if tile=='service' else (2,62))
    co=[((x+2)/N,(y+y0)/N),((x+62)/N,(y+y0)/N),((x+62)/N,(y+y1)/N),((x+2)/N,(y+y1)/N)]
    uvs.append(co[:len(points)])
def box(x,y,z,w,d,h,tile='plaster',front=None):
    xx=[x-w/2,x+w/2];yy=[y-d/2,y+d/2];zz=[z,z+h]
    p=[(xx[0],yy[0],zz[0]),(xx[1],yy[0],zz[0]),(xx[1],yy[1],zz[0]),(xx[0],yy[1],zz[0]),
       (xx[0],yy[0],zz[1]),(xx[1],yy[0],zz[1]),(xx[1],yy[1],zz[1]),(xx[0],yy[1],zz[1])]
    for j,idx in enumerate([(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7),(3,2,1,0)]):face([p[k] for k in idx],front if j==0 and front else tile)
def cylinder(x,y,z,r,h,tile,n=8):
    bot=[(x+r*math.cos(i*math.tau/n),y+r*math.sin(i*math.tau/n),z) for i in range(n)]
    top=[(p[0],p[1],z+h) for p in bot]
    for i in range(n):j=(i+1)%n;face([bot[i],bot[j],top[j],top[i]],tile)
    for i in range(1,n-1):face([bot[0],bot[i+1],bot[i]],tile);face([top[0],top[i],top[i+1]],tile)
def gable(w,d,z,rise,tile='roof'):
    x=w/2;y=d/2
    face([(-x,-y,z),(x,-y,z),(x,0,z+rise),(-x,0,z+rise)],tile)
    face([(-x,0,z+rise),(x,0,z+rise),(x,y,z),(-x,y,z)],tile)
    face([(-x,y,z),(-x,-y,z),(-x,0,z+rise)],'plaster')
    face([(x,-y,z),(x,y,z),(x,0,z+rise)],'plaster')
def window(x,y,z,w=1,h=1.2,lit=False):
    box(x,y,z,w+.16,.13,h+.16,'wood')
    box(x,y-.075,z+.08,w,.035,h,'warmglass' if lit else 'glass')
    box(x,y-.1,z+.06,.055,.04,h,'wood')
    box(x,y-.11,z-.04,w+.26,.28,.1,'concrete')
def side_window(x,y,z,angle,w=.85,h=1.2):
    start=len(verts);window(0,0,0,w,h)
    c=math.cos(angle);s=math.sin(angle)
    for i in range(start,len(verts)):
        xx,yy,zz=verts[i];verts[i]=(x+xx*c-yy*s,y+xx*s+yy*c,z+zz)
def facade(w,d,side,fn):
    """Build at local front (-Y), rotate to front/right/back/left."""
    start=len(verts);fn()
    angle=side*math.pi/2;c=math.cos(angle);s=math.sin(angle)
    ox,oy=((0,-d/2),(w/2,0),(0,d/2),(-w/2,0))[side]
    for i in range(start,len(verts)):
        x,y,z=verts[i];verts[i]=(ox+x*c-y*s,oy+x*s+y*c,z)

def facade_panel(x,z,w,h,tile,depth=.001):
    face([(x-w/2,-depth,z),(x+w/2,-depth,z),(x+w/2,-depth,z+h),(x-w/2,-depth,z+h)],tile)

def facade_window(x,z,w=.9,h=1.08,shutters=False,lit=False):
    window(x,-.01,z,w,h,lit)
    box(x,-.12,z+h*.58,w,.05,.055,'wood')
    box(x,-.045,z+h+.17,w+.22,.12,.08,'cream')
    if shutters:
        for dx in (-1,1):
            box(x+dx*(w/2+.23),-.065,z+.08,.27,.075,h,'teal')
            for dz in (.2,.5,.8):box(x+dx*(w/2+.23),-.11,z+dz,.24,.025,.04,'wood')

def wall_courses(w,d,h,split,open_front=False):
    for side in range(4):
        width=w if side%2==0 else d
        def details():
            if not (open_front and side==0):
                box(0,-.025,0,width,.05,.5,'quaystone')
                box(0,-.05,.5,width,.1,.075,'concrete')
            box(0,-.04,split,width,.08,.18,'concrete')
            box(0,-.075,split+.18,width,.15,.075,'cream')
            box(0,-.018,h-.25,width,.036,.15,'dark')
            box(0,-.065,h-.1,width,.13,.1,'wood')
            for x in (-width/2+.13,width/2-.13):
                facade_panel(x,1.9,.26,.6,'concrete')
                # No raised quoins in the AC/lamp band 1.9--2.5 m.
                for za,zb in ((.57,1.9),(2.5,h-.25)):
                    n=math.ceil((zb-za)/.42)
                    for j in range(n):
                        step=(zb-za)/n
                        box(x,-.035,za+j*step,.26 if j%2 else .35,.07,step-.025,'concrete')
        facade(w,d,side,details)

def shop(variant=0):
    box(0,0,0,6,5.2,6.2);wall_courses(6,5.2,6.2,3.55)
    for side in range(4):
        def windows():
            for x in ((-1.9,0,1.9) if side%2==0 else (-1.35,1.35)):
                facade_window(x,4.35,.86,1.12,lit=(x==0))
                if side!=0:facade_window(x,.65,.95,.97)
        facade(6,5.2,side,windows)
    def front():
        box(0,-.035,2.82,5.65,.07,.66,'teal' if variant==0 else 'wood')
        box(0,-.081,2.88,2.9,.022,.52,'mart' if variant==0 else 'sign_tea')
        for x in (-1.8,.02):
            facade_panel(x,.62,1.6,1.98,'wood');facade_panel(x,.73,1.42,1.71,'shelf',.002)
            box(x,-.07,.6,1.68,.14,.1,'concrete')
            for dx in (-.78,.78):box(x+dx,-.045,.7,.08,.09,1.18,'wood')
        facade_panel(2,.05,1.15,2.5,'wood');facade_panel(2,.56,.91,1.79,'warmglass',.002)
        box(2.35,-.075,1.05,.04,.15,.25,'metal')
        # Shallow canopy; all raised geometry is above the mounted-prop band.
        canopy='awning' if variant==0 else 'awning_rust'
        face([(-2.85,-.015,2.8),(2.85,-.015,2.8),(2.85,-.33,2.66),(-2.85,-.33,2.66)],canopy)
        box(0,-.315,2.56,5.7,.03,.1,canopy)
    facade(6,5.2,0,front)
    gable(6.45,5.65,6.2,.6,'roof' if variant==0 else 'roof_clay')

def garage(variant=0):
    # Original single-storey workshop, lower windows + upper clerestory row.
    box(-2.95,0,0,.5,5.6,3.8,'slate');box(2.95,0,0,.5,5.6,3.8,'slate')
    box(0,2.65,0,5.5,.3,3.8,'slate');box(0,0,0,5.5,5.6,.08,'concrete')
    box(0,-2.65,2.8,5.5,.3,1,'metal')
    box(0,-2.765,.08 if variant else 2.55,5.4,.05,2.72 if variant else .25,'shutter')
    wall_courses(6.4,5.6,3.8,2.65,True)
    for side in (1,2,3):
        def windows():
            for x in ((-1.95,0,1.95) if side==2 else (-1.5,1.5)):
                facade_window(x,.7,1.04,.92)
                facade_window(x,3.03,1.04,.36)
        facade(6.4,5.6,side,windows)
    def front():
        for x in (-2.1,2.1):facade_window(x,3.04,.65,.35)
        if variant==0:box(0,-.065,3.04,2.25,.13,.52,'service')
        for x in (-2.95,2.95):box(x,-.035,0,.45,.07,.5,'quaystone')
    facade(6.4,5.6,0,front)
    if variant:facade(6.4,5.6,1,lambda:box(0,-.075,2.98,1.5,.15,.54,'service'))
    gable(6.7,6.0,3.8,.35)
    box(-1.4,1.9,.05,1.5,.65,.9,'paintred');box(-1.4,1.5,.17,1.3,.02,.08,'metal')
    box(-1.4,1.5,.39,1.3,.02,.08,'metal');box(-1.4,1.5,.62,1.3,.02,.08,'metal')
    for x in (.6,2):box(x,2,.08,.07,.6,2,'wood')
    for z in (.3,1,1.7):box(1.3,2,z,1.5,.6,.09,'wood')
    box(.9,2,1.08,.4,.4,.44,'carton');box(1.6,2,.39,.5,.5,.5,'carton')
    box(0,-2.3,2.91,.17,.17,.2,'warmglass')

def house(variant=0):
    box(0,0,0,5,4.8,5.7);wall_courses(5,4.8,5.7,2.8)
    for side in range(4):
        def windows():
            for x in ((-1.4,1.4) if side%2==0 else (-1.2,1.2)):
                facade_window(x,.65,.9,.97)
                facade_window(x,3.75,.9,1.15,shutters=(variant!=0))
        facade(5,4.8,side,windows)
    def front():
        facade_panel(0,.05,.92,2.15,'teal');box(.28,-.065,1,.05,.13,.22,'metal')
        box(0,-.065,2.58,1.22,.13,.1,'cream')
        if variant==1:
            # Juliet balcony: maximum 0.32 m projection, no walkable extension.
            box(0,-.16,3.48,3.95,.32,.12,'concrete')
            box(0,-.28,4.22,3.95,.06,.06,'metal')
            for x in (-1.92,-1.44,-.96,-.48,0,.48,.96,1.44,1.92):box(x,-.28,3.6,.045,.045,.62,'metal')
            for x in (-1.95,1.95):box(x,-.14,4.22,.05,.28,.06,'metal')
    facade(5,4.8,0,front)
    gable(5.45,5.25,5.7,.9,('roof','roof_clay','roof_green')[variant])
    if variant in (1,2):
        box(-1.55,.55,6,.46,.5,1.15,'brick');box(-1.55,.55,7.15,.58,.62,.12,'concrete')

def wall():
    box(0,0,0,3,.3,1.25,'concrete');box(0,0,1.25,3.1,.4,.12,'cream')
def crate():box(0,0,0,.6,.45,.42,'crate')
def carton():box(0,0,0,.48,.4,.43,'carton')
def bag():
    box(0,0,0,.65,.45,.55,'orange');box(0,0,.55,.68,.48,.08,'orange')
    for x in (-.22,.22):box(x,0,.003,.045,.46,.57,'dark');box(x,0,.632,.045,.46,.025,'dark')
    box(0,0,.66,.25,.07,.04,'dark')
def pole():
    cylinder(0,0,0,.13,6.5,'wood');box(0,0,5.7,1.5,.11,.13,'wood')
    box(0,-.125,.05,.16,.04,1.0,'stripe')
    for x in (-.58,.58):cylinder(x,0,5.83,.065,.16,'cream')
def tires():
    for z in (0,.23,.46):
        n=12;ro=.38;ri=.21
        for i in range(n):
            a=i*math.tau/n;b=(i+1)*math.tau/n
            p=lambda r,t,h:(r*math.cos(t),r*math.sin(t),h)
            face([p(ro,a,z),p(ro,b,z),p(ro,b,z+.21),p(ro,a,z+.21)],'tire')
            face([p(ro,a,z+.21),p(ro,b,z+.21),p(ri,b,z+.21),p(ri,a,z+.21)],'tire')
            face([p(ri,b,z),p(ri,a,z),p(ri,a,z+.21),p(ri,b,z+.21)],'dark')
            face([p(ro,b,z),p(ro,a,z),p(ri,a,z),p(ri,b,z)],'tire')
def tree():
    cylinder(0,0,0,.14,2.7,'wood')
    for ci,(x,y,z,r) in enumerate([(0,0,2.7,1.03),(-.6,.15,2.3,.75),(.65,0,2.5,.8),(0,.2,3.4,.75)]):
        n=7;rings=[]
        for level,(zz,rr) in enumerate([(-.8,.5),(-.3,1),(.4,.92),(.85,.4)]):
            rings.append([(x+r*rr*math.cos(i*math.tau/n+ci*.4),y+r*rr*math.sin(i*math.tau/n+ci*.4),z+r*zz) for i in range(n)])
        for level in range(3):
            for i in range(n):
                j=(i+1)%n;face([rings[level][i],rings[level][j],rings[level+1][j],rings[level+1][i]],'leaves' if (i+ci)%3 else 'moss')
        for i in range(1,n-1):face([rings[0][0],rings[0][i+1],rings[0][i]],'moss');face([rings[3][0],rings[3][i],rings[3][i+1]],'leaves')
def road():box(0,0,-.18,7,8,.18,'asphalt')
def sidewalk():box(0,0,0,2,4,.15,'concrete');box(-.94,0,0,.12,4,.2,'cream')

# ---------- 2026-10-13 Claude: 생활 소품 + 도로 디테일 (브리프 2026-10-13-town-density) ----------
# 전부 원점=바닥 중심(z=0), 앞=-y. 벽걸이(차양·실외기·벽등)는 설치기가 높이를 준다.
def awning():
    # 경사 차양 1.6×0.9 + 쇠막대 2 — 벽에 붙는 면이 +y
    pts=[(-.8,.45,0),(.8,.45,0),(.8,-.45,-.32),(-.8,-.45,-.32)]
    face(pts,'awning');face(list(reversed(pts)),'awning')
    face([(-.8,.45,0),(-.8,-.45,-.32),(-.8,-.45,-.42)],'cream');face([(.8,-.45,-.42),(.8,-.45,-.32),(.8,.45,0)],'cream')
    box(0,-.45,-.42,1.6,.03,.1,'cream')
    for x in (-.7,.7):box(x,.0,-.38,.03,.9,.03,'metal')
def planter():
    box(0,0,0,.95,.36,.38,'wood');box(0,0,.38,.9,.3,.06,'moss')
    for x in (-.3,0,.3):box(x,0,.42,.22,.22,.2,'leaves')
def pots():
    for x,r,h in ((-.32,.16,.3),(.05,.2,.36),(.38,.13,.26)):
        cylinder(x,0,0,r,h,'terracotta',6);cylinder(x,0,h,r*.9,.22,'leaves',6)
def bench():
    for z in (.0,):
        box(0,0,.4,1.4,.42,.05,'wood');box(0,.19,.45,1.4,.05,.4,'wood')
        for x in (-.6,.6):box(x,0,0,.05,.4,.4,'metal')
def barrel():
    cylinder(0,0,0,.28,.78,'metal',8);box(0,0,.26,.6,.6,.03,'dark');box(0,0,.52,.6,.6,.03,'dark')
def acunit():
    box(0,.15,0,.7,.3,.5,'metal');box(0,-.01,.05,.6,.02,.4,'grate');box(0,.05,-.08,.74,.5,.04,'metal')
def walllamp():
    box(0,.08,0,.08,.16,.08,'metal');box(0,-.06,-.1,.22,.2,.2,'warmglass');box(0,-.06,.1,.26,.24,.03,'metal')
def clothesline():
    # 벽(+y 쪽)에서 밖으로 2.4m, 끝에 막대. 빨래 3장.
    box(0,0,0,.06,.06,2.3,'wood');box(0,1.2,2.25,.02,2.4,.02,'dark')
    for y,w in ((.35,.5),(1.05,.6),(1.8,.45)):box(0,y,1.6,.035,w,.62,'cloth')
def signpost():
    box(0,0,0,.09,.09,2.2,'wood');box(0,-.06,1.35,.95,.04,.8,'sign_tea')
def signpost2():
    box(0,0,0,.09,.09,2.2,'wood');box(0,-.06,1.35,.95,.04,.8,'sign_ice')
def signpost3():
    box(0,0,0,.09,.09,2.2,'wood');box(0,-.06,1.35,.95,.04,.8,'sign_rest')
def bollard():cylinder(0,0,0,.12,.65,'concrete',6);box(0,0,.65,.26,.26,.05,'cream')
def cratestack():
    box(-.3,0,0,.6,.45,.42,'crate');box(.35,0,0,.6,.45,.42,'crate');box(0,.02,.42,.6,.45,.42,'crate')
def crosswalk():box(0,0,0,3.4,2.2,.012,'zebra')
def manhole():cylinder(0,0,0,.34,.012,'metal',10);cylinder(0,0,.012,.26,.006,'dark',10)
def drain():box(0,0,0,.6,.26,.012,'grate');box(0,0,0,.7,.34,.006,'dark')

# ---------- 2026-10-13 Claude 3차: 바닥(연석·얼룩·가로등·쓰레기통·가판·소화전·풀·덤불·바위·꽃) + 원경(부두·계류·잔교·배) ----------
# 원점=바닥 중심, 앞=-y. 긴 쪽은 x.
def curb():box(0,0,0,6,.16,.14,'curb')
def streetlamp():
    cylinder(0,0,0,.14,.22,'dark',6);cylinder(0,0,.22,.06,2.95,'metal',6)
    box(0,-.3,3.05,.06,.6,.06,'metal');box(0,-.58,2.86,.34,.3,.18,'warmglass');box(0,-.58,3.04,.42,.38,.05,'dark')
def bin_():
    cylinder(0,0,0,.26,.82,'teal',8);box(0,0,.82,.58,.58,.06,'dark');box(0,0,.88,.3,.3,.1,'dark')
def stall():
    box(0,0,.72,2,.95,.08,'wood')
    for x in (-.9,.9):
        for y in (-.4,.4):box(x,y,0,.07,.07,.72,'wood')
    for x in (-.95,.95):box(x,.42,0,.06,.06,2.1,'wood');box(x,-.5,0,.06,.06,1.85,'wood')
    pts=[(-1.05,.5,2.1),(1.05,.5,2.1),(1.05,-.6,1.85),(-1.05,-.6,1.85)]
    face(pts,'awning');face(list(reversed(pts)),'awning')
    box(-.55,.05,.8,.5,.4,.3,'carton');box(.2,-.1,.8,.45,.45,.36,'crate');box(.7,.15,.8,.3,.3,.25,'terracotta')
def hydrant():
    cylinder(0,0,0,.13,.68,'paintred',6);box(0,0,.42,.42,.16,.15,'paintred');cylinder(0,0,.68,.09,.12,'paintred',6)
def grasstuft():
    for x,y,h in ((-.22,-.1,.28),(.18,.12,.34),(.02,-.3,.24)):
        for k in range(4):
            a0=k*math.tau/4;a1=(k+1)*math.tau/4
            face([(x+.17*math.cos(a0),y+.17*math.sin(a0),0),(x+.17*math.cos(a1),y+.17*math.sin(a1),0),(x,y,h)],'leaves' if k%2 else 'moss')
def shrub():
    box(0,0,0,.95,.8,.62,'leaves');box(.28,.22,.42,.5,.48,.4,'moss');box(-.32,-.12,.48,.46,.44,.32,'leaves')
def rock():
    box(0,0,0,.72,.56,.3,'slate');box(.12,.06,.22,.44,.36,.2,'concrete')
def flowerpatch():box(0,0,0,1.3,.9,.012,'flowers')
def oilstain():cylinder(0,0,0,.62,.012,'stain',8)
def patch():box(0,0,0,1.3,.9,.012,'stain')
def quaysegment():
    box(0,0,0,12,1.4,.7,'quaystone');box(0,-.6,.7,12,.2,.06,'concrete')
def mooring():cylinder(0,0,0,.14,.45,'dark',8);box(0,0,.45,.36,.36,.08,'dark')
def pier():
    for yy in (-1,-4.5,-8,-11.5,-13.6):
        for x in (-1.1,1.1):cylinder(x,yy,0,.09,.84,'wood',6)
    box(0,-7,.84,2.6,14,.12,'wood')
    for x in (-1.2,1.2):
        for yy in (-3,-7,-11):box(x,yy,.96,.06,.06,.9,'wood')
        box(x,-7,1.8,.05,13,.05,'wood')
def boat():
    box(0,0,0,1.6,3.6,.62,'paintred');box(0,-2.05,.08,1.0,1.0,.5,'paintred');box(0,2.0,.08,1.3,.5,.5,'paintred')
    box(0,0,.62,1.7,3.7,.06,'wood');box(0,.55,.68,1.1,1.25,.9,'cream');box(0,.1,1.15,1.12,.4,.3,'glass')
    box(0,.55,1.58,1.2,1.35,.06,'teal');cylinder(0,-.9,.68,.05,1.6,'wood',6)

manifest=[]
for name,fn in [('CornerShop',shop),('RepairGarage',garage),('CoastalHouse',house),('Wall3m',wall),('Crate',crate),('Parcel',carton),('DeliveryBag',bag),('UtilityPole',pole),('TireStack',tires),('CoastalTree',tree),('Road7x8',road),('Sidewalk2x4',sidewalk),
                ('Awning',awning),('Planter',planter),('Pots',pots),('Bench',bench),('Barrel',barrel),('AcUnit',acunit),('WallLamp',walllamp),('Clothesline',clothesline),
                ('SignTea',signpost),('SignIce',signpost2),('SignRest',signpost3),('Bollard',bollard),('CrateStack',cratestack),('Crosswalk',crosswalk),('Manhole',manhole),('Drain',drain),
                ('Curb',curb),('StreetLamp',streetlamp),('Bin',bin_),('Stall',stall),('Hydrant',hydrant),('GrassTuft',grasstuft),('Shrub',shrub),('Rock',rock),('FlowerPatch',flowerpatch),('OilStain',oilstain),('Patch',patch),
                ('QuaySegment',quaysegment),('Mooring',mooring),('Pier',pier),('Boat',boat),
                ('CoastalHouseB',lambda:house(1)),('CoastalHouseC',lambda:house(2)),
                ('CornerShopB',lambda:shop(1)),('RepairGarageB',lambda:garage(1))]:
    building_mode=name.startswith(('CoastalHouse','CornerShop','RepairGarage'))
    verts.clear();faces.clear();uvs.clear();fn()
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    uv=mesh.uv_layers.new(name='UVMap')
    for poly,coords in zip(mesh.polygons,uvs):
        for li,co in zip(poly.loop_indices,coords):uv.data[li].uv=co
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);obj.data.materials.append(mat)
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.context.view_layer.update()
    tris=sum(len(p.vertices)-2 for p in mesh.polygons)
    bounds=[[min(v.co[i] for v in mesh.vertices),max(v.co[i] for v in mesh.vertices)] for i in range(3)]
    assert all(math.isfinite(v) for p in verts for v in p)
    assert all(p.area>0 for p in mesh.polygons),name
    manifest.append({'name':name,'triangles':tris,'blender_bounds':bounds,'dimensions':[round(b[1]-b[0],3) for b in bounds],'materials':1,'uv_layer':True})
    # Native geometry interchange for independent post-import checks.
    # Blender x,y,z -> Unity x,z,-y (front -y -> +z)
    data={'vertices':[{'x':v.co.x,'y':v.co.z,'z':-v.co.y} for v in mesh.vertices], 'uv':[{'x':d.uv.x,'y':d.uv.y} for d in uv.data], 'triangles':[]}
    for poly in mesh.polygons:
        ids=list(poly.vertices)
        for k in range(1,len(ids)-1):data['triangles']+= [ids[0],ids[k],ids[k+1]]
    geometry_path=out/(name+'.mesh.json');serialized=json.dumps(data)
    # Avoid timestamp-only FBX rewrites/reimports of the unchanged prop kit.
    if not geometry_path.exists() or geometry_path.read_text(encoding='utf-8')!=serialized or not (out/(name+'.fbx')).exists():
        bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',use_mesh_modifiers=True,path_mode='STRIP')
        geometry_path.write_text(serialized,encoding='utf-8')
    obj.location=((len(manifest)-1)%4*9,(len(manifest)-1)//4*9,0)
(out/'asset-manifest.json').write_text(json.dumps({'seed':413,'atlas':[512,512],'tile_size':64,'assets':manifest},indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(source/'LofiCoastalKit.blend'))
print('LOFI_ASSETS_OK '+json.dumps(manifest))
