"""Metre-scale BayLoop architecture. Sequential type build, real bpy previews.
Blender -Y street -> exported +Z; origin is front wall centre at ground.
Reuses the planar metric UV clipping method from hill_house_v01/generate.py.
"""
from pathlib import Path
import bpy,sys,math,json,csv,hashlib,argparse
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art/models/bayloop_buildings_v01'
REVIEW=ROOT/'art/_review/2026-10-25-bayloop-buildings'
TEX=OUT/'BayLoopPhotoAtlas.png';N=1024;D=32;EDGE=252/D
TM=json.loads((OUT/'texture-manifest.json').read_text(encoding='utf-8'))
TILES={t['id']:t['index'] for t in TM['tiles']}
V=[];F=[];UV=[];PARTS=[];PART='shell'
def rel(p):return p.relative_to(ROOT).as_posix()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def clip(poly,axis,val,greater):
 result=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  ai=a[axis]>=val-1e-8 if greater else a[axis]<=val+1e-8;bi=b[axis]>=val-1e-8 if greater else b[axis]<=val+1e-8
  if ai:result.append(a)
  if ai!=bi:
   t=(val-a[axis])/(b[axis]-a[axis]);result.append(tuple(a[k]+t*(b[k]-a[k]) for k in [0,1]))
 return result
