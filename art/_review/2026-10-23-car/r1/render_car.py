"""Review-only imported-model comparison, same camera/light for old and new."""
from pathlib import Path
import bpy, math, json, argparse, sys
from mathutils import Vector
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2]
p=argparse.ArgumentParser();p.add_argument('--old',action='store_true');p.add_argument('--quick',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
if a.old:bpy.ops.wm.open_mainfile(filepath=str(HERE/'current-car-source.blend'))
else:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(ROOT/'art/models/car_v01/car_v01.glb'))
for ob in list(bpy.context.scene.objects):
    if ob.type!='MESH':bpy.data.objects.remove(ob,do_unlink=True)
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=32 if a.quick else 72;sc.cycles.use_denoising=False
sc.render.image_settings.color_mode='RGBA';sc.render.image_settings.file_format='PNG';sc.render.film_transparent=True
sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0
sc.world=bpy.data.worlds.new('coast_ambient');sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.61,.65,1)
sc.world.node_tree.nodes['Background'].inputs[1].default_value=.65
light=bpy.data.lights.new('coast_sun','SUN');light.energy=2.;light.color=(1,.86,.66);light.angle=math.radians(1)
sun=bpy.data.objects.new('sun',light);sc.collection.objects.link(sun)
sun.rotation_euler=Vector((.82,.48,-.31)).to_track_quat('-Z','Y').to_euler()
camd=bpy.data.cameras.new('matched_review_camera');cam=bpy.data.objects.new('camera',camd);sc.collection.objects.link(cam);sc.camera=cam
camd.sensor_fit='VERTICAL';camd.sensor_height=24;camd.lens=24/(2*math.tan(math.radians(50/2)))
def aim(loc,target):
    cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();bpy.context.view_layer.update()
def render(name,w=960,h=540):
    sc.render.resolution_x=w;sc.render.resolution_y=h;sc.render.resolution_percentage=100;sc.render.filepath=str(HERE/name);bpy.ops.render.render(write_still=True)
bpy.ops.mesh.primitive_plane_add(size=40,location=(0,0,-.001));ground=bpy.context.object;ground.name='REVIEW_ONLY_shadow';ground.is_shadow_catcher=True
gm=bpy.data.materials.new('review_road');gm.use_nodes=True;gm.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.25,.23,.19,1);gm.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=1;ground.data.materials.append(gm)
prefix='current' if a.old else 'new'
aim((0,4.0625,1.810015),(0,-8.1375,.910015));render(prefix+'-chase-render.png')
if not a.old:
    camd.type='ORTHO';camd.ortho_scale=4.9
    aim((4,6,3.1),(0,-.1,.48));render('new-close-render.png',1280,800)
    ground.hide_render=True
    for name,loc,target in [('front',(0,-7,.55),(0,0,.55)),('rear',(0,7,.55),(0,0,.55)),('side',(8,0,.55),(0,0,.55)),('top',(0,0,8),(0,0,0))]:
        camd.ortho_scale=4.5;aim(loc,target);render(name+'-render.png',960,640)
    # A view at the reference concept car's screen footprint, separate from live crop.
    ground.hide_render=False;camd.type='PERSP';camd.sensor_fit='VERTICAL';camd.lens=24/(2*math.tan(math.radians(50/2)))
    aim((0,6.6,2.5),(0,-3.0,.75));render('reference-chase-render.png')
    bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'review-rig.blend'))
proof={'input':'current-car-source.blend' if a.old else 'art/models/car_v01/car_v01.glb','camera_blender':[0,4.0625,1.810015],'target_blender':[0,-8.1375,.910015],'vertical_fov':50,'resolution':[960,540],'scene_basis':'CameraFeel_4_Postcard: height1.9,distance4.2,lookHeight1,lookAhead8; current visual origin offset removed','runtime_camera_exact':False,'light_color':[1,.86,.66],'sun_energy_blender':2,'ambient_color':[.55,.61,.65],'note':'Review approximation of Unity light; same rig for both models; not a Unity capture','render_engine':'Cycles','samples':sc.cycles.samples}
(HERE/(prefix+'-render-proof.json')).write_text(json.dumps(proof,indent=2),encoding='utf-8')
