from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,csv
R=Path(__file__).resolve().parents[2];O=R/'art/models/codex_garage_kart_pixel_v1';V=R/'art/_review/2026-10-27-garage-kart-v1'
files=['garage_pixel_v1_front','garage_pixel_v1_threequarter','garage_pixel_v1_interior','garage_pixel_v1_1x_6m','kart_pixel_v1_front','kart_pixel_v1_threequarter','kart_pixel_v1_side','kart_pixel_v1_1x_6m']
sheet=Image.new('RGB',(1920,2160))
for i,n in enumerate(files):sheet.paste(Image.open(V/(n+'.png')),(i%2*960,i//2*540))
sheet.save(V/'sheet.png')
checks=[]
for name in ['garage_pixel_v1','kart_pixel_v1']:
 im=Image.open(O/(name+'.png')).convert('RGB');rs=json.loads((O/(name+'_uv.json')).read_text());counts=[];dens=[]
 for r in rs:
  counts.append(len(im.crop((r['x'],r['y'],r['x']+r['w'],r['y']+r['h'])).getcolors(100000)))
  for d,p in zip(r['dims'],[r['w'],r['h']]):
   if d>=.25:dens.append(p/d)
 checks.append(dict(name=name,max_colors_per_face=max(counts),min_colors_per_face=min(counts),face_count=len(rs),density_range_faces_over_25cm=[min(dens),max(dens)],passed=max(counts)<=6 and min(dens)>=12 and max(dens)<=16.01,small_face_note='integer pixel rounding increases density on narrow edges; 14px/m target'))
(V/'pixel-check.json').write_text(json.dumps(checks,indent=2));print(checks)
ledger=R/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:rd=csv.DictReader(f);fields=rd.fieldnames;rows={r['path']:r for r in rd}
for p in O.iterdir():
 if p.suffix not in ['.png','.fbx','.glb','.blend']:continue
 rel=p.relative_to(R).as_posix();rows[rel]=dict(asset_id=p.stem+'_'+p.suffix[1:],path=rel,source_url='original scripted hand pixels and bpy; existing measured Codex layout; D-163',license='Original project artwork; Galmuri SIL OFL 1.1 lettering',accessed='2026-10-27',tool='garage_kart_pixel.py; garage_kart_pixel_paint.py',ai_generated='no',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes='14px/m target, Closest, <=6 colours/face; no photos/no noise/no gradients; no engine copy')
with ledger.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows.values())
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>D163 ??????</title><style>body{background:#d1c5a7;color:#365452;font:18px sans-serif}img{max-width:100%}</style><h1>D163 ?????? / ?? ?? ??</h1><p>??: ??? ???3/4 / ???6m / ?? ???3/4 / ??6m. ? ??? 960?540, ??? ?? ?? ???.</p><a href="visual-gate.md">?? ??</a> ? <a href="technical-check.json">FBX ??</a> ? <a href="pixel-check.json">?? ??</a><br><img src="sheet.png">',encoding='utf8')
