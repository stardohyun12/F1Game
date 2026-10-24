"""Freeze each visually assessed round, including matching exported models."""
from pathlib import Path
import shutil,sys
R=Path(__file__).resolve().parent;repo=R.parents[2]
dest=R/('round-'+sys.argv[1].zfill(2))
if dest.exists():raise SystemExit('Refusing to overwrite an assessed round')
dest.mkdir()
for name in ['blind','box','cake','eggs','wine','fish','validation']:
 shutil.copytree(R/name,dest/name)
for p in R.glob('*.png'):shutil.copy2(p,dest/p.name)
shutil.copy2(R/'comparison-proof.json',dest/'comparison-proof.json')
shutil.copy2(repo/'art/provenance.csv',dest/'provenance.csv')
for kind,folder in {'box':'cargo_box_final_v2','cake':'cargo_cake_v02','eggs':'cargo_eggs_v02','wine':'cargo_wine_v01','fish':'cargo_fish_v02'}.items():
 d=dest/'models'/kind;d.mkdir(parents=True)
 for p in (repo/'art/models'/folder).iterdir():
  if p.suffix in ['.glb','.py','.json'] or p.name.endswith('atlas_64.png'):shutil.copy2(p,d/p.name)
print(dest)
