"""Hand-authored house_home parts adapted for D-211 background houses.
Source: house_home.py inset/surround/shutter/door/roof/foundation proportions.
No noise, random facade grammar, generated albedo, or Unity access.
"""
import math, json
from mathutils import Vector
import bpy

def install(g):
    globals().update({k:g[k] for k in ('R','E','V','S','Mesh','box','cap','mats','cells','checks','exports','manifest')})
    mat=bpy.data.materials.new('Shared_retain_stone');mat.use_nodes=True
    tx=mat.node_tree.nodes.new('ShaderNodeTexImage');tx.image=bpy.data.images.load(str(E/'textures/retain_stone.png'));tx.interpolation='Closest'
    bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.95
    mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color']);mats['retain_stone']=mat
    cells['retain_stone']=('retain_stone',(0,0,80,60))

class HousePart:
    def __init__(self,mesh,h,b):self.mesh=mesh;self.h=h;self.b=b
    def point(self,p):
        x,y,z=p;h=self.h;px,pz=self.b['pivot_xz'];cx,cz=h['center'];u=h['u'];n=h['out']
        return (cx-px+u[0]*x-n[0]*y,-(cz-pz+u[1]*x-n[1]*y),z)
    def face(self,pts,cell):self.mesh.face([self.point(p) for p in pts],cell)
    def box(self,x0,x1,y0,y1,z0,z1,cell,skip=()):
        # house_home.box face ordering and explicit contact-face exclusion.
        fs={'front':[(x0,y0,z0),(x1,y0,z0),(x1,y0,z1),(x0,y0,z1)],
            'back':[(x1,y1,z0),(x0,y1,z0),(x0,y1,z1),(x1,y1,z1)],
            'left':[(x0,y1,z0),(x0,y0,z0),(x0,y0,z1),(x0,y1,z1)],
            'right':[(x1,y0,z0),(x1,y1,z0),(x1,y1,z1),(x1,y0,z1)],
            'top':[(x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)],
            'bottom':[(x0,y1,z0),(x1,y1,z0),(x1,y0,z0),(x0,y0,z0)]}
        for k,p in fs.items():
            if k not in skip:self.face(p,cell)

