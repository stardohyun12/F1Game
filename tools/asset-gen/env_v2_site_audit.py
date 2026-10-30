"""Read-only D-211 footprint audit; run with py -3.13 (Shapely required).

Writes only site-audit.json. A separate background Blender imports the garage
FBX without modifying it. This is not a live Unity terrain or collision test.
"""
from pathlib import Path
import hashlib
import json
import math
import subprocess
import sys

R = Path(__file__).resolve().parents[2]
MANIFEST = R / 'art/export/env-first-v2/manifest.json'
GARAGE = R / 'art/export/q2-garage/garage_v5_combo.fbx'
OUT = R / 'art/_review/2026-10-30-env-first-v2/site-audit.json'
TERRAIN = R / 'art/export/env-first-v2/review-terrain.json'


def review_ground_audit(manifest):
    """Sample the exact review mesh diagonal and source-lot display pads."""
    from shapely.geometry import Point, Polygon
    raw = TERRAIN.read_bytes()
    terrain = json.loads(raw)
    vertices, nx, nz = terrain['vertices'], terrain['nx'], terrain['nz']
    ox, oz = vertices[0][:2]
    sx, sz = vertices[1][0]-ox, vertices[nx][1]-oz
    pads = [(Polygon(b['poly']).buffer(1e-7), b.get('review_pad_y', b['y'])-.025)
            for b in manifest['buildings']]
    spacing, tolerance = .25, 1e-5

    def terrain_y(x, z):
        gx, gz = (x-ox)/sx, (z-oz)/sz
        ix, iz = math.floor(gx), math.floor(gz)
        if not (0 <= ix < nx-1 and 0 <= iz < nz-1):
            return None
        u, v = gx-ix, gz-iz
        k = iz*nx+ix
        a, b, c, d = (vertices[i][2] for i in (k, k+1, k+nx+1, k+nx))
        # Generated triangles are [a,d,c] and [a,c,b], not bilinear patches.
        return a+(d-a)*v+(c-d)*u if v >= u else a+(b-a)*u+(c-b)*v

    results = []
    for building in manifest['buildings']:
        for house in building['houses']:
            poly = Polygon(house['poly'])
            samples = set()
            ring = list(poly.exterior.coords)
            for a, b in zip(ring, ring[1:]):
                count = max(1, math.ceil(math.dist(a, b)/spacing))
                for j in range(count+1):
                    t = j/count
                    samples.add((round(a[0]+t*(b[0]-a[0]), 8), round(a[1]+t*(b[1]-a[1]), 8)))
            xmin, zmin, xmax, zmax = poly.bounds
            for ix in range(math.ceil((xmax-xmin)/spacing)+1):
                for iz in range(math.ceil((zmax-zmin)/spacing)+1):
                    point = (min(xmin+ix*spacing, xmax), min(zmin+iz*spacing, zmax))
                    if poly.covers(Point(point)):
                        samples.add(point)
            samples.add((poly.centroid.x, poly.centroid.y))
            bottom, body_bottom = building['y'], building['y']+house['foundation']
            tested, missing = [], []
            for x, z in sorted(samples):
                ty = terrain_y(x, z)
                if ty is None:
                    missing.append([x, z])
                    continue
                pad_heights = [height for pad, height in pads if pad.covers(Point(x, z))]
                ground = max([ty]+pad_heights)
                tested.append({'xz': [x, z], 'terrain_y': ty,
                               'pad_y': max(pad_heights) if pad_heights else None,
                               'highest_ground_y': ground,
                               'body_clearance_m': body_bottom-ground,
                               'foundation_bottom_gap_m': bottom-ground})
            if not tested:
                raise RuntimeError('No review terrain samples for '+building['id']+'/'+house['key'])
            results.append({
                'house': building['id']+'/'+house['key'], 'sample_count': len(tested),
                'missing_terrain_sample_count': len(missing), 'missing_xz': missing,
                'body_bottom_y': body_bottom, 'foundation_bottom_y': bottom,
                'body_buried_sample_count': sum(p['body_clearance_m'] < -tolerance for p in tested),
                'floating_foundation_sample_count': sum(p['foundation_bottom_gap_m'] > tolerance for p in tested),
                'minimum_body_clearance_m': min(p['body_clearance_m'] for p in tested),
                'maximum_foundation_bottom_gap_m': max(p['foundation_bottom_gap_m'] for p in tested),
                'worst_body_sample': min(tested, key=lambda p:p['body_clearance_m']),
                'worst_foundation_sample': max(tested, key=lambda p:p['foundation_bottom_gap_m']),
            })
    return {
        'scope': 'REVIEW GEOMETRY ONLY; not the actual map terrain, Unity scene, or gameplay collision.',
        'terrain_path': str(TERRAIN.relative_to(R)),
        'terrain_sha256': hashlib.sha256(raw).hexdigest(),
        'method': 'Boundary subdivisions <=0.25m plus <=0.25m world grid inside each body polygon and centroid; exact height on generated [a,d,c]/[a,c,b] triangles; maximum with all containing source-lot pads.',
        'pad_top_rule': 'review_pad_y - 0.025',
        'body_bottom_rule': 'building.y + house.foundation',
        'foundation_bottom_rule': 'building.y',
        'tolerance_m': tolerance, 'sample_spacing_m': spacing,
        'excluded': ['Road overlays', 'Landmark display pads', 'Foundation projection outside house body',
                     'Live-map ground', 'Unsampled points'],
        'houses': results,
        'summary': {
            'houses': len(results), 'samples': sum(h['sample_count'] for h in results),
            'missing_terrain_samples': sum(h['missing_terrain_sample_count'] for h in results),
            'body_buried_samples': sum(h['body_buried_sample_count'] for h in results),
            'floating_foundation_samples': sum(h['floating_foundation_sample_count'] for h in results),
            'minimum_body_clearance_m': min(h['minimum_body_clearance_m'] for h in results),
            'maximum_foundation_bottom_gap_m': max(h['maximum_foundation_bottom_gap_m'] for h in results),
        },
    }


