import bpy,os,glob,re
from mathutils import Vector
ROOT="D:/Github/F1Game/art/thirdparty"
def imp(f):
    before = set(bpy.data.objects)
    ext = f.lower().rsplit('.', 1)[1]
    try:
        if ext in ('glb', 'gltf'): bpy.ops.import_scene.gltf(filepath=f)
        elif ext == 'fbx': bpy.ops.import_scene.fbx(filepath=f)
        elif ext == 'obj': bpy.ops.wm.obj_import(filepath=f)
        elif ext == 'blend':
            with bpy.data.libraries.load(f, link=False) as (src, dst):
                dst.objects = [n for n in src.objects]
            for o in dst.objects:
                if o is not None and o.type in ('MESH', 'EMPTY'):
                    bpy.context.scene.collection.objects.link(o)
    except Exception as e:
        print('IMPORT FAIL', f, e)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.type in ('CAMERA', 'LIGHT'): bpy.data.objects.remove(o)
    return [o for o in bpy.data.objects if o not in before]

def pngs_for(f):
    pack = f.replace('\\', '/').split('/thirdparty/')[1].split('/')[0]
    return glob.glob(f'{ROOT}/{pack}/**/*.png', recursive=True) + glob.glob(f'{ROOT}/{pack}/**/*.PNG', recursive=True)

def fix_textures(objs, f):
    stem = os.path.splitext(os.path.basename(f))[0].lower()
    cands = None
    missing = 0
    for o in objs:
        if o.type != 'MESH': continue
        for slot in o.material_slots:
            m = slot.material
            if not m: continue
            m.use_nodes = True
            nt = m.node_tree
            tex = [n for n in nt.nodes if n.type == 'TEX_IMAGE' and n.image and (n.image.has_data or os.path.exists(bpy.path.abspath(n.image.filepath)))]
            if tex:
                nt.nodes.active = tex[0]; continue
            if cands is None: cands = pngs_for(f)
            key = (m.name.lower().split('.')[0], stem, o.name.lower().split('.')[0])
            best, bs = None, 0
            for p in cands:
                b = os.path.basename(p).lower()
                s = 0
                for k in key:
                    if k and k in b: s += 3
                    s += sum(1 for t in re.split(r'[_\- ]', k) if len(t) > 2 and t in b) * 0.5
                if 'fall' in b: s += 1.5
                if 'tex1' in b: s += 1
                if 'snow' in b or 'winter' in b: s -= 3
                if s > bs: best, bs = p, s
            if best:
                img = bpy.data.images.load(best, check_existing=True)
                n = nt.nodes.new('ShaderNodeTexImage'); n.image = img; n.interpolation = 'Closest'
                bsdf = next((x for x in nt.nodes if x.type == 'BSDF_PRINCIPLED'), None)
                if bsdf: nt.links.new(n.outputs['Color'], bsdf.inputs['Base Color'])
                nt.nodes.active = n
            else:
                missing += 1
    return missing

def world_bbox(objs):
    pts = []
    for o in objs:
        if o.type != 'MESH': continue
        pts += [o.matrix_world @ Vector(c) for c in o.bound_box]
    if not pts: return None
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx

