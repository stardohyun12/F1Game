"""A1 photographic tiles: original albedo + exported metric proof quads.

Called by check_asset.py --texture-manifest. This mode does not change cargo/UI
rules. Palette membership applies to the delivered albedo PNG only, not source
photographs, lit renders, fogged screenshots or a game's final framebuffer.
"""
from pathlib import Path
from datetime import date
from urllib.parse import urlparse
import argparse
import csv
import hashlib
import io
import json
import sys

import numpy as np
from PIL import Image

from check_asset import accessor, node_matrix, read_glb
from check_hill_house import triangle_density
from photo_to_pixel import load_palette

ROOT = Path(__file__).resolve().parents[2]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def canonical_sha(value):
    return hashlib.sha256(json.dumps(value, ensure_ascii=False, sort_keys=True,
                                    separators=(',', ':')).encode('utf-8')).hexdigest()


def rooted(root, value):
    path = Path(value)
    if path.is_absolute():
        raise ValueError('Manifest paths must be repository relative')
    resolved = (root / path).resolve()
    resolved.relative_to(root)
    return resolved


def allowed_url(value, download=False):
    parsed = urlparse(value or '')
    bases = ('polyhaven.com', 'ambientcg.com') + (('polyhaven.org',) if download else ())
    return parsed.scheme == 'https' and any(parsed.hostname == b or
            (parsed.hostname or '').endswith('.' + b) for b in bases)


def explicit_non_ai(value):
    return isinstance(value, str) and value.strip().casefold() in {
        'no', 'false', '0', '아니오', '비ai'}


def declared_non_ai(value):
    return value is False or explicit_non_ai(value)


def provenance_record(path, digest, source_url, rows, processed=False):
    matches = [r for r in rows if r.get('path') == path]
    row = matches[0] if len(matches) == 1 else {}
    try:
        date.fromisoformat(row.get('download_date') or row.get('accessed', ''))
        valid_date = True
    except (ValueError, TypeError):
        valid_date = False
    process_text = (row.get('tool', '') + ' ' + row.get('notes', '')).casefold()
    checks = {
        'exactly_one_path_record': len(matches) == 1,
        'current_file_hash': row.get('sha256') == digest,
        'explicit_non_ai': explicit_non_ai(row.get('ai_generated')),
        'source_url_linked': bool(source_url) and source_url in row.get('source_url', ''),
        'license_recorded': bool(row.get('license')),
        'access_date_valid': valid_date,
        'asset_id_and_tool_recorded': bool(row.get('asset_id') and row.get('tool')),
        'photo_quantization_process_recorded': not processed or (
            'photo_to_pixel' in process_text and
            any(word in process_text for word in ('palette', 'quantiz', '양자화'))),
    }
    return {'pass': all(checks.values()), 'checks': checks,
            'asset_id': row.get('asset_id'), 'path': path,
            'record_count': len(matches), 'recorded_sha256': row.get('sha256')}


def texture_diagnostics(image):
    """Diagnostics cannot prove tileability or require uniformly scattered noise."""
    px = np.asarray(image.convert('RGB'))
    h, w = px.shape[:2]
    flat = 0
    for y in range(max(0, h - 7)):
        for x in range(max(0, w - 7)):
            block = px[y:y+8, x:x+8]
            flat += int(np.all(block == block[0, 0]))
    a = px.astype(float)
    return {
        'diagnostic_only': True,
        'monochrome_8x8_windows': flat,
        'tested_8x8_windows': max(0, h-7) * max(0, w-7),
        'left_right_edge_rgb_mae': float(np.abs(a[:, 0]-a[:, -1]).mean()),
        'top_bottom_edge_rgb_mae': float(np.abs(a[0]-a[-1]).mean()),
        'interior_horizontal_rgb_mae': float(np.abs(a[:, 1:]-a[:, :-1]).mean()),
        'interior_vertical_rgb_mae': float(np.abs(a[1:]-a[:-1]).mean()),
        'scope': 'Quiet fields are allowed; 8x8 windows are not a noise quota. '
                 'Edge RGB deltas do not prove seamless patterns or perceptual tileability.',
    }


