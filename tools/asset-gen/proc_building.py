"""Alba 결(지중해 항구 마을) 건물 생성기 — D-095 (2026-10-13).

입력(파라미터/시드) → Blender 프리미티브로 한 채 조립 → FBX + GLB.
규칙(D-095): 2~3층 · 1층 문/창/차양은 사람 크기 · 집마다 하나씩 다르게 · 플랫 색 · 텍스처 없음 · 구역 팔레트.

사용:
    blender -b -P tools/asset-gen/proc_building.py -- --out out/bld --count 4 --zone harbor --seed 1
    (한 채만: --count 1 --w 7 --d 6 --floors 3 --roof flat --balcony --awning --shop)
출력: out/bld/<zone>_<i>.fbx/.glb, 콘솔 REPORT 줄(tris, 치수, 옵션)

Unity 쪽: FBX 머티리얼 이름(Wall/Trim/Shutter/Roof/Door/Glass/Awning/Metal)로 팔레트 머티리얼에 매핑한다.
"""
import bpy, sys, os, math, random, json
from mathutils import Vector

# ---------- 팔레트 (D-094 구역색과 맞춤) ----------
PALETTES = {
    "harbor": {
        "walls":    [(0xF6, 0xF1, 0xE4), (0xE9, 0xDF, 0xC6), (0xE6, 0xCF, 0x9F), (0xF2, 0xE6, 0xD2), (0xDD, 0xD3, 0xB8)],
        "shutters": [(0x2C, 0x3E, 0x57), (0x2E, 0x6F, 0x7A), (0x4A, 0x7C, 0x59), (0xC9, 0x5B, 0x43), (0x3B, 0x5B, 0x8C)],
        "trim":     [(0xFB, 0xF8, 0xF0)],
        "roof":     [(0xB8, 0x5C, 0x3E), (0xC2, 0x6A, 0x4A), (0xA8, 0x55, 0x3C)],
        "door":     [(0x5B, 0x3A, 0x26), (0x2C, 0x3E, 0x57), (0x2E, 0x6F, 0x7A)],
        "awning":   [(0xD9, 0x5B, 0x43), (0x2E, 0x6F, 0x7A), (0xE0, 0xA0, 0x3A)],
    },
    "downtown": {
        "walls":    [(0xE9, 0xDC, 0xC2), (0xF3, 0xEA, 0xD6), (0xD8, 0xC7, 0xA6), (0xE2, 0xB8, 0x9A)],
        "shutters": [(0x2E, 0x5F, 0x6A), (0x3E, 0x4E, 0x5E), (0x6A, 0x8E, 0x6A)],
        "trim":     [(0xFB, 0xF6, 0xEA)],
        "roof":     [(0x5A, 0x5E, 0x66), (0x2E, 0x5F, 0x6A)],
        "door":     [(0x2E, 0x5F, 0x6A), (0x4B, 0x3A, 0x2A)],
        "awning":   [(0xE0, 0x7A, 0x4F), (0x2E, 0x5F, 0x6A)],
    },
    "industrial": {
        "walls":    [(0xB8, 0xA5, 0x79), (0xC9, 0xB8, 0x92), (0xA3, 0x92, 0x6B)],
        "shutters": [(0x8A, 0x4A, 0x33), (0x55, 0x5A, 0x4E)],
        "trim":     [(0xEB, 0xE3, 0xCE)],
        "roof":     [(0x6B, 0x5A, 0x4A), (0x8A, 0x4A, 0x33)],
        "door":     [(0x55, 0x5A, 0x4E), (0x8A, 0x4A, 0x33)],
        "awning":   [(0xE0, 0x8A, 0x3A)],
    },
}
GLASS = (0xC9, 0xE4, 0xE4)
METAL = (0x8A, 0x8F, 0x96)
FLOOR_H = 3.0

def c01(rgb): return tuple(v / 255.0 for v in rgb)

_mats = {}
def mat(name, rgb):
    key = name
    if key in _mats: return _mats[key]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*c01(rgb), 1)
    bsdf.inputs["Roughness"].default_value = 0.85
    m.diffuse_color = (*c01(rgb), 1)
    _mats[key] = m
    return m

