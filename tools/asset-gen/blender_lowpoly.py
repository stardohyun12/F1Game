"""Hunyuan3D 가 뱉은 조밀한 GLB → 게임용 로우폴리 FBX (Unity 축, 바닥 원점, UV 포함).

사용 (Blender 를 백그라운드로):
    blender -b -P tools/asset-gen/blender_lowpoly.py -- --in cake_raw.glb --out cake_low.fbx \
        --tris 1500 --height 0.30 [--voxel 0.012] [--no-remesh] [--report cake_report.json]

단계:
  1. import GLB → 메시 전부 join → 삼각화 전 통계
  2. (기본) Voxel Remesh — marching-cubes 계단·구멍을 고르게 다시 깐다. 깎기 전에 하는 게 핵심:
     Meshy 메시를 UnityMeshSimplifier 로 바로 깎으면 "둥근 덩어리/조각" 으로 깨지던 원인이
     불균일한 삼각형 밀도라서, 먼저 균일하게 만든다.
  3. Decimate(collapse) → 목표 tris, 그 다음 Decimate(planar 3°) 로 평면 정리
  4. Shade Flat (Kenney 룩) + Smart UV Project
  5. 높이(미터) 맞춰 스케일, 바닥 중심을 원점으로
  6. FBX export (Unity: -Z forward, Y up, apply transform)
"""
import bpy, bmesh, sys, json, math, os
from mathutils import Vector

def args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument("--in", dest="inp", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--tris", type=int, default=1500)
    ap.add_argument("--height", type=float, default=0.3, help="완성 모델 높이(m)")
    ap.add_argument("--voxel", type=float, default=0.0, help="remesh voxel 크기(정규화 1m 기준). 0=자동(1/80)")
    ap.add_argument("--no-remesh", action="store_true")
    ap.add_argument("--planar-deg", type=float, default=3.0)
    ap.add_argument("--report", default="")
    return ap.parse_args(argv)

def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)

def mesh_stats(obj):
    """shell(연결 덩어리) 수 · 열린 가장자리 · 비매니폴드 · 퇴화면 — 깨지는 원인을 숫자로."""
    bm = bmesh.new(); bm.from_mesh(obj.data); bm.verts.ensure_lookup_table()
    seen = set(); shells = 0
    for v in bm.verts:
        if v.index in seen: continue
        shells += 1; stack = [v]
        while stack:
            x = stack.pop()
            if x.index in seen: continue
            seen.add(x.index)
            stack.extend(e.other_vert(x) for e in x.link_edges if e.other_vert(x).index not in seen)
    st = {"tris": sum(len(f.verts) - 2 for f in bm.faces), "shells": shells,
          "boundary_edges": sum(1 for e in bm.edges if e.is_boundary),
          "nonmanifold_edges": sum(1 for e in bm.edges if not e.is_manifold),
          "degenerate_faces": sum(1 for f in bm.faces if f.calc_area() < 1e-10)}
    bm.free(); return st

def drop_small_shells(obj, keep_ratio=0.02):
    """가장 큰 shell 대비 face 수가 keep_ratio 미만인 떠다니는 조각을 지운다. (리메시 부스러기)"""
    bm = bmesh.new(); bm.from_mesh(obj.data); bm.faces.ensure_lookup_table()
    seen = set(); islands = []
    for f in bm.faces:
        if f.index in seen: continue
        isl = []; stack = [f]
        while stack:
            x = stack.pop()
            if x.index in seen: continue
            seen.add(x.index); isl.append(x)
            for e in x.edges:
                stack.extend(g for g in e.link_faces if g.index not in seen)
        islands.append(isl)
    if not islands:
        bm.free(); return 0
    biggest = max(len(i) for i in islands)
    kill = [f for isl in islands if len(isl) < biggest * keep_ratio for f in isl]
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    bm.to_mesh(obj.data); bm.free(); obj.data.update()
    return len(islands) - sum(1 for isl in islands if len(isl) >= biggest * keep_ratio)

def cleanup(obj):
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=1e-5)
    bpy.ops.mesh.dissolve_degenerate(threshold=1e-5)
    bpy.ops.mesh.delete_loose()
    bpy.ops.object.mode_set(mode="OBJECT")

