"""Conservative solid-part clearance + supported parcel placement, Blender Z-up.

Shelf rows are packed around upright solids, retaining order and shelf level.
AABB tests include containment (BVH surface overlap alone cannot detect it).
1 cm is the reported defect threshold; the solver leaves 1 cm horizontal air.
"""
import bpy, sys, json, os
from pathlib import Path
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
from mesh_parts import islands, bounds

FBX, MODE, OUT = sys.argv[sys.argv.index('--') + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)
GAP, EPS = 0.01, 0.0005
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
parcels = sorted([o for o in meshes if o.name.startswith('PARCEL_')], key=lambda o: o.name)

def bbox(o):
    return bounds(o.matrix_world @ v.co for v in o.data.vertices)

def depth(a, b):
    return [min(a[1][k], b[1][k]) - max(a[0][k], b[0][k]) for k in range(3)]

def overlaps(a, b, eps=EPS):
    return min(depth(a, b)) > eps

def solids(o):
    # Flat labels do not occupy a volume. Boards/posts remain separate solids.
    return [(p['lo'], p['hi']) for p in islands(o) if min(p['hi'] - p['lo']) > EPS]

fixtures = [(o.name, part) for o in meshes if o.name.startswith(('SHELF_', 'CART_', 'TABLE_', 'COUNTER_', 'Q3_building')) for part in solids(o)]

def audit():
    pairs, fixed = [], []
    for i, p in enumerate(parcels):
        a = bbox(p)
        for q in parcels[i + 1:]:
            b = bbox(q)
            if overlaps(a, b):
                pairs.append(dict(a=p.name, b=q.name, depth_m=depth(a, b)))
        for name, part in fixtures:
            if overlaps(a, part):
                fixed.append(dict(a=p.name, b=name, depth_m=depth(a, part)))
    return dict(parcel_pairs=pairs, fixture_pairs=fixed)

before = audit()
moves = []
def move(o, delta, reason):
    if delta.length < 1e-6:
        return
    m = o.matrix_world.copy()
    m.translation += delta
    o.matrix_world = m
    bpy.context.view_layer.update()
    moves.append(dict(name=o.name, delta_m=list(delta), reason=reason))

if MODE == 'fix':
    handled = set()
    for shelf in sorted([o for o in meshes if o.name.startswith('SHELF_')], key=lambda o: o.name):
        slo, shi = bbox(shelf)
        levels = sorted({round(p['hi'].z, 5) for p in islands(shelf)
                         if p['hi'].x - p['lo'].x > 1 and p['hi'].z - p['lo'].z < .10})
        for level in levels:
            row = []
            for p in parcels:
                lo, hi = bbox(p)
                center = (lo + hi) / 2
                if abs(lo.z - level) < .015 and slo.x < center.x < shi.x and slo.y < center.y < shi.y:
                    row.append(p)
            row.sort(key=lambda p: bbox(p)[0].x)
            # Beam search keeps nearby positions, but can move earlier parcels to leave room for later ones.
            states = [(0., [], slo.x + GAP)]
            for p in row:
                lo, hi = bbox(p)
                size = hi - lo
                y = min(lo.y, shi.y - GAP - size.y)
                obstacles = [part for _, part in fixtures if
                             min(hi.z, part[1].z) - max(lo.z, part[0].z) > EPS and
                             min(y + size.y, part[1].y) - max(y, part[0].y) > EPS]
                next_states = []
                for cost, placements, edge in states:
                    xs = {edge, max(edge, lo.x)}
                    xs.update(max(edge, part[1].x + GAP) for part in obstacles)
                    for x in sorted(xs):
                        if x + size.x > shi.x - GAP:
                            continue
                        low = Vector((x, y, level))
                        candidate = (low, low + size)
                        if any(overlaps(candidate, part) for part in obstacles):
                            continue
                        next_states.append((cost + (x - lo.x) ** 2 + (y - lo.y) ** 2,
                                            placements + [(p, low - lo)], x + size.x + GAP))
                states = sorted(next_states, key=lambda s: s[0])[:128]
                if not states:
                    raise RuntimeError(f'No supported packing for {shelf.name} z={level}, at {p.name}')
            if row:
                for p, delta in states[0][1]:
                    move(p, delta, 'shelf row: avoid uprights, keep support and order')
                    handled.add(p.name)
    # Remaining parcels: resolve against fixed solids and lower parcels; prefer a lateral slide.
    settled = [p for p in parcels if p.name in handled]
    for p in sorted([p for p in parcels if p.name not in handled], key=lambda p: (bbox(p)[0].z, p.name)):
        lo, hi = bbox(p)
        obs = [part for _, part in fixtures] + [bbox(q) for q in settled]
        if any(overlaps((lo, hi), b) for b in obs):
            candidates = []
            for b in obs:
                if not overlaps((lo, hi), b):
                    continue
                for axis in (0, 1):
                    for margin in (GAP, 0.0):
                        for amount in (b[0][axis] - hi[axis] - margin, b[1][axis] - lo[axis] + margin):
                            delta = Vector((0, 0, 0)); delta[axis] = amount
                            candidates.append(delta)
                candidates.append(Vector((0, 0, b[1].z - lo.z)))
            valid = []
            for delta in candidates:
                candidate = (lo + delta, hi + delta)
                if any(overlaps(candidate, b) for b in obs):
                    continue
                center = (candidate[0] + candidate[1]) / 2
                support = abs(candidate[0].z) < .001 or any(
                    abs(candidate[0].z - b[1].z) < .001 and
                    b[0].x <= center.x <= b[1].x and b[0].y <= center.y <= b[1].y for b in obs)
                if support:
                    valid.append(delta)
            if not valid:
                raise RuntimeError('No supported clearance for ' + p.name)
            move(p, min(valid, key=lambda d: d.length + abs(d.z)), 'parcel/fixture clearance')
        settled.append(p)

after = audit()
report = dict(mode=MODE, threshold_m=.01, solver_epsilon_m=EPS, before=before, moves=moves, after=after,
              max_parcel_tris=max(sum(len(f.vertices)-2 for f in p.data.polygons) for p in parcels))
Path(OUT).write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print('[clearance]', MODE, 'before', len(before['parcel_pairs']), len(before['fixture_pairs']),
      'after', len(after['parcel_pairs']), len(after['fixture_pairs']), 'moves', len(moves))
if MODE in ('fix', 'check') and (after['parcel_pairs'] or after['fixture_pairs']):
    raise RuntimeError('Remaining parcel/fixture intersections: ' + OUT)
if MODE == 'fix':
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={'MESH','EMPTY'}, path_mode='STRIP', mesh_smooth_type='FACE')
sys.stdout.flush(); os._exit(0)