def inspect_quad(path, albedo, physical_size, target=64):
    g, blob = read_glb(path)
    vertices, uvs, density, materials, areas = [], [], [], [], []
    seen = set()

    def material_record(prim):
        mat = g['materials'][prim['material']]
        pbr = mat.get('pbrMetallicRoughness', {})
        info = pbr.get('baseColorTexture', {})
        tex = g['textures'][info['index']]
        sampler = g.get('samplers', [])[tex['sampler']] if 'sampler' in tex else {}
        meta = g['images'][tex['source']]
        if 'bufferView' not in meta:
            raise ValueError('Expected embedded albedo in proof quad')
        view = g['bufferViews'][meta['bufferView']]
        start = view.get('byteOffset', 0)
        embedded = Image.open(io.BytesIO(blob[start:start+view['byteLength']])).convert('RGBA')
        checks = {
            'point_no_mipmap_sampling': sampler.get('magFilter') == 9728 and sampler.get('minFilter') == 9728,
            'repeat_wrap': sampler.get('wrapS', 10497) == sampler.get('wrapT', 10497) == 10497,
            'embedded_matches_original_albedo': embedded.size == albedo.size and embedded.tobytes() == albedo.tobytes(),
            'unlit': 'KHR_materials_unlit' in mat.get('extensions', {}),
            'white_base_color_factor': bool(np.allclose(pbr.get('baseColorFactor', [1, 1, 1, 1]), [1, 1, 1, 1], rtol=0, atol=1e-7)),
            'opaque': mat.get('alphaMode', 'OPAQUE') == 'OPAQUE',
            'no_extra_maps_or_material_extensions': not any(k in mat for k in ('normalTexture', 'occlusionTexture', 'emissiveTexture')) and
                'metallicRoughnessTexture' not in pbr and set(mat.get('extensions', {})) == {'KHR_materials_unlit'},
            'no_metal_or_emission': pbr.get('metallicFactor', 1) == 0 and not any(mat.get('emissiveFactor', [0, 0, 0])),
            'untransformed_uv0': info.get('texCoord', 0) == 0 and not info.get('extensions'),
        }
        return {'pass': all(checks.values()), 'checks': checks, 'sampler': sampler}

    def walk(index, parent):
        if index in seen:
            raise ValueError('Shared/cyclic nodes unsupported')
        seen.add(index)
        node = g['nodes'][index]
        world = parent @ node_matrix(node)
        if not np.isfinite(world).all() or 'skin' in node or node.get('weights'):
            raise ValueError('Nonfinite, skinned or morphed proof quad')
        if 'mesh' in node:
            for prim in g['meshes'][node['mesh']]['primitives']:
                if prim.get('mode', 4) != 4 or prim.get('targets'):
                    raise ValueError('Proof quad must use static TRIANGLES')
                attrs = prim['attributes']
                if any(k.startswith('COLOR_') for k in attrs):
                    raise ValueError('Vertex tint not allowed in neutral albedo proof')
                pos = accessor(g, blob, attrs['POSITION']).astype(float)
                uv = accessor(g, blob, attrs['TEXCOORD_0']).astype(float)
                if pos.shape[1] != 3 or uv.shape != (len(pos), 2):
                    raise ValueError('Invalid position or UV dimensions')
                pos = (np.column_stack((pos, np.ones(len(pos)))) @ world.T)[:, :3]
                if not np.isfinite(pos).all() or not np.isfinite(uv).all():
                    raise ValueError('Nonfinite geometry/UV')
                ids = accessor(g, blob, prim['indices']).ravel().astype(int) if 'indices' in prim else np.arange(len(pos))
                if not len(ids) or len(ids) % 3 or ids.min() < 0 or ids.max() >= len(pos):
                    raise ValueError('Invalid triangle indices')
                materials.append(material_record(prim))
                for corners in ids.reshape(-1, 3):
                    p, t = pos[corners], uv[corners]
                    vertices.extend(p); uvs.extend(t)
                    density.append(triangle_density(p, t * albedo.size))
                    areas.append(float(np.linalg.norm(np.cross(p[1]-p[0], p[2]-p[0])) / 2))
        for child in node.get('children', []):
            walk(child, world)

    if g.get('animations'):
        raise ValueError('Animation not allowed in static proof')
    for index in g['scenes'][g.get('scene', 0)]['nodes']:
        walk(index, np.eye(4))
    if not vertices:
        raise ValueError('No rendered geometry')
    p, uv = np.asarray(vertices), np.asarray(uvs)
    dims = p.max(axis=0)-p.min(axis=0)
    corners = np.unique(np.round(uv, 6), axis=0)
    quad_uv = np.array([[0, 0], [0, 1], [1, 0], [1, 1]])
    dmin, dmax = min(d[0] for d in density), max(d[1] for d in density)
    checks = {
        'two_triangles': len(density) == 2,
        'four_unique_world_corners': len(np.unique(np.round(p, 6), axis=0)) == 4,
        'declared_metric_axis_aligned_plane': bool(np.allclose(sorted(dims), [0, *sorted(physical_size)], rtol=0, atol=1e-5)),
        'declared_physical_patch_area': bool(np.isclose(sum(areas), np.prod(physical_size), rtol=0, atol=1e-5)),
        'full_0_1_uv_quad': corners.shape == (4, 2) and bool(np.allclose(corners, quad_uv, rtol=0, atol=1e-6)),
        'density_both_principal_axes': dmin >= target * .9 - 1e-5 and dmax <= target * 1.1 + 1e-5,
        'all_material_checks': bool(materials) and all(m['pass'] for m in materials),
    }
    return {'pass': all(checks.values()), 'checks': checks, 'tris': len(density),
            'bounds_yup': [p.min(axis=0).tolist(), p.max(axis=0).tolist()],
            'dimensions_yup_m': dims.tolist(), 'area_m2': sum(areas),
            'declared_physical_size_m': physical_size,
            'density_target_px_m': target, 'density_tolerance': .1,
            'density_min': dmin, 'density_max': dmax,
            'per_triangle_principal_density': density, 'materials': materials}


