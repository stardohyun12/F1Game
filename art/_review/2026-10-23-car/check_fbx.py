"""Blender-only round-trip audit of car_v01 FBX versus independently imported GLB.

Writes a JSON result in this review folder; never saves imported assets.
Blender -b --python art/_review/2026-10-23-car/check_fbx.py -- --model-dir art/models/car_v01
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
EXPECTED_LOW = np.array([-.979454517, -2.029999971, 0.])
EXPECTED_HIGH = np.array([.979454517, 1.754999995, 1.072000027])
WHEELS = {'wheel_front_left': [.79, -1.2, .35], 'wheel_front_right': [-.79, -1.2, .35],
          'wheel_rear_left': [.79, 1.1, .375], 'wheel_rear_right': [-.79, 1.1, .375]}


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def metric(points, pixels):
    e = np.column_stack((points[1]-points[0], points[2]-points[0]))
    if np.linalg.norm(np.cross(e[:, 0], e[:, 1])) < 1e-12:
        raise ValueError('Degenerate geometry')
    _, basis = np.linalg.qr(e, mode='reduced')
    pe = np.column_stack((pixels[1]-pixels[0], pixels[2]-pixels[0]))
    singular = np.linalg.svd(pe@np.linalg.inv(basis), compute_uv=False)
    if not np.isfinite(singular).all() or singular.min() < 1e-8:
        raise ValueError('Degenerate UV')
    return [float(singular.min()), float(singular.max())]


def measure(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if path.suffix.lower() == '.fbx':
        bpy.ops.import_scene.fbx(filepath=str(path))
    else:
        bpy.ops.import_scene.gltf(filepath=str(path))
    bpy.context.view_layer.update()
    allv, objects, principal, failures, fingerprints = [], {}, [], [], {}
    tris = 0
    for o in bpy.context.scene.objects:
        if o.type != 'MESH':
            continue
        me = o.data; me.calc_loop_triangles()
        world = np.asarray(o.matrix_world, dtype=float)
        local = np.array([list(v.co) for v in me.vertices])
        p = (np.column_stack((local, np.ones(len(local))))@world.T)[:, :3]
        allv.extend(p.tolist())
        uv = me.uv_layers.active
        if uv is None:
            raise ValueError('Missing UV layer: '+o.name)
        points = set()
        for triangle in me.loop_triangles:
            ids = list(triangle.vertices)
            pix = np.array([list(uv.data[i].uv) for i in triangle.loops])*512
            try:
                scales = metric(p[ids], pix)
                principal.append({'node': o.name, 'triangle': tris, 'principal_px_per_m': scales})
            except Exception as exc:
                failures.append({'node': o.name, 'triangle': tris, 'reason': str(exc)})
            for vertex, tex in zip(p[ids], pix/512):
                points.add(tuple(np.concatenate((vertex, tex))))
            tris += 1
        fingerprints[o.name] = points
        lo, hi = p.min(axis=0), p.max(axis=0)
        origin = world[:3, 3]
        axis = world[:3, 0]; axis /= np.linalg.norm(axis)
        objects[o.name] = {'tris': len(me.loop_triangles), 'origin_blender': origin.tolist(),
                           'bounds_min': lo.tolist(), 'bounds_max': hi.tolist(), 'spin_axis_x': axis.tolist(),
                           'center_error_m': float(np.max(np.abs((lo+hi)/2-origin))),
                           'unique_position_uv_corners': len(points)}
    if not allv:
        raise ValueError('No imported mesh geometry')
    arr = np.asarray(allv)
    low, high = arr.min(axis=0), arr.max(axis=0)
    bad = [r for r in principal if r['principal_px_per_m'][0] < 57.6-1e-5 or r['principal_px_per_m'][1] > 70.4+1e-5]
    wheel_results = []
    for name, expected in WHEELS.items():
        n = objects.get(name)
        if n is None:
            wheel_results.append({'name': name, 'pass': False, 'reason': 'Missing separately named wheel'})
            continue
        good = bool(np.allclose(n['origin_blender'], expected, atol=.0001, rtol=0)
                    and np.allclose(n['spin_axis_x'], [1, 0, 0], atol=.00001, rtol=0)
                    and n['center_error_m'] <= .001 and abs(n['bounds_min'][2]) <= .0001)
        wheel_results.append({'name': name, 'pass': good, **n})
    checks = {'tris_at_most_3000': 0 < tris <= 3000,
              'source_bounds_and_pivot_preserved': bool(np.allclose(low, EXPECTED_LOW, atol=.0001, rtol=0) and np.allclose(high, EXPECTED_HIGH, atol=.0001, rtol=0)),
              'all_triangles_nondegenerate': len(principal) == tris and not failures,
              'all_triangles_density_64_both_principal_axes': not bad and len(principal) == tris,
              'four_wheel_pivots_axes_center_floor': all(w['pass'] for w in wheel_results) and sum(n.startswith('wheel_') for n in objects) == 4}
    return {'pass': all(checks.values()), 'checks': checks, 'path': str(path), 'sha256': sha(path),
            'tris': tris, 'bounds_min_blender': low.tolist(), 'bounds_max_blender': high.tolist(),
            'dimensions_blender': (high-low).tolist(), 'objects': objects, 'wheels': wheel_results,
            'density_min': min((s['principal_px_per_m'][0] for s in principal), default=0),
            'density_max': max((s['principal_px_per_m'][1] for s in principal), default=0),
            'density_failures': bad, 'geometry_failures': failures}, fingerprints


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--model-dir', type=Path, default=ROOT/'art/models/car_v01')
    parser.add_argument('--out', type=Path, default=Path(__file__).resolve().parent/'check_fbx.json')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    try:
        fbx, fpoints = measure(args.model_dir/'car_v01.fbx')
        glb, gpoints = measure(args.model_dir/'car_v01.glb')
        diffs, corner_comparisons = [], []
        for name in sorted(set(fpoints)|set(gpoints)):
            a, b = fpoints.get(name, set()), gpoints.get(name, set())
            if not a or not b:
                diffs.append({'node': name, 'reason': 'Missing corners in one format'})
                continue
            av, bv = np.array(list(a)), np.array(list(b))
            # Direct bidirectional nearest corner matching avoids quantization
            # boundary false positives (e.g. UV .125625 +/- float32 error).
            delta = np.abs(av[:, None, :]-bv[None, :, :])
            distance = delta.max(axis=2)
            closest_b = distance.argmin(axis=1)
            closest_a = distance.argmin(axis=0)
            worst_ab = delta[np.arange(len(av)), closest_b].max(axis=0)
            worst_ba = delta[closest_a, np.arange(len(bv))].max(axis=0)
            worst = np.maximum(worst_ab, worst_ba)
            detail = {'node': name, 'fbx_unique': len(a), 'glb_unique': len(b),
                      'max_position_delta_m': float(worst[:3].max()),
                      'max_uv_delta_normalized': float(worst[3:].max()),
                      'max_uv_delta_pixels_at_512': float(worst[3:].max()*512),
                      'position_tolerance_m': .00001, 'uv_tolerance_normalized': .00001}
            detail['pass'] = bool(np.all(worst <= .00001))
            corner_comparisons.append(detail)
            if not detail['pass']:
                diffs.append(detail)
        checks = {'fbx_individual_checks': fbx['pass'], 'glb_individual_checks': glb['pass'],
                  'same_triangle_count': fbx['tris'] == glb['tris'],
                  'same_mesh_object_names': set(fpoints) == set(gpoints),
                  'same_position_uv_corners_within_1e5': not diffs,
                  'same_bounds': bool(np.allclose(fbx['bounds_min_blender'], glb['bounds_min_blender'], atol=.00001, rtol=0) and np.allclose(fbx['bounds_max_blender'], glb['bounds_max_blender'], atol=.00001, rtol=0))}
        result = {'pass': all(checks.values()), 'checks': checks, 'blender_version': bpy.app.version_string,
                  'fbx': fbx, 'glb': glb, 'position_uv_differences': diffs,
                  'position_uv_corner_comparisons': corner_comparisons,
                  'method': 'Independent Blender FBX and glTF imports. Compare transformed mesh vertex/UV corner sets, bounds, pivots, X spin axes, triangle count and all triangle UV principal densities.',
                  'limitations': 'Blender round trip only. Does not run Unity FBX/glTF importer or prove runtime name/axis conversion; compare current-car-measure.md for measured Unity prefab convention. Texture/palette/provenance audit is check_car.py.'}
    except Exception as exc:
        result = {'pass': False, 'error': f'{type(exc).__name__}: {exc}'}
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k in ['pass', 'checks', 'error']}, ensure_ascii=False))
    if not result['pass']:
        raise RuntimeError('FBX audit failed; see '+str(args.out))


main()
