"""Independent exported-car audit: decoded GLB, not producer measurement claims.

All referenced triangles are checked at both principal UV texel scales. Flat
windows are derived from actual coplanar geometry/UV coverage, not an optional
surface manifest. Source dimensions and wheel pivots are the measured current
CoastalFormula asset, converted from Blender Z-up to GLB Y-up.
"""
from pathlib import Path
from collections import defaultdict
import argparse
import csv
import hashlib
import io
import json
import sys
from datetime import date

import numpy as np
from PIL import Image

from check_asset import accessor, node_matrix, read_glb
from check_hill_house import triangle_density
from photo_to_pixel import load_palette

ROOT = Path(__file__).resolve().parents[2]
EXPECTED_LOW = np.array([-.979454517, 0., -1.754999995])
EXPECTED_HIGH = np.array([.979454517, 1.072000027, 2.029999971])
EXPECTED_WHEELS = {
    'wheel_front_left': [.79, .35, 1.2],
    'wheel_front_right': [-.79, .35, 1.2],
    'wheel_rear_left': [.79, .375, -1.1],
    'wheel_rear_right': [-.79, .375, -1.1],
}


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def explicitly_non_ai(value):
    """CSV forms used in this project; unknown/empty/positive values fail."""
    return isinstance(value, str) and value.strip().casefold() in {'no', 'false', '0', '아니오', '비ai'}


def provenance_date(row):
    value = row.get('download_date') or row.get('accessed')
    try:
        return date.fromisoformat(value).isoformat()
    except (ValueError, TypeError):
        return None


def covered_pixel_centers(triangles, width, height):
    """Actual UV coverage at pixel centers; no neighbouring island or gutter."""
    mask = np.zeros((height, width), dtype=bool)
    for p in triangles:
        p = np.asarray(p, dtype=float)
        lo = np.maximum(np.floor(p.min(axis=0)).astype(int), [0, 0])
        hi = np.minimum(np.ceil(p.max(axis=0)).astype(int), [width, height])
        if np.any(hi <= lo):
            continue
        xx, yy = np.meshgrid(np.arange(lo[0], hi[0]) + .5, np.arange(lo[1], hi[1]) + .5)
        q = np.stack((xx, yy), axis=-1) - p[0]
        e0, e1 = p[1]-p[0], p[2]-p[0]
        den = e0[0]*e1[1]-e0[1]*e1[0]
        if abs(den) < 1e-10:
            continue
        u = (q[..., 0]*e1[1]-q[..., 1]*e1[0])/den
        v = (e0[0]*q[..., 1]-e0[1]*q[..., 0])/den
        inside = (u >= -1e-8) & (v >= -1e-8) & (u+v <= 1+1e-8)
        mask[lo[1]:hi[1], lo[0]:hi[0]] |= inside
    return mask


def connected_coplanar_surfaces(planes):
    """Do not let a disconnected textured patch rescue a blank coplanar panel."""
    result = []
    for plane in planes:
        world = plane.get('world_triangles')
        if world is None:  # Explicit synthetic test fixtures are one surface.
            result.append(plane)
            continue
        parent = list(range(len(world)))
        def find(i):
            while parent[i] != i:
                parent[i] = parent[parent[i]]
                i = parent[i]
            return i
        edges = {}
        for i, triangle in enumerate(world):
            vertices = [tuple(np.round(p, 5)) for p in triangle]
            for a, b in ((0, 1), (1, 2), (2, 0)):
                edge = tuple(sorted((vertices[a], vertices[b])))
                if edge in edges:
                    parent[find(i)] = find(edges[edge])
                else:
                    edges[edge] = i
        groups = defaultdict(list)
        for i in range(len(world)):
            groups[find(i)].append(i)
        for number, indices in enumerate(groups.values()):
            row = {k: plane[k] for k in ('node', 'normal', 'plane')}
            row['component'] = number
            row['area_m2'] = sum(float(np.linalg.norm(np.cross(world[i][1]-world[i][0], world[i][2]-world[i][0]))/2) for i in indices)
            row['triangles'] = [plane['triangles'][i] for i in indices]
            result.append(row)
    return result


