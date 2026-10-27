import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/photo-kit';E=R/'art/export/scene-a6-photo-kit';V=R/'art/_review/2026-10-27-photo-kit';items=json.loads((W/'manifest.json').read_text());checks=[]
def emat(path,name):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path),check_existing=True);tex.interpolation='Closest';em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(tex.outputs[0],em.inputs[0]);m.node_tree.links.new(em.outputs[0],out.inputs[0]);return m
def setup(sc):
 sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.use_denoising=False;sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0;sc.render.dither_intensity=0;sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.render.film_transparent=True
 for o in sc.objects:
  if o.name=='bake_ground':o.hide_render=True
 bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;return cam
def shot(sc,cam,name,target,offset,scale,res=(800,800)):
 cam.location=Vector(target)+Vector(offset);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=scale;sc.render.resolution_x,sc.render.resolution_y=res;sc.render.resolution_percentage=100;sc.render.filepath=str(V/(name+'.png'));bpy.ops.render.render(write_still=True)
for d in items:
 name=d['name'];bpy.ops.wm.open_mainfile(filepath=str(W/(name+'.blend')));sc=bpy.context.scene;o=bpy.data.objects[name];cam=setup(sc)
 if d['kind']=='prop':
  # Same geometry/camera: high-resolution rectified photo before, baked palette atlas after.
  for m in o.data.materials:
   n=m.node_tree.nodes;bs=next(x for x in n if x.type=='BSDF_DIFFUSE');out=next(x for x in n if x.type=='OUTPUT_MATERIAL');em=n.new('ShaderNodeEmission')
   if bs.inputs[0].is_linked:
    link=bs.inputs[0].links[0];tex=link.from_node;tex.image=bpy.data.images.load(str(W/'crops'/(name+'.png')),check_existing=True);m.node_tree.links.new(link.from_socket,em.inputs[0])
   else:em.inputs[0].default_value=bs.inputs[0].default_value
   m.node_tree.links.new(em.outputs[0],out.inputs[0])
  h=d['size'][2];shot(sc,cam,name+'_photo',(0,0,h*.48),(2.8,-4,2.3),max(h*1.55,d['size'][0]*1.65))
  o.data.uv_layers.remove(o.data.uv_layers['SourceUV']);o.data.uv_layers.active_index=0;o.data.uv_layers.active.name='UVMap';m=emat(E/(name+'.png'),name+'_unlit');o.data.materials.clear();o.data.materials.append(m)
  for f in o.data.polygons:f.material_index=0
  n=m.node_tree.nodes;out=next(x for x in n if x.type=='OUTPUT_MATERIAL');tex=next(x for x in n if x.type=='TEX_IMAGE');em=next(x for x in n if x.type=='EMISSION');bs=n.new('ShaderNodeBsdfPrincipled');m.node_tree.links.new(tex.outputs[0],bs.inputs['Base Color']);m.node_tree.links.new(bs.outputs[0],out.inputs[0]);bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  before=[list(v.co) for v in o.data.vertices];uv=[list(x.uv) for x in o.data.uv_layers.active.data];bpy.ops.export_scene.fbx(filepath=str(E/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False);m.node_tree.links.new(em.outputs[0],out.inputs[0]);n.remove(bs)
  shot(sc,cam,name+'_result',(0,0,h*.48),(2.8,-4,2.3),max(h*1.55,d['size'][0]*1.65));shot(sc,cam,name+'_front',(0,0,h*.48),(0,-5,0),max(h*1.2,d['size'][0]*1.2))
  o.hide_render=True;existing=set(sc.objects);bpy.ops.import_scene.fbx(filepath=str(E/(name+'.fbx')));new=[x for x in sc.objects if x not in existing and x.type=='MESH'];assert len(new)==1;v=new[0];pts=[v.matrix_world@x.co for x in v.data.vertices];dims=[max(p[i] for p in pts)-min(p[i] for p in pts) for i in range(3)];v.data.calc_loop_triangles();assert max(abs(a-b) for a,b in zip(dims,d['size']))<1e-5;assert len(v.data.uv_layers)==1
  checks.append(dict(name=name,tris=len(v.data.loop_triangles),dimensions=dims,expected=d['size'],uv_layers=1,texture_path=str(E/(name+'.png'))))
 else:
  # Repeat wall preview: physical UV metres divided by the tile's stated metre coverage.
  bpy.data.objects.remove(o,do_unlink=True);ww,hh=(6,.9) if name.startswith('stone') else (4,2);dep=.25 if name.startswith('stone') else .08
  bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,hh/2));o=bpy.context.object;o.dimensions=(ww,dep,hh);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  for f in o.data.polygons:
   for li in f.loop_indices:
    p=o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=((p.x+ww/2)/d['tile_m'][0],(p.y if abs(f.normal.z)>.5 else p.z)/d['tile_m'][1])
  for tag,path in [('photo',W/'crops'/(name+'.png')),('result',E/(name+'.png'))]:
   o.data.materials.clear();o.data.materials.append(emat(path,tag));shot(sc,cam,name+'_'+tag,(0,0,hh*.5),(3,-7,2.7),ww*1.18,(1100,600))
(E/'geometry-validation.json').write_text(json.dumps(checks,indent=2));print('EXPORTED',checks,flush=True)
# Existing garage FBX uses unchanged atlas UVs; render three texture alternatives only.
bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;bpy.ops.import_scene.fbx(filepath=str(R/'art/export/scene-a-v5b-garage/garage_v5_combo.fbx'));garage=[o for o in sc.objects if o.type=='MESH'];cam=setup(sc)
for tag,path in [('original',R/'art/export/scene-a-v5b-garage/garage_baked32.png'),('F2',E/'garage_tin_F2_atlas.png'),('F3',E/'garage_tin_F3_atlas.png')]:
 if not path.exists():continue
 m=emat(path,tag)
 for o in garage:
  o.data.materials.clear();o.data.materials.append(m)
  for f in o.data.polygons:f.material_index=0
 shot(sc,cam,'garage_'+tag,(0,5,2.3),(15,-15,13),23,(1100,750))
