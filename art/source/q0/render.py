import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/q0';E=R/'art/export/scene-albedo-kit';V=R/'art/_review/2026-10-28-q0';names=['vending_G1','vending_G2','ac_G3','postbox_G1','tin_F3'];stage='r1'
if '--' in sys.argv:stage=sys.argv[sys.argv.index('--')+1]
if '--' in sys.argv and len(sys.argv)>sys.argv.index('--')+2:names=sys.argv[sys.argv.index('--')+2:]
V=V/stage;V.mkdir(exist_ok=True)
def mat(path):
 m=bpy.data.materials.new('lit_albedo');m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.9;bs.inputs['Metallic'].default_value=0;bs.inputs['Specular IOR Level'].default_value=.15;t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path));t.interpolation='Closest';m.node_tree.links.new(t.outputs[0],bs.inputs['Base Color']);return m
for name in names:
 for tag in ['before','after']:
  bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene
  if name=='tin_F3':
   bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,1));o=bpy.context.object;o.dimensions=(2,.05,2);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
   for f in o.data.polygons:
    for li in f.loop_indices:
     p=o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=(p.x+1,p.z/2)
  else:
   fbx=(E if tag=='after' else R/'art/export/scene-a6-photo-kit')/(name+'.fbx');bpy.ops.import_scene.fbx(filepath=str(fbx));o=next(o for o in sc.objects if o.type=='MESH')
  o.data.materials.clear();o.data.materials.append(mat((W/'before' if tag=='before' else E)/(name+'.png')))
  for f in o.data.polygons:f.material_index=0
  pts=[o.matrix_world@v.co for v in o.data.vertices];lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)]);center=(lo+hi)/2;size=max(hi-lo)
  bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;gm=bpy.data.materials.new('ground');gm.diffuse_color=(.25,.24,.20,1);ground.data.materials.append(gm)
  bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=1.6;sun.data.color=(1,.92,.79);sun.data.angle=.15;sun.rotation_euler=Vector((.45,.6,-.8)).to_track_quat('-Z','Y').to_euler()
  sc.world=bpy.data.worlds.new('sky');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.65,.75,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.55
  sc.render.engine='CYCLES';sc.cycles.samples=4;sc.cycles.use_denoising=True;sc.cycles.max_bounces=2;sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0;sc.render.dither_intensity=0;sc.render.image_settings.file_format='PNG';sc.render.resolution_percentage=100
  try:
   pr=bpy.context.preferences.addons['cycles'].preferences;pr.compute_device_type='OPTIX';pr.get_devices()
   for dev in pr.devices:dev.use=dev.type=='OPTIX'
   sc.cycles.device='GPU'
  except Exception:pass
  bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam
  for view,vec in [('front',(0,-1,.04)),('three',(0.55,-1,.35)),('game',(0.45,-1,.25))]:
   cam.data.type='PERSP' if view=='game' else 'ORTHO';cam.data.ortho_scale=size*1.4;cam.data.sensor_fit='VERTICAL';cam.data.sensor_height=32;cam.data.lens=32/(2*math.tan(math.radians(64)/2));dist=8 if view=='game' else size*4;cam.location=center+Vector(vec).normalized()*dist;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x,sc.render.resolution_y=(1600,900) if view=='game' else (640,640);sc.render.filepath=str(V/(name+'_'+tag+'_'+view+'.png'));bpy.ops.render.render(write_still=True)
 print('RENDER',name,flush=True)
(V/'lighting.json').write_text(json.dumps(dict(sun_count=1,sun_energy=1.6,sun_color=[1,.92,.79],sky_color=[.55,.65,.75],sky_strength=.55,game_distance_m=8,game_vertical_fov=64,game_render=[1600,900],downsample='Nearest to 1280x720',texture_bake=False,approximation='Scene_A7_Light intention only, not Unity shader reproduction'),indent=2))
