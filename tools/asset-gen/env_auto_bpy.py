"""Geometry and reference studio for env_auto.py; Blender 5.2, no Unity writes."""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.geometry import tessellate_polygon

R=Path(__file__).resolve().parents[2];E=R/'art/export/env-first';V=R/'art/_review/2026-10-30-env-first'
K=R/'art/export/kit-shared-v2';S=R/'art/models/env_first';S.mkdir(parents=True,exist_ok=True)
manifest=json.loads((E/'manifest.json').read_text(encoding='utf-8'))
layout=json.loads((K/'layout.json').read_text(encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True)
mats={};cells={};uvlog=[];checks=[];exports=[]
for slot in ['wall','trim','roof','floor']:
    mat=bpy.data.materials.new('Shared_'+slot);mat.use_nodes=True
    ns=mat.node_tree.nodes;bs=ns.get('Principled BSDF');bs.inputs['Roughness'].default_value=.95
    tx=ns.new('ShaderNodeTexImage');tx.image=bpy.data.images.load(str(K/(slot+'.png')),check_existing=True);tx.interpolation='Closest'
    mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color']);mats[slot]=mat
    for c in layout[slot]:cells[c['name']]=(slot,c['rect_px'])

def clip(poly,axis,bound,positive):
    out=[]
    for a,b in zip(poly,poly[1:]+poly[:1]):
        ia=(a[axis]>=bound-1e-9) if positive else (a[axis]<=bound+1e-9)
        ib=(b[axis]>=bound-1e-9) if positive else (b[axis]<=bound+1e-9)
        if ia:out.append(a)
        if ia!=ib:
            t=(bound-a[axis])/(b[axis]-a[axis]);out.append((a[0]+t*(b[0]-a[0]),a[1]+t*(b[1]-a[1])))
    return out

class Mesh:
    def __init__(self,name):self.name=name;self.v=[];self.f=[];self.uv=[];self.mi=[]
    def face(self,pts,cell):
        pts=[Vector(p) for p in pts]
        pts=[p for i,p in enumerate(pts) if (p-pts[i-1]).length>1e-7]
        if len(pts)<3:return
        u=pts[1]-pts[0]
        if u.length<1e-7:return
        u.normalize();normal=None
        for p in pts[2:]:
            n=u.cross(p-pts[0])
            if n.length>1e-7:normal=n.normalized();break
        if normal is None:return
        vv=normal.cross(u);p0=pts[0];poly=[((p-p0).dot(u),(p-p0).dot(vv)) for p in pts]
        slot,(sx,sy,sw,sh)=cells[cell]
        # Rotate narrow trim faces so their long axis follows U, never stretch the band.
        if max(p[1] for p in poly)-min(p[1] for p in poly)>sh/32 and max(p[0] for p in poly)-min(p[0] for p in poly)<=sh/32:
            u,vv=vv,-u;poly=[((p-p0).dot(u),(p-p0).dot(vv)) for p in pts]
        minx=min(p[0] for p in poly);miny=min(p[1] for p in poly)
        maxx=max(p[0] for p in poly);maxy=max(p[1] for p in poly)
        tw=(sw-1)/32;th=(sh-1)/32
        for ix in range(max(1,math.ceil((maxx-minx)/tw-1e-8))):
            for iy in range(max(1,math.ceil((maxy-miny)/th-1e-8))):
                x0=minx+ix*tw;y0=miny+iy*th
                pp=poly
                for ax,b,pos in [(0,x0,True),(0,x0+tw,False),(1,y0,True),(1,y0+th,False)]:pp=clip(pp,ax,b,pos)
                if len(pp)<3:continue
                area=abs(sum(a[0]*b[1]-a[1]*b[0] for a,b in zip(pp,pp[1:]+pp[:1])))/2
                if area<1e-8:continue
                idx=len(self.v);self.v += [tuple(p0+u*x+vv*y) for x,y in pp]
                self.f.append(tuple(range(idx,idx+len(pp))));self.mi.append(list(mats).index(slot))
                self.uv.append([((sx+.5+(x-x0)*32)/512,1-(sy+sh-.5-(y-y0)*32)/512) for x,y in pp])
                uvlog.append({'object':self.name,'cell':cell,'area':area,'ppm':32.0})
    def obj(self,parent=None):
        if not self.f:return None
        me=bpy.data.meshes.new(self.name);me.from_pydata(self.v,[],self.f);me.update()
        for mat in mats.values():me.materials.append(mat)
        uv=me.uv_layers.new(name='UVMap')
        for p,coords,mi in zip(me.polygons,self.uv,self.mi):
            p.material_index=mi;p.use_smooth=False
            for li,co in zip(p.loop_indices,coords):uv.data[li].uv=co
        o=bpy.data.objects.new(self.name,me);bpy.context.collection.objects.link(o)
        if parent:o.parent=parent
        return o

def box(m,center,size,cell):
    x,y,z=center;a,b,c=[s/2 for s in size]
    x0,x1=x-a,x+a;y0,y1=y-b,y+b;z0,z1=z-c,z+c
    for ps in [((x0,y0,z0),(x1,y0,z0),(x1,y0,z1),(x0,y0,z1)),
               ((x1,y1,z0),(x0,y1,z0),(x0,y1,z1),(x1,y1,z1)),
               ((x1,y0,z0),(x1,y1,z0),(x1,y1,z1),(x1,y0,z1)),
               ((x0,y1,z0),(x0,y0,z0),(x0,y0,z1),(x0,y1,z1)),
               ((x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)),
               ((x0,y1,z0),(x1,y1,z0),(x1,y0,z0),(x0,y0,z0))]:m.face(ps,cell)

def oriented_box(m,a,t,n,s,z,w,h,depth,cell,offset=.04):
    # Local box X=facade axis, Y=outward normal, Z=height, transformed to world.
    tmp=Mesh('temporary');box(tmp,(s,offset,z),(w,depth,h),cell)
    base=len(m.v);m.v += [tuple(a+t*x+n*y+Vector((0,0,zz))) for x,y,zz in tmp.v]
    m.f += [tuple(base+j for j in p) for p in tmp.f];m.uv+=tmp.uv;m.mi+=tmp.mi

def cap(m,pts,z,cell):
    ring=[Vector((p.x,p.y,z if not callable(z) else z(p))) for p in pts]
    # mathutils tessellates concave footprints; no convex hull replacement.
    for tri in tessellate_polygon([ring]):
        if isinstance(tri[0], int):tri=tuple(ring[i] for i in tri)
        a,b,c=tri
        if (b-a).cross(c-a).z<0:tri=(c,b,a)
        m.face(tri,cell)

def make_building(b):
    name=b['name'];px,pz=b['pivot_xz'];pts=[Vector((x-px,-(z-pz),0)) for x,z in b['poly']]
    root=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(root)
    parts={s:Mesh(name+'_'+s) for s in ['BODY','ROOF','TRIM','GLASS','DOOR_SLOT','SIGN_SLOT']}
    body,roof,trim,glass,door,sign=parts.values();fh=2.95;H=b['model_floors']*fh
    shared={s['edge'] for s in b['shared_walls']};openings=[];boundary=[]
    for ei,(a,bb) in enumerate(zip(pts,pts[1:]+pts[:1])):
        t=bb-a;length=t.length;t.normalize();n=Vector((-t.y,t.x,0))
        # Source ring becomes clockwise in Blender (x,-z); this order faces outward.
        body.face([a,a+Vector((0,0,H)),bb+Vector((0,0,H)),bb],b['wall_cell'])
        def ob(m,s,z,w,h,d,cell,offset=.04):oriented_box(m,a,t,n,s,z,w,h,d,cell,offset)
        ob(trim,length/2,.17,length,.32,.08,'skirting')
        ob(trim,length/2,H-.13,length,.22,.14,'eaves',.07)
        for fl in range(1,b['model_floors']):ob(trim,length/2,fl*fh-.08,length,.10,.06,'window_frame',.04)
        if b['roof_mode']=='flat_parapet':roof.face([a+Vector((0,0,H)),a+Vector((0,0,H+.44)),bb+Vector((0,0,H+.44)),bb+Vector((0,0,H))],b['wall_cell'])
        if ei in shared:continue
        # Stable floor-aligned bays, no windows on party walls or edges shorter than a bay.
        bay=3.65 if b['type']=='apartment' else [2.9,3.15,3.45][b['number']%3]
        count=max(1,int(length/bay)) if length>=2.5 else 0
        if b['type']=='annex':count=1 if ei==b['front_edge'] and length>3.7 else 0
        ww=[.95,1.20,1.4][b['number']%3];wh=[1.40,1.55,1.25][b['number']%3]
        for col in range(count):
            s=length*(col+.5)/count
            for fl in range(b['model_floors']):
                if ei==b['front_edge'] and fl==0 and col==0:continue
                z=fh*fl+1.65
                if s-ww/2<.25 or s+ww/2>length-.25:continue
                o=.022;p=a+t*(s-ww/2)+n*o;v=t*ww;up=Vector((0,0,wh))
                p.z=z-wh/2;glass.face([p,p+up,p+up+v,p+v],'glass')
                for ss in [s-ww/2,s+ww/2]:ob(trim,ss,z,.075,wh+.12,.10,'window_frame',.07)
                for zz in [z-wh/2,z+wh/2]:ob(trim,s,zz,ww+.15,.075,.12,'window_frame',.075)
                ob(trim,s,z,.045,wh,.06,'window_frame',.075)
                if b['number']%3!=1:ob(trim,s,z-.12,ww,.04,.05,'window_frame',.07)
                # Two buildings use shutters, two use deep sills; keep a clear family without eight clones.
                if b['number'] in (2,6) and ei==b['front_edge']:
                    for ss in (s-ww/2-.22,s+ww/2+.22):ob(trim,ss,z,.32,wh,.075,'shutter',.06)
                if b['number'] in (1,8):ob(trim,s,z-wh/2-.08,ww+.32,.12,.28,'window_frame',.12)
                openings.append(dict(edge=ei,floor=fl,s=s,width=ww,bottom=z-wh/2,top=z+wh/2,edge_length=length))
        if ei==b['front_edge']:
            ds=min(max(.65,length/(max(1,count)*2)),length-.65)
            ob(door,ds,1.1,1.02,2.2,.035,'wood_door',.027)
            for ss in [ds-.55,ds+.55]:ob(trim,ss,1.14,.08,2.28,.10,'door_frame',.065)
            ob(trim,ds,2.28,1.18,.10,.10,'door_frame',.065)
            # Blank reserved sign, no fabricated names or text.
            if b['number'] in (4,5,6):ob(sign,ds,2.65,min(1.8,length-.5),.32,.08,'wood_door',.075)
            boundary.append(dict(kind='door_slot',edge=ei,s=ds,bottom=0,top=2.2,width=1.02))
    if b['roof_mode']=='gable':
        # Split the ORIGINAL ring along its long-axis ridge. Even near-collinear
        # OSM nodes survive, so a simplified quad cannot leave millimetre wedges.
        ei=max(range(len(pts)),key=lambda i:(pts[(i+1)%len(pts)]-pts[i]).length)
        u=(pts[(ei+1)%len(pts)]-pts[ei]).normalized();n=Vector((-u.y,u.x,0))
        coords=[(p.dot(u),p.dot(n)) for p in pts];mn=min(y for x,y in coords);mx=max(y for x,y in coords)
        mid=(mn+mx)/2;half=(mx-mn)/2;rise=min(1.9,half*.46)
        zfun=lambda p:H+rise*max(0,1-abs(p.dot(n)-mid)/half)
        for positive in (False,True):
            pp=[u*x+n*y for x,y in clip(coords,1,mid,positive)]
            cap(roof,pp,zfun,b['roof_cell'])
        for a,bb in zip(pts,pts[1:]+pts[:1]):
            # Split gable end at the ridge too, keeping wall top and roof edges identical.
            seq=[a]
            da=a.dot(n)-mid;db=bb.dot(n)-mid
            if da*db<0:seq.append(a+(bb-a)*(-da/(db-da)))
            seq.append(bb)
            for aa,zz in zip(seq,seq[1:]):
                if max(zfun(aa),zfun(zz))>H+1e-7:body.face([aa+Vector((0,0,H)),aa+Vector((0,0,zfun(aa))),zz+Vector((0,0,zfun(zz))),zz+Vector((0,0,H))],b['wall_cell'])
    elif b['roof_mode']=='shed':
        xx=[p.x for p in pts];lo=min(xx);span=max(xx)-lo
        zfun=lambda p:H+.15+.5*(p.x-lo)/max(span,.01)
        cap(roof,pts,zfun,b['roof_cell'])
        for a,bb in zip(pts,pts[1:]+pts[:1]):body.face([a+Vector((0,0,H)),a+Vector((0,0,zfun(a))),bb+Vector((0,0,zfun(bb))),bb+Vector((0,0,H))],b['wall_cell'])
    else:
        cap(roof,pts,H+.015,b['roof_cell'])
        for ring in b['parapet_inner']:
            pp=[Vector((x-px,-(z-pz),0)) for x,z in ring]
            for a,bb in zip(pp,pp[1:]+pp[:1]):roof.face([a+Vector((0,0,H)),bb+Vector((0,0,H)),bb+Vector((0,0,H+.44)),a+Vector((0,0,H+.44))],b['wall_cell'])
        for tri in b['parapet_caps']:
            cap(roof,[Vector((x-px,-(z-pz),0)) for x,z in tri],H+.44,b['wall_cell'])
    cap(body,pts,0,'floor_concrete')
    objs=[m.obj(root) for m in parts.values() if m.f]
    tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objs)
    bad=[w for w in openings if w['edge'] in shared or w['s']-w['width']/2<0 or w['s']+w['width']/2>w['edge_length'] or w['top']>H]
    assert not bad and tris<=b['budget_tris'],(name,bad,tris)
    allp=[v.co for o in objs for v in o.data.vertices]
    bounds=[[min(p[k] for p in allp) for k in range(3)],[max(p[k] for p in allp) for k in range(3)]]
    checks.append(dict(name=name,tris=tris,budget=b['budget_tris'],bounds_local_blender=bounds,windows=len(openings),openings=openings,slots=boundary,
                       windows_outside=0,windows_partywall=0,roof_footprint='original polygon; verify exported result with env_verify.py',bottom=min(p.z for p in allp),
                       ground_check='NOT RUN - current D-209 live terrain unavailable',materials=list(mats),roof_mode=b['roof_mode']))
    bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
    out=E/(name+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(out),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',path_mode='RELATIVE',bake_anim=False,add_leaf_bones=False)
    bpy.ops.export_scene.gltf(filepath=str(E/(name+'.glb')),export_format='GLB',use_selection=True,export_yup=True)
    # Review world = x east / y=-north / z altitude, origin at the home placement.
    root.location=(px-185,-(pz+61),b['y']-25.27)
    exports.append(root)
    return root

for b in manifest['buildings']:make_building(b)
(V/'geometry.json').write_text(json.dumps(dict(buildings=checks,total_tris=sum(c['tris'] for c in checks),uv_density='32 px/m analytic; separately checked from exported FBX',kit_unchanged=all(hashlib.sha256((K/n).read_bytes()).hexdigest()==h for n,h in manifest['kit_sha256'].items())),ensure_ascii=False,indent=2),encoding='utf-8')

# Import real library landmarks as references. Never regenerate or modify their source files.
refs={
 'house_home':R/'art/export/house-home/house_home.fbx',
 'garage':R/'art/export/q2-garage/garage_v5_combo.fbx',
 'depot':R/'art/export/q3-depot/q3_depot.fbx',
 'postoffice':R/'art/export/q4-postoffice/q4_postoffice.fbx'}
for key,path in refs.items():
    if not path.exists():continue
    before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(path))
    added=list(set(bpy.data.objects)-before)
    root=bpy.data.objects.new('REFERENCE_'+key,None);bpy.context.collection.objects.link(root)
    for o in added:
        if not o.parent:o.parent=root
    p=manifest['landmarks'][key];x,z=p['world']
    root.location=(x-185,-(z+61),p['y']-25.27);root.rotation_euler[2]=-math.radians(p['yaw'])
    # Resolve relative texture paths through the original export directory.
    for o in added:
        if o.type!='MESH':continue
        for m in o.data.materials:
            if not m or not m.use_nodes:continue
            if key=='house_home' and 'glass' not in m.name.lower():
                bs=m.node_tree.nodes.get('Principled BSDF')
                if bs and not bs.inputs['Base Color'].links:
                    tx=m.node_tree.nodes.new('ShaderNodeTexImage');tx.image=bpy.data.images.load(str(path.parent/'house_home.png'),check_existing=True);tx.interpolation='Closest'
                    m.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
            for node in m.node_tree.nodes:
                if node.type=='TEX_IMAGE' and node.image:
                    pp=path.parent/Path(node.image.filepath.replace('\\','/')).name
                    if pp.exists():node.image.filepath=str(pp);node.image.reload();node.interpolation='Closest'

