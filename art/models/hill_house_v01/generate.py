"""Deterministic metre-scale rural house; no F1/Assets writes.

Reuses lofi_pilot's 512/64 atlas grid, 2px gutter, flat faces. Partial
UV spans replace the old full-cell stretch, fixing world density to 32px/m.
"""
from pathlib import Path
import bpy, math, json, shutil
from mathutils import Vector

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
SRC=ROOT/'art/source/hillhouse-surfaces/extension_atlas.png'
ATLAS=HERE/'HillHouseAtlas.png'
D=32.; N=512; CELL=64; EDGE=60/D
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene;sc.unit_settings.system='METRIC';sc.unit_settings.scale_length=1
shutil.copy2(SRC,ATLAS)
im=bpy.data.images.load(str(ATLAS));im.name='HillHouseAtlas';im.pack()

def material(name,tint=(1,1,1,1),emission=0):
    m=bpy.data.materials.new(name);m.use_nodes=True
    nd=m.node_tree.nodes;bs=nd.get('Principled BSDF');bs.inputs['Roughness'].default_value=1
    tex=nd.new('ShaderNodeTexImage');tex.image=im;tex.interpolation='Closest'
    if tint!=(1,1,1,1):
        mix=nd.new('ShaderNodeMix');mix.data_type='RGBA';mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[7].default_value=tint
        m.node_tree.links.new(tex.outputs['Color'],mix.inputs[6]);m.node_tree.links.new(mix.outputs[2],bs.inputs['Base Color'])
    else:m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if emission:
        bs.inputs['Emission Color'].default_value=(1,.51,.18,1)
        bs.inputs['Emission Strength'].default_value=emission
    return m
MATS=[material('hill_photo32'),material('one_inhabited_window',emission=.6),material('unlit_glass',(.12,.19,.21,1)),material('plant',(.42,.6,.20,1))]
V=[];F=[];UV=[];MI=[];PART=[]
part='shell'

def clip(poly,axis,value,greater):
    if not poly:return []
    result=[]
    for a,b in zip(poly,poly[1:]+poly[:1]):
        ai=(a[axis]>=value-1e-9) if greater else (a[axis]<=value+1e-9)
        bi=(b[axis]>=value-1e-9) if greater else (b[axis]<=value+1e-9)
        if ai:result.append(a)
        if ai!=bi:
            t=(value-a[axis])/(b[axis]-a[axis]);result.append(tuple(a[k]+t*(b[k]-a[k]) for k in (0,1)))
    return result

