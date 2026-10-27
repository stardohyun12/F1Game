import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/q2-garage';E=R/'art/export/q2-garage';V=R/'art/_review/2026-10-28-q2-garage';stage=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'r1';D=V/stage;D.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(W/'garage.blend'));sc=bpy.context.scene
old=[o for o in sc.objects if o.get('q2_original')];new=[o for o in sc.objects if o.get('q2_added')];before_sign=next(o for o in sc.objects if o.get('q2_before_only'));sign=bpy.data.objects['sign_main'];base=bpy.data.materials['Garage_Albedo'];before=bpy.data.materials['Garage_Before'];signmat=bpy.data.materials['Garage_Sign_Photo']
def texmat(name,path):
 m=bpy.data.materials.new(name);m.use_nodes=True;b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Roughness'].default_value=.9;t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path));t.interpolation='Closest';m.node_tree.links.new(t.outputs[0],b.inputs['Base Color']);return m
bpy.ops.import_scene.fbx(filepath=str(R/'art/export/scene-a-v5b-garage/dirt_v5_combo.fbx'));dirt=bpy.context.selected_objects[0];dirt.data.materials.clear();dirt.data.materials.append(texmat('dirt_albedo',E/'dirt_baked32.png'))
bpy.ops.mesh.primitive_plane_add(size=160,location=(0,0,-.085));yard=bpy.context.object;yard.data.materials.append(texmat('preview_ground_photo',W/'preview_ground.png'))
for uv in yard.data.uv_layers.active.data:uv.uv*=40
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=1.6;sun.data.color=(1,.92,.79);sun.data.angle=.15;sun.rotation_euler=Vector((.2,.6,-.8)).to_track_quat('-Z','Y').to_euler()
sc.world=bpy.data.worlds.new('sky');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.65,.75,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.85
sc.render.engine='BLENDER_EEVEE';sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0;sc.render.dither_intensity=0;sc.render.image_settings.file_format='PNG';sc.render.resolution_percentage=100;sc.render.resolution_x=1600;sc.render.resolution_y=900
# Render-only, unoccluded sky ambient approximates the game's ambient term.
# This is NOT stored in the FBX material or PNG. Same equation for before/after.
for m in list(bpy.data.materials):
 if not m.use_nodes:continue
 bs=m.node_tree.nodes.get('Principled BSDF')
 if not bs:continue
 out=m.node_tree.nodes.get('Material Output');em=m.node_tree.nodes.new('ShaderNodeEmission');mix=m.node_tree.nodes.new('ShaderNodeMixShader');mix.inputs[0].default_value=.70
 if bs.inputs['Base Color'].is_linked:m.node_tree.links.new(bs.inputs['Base Color'].links[0].from_socket,em.inputs[0])
 else:em.inputs[0].default_value=bs.inputs['Base Color'].default_value
 em.inputs[1].default_value=1;m.node_tree.links.new(bs.outputs[0],mix.inputs[1]);m.node_tree.links.new(em.outputs[0],mix.inputs[2]);m.node_tree.links.new(mix.outputs[0],out.inputs[0])
try:
 pr=bpy.context.preferences.addons['cycles'].preferences;pr.compute_device_type='OPTIX';pr.get_devices()
 for dev in pr.devices:dev.use=dev.type=='OPTIX'
 sc.cycles.device='GPU'
except Exception:pass
bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.sensor_fit='VERTICAL';cam.data.sensor_height=32;cam.data.lens=32/(2*math.tan(math.radians(64)/2))
views={'outside15':((0,-15,2.8),(0,2,2.3)),'front5':((0,-5,1.8),(0,4,1.9)),'inside':((0,10.7,1.8),(0,0,2.0)),'sign20':((0,-20,2.8),(0,0,3.6))}
for tag in ['before','after']:
 for o in new:o.hide_render=tag=='before'
 sign.hide_render=tag=='before';before_sign.hide_render=tag=='after'
 for o in old:
  if o==sign:continue
  o.data.materials[0]=before if tag=='before' else base
 for view,(pos,target) in views.items():
  cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(D/(tag+'_'+view+'.png'));bpy.ops.render.render(write_still=True)
(D/'render-settings.json').write_text(json.dumps(dict(views=views,vertical_fov=64,render=[1600,900],final=[1280,720],resize='Nearest',sun_energy=1.6,sun_count=1,sky_strength=.85,no_added_interior_lights=True,engine='EEVEE',render_only_ambient_mix=.70,ambient_note='Unoccluded sky ambient approximation, identical before/after; not exported',no_light_bake=True),indent=2));print('Q2 GARAGE RENDER DONE')
