# artlive 1914 (D-196): 건물별로 Shared_wall·Shared_roof 면이 atlas 어느 칸(512 의 4분면)을 쓰는지 센다.
# blender -b --factory-startup -P docs/qa/tmp/cells_1914.py -- <fbx>
import bpy, sys, collections
fbx = sys.argv[sys.argv.index('--') + 1]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=fbx)
Q = {(0, 0): 'TL(apricot/E2)', (1, 0): 'TR(cream/E3)', (0, 1): 'BL(mint/F2)', (1, 1): 'BR(apricotB/F3)'}
out = collections.defaultdict(collections.Counter)
for o in bpy.data.objects:
    if o.type != 'MESH' or not o.data.uv_layers: continue
    me = o.data; uv = me.uv_layers.active.data
    for p in me.polygons:
        mn = me.materials[p.material_index].name.split('.')[0] if me.materials else ''
        if mn not in ('Shared_wall', 'Shared_roof'): continue
        u = sum(uv[l].uv.x for l in p.loop_indices) / p.loop_total % 1; v = sum(uv[l].uv.y for l in p.loop_indices) / p.loop_total % 1
        kind = 'PARCEL' if o.name.startswith('PARCEL') else o.name
        out[(mn, kind)][Q[(int(u * 2), int((1 - v) * 2))]] += p.area
for k in sorted(out): print('[cells]', k, {q: round(a, 1) for q, a in out[k].items()})
sys.stdout.flush(); import os; os._exit(0)
