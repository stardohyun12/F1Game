from pathlib import Path
import bpy,math
from mathutils import Vector
R=Path(__file__).resolve().parent
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(R/f'{R.name}.glb'))
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=False
sc.render.resolution_x=960;sc.render.resolution_y=540;sc.render.resolution_percentage=100;sc.render.film_transparent=True
sc.world=bpy.data.worlds.new('D124');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.64,.70,.69,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.8
light=bpy.data.lights.new('D124_sun','SUN');light.energy=2.3;light.color=(1,.91,.77);o=bpy.data.objects.new('D124_sun',light);sc.collection.objects.link(o);o.rotation_euler=(math.radians(22),math.radians(-35),math.radians(-25))
sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=.35
cam=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));sc.collection.objects.link(cam);sc.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=.88
center=Vector((0,0,.0475))
for name,vec in [('preview',(1,-1.6,1.05)),('preview_scene',(0,-1.5,.59))]:
 cam.location=center+Vector(vec);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(R/f'{name}.png');bpy.ops.render.render(write_still=True)
