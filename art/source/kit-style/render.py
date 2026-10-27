import bpy, math, json
from mathutils import Vector
from pathlib import Path
R=Path('D:/Github/F1Game'); S=R/'art/export/scene-a2-kit'; E=R/'art/export/scene-a2-kit-style'; V=R/'art/_review/2026-10-27-kit-style'
names=[x['name'] for x in json.loads((R/'art/source/kit-style/geometry.json').read_text())]+['autumn_tree_01']
def material(path):
 m=bpy.data.materials.new(path.stem);m.use_nodes=True;n=m.node_tree.nodes;n.clear()
 o=n.new('ShaderNodeOutputMaterial'); mix=n.new('ShaderNodeMixShader'); em=n.new('ShaderNodeEmission');tr=n.new('ShaderNodeBsdfTransparent');tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path),check_existing=True);tex.interpolation='Closest'
 m.node_tree.links.new(tex.outputs['Color'],em.inputs[0]);m.node_tree.links.new(tex.outputs['Alpha'],mix.inputs[0]);m.node_tree.links.new(tr.outputs[0],mix.inputs[1]);m.node_tree.links.new(em.outputs[0],mix.inputs[2]);m.node_tree.links.new(mix.outputs[0],o.inputs[0]);return m
for name in names:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(S/(name+'.fbx')))
 obs=[o for o in bpy.context.scene.objects if o.type=='MESH']
 if name=='pole_01':
  for ob in obs: ob.rotation_euler.z+=math.pi/2
 bpy.context.view_layer.update()
 corners=[o.matrix_world@Vector(c) for o in obs for c in o.bound_box]
 direction=Vector((.28,-1,.45)).normalized();rotation=direction.to_track_quat('Z','Y'); inv=rotation.inverted()
 projected=[inv@p for p in corners];mi=Vector(tuple(min(p[i] for p in projected) for i in range(3)));ma=Vector(tuple(max(p[i] for p in projected) for i in range(3)))
 center=rotation@((mi+ma)/2); width=ma.x-mi.x+.32;height=ma.y-mi.y+.32
 bpy.ops.object.camera_add(location=center+direction*70);cam=bpy.context.object;cam.rotation_euler=rotation.to_euler();cam.data.type='ORTHO'
 scene=bpy.context.scene;scene.camera=cam;scene.render.engine='CYCLES';scene.cycles.samples=8
 scene.render.resolution_x=max(24,math.ceil(width*64));scene.render.resolution_y=max(24,math.ceil(height*64));scene.render.resolution_percentage=100
 cam.data.sensor_fit='HORIZONTAL';cam.data.ortho_scale=scene.render.resolution_x/64
 scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.view_settings.view_transform='Standard'
 for variant in (['before'] if name.startswith('autumn') else ['before','after']):
  path=(S if variant=='before' else E)/(name+'.png');mat=material(path)
  for o in obs:o.data.materials.clear();o.data.materials.append(mat)
  scene.render.filepath=str(V/(name+'_'+variant+'.png'));bpy.ops.render.render(write_still=True)
 print('RENDERED',name,flush=True)
# Nature proxies explicitly only preview; same coarse triangulated geometry for before/after.
for name,scale in [('bush',(1.2,1,1)),('mound',(2,1.5,.7)),('rock',(1,.8,.65))]:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=(0,0,scale[2]));ob=bpy.context.object;ob.scale=scale
 bpy.context.view_layer.update();me=ob.data;uv=me.uv_layers.new();
 for poly in me.polygons:
  axis=max(range(3),key=lambda i:abs(poly.normal[i]));axes=[i for i in range(3) if i!=axis]
  for li in poly.loop_indices:
   p=ob.matrix_world@me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(p[axes[0]]*11/32,p[axes[1]]*11/32)
 mat=material(E/(name+'_style.png'));ob.data.materials.append(mat)
 bpy.ops.object.camera_add(location=(3,-8,4));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.6))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=5
 s=bpy.context.scene;s.camera=cam;s.render.engine='CYCLES';s.cycles.samples=8;s.render.resolution_x=320;s.render.resolution_y=220;s.render.resolution_percentage=100;s.render.film_transparent=True;s.render.image_settings.color_mode='RGBA';s.view_settings.view_transform='Standard'
 s.render.filepath=str(V/(name+'_proxy_after.png'));bpy.ops.render.render(write_still=True)
 # Baseline flat-face appearance, a documented proxy (not the Unity mesh).
 ob.data.materials.clear()
 for color in ([(.32,.40,.23,1),(.47,.55,.31,1),(.59,.64,.40,1)] if name!='rock' else [(.40,.40,.35,1),(.55,.54,.46,1),(.68,.65,.55,1)]):
  m=bpy.data.materials.new('flat-face');m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');e.inputs[0].default_value=color;o=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(e.outputs[0],o.inputs[0]);ob.data.materials.append(m)
 for p in me.polygons:p.material_index=2 if p.normal.z>.6 else (1 if p.normal.y<0 else 0)
 s.render.filepath=str(V/(name+'_proxy_before.png'));bpy.ops.render.render(write_still=True)