parts = []
def box(name, size, loc, m, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object
    o.name = name; o.scale = size; o.rotation_euler = rot
    o.data.materials.append(m); parts.append(o); return o

def wedge_roof(name, w, d, z, h, overhang, m):
    """맞배(gable) 지붕: 앞뒤로 경사. 두 면 + 박공 삼각 2."""
    import bmesh
    me = bpy.data.meshes.new(name); o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    bm = bmesh.new()
    hw, hd = w / 2 + overhang, d / 2 + overhang
    v = [bm.verts.new(p) for p in [(-hw, -hd, z), (hw, -hd, z), (hw, hd, z), (-hw, hd, z), (-hw, 0, z + h), (hw, 0, z + h)]]
    bm.faces.new((v[0], v[1], v[5], v[4]))  # 앞 경사
    bm.faces.new((v[3], v[4], v[5], v[2]))  # 뒤 경사
    bm.faces.new((v[0], v[4], v[3]))        # 좌 박공
    bm.faces.new((v[1], v[2], v[5]))        # 우 박공
    bm.faces.new((v[0], v[3], v[2], v[1]))  # 바닥(안 보임, 닫힘용)
    bm.to_mesh(me); bm.free()
    o.data.materials.append(m); parts.append(o); return o

def window(x, y, z, facing, wall_m, shutter_m, trim_m, glass_m, shutters=True, sill=True, planter=False, planter_m=None):
    """facing: 0=앞(-y), 1=뒤(+y), 2=좌(-x), 3=우(+x). 창 1.0×1.3m, 문틀 0.08, 덧창 0.45×1.3."""
    W, H, T = 1.0, 1.3, 0.08
    out = {0: (0, -1), 1: (0, 1), 2: (-1, 0), 3: (1, 0)}[facing]
    rot = (0, 0, 0) if facing in (0, 1) else (0, 0, math.pi / 2)
    ox, oy = out
    # 유리(살짝 안쪽) + 틀(바깥 돌출)
    box("Glass", (W, 0.05, H) if facing in (0, 1) else (0.05, W, H), (x - ox * 0.02, y - oy * 0.02, z), glass_m)
    frame_t = (W + 2 * T, 0.12, H + 2 * T) if facing in (0, 1) else (0.12, W + 2 * T, H + 2 * T)
    fr = box("Frame", frame_t, (x + ox * 0.04, y + oy * 0.04, z), trim_m)
    # 유리 자리 뚫지 않고(불리언 X) 틀을 네 조각으로 — 폴리 절약 + 플랫 룩
    bpy.data.objects.remove(fr, do_unlink=True); parts.remove(fr)
    for (dx, dz, sx, sz) in [(0, H / 2 + T / 2, W + 2 * T, T), (0, -H / 2 - T / 2, W + 2 * T, T), (-W / 2 - T / 2, 0, T, H), (W / 2 + T / 2, 0, T, H)]:
        if facing in (0, 1): box("Frame", (sx, 0.12, sz), (x + dx, y + oy * 0.04, z + dz), trim_m)
        else: box("Frame", (0.12, sx, sz), (x + ox * 0.04, y + dx, z + dz), trim_m)
    if shutters:
        for s in (-1, 1):
            off = W / 2 + T + 0.25
            if facing in (0, 1): box("Shutter", (0.45, 0.06, H), (x + s * off, y + oy * 0.06, z), shutter_m)
            else: box("Shutter", (0.06, 0.45, H), (x + ox * 0.06, y + s * off, z), shutter_m)
    if sill:
        if facing in (0, 1): box("Sill", (W + 0.4, 0.22, 0.08), (x, y + oy * 0.1, z - H / 2 - 0.06), trim_m)
        else: box("Sill", (0.22, W + 0.4, 0.08), (x + ox * 0.1, y, z - H / 2 - 0.06), trim_m)
    if planter:
        if facing in (0, 1): box("Planter", (W, 0.3, 0.3), (x, y + oy * 0.2, z - H / 2 - 0.25), planter_m)
        else: box("Planter", (0.3, W, 0.3), (x + ox * 0.2, y, z - H / 2 - 0.25), planter_m)

def build(rng, zone, w, d, floors, roof, balcony, awning, shop, idx):
    global parts; parts = []
    P = PALETTES[zone]
    m_wall = mat(f"Wall_{idx}", rng.choice(P["walls"]))
    m_shut = mat(f"Shutter_{idx}", rng.choice(P["shutters"]))
    m_trim = mat("Trim", P["trim"][0])
    m_roof = mat(f"Roof_{idx}", rng.choice(P["roof"]))
    m_door = mat(f"Door_{idx}", rng.choice(P["door"]))
    m_awn = mat(f"Awning_{idx}", rng.choice(P["awning"]))
    m_glass = mat("Glass", GLASS)
    m_metal = mat("Metal", METAL)
    m_plant = mat("Plant", (0x4A, 0x7C, 0x59))

    H = floors * FLOOR_H
    # 몸통 (두 칸으로 나누면 층 띠가 생겨 실루엣이 산다)
    box("Body", (w, d, H), (0, 0, H / 2), m_wall)
    # 기단(바닥 띠) + 층 사이 코니스
    box("Base", (w + 0.2, d + 0.2, 0.35), (0, 0, 0.175), m_trim)
    for f in range(1, floors):
        box("Cornice", (w + 0.25, d + 0.25, 0.12), (0, 0, f * FLOOR_H), m_trim)

    # 창 배치: 앞면(-y) 기준. 간격 2.2m, 양끝 여백.
    nwin = max(1, int((w - 1.2) // 2.2))
    xs = [(-(nwin - 1) / 2 + i) * 2.2 for i in range(nwin)]
    front_y = -d / 2
    # 1층 앞면: 문 + (상점이면 큰 유리창) + 창
    door_x = rng.choice(xs)
    for f in range(floors):
        z = f * FLOOR_H + 1.75
        for x in xs:
            if f == 0 and abs(x - door_x) < 0.01:
                # 문 2.1m × 1.0m + 계단
                box("Door", (1.0, 0.1, 2.1), (x, front_y - 0.03, 1.05 + 0.35), m_door)
                for (dx, dz, sx, sz) in [(0, 2.1 + 0.35 + 0.06, 1.24, 0.12), (-0.56, 1.05 + 0.35, 0.12, 2.1), (0.56, 1.05 + 0.35, 0.12, 2.1)]:
                    box("Frame", (sx, 0.14, sz), (x + dx, front_y - 0.05, dz), m_trim)
                box("Step", (1.6, 0.5, 0.18), (x, front_y - 0.25, 0.09), m_trim)
                if awning:
                    box("Awning", (1.9, 1.1, 0.06), (x, front_y - 0.55, 2.75), m_awn, rot=(math.radians(-15), 0, 0))
                continue
            if f == 0 and shop:
                # 상점 유리창 (넓고 낮게) + 차양
                box("Glass", (1.8, 0.05, 1.6), (x, front_y + 0.02, 1.35), m_glass)
                for (dx, dz, sx, sz) in [(0, 1.35 + 0.8 + 0.05, 2.0, 0.1), (0, 1.35 - 0.8 - 0.05, 2.0, 0.1), (-0.95, 1.35, 0.1, 1.6), (0.95, 1.35, 0.1, 1.6)]:
                    box("Frame", (sx, 0.12, sz), (x + dx, front_y - 0.04, dz), m_trim)
                if awning: box("Awning", (2.2, 1.1, 0.06), (x, front_y - 0.55, 2.55), m_awn, rot=(math.radians(-15), 0, 0))
                continue
            planter = (f >= 1 and rng.random() < 0.5)
            window(x, front_y, z, 0, m_wall, m_shut, m_trim, m_glass, shutters=rng.random() < 0.85, planter=planter, planter_m=m_plant)
        # 측면 창 (좌/우) — 깊이에 따라 1~2개
        nside = max(1, int((d - 1.2) // 2.6))
        ys = [(-(nside - 1) / 2 + i) * 2.6 for i in range(nside)]
        for y in ys:
            for facing, x in ((2, -w / 2), (3, w / 2)):
                if rng.random() < 0.8:
                    window(x, y, z, facing, m_wall, m_shut, m_trim, m_glass, shutters=rng.random() < 0.7)

    # 발코니 (2층 앞면, 창 하나 또는 전체)
    if balcony and floors >= 2:
        bw = w - 0.8 if rng.random() < 0.4 else 2.4
        bx = 0 if bw > 3 else rng.choice(xs)
        zb = FLOOR_H + 0.35
        box("Balcony", (bw, 0.9, 0.14), (bx, front_y - 0.45, zb), m_trim)
        # 난간: 상단 봉 + 기둥
        box("Rail", (bw, 0.05, 0.05), (bx, front_y - 0.88, zb + 0.95), m_metal)
        nposts = max(3, int(bw / 0.45))
        for i in range(nposts):
            px = bx - bw / 2 + 0.05 + i * (bw - 0.1) / (nposts - 1)
            box("Post", (0.04, 0.04, 0.95), (px, front_y - 0.88, zb + 0.48), m_metal)
        for s in (-1, 1):
            box("Rail", (0.05, 0.9, 0.05), (bx + s * (bw / 2 - 0.025), front_y - 0.45, zb + 0.95), m_metal)

    # 지붕
    if roof == "flat":
        box("Parapet", (w + 0.3, d + 0.3, 0.5), (0, 0, H + 0.25), m_wall)
        box("RoofSlab", (w - 0.3, d - 0.3, 0.1), (0, 0, H + 0.05), m_roof)
        if rng.random() < 0.6:
            box("Tank", (0.9, 0.9, 1.0), (w / 4, d / 4, H + 0.5 + 0.5), m_trim)   # 물탱크
        if rng.random() < 0.5:
            box("Chimney", (0.5, 0.5, 0.9), (-w / 3, -d / 4, H + 0.45 + 0.5), m_trim)
    else:
        pitch_h = 0.22 * d
        wedge_roof("Roof", w, d, H, pitch_h, 0.4, m_roof)
        box("Ridge", (w + 0.8, 0.25, 0.12), (0, 0, H + pitch_h + 0.02), m_roof)
        if rng.random() < 0.5:
            box("Chimney", (0.5, 0.5, 1.2), (w / 4, 0, H + pitch_h * 0.5 + 0.6), m_trim)

    # 잡소품: 배수관, 벽등, 실외기
    box("Pipe", (0.12, 0.12, H - 0.3), (w / 2 - 0.2, front_y - 0.08, (H - 0.3) / 2 + 0.3), m_metal)
    box("Lamp", (0.2, 0.3, 0.2), (door_x + 0.9, front_y - 0.17, 2.6), m_metal)
    if rng.random() < 0.5:
        box("AC", (0.7, 0.3, 0.5), (w / 2 + 0.17, rng.uniform(-d / 4, d / 4), FLOOR_H + 1.2), m_metal)

    # 합치기 + 정리
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts: o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = f"Bld_{zone}_{idx}"
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.faces_shade_flat()
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.01)
    bpy.ops.mesh.quads_convert_to_tris()
    bpy.ops.object.mode_set(mode="OBJECT")
    return obj

def apply_scale(obj, s):
    """세계 척도. 실제 치수(문 2.1m·층고 3m)로 짓고 마지막에 한 번 줄인다 — 차 크기에 맞추는 손잡이 하나."""
    if abs(s - 1.0) < 1e-6: return
    obj.scale = (s, s, s)
    bpy.ops.object.transform_apply(scale=True)

def export(obj, out_base):
    bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=out_base + ".fbx", use_selection=True, axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, path_mode="COPY")
    bpy.ops.export_scene.gltf(filepath=out_base + ".glb", use_selection=True, export_format="GLB")

def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--zone", default="harbor", choices=list(PALETTES))
    ap.add_argument("--count", type=int, default=4)
    ap.add_argument("--seed", type=int, default=1)
    ap.add_argument("--w", type=float, default=0); ap.add_argument("--d", type=float, default=0)
    ap.add_argument("--floors", type=int, default=0)
    ap.add_argument("--roof", default="", choices=["", "flat", "gable"])
    ap.add_argument("--balcony", action="store_true"); ap.add_argument("--awning", action="store_true"); ap.add_argument("--shop", action="store_true")
    ap.add_argument("--scale", type=float, default=1.0, help="세계 척도 (차 높이 / 실제 차 1.5m). 실제 치수로 짓고 마지막에 곱한다")
    a = ap.parse_args(argv)
    os.makedirs(a.out, exist_ok=True)
    rng = random.Random(a.seed)
    for i in range(a.count):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        _mats.clear()
        w = a.w or rng.choice([5.5, 6.5, 7.5, 9.0])
        d = a.d or rng.choice([5.5, 6.0, 7.0])
        floors = a.floors or rng.choice([2, 2, 3, 3])
        roof = a.roof or rng.choice(["flat", "flat", "gable"])
        balcony = a.balcony or rng.random() < 0.6
        awning = a.awning or rng.random() < 0.5
        shop = a.shop or rng.random() < 0.4
        obj = build(rng, a.zone, w, d, floors, roof, balcony, awning, shop, i)
        apply_scale(obj, a.scale)
        tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
        base = os.path.join(a.out, f"{a.zone}_{i}")
        export(obj, base)
        print("REPORT", json.dumps({"file": base + ".fbx", "tris": tris, "w": w, "d": d, "floors": floors, "roof": roof,
                                    "balcony": balcony, "awning": awning, "shop": shop,
                                    "dims": [round(v, 2) for v in obj.dimensions]}, ensure_ascii=False))

main()
