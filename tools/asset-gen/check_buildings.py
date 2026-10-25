"""Static building audit: actual GLB world geometry, albedo and source ledger.

FBX geometry/import is NOT verified by this audit; only existence and provenance.
Tiny triangles are reported separately, never used to certify main-face detail.
"""
from pathlib import Path
import argparse, csv, hashlib, io, json
from urllib.parse import urlparse
import numpy as np
from PIL import Image
from check_asset import read_glb, accessor, node_matrix
from check_hill_house import triangle_density
from photo_to_pixel import load_palette

ROOT = Path(__file__).resolve().parents[2]

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def is_prop(asset):
    return asset.get('asset_kind')=='prop' or asset.get('type')=='props'

def is_person(asset):
    return asset.get('asset_kind')=='person'

def is_landmark(asset):
    return asset.get('asset_kind')=='landmark'

def triangle_limit(asset, manifest):
    return min(1500 if is_person(asset) else 800 if is_prop(asset) else 4000,
               int(asset.get('max_tris',manifest['max_tris'])),int(manifest['max_tris']))

def prop_pivot_checks(asset, lo, hi):
    """Bounds measured after node transforms, Unity +Z is forward."""
    kind=asset.get('pivot_kind')
    return {'prop_pivot_kind':kind in ('floor_center','floor_back_center'),
            'prop_x_center_zero':abs(float(lo[0]+hi[0]))<1e-4,
            'prop_depth_pivot':bool(abs(float(lo[2]+hi[2]))<1e-4 if kind=='floor_center'
                                    else abs(float(lo[2]))<1e-4 if kind=='floor_back_center' else False)}

def rooted(root, value):
    p = (root / value).resolve()
    p.relative_to(root.resolve())
    return p

def triangle_colors(pixels, uv):
    """Exhaustive texel-centre rasterization inside UV triangle, no bbox leakage.

    glTF UV origin is image top-left. Clamp/repeat cannot mask out-of-atlas UVs.
    Degenerate/narrow triangles without texel centres use interior barycentric
    samples of the actual Point-filtered image, separately reported by caller.
    """
    h, w = pixels.shape[:2]
    if np.any(uv < -1e-4) or np.any(uv > np.array([w,h])+1e-4):
        raise ValueError('UV outside atlas')
    lo = np.maximum(np.floor(uv.min(0)).astype(int), 0)
    hi = np.minimum(np.ceil(uv.max(0)).astype(int), [w,h])
    inv = np.linalg.inv(np.column_stack([uv[1]-uv[0],uv[2]-uv[0]]))
    colors = set(); count = 0
    for y in range(lo[1], hi[1]):
        xs = np.arange(lo[0],hi[0])
        q = np.column_stack([xs+.5,np.full(len(xs),y+.5)])
        ab = (q-uv[0]) @ inv.T
        inside = (ab[:,0]>=0)&(ab[:,1]>=0)&(ab.sum(1)<=1)
        vals = pixels[y,xs[inside],:3]
        count += len(vals)
        colors.update(map(tuple, vals.tolist()))
    fallback = count == 0
    if fallback:
        for a,b in [(1/3,1/3),(.1,.1),(.8,.1),(.1,.8)]:
            p = uv[0]+a*(uv[1]-uv[0])+b*(uv[2]-uv[0])
            x,y = np.minimum(np.maximum(np.floor(p).astype(int),0),[w-1,h-1])
            colors.add(tuple(pixels[y,x,:3].tolist()))
    return len(colors), count, fallback

def ledger(path, rows, root, urls):
    matches=[r for r in rows if r.get('path')==path]
    row=matches[0] if len(matches)==1 else {}
    return {'pass':len(matches)==1 and row.get('sha256')==sha(rooted(root,path))
            and row.get('ai_generated')=='no' and bool(row.get('license'))
            and bool(row.get('tool')) and bool(urls)
            and all(u in row.get('source_url','') for u in urls),
            'path':path,'record_count':len(matches)}

def photo_metadata(manifest, root):
    """Explicit downloaded-photo records, not the name of a parent directory."""
    records=[]
    def visit(value, origin):
        if isinstance(value,list):
            for item in value:visit(item,origin)
        elif isinstance(value,dict):
            if all(k in value for k in ('path','url','download_url','sha256','license')):
                records.append(dict(value,metadata_file=origin))
            for item in value.values():
                if isinstance(item,(list,dict)):visit(item,origin)
    for path in manifest.get('source_manifests',[]):
        visit(json.loads(rooted(root,path).read_text(encoding='utf-8-sig')),path)
    return records