def make_house(b,h,root):
    pre=b['name']+'_'+h['key'];parts={s:Mesh(pre+'_'+s) for s in ['BODY','ROOF','TRIM','GLASS','DOOR_SLOT','LIFE','FOUNDATION']}
    P={k:HousePart(v,h,b) for k,v in parts.items()};body,roof,trim,glass,door,life,stone=[P[k] for k in parts]
    w=h['width'];d=h['depth'];X=w/2;F=-d/2;B=d/2;Z=h['foundation'];H=Z+{1:3.2,2:5.85,3:8.5}[h['floors']]
    wall=h['wall_cell'];tc=h['roof_cell'];windows=[]
    # Front opening compositions: irregular position, varied width, no floor-by-bay loop.
    if h['floors']==1:spec=[(-X*.49,1.52,.84,1.1,'open'),(X*.49,1.62,.78,1.0,'closed')]
    else:
        spec=[(X*.51,1.55,1.02,1.20,'half'),(-X*.50,4.55,.90,1.20,'open'),(X*.39,4.72,1.10,1.0,'closed')]
        if h['floors']==3:spec.append((-X*.12,7.15,.84,1.15,'open'))
    # Door remains at ground level; first-floor slots do not conflict with front windows.
    dx=-X*.38 if h['floors']>1 else 0;dw=.90 if h['treatment']!='shop' or h['floors']==1 else min(1.45,w*.29)
    holes=[(dx-dw/2,dx+dw/2,Z,Z+2.15,'door')]
    for cx,zz,ww,hh,state in spec:
        if h['treatment']=='balcony' and zz>3 and cx>0:zz=4.25;hh=2.02
        holes.append((cx-ww/2,cx+ww/2,Z+zz-hh/2,Z+zz+hh/2,state))
    # Front wall is cut around real openings, mirroring house_home's height-band method.
    zs=sorted({Z,H}|{q[2] for q in holes}|{q[3] for q in holes})
    for za,zb in zip(zs,zs[1:]):
        xx=-X
        for xa,xb,z0,z1,kind in sorted(holes):
            if z0<=za and zb<=z1:
                if xa>xx:body.face([(xx,F,za),(xa,F,za),(xa,F,zb),(xx,F,zb)],wall)
                xx=max(xx,xb)
        if xx<X:body.face([(xx,F,za),(X,F,za),(X,F,zb),(xx,F,zb)],wall)
    # Rear is a second hand-composed face, not an uninhabited blank apartment wall.
    backholes=[(-X*.32-.42,-X*.32+.42,Z+1.0,Z+2.12)]
    if h['floors']>1:backholes.append((X*.24-.48,X*.24+.48,Z+3.94,Z+5.10))
    if h['floors']>2:backholes.append((-X*.25-.40,-X*.25+.40,Z+6.70,Z+7.82))
    zs_back=sorted({Z,H}|{q[2] for q in backholes}|{q[3] for q in backholes})
    for za,zb in zip(zs_back,zs_back[1:]):
        xx=-X
        for xa,xb,z0,z1 in sorted(backholes):
            if z0<=za and zb<=z1:
                if xa>xx:body.face([(xa,B,za),(xx,B,za),(xx,B,zb),(xa,B,zb)],wall)
                xx=max(xx,xb)
        if xx<X:body.face([(X,B,za),(xx,B,za),(xx,B,zb),(X,B,zb)],wall)
    for xa,xb,z0,z1 in backholes:
        glass.face([(xb,B-.08,z0),(xa,B-.08,z0),(xa,B-.08,z1),(xb,B-.08,z1)],'glass')
        for a,c in [(xa-.10,xa),(xb,xb+.10)]:trim.box(a,c,B-.12,B+.025,z0,z1,'window_frame',('front','top','bottom'))
        for a,c in [(z0-.08,z0),(z1,z1+.10)]:trim.box(xa-.10,xb+.10,B-.12,B+.025,a,c,'window_frame',('front',))
        trim.box((xa+xb)/2-.018,(xa+xb)/2+.018,B-.075,B-.045,z0,z1,'window_frame',('top','bottom'))
        hw=(xb-xa)/2
        for a,c in [(xa-.13-hw,xa-.13),(xb+.13,xb+.13+hw)]:
            if a>=-X and c<=X:life.box(a,c,B+.04,B+.07,z0,z1,'shutter',('front',))
    # The oblique cart views mostly see gable sides. Reuse the same small window
    # assembly there, with one/two staggered openings rather than blank slabs.
    for side in (-1,1):
        def sidepart(key):
            ob=HousePart(parts[key],h,b)
            ob.point=lambda p,s=side:body.point((s*(X-p[1]),s*p[0],p[2]))
            return ob
        sb,st,sg,sl=[sidepart(k) for k in ('BODY','TRIM','GLASS','LIFE')]
        sh=[(-B*.2-.38,-B*.2+.38,Z+1.12,Z+2.18)]
        if h['floors']>1:
            if h['treatment']=='stairs' and side==1:
                landing=F-.1+min(d-1.1,4.8)+.475
                sh.append((landing-.45,landing+.45,Z+3.2,Z+5.35))
            else:sh.append((B*.18-.42,B*.18+.42,Z+4.00,Z+5.12))
        if h['floors']>2:sh.append((-B*.12-.36,-B*.12+.36,Z+6.88,Z+7.94))
        zs_side=sorted({Z,H}|{q[2] for q in sh}|{q[3] for q in sh})
        for za,zb in zip(zs_side,zs_side[1:]):
            xx=-B
            for xa,xb,z0,z1 in sorted(sh):
                if z0<=za and zb<=z1:
                    if xa>xx:sb.face([(xx,0,za),(xa,0,za),(xa,0,zb),(xx,0,zb)],wall)
                    xx=max(xx,xb)
            if xx<B:sb.face([(xx,0,za),(B,0,za),(B,0,zb),(xx,0,zb)],wall)
        for xa,xb,z0,z1 in sh:
            is_entry=h['treatment']=='stairs' and side==1 and abs(z0-Z-3.2)<.001
            if is_entry:sl.box(xa+.01,xb-.01,.06,.10,z0+.005,z1-.01,'wood_door',('back','bottom'))
            else:sg.face([(xa,.08,z0),(xb,.08,z0),(xb,.08,z1),(xa,.08,z1)],'glass')
            for a,c in [(xa-.10,xa),(xb,xb+.10)]:st.box(a,c,-.025,.12,z0,z1,'window_frame',('back','top','bottom'))
            for a,c in [(z0-.08,z0),(z1,z1+.10)]:
                if not (is_entry and c==z0):st.box(xa-.10,xb+.10,-.025,.12,a,c,'window_frame',('back',))
            if is_entry:continue
            mid=(xa+xb)/2;st.box(mid-.018,mid+.018,.045,.075,z0,z1,'window_frame',('top','bottom'))
            hw=(xb-xa)/2
            for a,c in [(xa-.13-hw,xa-.13),(xb+.13,xb+.13+hw)]:
                if a>=-B and c<=B:sl.box(a,c,-.07,-.04,z0,z1,'shutter',('back',))
    # Foundation 8cm lip, terrace 65cm; no hidden top face under wall/floor.
    stone.box(-X-.08,X+.08,F-.65,B+.08,0,Z,'retain_stone',('top','bottom'))
    for x0,x1,y0,y1 in [(-X-.08,X+.08,F-.65,F),(-X-.08,X+.08,B,B+.08),(-X-.08,-X,F,B),(X,X+.08,F,B)]:
        stone.face([(x0,y0,Z),(x1,y0,Z),(x1,y1,Z),(x0,y1,Z)],'retain_stone')
    body.face([(-X,F,Z),(X,F,Z),(X,B,Z),(-X,B,Z)],'floor_concrete')
    # Three distinct entry treads, skip coplanar contacts, door ends flush at floor.
    for k in range(3):
        y0=F-.65-(3-k)*.28;y1=y0+.28;zt=Z*(k+1)/3
        stone.box(dx-.68,dx+.68,y0,y1,0,zt,'retain_stone',('bottom','back'))
    for xa,xb,z0,z1,kind in holes:
        # Inset white reveals 12cm deep, frame 5cm, outside trim12cm (house_home).
        yf=F+.12
        if kind!='door':trim.face([(xa,F,z0),(xb,F,z0),(xb,yf,z0),(xa,yf,z0)],'window_frame')
        trim.face([(xb,F,z1),(xa,F,z1),(xa,yf,z1),(xb,yf,z1)],'window_frame')
        trim.face([(xa,F,z1),(xa,F,z0),(xa,yf,z0),(xa,yf,z1)],'window_frame')
        trim.face([(xb,F,z0),(xb,F,z1),(xb,yf,z1),(xb,yf,z0)],'window_frame')
        for a,c in [(xa-.12,xa),(xb,xb+.12)]:trim.box(a,c,F-.025,F,z0,z1,'window_frame',('back','bottom','top'))
        trim.box(xa-.12,xb+.12,F-.025,F,z1,z1+.12,'window_frame',('back',))
        if kind=='door':
            door.box(xa+.01,xb-.01,F+.065,F+.10,z0+.005,z1-.01,'wood_door',('back','bottom'))
            life.box(xb-.16,xb-.11,F+.025,F+.065,z0+1,z0+1.06,'gutter',('back',))
            if h['treatment']=='shop':
                # Broad shutter slats, 23cm apart: readable shop rather than fine pixel stripes.
                for k in range(8):life.box(xa+.025,xb-.025,F+.035,F+.055,z0+.17+k*.23,z0+.205+k*.23,'shutter',('back',))
            continue
        trim.box(xa-.18,xb+.18,F-.12,F,z0-.08,z0,'window_frame',('back',))
        # Glass at .08 inset, frame on separate depth; no wall under glass.
        glass.face([(xa+.05,F+.08,z0+.05),(xb-.05,F+.08,z0+.05),(xb-.05,F+.08,z1-.05),(xa+.05,F+.08,z1-.05)],'glass')
        for a,c in [(xa,xa+.05),(xb-.05,xb)]:trim.box(a,c,F+.055,F+.095,z0,z1,'window_frame',('left','right','bottom','top'))
        for a,c in [(z0,z0+.05),(z1-.05,z1)]:trim.box(xa+.05,xb-.05,F+.055,F+.095,a,c,'window_frame',('left','right','bottom','top'))
        mid=(xa+xb)/2
        trim.box(mid-.018,mid+.018,F+.045,F+.075,z0+.05,z1-.05,'window_frame',('top','bottom'))
        hw=(xb-xa)/2
        states=['open','open'] if kind=='open' else ['closed','closed'] if kind=='closed' else ['open','closed']
        for side,state in zip((-1,1),states):
            if state=='open':
                a,c=(xa-.14-hw,xa-.14) if side<0 else (xb+.14,xb+.14+hw)
                if a<-X or c>X:state='closed'
                for other in holes:
                    if other==(xa,xb,z0,z1,kind):continue
                    if z0<other[3]+.12 and z1>other[2]-.08 and a<other[1]+.14 and c>other[0]-.14:state='closed'
            if state=='closed':a,c=(xa+.012,mid-.012) if side<0 else (mid+.012,xb-.012)
            life.box(a,c,F-.075,F-.04,z0+.01,z1-.01,'shutter',('back',))
        windows.append(dict(local_rect=[xa,xb,z0,z1],shutters=kind))
    # Reuse house_home 24-degree gable construction; D-211 increases overhang to45cm.
    ov=h['eaves'];xe=X+ov;ye=B+ov;pitch=math.tan(math.radians(24));mode=h['roof_mode']
    if mode!='flat_parapet':
        cross=mode=='cross_gable';aa=ye if cross else xe;bb=xe if cross else ye
        def rp(x,y,z):return (-y,x,z) if cross else (x,y,z)
        ridge=H+pitch*(bb-ov);ze=H-pitch*ov
        if mode=='hip':
            r=max(0,aa-bb);tops=[[( -aa,-bb,ze),(aa,-bb,ze),(r,0,ridge),(-r,0,ridge)],[(aa,bb,ze),(-aa,bb,ze),(-r,0,ridge),(r,0,ridge)],
                [(-aa,bb,ze),(-aa,-bb,ze),(-r,0,ridge)],[(aa,-bb,ze),(aa,bb,ze),(r,0,ridge)]]
        else:
            tops=[[(-aa,-bb,ze),(aa,-bb,ze),(aa,0,ridge),(-aa,0,ridge)],[(aa,bb,ze),(-aa,bb,ze),(-aa,0,ridge),(aa,0,ridge)]]
            # Close gables at wall faces, roof remains 45cm past them.
            aw=B if cross else X;bw=X if cross else B;rw=ridge
            for s in (-1,1):
                q=[rp(s*aw,-bw,H),rp(s*aw,bw,H),rp(s*aw,0,rw)]
                if s<0:q.reverse()
                body.face(q,wall)
        for q in tops:
            roof.face([rp(x,y,z+.09) for x,y,z in q],tc)
            roof.face([rp(x,y,z) for x,y,z in q[::-1]],'eaves')
        # Exposed outer edges only. Shared roof ridge edges are internal, not double faces.
        edge_counts={}
        for q in tops:
            for a,c in zip(q,q[1:]+q[:1]):
                key=tuple(sorted((tuple(a),tuple(c))));edge_counts[key]=edge_counts.get(key,0)+1
        for (a,c),count in edge_counts.items():
            if count==1:roof.face([rp(*a),rp(*c),rp(c[0],c[1],c[2]+.09),rp(a[0],a[1],a[2]+.09)],'eaves')
        # Eaves band only under roof, no apartment-style floor stripes.
        if not cross:
            trim.box(-xe,xe,-ye-.025,-ye+.045,ze-.14,ze-.005,'eaves',('top','back'))
            trim.box(-xe,xe,ye-.045,ye+.025,ze-.14,ze-.005,'eaves',('top','front'))
        else:
            trim.box(-xe-.025,-xe+.045,-ye,ye,ze-.14,ze-.005,'eaves',('top','right'))
            trim.box(xe-.045,xe+.025,-ye,ye,ze-.14,ze-.005,'eaves',('top','left'))
    else:
        roof.box(-X,X,F,B,H,H+.12,'roof_F3',('bottom',))
        # Thin rooftop railing and a tank, only this one house has a flat roof.
        for xx in (-X+.12,X-.12):
            life.box(xx-.025,xx+.025,F+.1,B-.1,H+.95,H+1,'gutter')
            for yy in (F+.12,0,B-.12):life.box(xx-.025,xx+.025,yy-.025,yy+.025,H+.12,H+.95,'gutter',('top','bottom'))
        life.box(-.55,.55,B-1.4,B-.3,H+.12,H+1.02,'wall_cream',('bottom',))
        life.box(-.6,.6,B-1.45,B-.25,H+1.02,H+1.10,'roof_F3',('bottom',))
    # Household objects are authored per composition, not scatter.
    if h['treatment']=='balcony':
        bc=X*.39;bw=min(2.4,w*.52);za=Z+3.2;front=F-1.0
        life.box(bc-bw/2,bc+bw/2,front,F,za-.15,za,'window_frame',('back',))
        life.box(bc-bw/2,bc+bw/2,front-.025,front+.025,za+.88,za+.94,'shutter')
        for i in range(7):
            xx=bc-bw/2+i*bw/6;life.box(xx-.022,xx+.022,front-.022,front+.022,za,za+.88,'shutter',('top','bottom'))
        for xx in [bc-bw/2,bc+bw/2]:life.box(xx-.025,xx+.025,front+.025,F,za+.88,za+.94,'shutter',('front','back'))
    if h['treatment'] in ('awning','shop'):
        ax=dx;aw=min(2.7,w*.64);za=Z+2.47
        life.face([(ax-aw/2,F-.85,za),(ax+aw/2,F-.85,za),(ax+aw/2,F-.03,za+.3),(ax-aw/2,F-.03,za+.3)],'roof_F3' if h['treatment']=='awning' else 'shutter')
        life.box(ax-aw/2,ax+aw/2,F-.88,F-.83,za-.12,za,'eaves')
    # Air conditioner, bracket and service position on back wall.
    life.box(.25,1.10,B+.04,B+.37,Z+.35,Z+.90,'wall_cream',('front',))
    life.box(.34,1.0,B+.375,B+.39,Z+.46,Z+.77,'gutter',('front',))
    for xx in (.30,1.05):life.box(xx-.035,xx+.035,B,B+.40,Z+.28,Z+.35,'gutter',('front','top'))
    # Small planter on the stone terrace, leaf clusters as three broad forms.
    pc=X-.40;py=F-.43
    life.box(pc-.24,pc+.24,py-.12,py+.12,Z,Z+.25,'roof_E2',('bottom',))
    for dx_,hh in [(-.13,.18),(.02,.27),(.14,.15)]:life.box(pc+dx_-.05,pc+dx_+.05,py-.07,py+.07,Z+.25,Z+.25+hh,'shutter',('bottom',))
    if b['number'] in (2,3,6,7) and h['key']=='H1':
        # Back-yard laundry: two supports, sag in line, broad cloth polygons.
        yy=B+.65;zz=Z+2.65
        for xx in (-X+.3,X-.3):life.box(xx-.025,xx+.025,yy-.025,yy+.025,0,zz+.08,'gutter',('bottom',))
        life.box(-X+.3,X-.3,yy-.016,yy+.016,zz-.025,zz+.005,'gutter')
        for k,(xx,hh,cell) in enumerate([(-.7,.65,'wall_cream'),(.1,.80,'wall_apricot_B'),(.75,.5,'roof_F3')]):
            life.box(xx-.23,xx+.23,yy-.02,yy+.02,zz-hh,zz-.03,cell,('top',))
    if h['treatment']=='stairs':
        # One solid 16-riser side stair, extruded step profile avoids overlapping boxes.
        x0=X+.10;x1=X+.95;start=F-.1;run=min(d-1.1,4.8);step=run/16;rise=3.2/16
        profile=[(start,0),(start,Z)]
        for i in range(16):profile.extend([(start+i*step,Z+(i+1)*rise),(start+(i+1)*step,Z+(i+1)*rise)])
        profile.append((start+run,0))
        # Side strips instead of one concave ngon: FBX's ngon tessellator otherwise
        # creates collinear diagonals through aligned step corners.
        for i in range(16):
            ya=start+i*step;yb=ya+step;zt=Z+(i+1)*rise
            for xx,rev in [(x0,True),(x1,False)]:
                q=[(xx,ya,0),(xx,yb,0),(xx,yb,zt),(xx,ya,zt)];stone.face(q[::-1] if rev else q,'retain_stone')
        for (ya,za),(yb,zb) in zip(profile,profile[1:]):stone.face([(x0,ya,za),(x1,ya,za),(x1,yb,zb),(x0,yb,zb)],'retain_stone')
        life.box(X,x1,start+run,start+run+.95,Z+3.04,Z+3.2,'floor_stone',('front','left'))
    objs=[m.obj(root) for m in parts.values() if m.f]
    tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objs)
    assert tris<=h['budget_tris'],(pre,tris)
    return objs,dict(key=h['key'],tris=tris,floors=h['floors'],width=w,depth=d,eaves=ov,roof=mode,foundation=Z,windows=windows,treatment=h['treatment'],wall=wall)

def make_building(b):
    root=bpy.data.objects.new(b['name'],None);bpy.context.collection.objects.link(root)
    objs=[];houses=[]
    for h in b['houses']:
        oo,hh=make_house(b,h,root);objs+=oo;houses.append(hh)
    tris=sum(h['tris'] for h in houses);assert tris<=b['budget_tris']
    checks.append(dict(name=b['name'],tris=tris,budget=b['budget_tris'],houses=houses,bottom=0,ground_check='Source reference terrain only; live D-209 map NOT RUN'))
    bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
    bpy.ops.export_scene.fbx(filepath=str(E/(b['name']+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',path_mode='RELATIVE',bake_anim=False,add_leaf_bones=False)
    bpy.ops.export_scene.gltf(filepath=str(E/(b['name']+'.glb')),export_format='GLB',use_selection=True,export_yup=True)
    px,pz=b['pivot_xz'];root.location=(px-185,-(pz+61),b['y']-25.27);exports.append(root)
