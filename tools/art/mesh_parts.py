"""Read-only world-space connected parts (FBX seams welded virtually at 1 mm).

Adapted from split_1857.py / count_bundled.py; never welds the exported mesh.
"""
from mathutils import Vector, Matrix


def bounds(points):
    points = list(points)
    return tuple(Vector([fn(p[k] for p in points) for k in range(3)]) for fn in (min, max))


def islands(obj):
    me, mw = obj.data, obj.matrix_world
    parent = list(range(len(me.vertices)))
    def root(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    keys = {}
    for v in me.vertices:
        key = tuple(round(c * 1000) for c in mw @ v.co)
        if key in keys:
            parent[root(v.index)] = root(keys[key])
        else:
            keys[key] = v.index
    for face in me.polygons:
        for vi in face.vertices[1:]:
            parent[root(vi)] = root(face.vertices[0])
    groups = {}
    for face in me.polygons:
        groups.setdefault(root(face.vertices[0]), []).append(face.index)
    result = []
    for faces in groups.values():
        verts = {vi for fi in faces for vi in me.polygons[fi].vertices}
        lo, hi = bounds(mw @ me.vertices[i].co for i in verts)
        result.append(dict(faces=faces, lo=lo, hi=hi))
    return sorted(result, key=lambda g: tuple(round(c, 5) for c in g['lo']))


def touching_groups(parts, margin=0.02):
    groups = []
    for part in parts:
        hits = [g for g in groups if any(all(part['lo'][k] <= p['hi'][k] + margin and
                  p['lo'][k] <= part['hi'][k] + margin for k in range(3)) for p in g)]
        merged = [part]
        for g in hits:
            merged.extend(g)
            groups.remove(g)
        groups.append(merged)
    return sorted(groups, key=lambda g: tuple(min(p['lo'][k] for p in g) for k in range(3)))


def bottom_pivot(obj):
    lo, hi = bounds(obj.matrix_world @ v.co for v in obj.data.vertices)
    pivot = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    old = obj.matrix_world.copy()
    new = old.copy()
    new.translation = pivot
    obj.data.transform(new.inverted() @ old)
    obj.matrix_world = new
    return list(pivot)


def split_loose_props(objects, classify=None):
    """Only explicit movable-prop names; structural building meshes stay intact."""
    import bpy, bmesh, re
    eligible = re.compile(r'decal|sticker|notice|poster|sign|^PROP_', re.I)
    structural = re.compile(r'building|_body$|_shell$|wall|floor|roof', re.I)
    records = []
    for obj in sorted(objects, key=lambda o: o.name):
        if obj.type != 'MESH' or not eligible.search(obj.name) or structural.search(obj.name):
            continue
        groups = touching_groups(islands(obj))
        if len(groups) < 2:
            continue
        original = obj.name
        kind = 'SIGN' if 'sign' in original.lower() else 'STICKER' if 'sticker' in original.lower() else 'NOTICE'
        made = []
        for i, group in enumerate(groups, 1):
            keep = {fi for part in group for fi in part['faces']}
            part_kind = classify(obj, keep, kind) if classify else kind
            part = obj.copy()
            part.data = obj.data.copy()
            part.name = part.data.name = f'{original}_{part_kind}_{i:02d}'
            for collection in obj.users_collection:
                collection.objects.link(part)
            bm = bmesh.new()
            bm.from_mesh(part.data)
            bm.faces.ensure_lookup_table()
            # Delete vertices too: unused vertices must not inflate exported bounds/pivots.
            bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index not in keep], context='FACES')
            bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
            bm.to_mesh(part.data)
            bm.free()
            made.append(dict(name=part.name, pivot=bottom_pivot(part), faces=len(part.data.polygons)))
        records.append(dict(source=original, parts=made))
        bpy.data.objects.remove(obj, do_unlink=True)
    return records
