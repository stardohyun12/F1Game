import bpy,math,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');E=R/'art/export/q2-garage';V=R/'art/_review/2026-10-28-q2-garage/v2';V.mkdir(exist_ok=True)
files={'Garage_Albedo':'garage_baked32.png','Garage_Sign_Photo':'garage_sign.png','Garage_Details_Photo':'garage_details.png','race_photo_0_Photo':'garage_memory_1.png','race_photo_1_Photo':'garage_memory_2.png','race_photo_2_Photo':'garage_memory_3.png','festival_registration_Photo':'garage_festival.png'}
for tag in ['v1','v2']:
 bpy.ops.wm.read_factory_settings(use_empty=True);folder=E/'v1' if tag=='v1' else E
 bpy.ops.import_scene.fbx(filepath=str(folder/'garage_v5_combo.fbx'));o=next(o for o in bpy.context.scene.objects if o.type=='MESH')
 if tag=='v1':
  # Reproduce programmer preview fix: only Albedo UV0 <- atlas UV1.
  source=o.data.uv_layers.get('BakedUV') or o.data.uv_layers[1];uv=o.data.uv_layers[0]
  for p in o.data.polygons:
   if o.data.materials[p.material_index].name=='Garage_Albedo':
    for li in p.loop_indices:uv.data[li].uv=source.data[li].uv
 o.data.uv_layers.active_index=0;o.data.uv_layers[0].active_render=True
 for m in o.data.materials:
  m.use_nodes=True;n=m.node_tree.nodes;n.clear();out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.85;t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(folder/files[m.name]));t.interpolation='Closest';m.node_tree.links.new(t.outputs[0],bs.inputs['Base Color']);m.node_tree.links.new(bs.outputs[0],out.inputs[0])
 bpy.ops.import_scene.fbx(filepath=str(folder/'dirt_v5_combo.fbx'));dirt=bpy.context.selected_objects[0]
 for m in dirt.data.materials:
  m.use_nodes=True;n=m.node_tree.nodes;n.clear();out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfPrincipled');t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(folder/'dirt_baked32.png'));t.interpolation='Closest';m.node_tree.links.new(t.outputs[0],bs.inputs['Base Color']);m.node_tree.links.new(bs.outputs[0],out.inputs[0])
 sc=bpy.context.scene;bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=1.6;sun.data.color=(1,.92,.79);sun.data.angle=.15;sun.rotation_euler=Vector((.2,.6,-.8)).to_track_quat('-Z','Y').to_euler()
 sc.world=bpy.data.worlds.new('sky');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.65,.75,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.85
 sc.render.engine='BLENDER_EEVEE';sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.render.dither_intensity=0;sc.render.resolution_x=1600;sc.render.resolution_y=900;sc.render.resolution_percentage=100
 for m in bpy.data.materials:
  if not m.use_nodes:continue
  n=m.node_tree.nodes;bs=n.get('Principled BSDF');out=n.get('Material Output')
  if not bs or not bs.inputs['Base Color'].is_linked:continue
  em=n.new('ShaderNodeEmission');mix=n.new('ShaderNodeMixShader');mix.inputs[0].default_value=.70;m.node_tree.links.new(bs.inputs['Base Color'].links[0].from_socket,em.inputs[0]);m.node_tree.links.new(bs.outputs[0],mix.inputs[1]);m.node_tree.links.new(em.outputs[0],mix.inputs[2]);m.node_tree.links.new(mix.outputs[0],out.inputs[0])
 bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.sensor_fit='VERTICAL';cam.data.sensor_height=32;cam.data.lens=32/(2*math.tan(math.radians(64)/2))
 views={'inside':((0,10.7,1.8),(0,0,2.0)),'outside15':((0,-15,2.8),(0,2,2.3)),'board':((-2.4,4.65,2.15),(-4.85,4.65,2.12))}
 for view,(pos,target) in views.items():
  cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(V/(tag+'_'+view+'.png'));bpy.ops.render.render(write_still=True)
(V/'render-settings.json').write_text(json.dumps({'same_camera_lighting':True,'views':views,'engine':'EEVEE','ambient_approximation':.70,'sun':1,'lighting_baked':False,'v1_uv_workaround':'Albedo only UV0<-BakedUV, same as programmer 0535','v2_uv_workaround':None},indent=2));print('V2 REVIEW RENDERS DONE')
