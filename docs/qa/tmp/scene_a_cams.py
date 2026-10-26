import bpy
for o in bpy.data.objects:
    if o.type == 'CAMERA':
        print('CAM', o.name, tuple(round(v, 2) for v in o.matrix_world.translation), tuple(round(v * 57.2958, 1) for v in o.matrix_world.to_euler()), round(o.data.angle * 57.2958, 1), o.data.sensor_fit)
for c in bpy.data.collections:
    print('COL', c.name, len(c.objects))