# Each building has its own level display pad at SOURCE altitude. Not claimed as terrain.
ground=Mesh('STUDIO_source_height_pads')
for b in manifest['buildings']:
    pts=[Vector((x-185,-(z+61),0)) for x,z in b['poly']]
    cap(ground,pts,b['y']-25.27-.025,'floor_concrete')
for n,p in manifest['landmarks'].items():
    x,z=p['world'];size={'house_home':(12,10),'garage':(16,14),'depot':(16,12),'postoffice':(14,12)}[n]
    box(ground,(x-185,-(z+61),p['y']-25.27-.075),(size[0],size[1],.1),'floor_concrete')
ground.obj()

# Context-only terrain, with fixed-height pads. This is NOT the edited current map.
terrain=json.loads((E/'review-terrain.json').read_text(encoding='utf-8'))
land=Mesh('STUDIO_review_terrain_NOT_LIVE_MAP');nx=terrain['nx'];nz=terrain['nz']
vs=[Vector((x-185,-(z+61),h-25.27)) for x,z,h in terrain['vertices']]
for j in range(nz-1):
    for i in range(nx-1):
        a=j*nx+i;b=a+1;c=a+nx+1;d=a+nx
        land.face([vs[a],vs[d],vs[c]],'floor_concrete');land.face([vs[a],vs[c],vs[b]],'floor_concrete')
