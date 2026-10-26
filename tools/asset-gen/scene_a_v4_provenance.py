from pathlib import Path
import csv,hashlib,json
from PIL import Image
R=Path(__file__).resolve().parents[2];E=R/'art/export/scene-a-v4-baked';V=R/'art/_review/2026-10-27-scene-a-v4-baked';ledger=R/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:rd=csv.DictReader(f);fields=rd.fieldnames;rows={r['path']:r for r in rd}
for p in E.iterdir():
 if p.suffix not in ['.png','.fbx','.gpl']:continue
 rel=p.relative_to(R).as_posix();rows[rel]=dict(asset_id='scene_a_v4_'+p.stem,path=rel,source_url='repo:art/models/opus_garage_kart_v5.blend + art/source/opus-scene-a-kit/tex; copied read-only V5 source',license='Project original V5 artwork; inherited lettering licenses; no new photos or AI',accessed='2026-10-27',tool='Cycles128 DIFFUSE + AO bake; scene_a_v4_quantize.py; Emission-only preview',ai_generated='no',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes='40 texels/m props/building/kart;32 terrain;0.32 far sea exception;24 shared palette,no dither; static shadows baked; no Unity import')
with ledger.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows.values())
colors=set()
for p in E.glob('*_baked24.png'):colors.update(Image.open(p).convert('RGB').getdata())
(V/'palette-union.json').write_text(json.dumps(dict(unique_texture_colors=len(colors),passed=len(colors)<=24,colors=sorted(colors)),indent=2));print('global PNG colours',len(colors))
