from pathlib import Path
from PIL import Image
import numpy as np,json,csv,hashlib
R=Path(__file__).resolve().parents[3];E=R/'art/export/scene-a6-photo-kit';pal={tuple(map(int,l.split()[:3])) for l in (E/'palette-photo-kit.gpl').read_text().splitlines() if l[:1].isdigit()};results={}
for p in sorted(E.glob('*.png')):
 a=np.array(Image.open(p).convert('RGB'));packed=(a[:,:,0].astype('uint32')<<16)|(a[:,:,1].astype('uint32')<<8)|a[:,:,2];colors={(int(v)>>16,(int(v)>>8)&255,int(v)&255) for v in np.unique(packed)};assert colors<=pal
 same=np.zeros(a.shape[:2],bool)
 for dx,dy in [(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)]:same|=np.all(a==np.roll(a,(dy,dx),(0,1)),axis=2)
 isolated=float((~same)[1:-1,1:-1].mean()*100)
 results[p.name]=dict(palette_valid=True,colors=len(colors),isolated_8neighbor_percent=isolated)
 if p.stem.startswith('garage_'):
  old=np.array(Image.open(R/'art/export/scene-a-v5b-garage/garage_baked32.png').convert('RGB'));changed=np.any(a!=old,axis=2);results[p.name]['isolated_changed_pixels_percent']=float(((~same)&changed).sum()/max(changed.sum(),1)*100)
 if p.stem.startswith(('stone_','tin_')):
  assert np.array_equal(a[0],a[-1]) and np.array_equal(a[:,0],a[:,-1])
  results[p.name]['tile_edges_identical']=True
with (E/'provenance.csv').open(encoding='utf-8',newline='') as f:
 rows=list(csv.DictReader(f))
for r in rows:assert hashlib.sha256((R/r['path']).read_bytes()).hexdigest()==r['sha256']
for p in E.glob('*.fbm/*.png'):assert p.read_bytes()==(E/p.name).read_bytes()
out=dict(palette_colors=len(pal),provenance_hashes_verified=len(rows),textures=results)
(E/'audit.json').write_text(json.dumps(out,indent=2));print(json.dumps(out))
