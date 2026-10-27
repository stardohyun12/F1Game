"""Five shared texture slots only. No newly painted per-building texture."""
from pathlib import Path
import bpy,math,json
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/kit-shared-v1';E=R/'art/export/kit-shared-v1';V=R/'art/_review/2026-10-28-kit-shared-v1'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
layout=json.loads((E/'layout.json').read_text());mats={};slots={};uvstats=[]
for slot in ['wall','trim','floor','roof','decals']:
 m=bpy.data.materials.new('Shared_'+slot);m.use_nodes=True;n=m.node_tree.nodes;b=n.get('Principled BSDF');b.inputs['Roughness'].default_value=.9
 t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(E/(slot+'.png')));t.interpolation='Closest';m.node_tree.links.new(t.outputs['Color'],b.inputs['Base Color'])
 if slot=='decals':m.node_tree.links.new(t.outputs['Alpha'],b.inputs['Alpha']);m.surface_render_method='DITHERED'
 mats[slot]=m
 for c in layout[slot]:slots[c['name']]=(slot,c['rect_px'])

def face(name,pts,cell,uv_m=None):
 slot,rect=slots[cell];x,y,w,h=rect
 if uv_m is None:
  a,b,c,d=[Vector(p) for p in pts];uu=(b-a).length;vv=(d-a).length;uv_m=[(0,0),(uu,0),(uu,vv),(0,vv)]
 if slot=='decals':
  uv=[((x+.5+u*(w-1))/512,1-(y+h-.5-v*(h-1))/512) for u,v in uv_m]
 else:
  assert max(u for u,v in uv_m)*32<w+.01 and max(v for u,v in uv_m)*32<h+.01,(name,cell,uv_m)
  uv=[((x+.01+u*32)/512,1-(y+h-.01-v*32)/512) for u,v in uv_m]
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(pts,[],[(0,1,2,3)]);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);mesh.materials.append(mats[slot]);layer=mesh.uv_layers.new(name='UVMap')
 for i,v in enumerate(uv):layer.data[i].uv=v
 if slot!='decals':
  edges=[]
  for i,j in [(0,1),(0,3)]:
   length=(Vector(pts[j])-Vector(pts[i])).length
   if length:edges.append((Vector(uv[j])-Vector(uv[i])).length*512/length)
  uvstats.append(dict(object=name,cell=cell,texel_per_m=edges))
 return o

def box(name,center,size,cell):
 x,y,z=center;a,b,c=[v/2 for v in size];lo=[x-a,y-b,z-c];hi=[x+a,y+b,z+c]
 faces=[[(lo[0],lo[1],lo[2]),(hi[0],lo[1],lo[2]),(hi[0],lo[1],hi[2]),(lo[0],lo[1],hi[2])],
 [(hi[0],hi[1],lo[2]),(lo[0],hi[1],lo[2]),(lo[0],hi[1],hi[2]),(hi[0],hi[1],hi[2])],
 [(hi[0],lo[1],lo[2]),(hi[0],hi[1],lo[2]),(hi[0],hi[1],hi[2]),(hi[0],lo[1],hi[2])],
 [(lo[0],hi[1],lo[2]),(lo[0],lo[1],lo[2]),(lo[0],lo[1],hi[2]),(lo[0],hi[1],hi[2])],
 [(lo[0],lo[1],hi[2]),(hi[0],lo[1],hi[2]),(hi[0],hi[1],hi[2]),(lo[0],hi[1],hi[2])],
 [(lo[0],hi[1],lo[2]),(hi[0],hi[1],lo[2]),(hi[0],lo[1],lo[2]),(lo[0],lo[1],lo[2])]]
 out=[]
 for i,p in enumerate(faces):
  # Long frame axes map along U; do not stretch a 16px band over 7 metres.
  a,b,c,d=[Vector(v) for v in p];uu=(b-a).length;vv=(d-a).length;_,r=slots[cell]
  if vv*32>r[3] and uu*32<=r[3]:p=[p[1],p[2],p[3],p[0]]
  out.append(face(name+'_'+str(i),p,cell))
 return out

# Box envelope 6x5x7m, including roof/eaves. Wall body 5.6x4.6x6.65.
box('wall',(0,0,3.325),(5.6,4.6,6.65),'wall_cream')
face('roof_front',[(-3,-2.5,6.68),(3,-2.5,6.68),(3,0,7),(-3,0,7)],'roof_E2')
face('roof_back',[(3,2.5,6.68),(-3,2.5,6.68),(-3,0,7),(3,0,7)],'roof_E2')
for x in [-2.90,2.90]:box('side_eaves',(x,0,6.61),(.12,5,.12),'eaves')
for y in [-2.4,2.4]:box('front_eaves',(0,y,6.57),(6,.12,.20),'eaves')
box('storey_band',(0,-2.325,3.4),(5.6,.06,.12),'window_frame')
box('skirting',(0,-2.325,.18),(5.6,.06,.32),'skirting')

def window(idx,x,z):
 y=-2.34;ww=1.05;hh=1.50
 face('glass_'+str(idx),[(x-ww/2,y,z-hh/2),(x+ww/2,y,z-hh/2),(x+ww/2,y,z+hh/2),(x-ww/2,y,z+hh/2)],'glass')
 for xx in [x-ww/2,x+ww/2]:box('window_jamb',(xx,y-.06,z),(.10,.10,hh+.16),'window_frame')
 for zz in [z-hh/2,z+hh/2]:box('window_sill',(x,y-.07,zz),(ww+.2,.16,.10),'window_frame')
 box('window_mullion',(x,y-.08,z),(.06,.06,hh),'window_frame');box('window_cross',(x,y-.08,z-.10),(ww,.06,.06),'window_frame')
