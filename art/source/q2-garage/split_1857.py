# artlive 1857: 정비소를 한 메시(garage_v5_combo)로 합치지 않고 물건별 오브젝트로 내보낸다 (도현 뷰어 코멘트 2/2, D-195 lessons 15).
#  - garage.blend 의 낱개 부품을 물건 단위로 묶어 이름 = 종류_번호 (TIRE_01, DRUM_01 …), 피벗 = 바닥 가운데.
#  - 나머지 건물 몸(벽·바닥·지붕·기둥·셔터 틀 등)은 GARAGE_shell 한 개.
#  - 1846 에서 export fbx 에만 넣었던 타이어 이동(코멘트 183800-oih2, tD0·tD1)을 원본 garage.blend 에도 넣는다(한 번만, 표시 q2_moved_1846).
#  - 파일 이름은 그대로 garage_v5_combo.fbx (라이브러리·뷰어 경로 유지). LAMP_0/1 빈 객체 포함.
# D:/Tools/Blender/blender.exe -b --factory-startup -P art/source/q2-garage/split_1857.py
import bpy, os, sys, re
from mathutils import Vector, Matrix
R = 'D:/Github/F1Game'; E = R + '/art/export/q2-garage'
bpy.ops.wm.open_mainfile(filepath=R + '/art/source/q2-garage/garage.blend')
O = bpy.data.objects
for n, d in (('tD1', (-1.861, -0.297, -0.235)), ('tD0', (-1.854, -0.297, 0.295))):
    if not O[n].get('q2_moved_1846'):
        O[n].location += Vector(d); O[n]['q2_moved_1846'] = 1; print('MOVED', n, d)
bpy.ops.wm.save_as_mainfile(filepath=R + '/art/source/q2-garage/garage.blend')

obs = [o for o in bpy.context.scene.objects if o.type == 'MESH' and not o.get('q2_before_only') and not o.hide_render]
G = {}
def put(k, o): G.setdefault(k, []).append(o)
RULES = [  # (정규식, 종류) — 한 오브젝트 = 한 물건
    (r'^t[A-Z]\d$|^tLean$|^tWall$', 'TIRE'), (r'^drum', 'DRUM'), (r'^can\d', 'CAN'), (r'^i?crate\d', 'CRATE'),
    (r'^part-?\d+$', 'PART'), (r'^part_box$', 'PARTBOX'), (r'^toolbox$', 'TOOLBOX'), (r'^vise$', 'VISE'),
    (r'^poster[A-C]$', 'POSTER'), (r'^record$', 'BOARD'), (r'^pegboard$', 'PEGBOARD'),
]
GROUPS = [  # (종류_번호, 부품 이름 정규식) — 여러 부품 = 한 물건
    ('CHAIR_01', r'^(chair_|owner_chair_)'), ('HELMET_01', r'^(old_race_helmet|helmet_)'), ('KEYS_01', r'^key_'),
    ('BENCH_01', r'^(bench|leg4\.)'), ('TABLE_01', r'^(table|trest)'), ('ENGINE_01', r'^engine'),
    ('SHELF_01', r'^(shelf-1|shpost-1)'), ('SHELF_02', r'^(shelf1|shpost1)'), ('LADDER_01', r'^(lad-|rung)'),
    ('HOIST_01', r'^(hoist_beam|chain|hook)$'), ('LAMP_01', r'^(bulb0|cable0|shade0|lamp_glow0)$'), ('LAMP_02', r'^(bulb1|cable1|shade1|lamp_glow1)$'),
    ('SLOTS_01', r'^slots'), ('FESTIVAL_01', r'^festival_'), ('PHOTO_01', r'^(race_photo_0|memory_frame_0)$'),
    ('PHOTO_02', r'^(race_photo_1|memory_frame_1)$'), ('PHOTO_03', r'^(race_photo_2|memory_frame_2)$'),
    ('SIGN_01', r'^sign_'), ('DOOR_01', r'^pdoor'), ('SHUTTER_01', r'^sh_'),
]
cnt = {}
for o in sorted(obs, key=lambda o: o.name):
    k = next((g for g, rx in GROUPS if re.match(rx, o.name)), None)
    if not k:
        t = next((t for rx, t in RULES if re.match(rx, o.name)), None)
        if t: cnt[t] = cnt.get(t, 0) + 1; k = '%s_%02d' % (t, cnt[t])
    put(k or 'GARAGE_shell', o)

made = []
bpy.ops.object.select_all(action='DESELECT')
for k, L in sorted(G.items()):
    for o in L: o.select_set(True)
    bpy.context.view_layer.objects.active = L[0]; bpy.ops.object.duplicate()
    if len(L) > 1: bpy.ops.object.join()
    j = bpy.context.object; j.name = k; j.data.name = k
    ws = [j.matrix_world @ v.co for v in j.data.vertices]
    lo = Vector([min(w[i] for w in ws) for i in range(3)]); hi = Vector([max(w[i] for w in ws) for i in range(3)])
    piv = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z)) if k != 'GARAGE_shell' else Vector((0, 0, 0))
    bpy.context.scene.cursor.location = piv; bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    j.select_set(False); made.append(j)
    print('ITEM', k, len(L), [o.name for o in L][:6], [round(c, 2) for c in piv])
    bpy.ops.object.select_all(action='DESELECT')
for j in made: j.select_set(True)
for e in [O['LAMP_0'], O['LAMP_1']]: e.select_set(True)
bpy.ops.export_scene.fbx(filepath=E + '/garage_v5_combo.fbx', use_selection=True, object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y', path_mode='COPY', bake_anim=False)
print('EXPORT ok', len(made)); sys.stdout.flush(); os._exit(0)
