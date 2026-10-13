"""Blender 로 직접 짠 2단 생일 케이크 (참고 이미지 cake_ref.png 와 같은 구성).
원통 2개 + 접시 + 크림 구슬 + 초 3개. 전부 프리미티브라 UV 깔끔, 플랫 셰이딩.

사용:
    blender -b -P tools/asset-gen/proc_cake.py -- --out out/cake_proc.fbx --height 0.30 [--seg 16]
"""
import bpy, sys, os, math
from mathutils import Vector

def args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--height", type=float, default=0.30)
    ap.add_argument("--seg", type=int, default=16, help="원통 분할 수 (Kenney 룩은 12~16)")
    return ap.parse_args(argv)

def mat(name, rgb):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*rgb, 1)
    bsdf.inputs["Roughness"].default_value = 0.8
    m.diffuse_color = (*rgb, 1)  # Workbench/뷰포트 색 (비교 렌더용)
    return m

def cyl(name, r, h, z, seg, m):
    bpy.ops.mesh.primitive_cylinder_add(vertices=seg, radius=r, depth=h, location=(0, 0, z + h / 2))
    o = bpy.context.active_object; o.name = name; o.data.materials.append(m)
    return o

def ico(name, r, loc, m, sub=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub, radius=r, location=loc)
    o = bpy.context.active_object; o.name = name; o.data.materials.append(m)
    return o

def main():
    a = args()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    seg = a.seg
    PINK, CREAM, PLATE, ROSE, WAX, FLAME = (0.95, 0.60, 0.68), (0.98, 0.95, 0.88), (0.92, 0.92, 0.90), (0.90, 0.40, 0.58), (0.98, 0.85, 0.85), (1.0, 0.75, 0.15)
    m_pink, m_cream, m_plate, m_rose, m_wax, m_flame = (mat(n, c) for n, c in
        [("Pink", PINK), ("Cream", CREAM), ("Plate", PLATE), ("Rose", ROSE), ("Wax", WAX), ("Flame", FLAME)])

    parts = []
    # 단위: 완성 높이 1.0 기준으로 짠 뒤 마지막에 a.height 로 스케일
    plate_r, plate_h = 0.95, 0.04
    t1_r, t1_h = 0.72, 0.36
    t2_r, t2_h = 0.52, 0.30
    parts.append(cyl("Plate", plate_r, plate_h, 0.0, seg * 2, m_plate))
    parts.append(cyl("Tier1", t1_r, t1_h, plate_h, seg, m_pink))
    parts.append(cyl("Tier2", t2_r, t2_h, plate_h + t1_h, seg, m_pink))
    # 크림 구슬: 각 단 윗면 테두리 + 접시 가장자리
    def beads(name, r_ring, z, n, bead_r, m):
        for i in range(n):
            th = 2 * math.pi * i / n
            parts.append(ico(f"{name}{i}", bead_r, (r_ring * math.cos(th), r_ring * math.sin(th), z), m))
    beads("BeadBottom", t1_r, plate_h + 0.02, seg, 0.075, m_cream)
    beads("BeadT1", t1_r - 0.02, plate_h + t1_h, seg, 0.07, m_cream)
    beads("BeadT2", t2_r - 0.02, plate_h + t1_h + t2_h, seg, 0.06, m_cream)
    # 장미(분홍 구슬) 옆면
    beads("RoseT1", t1_r + 0.01, plate_h + t1_h * 0.55, seg // 2, 0.05, m_rose)
    beads("RoseT2", t2_r + 0.01, plate_h + t1_h + t2_h * 0.55, seg // 2, 0.045, m_rose)
    # 초 3개
    top = plate_h + t1_h + t2_h
    for i, x in enumerate((-0.22, 0.0, 0.22)):
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.035, depth=0.26, location=(x, 0, top + 0.13))
        c = bpy.context.active_object; c.name = f"Candle{i}"; c.data.materials.append(m_wax); parts.append(c)
        bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.035, radius2=0.0, depth=0.09, location=(x, 0, top + 0.26 + 0.045))
        f = bpy.context.active_object; f.name = f"Flame{i}"; f.data.materials.append(m_flame); parts.append(f)

    bpy.ops.object.select_all(action="DESELECT")
    for o in parts: o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object; obj.name = "Cake_Proc"
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.faces_shade_flat()
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
    bpy.ops.mesh.quads_convert_to_tris()
    bpy.ops.object.mode_set(mode="OBJECT")
    k = a.height / obj.dimensions.z
    obj.scale = (k, k, k); bpy.ops.object.transform_apply(scale=True)
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=a.out, use_selection=True, axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, path_mode="COPY")
    bpy.ops.export_scene.gltf(filepath=os.path.splitext(a.out)[0] + ".glb", use_selection=True, export_format="GLB")
    print(f"REPORT tris={tris} dims={[round(v,3) for v in obj.dimensions]} out={a.out}")

main()