def flat_surface_evidence(texture, surfaces, min_area=.04, window=8):
    """Car policy v2: detect blank/near-blank connected large face interiors.

    >=2 RGB colours and largest-colour occupancy <=98% are required over each
    actual covered surface. A single different pixel is insufficient on a large
    panel. 8x8 monochrome windows are kept as diagnostics, not an automatic art
    rejection; broad quiet paint and localized wear can be intentional.
    """
    arr = np.asarray(texture.convert('RGBA'))
    height, width = arr.shape[:2]
    failures, records = [], []
    for surface in surfaces:
        if surface['area_m2'] < min_area - 1e-10:
            continue
        mask = covered_pixel_centers(surface['triangles'], width, height)
        yy, xx = np.nonzero(mask)
        record = {k: surface[k] for k in ('node', 'normal', 'plane', 'area_m2')}
        record['component'] = surface.get('component', 0)
        record['covered_pixels'] = int(mask.sum())
        record['checked_8x8_windows'] = 0
        record['flat_8x8_windows'] = []
        if not len(xx):
            failures.append(dict(record, reason='No occupied texel centers on a large surface'))
            records.append(record)
            continue
        occupied = arr[mask]
        record['covered_bounds_xyxy'] = [int(xx.min()), int(yy.min()), int(xx.max())+1, int(yy.max())+1]
        rgb, counts = np.unique(occupied[:, :3], axis=0, return_counts=True)
        record['unique_rgb'] = len(rgb)
        dominant = int(np.argmax(counts))
        record['dominant_rgb'] = rgb[dominant].tolist()
        record['dominant_fraction'] = float(counts[dominant]/len(occupied))
        record['minority_pixel_count'] = int(len(occupied)-counts[dominant])
        if np.any(occupied[:, 3] != 255):
            failures.append(dict(record, reason='Large surface contains nonopaque texture pixels'))
        for y in range(int(yy.min()), int(yy.max())-window+2):
            for x in range(int(xx.min()), int(xx.max())-window+2):
                if not mask[y:y+window, x:x+window].all():
                    continue
                record['checked_8x8_windows'] += 1
                sample = arr[y:y+window, x:x+window, :3]
                if np.all(sample == sample[0, 0]):
                    record['flat_8x8_windows'].append([x, y, window, window])
        if record['unique_rgb'] < 2 or record['dominant_fraction'] > .98+1e-12:
            failures.append(dict(record, reason='Blank/near-blank large surface: need >=2 RGB colours and dominant fraction <=98%'))
        records.append(record)
    return {'pass': bool(records) and not failures, 'policy': 'car-v2-connected-surface-occupancy', 'minimum_area_m2': min_area,
            'minimum_unique_rgb': 2, 'maximum_dominant_fraction': .98,
            'window': [window, window], 'surfaces_checked': len(records),
            'total_checked_windows': sum(r['checked_8x8_windows'] for r in records),
            'diagnostic_monochrome_8x8_windows': sum(len(r['flat_8x8_windows']) for r in records),
            'diagnostic_8x8_is_pass_condition': False,
            'failures': failures, 'surfaces': records,
            'scope': 'Actual connected coplanar face UV masks; texture gutters and disconnected/neighbouring surfaces excluded. Large face occupancy is a blank-surface guard, not proof of artistic quality or meaningful wear. 8x8 windows retained as diagnostics; prior cargo policy is unchanged.'}


