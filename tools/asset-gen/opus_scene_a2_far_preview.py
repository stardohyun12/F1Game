"""장면 A2 미리보기 — 덩어리 장면(opus_scene_a2_blockout.blend)의 원경 town_* 을 빼고 far_town.fbx·yard_ground.fbx 를 넣어 카메라 B(게임)·D(비교) 렌더 (아트2, 과제 0525).
안개는 굽지 않았으므로 미리보기에서만 Emission 에 카메라 거리 안개(12→140m, 최대 .65, 크림 .86/.85/.80)를 섞는다 — Unity 장면 안개 흉내.
    D:/Tools/Blender/blender.exe -b art/models/opus_scene_a2_blockout.blend --python tools/asset-gen/opus_scene_a2_far_preview.py
"""
import bpy, math, os
from mathutils import Vector

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
EXP = J('art', 'export', 'scene-a2-far'); REV = J('art', '_review', '2026-10-27-scene-a2-far'); os.makedirs(REV, exist_ok=True)
CAMS = {'B': (5.6, 2.8, 64, 6, 1.0), 'D': (3.9, 2.0, 64, 5, .3)}
FOG, FOG0, FOG1, FOGMAX = (.86, .85, .80), 12, 140, .65
sc = bpy.context.scene
# 덩어리 장면을 'look' 색으로 되돌림(저장 때 id 색이었음)
sc.world.node_tree.nodes['Background'].inputs[0].default_value = (*FOG, 1)
for m in bpy.data.materials:
    if 'layer' not in m: continue
    n = m.node_tree.nodes; e = n.get('Emission')
    if not e: continue
    if 'tex' in m:
        t = next(x for x in n if x.type == 'TEX_IMAGE'); m.node_tree.links.new(t.outputs[0], e.inputs[0])
    else: e.inputs[0].default_value = (*m['look'], 1)
for o in sc.objects:
    if o.name.startswith('town_'): o.hide_render = True


def imp(name, loc):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=os.path.join(EXP, name))
    new = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    for o in new: o.location += Vector(loc)
    return new


far = imp('far_town.fbx', (16, 78, -8.6)); yard = imp('yard_ground.fbx', (0, 0, .06))
cam = sc.camera
for obs, png in (([o for o in far if 'terrace' not in o.name], 'far_town_baked32.png'), ([o for o in far if 'terrace' in o.name], 'far_terrace_baked32.png'), (yard, 'yard_ground_baked32.png')):  # unlit + 거리 안개(재질은 새로 짠다)
    m = bpy.data.materials.new('prev_' + png); m.use_nodes = True; nt = m.node_tree; n = nt.nodes; n.clear(); L = nt.links
    t = n.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(os.path.join(EXP, png)); t.interpolation = 'Closest'
    out = n.new('ShaderNodeOutputMaterial'); e = n.new('ShaderNodeEmission'); L.new(e.outputs[0], out.inputs[0])
    for o in obs:
        o.data.materials.clear(); o.data.materials.append(m)
    cd = n.new('ShaderNodeCameraData'); mr = n.new('ShaderNodeMapRange'); mr.inputs[1].default_value = FOG0; mr.inputs[2].default_value = FOG1
    mr.inputs[3].default_value = 0; mr.inputs[4].default_value = FOGMAX; mr.clamp = True
    mx = n.new('ShaderNodeMix'); mx.data_type = 'RGBA'; mx.inputs[7].default_value = (*FOG, 1)
    L.new(cd.outputs['View Distance'], mr.inputs[0]); L.new(mr.outputs[0], mx.inputs[0]); L.new(t.outputs[0], mx.inputs[6]); L.new(mx.outputs[2], e.inputs[0])
    e.inputs[1].default_value = 1
shots = dict(CAMS)
for k, (dist, hgt, fov, ahead, th) in shots.items():
    cam.location = (0, -dist, hgt); cam.data.angle_y = math.radians(fov)
    cam.rotation_euler = (Vector((0, ahead, th)) - cam.location).to_track_quat('-Z', 'Y').to_euler(); bpy.context.view_layer.update()
    sc.render.filepath = os.path.join(REV, f'look_{k}.png'); bpy.ops.render.render(write_still=True)
# 원경 가까이(망원, 안개 그대로) — 집 하나하나가 읽히는지
cam.location = (2, 30, 4); cam.data.angle_y = math.radians(26)
cam.rotation_euler = (Vector((16, 92, -2)) - cam.location).to_track_quat('-Z', 'Y').to_euler(); bpy.context.view_layer.update()
sc.render.filepath = os.path.join(REV, 'far_tele.png'); bpy.ops.render.render(write_still=True)
# 마당 위에서(카트 앞 바닥 무늬 크기 확인)
cam.location = (0, -2, 7); cam.data.angle_y = math.radians(70)
cam.rotation_euler = (Vector((0, -2.3, 0)) - cam.location).to_track_quat('-Z', 'Y').to_euler(); bpy.context.view_layer.update()
sc.render.filepath = os.path.join(REV, 'yard_top.png'); bpy.ops.render.render(write_still=True)
print('PREVIEW OK', len(far), len(yard), flush=True)
