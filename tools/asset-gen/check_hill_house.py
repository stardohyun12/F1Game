"""Audit exported hill-house geometry and atlas without trusting author metadata.

GLB uses metre/Y-up world positions and top-down image UVs. Legacy atlas slot
numbers are bottom-up (8 x 8 cells, 64 px each). Every triangle must fit inside
one permitted cell, and both singular values of its world-to-pixel Jacobian must
meet the density tolerance. This script never writes the model or source atlas.
"""
from pathlib import Path
import argparse
import csv
import hashlib
import io
import json
import sys

import numpy as np
from PIL import Image

from check_asset import accessor, node_matrix, read_glb
from photo_to_pixel import load_palette

ROOT = Path(__file__).resolve().parents[2]


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def tile_bounds(slot, size=512, cell=64):
    """Pixel rectangle in top-down PNG/glTF UV coordinates."""
    columns = size // cell
    x = (slot % columns) * cell
    y = size - (slot // columns + 1) * cell
    return np.array([x, y], dtype=float), np.array([x + cell, y + cell], dtype=float)


def triangle_density(points, pixels):
    """Return the principal pixel/metre scales, rejecting degenerate input."""
    edges = np.column_stack([points[1] - points[0], points[2] - points[0]])
    area2 = np.linalg.norm(np.cross(edges[:, 0], edges[:, 1]))
    if not np.isfinite(area2) or area2 < 1e-12:
        raise ValueError('Degenerate world-space geometry')
    _, basis = np.linalg.qr(edges, mode='reduced')
    pixel_edges = np.column_stack([pixels[1] - pixels[0], pixels[2] - pixels[0]])
    scales = np.linalg.svd(pixel_edges @ np.linalg.inv(basis), compute_uv=False)
    if not np.all(np.isfinite(scales)) or min(scales) < 1e-8:
        raise ValueError('Degenerate UV mapping')
    return [float(min(scales)), float(max(scales))]


def inspect(args):
    texture = Image.open(args.texture).convert('RGBA')
    width, height = texture.size
    palette = {tuple(c) for c in load_palette(args.palette)}
    array = np.asarray(texture)
    # Check all RGB pixels, including transparent ones: no hidden outside colours.
    colors = {tuple(map(int, c)) for c in array[:, :, :3].reshape(-1, 3)}
    outside = sorted(colors - palette)
    g, blob = read_glb(args.model)
    allowed = {slot: tile_bounds(slot) for slot in args.tiles}
    vertices, density_rows, failures, materials = [], [], [], []
    used_tiles = {}
    tris = 0
    geometry_error_count = 0

    def material_record(primitive):
        index = primitive.get('material')
        if index is None:
            raise ValueError('Every primitive requires a textured material')
        material = g['materials'][index]
        pbr = material.get('pbrMetallicRoughness', {})
        info = pbr.get('baseColorTexture')
        if not info:
            raise ValueError('Material has no baseColorTexture')
        if info.get('texCoord', 0) != 0 or info.get('extensions'):
            raise ValueError('Non-default or transformed texture coordinates unsupported; fail closed')
        tex = g['textures'][info['index']]
        sampler = g.get('samplers', [])[tex['sampler']] if 'sampler' in tex else {}
        meta = g['images'][tex['source']]
        if 'bufferView' not in meta:
            raise ValueError('Expected image embedded in GLB')
        view = g['bufferViews'][meta['bufferView']]
        start = view.get('byteOffset', 0)
        embedded = Image.open(io.BytesIO(blob[start:start + view['byteLength']])).convert('RGBA')
        return {
            'material_index': index,
            'name': material.get('name', ''),
            'sampler': sampler,
            'point_sampling': sampler.get('magFilter') == 9728 and sampler.get('minFilter') in (9728, 9984),
            'embedded_texture_matches_png': embedded.size == texture.size and embedded.tobytes() == texture.tobytes(),
            'no_normal_map': 'normalTexture' not in material,
            'metallic_zero': pbr.get('metallicFactor', 1) == 0,
        }

    def walk(index, parent, ancestors):
        nonlocal tris, geometry_error_count
        if index in ancestors:
            raise ValueError('Node hierarchy cycle')
        node = g['nodes'][index]
        world = parent @ node_matrix(node)
        if not np.all(np.isfinite(world)):
            raise ValueError('Non-finite node transform')
        if 'skin' in node or node.get('weights'):
            raise ValueError('Skinned or morphed geometry is not supported')
        if 'mesh' in node:
            mesh = g['meshes'][node['mesh']]
            for primitive_index, primitive in enumerate(mesh['primitives']):
                if primitive.get('mode', 4) != 4 or primitive.get('targets'):
                    raise ValueError('Expected static triangle primitives')
                attributes = primitive['attributes']
                if 'POSITION' not in attributes or 'TEXCOORD_0' not in attributes:
                    raise ValueError('Missing POSITION or TEXCOORD_0')
                positions = accessor(g, blob, attributes['POSITION']).astype(float)
                uv = accessor(g, blob, attributes['TEXCOORD_0']).astype(float)
                if positions.shape[1] != 3 or uv.shape != (len(positions), 2):
                    raise ValueError('Invalid position/UV dimensions')
                positions = (np.column_stack([positions, np.ones(len(positions))]) @ world.T)[:, :3]
                if not np.all(np.isfinite(positions)) or not np.all(np.isfinite(uv)):
                    raise ValueError('Non-finite vertex or UV')
                indices = accessor(g, blob, primitive['indices']).ravel().astype(int) if 'indices' in primitive else np.arange(len(positions))
                if len(indices) % 3 or len(indices) == 0 or indices.min() < 0 or indices.max() >= len(positions):
                    raise ValueError('Invalid triangle indices')
                vertices.extend(positions[np.unique(indices)])
                materials.append(material_record(primitive))
                for ids in indices.reshape(-1, 3):
                    number = tris
                    tris += 1
                    pixels = uv[ids] * [width, height]
                    detail = {'triangle': number, 'node': node.get('name', str(index)), 'primitive': primitive_index}
                    try:
                        scales = triangle_density(positions[ids], pixels)
                        density_rows.append(dict(detail, principal_px_per_m=scales))
                    except ValueError as exc:
                        geometry_error_count += 1
                        failures.append(dict(detail, reason=str(exc)))
                    lo, hi = pixels.min(axis=0), pixels.max(axis=0)
                    slots = [slot for slot, (low, high) in allowed.items() if np.all(lo >= low - 1e-3) and np.all(hi <= high + 1e-3)]
                    if len(slots) != 1:
                        failures.append(dict(detail, reason='Triangle crosses a tile boundary or is outside permitted cells', uv_pixel_bounds=[lo.tolist(), hi.tolist()]))
                    else:
                        used_tiles[slots[0]] = used_tiles.get(slots[0], 0) + 1
        for child in node.get('children', []):
            walk(child, world, ancestors | {index})

    scene = g['scenes'][g.get('scene', 0)]
    for index in scene['nodes']:
        walk(index, np.eye(4), set())
    if not vertices or not tris:
        raise ValueError('No rendered triangle geometry in selected scene')
    bounds = np.array(vertices)
    low, high = bounds.min(axis=0), bounds.max(axis=0)
    dimensions = high - low
    density_min = min((r['principal_px_per_m'][0] for r in density_rows), default=0)
    density_max = max((r['principal_px_per_m'][1] for r in density_rows), default=0)
    tolerance_low = args.density * (1 - args.tolerance)
    tolerance_high = args.density * (1 + args.tolerance)
    bad_density = [r for r in density_rows if r['principal_px_per_m'][0] < tolerance_low - 1e-5 or r['principal_px_per_m'][1] > tolerance_high + 1e-5]
    horizontal_center = ((low + high) / 2)[[0, 2]]
    checks = {
        'palette_max_32': len(palette) <= 32,
        'palette_outside_zero': not outside,
        'texture_power_of_two': all(n > 0 and n & (n - 1) == 0 for n in texture.size),
        'atlas_512_square': texture.size == (512, 512),
        'triangle_budget': 0 < tris <= args.max_tris,
        'all_triangles_have_valid_world_and_uv_area': geometry_error_count == 0,
        'density_both_principal_axes': len(density_rows) == tris and not bad_density,
        'all_triangles_inside_one_permitted_atlas_cell': not any('uv_pixel_bounds' in f for f in failures),
        'embedded_texture_matches_png': all(m['embedded_texture_matches_png'] for m in materials),
        'point_sampling': all(m['point_sampling'] for m in materials),
        'material_no_normal_or_metal': all(m['no_normal_map'] and m['metallic_zero'] for m in materials),
        'pivot_on_floor_y_up': abs(float(low[1])) <= args.pivot_tolerance,
        'origin_inside_horizontal_bounds': bool(low[0] <= args.pivot_tolerance and high[0] >= -args.pivot_tolerance and low[2] <= args.pivot_tolerance and high[2] >= -args.pivot_tolerance),
    }
    if args.dimensions:
        checks['expected_dimensions_m'] = bool(np.allclose(dimensions, args.dimensions, atol=1e-4))
    if args.require_centered_bounds:
        checks['pivot_floor_bounds_center'] = bool(np.all(np.abs(horizontal_center) <= args.pivot_tolerance))
    texture_hash = sha256(args.texture)
    relative = Path(args.texture).resolve().relative_to(ROOT).as_posix()
    provenance_path = Path(args.provenance)
    with provenance_path.open(encoding='utf-8-sig', newline='') as handle:
        provenance = list(csv.DictReader(handle))
    matches = [r for r in provenance if r['path'] == relative and r['sha256'] == texture_hash and r['ai_generated'] == 'no' and r['source_url'] and r['tool'] and r['license']]
    checks['provenance_registered_non_ai'] = bool(matches)
    return {
        'pass': all(checks.values()), 'checks': checks,
        'model': str(Path(args.model)), 'texture': str(Path(args.texture)),
        'tris': tris, 'triangle_budget': args.max_tris,
        'texture_size': list(texture.size), 'palette_colors': len(palette),
        'used_colors': len(colors), 'outside_color_count': len(outside), 'outside_colors': outside[:32],
        'density_target': args.density, 'density_tolerance': args.tolerance,
        'density_min': density_min, 'density_max': density_max,
        'density_failures': bad_density, 'per_triangle_density': density_rows,
        'atlas_grid': [8, 8], 'atlas_tile_size': 64, 'allowed_tiles': args.tiles,
        'tile_numbering': 'Original Blender atlas bottom-up rows; GLB/PNG image UVs converted to top-down rectangles',
        'used_tile_triangle_counts': used_tiles, 'geometry_or_uv_failures': failures,
        'dimensions_m': dimensions.tolist(), 'bounds_min_m': low.tolist(), 'bounds_max_m': high.tolist(),
        'horizontal_bounds_center_offset_xz_m': horizontal_center.tolist(),
        'pivot_scope': 'Floor Y=0 and origin within footprint; asymmetric external props need not center full bounds unless --require-centered-bounds is passed.',
        'materials': materials, 'provenance_rows': [r['asset_id'] for r in matches],
        'model_sha256': sha256(args.model), 'texture_sha256': texture_hash,
        'method': 'Decode exported GLB accessor positions, indices, UVs and parent/node transforms; measure both singular values of each world-to-UV-pixel Jacobian. No producer triangle/density claims used.',
        'limitations': 'Does not verify artistic wear, source licensing beyond recorded provenance, architectural realism, scene distance readability or Unity import settings.',
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--model', default=ROOT / 'art/models/hill_house_v01/hill_house_v01.glb', type=Path)
    parser.add_argument('--texture', default=ROOT / 'art/models/hill_house_v01/HillHouseAtlas.png', type=Path)
    parser.add_argument('--palette', default=ROOT / 'art/source/palette/pitcrew_d123_32.json', type=Path)
    parser.add_argument('--provenance', default=ROOT / 'art/provenance.csv', type=Path)
    parser.add_argument('--out', default=ROOT / 'art/_review/2026-10-23-hillhouse/check-hillhouse.json', type=Path)
    parser.add_argument('--density', type=float, default=32)
    parser.add_argument('--tolerance', type=float, default=.1)
    parser.add_argument('--max-tris', type=int, default=2500)
    parser.add_argument('--tiles', type=int, nargs='+', default=list(range(51, 59)))
    parser.add_argument('--dimensions', type=float, nargs=3)
    parser.add_argument('--pivot-tolerance', type=float, default=1e-4)
    parser.add_argument('--require-centered-bounds', action='store_true')
    args = parser.parse_args()
    try:
        if args.density <= 0 or not 0 <= args.tolerance < 1 or len(set(args.tiles)) != len(args.tiles) or any(not 0 <= t < 64 for t in args.tiles):
            raise ValueError('Invalid audit configuration')
        result = inspect(args)
    except Exception as exc:
        result = {'pass': False, 'error': f'{type(exc).__name__}: {exc}'}
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    summary = {k: v for k, v in result.items() if k not in ('per_triangle_density', 'materials', 'density_failures', 'geometry_or_uv_failures')}
    print(json.dumps(summary, ensure_ascii=False))
    return 0 if result['pass'] else 1


if __name__ == '__main__':
    sys.exit(main())
