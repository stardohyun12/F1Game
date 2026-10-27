import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/po1';E=R/'art/export/scene-a2-kit-po1';V=R/'art/_review/2026-10-27-po1';sources=json.loads((W/'sources.json').read_text())
def mat(path):
 m=bpy.data.materials.new('preview');m.use_nodes=True;n=m.node_tree.nodes;n.clear();t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path),check_existing=True);t.interpolation='Closest';em=n.new('ShaderNodeEmission');tr=n.new('ShaderNodeBsdfTransparent');mix=n.new('ShaderNodeMixShader');cut=n.new('ShaderNodeMath');cut.operation='GREATER_THAN';cut.inputs[1].default_value=.5;out=n.new('ShaderNodeOutputMaterial');l=m.node_tree.links;l.new(t.outputs[0],em.inputs[0]);l.new(t.outputs[1],cut.inputs[0]);l.new(cut.outputs[0],mix.inputs[0]);l.new(tr.outputs[0],mix.inputs[1]);l.new(em.outputs[0],mix.inputs[2]);l.new(mix.outputs[0],out.inputs[0]);return m
for name,path in sources.items():
 if '--' in sys.argv and name not in sys.argv[sys.argv.index('--')+1:]:continue
 bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;bpy.ops.import_scene.fbx(filepath=str(R/'art/export/scene-a2-kit'/(name+'.fbx')));obs=[o for o in sc.objects if o.type=='MESH'];pts=[o.matrix_world@v.co for o in obs for v in o.data.vertices];lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)]);ctr=(lo+hi)/2;size=max(hi-lo);tree=name.startswith('autumn')
 sc.render.engine='CYCLES';sc.cycles.samples=8;sc.cycles.use_denoising=False;sc.cycles.transparent_max_bounces=64;sc.render.film_transparent=True;sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.render.dither_intensity=0;sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.world=bpy.data.worlds.new('World');sc.world.use_nodes=True
 bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.type='ORTHO'
 def shot(tag,target,offset,scale,res,suffix):
  cam.location=Vector(target)+Vector(offset);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;sc.render.resolution_x,sc.render.resolution_y=res;sc.render.resolution_percentage=100;sc.render.filepath=str(V/(name+'_'+tag+suffix+'.png'));bpy.ops.render.render(write_still=True)
 for tag,tex in [('before',R/path),('after',E/(name+'.png'))]:
  m=mat(tex)
  for o in obs:o.data.materials.clear();o.data.materials.append(m)
  if tree:
   shot(tag,ctr,(size*.8,-size*1.6,size*.12),size*.81,(600,900),'');shot(tag,(0,0,2.0),(6,-10,3),4.6,(720,720),'_trunk')
  else:shot(tag,ctr,(size*1.5,-size*2,size*1.2),size*1.55,(900,800),'')
 print('RENDERED',name,flush=True)
