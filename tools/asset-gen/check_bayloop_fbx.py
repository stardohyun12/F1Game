"""Blender background re-import audit; never rewrites source FBX/GLB/PNG.

Run blender -b --python tools/asset-gen/check_bayloop_fbx.py -- --out PATH
FBX is copied to a temporary directory before import so embedded image
extraction cannot create .fbm files beside the delivery assets.
"""
import argparse, hashlib, json, shutil, sys, tempfile
from pathlib import Path
import bpy
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(Path(__file__).resolve().parent))
from vertex_position_audit import compare_positions

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.context.scene.unit_settings.scale_length=1

def geometry():
    vertices=[];tris=0;roots=[];front=[];texture_links=[]
    for obj in bpy.context.scene.objects:
        if obj.parent is None:roots.append({'name':obj.name,'origin_blender':list(obj.matrix_world.translation)})
        if obj.type!='MESH':continue
        mesh=obj.data;mesh.calc_loop_triangles();tris+=len(mesh.loop_triangles)
        points=np.array([list(obj.matrix_world@v.co) for v in mesh.vertices])
        # Blender Z-up -> delivery/Unity +Y-up, road/front +Z.
        points=points[:,[0,2,1]]*np.array([1,1,-1]);vertices.extend(points)
        for triangle in mesh.loop_triangles:
            p=points[list(triangle.vertices)]
            n=np.cross(p[1]-p[0],p[2]-p[0])
            if n[2]>0 and np.allclose(p[:,2],0,atol=1e-4):front.extend(p)
        for material in mesh.materials:
            if not material or not material.use_nodes:continue
            for node in material.node_tree.nodes:
                if node.type!='TEX_IMAGE' or not node.image:continue
                image=node.image; image_path=Path(bpy.path.abspath(image.filepath))
                packed=image.packed_file
                digest=hashlib.sha256(packed.data).hexdigest() if packed else (sha(image_path) if image_path.is_file() else None)
                linked=any(link.from_node==node for link in material.node_tree.links)
                texture_links.append({'material':material.name,'image':image.name,'linked':linked,
                    'sha256':digest,'packed':bool(packed),'resolved_file':image_path.name,
                    'size':list(image.size),'interpolation':node.interpolation})
    if not vertices:raise ValueError('Importer produced no mesh')
    v=np.array(vertices)
    return {'tris':tris,'bounds_min':v.min(0).tolist(),'bounds_max':v.max(0).tolist(),
            'dimensions_m':np.ptp(v,axis=0).tolist(),'roots':roots,'front_triangle_vertices':len(front),
            'texture_links':texture_links},v

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest',default='art/models/bayloop_buildings_v01/manifest.json')
    parser.add_argument('--out',default='art/_review/2026-10-25-bayloop-buildings/fbx-checks.json')
    parser.add_argument('--ids',nargs='*')
    parser.add_argument('--root',default=str(ROOT))
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    root=Path(args.root).resolve()
    manifest=json.loads((root/args.manifest).read_text(encoding='utf-8-sig'))
    results=[]
    for asset in manifest['assets']:
        if args.ids and asset['id'] not in args.ids:continue
        paths={key:root/asset[key] for key in ('model','fbx','texture')}
        before={key:sha(path) for key,path in paths.items()}
        try:
            clear();bpy.ops.import_scene.gltf(filepath=str(paths['model']))
            glb,gpoints=geometry()
            with tempfile.TemporaryDirectory(prefix='pitcrew-fbx-audit-') as directory:
                target=Path(directory)/paths['fbx'].name;shutil.copyfile(paths['fbx'],target)
                clear();bpy.ops.import_scene.fbx(filepath=str(target),global_scale=1.0,use_manual_orientation=False,use_image_search=False)
                fbx,fpoints=geometry()
                position_audit=compare_positions(gpoints,fpoints,tolerance=1e-4)
                checks={
                    'triangle_count_matches_glb':fbx['tris']==glb['tris'],
                    'triangle_budget':0<fbx['tris']<=min(800 if asset.get('asset_kind')=='prop' or asset.get('type')=='props' else 4000,int(asset.get('max_tris',manifest['max_tris'])),int(manifest['max_tris'])),
                    'bounds_match_glb':all(np.allclose(fbx[k],glb[k],atol=1e-4,rtol=0) for k in ('bounds_min','bounds_max')),
                    'dimensions_match_manifest_metres':bool(np.allclose(fbx['dimensions_m'],asset['dimensions_m'],atol=1e-4,rtol=0)),
                    'root_origins_zero':bool(fbx['roots']) and all(np.allclose(r['origin_blender'],0,atol=1e-5) for r in fbx['roots']),
                    'floor_y_zero':abs(fbx['bounds_min'][1])<1e-4,
                    'vertex_positions_match_glb':position_audit['pass'],
                    'embedded_albedo_recovered_and_linked':bool(fbx['texture_links']) and all(r['linked'] and r['sha256']==before['texture'] for r in fbx['texture_links']),
                    'source_files_unchanged':before=={key:sha(path) for key,path in paths.items()},
                }
                if asset.get('asset_kind')=='prop' or asset.get('type')=='props':
                    lo,hi=fbx['bounds_min'],fbx['bounds_max'];kind=asset.get('pivot_kind')
                    checks.update({'prop_budget_declared':asset.get('max_tris')==800,
                        'prop_pivot_kind':kind in ('floor_center','floor_back_center'),
                        'prop_x_center_zero':abs(lo[0]+hi[0])<1e-4,
                        'prop_depth_pivot':bool(abs(lo[2]+hi[2])<1e-4 if kind=='floor_center' else abs(lo[2])<1e-4 if kind=='floor_back_center' else False)})
                else:
                    checks['front_positive_z_wall_at_zero']=fbx['front_triangle_vertices']>0
                results.append({'id':asset['id'],'pass':all(checks.values()),'checks':checks,'fbx':fbx,'glb':glb,'vertex_position_audit':position_audit,'source_sha256':before})
        except Exception as exc:
            results.append({'id':asset['id'],'pass':False,'error':str(exc),'source_files_unchanged':before=={key:sha(path) for key,path in paths.items()}})
    report={'pass':bool(results) and all(r['pass'] for r in results),'blender_version':bpy.app.version_string,
            'method':'Fresh Blender FBX import global_scale=1.0, embedded axis metadata; Blender x,y,z converted to Unity x,z,-y. Compare independent GLB re-import geometry. FBX imported from temporary copy without external texture search, proving embedded albedo recovery. Source files hashed before/after.',
            'limitations':'No Unity import or prefab validation. FBX does not guarantee Unity Point importer settings; linked image interpolation is recorded, not treated as a Unity texture-setting proof.',
            'assets':results}
    output=root/args.out;output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'pass':report['pass'],'assets':[{k:a.get(k) for k in ('id','pass','error')} for a in results]}))

if __name__=='__main__':main()