def inspect_tile(spec, root, palette, rows):
    image_path = rooted(root, spec['albedo'])
    model_path = rooted(root, spec['model'])
    source = spec['source']
    source_path = rooted(root, source['path'])
    with Image.open(image_path) as im:
        image_format = im.format
        image = im.convert('RGBA')
    rgba = np.asarray(image)
    actual = {tuple(map(int, c)) for c in rgba[:, :, :3].reshape(-1, 3)}
    outside = sorted(actual-palette)
    process = spec.get('process', [])
    process_text = json.dumps(process, ensure_ascii=False).casefold()
    source_hash, albedo_hash, model_hash = sha(source_path), sha(image_path), sha(model_path)
    source_provenance = provenance_record(spec['source']['path'], source_hash, source['url'], rows)
    albedo_provenance = provenance_record(spec['albedo'], albedo_hash, source['url'], rows, processed=True)
    model_provenance = provenance_record(spec['model'], model_hash, source['url'], rows)
    physical_size = spec.get('physical_size_m')
    if (not isinstance(physical_size, list) or len(physical_size) != 2 or
            any(isinstance(v, bool) or not isinstance(v, (int, float)) or
                not np.isfinite(v) or v <= 0 for v in physical_size)):
        raise ValueError('physical_size_m must contain two finite positive metre dimensions')
    declared_density = spec.get('density_px_m')
    pixel_density = [image.width/physical_size[0], image.height/physical_size[1]]
    quad = inspect_quad(model_path, image, physical_size)
    checks = {
        'png_albedo': image_format == 'PNG',
        'texture_power_of_two': all(n > 0 and n & (n-1) == 0 for n in image.size),
        'opaque_pixels': bool(np.all(rgba[:, :, 3] == 255)),
        'albedo_palette_outside_zero': not outside,
        'physical_density_declaration': declared_density == 64,
        'pixel_dimensions_match_metric_density': all(57.6-1e-5 <= d <= 70.4+1e-5 for d in pixel_density),
        'approved_source_site': allowed_url(source.get('url')),
        'approved_direct_download_site': allowed_url(source.get('download_url'), download=True),
        'cc0_license': source.get('license') == 'CC0-1.0' and allowed_url(source.get('license_url')),
        'photographic_method_declared': any(word in source.get('method', '').casefold() for word in ('photogrammetry', 'photography', 'photograph')),
        'source_hash_matches_file': source.get('sha256') == source_hash,
        'nonempty_recorded_photo_process': isinstance(process, list) and len(process) >= 2 and 'photo_to_pixel' in process_text,
        'no_positive_ai_declaration': declared_non_ai(spec.get('ai_generated', False)) and declared_non_ai(source.get('ai_generated', False)),
        'source_provenance_current_non_ai': source_provenance['pass'],
        'albedo_provenance_current_non_ai': albedo_provenance['pass'],
        'model_provenance_current_non_ai': model_provenance['pass'],
        'exported_quad_checks': quad['pass'],
    }
    return {'id': spec['id'], 'pass': all(checks.values()), 'checks': checks,
            'albedo': spec['albedo'], 'model': spec['model'], 'source': source,
            'albedo_sha256': albedo_hash, 'model_sha256': model_hash,
            'process_sha256': canonical_sha(process), 'recorded_process': process,
            'source_sha256_actual': source_hash, 'used_albedo_colors': len(actual),
            'texture_size_px': list(image.size), 'physical_size_m': physical_size,
            'pixels_per_declared_metre': pixel_density,
            'outside_color_count': len(outside), 'outside_colors': outside,
            'palette_scope': 'Original delivered albedo PNG only. Source photo and final lit screen excluded.',
            'quad': quad, 'diagnostics': texture_diagnostics(image),
            'provenance': {'source': source_provenance, 'albedo': albedo_provenance, 'model': model_provenance}}


