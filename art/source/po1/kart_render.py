import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');E=R/'art/export/kart-v6e';V=R/'art/_review/2026-10-27-po1'
bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene
def load(path):
 before=set(sc.objects);bpy.ops.import_scene.fbx(filepath=str(path));return [o for o in sc.objects if o not in before and o.type=='MESH']
def snap(obs):
 result={}
 for o in obs:
  me=o.data;me.calc_loop_triangles()
  result[o.name]=dict(vertices=[list(v.co) for v in me.vertices],polygons=[list(f.vertices) for f in me.polygons],uv=[list(x.uv) for x in me.uv_layers.active.data],matrix=[list(r) for r in o.matrix_world],tris=len(me.loop_triangles))
 return result
kart=load(R/'art/export/kart-v6d/kart_v6d.fbx');before=snap(kart)
def material(path,name):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path),check_existing=True);t.interpolation='Closest';e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(t.outputs[0],e.inputs[0]);m.node_tree.links.new(e.outputs[0],out.inputs[0]);return m
def assign(obs,m):
 for o in obs:
  o.data.materials.clear();o.data.materials.append(m)
  for f in o.data.polygons:f.material_index=0
m=material(E/'kart_v6e.png','kart_v6e_unlit');assign(kart,m)
n=m.node_tree.nodes;out=next(x for x in n if x.type=='OUTPUT_MATERIAL');tex=next(x for x in n if x.type=='TEX_IMAGE');em=next(x for x in n if x.type=='EMISSION');bs=n.new('ShaderNodeBsdfPrincipled');m.node_tree.links.new(tex.outputs[0],bs.inputs['Base Color']);m.node_tree.links.new(bs.outputs[0],out.inputs[0])
bpy.ops.object.select_all(action='DESELECT')
for o in kart:o.select_set(True)
bpy.context.view_layer.objects.active=kart[0]
bpy.ops.export_scene.fbx(filepath=str(E/'kart_v6e.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False)
# Validate in a fresh scene, against the same import representation of the original FBX.
for o in list(sc.objects):bpy.data.objects.remove(o,do_unlink=True)
kart=load(E/'kart_v6e.fbx');after=snap(kart)
assert set(before)==set(after)
maxerr=0
for name in before:
 a,b=before[name],after[name];assert a['polygons']==b['polygons'];assert a['tris']==b['tris']
 for field in ['vertices','uv','matrix']:
  assert len(a[field])==len(b[field])
  for x,y in zip(a[field],b[field]):
   maxerr=max(maxerr,max(abs(p-q) for p,q in zip(x,y)))
assert maxerr<1e-6,maxerr
points=[o.matrix_world@v.co for o in kart for v in o.data.vertices]
bounds=[[min(p[i] for p in points) for i in range(3)],[max(p[i] for p in points) for i in range(3)]]
validation=dict(objects=len(kart),tris=sum(v['tris'] for v in after.values()),max_coordinate_uv_matrix_error=maxerr,topology_identical=True,bounds=bounds,dimensions=[bounds[1][i]-bounds[0][i] for i in range(3)],wheel_pivots={o.name:list(o.matrix_world.translation) for o in kart if o.name.startswith(('k_tire','k_hub'))},unity_executed=False)
assert len(validation['wheel_pivots'])==8
(E/'geometry-validation.json').write_text(json.dumps(validation,indent=2))
m=material(E/'kart_v6e.png','v6b_preview');old=material(R/'art/export/kart-v6d/kart_v6d.png','v6_preview');assign(kart,m)
sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.use_denoising=False;sc.render.dither_intensity=0;sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0
sc.world=bpy.data.worlds.new('World');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.5,.58,.58,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.8
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.render.film_transparent=True
bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam
def shot(name,pos,target,ortho=None,res=(900,720)):
 cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x,sc.render.resolution_y=res;sc.render.resolution_percentage=100
 cam.data.type='ORTHO' if ortho else 'PERSP';cam.data.sensor_fit='HORIZONTAL' if ortho else 'VERTICAL'
 if ortho:cam.data.ortho_scale=ortho
 else:cam.data.angle=math.radians(64)
 sc.render.filepath=str(V/(name+'.png'));bpy.ops.render.render(write_still=True)
for tag,mat in [('v6d',old),('v6e',m)]:
 assign(kart,mat);shot(tag+'_close',(2.2,2.9,2),(0,.03,.42),2.65);shot(tag+'_rear',(-2.1,-2.6,1.7),(0,-.15,.43),2.5)
for stem,texname in [('garage_v5_combo','garage_baked32'),('dirt_v5_combo','dirt_baked32')]:
 env=load(R/'art/export/scene-a-v5b-garage'/(stem+'.fbx'));assign(env,material(R/'art/export/scene-a-v5b-garage'/(texname+'.png'),stem))
sc.render.film_transparent=False
for o in kart:o.location.y-=3
for tag,mat in [('v6d',old),('v6e',m)]:
 assign(kart,mat);shot(tag+'_camera_B',(0,-8.6,2.8),(0,3,1),None,(1280,720))
(V/'camera.json').write_text(json.dumps(dict(rear=5.6,height=2.8,vertical_FOV=64,location=[0,-8.6,2.8],target=[0,3,1],kart_translation=[0,-3,0],same_geometry_and_camera=True),indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(R/'art/source/po1/kart-preview.blend'))
print('VERIFIED',json.dumps(validation),flush=True)