def downloaded_photo_evidence(url, records, rows, root):
    found=[]
    for record in records:
        if record['url']!=url or record['license']!='CC0-1.0':continue
        download=urlparse(record['download_url']);host=download.hostname or ''
        if download.scheme!='https' or not any(host==h or host.endswith('.'+h) for h in ('polyhaven.org','polyhaven.com','ambientcg.com')):continue
        path=record['path']; matches=[r for r in rows if r.get('path')==path]
        if len(matches)!=1:continue
        row=matches[0]; p=rooted(root,path)
        if not p.is_file() or p.suffix.lower() not in ('.png','.jpg','.jpeg','.exr'):continue
        if row.get('ai_generated')!='no' or row.get('license')!='CC0-1.0' or url not in row.get('source_url',''):continue
        digest=sha(p)
        if digest!=row.get('sha256') or digest!=record['sha256']:continue
        expected_md5=record.get('provider_md5',record.get('md5'))
        if expected_md5 and hashlib.md5(p.read_bytes()).hexdigest()!=expected_md5:continue
        found.append({'path':path,'metadata_file':record['metadata_file'],'download_url':record['download_url'],'sha256':digest})
    return {'url':url,'verified_source_paths':[r['path'] for r in found],'download_records':found,'pass':bool(found)}

def inspect_asset(asset, manifest, root, rows):
    image=Image.open(rooted(root,asset['texture'])).convert('RGBA')
    px=np.asarray(image); w,h=image.size
    palette={tuple(c) for c in load_palette(rooted(root,manifest['palette']))}
    actual=set(map(tuple,np.unique(px[:,:,:3].reshape(-1,3),axis=0).tolist()))
    checks={'palette_at_most_32':0<len(palette)<=32,'palette_outside_zero':not(actual-palette),
            'opaque_albedo':bool(np.all(px[:,:,3]==255)),
            'power_of_two':all(n>0 and n&(n-1)==0 for n in (w,h))}
    g,blob=read_glb(rooted(root,asset['model']))
    vertices=[]; densities=[]; faces=[]; front=[]; materials=[]; roots=[]
    active=set()
    def walk(index,parent):
        if index in active:raise ValueError('Cyclic node graph')
        active.add(index)
        node=g['nodes'][index]; world=parent@node_matrix(node)
        if 'mesh' in node:
            for prim in g['meshes'][node['mesh']]['primitives']:
                if prim.get('mode',4)!=4:raise ValueError('Nontriangle primitive')
                attrs=prim['attributes']; pos=accessor(g,blob,attrs['POSITION']).astype(float)
                pos=(np.column_stack([pos,np.ones(len(pos))])@world.T)[:,:3]
                uv=accessor(g,blob,attrs['TEXCOORD_0']).astype(float)*[w,h]
                ids=accessor(g,blob,prim['indices']).ravel() if 'indices' in prim else np.arange(len(pos))
                if len(ids)%3:raise ValueError('Invalid triangle count')
                mat=g['materials'][prim['material']]; pbr=mat['pbrMetallicRoughness']
                tc=pbr['baseColorTexture']; tex=g['textures'][tc['index']]
                sampler=g['samplers'][tex['sampler']]
                meta=g['images'][tex['source']]; view=g['bufferViews'][meta['bufferView']]
                off=view.get('byteOffset',0)
                embedded=Image.open(io.BytesIO(blob[off:off+view['byteLength']])).convert('RGBA')
                materials.append({'embedded_matches':embedded.size==image.size and embedded.tobytes()==image.tobytes(),
                    'point':sampler.get('magFilter')==9728 and sampler.get('minFilter') in (9728,9984),
                    'no_normal_metal':not any(k in mat for k in ('normalTexture','occlusionTexture','emissiveTexture')) and pbr.get('metallicFactor',1)==0 and 'metallicRoughnessTexture' not in pbr,
                    'white_factor':pbr.get('baseColorFactor',[1,1,1,1])==[1,1,1,1],
                    'uv0_no_transform':tc.get('texCoord',0)==0 and not tc.get('extensions')})
                for idx in ids.reshape(-1,3):
                    p=pos[idx]; t=uv[idx]; vertices.extend(p)
                    ds=triangle_density(p,t); densities.append(ds)
                    normal=np.cross(p[1]-p[0],p[2]-p[0]); area=np.linalg.norm(normal)/2
                    nc,samples,fallback=triangle_colors(px,t)
                    faces.append({'triangle':len(faces),'area_m2':float(area),'colors':nc,
                                  'texel_centres':samples,'fallback_samples':fallback,'tiny':bool(area<.04)})
                    if normal[2]>0 and np.allclose(p[:,2],0,atol=1e-4):front.extend(p)
        for child in node.get('children',[]):walk(child,world)
        active.remove(index)
    for idx in g['scenes'][g.get('scene',0)]['nodes']:
        roots.append(bool(np.allclose(node_matrix(g['nodes'][idx])[:3,3],0,atol=1e-5)))
        walk(idx,np.eye(4))
    if not vertices:raise ValueError('No geometry')
    v=np.array(vertices); lo=v.min(0); hi=v.max(0); d=np.array(densities)
    major=[f for f in faces if not f['tiny']]; flat=[f for f in major if f['colors']<2]
    tiny=[f for f in faces if f['tiny']]
    target=float(manifest['density_px_m'])
    checks.update({'world_density_32':target==32,'triangle_budget':0<len(faces)<=triangle_limit(asset,manifest),
        'density_both_principal_axes':bool(np.all(d>=target*.9-1e-4) and np.all(d<=target*1.1+1e-4)),
        'bounds_match':bool(np.allclose(lo,asset['bounds_min'],atol=1e-4) and np.allclose(hi,asset['bounds_max'],atol=1e-4)),
        'dimensions_match':bool(np.allclose(hi-lo,asset['dimensions_m'],atol=1e-4)),
        'root_origin':all(roots),'floor_y_zero':abs(float(lo[1]))<1e-4,
        'main_triangle_variation':(bool(major) or is_prop(asset) or is_person(asset)) and not flat,
        'material_checks':bool(materials) and all(all(m.values()) for m in materials),
        'fbx_exists':rooted(root,asset['fbx']).is_file() and rooted(root,asset['fbx']).stat().st_size>0})
    if is_landmark(asset):
        base=v[np.abs(v[:,1])<1e-4]
        checks.update({'landmark_floor_pivot':asset.get('pivot_kind')=='floor_center',
            'landmark_ground_anchor':len(base)>=3 and bool(np.allclose(base[:,[0,2]].min(0)+base[:,[0,2]].max(0),0,atol=1e-4)),
            'landmark_budget_declared':asset.get('max_tris')==4000})
    elif is_person(asset):
        checks.update({'person_height':1.6<=float(hi[1]-lo[1])<=1.7,
            'person_pivot_kind':asset.get('pivot_kind')=='floor_center',
            'person_x_center':abs(float(lo[0]+hi[0]))<1e-4,
            'person_budget_declared':asset.get('max_tris')==1500,
            'person_surface_variation':any(f['colors']>=2 for f in faces),
            'person_skin_present':bool(g.get('skins')) and any('skin' in n for n in g['nodes'])})
        # A human's toes extend ahead of the skeleton root. Depth-bound midpoint
        # is not the root pivot; actual floor height and lateral centre are tested.
    elif is_prop(asset):
        checks.update(prop_pivot_checks(asset,lo,hi))
        # Tiny objects need not contain a 0.04 m2 face, but cannot pass with
        # entirely uniform sampled surfaces. Density still checks every face.
        checks['prop_sampled_surface_variation']=any(f['colors']>=2 for f in faces)
        checks['prop_budget_declared']=asset.get('max_tris')==800
    else:
        checks['front_wall_z_zero']=asset['front_wall_z']==0 and bool(front)
        checks['front_wall_x_center_zero']=bool(front) and abs(float(np.array(front)[:,0].min()+np.array(front)[:,0].max()))<1e-4
    urls=asset['source_urls']; records=[ledger(asset[k],rows,root,urls) for k in ('texture','model','fbx')]
    metadata=photo_metadata(manifest,root)
    source_evidence=[downloaded_photo_evidence(url,metadata,rows,root) for url in urls]
    checks['provenance_files']=all(r['pass'] for r in records)
    checks['provenance_photos']=bool(source_evidence) and all(r['pass'] for r in source_evidence)
    return {'id':asset['id'],'type':asset['type'],'pass':all(checks.values()),'checks':checks,
        'tris':len(faces),'triangle_limit':triangle_limit(asset,manifest),'pivot_kind':asset.get('pivot_kind','floor_front_wall_center'),'bounds_min':lo.tolist(),'bounds_max':hi.tolist(),
        'density_min':float(d.min()),'density_max':float(d.max()),'texture_size':[w,h],
        'used_colors':len(actual),'outside_colors':len(actual-palette),
        'major_triangles':len(major),'flat_major_triangles':flat,'tiny_triangles':tiny,
        'tiny_area_m2':sum(f['area_m2'] for f in tiny),'materials':materials,
        'provenance':records,'source_provenance':source_evidence,
        'hashes':{k:sha(rooted(root,asset[k])) for k in ('texture','model','fbx')}}

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--building-manifest',required=True);p.add_argument('--out',required=True)
    p.add_argument('--root',default=str(ROOT));p.add_argument('--provenance',default='art/provenance.csv')
    args=p.parse_args(); results=[]
    try:
        root=Path(args.root).resolve()
        manifest=json.loads((root/args.building_manifest).read_text(encoding='utf-8-sig'))
        with rooted(root,args.provenance).open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))
        for a in manifest['assets']:
            try:results.append(inspect_asset(a,manifest,root,rows))
            except Exception as e:results.append({'id':a.get('id'),'pass':False,'error':str(e)})
        result={'pass':bool(results) and all(r['pass'] for r in results),'assets':results,
            'scope':'Actual GLB Y-up world geometry and albedo only; FBX geometry/Unity import and visual quality not verified. All triangles receive density check. Area <0.04 m2 triangles receive reported color samples but are excluded from main-surface variation gate. Props additionally require at least one sampled triangle with multiple colors, even when every triangle is tiny. Prop pivots are measured from actual world bounds; building front-wall pivot checks are unchanged. Source license truth requires human review.'}
    except Exception as e:result={'pass':False,'error':str(e)}
    output=Path(args.root)/args.out
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'pass':result['pass'],'assets':[{k:r.get(k) for k in ('id','pass','tris','error')} for r in results]}))
    return 0 if result['pass'] else 1

if __name__=='__main__':raise SystemExit(main())
