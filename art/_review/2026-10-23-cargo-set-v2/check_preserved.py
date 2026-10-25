from pathlib import Path
import sys,json,hashlib,numpy as np
from PIL import Image
R=Path(__file__).resolve().parent;repo=R.parents[2]
sys.path.insert(0,str(repo/'tools/asset-gen'));import check_asset as c
oldbox=repo/'art/models/cargo_box_final';newbox=repo/'art/models/cargo_box_final_v2'
a=np.asarray(Image.open(oldbox/'cargo_box_atlas_64.png'));b=np.asarray(Image.open(newbox/'cargo_box_atlas_64.png'));y,x=np.where(np.any(a!=b,axis=2))
g1,b1=c.read_glb(oldbox/'cargo_box_final.glb');g2,b2=c.read_glb(newbox/'cargo_box_final_v2.glb')
pr1=g1['meshes'][0]['primitives'][0];pr2=g2['meshes'][0]['primitives'][0]
positions_same=np.array_equal(c.accessor(g1,b1,pr1['attributes']['POSITION']),c.accessor(g2,b2,pr2['attributes']['POSITION']))
uv_same=np.array_equal(c.accessor(g1,b1,pr1['attributes']['TEXCOORD_0']),c.accessor(g2,b2,pr2['attributes']['TEXCOORD_0']))
wine=repo/'art/models/cargo_wine_v01/cargo_wine_v01.glb';oldwine=json.loads((repo/'art/_review/2026-10-23-cargo-set/validation/wine.json').read_text())
data={'box_changed_texels':len(x),'box_changed_bounds':[int(x.min()),int(y.min()),int(x.max()),int(y.max())],'box_other_five_faces_identical':bool(x.min()>=22 and y.min()>=43),'box_mesh_positions_identical':positions_same,'box_uv_identical':uv_same,'wine_model_unchanged':hashlib.sha256(wine.read_bytes()).hexdigest()==oldwine['model_sha256']}
(R/'preservation-proof.json').write_text(json.dumps(data,indent=2));print(data)
assert all(data[k] for k in ['box_other_five_faces_identical','box_mesh_positions_identical','box_uv_identical','wine_model_unchanged'])
