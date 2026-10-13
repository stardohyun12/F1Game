"""플레이어 차 생성기 — "망한 F1 팀의 배달 머신" (2026-10-13, D-095 연장).

Unity 쪽 `Race` 루트(scale 0.7)의 자식 body / wheel-front-left / wheel-front-right / wheel-back-left / wheel-back-right
메시만 갈아 끼우도록, **같은 이름·같은 루트-로컬 치수**로 만든다. 물리·카메라·자식 transform 은 손대지 않는다.
  - 루트-로컬 단위: 휠베이스 1.52, 트랙 0.70, 바퀴 중심 높이 0.30, 바퀴 반지름 0.30, 몸통 길이 ≈ 2.6
  - 축: Blender -Y = Unity +Z(앞), Blender +Z = Unity +Y(위), X 동일  → 코는 -Y 쪽
  - 바퀴 오브젝트는 원점이 바퀴 중심(Unity 자식 transform 이 위치를 준다)

사용:
    blender -b -P tools/asset-gen/proc_car.py -- --out out/race_v2.fbx [--seed 3] [--livery 0]
"""
import bpy, bmesh, sys, os, math, random, json
from mathutils import Vector

def c01(rgb): return tuple(v / 255.0 for v in rgb)
_mats = {}
def mat(name, rgb, rough=0.6):
    if name in _mats: return _mats[name]
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*c01(rgb), 1); b.inputs["Roughness"].default_value = rough
    m.diffuse_color = (*c01(rgb), 1); _mats[name] = m; return m

LIVERIES = [  # (몸통, 포인트 띠, 2차 띠)
    ((0xE8, 0x5A, 0x2A), (0xF5, 0xF0, 0xE6), (0x1E, 0x2A, 0x3A)),   # 주황 + 크림 + 네이비 (지금 차 계승)
    ((0xC9, 0x3A, 0x3A), (0xF5, 0xF0, 0xE6), (0x2E, 0x6F, 0x7A)),   # 빨강 + 크림 + 청록
    ((0x2E, 0x6F, 0x7A), (0xF2, 0xC4, 0x4A), (0x1E, 0x2A, 0x3A)),   # 청록 + 노랑 + 네이비
]
PRIMER = (0x8E, 0x8E, 0x8A); TIRE = (0x2A, 0x2A, 0x2C); RIM = (0xC8, 0xC9, 0xCC); METAL = (0x6E, 0x72, 0x78)
TAPE = (0x3A, 0x3A, 0x3C); HELMET = (0xF2, 0xC4, 0x4A); VISOR = (0x1A, 0x2A, 0x3A); RACK = (0x4A, 0x4E, 0x55)
LAMP = (0xFF, 0xF0, 0xB0); RUST = (0x7A, 0x4A, 0x30)

