"""Deliberately invalid fixtures. Never import these files as game assets."""
from pathlib import Path
import hashlib
import json
import struct
import subprocess
import sys
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
CHECK = ROOT / 'tools/asset-gen/check_asset.py'
MODEL = ROOT / 'art/models/cargo_box_v01/cargo_box_v01.glb'
TEXTURE = ROOT / 'art/models/cargo_box_v01/cargo_box_atlas_64.png'
PALETTE = ROOT / 'art/source/palette/pitcrew_d123_32.gpl'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_scaled_glb():
    data = MODEL.read_bytes()
    chunks = []
    offset = 12
    while offset < len(data):
        size, kind = struct.unpack_from('<II', data, offset)
        payload = data[offset+8:offset+8+size]
        offset += size+8
        if kind == 0x4E4F534A:
            document = json.loads(payload)
            roots = document['scenes'][document.get('scene', 0)]['nodes']
            # Parent scales world X by 2 and Y by 1/2; front/back XY area is
            # unchanged while principal-axis texel densities are doubled/halved.
            document['nodes'].append({'name': 'INVALID_anisotropy_test',
                                      'scale': [2.0, 0.5, 1.0], 'children': roots[:]})
            document['scenes'][document.get('scene', 0)]['nodes'] = [len(document['nodes'])-1]
            payload = json.dumps(document, separators=(',', ':')).encode('utf8')
            payload += b' ' * (-len(payload) % 4)
        chunks.append(struct.pack('<II', len(payload), kind)+payload)
    body = b''.join(chunks)
    output = HERE / 'INVALID_nonuniform_scale.glb'
    output.write_bytes(struct.pack('<4sII', b'glTF', 2, 12+len(body))+body)
    return output


def main():
    original_hashes = {'model': digest(MODEL), 'texture': digest(TEXTURE)}
    outside = HERE / 'INVALID_palette_color.png'
    image = Image.open(TEXTURE).convert('RGB')
    palette_colors = set(image.getdata())
    chosen = next((r, 0, 255) for r in range(256) if (r, 0, 255) not in palette_colors)
    image.putpixel((0, 0), chosen)
    image.save(outside)
    size_bad = HERE / 'INVALID_63x64.png'
    Image.open(TEXTURE).crop((0, 0, 63, 64)).save(size_bad)
    scaled = write_scaled_glb()
    cases = [
        ('baseline', MODEL, TEXTURE, [], None),
        ('outside_palette', MODEL, outside, [], 'palette_outside_zero'),
        ('non_power_of_two', MODEL, size_bad, [], 'texture_power_of_two'),
        ('wrong_target_density', MODEL, TEXTURE, ['--density', '64'], 'density_both_principal_axes'),
        ('triangle_budget_10', MODEL, TEXTURE, ['--max-tris', '10'], 'triangle_budget'),
        ('nonuniform_scale', scaled, TEXTURE, ['--dimensions', '.8', '.15', '.3'], 'density_both_principal_axes'),
    ]
    records = []
    for name, model, texture, extra, failed_key in cases:
        report = HERE / f'{name}.json'
        command = [sys.executable, str(CHECK), '--model', str(model), '--texture', str(texture),
                   '--palette', str(PALETTE), '--out', str(report), *extra]
        run = subprocess.run(command, capture_output=True, text=True, encoding='utf8')
        result = json.loads(report.read_text(encoding='utf8')) if report.exists() else {'pass': False, 'error': 'No checker report emitted'}
        expected = (run.returncode == 0 and result['pass']) if failed_key is None else (
            run.returncode != 0 and not result['pass'] and result.get('checks', {}).get(failed_key) is False)
        records.append({'name': name, 'expected_behavior_observed': expected,
                        'exit_code': run.returncode, 'expected_failed_check': failed_key,
                        'failed_checks': [k for k, v in result.get('checks', {}).items() if not v],
                        'report': report.name, 'command': command, 'stderr': run.stderr})
    scaled_result = json.loads((HERE/'nonuniform_scale.json').read_text())
    baseline = json.loads((HERE/'baseline.json').read_text())
    # At least the front/back XY faces retain geometric-mean density despite
    # invalid anisotropy. This demonstrates why area-only density is insufficient.
    area_preserved = []
    for i, (before, after) in enumerate(zip(baseline['per_triangle_density'], scaled_result['per_triangle_density'])):
        if abs(before[0]*before[1] - after[0]*after[1]) < .001:
            area_preserved.append({'triangle': i, 'before_axes': before, 'after_axes': after,
                                   'geometric_mean': (after[0]*after[1])**.5})
    unchanged = original_hashes == {'model': digest(MODEL), 'texture': digest(TEXTURE)}
    final = {'pass': all(r['expected_behavior_observed'] for r in records) and unchanged and bool(area_preserved),
             'original_assets_unchanged': unchanged, 'original_sha256': original_hashes,
             'cases': records, 'area_only_false_positive_examples': area_preserved,
             'baseline_measurements': {k:baseline[k] for k in ('tris','dimensions_m','density_min','density_max','texture_size')},
             'note': 'Deliberately invalid regression fixtures, not production assets.'}
    (HERE/'test-results.json').write_text(json.dumps(final, ensure_ascii=False, indent=2)+'\n', encoding='utf8')
    print(json.dumps({'pass': final['pass'], 'cases': len(records), 'area_preserved_triangles': len(area_preserved),
                      'original_assets_unchanged': unchanged}))
    return 0 if final['pass'] else 1


if __name__ == '__main__':
    sys.exit(main())
