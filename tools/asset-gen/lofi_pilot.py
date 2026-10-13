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
 ('zebra','#666c70'),('grate','#293337'),('terracotta','#b5674a'),('cloth','#d5c9a8')]

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
img=bpy.data.images.new('LofiAtlas',width=N,height=N,alpha=True)
img.pixels.foreach_set(pixels);img.filepath_raw=str(out/'LofiAtlas.png');img.file_format='PNG';img.save()
mat=bpy.data.materials.new('LofiAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=1
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img;tex.interpolation='Closest'
mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])

verts=[];faces=[];uvs=[]
def face(points,tile,split=True):
    if split and len(points)==4 and tile in ('plaster','concrete','slate','roof','asphalt','grass','wood','metal','shutter'):
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
    y0,y1=(23,41) if tile=='mart' else ((4,49) if tile=='service' else (2,62))
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
def gable(w,d,z,rise):
    x=w/2;y=d/2
    face([(-x,-y,z),(x,-y,z),(x,0,z+rise),(-x,0,z+rise)],'roof')
    face([(-x,0,z+rise),(x,0,z+rise),(x,y,z),(-x,y,z)],'roof')
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
def shop():
    box(0,0,0,6,5.2,6.2);box(0,-.02,0,6.06,5.24,.48,'concrete')
    box(0,-2.64,2.7,6.04,.12,.9,'teal');box(0,-2.72,2.84,3.0,.07,.56,'mart')
    for x in (-1.9,0,1.9):window(x,-2.65,4.28,.85,1.28,lit=x==0)
    for x,angle in [(-3.02,-math.pi/2),(3.02,math.pi/2)]:
        for y in (-1.25,1.25):side_window(x,y,4.28,angle)
    for x in (-1.8,.02):
        box(x,-2.68,.65,1.6,.12,1.83,'wood');box(x,-2.76,.74,1.45,.035,1.65,'shelf')
        box(x,-2.8,.7,.055,.04,1.78,'wood')
    box(2,-2.69,.05,1.15,.15,2.5,'wood');box(2,-2.78,.55,.91,.035,1.79,'warmglass')
    box(2.35,-2.82,1.05,.04,.08,.25,'metal')
    box(0,-3,2.57,6.2,.96,.11,'teal');box(0,-3.46,2.38,6.2,.09,.22,'teal')
    for x in (-2.7,2.7):box(x,-3.22,2.18,.05,.05,.45,'metal')
    box(0,-2.9,0,6.1,.6,.08,'concrete');gable(6.45,5.65,6.2,.6)
    for x in (-1.7,1.7):window(x,2.62,3.9,.8,1.1)
def garage():
    # actual open mouth: no solid wall behind opening
    box(-2.95,0,0,.5,5.6,3.8,'slate');box(2.95,0,0,.5,5.6,3.8,'slate')
    box(0,2.65,0,5.5,.3,3.8,'slate');box(0,0,0,5.5,5.6,.08,'concrete')
    box(0,-2.65,2.8,5.5,.3,1.0,'metal');box(0,-2.84,2.12,5.5,.06,.75,'shutter')
    box(0,-2.84,3.02,2.3,.06,.58,'service');gable(6.7,6.0,3.8,.35)
    for x,angle in [(-3.22,-math.pi/2),(3.22,math.pi/2)]:side_window(x,.7,1.9,angle,1.3,.7)
    box(-1.4,1.9,.05,1.5,.65,.9,'paintred');box(-1.4,1.5,.17,1.3,.02,.08,'metal')
    box(-1.4,1.5,.39,1.3,.02,.08,'metal');box(-1.4,1.5,.62,1.3,.02,.08,'metal')
    for x in (.6,2):box(x,2,.08,.07,.6,2,'wood')
    for z in (.3,1,1.7):box(1.3,2,z,1.5,.6,.09,'wood')
    box(.9,2,1.08,.4,.4,.44,'carton');box(1.6,2,.39,.5,.5,.5,'carton')
    box(0,-2.3,2.91,.17,.17,.2,'warmglass')
def house():
    box(0,0,0,5,4.8,5.7);box(0,0,0,5.06,4.86,.45,'concrete')
    for x in (-1.4,1.4):
        for z in (.9,3.8):window(x,-2.43,z,.9,1.15)
    box(0,-2.44,.05,.92,.13,2.15,'teal');box(.28,-2.53,1,.05,.05,.22,'metal')
    for x,angle in [(-2.52,-math.pi/2),(2.52,math.pi/2)]:
        for z in (.9,3.8):side_window(x,0,z,angle)
    gable(5.45,5.25,5.7,.9)
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

manifest=[]
for name,fn in [('CornerShop',shop),('RepairGarage',garage),('CoastalHouse',house),('Wall3m',wall),('Crate',crate),('Parcel',carton),('DeliveryBag',bag),('UtilityPole',pole),('TireStack',tires),('CoastalTree',tree),('Road7x8',road),('Sidewalk2x4',sidewalk),
                ('Awning',awning),('Planter',planter),('Pots',pots),('Bench',bench),('Barrel',barrel),('AcUnit',acunit),('WallLamp',walllamp),('Clothesline',clothesline),
                ('SignTea',signpost),('SignIce',signpost2),('SignRest',signpost3),('Bollard',bollard),('CrateStack',cratestack),('Crosswalk',crosswalk),('Manhole',manhole),('Drain',drain)]:
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
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',use_mesh_modifiers=True,path_mode='STRIP')
    manifest.append({'name':name,'triangles':tris,'blender_bounds':bounds,'dimensions':[round(b[1]-b[0],3) for b in bounds],'materials':1,'uv_layer':True})
    # Native geometry interchange for independent post-import checks.
    # Blender x,y,z -> Unity x,z,-y (front -y -> +z)
    data={'vertices':[{'x':v.co.x,'y':v.co.z,'z':-v.co.y} for v in mesh.vertices], 'uv':[{'x':d.uv.x,'y':d.uv.y} for d in uv.data], 'triangles':[]}
    for poly in mesh.polygons:
        ids=list(poly.vertices)
        for k in range(1,len(ids)-1):data['triangles']+= [ids[0],ids[k],ids[k+1]]
    (out/(name+'.mesh.json')).write_text(json.dumps(data),encoding='utf-8')
    obj.location=((len(manifest)-1)%4*9,(len(manifest)-1)//4*9,0)
(out/'asset-manifest.json').write_text(json.dumps({'seed':413,'atlas':[512,512],'tile_size':64,'assets':manifest},indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(source/'LofiCoastalKit.blend'))
print('LOFI_ASSETS_OK '+json.dumps(manifest))
