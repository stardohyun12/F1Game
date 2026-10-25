from pathlib import Path
import json,hashlib
from html.parser import HTMLParser
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'art/_review/2026-10-25-bayloop-props2'
def read(p):return json.loads(p.read_text(encoding='utf8'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
m=read(ROOT/'art/models/bayloop_props2_v01/manifest.json');d=read(R/'delivery.json');g=read(R/'gates.json')
assert all(sha(ROOT/x['path'])==x['sha256'] for x in d['copied'])
assert all(not (ROOT/'F1/Assets/Art/BayLoop/Props'/(x+'.fbx')).exists() for x in d['held'])
a=read(R/'repro-a.json');mismatches=[x['id'] for x in a if x['geometry_sha256']!=next(y for y in m['assets'] if y['id']==x['id'])['geometry_sha256']]
assert not mismatches,mismatches
for name in [x['id'] for x in d['approved']]:
 for p,h in g[name]['render_sha256'].items():assert sha(R/p)==h
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,val in attrs:
   if key in ('href','src') and not val.startswith(('http','#')):assert (R/val).exists(),val
Links().feed((R/'index.html').read_text(encoding='utf8'))
(R/'delivery-check.json').write_text(json.dumps(dict(pass_=True,approved=len(d['approved']),held=len(d['held']),files_sha_match=len(d['copied']),held_not_in_F1=True,final_geometry_matches_repro=True,gate_image_hashes_match=True,html_links_exist=True),indent=2))
print('DELIVERY_CHECK_PASS')
