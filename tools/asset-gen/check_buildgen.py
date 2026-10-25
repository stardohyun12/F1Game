"""Parallel dispatch to unchanged check_buildings.inspect_asset (no relaxed gates)."""
from __future__ import annotations

import argparse
import csv
import json
from concurrent.futures import ProcessPoolExecutor, as_completed
from pathlib import Path

from check_buildings import ROOT, inspect_asset, rooted


_manifest = None
_rows = None
_root = None


def initialize(manifest_path: str, root_path: str, provenance_path: str):
    """Each process independently reads the same manifest and provenance CSV."""
    global _manifest, _rows, _root
    _root = Path(root_path).resolve()
    _manifest = json.loads(rooted(_root, manifest_path).read_text(encoding='utf-8-sig'))
    with rooted(_root, provenance_path).open(encoding='utf-8-sig', newline='') as handle:
        _rows = list(csv.DictReader(handle))


def inspect_index(index: int):
    asset = _manifest['assets'][index]
    try:
        return inspect_asset(asset, _manifest, _root, _rows)
    except Exception as exc:
        return {'id': asset.get('id'), 'pass': False, 'error': str(exc)}


def compare_assets(actual, reference):
    """Exact result comparison, including all detailed triangle/source/hash fields."""
    refs = {item['id']: item for item in reference.get('assets', [])}
    differences = [item['id'] for item in actual if refs.get(item['id']) != item]
    return {'pass': bool(actual) and not differences,
            'compared_ids': [item['id'] for item in actual],
            'different_or_missing_ids': differences,
            'scope': 'Exact per-asset JSON equality; no float tolerance or field omission.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--ids', nargs='+')
    parser.add_argument('--workers', type=int, default=4)
    parser.add_argument('--root', default=str(ROOT))
    parser.add_argument('--provenance', default='art/provenance.csv')
    parser.add_argument('--compare-result', help='Existing original-CLI JSON, exact selected-asset comparison')
    args = parser.parse_args()
    results = []
    try:
        if args.workers < 1:
            raise ValueError('--workers must be positive')
        root = Path(args.root).resolve()
        manifest = json.loads(rooted(root, args.manifest).read_text(encoding='utf-8-sig'))
        ids = [a['id'] for a in manifest['assets']]
        if len(ids) != len(set(ids)):
            raise ValueError('Duplicate manifest asset IDs')
        if args.ids and len(args.ids) != len(set(args.ids)):
            raise ValueError('Duplicate requested IDs')
        if args.ids and set(args.ids) - set(ids):
            raise ValueError('Unknown IDs: ' + ', '.join(sorted(set(args.ids) - set(ids))))
        indices = [i for i, asset in enumerate(manifest['assets'])
                   if not args.ids or asset['id'] in args.ids]
        if not indices:
            raise ValueError('No assets selected')
        # Fail before launching processes for unreadable provenance rather than
        # allowing a worker initializer failure to obscure the path error.
        with rooted(root, args.provenance).open(encoding='utf-8-sig', newline='') as handle:
            list(csv.DictReader(handle))
        indexed_results = {}
        with ProcessPoolExecutor(max_workers=min(args.workers, len(indices)),
                                 initializer=initialize,
                                 initargs=(args.manifest, str(root), args.provenance)) as pool:
            futures = {pool.submit(inspect_index, index): index for index in indices}
            for future in as_completed(futures):
                index = futures[future]
                try:
                    result = future.result()
                except Exception as exc:
                    result = {'id': ids[index], 'pass': False, 'error': str(exc)}
                indexed_results[index] = result
                print(json.dumps({'completed': len(indexed_results), 'total': len(indices),
                                  **{key: result.get(key) for key in ('id', 'pass', 'tris', 'error')}}), flush=True)
        results = [indexed_results[i] for i in indices]
        report = {'pass': bool(results) and all(r['pass'] for r in results), 'assets': results,
                  'scope': 'Unmodified check_buildings.inspect_asset on actual GLB world geometry and albedo. '
                           'Every triangle receives density check; area <0.04m2 remains excluded only from '
                           'the original main-surface variation gate. FBX reimport, Unity import, and visual '
                           'quality are not verified by this wrapper. Source license truth requires human review.',
                  'execution': {'workers': min(args.workers, len(indices)), 'manifest': args.manifest,
                                'provenance': args.provenance, 'selected_ids': [ids[i] for i in indices]}}
        if args.compare_result:
            reference = json.loads(rooted(root, args.compare_result).read_text(encoding='utf-8-sig'))
            report['original_cli_parity'] = compare_assets(results, reference)
            report['pass'] = report['pass'] and report['original_cli_parity']['pass']
    except Exception as exc:
        report = {'pass': False, 'error': str(exc), 'assets': results}
    output = rooted(Path(args.root).resolve(), args.out)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'pass': report['pass'], 'asset_count': len(results),
                      'error': report.get('error'), 'out': str(output)}), flush=True)
    return 0 if report['pass'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