def face(points,tile=51,mat=0,offset=None):
    """Planar polygons tiled in orthonormal coordinates, exact density."""
    ps=[Vector(p) for p in points]
    origin=ps[0];u=(ps[1]-origin).normalized()
    normal=(ps[1]-origin).cross(ps[2]-origin).normalized();v=normal.cross(u).normalized()
    uv=[((p-origin).dot(u),(p-origin).dot(v)) for p in ps]
    minx=min(p[0] for p in uv);maxx=max(p[0] for p in uv);miny=min(p[1] for p in uv);maxy=max(p[1] for p in uv)
    wall_strips=tile==51 and part in ('walls','gable') and abs(normal.z)<.1
    edge_y=.875 if wall_strips else EDGE
    nx=max(1,math.ceil((maxx-minx)/EDGE-1e-7));ny=max(1,math.ceil((maxy-miny)/edge_y-1e-7))
    for j in range(ny):
      for i in range(nx):
        a=minx+i*EDGE;b=min(a+EDGE,maxx);c=miny+j*edge_y;d=min(c+edge_y,maxy)
        q=uv
        for axis,val,g in [(0,a,True),(0,b,False),(1,c,True),(1,d,False)]:q=clip(q,axis,val,g)
        clean=[]
        for p in q:
            if not clean or abs(p[0]-clean[-1][0])+abs(p[1]-clean[-1][1])>1e-5:clean.append(p)
        if len(clean)>2 and abs(clean[0][0]-clean[-1][0])+abs(clean[0][1]-clean[-1][1])<1e-5:clean.pop()
        if len(clean)<3:continue
        area=abs(sum(p[0]*r[1]-r[0]*p[1] for p,r in zip(clean,clean[1:]+clean[:1])))*.5
        if area<1e-10:continue
        # Center small pieces; opt-in physical location selects localized wear.
        ox=2+(60-(b-a)*D)/2;oy=2+(60-(d-c)*D)/2
        if wall_strips:
            low_z=min((origin+u*p[0]+v*p[1]).z for p in clean)
            oy=2 if low_z<.8 else 32
        if offset:ox,oy=offset
        ox=max(2,min(62-(b-a)*D,ox));oy=max(2,min(62-(d-c)*D,oy))
        start=len(V);V.extend([tuple(origin+u*p[0]+v*p[1]) for p in clean])
        F.append(tuple(range(start,start+len(clean))))
        UV.append([(((tile%8)*64+ox+(p[0]-a)*D)/N,((tile//8)*64+oy+(p[1]-c)*D)/N) for p in clean])
        MI.append(mat);PART.append(part)

def box(x,y,z,w,d,h,tile=51,mat=0,offset=None):
    p=[(x-w/2,y-d/2,z),(x+w/2,y-d/2,z),(x+w/2,y+d/2,z),(x-w/2,y+d/2,z),
       (x-w/2,y-d/2,z+h),(x+w/2,y-d/2,z+h),(x+w/2,y+d/2,z+h),(x-w/2,y+d/2,z+h)]
    for ids in [(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7),(3,2,1,0)]:face([p[k] for k in ids],tile,mat,offset)

def cylinder(x,y,z,r,h,tile=52,n=8,top_r=None):
    rt=r if top_r is None else top_r
    a=[(x+r*math.cos(i*math.tau/n),y+r*math.sin(i*math.tau/n),z) for i in range(n)]
    b=[(x+rt*math.cos(i*math.tau/n),y+rt*math.sin(i*math.tau/n),z+h) for i in range(n)]
    for i in range(n):j=(i+1)%n;face([a[i],a[j],b[j],b[i]],tile)
    face(a[::-1],tile);face(b,tile)

def beam(a,b,width,tile=52):
    a=Vector(a);b=Vector(b);z=(b-a).normalized();helper=Vector((0,0,1)) if abs(z.z)<.9 else Vector((1,0,0))
    x=z.cross(helper).normalized()*width/2;y=z.cross(x).normalized()*width/2
    p=[a-x-y,a+x-y,a+x+y,a-x+y,b-x-y,b+x-y,b+x+y,b-x+y]
    for ids in [(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7),(3,2,1,0)]:face([p[k] for k in ids],tile)

# 7.2 x 5.8m rectangular footprint, 3.05m wall above 0.25m foundation.
part='foundation';box(0,0,0,7.3,5.9,.25,57)
part='walls';box(0,0,.25,7.2,5.8,3.05,51)
part='gable'
face([(-3.6,2.9,3.3),(-3.6,-2.9,3.3),(-3.6,0,5.05)],51)
face([(3.6,-2.9,3.3),(3.6,2.9,3.3),(3.6,0,5.05)],51)
part='roof'
# Pitch aligned with the facade; chimney pierces one slope, crisp overhang.
face([(-4.03,-3.32,3.16),(4.03,-3.32,3.16),(4.03,0,5.17),(-4.03,0,5.17)],53)
face([(-4.03,0,5.17),(4.03,0,5.17),(4.03,3.32,3.16),(-4.03,3.32,3.16)],53)
face([(-4.03,3.32,3.05),(4.03,3.32,3.05),(4.03,0,5.06),(-4.03,0,5.06)],52)
face([(-4.03,0,5.06),(4.03,0,5.06),(4.03,-3.32,3.05),(-4.03,-3.32,3.05)],52)
for y in [-3.32,3.32]:box(0,y,3.04,8.06,.12,.15,52)
for x in [-4.03,4.03]:
    beam((x,-3.32,3.12),(x,0,5.13),.14,52);beam((x,0,5.13),(x,3.32,3.12),.14,52)
# Ridge capped with a thin repair strip; same atlas at exact metre density.
beam((-4.08,0,5.17),(4.08,0,5.17),.16,53)
# One local patch, along a previous leak close to the chimney. It is a flush
# sheet, not a repeated damage stamp over the entire roof.
part='local_roof_repair'
roofz=lambda y:3.16+(y+3.32)*(2.01/3.32)+.018
face([(-2.95,-2.45,roofz(-2.45)),(-1.85,-2.45,roofz(-2.45)),(-1.85,-1.58,roofz(-1.58)),(-2.95,-1.58,roofz(-1.58))],53,offset=(2,2))
part='curved_tile_eave_ends'
for k in range(25):
    cx=-3.84+k*.32
    profile=[(-.14,0),(-.07,.043),(.07,.043),(.14,0)]
    front=[(cx+dx,-3.36,roofz(-3.36)+dz) for dx,dz in profile]
    back=[(cx+dx,-3.02,roofz(-3.02)+dz) for dx,dz in profile]
    for j in range(3):face([front[j],front[j+1],back[j+1],back[j]],53)
    face(front[::-1],53)
part='exposed_rafter_ends'
for x in [-3.5,-2.5,-1.5,-.5,.5,1.5,2.5,3.5]:
    box(x,-3.12,2.99,.12,.5,.14,52)
part='chimney'
box(-2.68,.4,4.75,.56,.66,1.35,57)
box(-2.68,.4,6.1,.74,.84,.12,53)
box(-2.68,.4,6.22,.36,.4,.18,53)

part='door'
box(.4,-2.945,.28,.92,.12,2.08,52,offset=(8,2))
for x in [-.13,.93]:box(x,-3.035,.23,.14,.18,2.27,52,offset=(40,2))
box(.4,-3.035,2.43,1.2,.2,.16,52)
box(.4,-3.05,.22,1.13,.22,.12,57)
# Small recessed plank edges, two rails and worn handle (no oversized outlines).
for x in [.10,.4,.7]:box(x,-3.01,.35,.018,.014,1.9,52)
for z in [.75,1.61]:box(.4,-3.015,z,.81,.035,.06,52)
box(.73,-3.07,1.18,.04,.06,.18,53)
part='front_steps'
box(.4,-3.47,0,1.46,.76,.11,54)
box(.4,-3.25,.11,1.3,.44,.12,57)

def window(x,y,z,w=1.05,h=1.25,lit=False,side=False):
    startv=len(V)
    partname='lit_window' if lit else 'window'
    global part;old=part;part=partname
    # glass is a deliberately small pale-photo crop; only one pane emits.
    box(x,y,z,w,.08,h,58,1 if lit else 2,offset=(2,2))
    for dx in [-w/2-.055,w/2+.055]:box(x+dx,y-.05,z-.04,.11,.14,h+.14,52)
    for zz in [z-.04,z+h+.02]:box(x,y-.05,zz,w+.2,.16,.1,52)
    box(x,y-.11,z,.055,.07,h,52);box(x,y-.115,z+h*.5,w,.07,.05,52)
    box(x,y-.13,z-.12,w+.38,.33,.12,57)
    # One opened shutter makes occupancy readable at close range.
    if not lit:
        box(x-w/2-.3,y+.04,z,.38,.07,h,52)
    if side:
        for i in range(startv,len(V)):
            px,py,pz=V[i];V[i]=(-py,px,pz)
    part=old
window(-2,-2.985,1.12,lit=False)
window(2.22,-2.985,1.12,lit=True)
window(.7,-3.68,1.12,w=.9,h=1.15,side=True)
# Back window, a restrained plain frame (wall is present behind opaque pane).
part='back_window';box(1.2,2.94,1.2,1.05,.10,1.15,58,2)
for x in [.62,1.78]:box(x,3.02,1.15,.1,.12,1.3,52)
for z in [1.15,2.35]:box(1.2,3.02,z,1.22,.12,.1,52)
box(1.2,3.03,1.2,.06,.12,1.15,52)

# Doorstep chair, human-scale, with a single repaired rear rail.
part='chair';cx=-.9;cy=-3.55
for dx in [-.2,.2]:
    for dy in [-.2,.2]:box(cx+dx,cy+dy,.01,.055,.055,.46 if dy<0 else .95,52)
box(cx,cy,.44,.48,.49,.055,52)
for z in [.64,.84]:box(cx,cy+.2,z,.48,.055,.1,52)
box(cx,cy+.17,.79,.28,.035,.06,53)
for x in [-.2,.2]:box(cx+x,cy,.2,.04,.43,.045,52)

# Terracotta pots and sparse faceted leaves (photographic atlas, no AI pixels).
def plant(x,y,scale=1):
    global part;part='doorstep_plant'
    cylinder(x,y,0,.15*scale,.33*scale,53,8,.21*scale)
    cylinder(x,y,.31*scale,.22*scale,.06*scale,53,8)
    cylinder(x,y,.37*scale,.025*scale,.45*scale,52,5)
    for k in range(7):
        a=k*2.399;z=(.48+(k%3)*.12)*scale
        tip=(x+math.cos(a)*.29*scale,y+math.sin(a)*.29*scale,z+.08*scale)
        mid=Vector((x,y,z));p=Vector(tip);side=Vector((-math.sin(a),math.cos(a),0))*.095*scale
        # two triangular lobes, not billboarded cards
        face([mid,mid.lerp(p,.6)+side,p],52,3)
        face([mid,p,mid.lerp(p,.6)-side],52,3)
plant(1.35,-3.38);plant(-2.85,-3.31,.8)

# Laundry suspended in a shallow sag; folds provide static wind direction.
part='laundry_posts'
for x in [4.5,7.2]:beam((x,-.4,0),(x,-.4,2.05),.07,52)
part='laundry_line'
for a,b in [((4.5,-.4,2.02),(5.85,-.4,1.91)),((5.85,-.4,1.91),(7.2,-.4,2.02))]:beam(a,b,.012,52)
part='laundry'
for x,w,h in [(4.72,.62,.8),(5.5,.8,1.1),(6.48,.48,.7)]:
    z=1.94
    # four planar panels with their own exact face metric, no stretched fold UV.
    for k in range(4):
        xx=x+w*k/4;xn=x+w*(k+1)/4
        yy=-.4+(.055 if k%2 else -.055);yn=-.4+(.055 if (k+1)%2 else -.055)
        face([(xx,yy,z),(xx,yy-.17,z-h),(xn,yn-.17,z-h),(xn,yn,z)],58)
    for xx in [x+.04,x+w-.04]:box(xx,-.42,z-.03,.025,.035,.08,52)

mesh=bpy.data.meshes.new('hill_house_geometry');mesh.from_pydata(V,[],F);mesh.update()
asset=bpy.data.objects.new('hill_house_v01',mesh);sc.collection.objects.link(asset)
for m in MATS:mesh.materials.append(m)
uvlayer=mesh.uv_layers.new(name='UVMap')
for poly,coords,mat in zip(mesh.polygons,UV,MI):
    poly.material_index=mat;poly.use_smooth=False
    for loop,co in zip(poly.loop_indices,coords):uvlayer.data[loop].uv=co
bpy.context.view_layer.objects.active=asset;asset.select_set(True)
tri=asset.modifiers.new('export_triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
asset['density_px_m']=32;asset['units']='metres';asset['pivot']='building footprint bottom centre; laundry asymmetric'
asset['source']='CC0 photos + deterministic geometry; concept excluded'
asset['wear']='entry handling; low plaster repaint; roof patch; chair repair'
bpy.context.view_layer.update();tris=len(asset.data.polygons)
assert tris<=2500,tris
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'hill_house_v01.blend'))
bpy.ops.export_scene.gltf(filepath=str(HERE/'hill_house_v01.glb'),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_extras=True)
# FBX only understands a direct image link plus the socket's diffuse factor.
# Flatten the supported material description after saving the authoritative
# Blender/GLB versions; this does not rebake or recolor the palette texture.
for m,tint in [(MATS[2],(.12,.19,.21,1)),(MATS[3],(.42,.6,.20,1))]:
    bs=m.node_tree.nodes.get('Principled BSDF');tex=next(n for n in m.node_tree.nodes if n.type=='TEX_IMAGE')
    m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Base Color'].default_value=tint
bpy.ops.export_scene.fbx(filepath=str(HERE/'hill_house_v01.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,mesh_smooth_type='OFF',path_mode='COPY',embed_textures=True,add_leaf_bones=False)
(HERE/'geometry.json').write_text(json.dumps({'tris':tris,'dimensions_xyz_m':list(asset.dimensions),'body_dimensions_m':[7.2,5.8,3.3],'ridge_height_m':5.25,'chimney_height_m':6.4,'door_m':[.92,2.08],'window_m':[1.05,1.25],'chair_seat_m':[.48,.49,.44],'density_px_m':32,'atlas_grid':[512,64,8,2],'tile_ids':[51,52,53,54,57,58],'ai_texture':False,'part_polygon_counts':{k:PART.count(k) for k in sorted(set(PART))},'dimensions_basis':'Authored metre-scale dimensions using human-scale doors/windows. Not a surveyed replica; HABS vernacular roof/eaves/repair patterns only.'},indent=2),encoding='utf-8')
print('HOUSE EXPORT',tris,'tris',list(asset.dimensions),flush=True)
