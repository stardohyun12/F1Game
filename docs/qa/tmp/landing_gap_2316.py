# 2316 house_home 2층 계단참 통로 최소 폭 — 내보낸 FBX 를 다시 읽어 잰다(생성기 숫자가 아니라 납품물 기준).
# 왼쪽 = 난간(RAIL_02·RAIL_STAIRS), 오른쪽 = 책장 SHELF_04 + 동쪽 벽 안쪽 면(x 4.15). 바닥(z 3.2) 위 5cm~1.8m 꼭짓점만, 위에서 내려다본 2D 최소 거리.
# blender -b -P landing_gap_2316.py -- <fbx>
import bpy, sys, math
fbx = sys.argv[sys.argv.index('--') + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx, axis_forward='-Z', axis_up='Y')
H2, XI = 3.2, 4.15
def pts(names, box):
    out = []
    for o in bpy.data.objects:
        if o.type != 'MESH' or not any(o.name.startswith(n) for n in names): continue
        for v in o.data.vertices:
            p = o.matrix_world @ v.co
            if H2 + .05 < p.z < H2 + 1.8 and box[0] <= p.x <= box[1] and box[2] <= p.y <= box[3]: out.append((p.x, p.y, o.name))
    return out
L = pts(('RAIL_02', 'RAIL_STAIRS'), (2.0, 4.2, 1.6, 2.0))
Rs = pts(('SHELF_04',), (3.0, 4.2, .5, 2.0))
print('MEAS nL', len(L), 'nR', len(Rs), 'rail east end x', round(max(p[0] for p in L), 3))
best = min(((math.hypot(a[0] - b[0], a[1] - b[1]), a, b) for a in L for b in Rs), key=lambda t: t[0])
wall = min(((XI - a[0]), a) for a in L)
print('MEAS rail-shelf', round(best[0], 3), 'at', [round(c, 3) for c in best[1][:2]], best[1][2], '->', [round(c, 3) for c in best[2][:2]], best[2][2])
print('MEAS rail-wall', round(wall[0], 3), wall[1][2])
print('MEAS PASSAGE_MIN', round(min(best[0], wall[0]), 3))
