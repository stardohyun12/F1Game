import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/photo-kit'
for variant in ['tin_F2','tin_F3']:
 bpy.ops.wm.open_mainfile(filepath=str(R/'art/models/garage_scene_a_v5_combo/baked-lit.blend'));sc=bpy.context.scene
 for im in bpy.data.images:
  if im.source=='FILE':
   basename=Path(im.filepath.replace('\\','/')).name;candidates=list((R/'art/export/scene-a-v1b-photobash').glob('*.fbm/'+basename))
   if candidates:im.filepath=str(candidates[0]);im.reload()
 for o in list(sc.objects):
  if o.type=='LIGHT':bpy.data.objects.remove(o,do_unlink=True)
 bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=2.4;sun.data.color=(1,.83,.60);sun.data.angle=.10;sun.rotation_euler=Vector((.7,.65,-.45)).to_track_quat('-Z','Y').to_euler();sc.world.use_nodes=True;sc.world.node_tree.nodes.get('Background').inputs[0].default_value=(.30,.52,.65,1);sc.world.node_tree.nodes.get('Background').inputs[1].default_value=.65
 sc.render.engine='CYCLES';sc.cycles.samples=128;sc.cycles.max_bounces=4;sc.cycles.use_denoising=True;sc.render.bake.margin=3;sc.render.bake.use_clear=True;sc.render.bake.use_pass_direct=True;sc.render.bake.use_pass_indirect=True;sc.render.bake.use_pass_color=True
 obs=[o for o in sc.objects if o.type=='MESH' and any(m and m.name.startswith(('tin_wall','tin_roof')) for m in o.data.materials) and any(c.name=='garage' for c in o.users_collection)]
 source=bpy.data.images.load(str(W/'source'/(variant+'.png')));ref=bpy.data.images.load(str(R/'art/export/scene-a-v5b-garage/garage_baked32.png'));target=bpy.data.images.new('new_tin',*ref.size,alpha=False);jobs=[]
 for o in obs:
  m=bpy.data.materials.new('photo_'+o.name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfDiffuse');tex=n.new('ShaderNodeTexImage');tex.image=source;tex.interpolation='Closest';uv=n.new('ShaderNodeUVMap');uv.uv_map='SourceUV';m.node_tree.links.new(uv.outputs[0],tex.inputs[0]);m.node_tree.links.new(tex.outputs[0],bs.inputs[0]);m.node_tree.links.new(bs.outputs[0],out.inputs[0]);t=n.new('ShaderNodeTexImage');t.image=target;n.active=t;o.data.materials.clear();o.data.materials.append(m)
  me=o.data
  for f in me.polygons:
   f.material_index=0
   for li in f.loop_indices:
    p=o.matrix_world@me.vertices[me.loops[li].vertex_index].co;me.uv_layers['SourceUV'].data[li].uv=((p.x if o.name=='roof' else p.y)/(2 if variant=='tin_F2' else 1),(p.y if o.name=='roof' else p.z)/2)
  me.uv_layers.active=me.uv_layers['BakedUV'];me.calc_loop_triangles()
  for t in me.loop_triangles:
   p=[o.matrix_world@me.vertices[i].co for i in t.vertices];center=sum(p,Vector())/3;n=o.matrix_world.to_3x3()@t.normal
   exterior=n.z>.3 if o.name=='roof' else n.x*center.x>.1
   if exterior:jobs.append(dict(name=o.name,uv=[list(me.uv_layers['BakedUV'].data[i].uv) for i in t.loops]))
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.duplicate();dups=list(bpy.context.selected_objects);bpy.context.view_layer.objects.active=dups[0];bpy.ops.object.join();joined=bpy.context.object
 for o in obs:o.hide_render=True
 bpy.ops.object.bake(type='DIFFUSE');target.filepath_raw=str(W/('garage_'+variant+'_raw.png'));target.file_format='PNG';target.save();bpy.ops.object.bake(type='AO');target.filepath_raw=str(W/('garage_'+variant+'_ao.png'));target.save();(W/'garage-tin-mask.json').write_text(json.dumps(jobs));print('GARAGE_BAKED',variant,len(jobs),flush=True)
