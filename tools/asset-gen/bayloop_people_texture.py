"""CC0 rough linen photograph -> metric pixel cloth, no synthetic noise."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image
R=Path(__file__).resolve().parents[2];O=R/'art/models/bayloop_people_v01';O.mkdir(parents=True,exist_ok=True)
P=np.array(json.loads((R/'art/source/palette/pitcrew_d123_32.json').read_text())['colors'],dtype=np.uint8)
src=R/'art/models/bayloop_props2_v01/sources/rough_linen_diff_1k.jpg'
a=np.asarray(Image.open(src).convert('RGB'))@np.array([.2126,.7152,.0722])
a=np.asarray(Image.fromarray(a.astype('float32')).resize((9,9),Image.Resampling.BOX))
mask=np.digitize(a,[np.quantile(a,.3),np.quantile(a,.8)])
# Shared 4x4 cells, each 256px. Clothing tones use photo luminance; small skin/eye regions use restrained same photo.
ladders=[[18,18,7],[29,29,30],[5,5,9],[9,9,5],[0,0,16],[2,6,9],[14,19,14],[2,26,2],[19,14,0],[15,17,4],[2,9,7],[0,0,16],[18,18,7],[29,29,30],[5,5,9],[9,9,5]]
atlas=np.zeros((1024,1024,3),dtype=np.uint8)
for i,ladder in enumerate(ladders):
 tile=P[np.array(ladder)[mask]];patch=np.tile(tile,(29,29,1))[:252,:252]
 atlas[i//4*256:(i//4+1)*256,i%4*256:(i%4+1)*256]=np.pad(patch,((2,2),(2,2),(0,0)),mode='edge')
out=O/'PeoplePhotoAtlas.png';Image.fromarray(atlas).save(out)
meta=dict(atlas=str(out.relative_to(R)).replace('\\','/'),source=str(src.relative_to(R)).replace('\\','/'),source_url='https://polyhaven.com/a/rough_linen',license='CC0',source_width_m=.271,pixel_sample=[9,9],density=32,method='photo luminance BOX 9px from .271m then 3-step shared-palette recolor; repeat; two-pixel edge gutter',outside_palette=0,colors=len(np.unique(atlas.reshape(-1,3),axis=0)),sha256=hashlib.sha256(out.read_bytes()).hexdigest(),ladders=ladders)
(O/'texture-manifest.json').write_text(json.dumps(meta,indent=2),encoding='utf8')
print(json.dumps(meta))
