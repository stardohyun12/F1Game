from pathlib import Path
import bpy,math,json
from mathutils import Vector
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_scene_a_v5_combo';V=R/'art/_review/2026-10-27-scene-a-v5-combo';bpy.ops.wm.open_mainfile(filepath=str(O/'unlit-final.blend'));s=bpy.context.scene
c=json.loads((V/'camera.json').read_text())['game'];bpy.ops.object.camera_add(location=c['location']);cam=bpy.context.object;cam.data.sensor_fit='VERTICAL';cam.data.angle_y=math.radians(c['vertical_fov']);cam.rotation_euler=(Vector(c['target'])-cam.location).to_track_quat('-Z','Y').to_euler();s.camera=cam
bpy.ops.object.light_add(type='AREA',location=(0,-4,6));li=bpy.context.object;li.data.energy=100000;li.data.color=(1,0,0);li.data.size=5;li.rotation_euler=(Vector((0,0,0))-li.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(V/'game_red_light_proof.png');bpy.ops.render.render(write_still=True)