def inspect_manifest(manifest_path, root=ROOT, palette_path=None, provenance_path=None):
    root = Path(root).resolve()
    manifest_path = Path(manifest_path).resolve()
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    declared_palette = rooted(root, manifest['palette'])
    palette_path = Path(palette_path).resolve() if palette_path else declared_palette
    palette = {tuple(c) for c in load_palette(palette_path)}
    with (Path(provenance_path) if provenance_path else root/'art/provenance.csv').open(encoding='utf-8-sig', newline='') as f:
        rows = list(csv.DictReader(f))
    specs = manifest['tiles']
    ids = [s['id'] for s in specs]
    checks = {
        'schema_version_1': manifest.get('schema_version') == 1,
        'four_unique_tiles': len(ids) == len(set(ids)) == 4,
        'four_unique_albedos_and_models': len({s['albedo'] for s in specs}) == len({s['model'] for s in specs}) == 4,
        'shared_palette_exactly_32': len(palette) == 32,
        'palette_matches_manifest': declared_palette == palette_path,
    }
    tiles = []
    for spec in specs:
        try:
            tiles.append(inspect_tile(spec, root, palette, rows))
        except Exception as exc:
            tiles.append({'id': spec.get('id'), 'pass': False, 'error': f'{type(exc).__name__}: {exc}'})
    checks['all_tile_checks'] = bool(tiles) and all(t['pass'] for t in tiles)
    return {'pass': all(checks.values()), 'checks': checks, 'tile_count': len(tiles),
            'manifest': str(manifest_path), 'manifest_sha256': sha(manifest_path),
            'palette': str(palette_path), 'palette_sha256': sha(palette_path),
            'palette_colors': len(palette), 'tiles': tiles,
            'scope': 'Original albedo PNG palette; embedded PNG equality; actual exported GLB '
                     'world-space UV Jacobian on all triangles; recorded CC0 photographic lineage and hashes.',
            'limitations': ['Does not infer AI origin from pixels or independently certify a source license.',
                           'Process hash binds the recorded steps; it does not prove those steps were executed.',
                           'Proof quad density is measured; later Unity object scaling/UVs/import are not tested.',
                           'Final framebuffer palette, mip/filter behaviour in motion, artistic readability, '
                           'and perceptual tileability require separate game capture review.']}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--texture-manifest', required=True, type=Path)
    parser.add_argument('--palette', type=Path)
    parser.add_argument('--provenance', type=Path)
    parser.add_argument('--out', required=True, type=Path)
    args = parser.parse_args()
    try:
        result = inspect_manifest(args.texture_manifest, palette_path=args.palette, provenance_path=args.provenance)
    except Exception as exc:
        result = {'pass': False, 'error': f'{type(exc).__name__}: {exc}'}
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    summary = {k: result[k] for k in ('pass', 'checks', 'tile_count', 'error') if k in result}
    summary['tiles'] = [{'id': t.get('id'), 'pass': t['pass'],
                         'failed_checks': [k for k, v in t.get('checks', {}).items() if not v],
                         'error': t.get('error'), 'density_min': t.get('quad', {}).get('density_min'),
                         'density_max': t.get('quad', {}).get('density_max'),
                         'outside_color_count': t.get('outside_color_count')}
                        for t in result.get('tiles', [])]
    print(json.dumps(summary, ensure_ascii=False))
    return 0 if result['pass'] else 1


if __name__ == '__main__':
    sys.exit(main())