def inspect(args):
    image = Image.open(args.texture).convert('RGBA')
    width, height = image.size
    palette = {tuple(c) for c in load_palette(args.palette)}
    colors = {tuple(map(int, c)) for c in np.asarray(image)[:, :, :3].reshape(-1, 3)}
    outside = sorted(colors-palette)
    g, blob = read_glb(args.model)
    tris, density, geometry_errors, uv_errors = 0, [], [], []
    vertices, materials, transforms = [], [], []
    planes = {}
    node_data = []
    seen = set()

    def material(primitive):
        if 'material' not in primitive:
            raise ValueError('Primitive without material')
        mat = g['materials'][primitive['material']]
        pbr = mat.get('pbrMetallicRoughness', {})
        info = pbr.get('baseColorTexture')
        if not info or info.get('texCoord', 0) != 0 or info.get('extensions'):
            raise ValueError('Expected untransformed TEXCOORD_0 baseColorTexture')
        tex = g['textures'][info['index']]
        sampler = g.get('samplers', [])[tex['sampler']] if 'sampler' in tex else {}
        imeta = g['images'][tex['source']]
        if 'bufferView' not in imeta:
            raise ValueError('Texture must be embedded in GLB')
        view = g['bufferViews'][imeta['bufferView']]
        off = view.get('byteOffset', 0)
        embedded = Image.open(io.BytesIO(blob[off:off+view['byteLength']])).convert('RGBA')
        factors = pbr.get('baseColorFactor', [1, 1, 1, 1])
        return {'name': mat.get('name', ''), 'index': primitive['material'],
                'point_sampling': sampler.get('magFilter') == 9728 and sampler.get('minFilter') in (9728, 9984),
                'sampler': sampler,
                'embedded_texture_matches_png': embedded.size == image.size and embedded.tobytes() == image.tobytes(),
                'no_normal_map_or_metal': 'normalTexture' not in mat and pbr.get('metallicFactor', 1) == 0,
                'base_color_factor_white': bool(np.allclose(factors, [1, 1, 1, 1], atol=1e-6)),
                'opaque': mat.get('alphaMode', 'OPAQUE') == 'OPAQUE'}

    def walk(index, parent, ancestors, parent_index=None):
        nonlocal tris
        if index in seen or index in ancestors:
            raise ValueError('Shared or cyclic node references are unsupported; fail closed')
        seen.add(index)
        node = g['nodes'][index]
        world = parent @ node_matrix(node)
        if not np.all(np.isfinite(world)):
            raise ValueError('Nonfinite node transform')
        if 'skin' in node or node.get('weights'):
            raise ValueError('Unexpected skinned or morphed geometry')
        name = node.get('name', str(index))
        info = {'node_index': index, 'name': name, 'parent_index': parent_index,
                'world_origin': world[:3, 3].tolist(), 'world_matrix': world.tolist(),
                'has_mesh': 'mesh' in node, 'vertices': []}
        if 'mesh' in node:
            for pi, primitive in enumerate(g['meshes'][node['mesh']]['primitives']):
                if primitive.get('mode', 4) != 4 or primitive.get('targets'):
                    raise ValueError('Only static TRIANGLES primitives supported')
                attrs = primitive['attributes']
                if 'POSITION' not in attrs or 'TEXCOORD_0' not in attrs:
                    raise ValueError('Missing vertex positions or UV0')
                p = accessor(g, blob, attrs['POSITION']).astype(float)
                uv = accessor(g, blob, attrs['TEXCOORD_0']).astype(float)
                if p.shape[1] != 3 or uv.shape != (len(p), 2):
                    raise ValueError('Invalid position/UV dimensions')
                p = (np.column_stack((p, np.ones(len(p)))) @ world.T)[:, :3]
                if not np.isfinite(p).all() or not np.isfinite(uv).all():
                    raise ValueError('Nonfinite position or UV')
                ids = accessor(g, blob, primitive['indices']).ravel().astype(int) if 'indices' in primitive else np.arange(len(p))
                if not len(ids) or len(ids) % 3 or ids.min() < 0 or ids.max() >= len(p):
                    raise ValueError('Invalid triangle indices')
                used = p[np.unique(ids)]
                vertices.extend(used); info['vertices'].extend(used.tolist())
                materials.append(material(primitive))
                for corners in ids.reshape(-1, 3):
                    tri, pix = p[corners], uv[corners]*[width, height]
                    row = {'triangle': tris, 'node': name, 'primitive': pi}
                    tris += 1
                    try:
                        scales = triangle_density(tri, pix)
                        density.append(dict(row, principal_px_per_m=scales))
                    except ValueError as exc:
                        geometry_errors.append(dict(row, reason=str(exc)))
                        continue
                    if (pix < -1e-3).any() or (pix > [width+1e-3, height+1e-3]).any():
                        uv_errors.append(dict(row, reason='UV outside atlas', bounds=[pix.min(axis=0).tolist(), pix.max(axis=0).tolist()]))
                    normal = np.cross(tri[1]-tri[0], tri[2]-tri[0])
                    area2 = np.linalg.norm(normal); normal /= area2
                    offset = float(normal@tri[0])
                    key = (index, primitive.get('material'), *np.round(normal, 5), round(offset, 5))
                    if key not in planes:
                        planes[key] = {'node': name, 'normal': normal.tolist(), 'plane': offset, 'area_m2': 0., 'triangles': [], 'world_triangles': []}
                    planes[key]['area_m2'] += float(area2/2)
                    planes[key]['triangles'].append(pix)
                    planes[key]['world_triangles'].append(tri)
        node_data.append(info)
        for child in node.get('children', []):
            walk(child, world, ancestors | {index}, index)

    for index in g['scenes'][g.get('scene', 0)]['nodes']:
        walk(index, np.eye(4), set())
    if not tris or not vertices:
        raise ValueError('No rendered geometry')
    allv = np.asarray(vertices)
    low, high = allv.min(axis=0), allv.max(axis=0)
    wheel_records = []
    wheel_nodes = [n for n in node_data if n['name'].startswith('wheel_')]
    for name, expected in EXPECTED_WHEELS.items():
        matches = [n for n in node_data if n['name'] == name]
        if len(matches) != 1:
            wheel_records.append({'name': name, 'pass': False, 'reason': 'Expected exactly one named wheel node'})
            continue
        n = matches[0]
        if not n['vertices']:
            wheel_records.append({'name': name, 'pass': False, 'reason': 'Named wheel must own independent mesh geometry'})
            continue
        p = np.asarray(n['vertices']); blo, bhi = p.min(axis=0), p.max(axis=0)
        origin = np.asarray(n['world_origin'])
        axis = np.asarray(n['world_matrix'])[:3, 0]
        axis /= np.linalg.norm(axis)
        center_error = float(np.max(np.abs((blo+bhi)/2-origin)))
        pivot_error = float(np.max(np.abs(origin-expected)))
        checks = {'source_pivot_preserved': pivot_error <= args.position_tolerance,
                  'center_at_mesh_bounds': center_error <= args.wheel_center_tolerance,
                  'spin_axis_x': bool(np.allclose(axis, [1, 0, 0], atol=1e-5)),
                  'bottom_at_ground': abs(float(blo[1])) <= args.position_tolerance,
                  'not_child_of_another_wheel': n['parent_index'] not in [w['node_index'] for w in wheel_nodes]}
        wheel_records.append({'name': name, 'pass': all(checks.values()), 'checks': checks,
                              'origin_yup': origin.tolist(), 'expected_origin_yup': expected,
                              'pivot_error_max_m': pivot_error, 'bounds_center_error_max_m': center_error,
                              'spin_axis_yup': axis.tolist(), 'bounds_min': blo.tolist(), 'bounds_max': bhi.tolist()})

    flat = flat_surface_evidence(image, connected_coplanar_surfaces(list(planes.values())), args.min_flat_area)
    target_density = getattr(args, 'density', 64)
    triangle_budget = getattr(args, 'max_tris', 3000)
    bad_density = [r for r in density if r['principal_px_per_m'][0] < target_density*.9-1e-5 or r['principal_px_per_m'][1] > target_density*1.1+1e-5]
    texture_hash = sha256(args.texture)
    relative = Path(args.texture).resolve().relative_to(ROOT).as_posix()
    with Path(args.provenance).open(encoding='utf-8-sig', newline='') as f:
        rows = list(csv.DictReader(f))
    matches = [r for r in rows if r.get('path') == relative and r.get('sha256') == texture_hash and explicitly_non_ai(r.get('ai_generated')) and provenance_date(r) and all(r.get(k) for k in ['asset_id', 'source_url', 'license', 'tool'])]
    checks = {
        'palette_max_32': len(palette) <= 32,
        'palette_outside_zero': not outside,
        'texture_512_square': image.size == (512, 512),
        'texture_power_of_two': all(n > 0 and n & (n-1) == 0 for n in image.size),
        f'tris_at_most_{triangle_budget}': 0 < tris <= triangle_budget,
        'all_triangle_geometry_and_uv_nondegenerate': not geometry_errors,
        f'density_{target_density}_both_principal_axes_all_triangles': len(density) == tris and not bad_density,
        'uv_all_inside_atlas': not uv_errors,
        'source_bounds_and_origin_preserved': bool(np.allclose(low, EXPECTED_LOW, atol=args.position_tolerance, rtol=0) and np.allclose(high, EXPECTED_HIGH, atol=args.position_tolerance, rtol=0)),
        'four_independent_named_wheels': len(wheel_nodes) == 4 and all(w['pass'] for w in wheel_records),
        'point_sampling': bool(materials) and all(m['point_sampling'] for m in materials),
        'embedded_texture_matches_png': bool(materials) and all(m['embedded_texture_matches_png'] for m in materials),
        'material_no_normal_or_metal': all(m['no_normal_map_or_metal'] for m in materials),
        'base_color_factor_white': all(m['base_color_factor_white'] for m in materials),
        'opaque_materials': all(m['opaque'] for m in materials),
        'no_blank_or_near_blank_large_connected_surfaces': flat['pass'],
        'provenance_registered_current_hash_non_ai': bool(matches),
    }
    return {'pass': all(checks.values()), 'checks': checks, 'model': str(args.model), 'texture': str(args.texture),
            'model_sha256': sha256(args.model), 'texture_sha256': texture_hash,
            'tris': tris, 'triangle_budget': triangle_budget, 'texture_size': list(image.size),
            'palette_colors': len(palette), 'used_colors': len(colors), 'outside_color_count': len(outside), 'outside_colors': outside[:32],
            'density_target': target_density, 'density_tolerance': .1,
            'density_min': min((r['principal_px_per_m'][0] for r in density), default=0),
            'density_max': max((r['principal_px_per_m'][1] for r in density), default=0),
            'density_failures': bad_density, 'per_triangle_density': density,
            'geometry_failures': geometry_errors, 'uv_failures': uv_errors,
            'bounds_min_yup': low.tolist(), 'bounds_max_yup': high.tolist(), 'dimensions_yup': (high-low).tolist(),
            'expected_bounds_min_yup': EXPECTED_LOW.tolist(), 'expected_bounds_max_yup': EXPECTED_HIGH.tolist(),
            'position_tolerance_m': args.position_tolerance, 'wheel_center_tolerance_m': args.wheel_center_tolerance,
            'wheels': wheel_records, 'materials': materials, 'large_surface_variation': flat,
            'provenance_rows': [r['asset_id'] for r in matches],
            'provenance_non_ai_raw_values': [r['ai_generated'] for r in matches],
            'provenance_non_ai_accepted_values': ['no', 'false', '0', '아니오', '비AI'],
            'provenance_access_dates': [provenance_date(r) for r in matches],
            'coordinate_contract': 'GLB world = (Blender X, Blender Z, -Blender Y). Nose +Z, up +Y; left-labelled wheel lies +X in this source GLB. Existing Unity FBX import mirrors X; match the measured Unity prefab names/positions when wiring.',
            'method': 'Decode exported GLB POSITION, TEXCOORD_0, indices, embedded textures, hierarchy and node transforms. Principal world-to-pixel scales checked for every triangle; no producer count/density metadata trusted.',
            'limitations': 'Does not validate Unity importer, runtime material swaps/wheel animation, artistic readability, provenance license validity beyond recorded fields, or meaning of wear.'}


