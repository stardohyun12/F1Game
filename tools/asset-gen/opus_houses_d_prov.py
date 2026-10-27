"""scene-a4-houses-d 출처 줄 (아트2, 과제 1331). 쓴 사진 원본(Commons CC0 · ambientCG CC0) + 산출 파일. 이미 있는 asset_id 는 건너뜀.
    python tools/asset-gen/opus_houses_d_prov.py
"""
import csv, hashlib, json, os

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); CSV = os.path.join(R, 'art', 'provenance.csv')
rows = list(csv.reader(open(CSV, encoding='utf-8'))); have = {r[0] for r in rows if r}
FAC = 'art/source/facades-cc0'; meta = json.load(open(os.path.join(R, FAC, 'commons_meta.json'), encoding='utf-8'))
USED = ['commons_Colonial_facade_with_graffiti_in_central_Sao_Paulo.jpg', 'commons_Barcelona_003.jpg', 'commons_Casa_en_Mazatlán__9_de_diciembre_de_2022_07.jpg',
        'commons_Finalborgo__Italy__52404658808_.jpg', 'commons_Casa_en_Mazatlán__30_de_noviembre_de_2022.jpg', 'commons_Venice__Unsplash_.jpg',
        'commons_Casas_en_Mazatlán__30_de_noviembre_de_2022.jpg', 'commons_Small_pink_colonial_house_with_clay_tile_roof_in_Laguna_de_R.jpg',
        'commons_Casa_en_Mazatlán__9_de_diciembre_de_2022.jpg', 'commons_Romantic_Facade_In_Tuscany_Broken_Symmetry__135007059_.jpg']
sha = lambda p: hashlib.sha256(open(os.path.join(R, p), 'rb').read()).hexdigest()
out = []
for f in USED:
    m = meta[f]; aid = 'facade_cc0_' + ''.join(c if c.isalnum() else '_' for c in f[8:40]).lower()
    if aid not in have: out.append([aid, f'{FAC}/{f}', m['page'], 'CC0-1.0', '2026-10-27', 'opus_facade_commons.py (Commons API thumb 1920px)', 'no', sha(f'{FAC}/{f}'),
                                    f"original {m['original']}; license field '{m['license']}' {m['license_url']}; structured data P275=CC0 checked"])
for rid in ('RoofingTiles013A', 'RoofingTiles011A'):
    aid = 'ambientcg_' + rid.lower() + '_color'
    if aid not in have: out.append([aid, f'{FAC}/{rid}_Color.jpg', f'https://ambientcg.com/view?id={rid}', 'CC0-1.0', '2026-10-27', 'opus_facade_fetch.py (2K-JPG zip)', 'no',
                                    sha(f'{FAC}/{rid}_Color.jpg'), f'https://ambientcg.com/get?file={rid}_2K-JPG.zip; license https://docs.ambientcg.com/license/ (saved license.html)'])
D = 'art/export/scene-a4-houses-d'
for f in [f'house_{n}.{e}' for n in ('narrow_a', 'narrow_b', 'shop', 'slope', 'warehouse', 'cafe') for e in ('fbx', 'png')] + ['kit.json', 'measure.json', 'palette32.gpl']:
    aid = 'scene_a4_houses_d_' + f.replace('.', '_')
    if aid in have: continue
    lic = 'CC0-1.0 + CC-BY-3.0 (vending machine by dook, credit)' if 'shop' in f else 'CC0-1.0'
    out.append([aid, f'{D}/{f}', 'derived: elegantcrow Retro Modular House roofs (CC0) + Commons CC0 facade photos + ambientCG roof tiles (see facade_cc0_*, ambientcg_*)', lic,
                '2026-10-27', 'opus_houses.py -- d (task 1331)', 'no', sha(f'{D}/{f}'),
                'variant d: c mesh minus windows/doors/shutters/rails/awnings/signs, walls = flat box; real facade photos projected (area downscale 10 texels/m, flattened light, sat .8, warm), 6-house k-means 32 palette, despeckle; no AI; Unity not run'])
with open(CSV, 'a', encoding='utf-8', newline='') as fp:
    if not open(CSV, encoding='utf-8').read().endswith('\n'): fp.write('\n')
    csv.writer(fp, lineterminator='\n').writerows(out)
print('ADDED', len(out))
