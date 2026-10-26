from pathlib import Path
from PIL import Image
import json,csv,hashlib,struct
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_kart_v3';V=R/'art/_review/2026-10-27-garage-kart-v3';checks=[]
for name in ['garage_v3','kart_v3']:
 im=Image.open(O/(name+'.png'));rs=json.loads((O/(name+'_uv.json')).read_text());counts=[len(im.crop((r['x'],r['y'],r['x']+r['w'],r['y']+r['h'])).getcolors(100000)) for r in rs];raw=(O/(name+'.glb')).read_bytes();n=struct.unpack_from('<I',raw,12)[0];gl=json.loads(raw[20:20+n]);checks.append(dict(id=name,texture=list(im.size),max_colors_per_face=max(counts),min_colors_per_face=min(counts),samplers=gl.get('samplers'),nearest=all(x.get('magFilter')==9728 for x in gl.get('samplers',[]))))
(V/'texture-check.json').write_text(json.dumps(checks,indent=2))
ledger=R/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:rd=csv.DictReader(f);fields=rd.fieldnames;rows={r['path']:r for r in rd}
for p in O.iterdir():
 if p.suffix not in ['.fbx','.glb','.blend','.png']:continue
 rel=p.relative_to(R).as_posix();rows[rel]=dict(asset_id=p.stem+'_'+p.suffix[1:],path=rel,source_url='original bpy and authored pixels; D165 screenshot study only, no source reference pixels in models',license='Original project artwork; Galmuri SIL OFL 1.1 lettering',accessed='2026-10-27',tool='garage_kart_v3.py; garage_kart_v3_paint.py',ai_generated='no',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes='960x540 -> 1920x1080 exact Nearest2; flat sun, coloured ambient; no noise/photo/AI textures; no engine copy')
with ledger.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows.values())
print(checks)
