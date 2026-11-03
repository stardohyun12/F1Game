"""2150: 문설주 면(x 2.55·3.45, y 0.48~0.62) 위 꼭짓점 위치 전부 — T자 이음 찾기."""
import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/Github/F1Game/art/export/house-home/house_home.fbx')
for o in bpy.data.objects:
    if o.name != 'HOUSE_shell': continue
    vs = [o.matrix_world @ v.co for v in o.data.vertices]
    for X in (2.55, 3.45, 2.49, 3.51):
        near = sorted(set((round(abs(v.x), 3), round(abs(v.y), 3), round(v.z, 3)) for v in vs if abs(abs(v.x) - X) < .003 and .45 < abs(v.y) < .65 and 3.1 < v.z < 5.4))
        print('PROBE', X, near)
