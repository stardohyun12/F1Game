import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/kart-v6b';bpy.ops.wm.open_mainfile(filepath=str(R/'art/source/kart-v6/kart-v6-baked.blend'));sc=bpy.context.scene
obs=[o for o in sc.objects if o.type=='MESH'];data=[]
for o in obs:
 me=o.data;me.calc_loop_triangles();base=o.data.materials[0].name
 group='plastic' if base=='plastic' else ('tire' if o.name.startswith('k_tire') else ('seat' if base=='seat' else ('plate' if '07' in o.name else ('metal' if base in ['metal','rubber','cream'] or o.name.startswith('steering') else 'warm'))))
 for t in me.loop_triangles:
  data.append(dict(name=o.name,group=group,uv=[list(me.uv_layers['BakedUV'].data[i].uv) for i in t.loops],source_uv=[list(me.uv_layers['SourceUV'].data[i].uv) for i in t.loops],p=[list(o.matrix_world@me.vertices[i].co) for i in t.vertices],normal=list(t.normal),material=base))
(W/'triangles.json').write_text(json.dumps(data))
# Controlled original reconstruction / neutral-sky ablation; keep source textures, sun and AO geometry fixed.
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=2.4;sun.data.color=(1,.83,.60);sun.data.angle=.10;sun.rotation_euler=Vector((.7,.65,-.45)).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=12,location=(0,0,.09));floor=bpy.context.object;fm=bpy.data.materials.new('probe_ground');fm.diffuse_color=(.42,.36,.27,1);floor.data.materials.append(fm)
sc.world.use_nodes=True;bg=sc.world.node_tree.nodes['Background'];bg.inputs[1].default_value=.65;sc.cycles.samples=128;sc.cycles.max_bounces=4
img=bpy.data.images.new('probe_target',1024,256,alpha=False)
for m in {s.material for o in obs for s in o.material_slots if s.material}:
 t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=img;m.node_tree.nodes.active=t
bpy.ops.object.select_all(action='DESELECT')
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.duplicate();dups=list(bpy.context.selected_objects);bpy.context.view_layer.objects.active=dups[0];bpy.ops.object.join();joined=bpy.context.object
for o in obs:o.hide_render=True
for name,color in [('reconstructed',(.30,.52,.65,1)),('neutral_sky',((.30*.2126+.52*.7152+.65*.0722),)*3+(1,))]:
 bg.inputs[0].default_value=color;bpy.ops.object.bake(type='DIFFUSE');img.filepath_raw=str(W/(name+'.png'));img.file_format='PNG';img.save()
 print('BAKED',name,flush=True)