parts = []
def box(name, size, loc, m, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object; o.name = name; o.scale = size; o.rotation_euler = rot
    o.data.materials.append(m); parts.append(o); return o

def cyl(name, r, depth, loc, m, seg=16, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=seg, radius=r, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object; o.name = name; o.data.materials.append(m); parts.append(o); return o

def sphere(name, r, loc, m, sub=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub, radius=r, location=loc)
    o = bpy.context.active_object; o.name = name; o.data.materials.append(m); parts.append(o); return o

def taper(obj, front_scale, axis=1):
    """오브젝트의 앞쪽(-Y) 끝 단면을 front_scale 배로 좁힌다 (코 테이퍼)."""
    me = obj.data
    ymin = min(v.co.y for v in me.vertices); ymax = max(v.co.y for v in me.vertices)
    for v in me.vertices:
        t = (ymax - v.co.y) / (ymax - ymin) if ymax > ymin else 0  # 앞(-Y)에서 1
        k = 1 - (1 - front_scale) * t
        v.co.x *= k; v.co.z = v.co.z * (1 - (1 - front_scale * 0.9) * t * 0.6)

def join(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.active_object; o.name = name
    return o

def finalize(o):
    bpy.ops.object.select_all(action="DESELECT"); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.faces_shade_flat(); bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.01)
    bpy.ops.mesh.quads_convert_to_tris(); bpy.ops.object.mode_set(mode="OBJECT")

def build_body(rng, livery):
    global parts; parts = []
    body_c, accent_c, accent2_c = LIVERIES[livery]
    m_body = mat("Body", body_c); m_acc = mat("Accent", accent_c); m_acc2 = mat("Accent2", accent2_c)
    m_prim = mat("Primer", PRIMER, 0.9); m_metal = mat("Metal", METAL, 0.4); m_tape = mat("Tape", TAPE, 0.9)
    m_helm = mat("Helmet", HELMET, 0.3); m_visor = mat("Visor", VISOR, 0.1); m_rack = mat("Rack", RACK, 0.7)
    m_lamp = mat("Lamp", LAMP, 0.2); m_rust = mat("Rust", RUST, 0.95)

    # ---- 모노코크 (앞 -Y). Kenney 장난감 비례(길이 2.6 · 폭 1.3 · 높이 0.73)를 지키고 디테일만 올린다.
    # 바퀴 바깥폭 = 0.70+0.30 = 1.00, 바퀴 중심 y = -0.64 / +0.88
    nose = box("Nose", (0.62, 0.90, 0.34), (0, -0.98, 0.33), m_body); taper(nose, 0.55)
    box("NoseTip", (0.30, 0.16, 0.22), (0, -1.46, 0.30), m_acc2)
    box("Cockpit", (0.82, 0.90, 0.46), (0, -0.08, 0.39), m_body)
    box("EngineCover", (0.70, 0.80, 0.42), (0, 0.62, 0.44), m_body)
    box("CoverFin", (0.10, 0.62, 0.22), (0, 0.60, 0.74), m_acc)                  # 상어지느러미 엔진커버
    # 사이드포드: 몸통에 붙는 큰 덩어리 (한쪽은 프라이머 — 망한 팀)
    box("PodL", (0.30, 1.05, 0.34), (-0.50, 0.22, 0.29), m_body)
    box("PodR", (0.30, 1.05, 0.34), (0.50, 0.22, 0.29), m_prim if rng.random() < 0.7 else m_body)
    box("IntakeL", (0.26, 0.08, 0.24), (-0.50, -0.32, 0.31), m_tape); box("IntakeR", (0.26, 0.08, 0.24), (0.50, -0.32, 0.31), m_tape)
    # 리버리 띠
    box("StripeTop", (0.16, 1.75, 0.02), (0, -0.45, 0.63), m_acc)
    box("StripeL", (0.02, 1.05, 0.10), (-0.66, 0.22, 0.36), m_acc2); box("StripeR", (0.02, 1.05, 0.10), (0.66, 0.22, 0.36), m_acc2)
    # 번호 원판 (코 양옆)
    cyl("RoundelL", 0.13, 0.02, (-0.32, -0.95, 0.36), m_acc, seg=12, rot=(0, math.pi / 2, 0))
    cyl("RoundelR", 0.13, 0.02, (0.32, -0.95, 0.36), m_acc, seg=12, rot=(0, math.pi / 2, 0))
    # 콕핏: 어두운 구멍 판 + 헬멧 + 롤후프 + 핸들
    box("CockpitHole", (0.44, 0.52, 0.02), (0, -0.14, 0.625), m_tape)
    sphere("Helmet", 0.15, (0, -0.06, 0.66), m_helm, sub=2)
    box("Visor", (0.24, 0.06, 0.09), (0, -0.19, 0.67), m_visor)
    box("RollHoop", (0.30, 0.07, 0.30), (0, 0.16, 0.78), m_metal)
    cyl("Wheel", 0.09, 0.02, (0, -0.36, 0.60), m_tape, seg=12, rot=(math.radians(70), 0, 0))
    # 앞 윙 + 엔드플레이트 (오버행 짧게: 전체 길이 ≈ 2.7)
    box("FrontWing", (1.12, 0.26, 0.05), (0, -1.30, 0.12), m_acc)
    box("FrontWing2", (0.84, 0.12, 0.04), (0, -1.18, 0.19), m_body)
    box("EndplateL", (0.04, 0.30, 0.18), (-0.56, -1.28, 0.17), m_acc2); box("EndplateR", (0.04, 0.30, 0.18), (0.56, -1.28, 0.17), m_acc2)
    # 배달용 헤드램프 (볼트로 단 둥근 등 2개) + 바
    cyl("LampL", 0.075, 0.08, (-0.20, -1.40, 0.44), m_lamp, seg=12, rot=(math.pi / 2, 0, 0))
    cyl("LampR", 0.075, 0.08, (0.20, -1.40, 0.44), m_lamp, seg=12, rot=(math.pi / 2, 0, 0))
    box("LampBar", (0.50, 0.04, 0.04), (0, -1.38, 0.52), m_metal)
    # 뒤: 화물 데크 (낮은 옆판 달린 짐칸) + 작은 윙
    box("Deck", (0.86, 0.56, 0.08), (0, 1.08, 0.30), m_rack)
    box("DeckBack", (0.86, 0.05, 0.22), (0, 1.33, 0.44), m_rack)
    box("DeckL", (0.05, 0.56, 0.22), (-0.405, 1.08, 0.44), m_rack); box("DeckR", (0.05, 0.56, 0.22), (0.405, 1.08, 0.44), m_rack)
    box("Strap", (0.90, 0.06, 0.02), (0, 1.08, 0.35), m_acc2)
    box("RearWing", (0.98, 0.22, 0.05), (0, 1.30, 0.72), m_acc)
    box("WingPlateL", (0.04, 0.26, 0.18), (-0.49, 1.30, 0.68), m_acc2); box("WingPlateR", (0.04, 0.26, 0.18), (0.49, 1.30, 0.68), m_acc2)
    box("WingStrut", (0.12, 0.08, 0.18), (0, 1.22, 0.62), m_metal)
    # 배기관 2, 사이드미러 2, 안테나
    cyl("ExhL", 0.04, 0.22, (-0.18, 1.08, 0.24), m_metal, seg=10, rot=(math.pi / 2, 0, 0))
    cyl("ExhR", 0.04, 0.22, (0.18, 1.08, 0.24), m_metal, seg=10, rot=(math.pi / 2, 0, 0))
    box("MirrorL", (0.11, 0.05, 0.07), (-0.50, -0.46, 0.60), m_tape); box("MirrorArmL", (0.12, 0.025, 0.025), (-0.44, -0.46, 0.56), m_metal)
    box("MirrorR", (0.11, 0.05, 0.07), (0.50, -0.46, 0.60), m_tape); box("MirrorArmR", (0.12, 0.025, 0.025), (0.44, -0.46, 0.56), m_metal)
    box("Antenna", (0.02, 0.02, 0.28), (0.14, 0.34, 0.86), m_metal)
    # 서스펜션 암 (몸통 → 바퀴 쪽) — 바퀴는 별 오브젝트
    for (x, y) in [(-0.35, -0.64), (0.35, -0.64), (-0.35, 0.88), (0.35, 0.88)]:
        sx = 1 if x > 0 else -1
        box("ArmUp", (0.30, 0.04, 0.04), (x - sx * 0.10, y - 0.09, 0.40), m_metal)
        box("ArmLo", (0.30, 0.04, 0.04), (x - sx * 0.10, y + 0.09, 0.24), m_metal)
    # 망한 팀 표식: 녹 패치·테이프 X
    box("RustPatch", (0.14, 0.20, 0.012), (0.20, -0.75, 0.506), m_rust)
    box("TapeX1", (0.035, 0.26, 0.012), (-0.22, 0.52, 0.656), m_tape, rot=(0, 0, math.radians(30)))
    box("TapeX2", (0.035, 0.26, 0.012), (-0.22, 0.52, 0.656), m_tape, rot=(0, 0, math.radians(-30)))
    body = join(parts, "body")
    # 원점을 세계 원점(루트)으로 — join 은 첫 부품(코) 원점을 쓰므로 그대로 두면 Unity 에서 1m 밀린다
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    finalize(body)
    return body

def build_wheel(name, rng):
    global parts; parts = []
    m_tire = mat("Tire", TIRE, 0.95); m_rim = mat("Rim", RIM, 0.35); m_metal = mat("Metal", METAL, 0.4)
    # 바퀴 축 = X (좌우). 반지름 0.30, 폭 0.30. 원점 = 바퀴 중심.
    cyl("Tire", 0.30, 0.34, (0, 0, 0), m_tire, seg=18, rot=(0, math.pi / 2, 0))
    cyl("Rim", 0.20, 0.35, (0, 0, 0), m_rim, seg=12, rot=(0, math.pi / 2, 0))
    cyl("Hub", 0.07, 0.38, (0, 0, 0), m_metal, seg=8, rot=(0, math.pi / 2, 0))
    for i in range(5):
        a = i * 2 * math.pi / 5
        box("Spoke", (0.37, 0.05, 0.15), (0, 0, 0), m_metal, rot=(a, 0, 0))
    w = join(parts, name)
    finalize(w)
    return w

def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True); ap.add_argument("--seed", type=int, default=3); ap.add_argument("--livery", type=int, default=0)
    a = ap.parse_args(argv)
    rng = random.Random(a.seed)
    bpy.ops.wm.read_factory_settings(use_empty=True); _mats.clear()
    objs = [build_body(rng, a.livery)]
    # 바퀴 4개: 원점은 각자 중심이지만, GLB 미리보기용으로 Unity 자식 위치(루트-로컬)에 놓는다.
    # FBX 에선 Unity 가 자식 transform 을 그대로 쓰므로 위치는 무시돼도 된다 (메시만 교체).
    for name, (x, y) in {"wheel-front-left": (-0.35, -0.64), "wheel-front-right": (0.35, -0.64),
                         "wheel-back-left": (-0.35, 0.88), "wheel-back-right": (0.35, 0.88)}.items():
        w = build_wheel(name, rng)
        w.location = (x, y, 0.30)
        objs.append(w)
    tris = {o.name: sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs}
    os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=a.out, use_selection=True, axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             mesh_smooth_type="FACE", add_leaf_bones=False, path_mode="COPY", apply_scale_options="FBX_SCALE_ALL")
    bpy.ops.export_scene.gltf(filepath=os.path.splitext(a.out)[0] + ".glb", use_selection=True, export_format="GLB")
    body = objs[0]
    # Unity 쪽이 임포터 색 해석에 기대지 않도록 머티리얼 색표를 같이 쓴다 (SwapCarVisual 이 읽음)
    table = {}
    for m in _mats.values():
        c = m.diffuse_color
        table[m.name] = [round(c[0], 4), round(c[1], 4), round(c[2], 4)]
    with open(os.path.splitext(a.out)[0] + ".materials.json", "w", encoding="utf-8") as f:
        json.dump(table, f, ensure_ascii=False, indent=1)
    print("REPORT", json.dumps({"out": a.out, "tris": tris, "total": sum(tris.values()),
                                "body_dims": [round(v, 3) for v in body.dimensions]}, ensure_ascii=False))

main()