def main():
    a = args()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    ext = os.path.splitext(a.inp)[1].lower()
    if ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=a.inp)
    elif ext == ".obj":
        bpy.ops.wm.obj_import(filepath=a.inp)
    elif ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=a.inp)
    else:
        raise SystemExit(f"지원 안 하는 입력: {ext}")

    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if not meshes:
        raise SystemExit("메시 없음")
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = os.path.splitext(os.path.basename(a.out))[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # 부모(빈 오브젝트) 제거
    for o in list(bpy.context.scene.objects):
        if o.type != "MESH":
            bpy.data.objects.remove(o, do_unlink=True)

    report = {"input": a.inp, "tris_in": tri_count(obj), "verts_in": len(obj.data.vertices),
              "stats_in": mesh_stats(obj)}
    if report["stats_in"]["boundary_edges"] > 0 and not a.no_remesh:
        print(f"경고: 입력이 열린 표면(boundary_edges={report['stats_in']['boundary_edges']}) — "
              "복셀 리메시가 조각낼 수 있다. 결과 shells 를 확인할 것.")

    # 1m 로 정규화 (remesh voxel 크기를 절대값으로 다루기 위해)
    dims = obj.dimensions
    s = 1.0 / max(dims)
    obj.scale = (s, s, s)
    bpy.ops.object.transform_apply(scale=True)

    if not a.no_remesh:
        vox = a.voxel if a.voxel > 0 else 1.0 / 80.0
        m = obj.modifiers.new("Remesh", "REMESH")
        m.mode = "VOXEL"
        m.voxel_size = vox
        m.use_smooth_shade = False
        bpy.ops.object.modifier_apply(modifier=m.name)
        report["tris_after_remesh"] = tri_count(obj)
        report["voxel"] = vox
        report["shells_dropped"] = drop_small_shells(obj)
        cleanup(obj)
        report["stats_after_remesh"] = mesh_stats(obj)

    # 삼각화 후 collapse
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.quads_convert_to_tris(quad_method="BEAUTY", ngon_method="BEAUTY")
    bpy.ops.object.mode_set(mode="OBJECT")
    cur = tri_count(obj)
    if cur > a.tris:
        d = obj.modifiers.new("Decimate", "DECIMATE")
        d.decimate_type = "COLLAPSE"
        d.ratio = a.tris / cur
        d.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=d.name)
    cleanup(obj)
    report["tris_after_collapse"] = tri_count(obj)

    if a.planar_deg > 0:
        p = obj.modifiers.new("Planar", "DECIMATE")
        p.decimate_type = "DISSOLVE"
        p.angle_limit = math.radians(a.planar_deg)
        bpy.ops.object.modifier_apply(modifier=p.name)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.mesh.quads_convert_to_tris(quad_method="BEAUTY", ngon_method="BEAUTY")
        bpy.ops.object.mode_set(mode="OBJECT")
    report["tris_out"] = tri_count(obj)
    report["stats_out"] = mesh_stats(obj)

    # 플랫 셰이딩 + 노멀 정리
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.mesh.faces_shade_flat()
    # Smart UV
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02, scale_to_bounds=True)
    bpy.ops.object.mode_set(mode="OBJECT")

    # 크기: 높이(Z) 를 a.height 로
    h = obj.dimensions.z
    k = a.height / h if h > 0 else 1.0
    obj.scale = (k, k, k)
    bpy.ops.object.transform_apply(scale=True)
    # 바닥 중심을 원점으로
    xs = [v.co.x for v in obj.data.vertices]; ys = [v.co.y for v in obj.data.vertices]; zs = [v.co.z for v in obj.data.vertices]
    cx, cy, zmin = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, min(zs)
    for v in obj.data.vertices:
        v.co -= Vector((cx, cy, zmin))
    obj.location = (0, 0, 0)
    report["dimensions_m"] = [round(d, 4) for d in obj.dimensions]

    # UV 섬 수 / 점유율(대략)
    uv = obj.data.uv_layers.active
    report["uv_layer"] = bool(uv)

    os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=a.out, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE",
                             use_mesh_modifiers=True, add_leaf_bones=False, path_mode="COPY")
    report["output"] = a.out
    # 브라우저/비교 렌더용 GLB 도 같이
    glb = os.path.splitext(a.out)[0] + ".glb"
    bpy.ops.export_scene.gltf(filepath=glb, use_selection=True, export_format="GLB", export_yup=True)
    report["output_glb"] = glb
    print("REPORT", json.dumps(report, ensure_ascii=False))
    if a.report:
        with open(a.report, "w", encoding="utf-8") as f:
            json.dump(report, f, ensure_ascii=False, indent=1)

main()
