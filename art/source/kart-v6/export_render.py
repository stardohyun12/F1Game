import bpy,math,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/kart-v6';E=R/'art/export/kart-v6';V=R/'art/_review/2026-10-27-kart-v6'
bpy.ops.wm.open_mainfile(filepath=str(W/'kart-v6-baked.blend'));sc=bpy.context.scene;kart=[o for o in sc.objects if o.type=='MESH']
def emission(path,name):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path),check_existing=True);t.interpolation='Closest';e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(t.outputs[0],e.inputs[0]);m.node_tree.links.new(e.outputs[0],out.inputs[0]);return m
m=emission(E/'kart_v6_baked32.png','kart_v6_unlit')
for o in kart:
 o.data.materials.clear();o.data.materials.append(m)
 for f in o.data.polygons:f.material_index=0
 o.data.uv_layers.remove(o.data.uv_layers['SourceUV']);o.data.uv_layers.active_index=0;o.data.uv_layers[0].name='UVMap'
# FBX exports Principled diffuse linkage, preview uses true Emission.
n=m.node_tree.nodes;out=next(x for x in n if x.type=='OUTPUT_MATERIAL');tex=next(x for x in n if x.type=='TEX_IMAGE');em=next(x for x in n if x.type=='EMISSION');bs=n.new('ShaderNodeBsdfPrincipled');m.node_tree.links.new(tex.outputs[0],bs.inputs['Base Color']);m.node_tree.links.new(bs.outputs[0],out.inputs[0])
bpy.ops.object.select_all(action='DESELECT')
for o in kart:o.select_set(True)
bpy.context.view_layer.objects.active=kart[0]
bpy.ops.export_scene.fbx(filepath=str(E/'kart_v6.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False)
m.node_tree.links.new(em.outputs[0],out.inputs[0]);n.remove(bs)
sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.use_denoising=False;sc.render.dither_intensity=0;sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.5,.58,.58,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.8
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.render.film_transparent=True
bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam
def shot(name,pos,target,ortho=None,res=(640,640)):
 cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x,sc.render.resolution_y=res;sc.render.resolution_percentage=100
 cam.data.type='ORTHO' if ortho else 'PERSP';cam.data.sensor_fit='HORIZONTAL' if ortho else 'VERTICAL'
 if ortho:cam.data.ortho_scale=ortho
 else:cam.data.angle=math.radians(64)
 sc.render.filepath=str(V/(name+'.png'));bpy.ops.render.render(write_still=True)
views={'top':((0,.09,4),(0,.09,.3)),'side':((4,.09,.5),(0,.09,.5)),'rear':((0,-4,.55),(0,.09,.5))}
for name,(pos,target) in views.items():shot('v6_'+name,pos,target,2.30)
shot('v6_threequarter',(2.2,2.9,2.0),(0,.03,.42),2.65,(900,720))
shot('v6_rear_detail',(-2.1,-2.6,1.7),(0,-.15,.43),2.50,(900,720))
bpy.ops.wm.save_as_mainfile(filepath=str(W/'kart-v6-preview.blend'))
# Black silhouette diagnostic, no texture normalization.
black=bpy.data.materials.new('silhouette');black.use_nodes=True;pn=black.node_tree.nodes.get('Principled BSDF');pn.inputs['Base Color'].default_value=(0,0,0,1)
for o in kart:o.data.materials.clear();o.data.materials.append(black)
for name,(pos,target) in views.items():shot('silhouette_'+name,pos,target,2.30)
for o in kart:o.data.materials.clear();o.data.materials.append(m)
# Original kart, normalized only to the game's specified 1.80m length for fair comparison.
before=set(sc.objects);bpy.ops.import_scene.fbx(filepath=str(R/'art/export/scene-a2-kart/kart_a2.fbx'));old=[o for o in sc.objects if o not in before and o.type=='MESH'];om=emission(R/'art/export/scene-a2-kart/kart_a2_baked32.png','old_unlit')
pts=[o.matrix_world@v.co for o in old for v in o.data.vertices];lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)]);k=1.8/(hi.y-lo.y);ctr=(hi+lo)/2
for o in old:
 for v in o.data.vertices:
  p=o.matrix_world@v.co;v.co=Vector(((p.x-ctr.x)*k,(p.y-ctr.y)*k+.09,(p.z-lo.z)*k+.092))
 o.matrix_world.identity();o.data.materials.clear();o.data.materials.append(om)
for o in kart:o.hide_render=True
for name,(pos,target) in views.items():shot('old_'+name,pos,target,2.30)
shot('old_threequarter',(2.2,2.9,2.0),(0,.03,.42),2.65,(900,720))
for o in old:o.hide_render=True
for o in kart:o.hide_render=False
# Garage environment uses original FBX+PNG as-is, no source edits.
env=[]
for stem,texname in [('garage_v5_combo','garage_baked32'),('dirt_v5_combo','dirt_baked32')]:
 before=set(sc.objects);bpy.ops.import_scene.fbx(filepath=str(R/'art/export/scene-a-v5b-garage'/(stem+'.fbx')));added=[o for o in sc.objects if o not in before and o.type=='MESH'];ma=emission(R/'art/export/scene-a-v5b-garage'/(texname+'.png'),stem+'_preview')
 for o in added:o.data.materials.clear();o.data.materials.append(ma)
 env+=added
sc.render.film_transparent=False
# Kart 3m outside front doorway. Camera is rear 5.6m, height2.8m, vertical64deg.
for o in kart:o.location.y-=3
shot('garage_camera_B',(0,-8.6,2.8),(0,3,1),None,(1280,720))
for o in kart:o.hide_render=True
for o in old:o.location.y-=3;o.hide_render=False
shot('garage_camera_B_old',(0,-8.6,2.8),(0,3,1),None,(1280,720))
(V/'camera.json').write_text(json.dumps(dict(camera_B=dict(rear=5.6,height=2.8,vertical_FOV=64,look_ahead=6,look_height=1),kart_position=[0,-3,0],orthographic_scale=2.3,old_comparison_length_normalized=1.8,unity_executed=False),indent=2))
print('EXPORTED_RENDERED',flush=True)
