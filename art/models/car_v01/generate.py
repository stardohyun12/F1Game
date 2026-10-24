"""Old delivery formula: metre geometry, exact 64 px/m, no F1 writes.

Blender -Y forward / Z up. Current CoastalFormula external bounds and
four wheel centres are retained. Texture is CC0 photographs + scripted ink.
"""
from pathlib import Path
import bpy, math, json
from mathutils import Vector

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
D=64.;N=512;EDGE=120/D
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene;sc.unit_settings.system='METRIC';sc.unit_settings.scale_length=1
image=bpy.data.images.load(str(HERE/'CarAtlas.png'));image.pack()
m=bpy.data.materials.new('Car_photo64');m.use_nodes=True
bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=1
bs.inputs['Specular IOR Level'].default_value=0
tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest'
m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
groups={};part='body';detail='shell'

def data():return groups.setdefault(part,{'v':[],'f':[],'uv':[],'details':[]})
def clip(poly,axis,val,greater):
    out=[]
    for a,b in zip(poly,poly[1:]+poly[:1]):
        ai=a[axis]>=val-1e-8 if greater else a[axis]<=val+1e-8
        bi=b[axis]>=val-1e-8 if greater else b[axis]<=val+1e-8
        if ai:out.append(a)
        if ai!=bi:
            t=(val-a[axis])/(b[axis]-a[axis]);out.append(tuple(a[k]+t*(b[k]-a[k]) for k in (0,1)))
    return out

