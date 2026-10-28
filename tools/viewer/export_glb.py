"""FBX -> GLB batch export for the PitCrew building web viewer.

Run (headless):
  "D:/Tools/Blender/blender.exe" --background --factory-startup --python tools/viewer/export_glb.py -- [name ...]

Reads tools/viewer/buildings.json, re-reads whatever FBX/PNG is currently in
art/export/<...> (never writes there), makes sure each material has a base
color image (APPLY.md slot table in buildings.json -> png), sets image
interpolation to 'Closest', exports tools/viewer/assets/<name>.glb with
embedded textures, re-imports each GLB to count triangles, and writes
tools/viewer/assets/manifest.json.
"""
import bpy
import json
import os
import sys
import time
import mathutils

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
ASSETS = os.path.join(HERE, "assets")


def log(*a):
    print("[export_glb]", *a, flush=True)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mesh_objects():
    return [o for o in bpy.data.objects if o.type == "MESH"]


def tri_count():
    n = 0
    for o in mesh_objects():
        n += sum(len(p.vertices) - 2 for p in o.data.polygons)
    return n


def world_bbox():
    mn = [1e9] * 3
    mx = [-1e9] * 3
    for o in mesh_objects():
        for v in o.bound_box:
            w = o.matrix_world @ mathutils.Vector(v)
            for i in range(3):
                mn[i] = min(mn[i], w[i])
                mx[i] = max(mx[i], w[i])
    return mn, mx


def find_mapping(mat_name, textures):
    # exact, then prefix/substring (e.g. postbox_G1_unlit -> postbox_G1)
    base = mat_name.split(".")[0]
    if base in textures:
        return textures[base]
    for k in sorted(textures, key=len, reverse=True):
        if base.startswith(k) or k in base:
            return textures[k]
    return None


def load_image(path):
    for img in bpy.data.images:
        if img.filepath and os.path.normcase(bpy.path.abspath(img.filepath)) == os.path.normcase(path):
            return img
    img = bpy.data.images.load(path, check_existing=True)
    return img


def image_ok(img):
    if img is None:
        return False
    try:
        return img.size[0] > 0 and img.size[1] > 0
    except Exception:
        return False


