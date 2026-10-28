from pathlib import Path
import sys,json,subprocess,hashlib
import numpy as np
from PIL import Image
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kit-shared-v2';O=R/'art/export/kit-shared-v1';V=R/'art/_review/2026-10-28-kit-shared-v2'
sys.path.insert(0,str(R/'tools/asset-gen'));import delight as dl
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
result={}
for name in ['roof.png','decals.png']:assert sha(E/name)==sha(O/name)
result['roof_decals_byte_identical']=True
old=json.loads((R/'art/source/kit-shared-v1/jobs.json').read_text());j=old['wall_cream'];a,_=dl.process(j,R,dl.palette(O/'palette.gpl'))
assert np.array_equal(a,np.array(Image.open(R/j['output'])))
result['v1_job_backward_compatible_pixels']=True
jobs=json.loads((W/'jobs.json').read_text());j=jobs['wall_cream'];(W/'wall-cli.json').write_text(json.dumps(j));before=sha(R/j['output'])
subprocess.run([sys.executable,str(R/'tools/asset-gen/delight.py'),str(W/'wall-cli.json'),'--root',str(R)],check=True)
assert sha(R/j['output'])==before;result['v2_wall_cli_byte_identical']=True
jbad=dict(j,shadow_radius_m=.9)
try:dl.process(jbad,R);raise AssertionError('sub-metre shadow radius accepted')
except ValueError:pass
result['sub_metre_shadow_rejected']=True
v1=json.loads((R/'art/source/kit-shared-v1/proof-validation.json').read_text());v2=json.loads((W/'proof-validation.json').read_text())
for k in ['house_dimensions_m','bounds','windows','doors','tris','material_slots','texel_density_edges','render_only_ambient_mix','views']:assert v1[k]==v2[k],k
result['proof_geometry_uv_camera_lighting_settings_equal']=True
result['palette_colors']=len(dl.palette(E/'palette.gpl'))
result['hashes']={p.name:sha(p) for p in E.glob('*.png')}
result['parameters']={k:j[k] for k in ['shadow_radius_m','texture_strength','grain_sigma_px','amplitude','px_per_m']}
result['source_provenance']='provenance.csv; identical v1 source photographs and quads'
(E/'delivery-validation.json').write_text(json.dumps(result,indent=2))
p=E/'APPLY.md';s=p.read_text(encoding='utf-8').replace('키트 v1','키트 v2').replace('총 135색','총 206색').replace('명도 결 ±3%','명도 결 ±4%')
s+='\n## v2 결 조절\n\n`shadow_radius_m=1.0`은 원근 보정 후 물리 크기에서 Gaussian sigma 1m이다. 10~60cm 사진 변화는 잔여 결로 남긴다. `texture_strength=3.0`, `grain_sigma_px=0.25`; ±4% 명도 상한과 디더 없음은 유지. 기존 job에 새 인자가 없으면 기존 계산이다. 창틀 RGB 137/163/146. 지붕·데칼 PNG는 v1 바이트 그대로다. `sheet.py`를 package.py 뒤에 실행한다.\n'
p.write_text(s,encoding='utf-8')
print(json.dumps(result,indent=2),flush=True)