def face(points,tile=0,offset=None):
    ps=[Vector(p) for p in points];origin=ps[0]
    u=(ps[1]-origin).normalized();normal=(ps[1]-origin).cross(ps[2]-origin).normalized()
    if normal.length<.1:return
    v=normal.cross(u).normalized();uv=[((p-origin).dot(u),(p-origin).dot(v)) for p in ps]
    mn=[min(p[k] for p in uv) for k in (0,1)];mx=[max(p[k] for p in uv) for k in (0,1)]
    for j in range(max(1,math.ceil((mx[1]-mn[1])/EDGE-1e-7))):
      for i in range(max(1,math.ceil((mx[0]-mn[0])/EDGE-1e-7))):
        a=mn[0]+i*EDGE;b=min(a+EDGE,mx[0]);c=mn[1]+j*EDGE;d=min(c+EDGE,mx[1]);q=uv
        for axis,val,g in [(0,a,True),(0,b,False),(1,c,True),(1,d,False)]:q=clip(q,axis,val,g)
        clean=[]
        for p in q:
            if not clean or abs(p[0]-clean[-1][0])+abs(p[1]-clean[-1][1])>1e-5:clean.append(p)
        if len(clean)>2 and abs(clean[0][0]-clean[-1][0])+abs(clean[0][1]-clean[-1][1])<1e-5:clean.pop()
        if len(clean)<3:continue
        area=abs(sum(p[0]*r[1]-r[0]*p[1] for p,r in zip(clean,clean[1:]+clean[:1])))*.5
        if area<1e-9:continue
        ox=64-(b-a)*D/2;oy=64-(d-c)*D/2
        # Choose real photographed cloth variation at shoulder and folded lower
        # bag bevel; their tiny centred crop otherwise missed all material detail.
        if detail=='jacket_shoulders' and abs(normal.x)>.95:oy=52
        if tile==6 and abs(normal.z)>.85 and abs(normal.y)>.2:oy=58
        if offset is not None:ox,oy=offset
        ox=max(4,min(124-(b-a)*D,ox));oy=max(4,min(124-(d-c)*D,oy))
        g=data();start=len(g['v']);g['v'].extend([tuple(origin+u*p[0]+v*p[1]) for p in clean])
        g['f'].append(tuple(range(start,start+len(clean))))
        # Top-down PNG cell and local Y; UV v is bottom-up.
        g['uv'].append([((tile%4*128+ox+(p[0]-a)*D)/N,1-(tile//4*128+oy+(d-p[1])*D)/N) for p in clean])
        g['details'].append(detail)

def box(name,loc,size,tile=0):
    global detail;detail=name
    x,y,z=loc;w,d,h=[v/2 for v in size]
    p=[(x-w,y-d,z-h),(x+w,y-d,z-h),(x+w,y+d,z-h),(x-w,y+d,z-h),
       (x-w,y-d,z+h),(x+w,y-d,z+h),(x+w,y+d,z+h),(x-w,y+d,z+h)]
    for ids in [(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7),(3,2,1,0)]:face([p[k] for k in ids],tile)

def hull(name,sections,tile=0,shift=0):
    """Chamfered eight-vertex sections; same physical taper as sheet aluminium."""
    global detail;detail=name;rings=[]
    for y,w,b,t in sections:
        c=min(.045,w*.18,(t-b)*.22)
        rings.append([(shift-w+c,y,b),(shift+w-c,y,b),(shift+w,y,b+c),(shift+w,y,t-c),
                      (shift+w-c,y,t),(shift-w+c,y,t),(shift-w,y,t-c),(shift-w,y,b+c)])
    face(rings[0][::-1],tile)
    for a,b in zip(rings,rings[1:]):
        for i in range(8):face([a[i],a[(i+1)%8],b[(i+1)%8],b[i]],1 if tile==0 and i in (2,6) else tile)
    face(rings[-1],tile)

def beam(name,a,b,width,tile=3):
    global detail;detail=name;a=Vector(a);b=Vector(b);z=(b-a).normalized()
    helper=Vector((0,0,1)) if abs(z.z)<.9 else Vector((1,0,0))
    x=z.cross(helper).normalized()*width/2;y=z.cross(x).normalized()*width/2
    p=[a-x-y,a+x-y,a+x+y,a-x+y,b-x-y,b+x-y,b+x+y,b-x+y]
    for ids in [(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7),(3,2,1,0)]:face([p[k] for k in ids],tile)

def disc_x(name,x,y,z,r,n=12,tile=3):
    global detail;detail=name
    ps=[(x,y+math.sin(k*math.tau/n)*r,z+math.cos(k*math.tau/n)*r) for k in range(n)]
    face(ps if x>0 else ps[::-1],tile)

# Restrained winged 1970s silhouette: open wheels, narrow wedge, visible frame.
part='body'
box('dark_undertray',(0,.05,.18),(1.25,2.40,.08),14)
hull('tapered_nose',[(-2.03,.115,.275,.36),(-1.66,.16,.275,.415),(-.79,.26,.25,.57),(-.37,.31,.25,.635)])
# A narrow cream stripe, deliberately shorter than the nose; no wide white wing.
face([(-.047,-1.85,.392),(.047,-1.85,.392),(.067,-.82,.571),(-.067,-.82,.571)],12)
for s in [-1,1]:
    hull('sidepod', [(-.43,.12,.24,.48),(-.22,.23,.23,.56),(.62,.22,.23,.56),(1.19,.12,.25,.42)],shift=s*.43)
    box('intake_recess',(s*.43,-.437,.40),(.20,.014,.12),14)
    hull('cockpit_rim',[(-.34,.045,.49,.65),(.34,.045,.49,.65)],shift=s*.29)
    # Small worn number sticker: 0.375 × 0.25 m, exact 24×16 texels.
    x=s*.666
    points=[(x,-.10,.285),(x,.275,.285),(x,.275,.535),(x,-.10,.535)]
    if s<0:points=[points[k] for k in (1,0,3,2)]
    face(points,10,offset=(52,56))
    beam('mirror_stalk',(s*.30,-.32,.59),(s*.49,-.39,.69),.024)
    box('mirror_shell',(s*.50,-.40,.70),(.14,.08,.07),0)
    box('mirror_glass',(s*.50,-.354,.70),(.105,.01,.04),13)
# One tape repair bridges a seam on the left upper pod. Physical 4cm tape.
face([(-.64,.39,.561),(-.32,.39,.561),(-.32,.44,.561),(-.64,.44,.561)],11)
face([(-.665,.44,.36),(-.665,.39,.36),(-.665,.39,.53),(-.665,.44,.53)],11)
box('seat_cavity',(0,.025,.45),(.47,.64,.08),14)
box('seat_back',(0,.29,.64),(.39,.1,.35),9)
box('dash',(0,-.31,.57),(.43,.1,.14),14)
part='engine'
hull('engine_cowl',[(.37,.25,.30,.65),(.80,.22,.30,.59),(1.31,.13,.28,.40)])
for s in [-1,1]:beam('exhaust',(s*.18,1.25,.31),(s*.18,1.65,.31),.055,3)
box('tail_lamp',(0,1.66,.39),(.09,.018,.07),15)
part='aero'
box('front_wing',(0,-1.78,.18),(1.78,.30,.055),0)
box('front_flap',(0,-1.59,.235),(1.56,.10,.035),1)
for s in [-1,1]:
    box('front_endplate',(s*.875,-1.77,.23),(.035,.43,.22),1)
    box('rear_post',(s*.36,1.49,.47),(.035,.09,.43),3)
    box('rear_endplate',(s*.72,1.545,.69),(.04,.42,.20),1)
box('rear_wing',(0,1.575,.735),(1.46,.36,.045),0)
box('rear_worn_lip',(0,1.735,.756),(1.40,.04,.018),11)
box('rear_flap',(0,1.395,.77),(1.40,.09,.04),0)
part='suspension'
for s in [-1,1]:
    for y in [-1.2,1.1]:
        for z in [.27,.42]:
            for dy in [-.22,.22]:beam('wishbone',(s*.25,y+dy,z),(s*.79,y,.35 if y<0 else .375),.025)
    beam('rear_cross_stay',(s*.28,.65,.35),(s*.38,1.30,.62),.028)
beam('rear_crossbeam',(-.65,1.40,.26),(.65,1.40,.26),.035)
for s in [-1,1]:beam('rear_diagonal',(s*.65,1.41,.26),(-s*.23,1.41,.58),.028)
part='driver'
box('jacket_shoulders',(0,.07,.61),(.35,.25,.17),12)
box('neck_collar',(0,.055,.752),(.18,.16,.18),12)
for s in [-1,1]:beam('sleeve',(s*.15,.03,.66),(s*.16,-.22,.62),.075,12)
# 12 sided helmet with finite top; highest point matches old 1.072m exactly.
detail='helmet';rings=[]
for z,r in [(.78,.13),(.86,.185),(.97,.16),(1.045,.09),(1.072,.025)]:
    rings.append([(math.cos(i*math.tau/12)*r,.055+math.sin(i*math.tau/12)*r,z) for i in range(12)])
for a,b in zip(rings,rings[1:]):
    for i in range(12):face([a[i],a[(i+1)%12],b[(i+1)%12],b[i]],12)
face(rings[0][::-1],12);face(rings[-1],12)
face([(-.13,-.084,.88),(-.08,-.126,.88),(-.08,-.126,.95),(-.13,-.084,.95)],13)
face([(-.08,-.126,.88),(.08,-.126,.88),(.08,-.126,.95),(-.08,-.126,.95)],13)
face([(.08,-.126,.88),(.13,-.084,.88),(.13,-.084,.95),(.08,-.126,.95)],13)
for s in [-1,1]:beam('rollbar',(s*.22,.36,.60),(s*.17,.36,.94),.031,3)
beam('rollbar_top',(-.17,.36,.94),(.17,.36,.94),.031,3)
beam('steering_bar',(-.14,-.25,.63),(.14,-.25,.63),.035,9)
part='delivery_bag'
box('bag_rack',(0,.94,.64),(.78,.72,.045),3)
# Chamfered soft rectangular insulated box, broad orange face toward chase camera.
hull('insulated_canvas_bag',[(.62,.28,.68,.93),(.68,.34,.65,.955),(1.18,.34,.65,.955),(1.25,.28,.68,.93)],6)
hull('padded_fabric_lid',[(.61,.28,.946,.966),(.68,.35,.946,.991),(1.18,.35,.946,.982),(1.26,.28,.936,.959)],8)
# A continuous soft front panel, lightly bulged between the retaining straps.
for xa,xb,ya,yb in [(-.28,-.20,1.252,1.270),(-.20,.20,1.270,1.270),(.20,.28,1.270,1.252)]:
    face([(xb,yb,.69),(xa,ya,.69),(xa,ya,.925),(xb,yb,.925)],7)
for s in [-1,1]:
    box('bag_strap_top',(s*.24,.935,1.001),(.045,.64,.014),9)
    for y in [.61,1.27]:box('bag_strap_vertical',(s*.24,y,.823),(.045,.013,.325),9)
    box('small_metal_buckle',(s*.24,1.283,.785),(.06,.014,.047),3)
for x in [-.09,.09]:beam('bag_handle_upright',(x,.93,1.009),(x,.93,1.047),.022,9)
beam('bag_handle',(-.09,.93,1.047),(.09,.93,1.047),.022,9)
# Small clear delivery tag; no invented words in texture.
face([(.065,1.276,.80),(-.065,1.276,.80),(-.065,1.276,.865),(.065,1.276,.865)],12)

wheel_centers={}
for axle,y in [('front',-1.2),('rear',1.1)]:
  for side,s in [('left',1),('right',-1)]:
    part=f'wheel_{axle}_{side}';cx=s*.79;r=.35 if axle=='front' else .375;z=r
    w=.30 if axle=='front' else .378909035
    wheel_centers[part]=(cx,y,z);detail='rubber';rings=[];n=16
    for dx,rad in [(-w/2,r*.88),(-w/2+.032,r),(w/2-.032,r),(w/2,r*.88)]:
        rings.append([(cx+dx,y+math.sin(k*math.tau/n)*rad,z+math.cos(k*math.tau/n)*rad) for k in range(n)])
    for j,(a,b) in enumerate(zip(rings,rings[1:])):
        for k in range(n):face([a[k],a[(k+1)%n],b[(k+1)%n],b[k]],5 if j==1 else 4)
    face(rings[0][::-1],4);face(rings[-1],4)
    # Shallow stamped metal rim on each side, no 3D spokes wasting silhouettes.
    for ss in [-1,1]:
        xx=cx+ss*(w/2+.0001)
        detail='rim';outer=[];inner=[]
        for k in range(12):
            a=k*math.tau/12;outer.append((xx,y+math.sin(a)*r*.54,z+math.cos(a)*r*.54));inner.append((xx,y+math.sin(a)*r*.39,z+math.cos(a)*r*.39))
        for k in range(12):face([outer[k],outer[(k+1)%12],inner[(k+1)%12],inner[k]],3)
        disc_x('rim_inset',xx+ss*.0001,y,z,r*.39,12,14)
        disc_x('hub',xx+ss*.0002,y,z,.057,8,3)

objects=[];report={}
for name,g in groups.items():
    pivot=Vector(wheel_centers.get(name,(0,0,0)))
    me=bpy.data.meshes.new(name+'_mesh');me.from_pydata([Vector(v)-pivot for v in g['v']],[],g['f']);me.update()
    o=bpy.data.objects.new(name,me);sc.collection.objects.link(o);o.location=pivot;me.materials.append(m)
    uv=me.uv_layers.new(name='Pixel64')
    for polygon,coords in zip(me.polygons,g['uv']):
        for loop,co in zip(polygon.loop_indices,coords):uv.data[loop].uv=co
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    # Recalculate closed component normals while retaining split UV seams.
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.000001);bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    mod=o.modifiers.new('triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name)
    o['density_px_m']=64;o['units']='metres';o['AI_generated']=False
    if name.startswith('wheel_'):o['spin_axis']='local X';o['pivot_contract']='current CoastalFormula wheel centre'
    objects.append(o);report[name]={'triangles':len(me.polygons),'pivot_blender':list(pivot),'parts':sorted(set(g['details']))}
    o.select_set(False)
# New rim discs extend 0.3mm: remove it from tyre width to preserve measured bounds.
for o in objects:
    if o.name.startswith('wheel_rear'):
        for v in o.data.vertices:v.co.x*=.1894545175/.1897545175
total=sum(v['triangles'] for v in report.values());assert total<=3000,total
bpy.context.view_layer.update()
vs=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
bounds={'min':[min(v[k] for v in vs) for k in range(3)],'max':[max(v[k] for v in vs) for k in range(3)]}
for o in objects:o.select_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'car_v01.blend'))
bpy.ops.export_scene.gltf(filepath=str(HERE/'car_v01.glb'),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_extras=True)
bpy.ops.export_scene.fbx(filepath=str(HERE/'car_v01.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,apply_scale_options='FBX_SCALE_ALL',path_mode='COPY',embed_textures=True)
report={'triangles':total,'density':64,'bounds_blender':bounds,'objects':report,'wear':{'body':'lower sidepod chip, left seam tape','bag':'handling edges, lid folds, strap contact','metal':'rack/wishbone local oxidation'},'source':'CC0 photography + scripts; original concept is visual reference only'}
(HERE/'geometry.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('CAR_OK',json.dumps({'triangles':total,'bounds':bounds}))
