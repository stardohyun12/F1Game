"""2150 문 자리 탐색: 뷰어 glb 에서 두 입구 문짝 회전 범위와 겹치는 물건 목록(Blender 좌표로 되돌려 출력)."""
import bpy
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='D:/Github/F1Game/tools/viewer/assets/house_home.glb')
regions = {'room_swing': ((2.3, 3.7), (-0.6, 0.5)), 'parents_swing': ((-0.5, 0.55), (0.6, 1.75))}
for o in bpy.data.objects:
    if o.type != 'MESH': continue
    ws = [o.matrix_world @ Vector(c) for c in o.bound_box]
    mn = Vector((min(v.x for v in ws), min(v.y for v in ws), min(v.z for v in ws))); mx = Vector((max(v.x for v in ws), max(v.y for v in ws), max(v.z for v in ws)))
    if mx.z - mn.z > 8: continue
    for fx in (1, -1):
        for fy in (1, -1):
            ax0, ax1 = sorted((mn.x * fx, mx.x * fx)); by0, by1 = sorted((mn.y * fy, mx.y * fy))
            for nm, ((x0, x1), (y0, y1)) in regions.items():
                if ax0 < x1 and ax1 > x0 and by0 < y1 and by1 > y0 and mx.z > 3.0 and mn.z < 5.5:
                    print('HIT', nm, fx, fy, o.name, [round(v, 2) for v in (ax0, ax1, by0, by1, mn.z, mx.z)])
