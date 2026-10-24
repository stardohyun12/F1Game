import bpy,sys,math
from pathlib import Path
from mathutils import Vector
kind=sys.argv[sys.argv.index('--')+1];root=Path(__file__).resolve().parents[1]/f'cargo_{kind}_v01'
bpy.ops.wm.open_mainfile(filepath=str(root/f'cargo_{kind}_v01.blend'));sc=bpy.context.scene
sc.render.engine='CYCLES';sc.cycles.samples=16;sc.render.resolution_x=640;sc.render.resolution_y=480;sc.render.resolution_percentage=100;sc.render.film_transparent=True
sc.world=bpy.data.worlds.new('ambient');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.64,.7,.69,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.8
sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=.35
l=bpy.data.lights.new('sun','SUN');l.energy=2.3;l.color=(1,.91,.77);o=bpy.data.objects.new('sun',l);sc.collection.objects.link(o);o.rotation_euler=(.4,-.6,-.4)
c=bpy.data.cameras.new('camera');o=bpy.data.objects.new('camera',c);sc.collection.objects.link(o);sc.camera=o;c.type='ORTHO';c.ortho_scale=.46;o.location=(.7,-1,.8);o.rotation_euler=(Vector((0,0,.06))-o.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(root/'preview.png');bpy.ops.render.render(write_still=True)
