import sys,json,struct,argparse
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0,str(Path('tools/asset-gen').resolve()))
import check_asset as c
out=Path('art/_review/2026-10-23-cargo-set/validation');out.mkdir(parents=True,exist_ok=True)
base=Path('art/models/cargo_box_v03/density64')
a=argparse.Namespace(model=str(base/'cargo_box_v03.glb'),texture=str(base/'cargo_box_atlas_64.png'),palette='art/source/palette/pitcrew_d123_32.gpl',density=64,tolerance=.1,max_tris=2000,dimensions=[.4,.3,.3],shape='beveled-box')
tests=[]
r=c.inspect(a);tests.append({'case':'legacy_beveled_box','pass':r['pass']})
g,b=c.read_glb(a.model);b=bytearray(b);removed=0
for mi,mesh in enumerate(g['meshes']):
 for prim in mesh['primitives']:
  pos=c.accessor(g,b,prim['attributes']['POSITION']);ii=prim['indices'];ids=c.accessor(g,b,ii).ravel();tri=ids.reshape(-1,3)
  node=next(n for n in g['nodes'] if n.get('mesh')==mi);world=c.node_matrix(node);pos=(np.column_stack([pos,np.ones(len(pos))])@world.T)[:,:3]
  n=np.cross(pos[tri[:,1]]-pos[tri[:,0]],pos[tri[:,2]]-pos[tri[:,0]])
  keep=~((np.argmax(abs(n),axis=1)==1)&(n[:,1]>0));removed+=int(sum(~keep));kept=tri[keep].ravel().astype(ids.dtype)
  ac=g['accessors'][ii];v=g['bufferViews'][ac['bufferView']];off=v.get('byteOffset',0)+ac.get('byteOffset',0);b[off:off+kept.nbytes]=kept.tobytes();ac['count']=len(kept)
s=json.dumps(g,separators=(',',':')).encode();s+=b' '*((-len(s))%4);b+=b'\0'*((-len(b))%4)
payload=struct.pack('<II',len(s),0x4e4f534a)+s+struct.pack('<II',len(b),0x004e4942)+b
bad=out/'missing-positive-y.glb';bad.write_bytes(struct.pack('<4sII',b'glTF',2,12+len(payload))+payload)
a.model=str(bad);a.shape='cargo';r=c.inspect(a);(out/'missing-positive-y.json').write_text(json.dumps(r,indent=2));tests.append({'case':'cargo_missing_positive_y','removed_triangles':removed,'pass':not r['checks']['six_distinct_face_regions'],'actual_directions':[x['direction'] for x in r['main_face_uv_regions']]})
im=Image.new('RGB',(4,19),(70,70,70));regions=[{'face':'narrow','rect_xywh':[0,0,4,19]}]
r=c.flat_windows(im,regions);tests.append({'case':'narrow_uniform_rejected','pass':not r['pass'] and r['standard_8x8_window_count']==0 and r['narrow_auxiliary_window_count']==12})
for y in range(19):im.putpixel((0,y),(80,80,80))
r=c.flat_windows(im,regions);tests.append({'case':'narrow_varied_auxiliary_only','pass':r['pass'] and r['standard_8x8_window_count']==0,'evidence':r})
(out/'checker-regression.json').write_text(json.dumps({'pass':all(t['pass'] for t in tests),'tests':tests},indent=2));print(json.dumps(tests,indent=2))
sys.exit(0 if all(t['pass'] for t in tests) else 1)
