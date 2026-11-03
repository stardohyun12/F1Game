import bpy, sys
a = sys.argv[sys.argv.index('--') + 1:]
if a[0].endswith('.fbx'):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=a[0])
else:
    bpy.ops.wm.open_mainfile(filepath=a[0])
print('NAMES', sorted(o.name for o in bpy.context.scene.objects if o.type in ('MESH', 'EMPTY') and not o.get('q2_before_only') and not o.hide_render))