land_obj=land.obj()
for p in land_obj.data.polygons:p.use_smooth=True
# Neutral context material, only in the review scene. No new PNG, no asset repaint.
stage_mat=mats['floor'].copy();stage_mat.name='STUDIO_CONTEXT_ONLY'
ns=stage_mat.node_tree.nodes;bs=ns.get('Principled BSDF');tx=next(n for n in ns if n.type=='TEX_IMAGE')
mix=ns.new('ShaderNodeMixRGB');mix.blend_type='MIX';mix.inputs[0].default_value=.82;mix.inputs[2].default_value=(.46,.48,.41,1)
stage_mat.node_tree.links.new(tx.outputs['Color'],mix.inputs[1]);stage_mat.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
for i in range(len(land_obj.data.materials)):land_obj.data.materials[i]=stage_mat
roads=Mesh('STUDIO_source_roads')
for tri in terrain['road_triangles']:
    pp=[Vector((x-185,-(z+61),h-25.27)) for x,z,h in tri]
    if (pp[1]-pp[0]).cross(pp[2]-pp[0]).z<0:pp.reverse()
    roads.face(pp,'floor_asphalt')
roads.obj()

# Same review-context triangles for the authorized A7 operator; this file contains
# geometry only, never generated textures, and does not write a Unity project.
context=[]
for name in ['STUDIO_source_height_pads','STUDIO_review_terrain_NOT_LIVE_MAP','STUDIO_source_roads']:
    ob=bpy.data.objects[name];me=ob.data;me.calc_loop_triangles();verts=[];uvs=[];indices=[]
    for tri in me.loop_triangles:
        for li in tri.loops:
            p=ob.matrix_world@me.vertices[me.loops[li].vertex_index].co
            indices.append(len(verts));verts.append([round(float(c),6) for c in p]);uvs.append([round(float(c),8) for c in me.uv_layers.active.data[li].uv])
    context.append(dict(name=name,vertices=verts,uv=uvs,triangles=indices))
