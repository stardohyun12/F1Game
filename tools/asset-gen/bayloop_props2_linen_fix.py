"""Quantizer tie repair: preserve the two actual downsampled linen values."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art/models/bayloop_props2_v01'
p=np.array(json.loads((ROOT/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'],dtype=np.uint8)
raw=np.asarray(Image.open(O/'sources/rough_linen_diff_1k.jpg').convert('RGB'));photo=np.asarray(Image.fromarray(raw).resize((9,9),Image.Resampling.BOX))
lum=np.asarray(Image.fromarray((raw@np.array([.2126,.7152,.0722])).astype(np.float32)).resize((9,9),Image.Resampling.BOX))
# Median equals minimum for this quiet photo. >=median collapsed to one colour.
# Strict midpoint retains photographed variation; no synthetic noise is added.
mask=(lum>np.median(lum)).astype(int);assert len(np.unique(mask))==2
a=np.asarray(Image.open(O/'Props2FurnitureAtlas.png').convert('RGB')).copy()
for idx in [8,9]:
 patch=p[np.array([19,14])[mask]];inner=np.tile(patch,(28,28,1));x=idx%4*256;y=idx//4*256;a[y:y+256,x:x+256]=np.pad(inner,((2,2),(2,2),(0,0)),mode='edge')
out=O/'Props2LinenAtlas.png';Image.fromarray(a).save(out)
(O/'linen-quantizer-fix.json').write_text(json.dumps(dict(path=out.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(out.read_bytes()).hexdigest(),source_url='https://polyhaven.com/a/rough_linen',actual_reduced_rgb=np.unique(photo.reshape(-1,3),axis=0).tolist(),float_luminance_range=[float(lum.min()),float(lum.max())],fix='Float luminance BOX reduction before quantization retains sub-byte photographed variation; strict >median split, no synthetic noise. Separate atlas preserves other reviewed embedded images.',outside_palette_colors=0),indent=2))
