"""해안 집 6채 미리보기 — 길 양옆에 줄지어 세우고 카메라 B(뒤 5.6·높이 2.8·FOV 64)로 1장 + 집마다 3/4 근접 1장 (아트2, 과제 1232).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_preview.py
unlit(Emission) + Standard 색 변환 — 텍스처 색 그대로. 길·인도·땅은 미리보기용 단색 판. 안개 없음.
"""
import bpy, math, os
from mathutils import Vector

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
EXP = J('art', 'export', 'scene-a4-houses'); REV = J('art', '_review', '2026-10-27-houses'); os.makedirs(REV, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene; sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
sc.view_settings.view_transform = 'Standard'; sc.render.filter_size = .5
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (.62 ** 2.2, .76 ** 2.2, .88 ** 2.2, 1)


def emis(name, rgb=None, png=None):
    m = bpy.data.materials.new(name); m.use_nodes = True; n = m.node_tree.nodes; n.clear(); L = m.node_tree.links
    e = n.new('ShaderNodeEmission'); o = n.new('ShaderNodeOutputMaterial'); L.new(e.outputs[0], o.inputs[0])
    if png:
        t = n.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(png); t.interpolation = 'Closest'; L.new(t.outputs[0], e.inputs[0])
    else: e.inputs[0].default_value = (*[(c / 255) ** 2.2 for c in rgb], 1)
    return m


def plane(x0, x1, y0, y1, z, rgb):
    me = bpy.data.meshes.new('p'); me.from_pydata([(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], [], [(0, 1, 2, 3)])
    o = bpy.data.objects.new('p', me); sc.collection.objects.link(o); me.materials.append(emis('m', rgb))


plane(-80, 80, -20, 160, -.02, (150, 158, 87)); plane(-3, 3, -20, 160, 0, (154, 151, 134)); plane(-4.5, -3, -20, 160, .05, (207, 204, 186)); plane(3, 4.5, -20, 160, .05, (207, 204, 186))


def house(name, x, y, rot):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=os.path.join(EXP, name + '.fbx'))
    o = [o for o in bpy.data.objects if o not in before and o.type == 'MESH'][0]
    o.data.materials.clear(); o.data.materials.append(emis('h_' + name, png=os.path.join(EXP, name + '.png')))
    o.location = (x, y, 0); o.rotation_euler = (0, 0, math.radians(rot)); bpy.context.view_layer.update()
    pts = [o.matrix_world @ Vector(c) for c in o.bound_box]; return o, pts


DIMS = dict(house_narrow_a=(5, 6), house_narrow_b=(4, 5), house_shop=(6, 6), house_slope=(6, 5), house_warehouse=(8, 7), house_cafe=(6, 4))
placed = {}
y = 1.0
for nm in ('house_shop', 'house_narrow_a', 'house_narrow_b', 'house_warehouse'):  # 길 왼쪽, 정면이 +x(길)
    W, D = DIMS[nm]; placed[nm] = house(nm, -4.6 - D / 2 - .1, y + W / 2, 90); y += W + .5
y = 3.0
for nm in ('house_cafe', 'house_slope', 'house_narrow_a', 'house_shop'):  # 길 오른쪽, 정면이 -x
    W, D = DIMS[nm]; o, pts = house(nm, 4.6 + D / 2 + .1, y + W / 2, -90); placed.setdefault(nm, (o, pts)); y += W + .5
cd = bpy.data.cameras.new('c'); cam = bpy.data.objects.new('c', cd); sc.collection.objects.link(cam); sc.camera = cam


def shot(path, loc, aim, fov, res):
    cam.location = loc; cd.angle_y = math.radians(fov); cd.sensor_fit = 'VERTICAL'
    cam.rotation_euler = (Vector(aim) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler(); bpy.context.view_layer.update()
    sc.render.resolution_x, sc.render.resolution_y = res; sc.render.filepath = path; bpy.ops.render.render(write_still=True)


shot(os.path.join(REV, 'street_B.png'), (0, -5.6, 2.8), (0, 6, 1.0), 64, (1280, 720))
ALL = [ob for ob in sc.objects if ob.type == 'MESH' and ob.data.materials and ob.data.materials[0].name.startswith('h_')]
for nm in DIMS:  # 집마다 3/4 근접(길 가운데 약간 앞에서 올려다봄) — 다른 집은 숨김
    o, pts = placed[nm]; c = sum(pts, Vector()) / 8; hgt = max(p.z for p in pts)
    for ob in ALL: ob.hide_render = ob is not o
    side = 1 if c.x < 0 else -1
    shot(os.path.join(REV, f'close_{nm}.png'), (c.x + side * 9.5, c.y - 7.5, 2.2), (c.x, c.y, hgt * .45), 50, (480, 600))
print('PREVIEW OK', flush=True)
