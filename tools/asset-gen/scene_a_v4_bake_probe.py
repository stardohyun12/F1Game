import bpy,time
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.mesh.primitive_cube_add();o=bpy.context.object;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT');m=bpy.data.materials.new('m');m.use_nodes=True;o.data.materials.append(m);im=bpy.data.images.new('test',width=128,height=128);n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;m.node_tree.nodes.active=n;s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=4;p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for d in p.devices:d.use=d.type=='OPTIX'
s.cycles.device='GPU';print('TEST_START',flush=True);bpy.ops.object.bake(type='DIFFUSE');print('TEST_DONE',flush=True)