def extract_blender():
    import bpy
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(GARAGE))
    bpy.context.view_layer.update()
    records = []
    for ob in bpy.context.scene.objects:
        if ob.type != 'MESH':
            continue
        points = [list(ob.matrix_world @ v.co) for v in ob.data.vertices]
        records.append({'name': ob.name, 'vertices': points})
    print('SITE_AUDIT_GEOMETRY=' + json.dumps(records, separators=(',', ':')), flush=True)


def main():
    from shapely.geometry import MultiPoint, Polygon
    from shapely.ops import unary_union
    manifest_bytes = MANIFEST.read_bytes()
    m = json.loads(manifest_bytes)
    proc = subprocess.run([
        'D:/Tools/Blender/blender.exe', '-b', '-t', '2', '--factory-startup',
        '--python', str(Path(__file__).resolve()), '--', '--blender-extract'
    ], capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=180)
    rows = [line for line in proc.stdout.splitlines() if line.startswith('SITE_AUDIT_GEOMETRY=')]
    if proc.returncode or not rows:
        raise RuntimeError('Garage import failed: ' + (proc.stdout + proc.stderr)[-3000:])
    mesh_records = json.loads(rows[-1].split('=', 1)[1])
    points = [p for ob in mesh_records for p in ob['vertices']]
    if not points:
        raise RuntimeError('Garage contains no mesh vertices')
    placement = m['landmarks']['garage']
    yaw = math.radians(placement['yaw'])
    c, s = math.cos(yaw), math.sin(yaw)
    wx, wz = placement['world']

    def world(p, review_sign=False):
        # Blender x,-y -> Unity x,z, followed by Unity's left-handed Y yaw.
        x, z = p[0], -p[1]
        ss = -s if review_sign else s
        return (wx + c*x + ss*z, wz - ss*x + c*z)

    local_hull = MultiPoint([(p[0], -p[1]) for p in points]).convex_hull
    garage_hull = MultiPoint([world(p) for p in points]).convex_hull
    review_hull = MultiPoint([world(p, True) for p in points]).convex_hull
    bodies, roofs, lots, per_house, per_plot = {}, {}, {}, [], []
    for b in m['buildings']:
        bid = b['id']
        source = Polygon(b['source_poly'])
        lots[bid] = source
        bp, rp = [], []
        for h in b['houses']:
            key = bid + '/' + h['key']
            body = Polygon(h['poly'])
            # Conservative horizontal envelope; flat parapets have no projecting eave.
            eaves = 0 if h['roof_mode'] == 'flat_parapet' else float(h.get('eaves', .45))
            roof = body.buffer(eaves, join_style=2)
            bodies[key], roofs[key] = body, roof
            bp.append(body); rp.append(roof)
            outside = body.difference(source)
            per_house.append({
                'house': key, 'body_area_m2': body.area, 'roof_envelope_area_m2': roof.area,
                'eaves_m': eaves, 'body_outside_source_m2': outside.area,
                'body_inside_source': outside.area <= 1e-5,
                'roof_outside_source_m2': roof.difference(source).area,
                'source_boundary_distance_m': body.distance(source.boundary),
                'body_polygon_xz': list(body.exterior.coords)[:-1],
                'roof_envelope_xz': list(roof.exterior.coords)[:-1],
            })
        body_group, roof_group = unary_union(bp), unary_union(rp)
        per_plot.append({
            'plot': bid, 'house_count': len(bp), 'source_area_m2': source.area,
            'body_union_area_m2': body_group.area, 'roof_union_area_m2': roof_group.area,
            'body_area_reduction_percent': 100*(1-body_group.area/source.area),
            'body_outside_source_m2': body_group.difference(source).area,
            'body_union_bounds_xz': list(body_group.bounds),
            'roof_union_bounds_xz': list(roof_group.bounds),
        })

    def pairs(shapes):
        names = list(shapes)
        results = []
        for i, a in enumerate(names):
            for b in names[i+1:]:
                pa, pb = shapes[a], shapes[b]
                results.append({'a': a, 'b': b, 'intersection_area_m2': pa.intersection(pb).area,
                                'distance_m': pa.distance(pb),
                                'same_plot': a.split('/')[0] == b.split('/')[0]})
        return results

    body_pairs, roof_pairs, lot_pairs = pairs(bodies), pairs(roofs), pairs(lots)
    target = 'camo_204696009'
    target_body = unary_union([p for k, p in bodies.items() if k.startswith(target + '/')])
    target_roof = unary_union([p for k, p in roofs.items() if k.startswith(target + '/')])

    def garage_compare(hull):
        return {label: {'intersection_area_m2': p.intersection(hull).area,
                        'distance_m': p.distance(hull)}
                for label, p in [('original_source_lot', lots[target]),
                                 ('v2_body', target_body), ('v2_roof_envelope', target_roof)]}

    data = {
        'scope': 'Static manifest 2D footprint audit plus actual garage FBX convex hull; NOT live-map terrain/Unity verification.',
        'manifest_sha256': hashlib.sha256(manifest_bytes).hexdigest(),
        'garage_source': str(GARAGE.relative_to(R)),
        'garage_sha256': hashlib.sha256(GARAGE.read_bytes()).hexdigest(),
        'limitations': ['Convex hull includes every mesh and may fill empty garage yard/concavities.',
                       'Roofs use conservative 2D eave envelopes; height separation is not tested.',
                       'House attachments, foundation extensions, stairs and actual generated FBX geometry are not audited.',
                       'Live ground burial, Unity FBX import handedness and collider clearance are NOT verified.'],
        'garage': {
            'mesh_count': len(mesh_records), 'vertex_count': len(points),
            'mesh_names': [ob['name'] for ob in mesh_records],
            'blender_bounds_xyz': [[min(p[i] for p in points) for i in range(3)],
                                   [max(p[i] for p in points) for i in range(3)]],
            'local_hull_unity_xz': list(local_hull.exterior.coords)[:-1],
            'placement': placement,
            'transform': 'local=(Blender x,-y); world x=wx+cos(yaw)*x+sin(yaw)*z; world z=wz-sin(yaw)*x+cos(yaw)*z',
            'world_hull_xz': list(garage_hull.exterior.coords)[:-1],
            'world_bounds_xz': list(garage_hull.bounds),
            'target_comparison': garage_compare(garage_hull),
            'existing_review_negative_blender_yaw': {
                'note': 'env_auto_bpy.py applies Blender -yaw; under x,-y mapping that is the opposite Unity yaw. Included separately without changing source.',
                'world_hull_xz': list(review_hull.exterior.coords)[:-1],
                'world_bounds_xz': list(review_hull.bounds),
                'target_comparison': garage_compare(review_hull),
            },
        },
        'houses': per_house, 'plots': per_plot,
        'body_pairs': body_pairs, 'roof_pairs': roof_pairs, 'source_lot_pairs': lot_pairs,
        'summary': {
            'plots': len(lots), 'houses': len(bodies),
            'body_outside_source_count': sum(not h['body_inside_source'] for h in per_house),
            'body_overlap_count': sum(p['intersection_area_m2'] > 1e-5 for p in body_pairs),
            'roof_envelope_overlap_count': sum(p['intersection_area_m2'] > 1e-5 for p in roof_pairs),
            'source_lot_overlap_count': sum(p['intersection_area_m2'] > 1e-5 for p in lot_pairs),
            'minimum_body_gap_m': min(p['distance_m'] for p in body_pairs),
            'minimum_roof_envelope_gap_m': min(p['distance_m'] for p in roof_pairs),
            'target_reduction_percent': next(p['body_area_reduction_percent'] for p in per_plot if p['plot'] == target),
        },
    }
    data['review_ground'] = review_ground_audit(m)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'summary': data['summary'], 'garage': data['garage']['target_comparison'],
                      'review_garage': data['garage']['existing_review_negative_blender_yaw']['target_comparison'],
                      'review_ground': data['review_ground']['summary']}, indent=2))


if __name__ == '__main__':
    if '--blender-extract' in sys.argv:
        extract_blender()
    else:
        main()
