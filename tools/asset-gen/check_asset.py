"""Validate the exported GLB, not producer-supplied triangle/density claims.
Requires Pillow + numpy. Exit 0 only when every check passes.
"""
from pathlib import Path
import argparse,json,struct,io,hashlib,sys,csv
import numpy as np
from PIL import Image
from photo_to_pixel import load_palette

def read_glb(path):
 data=Path(path).read_bytes()
 magic,version,length=struct.unpack_from('<4sII',data)
 if magic!=b'glTF' or version!=2 or length!=len(data):raise ValueError('Invalid GLB2 header')
 pos=12;g=None;blob=None
 while pos<len(data):
  size,kind=struct.unpack_from('<II',data,pos);pos+=8;payload=data[pos:pos+size];pos+=size
  if kind==0x4E4F534A:g=json.loads(payload)
  elif kind==0x004E4942:blob=payload
 if g is None or blob is None:raise ValueError('GLB requires JSON + BIN')
 return g,blob

def accessor(g,blob,index):
 a=g['accessors'][index]
 if 'sparse' in a:raise ValueError('Sparse accessors unsupported: fail closed')
 view=g['bufferViews'][a['bufferView']]
 types={5120:'i1',5121:'u1',5122:'<i2',5123:'<u2',5125:'<u4',5126:'<f4'}
 dims={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
 dt=np.dtype(types[a['componentType']]);n=dims[a['type']]
 off=view.get('byteOffset',0)+a.get('byteOffset',0);stride=view.get('byteStride',dt.itemsize*n)
 return np.ndarray((a['count'],n),dtype=dt,buffer=blob,offset=off,strides=(stride,dt.itemsize)).copy()

def node_matrix(n):
 if 'matrix' in n:return np.array(n['matrix']).reshape(4,4).T
 x,y,z,w=n.get('rotation',[0,0,0,1])
 r=np.array([[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]])
 out=np.eye(4);out[:3,:3]=r@np.diag(n.get('scale',[1,1,1]));out[:3,3]=n.get('translation',[0,0,0]);return out

def flat_windows(image,regions,window=8):
 """Every stride-1 window wholly inside a declared main UV face.
 Gutters, unused pixels and neighbouring faces never supply variation.
 This minimum variation test is not an artistic quality or noise test.
 """
 pixels=np.asarray(image.convert('RGBA'));height,width=pixels.shape[:2]
 if window<1 or not regions:raise ValueError('Surface check needs regions and a positive window')
 results=[];total=0;violations=[]
 for region in regions:
  rect=region['rect_xywh']
  if len(rect)!=4 or any(isinstance(v,bool) or int(v)!=v for v in rect):raise ValueError('Surface rect_xywh must contain four integers')
  x,y,w,h=map(int,rect);name=region.get('face',str(len(results)))
  if min(x,y)<0 or min(w,h)<1 or x+w>width or y+h>height:raise ValueError('Invalid surface region: '+name)
  ww,wh=min(window,w),min(window,h)
  surface=pixels[y:y+h,x:x+w]
  if np.any(surface[:,:,3]!=255):raise ValueError('Main surface must be opaque: '+name)
  count=0;bad=[];minimum=ww*wh
  for sy in range(h-wh+1):
   for sx in range(w-ww+1):
    colors=len(np.unique(surface[sy:sy+wh,sx:sx+ww,:3].reshape(-1,3),axis=0))
    count+=1;minimum=min(minimum,colors)
    if colors<2:bad.append([x+sx,y+sy,ww,wh])
  total+=count
  violations.extend({'face':name,'rect_xywh':r} for r in bad)
  results.append({'face':name,'rect_xywh':[x,y,w,h],'window_size':[ww,wh],'check_kind':'standard_8x8' if ww==wh==8 else 'narrow_surface_auxiliary','window_count':count,'minimum_colors':minimum,'flat_window_count':len(bad)})
 return {'status':'checked','pass':not violations,'requested_window_size':[window,window],'stride':1,'minimum_required_colors':2,'window_count':total,'standard_8x8_window_count':sum(r['window_count'] for r in results if r['check_kind']=='standard_8x8'),'narrow_auxiliary_window_count':sum(r['window_count'] for r in results if r['check_kind']=='narrow_surface_auxiliary'),'flat_window_count':len(violations),'flat_windows':violations,'regions':results,'scope':'Declared main face interiors only; atlas padding, gutters and neighbouring faces excluded. Narrow faces use min(8,width) x min(8,height) auxiliary windows, not an 8x8 claim. RGB variation only, not alpha; manifest coverage is not inferred from geometry.'}

def inspect(args):
 palette=load_palette(args.palette);img=Image.open(args.texture).convert('RGBA');w,h=img.size
 px=np.asarray(img);opaque=px[:,:,3]>0
 actual={tuple(int(v) for v in c) for c in px[:,:,:3][opaque]};extra=sorted(actual-set(palette))
 g,blob=read_glb(args.model);densities=[];vertices=[];tris=0;groups={};group_areas={};triangle_regions=[];samplers=[];material_issues=[];image_match=[]
 def walk(idx,parent):
  nonlocal tris
  n=g['nodes'][idx];world=parent@node_matrix(n)
  if 'mesh' in n:
   for prim in g['meshes'][n['mesh']]['primitives']:
    if prim.get('mode',4)!=4:raise ValueError('Only triangle primitives supported')
    attrs=prim['attributes'];pos=accessor(g,blob,attrs['POSITION']).astype(float)
    pos=(np.column_stack([pos,np.ones(len(pos))])@world.T)[:,:3];vertices.extend(pos)
    uv=accessor(g,blob,attrs['TEXCOORD_0']).astype(float)*[w,h]
    ids=accessor(g,blob,prim['indices']).ravel() if 'indices' in prim else np.arange(len(pos))
    if len(ids)%3:raise ValueError('Triangle index count not divisible by 3')
    for ids3 in ids.reshape(-1,3):
     p=pos[ids3];t=uv[ids3];edge=np.column_stack([p[1]-p[0],p[2]-p[0]])
     normal=np.cross(edge[:,0],edge[:,1]);area=np.linalg.norm(normal)
     if area<1e-12:raise ValueError('Degenerate geometry')
     normal/=area;_,basis=np.linalg.qr(edge)
     j=np.column_stack([t[1]-t[0],t[2]-t[0]])@np.linalg.inv(basis)
     singular=np.linalg.svd(j,compute_uv=False)
     densities.append([float(min(singular)),float(max(singular))]);tris+=1
     triangle_regions.append({'normal':tuple(np.round(normal,5)),'area_m2':float(area/2),'uv_pixel_bounds':[t.min(axis=0).tolist(),t.max(axis=0).tolist()]})
     key=tuple(np.round(normal,5));groups.setdefault(key,[]).extend(t.tolist());group_areas[key]=group_areas.get(key,0)+float(area/2)
    mat=g['materials'][prim['material']];pbr=mat['pbrMetallicRoughness'];tex=g['textures'][pbr['baseColorTexture']['index']]
    sampler=g.get('samplers',[])[tex['sampler']] if 'sampler' in tex else {}
    samplers.append(sampler)
    if 'normalTexture' in mat:material_issues.append('normal map')
    if pbr.get('metallicFactor',1)!=0:material_issues.append('metallicFactor must be zero')
    imeta=g['images'][tex['source']]
    if 'bufferView' not in imeta:raise ValueError('Expected embedded texture')
    view=g['bufferViews'][imeta['bufferView']];off=view.get('byteOffset',0)
    embedded=Image.open(io.BytesIO(blob[off:off+view['byteLength']])).convert('RGBA')
    image_match.append(embedded.size==img.size and embedded.tobytes()==img.tobytes())
  for child in n.get('children',[]):walk(child,world)
 scene=g['scenes'][g.get('scene',0)]
 for idx in scene['nodes']:walk(idx,np.eye(4))
 if not vertices or not densities:raise ValueError('No textured triangle geometry')
 bounds=np.array(vertices);lo=bounds.min(axis=0);hi=bounds.max(axis=0)
 ranges=[]
 for normal,points in groups.items():
  arr=np.array(points);ranges.append({'normal':normal,'area_m2':group_areas[normal],'uv_pixel_bounds':[arr.min(axis=0).tolist(),arr.max(axis=0).tolist()]})
 density_min=min(x[0] for x in densities);density_max=max(x[1] for x in densities)
 low=args.density*(1-args.tolerance);high=args.density*(1+args.tolerance)
 unique_rects={tuple(round(v,4) for row in r['uv_pixel_bounds'] for v in row) for r in ranges}
 # Bevels and dents introduce additional normals. Select the largest near-axis
 # plane for each signed axis; tiny bevel faces cannot replace a missing side.
 main_regions=[]
 for axis in range(3):
  for sign in [-1,1]:
   candidates=[r for r in triangle_regions if int(np.argmax(np.abs(r['normal'])))==axis and r['normal'][axis]*sign>0] if args.shape=='cargo' else [r for r in triangle_regions if r['normal'][axis]*sign>=.98]
   if candidates:
    main=max(candidates,key=lambda r:r['area_m2'])
    projected_area=float(np.prod(np.delete(hi-lo,axis)))
    if args.shape=='cargo' or main['area_m2']>=projected_area*.1:main_regions.append(dict(main,direction=('+' if sign>0 else '-')+'XYZ'[axis]))
 main_overlap=[]
 for i,a in enumerate(main_regions):
  for j,b in enumerate(main_regions[:i]):
   a0,a1=np.array(a['uv_pixel_bounds']);b0,b1=np.array(b['uv_pixel_bounds'])
   if np.all(np.minimum(a1,b1)-np.maximum(a0,b0)>1e-4):main_overlap.append([j,i])
 main_rects={tuple(round(v,4) for row in r['uv_pixel_bounds'] for v in row) for r in main_regions}
 six_regions=(len(main_regions)==len(main_rects)==6 and not main_overlap) if args.shape in ['beveled-box','cargo'] else (len(groups)==len(unique_rects)==6)
 checks={'palette_outside_zero':not extra,'texture_power_of_two':all(n>0 and n&(n-1)==0 for n in [w,h]),'texture_is_64':w==h==64,'triangle_budget':0<tris<=args.max_tris,'density_both_principal_axes':density_min>=low-1e-5 and density_max<=high+1e-5,'embedded_texture_matches_png':all(image_match),'point_sampling':all(s.get('magFilter')==9728 and s.get('minFilter') in [9728,9984] for s in samplers),'six_distinct_face_regions':six_regions,'material_no_normal_or_metal':not material_issues,'dimensions_metres':np.allclose(hi-lo,args.dimensions,atol=.00001),'pivot_floor_center':np.allclose(lo,[-args.dimensions[0]/2,0,-args.dimensions[2]/2],atol=.00001)}
 return {'shape':args.shape,'six_region_scope':'cargo: largest actual triangle per signed dominant world-normal axis; UV bounding boxes must be distinct and disjoint. This verifies six representative directions, not all island overlap, text semantics, or complete surface-manifest coverage. Beveled-box keeps near-axis and projected-area thresholds.', 'main_face_uv_regions':main_regions,'main_face_uv_overlaps':main_overlap,'pass':all(checks.values()),'checks':checks,'palette_colors':len(palette),'used_colors':len(actual),'outside_color_count':len(extra),'outside_colors':extra[:32],'texture_size':[w,h],'tris':tris,'density_target':args.density,'density_tolerance':args.tolerance,'density_min':density_min,'density_max':density_max,'per_triangle_density':densities,'dimensions_m':(hi-lo).tolist(),'bounds_min':lo.tolist(),'bounds_max':hi.tolist(),'face_uv_regions':ranges,'samplers':samplers,'material_issues':material_issues,'model_sha256':hashlib.sha256(Path(args.model).read_bytes()).hexdigest(),'texture_sha256':hashlib.sha256(Path(args.texture).read_bytes()).hexdigest(),'method':'GLB binary accessor positions + node transforms; singular values of world-to-UV-pixel Jacobian per triangle. Not metadata claims.'}

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--model',required=True);p.add_argument('--texture',required=True);p.add_argument('--palette',required=True);p.add_argument('--out',required=True);p.add_argument('--density',type=float,default=32);p.add_argument('--tolerance',type=float,default=.1);p.add_argument('--max-tris',type=int,default=2000);p.add_argument('--dimensions',nargs=3,type=float,default=[.4,.3,.3]);p.add_argument('--shape',choices=['box','beveled-box','cargo'],default='box',help='Six separate near-axis main UV regions; every triangle receives density checks.');p.add_argument('--surface-manifest',help='JSON faces[].rect_xywh main face regions for mandatory 8x8 variation checking');args=p.parse_args()
 try:
  result=inspect(args)
  if args.surface_manifest:
   manifest=json.loads(Path(args.surface_manifest).read_text(encoding='utf-8'))
   regions=manifest['faces']
   if len(regions)!=6 or len({r['face'] for r in regions})!=6:raise ValueError('Surface manifest must declare six distinct main faces')
   evidence=flat_windows(Image.open(args.texture),regions)
   evidence['manifest']=str(args.surface_manifest)
   evidence['manifest_sha256']=hashlib.sha256(Path(args.surface_manifest).read_bytes()).hexdigest()
   result['flat_windows']=evidence
   result['checks']['no_monochrome_main_face_windows']=evidence['pass']
  else:result['flat_windows']={'status':'not_requested','reason':'Pass --surface-manifest for explicit main-face coverage. Legacy runs do not claim this check.'}
  root=Path(__file__).resolve().parents[2]
  with (root/'art/provenance.csv').open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))
  relative=Path(args.texture).resolve().relative_to(root).as_posix()
  recorded=any(r['path']==relative and r['sha256']==result['texture_sha256'] and r['ai_generated']=='no' and r['source_url'] and r['tool'] for r in rows)
  result['checks']['provenance_registered_non_ai']=recorded
  result['pass']=all(result['checks'].values())
 except Exception as e:result={'pass':False,'error':str(e)}
 Path(args.out).parent.mkdir(parents=True,exist_ok=True);Path(args.out).write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps({k:v for k,v in result.items() if k not in ['per_triangle_density','face_uv_regions']},ensure_ascii=False));return 0 if result['pass'] else 1
if __name__=='__main__':
 if '--texture-manifest' in sys.argv:
  from check_texture_tiles import main as texture_main
  sys.exit(texture_main())
 if '--ui-manifest' in sys.argv:
  from check_ui_sprites import main as ui_main
  sys.exit(ui_main())
 sys.exit(main())