for i,(x,z) in enumerate([(-1.65,2.0),(1.65,2.0),(-1.4,4.9),(1.4,4.9)]):window(i,x,z)
box('door',(0,-2.34,1.15),(.92,.10,2.3),'wood_door')
for x in [-.51,.51]:box('door_jamb',(x,-2.42,1.18),(.12,.15,2.36),'door_frame')
box('door_header',(0,-2.42,2.39),(1.14,.15,.12),'door_frame');box('door_handle',(.32,-2.45,1.1),(.06,.06,.16),'gutter')
box('gutter',(2.70,-2.38,3.4),(.12,.12,6.3),'gutter')
box('sign_board',(0,-2.40,3.00),(2.12,.16,.85),'wood_door')
face('sign',[(-1,-2.49,2.625),(1,-2.49,2.625),(1,-2.49,3.375),(-1,-2.49,3.375)],'sample_tea',[(0,0),(1,0),(1,1),(0,1)])
face('poster',[(2.81,-.8,1),(2.81,-.05,1),(2.81,-.05,2),(2.81,-.8,2)],'poster_harbor',[(0,0),(1,0),(1,1),(0,1)])
face('number',[(.65,-2.42,1.7),(1.15,-2.42,1.7),(1.15,-2.42,2.0125),(.65,-2.42,2.0125)],'number_07',[(0,0),(1,0),(1,1),(0,1)])
face('wear_corner',[(-2.8,-2.351,.25),(-1.6,-2.351,.25),(-1.6,-2.351,1.45),(-2.8,-2.351,1.45)],'edge_chip',[(0,0),(1,0),(1,1),(0,1)])
face('wear_eave',[(-2.8,-2.351,5.65),(-.8,-2.351,5.65),(-.8,-2.351,6.65),(-2.8,-2.351,6.65)],'edge_chip',[(0,0),(1,0),(1,1),(0,1)])
house=[o for o in bpy.context.scene.objects if o.type=='MESH'];tris=sum(len(o.data.polygons)*2 for o in house)
bpy.ops.wm.save_as_mainfile(filepath=str(W/'proof.blend'))
# Export proof only to source folder, not production asset output.
bpy.ops.object.select_all(action='DESELECT')
for o in house:o.select_set(True)
bpy.context.view_layer.objects.active=house[0]
bpy.ops.export_scene.fbx(filepath=str(W/'proof.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',path_mode='COPY',bake_anim=False)

# Ground is assembled from the same kit, not an external photographic backdrop.
for x in range(-16,16,8):
 for y in range(-16,16,8):face('ground',[(x,y,-.02),(x+8,y,-.02),(x+8,y+8,-.02),(x,y+8,-.02)],'floor_stone')
sc=bpy.context.scene
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=1.6;sun.data.color=(1,.94,.85);sun.data.angle=.15;sun.rotation_euler=Vector((.3,.5,-.8)).to_track_quat('-Z','Y').to_euler()
sc.world=bpy.data.worlds.new('sky');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.48,.59,.64,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.85
sc.render.engine='BLENDER_EEVEE';sc.render.resolution_x=1600;sc.render.resolution_y=900;sc.render.resolution_percentage=100;sc.render.dither_intensity=0
sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0
# Same previously documented unoccluded sky approximation; render only, not saved/exported.
for m in mats.values():
 n=m.node_tree.nodes;bs=n.get('Principled BSDF');out=n.get('Material Output');em=n.new('ShaderNodeEmission');mix=n.new('ShaderNodeMixShader');mix.inputs[0].default_value=.60
 m.node_tree.links.new(bs.inputs['Base Color'].links[0].from_socket,em.inputs[0]);m.node_tree.links.new(bs.outputs[0],mix.inputs[1]);m.node_tree.links.new(em.outputs[0],mix.inputs[2])
 if m.name=='Shared_decals':
  trans=n.new('ShaderNodeBsdfTransparent');alpha=n.new('ShaderNodeMixShader');tex=next(v for v in n if v.type=='TEX_IMAGE');m.node_tree.links.new(tex.outputs['Alpha'],alpha.inputs[0]);m.node_tree.links.new(trans.outputs[0],alpha.inputs[1]);m.node_tree.links.new(mix.outputs[0],alpha.inputs[2]);m.node_tree.links.new(alpha.outputs[0],out.inputs[0])
 else:m.node_tree.links.new(mix.outputs[0],out.inputs[0])
bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.type='PERSP';cam.data.sensor_fit='VERTICAL';cam.data.sensor_height=32;cam.data.lens=32/(2*math.tan(math.radians(64)/2))
views={'assembly':((10,-17,8),(0,0,3.3)),'front':((0,-15,3.5),(0,0,3.5)), 'close':((5,-10,4.0),(0,0,3.1))}
for name,(loc,target) in views.items():
 cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(V/(name+'.png'));bpy.ops.render.render(write_still=True)
points=[o.matrix_world@v.co for o in house for v in o.data.vertices];bounds=[[min(p[i] for p in points) for i in range(3)],[max(p[i] for p in points) for i in range(3)]]
validation={'house_dimensions_m':[bounds[1][i]-bounds[0][i] for i in range(3)],'bounds':bounds,'windows':4,'doors':1,'tris':tris,'material_slots':list(mats),'texel_density_edges':uvstats,'no_light_bake':True,'source_only_proof_fbx':str(W/'proof.fbx'),'render_only_ambient_mix':.60,'views':views}
(W/'proof-validation.json').write_text(json.dumps(validation,indent=2));print('SHARED KIT PROOF DONE',tris)
