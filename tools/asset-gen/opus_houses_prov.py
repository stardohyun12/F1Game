"""scene-a4-houses 출처 줄을 art/provenance.csv 에 덧붙인다 (아트2, 과제 1232). 이미 있는 asset_id 는 건너뜀.
    python tools/asset-gen/opus_houses_prov.py
"""
import hashlib, json, os

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CSV = os.path.join(R, 'art', 'provenance.csv'); D = 'art/export/scene-a4-houses'
TOOL = 'opus_houses.py'
EC = 'https://elegantcrow.itch.io/retro-modular-house-pack'
SRC = (EC + ' (CC0 walls/windows/doors/shutters/railings/stairs); https://valsekamerplant.itch.io/psx-style-house-plants-and-pots (CC0 plants);'
       ' https://polyhaven.com (CC0 photos: worn_plaster_wall clay_roof_tiles wood_peeling_paint_weathered stone_block_wall rough_linen)')
NOTE = 'assembled coastal house from CC0 modules + hand roof/pillars/awning/sign/AC masses; photo luminance detail x late-autumn analytic light; teal shade; shared palette32; no AI; Unity not run'
names = [f['name'] for f in json.load(open(os.path.join(R, D, 'kit.json'), encoding='utf-8'))['files']]
rows = []
for n in names:
    shop = n == 'house_shop'
    lic = 'CC0-1.0 + CC-BY-3.0' if shop else 'CC0-1.0'
    extra = '; includes Vending Machine by dook via Poly Pizza (CC-BY 3.0 — credit required)' if shop else ''
    for ext in ('fbx', 'png'):
        rows.append((f'{n}.{ext}', SRC + (' ; https://poly.pizza (Vending Machine by dook)' if shop else ''), lic, NOTE + extra))
for f in ('kit.json', 'measure.json', 'palette32.gpl'):
    rows.append((f, 'generated', 'CC0-1.0', 'metadata/measurement/palette for scene-a4-houses'))
have = {l.split(',', 1)[0] for l in open(CSV, encoding='utf-8')}
out = []
for f, src, lic, note in rows:
    aid = 'scene_a4_houses_' + f.replace('.', '_')
    if aid in have: continue
    h = hashlib.sha256(open(os.path.join(R, D, f), 'rb').read()).hexdigest()
    out.append(','.join((aid, f'{D}/{f}', src.replace(',', ';'), lic, '2026-10-27', TOOL, 'no', h, note.replace(',', ';'))))
txt = open(CSV, encoding='utf-8').read()
open(CSV, 'a', encoding='utf-8').write(('' if txt.endswith('\n') else '\n') + ''.join(l + '\n' for l in out))
print('ADDED', len(out))
