"""scene-a2-bldg-light 출처 줄을 art/provenance.csv 에 덧붙인다 (아트2, 과제 1134). 이미 있는 asset_id 는 건너뜀.
    python tools/asset-gen/opus_bldg_light_prov.py
"""
import hashlib, os

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CSV = os.path.join(R, 'art', 'provenance.csv'); D = 'art/export/scene-a2-bldg-light'
TOOL = 'opus_bldg_light.py; opus_bldg_light_post.py'
VAL = 'https://valsekamerplant.itch.io/psx-style-urban-stacked-pack'
NOTE = 'repaint of {src} — original geometry kept; analytic paint (no photo; no AI); bright walls/orange roofs; few big teal windows; 3-tone teal shade; shared median-cut palette32; Unity not run'
FAR_SRC = 'CC0 bases via art/export/scene-a2-far (valsekamerplant / elegantcrow / chillydurango)'
rows = []
for f in ('far_town.fbx', 'far_town_baked32.png', 'far_terrace_baked32.png'):
    rows.append((f, VAL, NOTE.format(src='scene-a2-far/' + ('far_town.fbx' if f.endswith('.fbx') else f) + '; ' + FAR_SRC) + '; air perspective baked'))
for i in range(1, 5):
    for ext in ('fbx', 'png'):
        rows.append((f'background_building_0{i}.{ext}', VAL, NOTE.format(src=f'scene-a2-kit/background_building_0{i}.{ext}; valsekamerplant-urban-stacked CC0')))
for f in ('kit.json', 'measure.json', 'palette32.gpl'):
    rows.append((f, 'generated', 'metadata/measurement/palette for scene-a2-bldg-light'))
have = {l.split(',', 1)[0] for l in open(CSV, encoding='utf-8')}
out = []
for f, src, note in rows:
    aid = 'scene_a2_bldg_light_' + f.replace('.', '_')
    if aid in have: continue
    h = hashlib.sha256(open(os.path.join(R, D, f), 'rb').read()).hexdigest()
    out.append(','.join((aid, f'{D}/{f}', src, 'CC0-1.0', '2026-10-27', TOOL, 'no', h, note.replace(',', ';'))))
txt = open(CSV, encoding='utf-8').read()
open(CSV, 'a', encoding='utf-8').write(('' if txt.endswith('\n') else '\n') + ''.join(l + '\n' for l in out))
print('ADDED', len(out))
