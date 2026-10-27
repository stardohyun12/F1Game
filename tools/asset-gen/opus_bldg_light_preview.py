"""건물 밝히기 전후 미리보기 — 덩어리 장면에 원경 far_town + 배경 건물 4채를 놓고 카메라 B(뒤 5.6·높이 2.8·FOV 64)로 렌더 (아트2, 과제 1134).
    D:/Tools/Blender/blender.exe -b art/models/opus_scene_a2_blockout.blend --python tools/asset-gen/opus_bldg_light_preview.py -- before|after
배경 건물 자리는 미리보기용 임시(03 판잣집만 programmer-0601 의 '길 오른쪽 중경 y 33' 를 따름). 안개는 끔(공기 원근이 텍스처에 들어 있는지 보려고).
"""
import bpy, math, os, sys
from mathutils import Vector

MODE = sys.argv[sys.argv.index('--') + 1]
R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
REV = J('art', '_review', '2026-10-27-bldg-light'); os.makedirs(REV, exist_ok=True)
SRC = dict(before=dict(far=J('art', 'export', 'scene-a2-far'), kit=J('art', 'export', 'scene-a2-kit'), farpng='far_town_baked32.png', bg='background_building_0{}.png'),
           after=dict(far=J('art', 'export', 'scene-a2-bldg-light'), kit=J('art', 'export', 'scene-a2-bldg-light'), farpng='far_town_baked32.png', bg='background_building_0{}.png'))[MODE]
sc = bpy.context.scene
sc.world.node_tree.nodes['Background'].inputs[0].default_value = (.62, .76, .88, 1)  # 하늘(A2f 컷처럼 맑은 하늘)
for m in bpy.data.materials:
    if 'layer' not in m: continue
    n = m.node_tree.nodes; e = n.get('Emission')
    if not e: continue
    if 'tex' in m:
        t = next(x for x in n if x.type == 'TEX_IMAGE'); m.node_tree.links.new(t.outputs[0], e.inputs[0])
    else: e.inputs[0].default_value = (*m['look'], 1)
for o in sc.objects:
    if o.name.startswith('town_') or o.get('layer') == 'mid' and o.type == 'MESH' and 'house' in o.name.lower(): o.hide_render = True


def h(x, y):
    yy = max(0, y - 4); z = -min(yy, 60) * .13 - max(0, yy - 60) * .03; z -= .05 * max(0, x - 8) * min(1, yy / 10)
    z += .22 * max(0, -x - 9) * min(1, (y + 15) / 20); z -= max(0, y - 130) * .08; return z


def imp(path, png, loc, rot=0, split=None):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    mats = {}
    for o in new:
        p = split(o) if split else png
        if p not in mats:
            m = bpy.data.materials.new('prev_' + p); m.use_nodes = True; n = m.node_tree.nodes; n.clear(); L = m.node_tree.links
            t = n.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(p); t.interpolation = 'Closest'
            e = n.new('ShaderNodeEmission'); out = n.new('ShaderNodeOutputMaterial'); L.new(t.outputs[0], e.inputs[0]); L.new(e.outputs[0], out.inputs[0]); mats[p] = m
        o.data.materials.clear(); o.data.materials.append(mats[p])
        o.location += Vector(loc); o.rotation_euler.z += math.radians(rot)
    return new


imp(os.path.join(SRC['far'], 'far_town.fbx'), None, (16, 78, -8.6),
    split=lambda o: os.path.join(SRC['far'], 'far_terrace_baked32.png' if 'terrace' in o.name else SRC['farpng']))
BG = {1: (-20, 52, 15), 2: (30, 58, -70), 3: (14, 33, 95), 4: (6, 46, 10)}
for i, (x, y, r) in BG.items():
    imp(os.path.join(SRC['kit'], f'background_building_0{i}.fbx'), os.path.join(SRC['kit'], SRC['bg'].format(i)), (x, y, h(x, y) - .2), r)
cam = sc.camera
shots = dict(B=((0, -5.6, 2.8), 64, (0, 6, 1.0)), tele=((4, 12, 3.5), 34, (12, 50, -2)))
for k, (loc, fov, aim) in shots.items():
    cam.location = loc; cam.data.angle_y = math.radians(fov)
    cam.rotation_euler = (Vector(aim) - cam.location).to_track_quat('-Z', 'Y').to_euler(); bpy.context.view_layer.update()
    sc.render.filepath = os.path.join(REV, f'{MODE}_{k}.png'); bpy.ops.render.render(write_still=True)
print('PREVIEW', MODE, flush=True)
