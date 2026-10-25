"""Synthetic isolated fixtures, never production/provenance data or real CC0 claims."""
from pathlib import Path
import argparse
import copy
import csv
import io
import json
import struct
import tempfile

import numpy as np
from PIL import Image

from check_texture_tiles import inspect_manifest, sha


def write_quad(path, image, uv_scale=1., tint=None, mag_filter=9728, normal_map=False,
               physical_size=(1., 1.)):
    stream = io.BytesIO()
    image.save(stream, format='PNG')
    pos = np.array([[-.5, 0, -.5], [.5, 0, -.5], [.5, 0, .5], [-.5, 0, .5]], dtype='<f4')
    pos[:, 0] *= physical_size[0]
    pos[:, 2] *= physical_size[1]
    uv = np.array([[0, 0], [1, 0], [1, 1], [0, 1]], dtype='<f4') * uv_scale
    indices = np.array([0, 1, 2, 0, 2, 3], dtype='<u2')
    views, payload = [], bytearray()
    for chunk in (pos.tobytes(), uv.tobytes(), indices.tobytes(), stream.getvalue()):
        while len(payload) % 4:
            payload.append(0)
        views.append({'buffer': 0, 'byteOffset': len(payload), 'byteLength': len(chunk)})
        payload.extend(chunk)
    material = {'pbrMetallicRoughness': {'baseColorTexture': {'index': 0},
                                        'baseColorFactor': tint or [1, 1, 1, 1],
                                        'metallicFactor': 0, 'roughnessFactor': 1},
                'extensions': {'KHR_materials_unlit': {}}}
    if normal_map:
        material['normalTexture'] = {'index': 0}
    g = {'asset': {'version': '2.0'}, 'scene': 0, 'scenes': [{'nodes': [0]}],
         'nodes': [{'mesh': 0}], 'buffers': [{'byteLength': len(payload)}],
         'bufferViews': views,
         'accessors': [{'bufferView': 0, 'componentType': 5126, 'count': 4, 'type': 'VEC3'},
                       {'bufferView': 1, 'componentType': 5126, 'count': 4, 'type': 'VEC2'},
                       {'bufferView': 2, 'componentType': 5123, 'count': 6, 'type': 'SCALAR'}],
         'meshes': [{'primitives': [{'attributes': {'POSITION': 0, 'TEXCOORD_0': 1}, 'indices': 2, 'material': 0}]}],
         'materials': [material], 'textures': [{'source': 0, 'sampler': 0}],
         'images': [{'bufferView': 3, 'mimeType': 'image/png'}],
         'samplers': [{'magFilter': mag_filter, 'minFilter': 9728, 'wrapS': 10497, 'wrapT': 10497}],
         'extensionsUsed': ['KHR_materials_unlit']}
    encoded = json.dumps(g).encode()
    encoded += b' ' * (-len(encoded) % 4)
    payload += b'\0' * (-len(payload) % 4)
    binary = struct.pack('<4sII', b'glTF', 2, 12+8+len(encoded)+8+len(payload))
    binary += struct.pack('<II', len(encoded), 0x4E4F534A) + encoded
    binary += struct.pack('<II', len(payload), 0x004E4942) + payload
    path.write_bytes(binary)


def fixture(root):
    (root/'art').mkdir()
    palette = [[i*8]*3 for i in range(32)]
    (root/'palette.json').write_text(json.dumps(palette), encoding='utf-8')
    source = root/'source.png'
    Image.new('RGB', (2, 2), (123, 86, 45)).save(source)
    image = Image.new('RGB', (64, 64), (64, 64, 64))
    px = np.asarray(image).copy()
    px[:, ::4] = (96, 96, 96)
    image = Image.fromarray(px)
    specs = []
    for i in range(4):
        image.save(root/f'tile{i}.png')
        write_quad(root/f'tile{i}.glb', image)
        specs.append({'id': f'fixture{i}', 'albedo': f'tile{i}.png', 'model': f'tile{i}.glb',
                      'physical_size_m': [1, 1], 'density_px_m': 64,
                      'source': {'path': 'source.png', 'url': 'https://polyhaven.com/a/synthetic-test-fixture',
                                 'download_url': 'https://dl.polyhaven.org/synthetic-test-fixture.png',
                                 'license': 'CC0-1.0', 'license_url': 'https://polyhaven.com/license',
                                 'sha256': sha(source), 'method': 'photography'},
                      'process': ['photo_to_pixel fixture only', 'palette quantization fixture only']})
    manifest = {'schema_version': 1, 'palette': 'palette.json', 'tiles': specs}
    return manifest


