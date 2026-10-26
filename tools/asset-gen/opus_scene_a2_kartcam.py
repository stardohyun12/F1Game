"""장면 A2 카트 크기 — 카메라 후보 더 재기 (opus_scene_a2_kart.py 결과 blend 를 열어 다시 굽지 않고 카메라만 바꿈).
    D:/Tools/Blender/blender.exe -b art/models/opus_scene_a2_kart/kart_a2_unlit.blend --python tools/asset-gen/opus_scene_a2_kartcam.py
"""
import bpy, json, math, os
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
REV = os.path.join(R, 'art', '_review', '2026-10-27-scene-a2')
CAMS = {'D': (3.9, 2.0, 64, 5, .3), 'E': (4.4, 2.1, 56, 6, .5)}  # 뒤 거리, 높이, 세로 FOV, 보는 점 앞 m, 보는 점 높이
sc = bpy.context.scene; cam = sc.camera
obs = [o for o in sc.objects if o.type == 'MESH' and o.data.materials and o.data.materials[0].name == 'kart_a2_unlit']
P = [o.matrix_world @ v.co for o in obs for v in o.data.vertices]
js = os.path.join(REV, 'kart_size.json'); info = json.load(open(js))
for k, (dist, hgt, fov, ahead, th) in CAMS.items():
    cam.location = (0, -dist, hgt); cam.data.angle_y = math.radians(fov)
    cam.rotation_euler = (Vector((0, ahead, th)) - cam.location).to_track_quat('-Z', 'Y').to_euler(); bpy.context.view_layer.update()
    V = [world_to_camera_view(sc, cam, p) for p in P]; xs = [v.x for v in V]; ys = [v.y for v in V]
    info['kart'][k] = dict(cam=dict(back=dist, height=hgt, vfov=fov, look_ahead=ahead, look_h=th), w=round(max(xs) - min(xs), 3),
                           h=round(max(ys) - min(ys), 3), cy=round(1 - (max(ys) + min(ys)) / 2, 3))
    sc.render.filepath = os.path.join(REV, f'kart_cam_{k}.png'); bpy.ops.render.render(write_still=True)
json.dump(info, open(js, 'w'), indent=1)
print('DONE', json.dumps({k: (v['w'], v['h'], v['cy']) for k, v in info['kart'].items()}), flush=True)