def fix_material(mat, entry, tex_dir):
    textures = entry.get("textures", {})
    mapped = find_mapping(mat.name, textures)
    mapped_path = os.path.join(tex_dir, mapped) if mapped else None
    if mapped_path and not os.path.isfile(mapped_path):
        log("  WARN mapped texture missing:", mapped_path)
        mapped_path = None

    nt = mat.node_tree
    if nt is None:
        mat.use_nodes = True
        nt = mat.node_tree
    nodes, links = nt.nodes, nt.links
    bsdf = next((n for n in nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        out = next((n for n in nodes if n.type == "OUTPUT_MATERIAL"), None) or nodes.new("ShaderNodeOutputMaterial")
        bsdf = nodes.new("ShaderNodeBsdfPrincipled")
        links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

    base_in = bsdf.inputs["Base Color"]
    # image node currently feeding base color (directly)
    tex = None
    if base_in.is_linked and base_in.links[0].from_node.type == "TEX_IMAGE":
        tex = base_in.links[0].from_node
    if tex is None:
        tex = next((n for n in nodes if n.type == "TEX_IMAGE"), None)

    action = "kept"
    if mapped_path:
        img = load_image(mapped_path)
        if tex is None:
            tex = nodes.new("ShaderNodeTexImage")
            action = "added"
        elif not (tex.image and os.path.normcase(bpy.path.abspath(tex.image.filepath)) == os.path.normcase(mapped_path)):
            action = "set"
        tex.image = img
    elif tex is None or not image_ok(tex.image):
        dflt = entry.get("default_texture")
        p = os.path.join(tex_dir, dflt) if dflt else None
        if p and os.path.isfile(p):
            if tex is None:
                tex = nodes.new("ShaderNodeTexImage")
            tex.image = load_image(p)
            action = "default"
        else:
            action = "NONE"

    if tex is not None:
        if not (base_in.is_linked and base_in.links[0].from_node == tex):
            for l in list(base_in.links):
                links.remove(l)
            links.new(tex.outputs["Color"], base_in)
        # decals: cutout alpha
        if any(k in mat.name.lower() for k in ("decal", "icon")):
            a_in = bsdf.inputs["Alpha"]
            if not a_in.is_linked:
                links.new(tex.outputs["Alpha"], a_in)
    # every image node -> Closest (pixel textures)
    for n in nodes:
        if n.type == "TEX_IMAGE":
            n.interpolation = "Closest"
    img_name = os.path.basename(tex.image.filepath) if tex is not None and tex.image else None
    return action, img_name


def export_one(entry):
    name = entry["name"]
    fbx = os.path.join(REPO, entry["fbx"])
    tex_dir = os.path.join(REPO, entry.get("tex_dir") or os.path.dirname(entry["fbx"]))
    if not os.path.isfile(fbx):
        log("ERROR missing fbx", fbx)
        return None
    reset()
    bpy.ops.import_scene.fbx(filepath=fbx)
    log(name, "<-", entry["fbx"], "meshes", len(mesh_objects()), "tris", tri_count())
    for mat in bpy.data.materials:
        if mat.users == 0:
            continue
        action, img = fix_material(mat, entry, tex_dir)
        log("  mat %-30s %-8s %s" % (mat.name, action, img))
    mn, mx = world_bbox()
    # Blender Z-up -> glTF Y-up: (x, y, z) -> (x, z, -y). Size in Unity/glTF axes [x, height, depth].
    size = [round(mx[0] - mn[0], 2), round(mx[2] - mn[2], 2), round(mx[1] - mn[1], 2)]
    out = os.path.join(ASSETS, name + ".glb")
    tmp = out + ".tmp.glb"
    bpy.ops.export_scene.gltf(
        filepath=tmp,
        export_format="GLB",
        export_yup=True,
        export_apply=True,
        export_texcoords=True,
        export_normals=True,
        export_materials="EXPORT",
        export_image_format="AUTO",
        export_cameras=False,
        export_lights=False,
        export_animations=False,
    )
    os.replace(tmp, out)  # atomic swap so the viewer never reads a half-written file
    # verify: re-import the glb and count triangles
    reset()
    bpy.ops.import_scene.gltf(filepath=out)
    tris = tri_count()
    nimg = len([i for i in bpy.data.images if i.size[0] > 0])
    log("  -> %s  %.1f KB  reimport tris=%d images=%d size_m=%s" % (out, os.path.getsize(out) / 1024, tris, nimg, size))
    return {
        "name": name,
        "label_ko": entry.get("label_ko", name),
        "file": name + ".glb",
        "size_m": size,
        "tris": tris,
        "bytes": os.path.getsize(out),
        "source": entry["fbx"],
        "exported": time.strftime("%Y-%m-%d %H:%M:%S"),
    }


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    with open(os.path.join(HERE, "buildings.json"), encoding="utf-8") as f:
        entries = json.load(f)
    os.makedirs(ASSETS, exist_ok=True)
    man_path = os.path.join(ASSETS, "manifest.json")
    old = {}
    if os.path.isfile(man_path):
        try:
            old = {m["name"]: m for m in json.load(open(man_path, encoding="utf-8"))}
        except Exception:
            old = {}
    manifest, failed = [], []
    for e in entries:
        if argv and e["name"] not in argv:
            if e["name"] in old:
                manifest.append(old[e["name"]])
            continue
        try:
            m = export_one(e)
        except Exception as ex:  # keep going for the other buildings
            log("ERROR", e["name"], repr(ex))
            m = None
        if m:
            manifest.append(m)
        else:
            failed.append(e["name"])
            if e["name"] in old:
                manifest.append(old[e["name"]])
    with open(man_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)
    log("manifest:", man_path, [m["name"] for m in manifest])
    if failed:
        log("FAILED:", failed)
        sys.exit(1)


main()
