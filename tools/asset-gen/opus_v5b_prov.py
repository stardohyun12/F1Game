"""scene-a-v5b-garage · scene-a4-houses-b 출처 줄 (아트2, 과제 1248/1259). 원본 줄(scene_a_v5_combo_* / scene_a4_houses_*)의 출처·라이선스를 물려받고
경로·sha256·메모만 새로. 이미 있는 asset_id 는 건너뜀.   python tools/asset-gen/opus_v5b_prov.py
"""
import csv, hashlib, os

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); CSV = os.path.join(R, 'art', 'provenance.csv')
rows = list(csv.reader(open(CSV, encoding='utf-8'))); by = {r[0]: r for r in rows if r}; have = set(by)
JOBS = [('art/export/scene-a-v5b-garage', 'scene_a_v5b_garage_', 'scene_a_v5_combo_', 'opus_v5b_garage.py (re-bake interior over V5)',
         ['garage_v5_combo.fbx', 'garage_baked32.png', 'dirt_v5_combo.fbx', 'dirt_baked32.png', 'palette32.gpl', 'measure.json'],
         'V5 fbx copied unchanged; texture re-baked inside garage only (2 ceiling lamps + warm door bounce) + floor lift; pixels outside interior = V5 exactly; V5 palette32; no AI; Unity not run'),
        ('art/export/scene-a4-houses-b', 'scene_a4_houses_b_', 'scene_a4_houses_', 'opus_houses.py (HOUSES_VARIANT=b)',
         [f'house_{n}.{e}' for n in ('narrow_a', 'narrow_b', 'shop', 'slope', 'warehouse', 'cafe') for e in ('fbx', 'png')] + ['kit.json', 'measure.json', 'palette32.gpl'],
         'variant b: plant colour from opaque pixels only; wall blotches warm grey-brown; warmer apricot/cream walls; palette 20 shared + 12 warm k-means; no AI; Unity not run'),
        ('art/export/scene-a4-houses-c', 'scene_a4_houses_c_', 'scene_a4_houses_', 'opus_houses.py -- c (task 1302)',
         [f'house_{n}.{e}' for n in ('narrow_a', 'narrow_b', 'shop', 'slope', 'warehouse', 'cafe') for e in ('fbx', 'png')] + ['kit.json', 'measure.json', 'palette32.gpl'],
         'variant c: same mesh as b; UV re-packed 9 texels/m; flat paint + per-face 3-step light + hand-placed texel details (procedural PIL-style numpy); no photo texture used in pixels; palette b 22 + 10; no AI; Unity not run')]
out = []
for D, pre, src_pre, tool, files, note in JOBS:
    for f in files:
        aid = pre + f.replace('.', '_')
        if aid in have: continue
        base = by.get(src_pre + f.replace('.', '_')) or by.get(src_pre + os.path.splitext(f)[0])
        src, lic = (base[2], base[3]) if base else ('generated', 'CC0-1.0')
        if base and base[3].startswith('CC0') is False: lic = base[3]
        h = hashlib.sha256(open(os.path.join(R, D, f), 'rb').read()).hexdigest()
        out.append([aid, f'{D}/{f}', src, lic, '2026-10-27', tool, 'no', h, note + ('; derived from ' + base[0] if base else '')])
with open(CSV, 'a', encoding='utf-8', newline='') as fp:
    if not open(CSV, encoding='utf-8').read().endswith('\n'): fp.write('\n')
    csv.writer(fp, lineterminator='\n').writerows(out)
print('ADDED', len(out), [r[0] for r in out if r[2] == 'generated'])
