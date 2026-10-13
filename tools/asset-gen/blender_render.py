"""모델 여러 개를 같은 카메라로 렌더해 비교 PNG 를 만든다 (도현 판정용: 1× → 흑실루엣).

사용:
    blender -b -P tools/asset-gen/blender_render.py -- --out out/compare --size 512 \
        a.glb b.fbx c.glb ...
결과: out/compare/<이름>_shaded.png, <이름>_silhouette.png, <이름>_shaded_side.png
Workbench 엔진(GPU 무관, 수 초), 플랫 셰이딩, 흰 배경.
"""
import bpy, sys, os, math
from mathutils import Vector

def args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--size", type=int, default=512)
    ap.add_argument("--color", default="0.93,0.55,0.65", help="단색 r,g,b (0~1)")
    ap.add_argument("models", nargs="+")
    return ap.parse_args(argv)

def load(path):
    ext = os.path.splitext(path)[1].lower()
    before = set(bpy.data.objects)
    if ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=path)
    elif ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif ext == ".obj":
        bpy.ops.wm.obj_import(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    meshes = [o for o in new if o.type == "MESH"]
    for o in new:
        if o.type != "MESH":
            pass
    return new, meshes

def bounds(meshes):
    pts = []
    for o in meshes:
        for c in o.bound_box:
            pts.append(o.matrix_world @ Vector(c))
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi

def setup_scene(size):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.render.resolution_x = sc.render.resolution_y = size
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sh = sc.display.shading
    sh.light = "STUDIO"
    sh.color_type = "SINGLE"
    sh.show_shadows = True
    sh.show_cavity = False
    sh.show_specular_highlight = False
    sh.show_object_outline = True
    sc.display_settings.display_device = "sRGB"
    sc.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("W") if not bpy.data.worlds else bpy.data.worlds[0]
    sc.world = world
    world.color = (1, 1, 1)
    cam_data = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam_data.type = "ORTHO"
    return sc, cam

def aim(cam, lo, hi, yaw_deg, pitch_deg):
    center = (lo + hi) / 2
    radius = (hi - lo).length / 2
    yaw, pitch = math.radians(yaw_deg), math.radians(pitch_deg)
    d = Vector((math.cos(pitch) * math.sin(yaw), -math.cos(pitch) * math.cos(yaw), math.sin(pitch)))
    cam.location = center + d * (radius * 4)
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = radius * 2.2

def main():
    a = args()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc, cam = setup_scene(a.size)
    os.makedirs(a.out, exist_ok=True)
    col = tuple(float(x) for x in a.color.split(","))
    for path in a.models:
        name = os.path.splitext(os.path.basename(path))[0]
        new, meshes = load(path)
        if not meshes:
            print("메시 없음:", path); continue
        lo, hi = bounds(meshes)
        tris = sum(len(p.vertices) - 2 for o in meshes for p in o.data.polygons)
        for o in meshes:
            o.color = (*col, 1.0)
            for p in o.data.polygons:
                p.use_smooth = False
        # 머티리얼이 여럿이면 그 색을 그대로, 아니면 단색
        has_mats = any(len(o.data.materials) > 1 or (o.data.materials and o.data.materials[0] and o.data.materials[0].use_nodes) for o in meshes)
        sc.display.shading.color_type = "MATERIAL" if has_mats else "OBJECT"
        for view, (yaw, pitch) in {"shaded": (35, 25), "shaded_side": (90, 5)}.items():
            aim(cam, lo, hi, yaw, pitch)
            sc.display.shading.light = "STUDIO"
            sc.display.shading.show_shadows = True
            sc.display.shading.show_cavity = True
            sc.render.filepath = os.path.join(a.out, f"{name}_{view}.png")
            bpy.ops.render.render(write_still=True)
        # 흑실루엣
        aim(cam, lo, hi, 35, 25)
        sc.display.shading.light = "FLAT"
        sc.display.shading.show_shadows = False
        sc.display.shading.show_cavity = False
        sc.display.shading.show_object_outline = False
        sc.display.shading.color_type = "OBJECT"
        for o in meshes:
            o.color = (0, 0, 0, 1)
        sc.render.filepath = os.path.join(a.out, f"{name}_silhouette.png")
        bpy.ops.render.render(write_still=True)
        sc.display.shading.show_object_outline = True
        print(f"RENDERED {name} tris={tris} dims={[round(v,3) for v in (hi-lo)]}")
        for o in new:
            bpy.data.objects.remove(o, do_unlink=True)

main()
