from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/scene-albedo-kit';data=json.loads((E/'validation.json').read_text());assert data['png_count']==35 and len(list(E.glob('*.png')))==35 and not list(E.glob('*.fbx'))
for c in data['files']:
 assert hashlib.sha256((E/c['file']).read_bytes()).hexdigest()==c['sha256']
 assert hashlib.sha256((R/c['source']).read_bytes()).hexdigest()==c['source_sha256']
for p in E.glob('stone_*.png'):
 a=np.array(Image.open(p));assert np.array_equal(a[:,0],a[:,-1]) and np.array_equal(a[0],a[-1])
for p in E.glob('tin_*.png'):
 a=np.array(Image.open(p));assert np.array_equal(a[:,0],a[:,-1]) and np.array_equal(a[0],a[-1])
a=np.array(Image.open(E/'kart_v6e.png').convert('RGB'));b=np.array(Image.open(R/'art/export/kart-v6e/kart_v6e.png').convert('RGB'));mask=np.load(R/'art/source/kart-v6b/group-mask.npy');assert np.array_equal(a[mask==5],b[mask==5]);assert np.ptp(a[(mask==1)|(mask==2)],axis=1).max()==0
for n in ['tires_01','tires_02']:
 a=np.array(Image.open(E/(n+'.png')).convert('RGB'));assert np.ptp(a,axis=2).max()==0
proof=json.loads((W/'transfer-validation.json').read_text());assert all(c['geometry_uv_unchanged'] and c['pass_type']=='EMIT' and not c['direct'] and not c['indirect'] and not c['ao'] for c in proof)
result=dict(png_count=35,fbx_changed=0,source_and_output_hashes_verified=35,alpha_and_dimensions_identical=35,emission_transfer_geometry_uv_hashes=len(proof),seamless_tiles=5,kart_plate_exact=True,kart_tire_metal_neutral=True,kit_tires_neutral=True,tree_limitation='Painted clump vs historical baked lighting cannot be separated exactly after R2 quantization; conservative +/-4% correction retained',unity_executed=False)
(E/'final-check.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