def face(points,tile='plaster',offset=(0,0)):
 ps=[Vector(p) for p in points];origin=ps[0];u=(ps[1]-origin).normalized();normal=u.cross(ps[2]-origin).normalized();v=normal.cross(u).normalized()
 q=[((p-origin).dot(u),(p-origin).dot(v)) for p in ps];mn=[min(p[k] for p in q) for k in [0,1]];mx=[max(p[k] for p in q) for k in [0,1]]
 for j in range(max(1,math.ceil((mx[1]-mn[1])/EDGE-1e-8))):
  for i in range(max(1,math.ceil((mx[0]-mn[0])/EDGE-1e-8))):
   a=mn[0]+i*EDGE;c=mn[1]+j*EDGE;b=min(a+EDGE,mx[0]);d=min(c+EDGE,mx[1]);cut=q
   for axis,val,g in [(0,a,True),(0,b,False),(1,c,True),(1,d,False)]:cut=clip(cut,axis,val,g)
   clean=[]
   for p in cut:
    if not clean or sum(abs(p[k]-clean[-1][k]) for k in [0,1])>1e-7:clean.append(p)
   if len(clean)>2 and sum(abs(clean[0][k]-clean[-1][k]) for k in [0,1])<1e-7:clean.pop()
   if len(clean)<3 or abs(sum(p[0]*r[1]-r[0]*p[1] for p,r in zip(clean,clean[1:]+clean[:1])))<1e-9:continue
   idx=TILES[tile];ox=min(offset[0],252-(b-a)*32);oy=min(offset[1],252-(d-c)*32)
   start=len(V);V.extend(tuple(origin+u*p[0]+v*p[1]) for p in clean);F.append(tuple(range(start,start+len(clean))));PARTS.append(PART)
   # PNG origin at top, glTF/Blender UV origin at bottom.
   UV.append([(((idx%4)*256+2+ox+(p[0]-a)*D)/N,1-((idx//4)*256+2+oy+(d-p[1])*D)/N) for p in clean])
def box(x,y,z,w,d,h,tile='plaster',front=None):
 p=[(x-w/2,y-d/2,z),(x+w/2,y-d/2,z),(x+w/2,y+d/2,z),(x-w/2,y+d/2,z),(x-w/2,y-d/2,z+h),(x+w/2,y-d/2,z+h),(x+w/2,y+d/2,z+h),(x-w/2,y+d/2,z+h)]
 for k,ids in enumerate([(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7),(3,2,1,0)]):face([p[i] for i in ids],front if k==0 and front else tile)
def beam(a,b,width,tile='wood_dark',offset=(0,0)):
 a=Vector(a);b=Vector(b);z=(b-a).normalized();ref=Vector((0,0,1)) if abs(z.z)<.9 else Vector((1,0,0));x=z.cross(ref).normalized()*width/2;y=z.cross(x).normalized()*width/2
 p=[a-x-y,a+x-y,a+x+y,a-x+y,b-x-y,b+x-y,b+x+y,b-x+y]
 for ids in [(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7),(3,2,1,0)]:face([p[i] for i in ids],tile,offset)
def front_window(x,y,z,w,h,lattice=False,shutters=False):
 box(x,y+.035,z,w,.08,h,'glass')
 for xx in [x-w/2,x+w/2]:box(xx,y-.035,z,.075,.14,h,'wood_dark')
 for zz in [z,z+h]:box(x,y-.05,zz,w+.12,.17,.075,'wood_dark')
 if lattice:
  for k in range(1,max(2,round(w/.36))):box(x-w/2+k*w/round(w/.36),y-.1,z,.028,.055,h,'wood_dark')
  box(x,y-.12,z+h*.45,w,.06,.045,'wood_dark')
 else:box(x,y-.06,z,.045,.08,h,'trim');box(x,y-.07,z+h*.52,w,.08,.045,'trim')
 box(x,y-.12,max(0,z-.09),w+.25,.34,.1,'stone')
 if shutters:
  for xx in [x-w*.75,x+w*.75]:box(xx,y-.11,z,w*.42,.09,h,'teal')
def wall_front(w,h,openings,tile='plaster',y=0,z=0):
 # Actual holes with 18cm jamb depth, not black stickers on a solid wall.
 xs=sorted(set([-w/2,w/2]+[q for a,b,c,d in openings for q in [a,b]]));zs=sorted(set([z,z+h]+[q for a,b,c,d in openings for q in [c,d]]))
 for a,b in zip(xs,xs[1:]):
  for c,d in zip(zs,zs[1:]):
   if any(a>=l-1e-6 and b<=r+1e-6 and c>=lo-1e-6 and d<=hi+1e-6 for l,r,lo,hi in openings):continue
   if b-a>1e-6 and d-c>1e-6:box((a+b)/2,y+.09,c,b-a,.18,d-c,tile)
def shell(w,d,h,tile='plaster',openings=(),y=0,z=0):
 wall_front(w,h,openings,tile,y,z)
 # Butt joints, no overlapping coplanar front strips (Cycles shadow acne).
 box(-w/2+.09,y+d/2,z,.18,d-.36,h,tile);box(w/2-.09,y+d/2,z,.18,d-.36,h,tile);box(0,y+d-.09,z,w,.18,h,tile)
 box(0,y+d/2,z,w-.36,d-.36,.06,'stone');box(0,y+d/2,z+h-.08,w-.36,d-.36,.08,tile)
def roof(w,d,z,rise,tile='roof',along_x=True,y=0):
 e=.35;a=-w/2-e;b=w/2+e;f=y-.5;r=y+d+.3;mid=y+d/2
 if along_x:
  face([(a,f,z),(b,f,z),(b,mid,z+rise),(a,mid,z+rise)],tile);face([(a,mid,z+rise),(b,mid,z+rise),(b,r,z),(a,r,z)],tile)
  face([(a,f,z),(a,mid,z+rise),(a,r,z)],'wood');face([(b,r,z),(b,mid,z+rise),(b,f,z)],'wood')
  beam((a,mid,z+rise+.03),(b,mid,z+rise+.03),.16,tile)
  for yy in [f,r]:box(0,yy,z-.13,w+2*e,.14,.14,'wood_dark')
  for xx in [a,b]:beam((xx,f,z-.05),(xx,mid,z+rise-.05),.12,'wood_dark');beam((xx,mid,z+rise-.05),(xx,r,z-.05),.12,'wood_dark')
 else:
  face([(a,f,z),(0,f,z+rise),(0,r,z+rise),(a,r,z)],tile);face([(0,f,z+rise),(b,f,z),(b,r,z),(0,r,z+rise)],tile)
  face([(a,f,z),(b,f,z),(0,f,z+rise)],'plaster');face([(b,r,z),(a,r,z),(0,r,z+rise)],'plaster')
  for yy in [f,r]:beam((a,yy,z),(0,yy,z+rise),.17,'metal');beam((0,yy,z+rise),(b,yy,z),.17,'metal')
  beam((0,f,z+rise),(0,r,z+rise),.18,'metal')
def canopy(w,y,z,depth=1.1,tile='roof'):
 face([(-w/2,y-depth,z-.32),(w/2,y-depth,z-.32),(w/2,y+.1,z),(-w/2,y+.1,z)],tile)
 box(0,y-depth,z-.42,w,.13,.11,'wood_dark')
 for x in [-w/2+.2,w/2-.2]:beam((x,y,z-.8),(x,y-depth+.12,z-.36),.08)
def bench(x,y,z=0,w=1.3):
 box(x,y,z+.43,w,.38,.07,'wood');box(x,y+.15,z+.5,w,.055,.35,'wood')
 for xx in [x-w*.35,x+w*.35]:box(xx,y,z,.07,.32,.43,'wood_dark')
def pot(x,y,z=0):
 # worn planter with soil; no candy-coloured spheres.
 box(x,y,z,.38,.38,.38,'rust');box(x,y,z+.38,.32,.32,.02,'stonewall')
 for a,b in [(-.07,.1),(.08,-.04)]:beam((x+a,y+b,z+.35),(x+a+.09,y+b,z+.8),.035,'teal')
def sign(x,y,z,w=1.8):box(x,y,z,w,.1,.5,'wood_dark',front='sign')
def shop(v):
 global PART
 w=[5.5,6.75][v];d=8.;h=[5.65,6.1][v];PART='walls'
 win=[(-w*.25,.16,3.65,1.55,1.25),(w*.25,.16,3.65,1.55,1.25)]
 doorx=w*.29 if v==0 else -w*.31;storex=-.85 if v==0 else .9;storew=2.6 if v==0 else 3.5
 openings=[(x-ww/2,x+ww/2,z,z+hh) for x,y,z,ww,hh in win]+[(doorx-.47,doorx+.47,0,2.15),(storex-storew/2,storex+storew/2,.65,2.2)]
 shell(w,d,h,'plaster',openings)
 PART='wood_panelling';box(0,-.045,0,w,.09,.58,'wood');box(0,-.035,2.38,w,.07,.55,'wood')
 for x in [-w/2+.1,-w*.13,w/2-.1]:box(x,-.09,0,.14,.18,h,'wood_dark')
 for z in [3.1,h-.12]:box(0,-.07,z,w,.16,.12,'wood_dark')
 PART='windows'
 for x,y,z,ww,hh in win:front_window(x,y,z,ww,hh,True)
 front_window(storex,.16,.65,storew,1.55,False);front_window(doorx,.16,.03,.94,2.12,False)
 # The shop window contains shelves and tea tins rather than a prison grille.
 for zz in [.86,1.35]:
  box(storex,.04,zz,storew-.15,.12,.055,'wood')
  for i in range(5 if v==0 else 6):
   xx=storex-storew*.4+i*(storew*.8)/(4 if v==0 else 5)
   box(xx,-.015,zz+.055,.18,.14,.24 if i%2 else .31,'teal' if i%3 else 'cloth')
 PART='roofs';roof(w,d,h,1.35 if not v else 1.5,'roof');canopy(w+.25,0,3.05,1.05 if not v else 1.35)
 PART='life';sign(storex,-1.15 if not v else -1.45,2.15,2)
 if not v:bench(-w*.29,-.48,w=1.15)
 else:
  n=len(V);bench(0,0,w=1.15)
  for i in range(n,len(V)):x,y,z=V[i];V[i]=(w/2+.38-y,1.5+x,z)
  box(doorx,.055,.03,.86,.07,1.02,'wood');box(doorx+.31,-.015,.97,.045,.08,.22,'metal')
  box(doorx,-.22,0,1.1,.45,.08,'stone')
 pot(w*.43,-.35)
 for xx in [storex-.7,storex+.7]:box(xx,-1.12 if not v else -1.42,2.6,.045,.045,.14,'wood_dark')
 # Rainwater is directed to the ground beside the entrance, not arbitrary pipes.
 beam((w/2+.08,-.16,.08),(w/2+.08,-.16,h-.1),.075,'metal')
 if v:
  n=len(V);canopy(1.5,0,2.5,.9,'cloth')
  for i in range(n,len(V)):x,y,z=V[i];V[i]=(x+doorx,y,z)
 # Side and rear service windows break the deep blank shop volume.
 side_details(w,d,h)
 n=len(V);front_window(0,-.13,.03,.95,2.1,False)
 for i in range(n,len(V)):x,y,z=V[i];V[i]=(x+1.1,d-y,z)
 for x in [-1.5,-.9]:box(x,d+.26,0,.5,.45,.55,'wood')
 beam((-1.5,d+.25,2.5),(.5,d+.25,2.5),.035,'wood_dark')
 for x in [-1.1,-.3]:box(x,d+.25,1.65,.58,.025,.8,'cloth')
 return {'footprint_m':[w,d],'storeys':2,'wear_locations':['lower wood panelling','thresholds','rainwater corner','old sign support'],'front_wall_z':0}
def side_details(w,d,h):
 start=len(V)
 for yy in [2.5,d-1.7]:
  # +X side face equivalent window, transform a normal front window locally.
  n=len(V);front_window(0,-.13,1.2,1,1.2,False)
  for i in range(n,len(V)):
   x,y,z=V[i];V[i]=(w/2-y,yy+x,z)
 # Simple rear doorway/window at back wall.
 n=len(V);front_window(0,-.13,3.5 if h>5 else 1.5,1,1.15,False)
 for i in range(n,len(V)):
  x,y,z=V[i];V[i]=(-x,d-y,z)
def railing(w,y,z,tile='metal'):
 for x in [-w/2,w/2]:box(x,y,z,.055,.055,.95,tile)
 box(0,y,z+.95,w,.05,.05,tile)
 for i in range(1,round(w/.24)):box(-w/2+i*w/round(w/.24),y,z,.028,.028,.91,tile)
def arched_window(x,y,z,w,h,wall='plaster'):
 # Semicircular masonry head, dark real opening and thin weathered jamb.
 r=w/2;spring=z+h-r
 pts=[(x-r,y+.04,z),(x+r,y+.04,z),(x+r,y+.04,spring)]
 pts += [(x+r*math.cos(i*math.pi/8),y+.04,spring+r*math.sin(i*math.pi/8)) for i in range(1,9)]
 face(pts,'glass')
 for xx in [x-r,x+r]:box(xx,y-.05,z,.085,.15,h-r,'trim')
 for i in range(8):
  a=i*math.pi/8;b=(i+1)*math.pi/8
  face([(x+r*math.cos(a),y-.04,spring+r*math.sin(a)),(x+(r+.09)*math.cos(a),y-.04,spring+(r+.09)*math.sin(a)),(x+(r+.09)*math.cos(b),y-.04,spring+(r+.09)*math.sin(b)),(x+r*math.cos(b),y-.04,spring+r*math.sin(b))],'trim')
  # Fill square opening corners above the arch with actual wall triangles.
  face([(x+r*math.cos(b),y-.02,spring+r*math.sin(b)),(x+r*math.cos(a),y-.02,spring+r*math.sin(a)),(x+r*math.cos(a),y-.02,z+h+.01),(x+r*math.cos(b),y-.02,z+h+.01)],wall)
 box(x,y-.06,z,.045,.09,h-.08,'wood_dark');box(x,y-.06,spring,w,.09,.045,'wood_dark');box(x,y-.1,z-.07,w+.18,.3,.08,'stone')
def terrace(v):
 global PART
 w=[5.5,6.75][v];d=6.;levels=2+v;fh=2.8
 for level in range(levels):
  y=level*(.9 if not v else 1.1);width=w-(.35 if level==levels-1 else 0);tile='plaster' if not v or level==0 else 'rose_wall';z=level*fh;PART='stepped_walls'
  xs=[-width*.26,width*.26];ops=[(xs[0]-.52,xs[0]+.52,z+.75,z+2.12),(xs[1]-.48,xs[1]+.48,z+.03,z+2.13)]
  if level==0:ops=[(-.48,.48,0,2.2)]+[(xs[0]-.5,xs[0]+.5,.75,2.05)]
  shell(width,d-y,fh,tile,ops,y,z)
  for x in [xs[0]]:
   if level==levels-1:arched_window(x,y+.1,z+.75,1.04,1.37,tile)
   else:front_window(x,y+.16,z+.75,1.04,1.37,False,True)
  if level==0:front_window(0,y+.16,.03,.96,2.17,False)
  else:
   front_window(xs[1],y+.16,z+.03,.96,2.1,False)
   box(xs[1]+.32,y-.01,z+1.02,.045,.09,.18,'metal')
   box(xs[1],y+.055,z+.03,.88,.07,.65,'wood')
  PART='terrace_coping';box(0,y+(d-y)/2,z+fh,width+.12,d-y+.12,.12,'stone')
  if level:
   railing(width-.2,y-.65,z+.12)
   for xx in [-(width-.2)/2,(width-.2)/2]:
    beam((xx,y-.65,z+1.07),(xx,y,z+1.07),.05,'metal')
   if level==1:bench(-width*.26,y-.34,z+.12,w=1.15);pot(0,y-.32,z+.12)
   else:
    # Folded laundry hangs over the low balcony rail, clear of the window.
    box(-width*.3,y-.69,z+.55,.7,.025,.48,'cloth')
    box(-width*.3,y-.61,z+.98,.7,.18,.035,'cloth')
    box(width*.06,y-.28,z+.12,.72,.35,.4,'wood')
  # short parapet at flat roof sides/rear leaves visible walkable terrace.
  if level==levels-1:
   for x in [-width/2,width/2]:box(x,y+(d-y)/2,z+fh+.12,.14,d-y,.55,tile)
   box(0,d-.04,z+fh+.12,width,.14,.55,tile)
 if v:
  PART='slope_access';x=w/2+.55
  for i in range(14):box(x,.25+i*.32,0,.95,.32,(i+1)*.2,'stone')
  beam((x+.43,.3,.95),(x+.43,4.55,3.75),.055,'metal')
  box(x,5.17,2.68,1.1,1.2,.12,'stone')
  for yy in [.3,4.55,5.72]:
   base=.2 if yy==.3 else 2.8
   beam((x+.48,yy,base),(x+.48,yy,base+.95),.055,'metal')
  beam((x+.48,4.55,3.75),(x+.48,5.72,3.75),.055,'metal')
  for i in range(1,14):
   yy=.3+i*(4.25/14);top=.95+(yy-.3)*(2.8/4.25)
   beam((x+.45,yy,max(.2,top-.85)),(x+.45,yy,top),.045,'metal')
  for yy in [4.85,5.15,5.45]:beam((x+.48,yy,2.8),(x+.48,yy,3.75),.045,'metal')
  box(x+.47,5.65,0,.09,.09,2.68,'metal')
  # Upper side entrance serves the external stair landing (not a floating door).
  n=len(V);front_window(0,-.13,2.8,.9,2.1,False)
  for i in range(n,len(V)):
   a,b,c=V[i];V[i]=(w/2-b,5.15+a,c)
 else:canopy(1.5,0,2.42,.8,'metal')
 PART='service';side_details(w,d,levels*fh);pot(-w*.4,-.28);bench(w*.25,-.32,w=1.2)
 return {'footprint_m':[w,d],'storeys':levels,'slope_variant':bool(v),'upper_access_height_m':2.8 if v else None,'wear_locations':['stair treads','plinth','parapet coping','shutter edges'],'front_wall_z':0}
def warehouse(v):
 global PART
 w=[9.,11.][v];d=[8.5,9.5][v];h=[4.25,5.1][v];PART='warehouse_shell'
 centers=[-1.2] if not v else [-2.5,2.5];doorw=3.6 if not v else 3.5
 ops=[(x-doorw/2,x+doorw/2,0,3.25) for x in centers]
 if not v:ops.append((w*.33-.45,w*.33+.45,0,2.2))
 else:ops.append((-.45,.45,0,2.2))
 shell(w,d,h,'plaster',ops)
 PART='base'
 edges=sorted([-w/2,w/2]+[q for a,b,c,d0 in ops for q in [a,b]])
 for a,b in zip(edges,edges[1:]):
  if not any(a>=l and b<=r for l,r,lo,hi in ops):box((a+b)/2,-.025,0,b-a,.05,.55,'stonewall')
 PART='shutters'
 for x in centers:
  lift=.65 if v and x>0 else 0
  box(x,.12,.03+lift,doorw,.1,3.2-lift,'metal')
  for z in [i*.2+.12 for i in range(16)]:
   if z>lift:box(x,.035,z,doorw,.05,.035,'metal')
  if lift:
   box(x,.02,lift,doorw,.13,.09,'metal')
   box(x,-.22,0,doorw+.18,.65,.07,'stone')
  for xx in [x-doorw/2-.1,x+doorw/2+.1]:box(xx,-.055,0,.16,.2,3.45,'metal')
  box(x,-.06,3.3,doorw+.35,.32,.25,'metal')
 PART='roof';roof(w,d,h,1.65 if not v else 2.,'metal',False)
 for i in range(1,round(w/.55)):
  x=-w/2+i*w/round(w/.55);zz=h+(1.65 if not v else 2.)*(1-abs(x)/(w/2+.35))+.035
  beam((x,-.47,zz),(x,d+.27,zz),.045,'metal',offset=(24,24))
 PART='life';sign(0,-.59,h+.3,2.2)
 # Service door and asymmetrical loading canopy.
 if not v:
  front_window(w*.33,.16,.03,.9,2.1,False)
  box(w*.33,.055,.03,.82,.07,.65,'wood')
  box(w*.33+.3,-.01,1.02,.045,.09,.18,'metal')
 else:
  box(0,.08,.03,.9,.1,2.1,'wood_dark')
  for xx in [-.48,.48]:box(xx,-.015,0,.09,.16,2.2,'metal')
  box(0,-.015,2.15,1.05,.16,.09,'metal')
  box(0,.015,1.52,.62,.04,.4,'glass')
  box(.3,-.04,.95,.055,.1,.22,'trim')
  # A working loading bay: one door raised, pallet and goods at its side.
  box(3.65,-.55,0,.85,.75,.1,'wood_dark')
  box(3.65,-.55,.1,.65,.58,.48,'wood')
  box(3.65,-.55,.58,.52,.5,.42,'wood')
 canopy(4.5 if not v else 8.8,0,3.8,1.4,'metal')
 for x in [-w/2+.12,w/2-.12]:beam((x,-.06,.08),(x,-.06,h-.1),.09,'metal')
 if not v:
  for i in range(2):box(w/2+.32,1.2+i*.7,0,.58,.6,.55,'wood')
 side_details(w,d,h)
 return {'footprint_m':[w,d],'storeys':1,'wear_locations':['loading threshold','lower masonry','shutter edges','roof seams','rain pipes'],'front_wall_z':0}
def material():
 im=bpy.data.images.load(str(TEX),check_existing=True);m=bpy.data.materials.new('BayLoop_Photo32');m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=1;bs.inputs['Metallic'].default_value=0
 tx=m.node_tree.nodes.new('ShaderNodeTexImage');tx.image=im;tx.interpolation='Closest';tx.extension='EXTEND';m.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color']);return m
def mesh(name,mat):
 me=bpy.data.meshes.new(name);me.from_pydata(V,[],F);me.update();uv=me.uv_layers.new()
 for p,coords in zip(me.polygons,UV):
  for li,co in zip(p.loop_indices,coords):uv.data[li].uv=co
 obj=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(obj);obj.data.materials.append(mat);return obj
def aim(ob,target):ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def stage():
 sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=24;sc.cycles.use_denoising=True
 sc.world=bpy.data.worlds.new('Shared_Autumn_Ambient')
 sc.render.resolution_x=960;sc.render.resolution_y=540;sc.render.resolution_percentage=100;sc.world.color=(.6,.65,.7)
 sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.62,.69,.75,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.65
 sc.view_settings.view_transform='Standard';sc.view_settings.look='Medium High Contrast' if False else 'None';sc.view_settings.exposure=0
 bpy.ops.object.light_add(type='AREA',location=(-8,-10,17));bpy.context.object.data.energy=2400;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=12;aim(bpy.context.object,(0,3,2))
 bpy.ops.object.camera_add(location=(15,-20,13));sc.camera=bpy.context.object;sc.camera.data.type='ORTHO';sc.camera.data.ortho_scale=20;aim(sc.camera,(0,3,3))
 bpy.ops.mesh.primitive_plane_add(size=300,location=(0,0,-.025));ground=bpy.context.object;ground.name='REVIEW_ONLY_ground'
 gm=bpy.data.materials.new('REVIEW_ONLY_neutral');gm.diffuse_color=(.52,.52,.46,1);ground.data.materials.append(gm)
 return sc
def render(sc,path,loc,target,scale):
 sc.camera.location=loc;aim(sc.camera,target);sc.camera.data.ortho_scale=scale;sc.render.filepath=str(path);bpy.ops.render.render(write_still=True)
def register(paths,urls):
 prov=ROOT/'art/provenance.csv'
 with prov.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
 for p in paths:
  rows=[r for r in rows if r['path']!=rel(p)];rows.append(dict(asset_id='bayloop_'+p.stem+'_'+p.suffix[1:],path=rel(p),source_url='; '.join(urls),license='CC0-1.0; original bpy geometry; Pretendard OFL-1.1',accessed='2026-10-25',tool='bayloop_buildings.py; Blender bpy; photo_to_pixel.py; bayloop_textures.py',ai_generated='no',sha256=sha(p),notes='Original metre-scale geometry; photographed surface atlas 32px/m; origin front wall ground centre; +Z front; preview-only native Blender scene, no AI'))
 with prov.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
def main():
 global TEX
 p=argparse.ArgumentParser();p.add_argument('--type',required=True,choices=['shop_house','terrace_house','harbor_warehouse']);args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
 specific=OUT/(args.type+'_Atlas.png')
 if specific.exists():TEX=specific
 REVIEW.mkdir(parents=True,exist_ok=True);bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC';mat=material()
 models=[];assets=[];urls=sorted({t['source_url'] for t in TM['tiles']});fn={'shop_house':shop,'terrace_house':terrace,'harbor_warehouse':warehouse}[args.type]
 register([TEX],urls)
 for v in range(2):
  V.clear();F.clear();UV.clear();PARTS.clear();info=fn(v);name=args.type+'_'+chr(97+v);obj=mesh(name,mat);tris=sum(len(f)-2 for f in F);assert tris<=4000,(name,tris)
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
  glb=OUT/(name+'.glb');fbx=OUT/(name+'.fbx')
  bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_yup=True,export_texcoords=True,export_normals=True,export_materials='EXPORT')
  bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
  verts=[(a,c,-b) for a,b,c in V];lo=[min(p[k] for p in verts) for k in range(3)];hi=[max(p[k] for p in verts) for k in range(3)]
  asset=dict(id=name,type=args.type,model=rel(glb),fbx=rel(fbx),texture=rel(TEX),dimensions_m=[hi[k]-lo[k] for k in range(3)],bounds_min=lo,bounds_max=hi,source_urls=urls,tris=tris,**info)
  (OUT/(name+'.geometry.json')).write_text(json.dumps({'vertices_y_up':verts,'faces':F,'part_per_face':PARTS,'uv_per_face':UV}),encoding='utf-8');assets.append(asset);models.append(obj)
  register([glb,fbx],urls)
 manifest=OUT/'manifest.json';old=json.loads(manifest.read_text(encoding='utf-8')) if manifest.exists() else {'palette':rel(ROOT/'art/source/palette/pitcrew_d123_32.json'),'density_px_m':32,'max_tris':4000,'assets':[]}
 old['source_manifests']=['art/textures/2026-10-24-kp-v2/source_wood_e.json','art/textures/2026-10-24-kp-v2/source_roof_candidates.json','art/textures/2026-10-24-kp/source_plaster.json','art/textures/2026-10-24-kp/source_stone.json','art/source/hillhouse-surfaces/sources.json','art/models/bayloop_buildings_v01/metal-source.json']
 old['assets']=[a for a in old['assets'] if a['type']!=args.type]+assets;manifest.write_text(json.dumps(old,ensure_ascii=False,indent=2),encoding='utf-8')
 sc=stage()
 for i,obj in enumerate(models):
  for other in models:other.hide_render=other!=obj
  name=obj.name;h=assets[i]['dimensions_m'][1];scale=max(19,assets[i]['dimensions_m'][0]*1.8,h*2.3)
  render(sc,REVIEW/(name+'_front.png'),(13,-20,12),(0,3,h*.43),scale)
 for i,obj in enumerate(models):obj.hide_render=False;obj.location.x=(-1 if i==0 else 1)*7
 render(sc,REVIEW/(args.type+'_pair.png'),(14,-32,16),(0,3,3.5),31)
 for obj in models:obj.location.x=0
 for i,obj in enumerate(models):
  for other in models:other.hide_render=other!=obj
  name=obj.name;h=assets[i]['dimensions_m'][1];scale=max(19,assets[i]['dimensions_m'][0]*1.8,h*2.3)
  render(sc,REVIEW/(name+'_back.png'),(-15,20,13),(0,3,h*.43),scale)
  render(sc,REVIEW/(name+'_top.png'),(0,3,35),(0,3,0),scale)
  render(sc,REVIEW/(name+'_side.png'),(30,3,h*.5),(0,3,h*.5),scale)
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(args.type+'.blend')))
 print('BAYLOOP_DONE',json.dumps([{'id':a['id'],'tris':a['tris'],'dimensions':a['dimensions_m']} for a in assets]))
if __name__=='__main__':main()
