import bpy,math,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/kart-v6d';E=R/'art/export/kart-v6d';V=R/'art/_review/2026-10-27-kart-v6d'
bpy.ops.wm.open_mainfile(filepath=str(W/'modified.blend'));sc=bpy.context.scene;kart=[o for o in sc.objects if o.type=='MESH']
def material(path,name):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path),check_existing=True);t.interpolation='Closest';e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(t.outputs[0],e.inputs[0]);m.node_tree.links.new(e.outputs[0],out.inputs[0]);return m
def assign(obs,m):
 for o in obs:
  o.data.materials.clear();o.data.materials.append(m)
  for f in o.data.polygons:f.material_index=0
def load(path):
 before=set(sc.objects);bpy.ops.import_scene.fbx(filepath=str(path));return [o for o in sc.objects if o not in before and o.type=='MESH']
def snap(obs):
 d={}
 for o in obs:
  me=o.data;me.calc_loop_triangles();d[o.name]=dict(vertices=[list(v.co) for v in me.vertices],uv=[list(v.uv) for v in me.uv_layers.active.data],polygons=[list(f.vertices) for f in me.polygons],matrix=[list(r) for r in o.matrix_world],tris=len(me.loop_triangles))
 return d
m=material(E/'kart_v6d.png','kart_v6d_unlit');assign(kart,m)
n=m.node_tree.nodes;out=next(x for x in n if x.type=='OUTPUT_MATERIAL');tex=next(x for x in n if x.type=='TEX_IMAGE');bs=n.new('ShaderNodeBsdfPrincipled');m.node_tree.links.new(tex.outputs[0],bs.inputs['Base Color']);m.node_tree.links.new(bs.outputs[0],out.inputs[0])
bpy.ops.object.select_all(action='DESELECT')
for o in kart:o.select_set(True)
bpy.context.view_layer.objects.active=kart[0];bpy.ops.export_scene.fbx(filepath=str(E/'kart_v6d.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False)
for o in list(sc.objects):bpy.data.objects.remove(o,do_unlink=True)
kart=load(E/'kart_v6d.fbx');after=snap(kart);original=json.loads((W/'before.json').read_text());modified=json.loads((W/'modified.json').read_text());err=0;unchanged=[]
def compare(a,b):
 assert a['polygons']==b['polygons'];mx=0
 for f in ['vertices','uv','matrix']:
  assert len(a[f])==len(b[f]);mx=max(mx,max(abs(p-q) for x,y in zip(a[f],b[f]) for p,q in zip(x,y)))
 return mx
for name in modified:err=max(err,compare(modified[name],after[name]))
for name in original:
 if name!='seat_back':assert compare(original[name],after[name])<1e-6;unchanged.append(name)
assert err<1e-6
pts=[o.matrix_world@v.co for o in kart for v in o.data.vertices];lo=[min(p[i] for p in pts) for i in range(3)];hi=[max(p[i] for p in pts) for i in range(3)];dims=[b-a for a,b in zip(lo,hi)];tris=sum(a['tris'] for a in after.values());assert tris<=2000
assert max(abs(a-b) for a,b in zip(dims,[1.28,1.80,.82]))<1e-5
wheel={o.name:list(o.matrix_world.translation) for o in kart if o.name.startswith(('k_tire','k_hub'))};assert len(wheel)==8
validation=dict(tris=tris,objects=len(kart),dimensions=dims,bounds=[lo,hi],unchanged_original_objects=unchanged,changed_original_objects=['seat_back'],new_objects=sorted(set(after)-set(original)),max_export_error=err,wheel_pivots=wheel,seat_back_top_before=.912,seat_back_top_after=.6672,seat_back_rise_before=.612,seat_back_rise_after=.3672,cargo_size=[.40,.235,.4035],unity_executed=False)
(E/'geometry-validation.json').write_text(json.dumps(validation,indent=2));assign(kart,material(E/'kart_v6d.png','v6d_preview'))
for o in kart:o.name='v6d_'+o.name
old=load(R/'art/export/kart-v6c/kart_v6c.fbx');assign(old,material(R/'art/export/kart-v6c/kart_v6c.png','v6c_preview'))
sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.use_denoising=False;sc.render.dither_intensity=0;sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0
sc.world=bpy.data.worlds.new('World');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.5,.58,.58,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.8
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.render.film_transparent=True;bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam
def shot(name,pos,target,ortho=None,res=(900,720)):
 cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x,sc.render.resolution_y=res;sc.render.resolution_percentage=100;cam.data.type='ORTHO' if ortho else 'PERSP';cam.data.sensor_fit='HORIZONTAL' if ortho else 'VERTICAL'
 if ortho:cam.data.ortho_scale=ortho
 else:cam.data.angle=math.radians(64)
 sc.render.filepath=str(V/(name+'.png'));bpy.ops.render.render(write_still=True)
def show(which):
 for o in kart:o.hide_render=which!='v6d'
 for o in old:o.hide_render=which!='v6c'
for tag in ['v6c','v6d']:
 show(tag);shot(tag+'_rear',(0,-4,1),(0,-.15,.46),2.15);shot(tag+'_side',(-4,.09,.65),(0,.09,.5),2.3);shot(tag+'_close',(-2.1,-2.6,1.7),(0,-.15,.43),2.5)
for stem,texname in [('garage_v5_combo','garage_baked32'),('dirt_v5_combo','dirt_baked32')]:
 env=load(R/'art/export/scene-a-v5b-garage'/(stem+'.fbx'));assign(env,material(R/'art/export/scene-a-v5b-garage'/(texname+'.png'),stem))
sc.render.film_transparent=False
for o in kart+old:o.location.y-=3
for tag in ['v6c','v6d']:show(tag);shot(tag+'_camera_B',(0,-8.6,2.8),(0,3,1),None,(1280,720))
bpy.ops.wm.save_as_mainfile(filepath=str(W/'preview.blend'));print('VERIFIED',json.dumps(validation),flush=True)