def self_test():
    tri = np.array([[0., 0., 0.], [1., 0., 0.], [0., 1., 0.]])
    scales = triangle_density(tri, np.array([[0., 0.], [64., 0.], [0., 64.]]))
    assert np.allclose(scales, [64, 64])
    assert np.allclose(triangle_density(tri, np.array([[0., 0.], [32., 0.], [0., 32.]])), [32,32])
    distorted = triangle_density(tri, np.array([[0., 0.], [64., 0.], [0., 128.]]))
    assert distorted[1] > 70.4
    try:
        triangle_density(tri, np.array([[0., 0.], [0., 0.], [0., 0.]]))
        raise AssertionError('Degenerate UV was not rejected')
    except ValueError:
        pass
    surfaces = [{'node': 'fixture', 'normal': [0, 0, 1], 'plane': 0, 'area_m2': 1., 'triangles': [np.array([[0, 0], [32, 0], [0, 32]]), np.array([[32, 0], [32, 32], [0, 32]])]}]
    blank = Image.new('RGB', (64, 64), (20, 20, 20))
    assert not flat_surface_evidence(blank, surfaces)['pass']
    textured = np.asarray(blank).copy()
    textured[::4, ::4] = (40, 40, 40)
    assert flat_surface_evidence(Image.fromarray(textured), surfaces)['pass']
    # Variation only in neighbouring atlas space must not rescue a blank face.
    neighbour = np.asarray(blank).copy(); neighbour[:, 33:] = (255, 255, 255)
    assert not flat_surface_evidence(Image.fromarray(neighbour), surfaces)['pass']
    one_pixel = np.asarray(blank).copy(); one_pixel[10, 10] = (40, 40, 40)
    assert not flat_surface_evidence(Image.fromarray(one_pixel), surfaces)['pass']
    # A restrained edge band with a quiet central paint field is allowed. This
    # intentionally contains many monochrome 8x8 windows, still reported.
    edge_band = np.asarray(blank).copy(); edge_band[:2, :32] = (40, 40, 40)
    evidence = flat_surface_evidence(Image.fromarray(edge_band), surfaces)
    assert evidence['pass'] and evidence['diagnostic_monochrome_8x8_windows'] > 0
    # Two disconnected patches in one plane become independent surfaces.
    split = [{'node': 'fixture', 'normal': [0, 0, 1], 'plane': 0, 'area_m2': 1.,
              'triangles': [np.array([[0, 0], [32, 0], [0, 32]]), np.array([[32, 0], [64, 0], [32, 32]])],
              'world_triangles': [np.array([[0, 0, 0], [1, 0, 0], [0, 1, 0]]), np.array([[3, 0, 0], [4, 0, 0], [3, 1, 0]])]}]
    parts = connected_coplanar_surfaces(split)
    assert len(parts) == 2
    mixed = np.asarray(blank).copy(); mixed[::4, 36::4] = (40, 40, 40)
    split_evidence = flat_surface_evidence(Image.fromarray(mixed), parts)
    assert not split_evidence['pass']
    assert all(explicitly_non_ai(v) for v in ['no', ' false ', '0', '아니오', '비AI'])
    assert not any(explicitly_non_ai(v) for v in ['yes', 'true', '1', '예', 'AI', '', None, 'unknown'])
    assert provenance_date({'accessed': '2026-10-23'}) == '2026-10-23'
    assert provenance_date({'download_date': '2026-10-23'}) == '2026-10-23'
    assert provenance_date({'accessed': 'invalid'}) is None
    return {'pass': True, 'policy': 'car-v2-connected-surface-occupancy', 'tests': ['64px/m metric reproduction', 'anisotropic stretch detection', 'collapsed UV rejection', 'blank face rejection', 'textured face pass', 'neighbour island cannot rescue blank face', 'one different pixel cannot rescue blank face', 'localized edge wear with broad quiet paint allowed; 8x8 diagnostics retained', 'disconnected coplanar panels checked separately', 'explicit non-AI CSV spellings accepted; positive and ambiguous values rejected', 'accessed/download_date ISO dates accepted; invalid dates rejected']}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--model', type=Path, default=ROOT/'art/models/car_v01/car_v01.glb')
    parser.add_argument('--texture', type=Path, default=ROOT/'art/models/car_v01/CarAtlas.png')
    parser.add_argument('--palette', type=Path, default=ROOT/'art/source/palette/pitcrew_d123_32.json')
    parser.add_argument('--provenance', type=Path, default=ROOT/'art/provenance.csv')
    parser.add_argument('--out', type=Path, default=ROOT/'art/_review/2026-10-23-car/check_car.json')
    parser.add_argument('--position-tolerance', type=float, default=.0001)
    parser.add_argument('--wheel-center-tolerance', type=float, default=.001)
    parser.add_argument('--min-flat-area', type=float, default=.04)
    parser.add_argument('--self-test', action='store_true')
    parser.add_argument('--density', type=int, choices=[32,64], default=64,
                        help='Explicit task contract; legacy car remains 64')
    parser.add_argument('--max-tris', type=int, choices=[3000,6000], default=3000,
                        help='D142 candidate contract permits 6000; legacy stays 3000')
    args = parser.parse_args()
    try:
        result = self_test() if args.self_test else inspect(args)
    except Exception as exc:
        result = {'pass': False, 'error': f'{type(exc).__name__}: {exc}'}
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k in ['pass', 'checks', 'error', 'tris', 'density_min', 'density_max', 'outside_color_count', 'tests']}, ensure_ascii=False))
    return 0 if result['pass'] else 1


if __name__ == '__main__':
    sys.exit(main())
