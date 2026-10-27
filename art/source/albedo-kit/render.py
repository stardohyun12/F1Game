import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/albedo-kit';E=R/'art/export/scene-albedo-kit';V=R/'art/_review/2026-10-27-albedo-kit';jobs=json.loads((W/'jobs.json').read_text())
for d in json.loads((R/'art/source/photo-kit/manifest.json').read_text()):
 if d['kind']=='tile':jobs.append(dict(name=d['name'],file=d['name']+'.png',before=str(R/'art/export/scene-a6-photo-kit'/(d['name']+'.png')),kind='tile',tile=d['tile_m']))
jobs.append(dict(name='kart_v6e',file='kart_v6e.png',before=str(R/'art/export/kart-v6e/kart_v6e.png'),fbx=str(R/'art/export/kart-v6e/kart_v6e.fbx'),kind='kart'))
for k in ['F2','F3']:jobs.append(dict(name='garage_tin_'+k,file='garage_tin_'+k+'_atlas.png',before=str(R/'art/export/scene-a6-photo-kit'/('garage_tin_'+k+'_atlas.png')),fbx=str(R/'art/export/scene-a-v5b-garage/garage_v5_combo.fbx'),kind='garage'))
(W/'inventory.json').write_text(json.dumps(jobs,indent=2))
def material(path):
 m=bpy.data.materials.new('compare');m.use_nodes=True;n=m.node_tree.nodes;n.clear();l=m.node_tree.links;t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path),check_existing=True);t.interpolation='Closest';em=n.new('ShaderNodeEmission');tr=n.new('ShaderNodeBsdfTransparent');mix=n.new('ShaderNodeMixShader');out=n.new('ShaderNodeOutputMaterial');l.new(t.outputs[0],em.inputs[0]);l.new(t.outputs[1],mix.inputs[0]);l.new(tr.outputs[0],mix.inputs[1]);l.new(em.outputs[0],mix.inputs[2]);l.new(mix.outputs[0],out.inputs[0]);return m
for d in jobs:
 if '--trees' in sys.argv and not d['name'].startswith('autumn_tree'):continue
 if '--refined' in sys.argv and not (d['name'].startswith('autumn_tree') or d['kind']=='photo'):continue
 bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene
 if d['kind']=='tile':
  bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,.45));o=bpy.context.object;o.dimensions=(4,.2,.9);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  for f in o.data.polygons:
   for li in f.loop_indices:
    p=o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=((p.x+2)/d['tile'][0],(p.y if abs(f.normal.z)>.5 else p.z)/d['tile'][1])
 else:bpy.ops.import_scene.fbx(filepath=d['fbx'])
 obs=[o for o in sc.objects if o.type=='MESH'];pts=[o.matrix_world@v.co for o in obs for v in o.data.vertices];lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)]);center=(lo+hi)/2;size=max(hi-lo)
 sc.render.engine='CYCLES';sc.cycles.samples=4;sc.cycles.use_denoising=False;sc.cycles.transparent_max_bounces=64;sc.render.film_transparent=True;sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0;sc.render.dither_intensity=0;sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.render.resolution_x=640;sc.render.resolution_y=600;sc.render.resolution_percentage=100
 bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=size*1.5;cam.location=center+Vector((size*1.5,-size*2,size*.85));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
 if d['name'].startswith('autumn_tree'):cam.location=center+Vector((size*.8,-size*1.6,size*.10));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=size*1.2
 for tag,p in [('baked',Path(d['before'])),('albedo',E/d['file'])]:
  mat=material(p)
  for o in obs:
   o.data.materials.clear();o.data.materials.append(mat)
   for f in o.data.polygons:f.material_index=0
  sc.render.filepath=str(V/(d['name']+'_'+tag+'.png'));bpy.ops.render.render(write_still=True)
 print('REVIEW',d['name'],flush=True)