(E/'studio-context.json').write_text(json.dumps(dict(note='Review terrain only, not live D-209 map',meshes=context),separators=(',',':')),encoding='utf-8')

# Matte studio surface below all geometry, explicitly a review stage.
base=Mesh('STUDIO_floor');box(base,(50,3,-15),(230,170,.2),'floor_concrete');base.obj()
sc=bpy.context.scene;sc.render.engine='BLENDER_EEVEE';sc.render.resolution_x=1280;sc.render.resolution_y=720;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.dither_intensity=0;sc.render.film_transparent=False
sc.world=bpy.data.worlds.new('Review_sky');sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.43,.60,.67,1)
sc.world.node_tree.nodes['Background'].inputs[1].default_value=.8
sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=-.45
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.name='REVIEW_SUN_NOT_A7';sun.data.energy=2.;sun.data.color=(1.,.93,.82);sun.data.angle=.10
sun.rotation_euler=Vector((.3,.6,-.8)).to_track_quat('-Z','Y').to_euler()
# Low ambient contribution for shaded readability. Render-only; not exported as albedo.
for m in bpy.data.materials:
    if not m.use_nodes:continue
    ns=m.node_tree.nodes;bs=ns.get('Principled BSDF')
    if bs and bs.inputs['Base Color'].links:
        m.node_tree.links.new(bs.inputs['Base Color'].links[0].from_socket,bs.inputs['Emission Color'])
        bs.inputs['Emission Strength'].default_value=.22
bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='REVIEW_CAMERA';sc.camera=cam
cam.data.type='PERSP';cam.data.sensor_fit='VERTICAL';cam.data.sensor_height=32;cam.data.lens=32/(2*math.tan(math.radians(64)/2));cam.data.clip_end=1000
views={
 'cart_home':dict(location=(-8,19,2.8),target=(16,0,4.8),height_above_home_pad=2.8),
 'cart_route':dict(location=(67,-11,5.1),target=(88,-36,8.73),height_above_reference_ground=2.8),
 'top':dict(location=(42,5,180),target=(42,5,0),orthographic=157)}
for name,v in views.items():
    cam.location=v['location'];cam.rotation_euler=(Vector(v['target'])-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO' if 'orthographic' in v else 'PERSP'
    if 'orthographic' in v:cam.data.ortho_scale=v['orthographic']
    sc.render.filepath=str(V/(name+'.png'));bpy.ops.render.render(write_still=True)
(V/'cameras.json').write_text(json.dumps(dict(renderer='Blender EEVEE reference, NOT Unity A7',resolution=[1280,720],fov_vertical=64,origin_unity=[185,25.27,-61],views=views),indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(S/'env_first.blend'))
print('ENV_FIRST_DONE buildings=',len(checks),'tris=',sum(c['tris'] for c in checks))
