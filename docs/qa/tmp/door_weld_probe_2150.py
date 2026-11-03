"""2150: 내보낸 FBX 에서 주인공 방 입구 서쪽 문설주 모서리(x 2.55, y 0.5) 꼭짓점 수 — 붙임(weld)이 됐는지."""
import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/Github/F1Game/art/export/house-home/house_home.fbx')
for o in bpy.data.objects:
    if o.name != 'HOUSE_shell': continue
    vs = [o.matrix_world @ v.co for v in o.data.vertices]
    near = [v for v in vs if abs(abs(v.x) - 2.55) < .005 and abs(abs(v.y) - 0.5) < .005 and v.z > 3.0 and v.z < 5.5]
    print('PROBE verts', len(vs), 'at jamb corner', len(near), sorted(set((round(v.x, 3), round(v.y, 3), round(v.z, 3)) for v in near)))
    near2 = [v for v in vs if abs(abs(v.x) - 2.55) < .005 and abs(abs(v.z) - 0.5) < .005]
    print('PROBE alt-axis', len(near2))
