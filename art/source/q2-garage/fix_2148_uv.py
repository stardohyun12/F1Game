# artlive 2148: UV 축척을 원본 garage.blend 에서 맞춘다(교훈 4·8). build_asset.py garage 의 overlap 단계가 부른다. 한 번만(표시 q2_uv_2148).
#  - 면마다 자기 UV 가운데를 기준으로 키우고/줄인 뒤, 그 물건의 원래 UV 사각형 안으로 밀어 넣는다 → 같은 텍스처 자리를 다른 밀도로.
#    drum_lying 43→56px/m(다른 드럼과 같게) · wall_±1_planks_dark 64→32(벽 아래 판자 띠 12m, 건물 = 세상 32) · trest±0.7 33→64(탁자 받침 0.8m = 작은 물건)
#  - sign_main 옆 두께면(UV 가 점 하나, 붕괴 15%): 면의 긴 변 → u, 두께 → v 로 64px/m, 간판 칸 위 가장자리 띠에.
# D:/Tools/Blender/blender.exe -b --factory-startup -P art/source/q2-garage/fix_2148_uv.py
import bpy, os, sys
from mathutils import Vector
R = 'D:/Github/F1Game'
bpy.ops.wm.open_mainfile(filepath=R + '/art/source/q2-garage/garage.blend')
SCALE = {'drum_lying': 56 / 43, 'wall_-1_planks_dark': 0.5, 'wall_1_planks_dark': 0.5, 'trest-0.7': 2.0, 'trest0.7': 2.0}
def img_wh(o):
    m = o.data.materials[0]; im = next((n.image for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image), None); return im.size if im else (1, 1)
log = {}
for nm, k in SCALE.items():
    o = bpy.data.objects[nm]
    if o.get('q2_uv_2148'): continue
    uv = o.data.uv_layers.active.data; allu = [uv[i].uv.copy() for p in o.data.polygons for i in p.loop_indices]
    lo = Vector((min(u.x for u in allu), min(u.y for u in allu))); hi = Vector((max(u.x for u in allu), max(u.y for u in allu)))
    for p in o.data.polygons:
        li = list(p.loop_indices); c = sum((uv[i].uv for i in li), Vector((0, 0))) / len(li)
        for i in li: uv[i].uv = c + (uv[i].uv - c) * k
        a = Vector((min(uv[i].uv.x for i in li), min(uv[i].uv.y for i in li))); b = Vector((max(uv[i].uv.x for i in li), max(uv[i].uv.y for i in li)))
        sh = Vector((max(0, lo.x - a.x) - max(0, b.x - hi.x), max(0, lo.y - a.y) - max(0, b.y - hi.y)))  # 사각형 밖으로 나간 만큼 되밀기
        for i in li: uv[i].uv += sh
    o['q2_uv_2148'] = 1; log[nm] = round(k, 3)
o = bpy.data.objects['sign_main']
if not o.get('q2_sign_2148c'):  # b: 첫 판은 "UV 1px 미만" 으로 골라 붕괴 면을 놓침 → 검사기와 같이 4px/m 미만
    W, H = img_wh(o); uv = o.data.uv_layers.active.data; M = o.matrix_world; PPM = 64
    allu = [uv[i].uv.copy() for p in o.data.polygons for i in p.loop_indices]
    u0 = min(u.x for u in allu); v1 = max(u.y for u in allu); n = 0
    big2 = {p.index for p in sorted(o.data.polygons, key=lambda p: -p.area)[:2]}
    if o.get('q2_sign_2148b'): u0 = 0.0  # b 판이 옆면 UV 를 u0 기준으로 이미 옮겼어도 같은 자리(간판 칸 왼쪽 = 0)
    for p in o.data.polygons:
        li = list(p.loop_indices); us = [uv[i].uv for i in li]
        ws = [M @ o.data.vertices[v].co for v in p.vertices]
        sa = abs(sum(us[j].x * us[(j + 1) % len(us)].y - us[(j + 1) % len(us)].x * us[j].y for j in range(len(us))) / 2)
        wa = sum(((ws[0] - ws[j]).cross(ws[0] - ws[j + 1])).length / 2 for j in range(1, len(ws) - 1))
        if p.index in big2: continue  # 옆 두께면 4개만(앞·뒤 = 가장 큰 두 면). 원래 넷 다 4px/m 미만 붕괴였음(1.27/8.15m² = 15%)
        ext = [max(w[j] for w in ws) - min(w[j] for w in ws) for j in range(3)]
        ax = sorted(range(3), key=lambda j: -ext[j]); a_, b_ = ax[0], ax[1]  # 긴 변, 두께
        mn = [min(w[j] for w in ws) for j in range(3)]
        for i in li:
            w = M @ o.data.vertices[o.data.loops[i].vertex_index].co; uv[i].uv = (u0 + (w[a_] - mn[a_]) * PPM / W, v1 - (w[b_] - mn[b_]) * PPM / H)
        us = [uv[i].uv for i in li]
        if sum(us[j].x * us[(j + 1) % len(us)].y - us[(j + 1) % len(us)].x * us[j].y for j in range(len(us))) < 0:  # 뒤집힌 면(거울) → u 를 되돌림
            for i in li: uv[i].uv.x = 2 * u0 + ext[a_] * PPM / W - uv[i].uv.x
        n += 1
    o['q2_sign_2148c'] = 1; log['sign_main_edges'] = n
print('UV2148', log); sys.stdout.flush()  # 2201: os._exit 는 버퍼를 안 비워서, 파이프로 받으면 이 줄이 build 로그에 안 남았다
bpy.ops.wm.save_as_mainfile(filepath=R + '/art/source/q2-garage/garage.blend'); os._exit(0)