def register(root, manifest, omit=None):
    paths = ['source.png'] + [s[k] for s in manifest['tiles'] for k in ('albedo', 'model')]
    fields = ['asset_id', 'path', 'source_url', 'license', 'accessed', 'tool', 'ai_generated', 'sha256', 'notes']
    with (root/'art/provenance.csv').open('w', encoding='utf-8', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=fields)
        writer.writeheader()
        for i, path in enumerate(paths):
            if path == omit:
                continue
            writer.writerow({'asset_id': f'synthetic_{i}', 'path': path,
                             'source_url': 'https://polyhaven.com/a/synthetic-test-fixture',
                             'license': 'CC0-1.0', 'accessed': '2026-10-24',
                             'tool': 'photo_to_pixel synthetic fixture, palette quantization',
                             'ai_generated': 'no', 'sha256': sha(root/path),
                             'notes': 'Synthetic validator fixture; URL/CC0 fields are test data, not a real source claim.'})


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--out', type=Path)
    args = p.parse_args()
    tests = []
    with tempfile.TemporaryDirectory(prefix='pitcrew-tile-check-') as td:
        root = Path(td)
        base = fixture(root)
        original = Image.open(root/'tile0.png').copy()

        def check(name, mutate=None, expected=True, expected_check=None, register_missing=None):
            spec = copy.deepcopy(base)
            original.save(root/'tile0.png')
            write_quad(root/'tile0.glb', original)
            if mutate:
                mutate(spec)
            register(root, spec, omit=register_missing)
            path = root/'manifest.json'
            path.write_text(json.dumps(spec), encoding='utf-8')
            result = inspect_manifest(path, root=root)
            assert result['pass'] is expected, (name, result)
            first = result['tiles'][0]
            if expected_check:
                assert first.get('checks', {}).get(expected_check) is False, (name, first)
            tests.append({'name': name, 'pass': True, 'actual_asset_pass': result['pass'],
                          'failed_checks': [k for k, v in first.get('checks', {}).items() if not v]})

        check('Baseline: four 64 px / one metre neutral unlit proof quads')

        def repeat_unit(spec, size_m, texture_size):
            changed = original.resize(texture_size, Image.Resampling.NEAREST)
            changed.save(root/'tile0.png')
            write_quad(root/'tile0.glb', changed, physical_size=size_m)
            spec['tiles'][0]['physical_size_m'] = list(size_m)
        check('Whole 2 m repeat unit / 128 px has actual 64 px/m',
              lambda s: repeat_unit(s, [2, 2], (128, 128)))
        check('Whole 4 m repeat unit / 256 px has actual 64 px/m',
              lambda s: repeat_unit(s, [4, 4], (256, 256)))
        check('Non-square 1 x 2 m unit / 64 x 128 px preserves both density axes',
              lambda s: repeat_unit(s, [1, 2], (64, 128)))
        check('Larger repeat unit with too few pixels rejected',
              lambda s: repeat_unit(s, [2, 2], (64, 64)), False,
              'pixel_dimensions_match_metric_density')
        check('Metric declaration cannot hide undersized exported geometry',
              lambda s: s['tiles'][0].update(physical_size_m=[2, 2]), False,
              'exported_quad_checks')
        check('Non-power-of-two texture rejected even when density is exactly 64',
              lambda s: repeat_unit(s, [1.5, 1.5], (96, 96)), False,
              'texture_power_of_two')

        def color_outside(spec):
            changed = original.copy()
            changed.putpixel((3, 3), (255, 0, 255))
            changed.save(root/'tile0.png')
            write_quad(root/'tile0.glb', changed)
        check('Outside-palette RGB rejected even with current hashes and matching embedded image',
              color_outside, False, 'albedo_palette_outside_zero')
        check('Half-scale UV rejected from decoded geometry despite manifest declaring 64 px/m',
              lambda s: write_quad(root/'tile0.glb', original, uv_scale=.5), False, 'exported_quad_checks')
        check('Source hash mismatch rejected',
              lambda s: s['tiles'][0]['source'].update(sha256='0'*64), False, 'source_hash_matches_file')
        check('Missing albedo provenance rejected', expected=False,
              expected_check='albedo_provenance_current_non_ai', register_missing='tile0.png')
        check('Missing proof model provenance rejected', expected=False,
              expected_check='model_provenance_current_non_ai', register_missing='tile0.glb')
        check('Tinted material rejected', lambda s: write_quad(root/'tile0.glb', original, tint=[.8, 1, 1, 1]),
              False, 'exported_quad_checks')
        check('Linear magnification rejected', lambda s: write_quad(root/'tile0.glb', original, mag_filter=9729),
              False, 'exported_quad_checks')
        check('Extra normal texture rejected', lambda s: write_quad(root/'tile0.glb', original, normal_map=True),
              False, 'exported_quad_checks')
        check('Procedural-only source does not meet photography contract',
              lambda s: s['tiles'][0]['source'].update(method='procedural'), False, 'photographic_method_declared')
        check('Third-party download host rejected',
              lambda s: s['tiles'][0]['source'].update(download_url='https://example.org/file.png'),
              False, 'approved_direct_download_site')
        check('Explicit non-AI string declaration supported',
              lambda s: s['tiles'][0]['source'].update(ai_generated='no'))
        check('Positive AI declaration rejected',
              lambda s: s['tiles'][0]['source'].update(ai_generated=True),
              False, 'no_positive_ai_declaration')

        def quiet_field(spec):
            changed = Image.new('RGB', (64, 64), (64, 64, 64))
            changed.save(root/'tile0.png')
            write_quad(root/'tile0.glb', changed)
        check('Monochrome 8x8 diagnostic does not force synthetic noise into quiet fields', quiet_field)
    result = {'pass': all(t['pass'] for t in tests), 'test_count': len(tests), 'tests': tests,
              'scope': 'Synthetic temporary fixtures only; no downloads, production edits or real source license claims.'}
    if args.out:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        args.out.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
