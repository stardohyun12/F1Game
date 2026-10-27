"""j판 FBX 의 UV 층 순서 확인 (Unity 는 첫 UV 층을 씀)."""
import bpy, sys
d = sys.argv[sys.argv.index('--') + 1]
for n in ['house_shop', 'house_narrow_a']:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=f'D:/Github/F1Game/art/export/{d}/{n}.fbx')
    for o in bpy.data.objects:
        if o.type == 'MESH': print('UVL', n, o.name, [u.name for u in o.data.uv_layers], len(o.data.polygons), [m.name for m in o.data.materials], flush=True)
