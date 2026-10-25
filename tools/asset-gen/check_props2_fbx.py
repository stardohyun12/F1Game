"""D141 FBX reimport audit, reusing established importer/geometry measurement."""
import argparse
import json
from pathlib import Path
import shutil
import sys
import tempfile
sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
import numpy as np
from check_bayloop_fbx import clear, geometry, sha, compare_positions
from check_props2 import ROOT, rooted, budget, pivot_checks, selected_assets


def inspect(asset, manifest, root):
    limit = budget(asset, manifest)
    paths = {key: rooted(root, asset[key]) for key in ('model','fbx','texture')}
    before = {key:sha(path) for key,path in paths.items()}
    clear(); bpy.ops.import_scene.gltf(filepath=str(paths['model']))
    glb, gp = geometry()
    with tempfile.TemporaryDirectory(prefix='pitcrew-props2-audit-') as directory:
        target = Path(directory)/paths['fbx'].name
        shutil.copyfile(paths['fbx'], target)
        clear()
        bpy.ops.import_scene.fbx(filepath=str(target), global_scale=1.0,
            use_manual_orientation=False, use_image_search=False)
        fbx, fp = geometry()
        positions = compare_positions(gp, fp, tolerance=1e-4)
        checks = {
            'triangle_count_matches_glb': fbx['tris']==glb['tris'],
            'triangle_budget': 0 < fbx['tris'] <= limit,
            'prop_budget_declared': 0 < limit <= 1500,
            'bounds_match_glb': all(np.allclose(fbx[k],glb[k],atol=1e-4,rtol=0)
                for k in ('bounds_min','bounds_max')),
            'bounds_match_manifest': all(np.allclose(fbx[k],asset[k],atol=1e-4,rtol=0)
                for k in ('bounds_min','bounds_max')),
            'dimensions_match_manifest_metres': bool(np.allclose(fbx['dimensions_m'],asset['dimensions_m'],atol=1e-4,rtol=0)),
            'root_origins_zero': bool(fbx['roots']) and all(np.allclose(r['origin_blender'],0,atol=1e-5,rtol=0) for r in fbx['roots']),
            'floor_y_zero': abs(fbx['bounds_min'][1])<1e-4,
            'vertex_positions_match_glb': positions['pass'],
            'embedded_albedo_recovered_and_linked': bool(fbx['texture_links']) and all(r['linked'] and r['sha256']==before['texture'] for r in fbx['texture_links']),
            'source_files_unchanged': before=={key:sha(path) for key,path in paths.items()},
        }
        checks.update(pivot_checks(asset,fbx['bounds_min'],fbx['bounds_max']))
        return {'id':asset['id'],'pass':all(checks.values()),'checks':checks,
            'triangle_limit':limit,'fbx':fbx,'glb':glb,'vertex_position_audit':positions,'source_sha256':before}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--manifest', default='art/models/bayloop_props2_v01/manifest.json')
    p.add_argument('--out', required=True); p.add_argument('--ids', nargs='+')
    p.add_argument('--root', default=str(ROOT))
    args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    root=Path(args.root).resolve(); results=[]
    try:
        manifest=json.loads(rooted(root,args.manifest).read_text(encoding='utf-8-sig'))
        for asset in selected_assets(manifest,args.ids):
            try: results.append(inspect(asset,manifest,root))
            except Exception as exc: results.append({'id':asset.get('id'),'pass':False,'error':str(exc)})
        report={'pass':bool(results) and all(r['pass'] for r in results),'assets':results,
            'blender_version':bpy.app.version_string,
            'method':'Fresh GLB and temporary-copy FBX imports; existing geometry() and compare_positions(). D141 max 1500, floor center.',
            'limitations':'No Unity import, texture importer Point-setting proof, or visual gate.'}
    except Exception as exc: report={'pass':False,'assets':results,'error':str(exc)}
    out=rooted(root,args.out); out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'pass':report['pass'],'assets':len(results),'error':report.get('error')}))
    return 0 if report['pass'] else 1


if __name__=='__main__': raise SystemExit(main())
