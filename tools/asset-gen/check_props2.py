"""D141 props: unchanged full GLB audit, explicit replacement of two 800 gates.

No monkey patch, source rewriting, category disguise, or skipped geometry checks.
Original gate values remain in the report; only budget policy changes to 1500.
"""
import argparse
import csv
import json
from pathlib import Path
import numpy as np
ROOT = Path(__file__).resolve().parents[2]


def rooted(root, value):
    path = (root / value).resolve()
    path.relative_to(root.resolve())
    return path


def budget(asset, manifest):
    values = (asset.get('max_tris'), manifest.get('max_tris'))
    if any(type(v) is not int or not 0 < v <= 1500 for v in values):
        raise ValueError('D141 requires integer asset/manifest max_tris in 1..1500')
    if not (asset.get('asset_kind') == 'prop' or asset.get('type') == 'props'):
        raise ValueError('D141 checker accepts props only')
    return min(values)


def pivot_checks(asset, lo, hi):
    lo, hi = np.asarray(lo), np.asarray(hi)
    return {'d141_floor_center_declared': asset.get('pivot_kind') == 'floor_center',
            'd141_floor_center_measured': bool(abs(lo[1]) < 1e-4 and
                abs(lo[0]+hi[0]) < 1e-4 and abs(lo[2]+hi[2]) < 1e-4)}


def adapt_result(result, asset, manifest):
    limit = budget(asset, manifest)
    checks = result['checks']
    # Require the known gates: an upstream renamed/missing gate fails closed.
    original = {key: checks[key] for key in ('triangle_budget', 'prop_budget_declared')}
    result['legacy_800_policy'] = {'checks': original, 'triangle_limit': result['triangle_limit'],
        'reason': 'D141 authorizes <=1500 tris; replaced by measured-count budget checks.'}
    checks['triangle_budget'] = type(result['tris']) is int and 0 < result['tris'] <= limit
    checks['prop_budget_declared'] = all(type(v) is int and 0 < v <= 1500
        for v in (asset.get('max_tris'), manifest.get('max_tris')))
    checks.update(pivot_checks(asset, result['bounds_min'], result['bounds_max']))
    result['triangle_limit'] = limit
    result['pass'] = all(checks.values())
    return result


def inspect_props2(asset, manifest, root, rows):
    # Blender's bundled Python need not install Pillow to use shared policy.
    from check_buildings import inspect_asset
    budget(asset, manifest)
    return adapt_result(inspect_asset(asset, manifest, root, rows), asset, manifest)


def selected_assets(manifest, ids):
    assets = manifest['assets']; names = [a['id'] for a in assets]
    if len(names) != len(set(names)) or (ids and len(ids) != len(set(ids))):
        raise ValueError('Duplicate asset IDs')
    if ids and set(ids)-set(names):
        raise ValueError('Unknown requested asset IDs')
    selected = [a for a in assets if not ids or a['id'] in ids]
    if not selected: raise ValueError('No assets selected')
    return selected


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', default='art/models/bayloop_props2_v01/manifest.json')
    parser.add_argument('--out', required=True)
    parser.add_argument('--ids', nargs='+')
    parser.add_argument('--root', default=str(ROOT))
    parser.add_argument('--provenance', default='art/provenance.csv')
    args = parser.parse_args(); results = []; root = Path(args.root).resolve()
    try:
        manifest = json.loads(rooted(root, args.manifest).read_text(encoding='utf-8-sig'))
        with rooted(root, args.provenance).open(encoding='utf-8-sig', newline='') as f:
            rows = list(csv.DictReader(f))
        for asset in selected_assets(manifest, args.ids):
            try: results.append(inspect_props2(asset, manifest, root, rows))
            except Exception as exc:
                results.append({'id':asset.get('id'), 'pass':False, 'error':str(exc)})
        report = {'pass': bool(results) and all(r['pass'] for r in results), 'assets':results,
            'scope':'All original GLB geometry/density/major-flat/palette/material/bounds/provenance checks run. '
                    'Only two legacy 800 budget gates are explicitly recomputed under D141 <=1500 policy. '
                    'Floor center required even for wall props. Planar decals allowed; degenerate triangles are not. '
                    'No FBX geometry, Unity import or visual approval claimed.'}
    except Exception as exc: report = {'pass':False, 'assets':results, 'error':str(exc)}
    output = rooted(root, args.out); output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'pass':report['pass'], 'assets':len(results), 'error':report.get('error')}))
    return 0 if report['pass'] else 1


if __name__ == '__main__': raise SystemExit(main())
