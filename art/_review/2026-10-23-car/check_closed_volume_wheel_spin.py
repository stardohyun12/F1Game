"""Read-only final static car checks: signed closed volumes and 90-degree spins.

Blender -b --python art/_review/2026-10-23-car/check_closed_volume_wheel_spin.py
Only imports source GLB and writes review JSON. All seam welding and wheel
rotation happen in memory; no asset is saved or exported.
"""
from pathlib import Path
from collections import deque
import hashlib
import json
import math

import bpy
import bmesh
import numpy as np
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
MODEL = ROOT/'art/models/car_v01/car_v01.glb'
PROTECTED = [MODEL, ROOT/'art/models/car_v01/car_v01.fbx', ROOT/'art/models/car_v01/car_v01.blend', ROOT/'art/models/car_v01/CarAtlas.png']
EXPECTED = {'wheel_front_left': [.79, -1.2, .35], 'wheel_front_right': [-.79, -1.2, .35],
            'wheel_rear_left': [.79, 1.1, .375], 'wheel_rear_right': [-.79, 1.1, .375]}


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def hashes(): return {p.relative_to(ROOT).as_posix(): sha(p) for p in PROTECTED}


def component_records(obj):
    bm = bmesh.new(); bm.from_mesh(obj.data)
    original_vertices = len(bm.verts)
    # glTF splits geometric vertices at UV/normal seams. Reconnect coordinates
    # only on this temporary BMesh; do not recalculate or fix any face winding.
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.000001)
    bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table(); bm.edges.ensure_lookup_table()
    world = obj.matrix_world
    unvisited = set(bm.faces)
    records = []
    while unvisited:
        seed = unvisited.pop(); queue = deque([seed]); faces = {seed}
        while queue:
            face = queue.popleft()
            for edge in face.edges:
                for neighbour in edge.link_faces:
                    if neighbour in unvisited:
                        unvisited.remove(neighbour); faces.add(neighbour); queue.append(neighbour)
        edges = {e for f in faces for e in f.edges}
        vertices = {v for f in faces for v in f.verts}
        points = [world@v.co for v in vertices]
        low = Vector([min(v[i] for v in points) for i in range(3)])
        high = Vector([max(v[i] for v in points) for i in range(3)])
        reference = (low+high)/2
        closed = all(len(e.link_faces) == 2 for e in edges)
        boundary_count = sum(len(e.link_faces) == 1 for e in edges)
        nonmanifold_count = sum(len(e.link_faces) > 2 for e in edges)
        inconsistent_count = sum(len(e.link_faces) == 2 and not e.is_contiguous for e in edges)
        volume = 0.
        normal_sum = Vector((0, 0, 0))
        triangle_count = 0
        for face in faces:
            p = [world@v.co for v in face.verts]
            for i in range(1, len(p)-1):
                a, b, c = p[0]-reference, p[i]-reference, p[i+1]-reference
                volume += a.dot(b.cross(c))/6
                normal_sum += (p[i]-p[0]).cross(p[i+1]-p[0])
                triangle_count += 1
        normal = normal_sum.normalized() if normal_sum.length > 1e-10 else Vector((0, 0, 0))
        records.append({'node': obj.name, 'component': len(records), 'triangles': triangle_count,
                        'vertices_after_virtual_seam_weld': len(vertices), 'closed': closed,
                        'boundary_edges': boundary_count, 'nonmanifold_edges_gt2': nonmanifold_count,
                        'inconsistent_winding_shared_edges': inconsistent_count,
                        'signed_volume_m3': float(volume) if closed else None,
                        'positive_closed_volume': volume > 1e-12 if closed else None,
                        'bounds_min_blender': list(low), 'bounds_max_blender': list(high),
                        'area_weighted_open_normal_blender': list(normal) if not closed else None})
    info = {'object': obj.name, 'original_uv_split_vertices': original_vertices,
            'virtual_geometric_vertices': len(bm.verts), 'components': records}
    bm.free()
    return info


def world_state(obj):
    points = [obj.matrix_world@v.co for v in obj.data.vertices]
    low = np.array([min(v[i] for v in points) for i in range(3)])
    high = np.array([max(v[i] for v in points) for i in range(3)])
    axis = obj.matrix_world.to_3x3()@Vector((1, 0, 0)); axis.normalize()
    return {'origin': list(obj.matrix_world.translation), 'bounds_center': ((low+high)/2).tolist(),
            'bounds_min': low.tolist(), 'bounds_max': high.tolist(), 'spin_axis': list(axis)}


def spin_record(obj, expected):
    original = obj.matrix_basis.copy()
    before = world_state(obj)
    # Postmultiplication rotates the mesh around its unchanged local X pivot.
    obj.matrix_basis = original@Matrix.Rotation(math.pi/2, 4, 'X')
    bpy.context.view_layer.update()
    after = world_state(obj)
    obj.matrix_basis = original
    bpy.context.view_layer.update()
    pivot_shift = float(np.max(np.abs(np.array(after['origin'])-before['origin'])))
    center_shift = float(np.max(np.abs(np.array(after['bounds_center'])-before['bounds_center'])))
    source_error = float(np.max(np.abs(np.array(before['origin'])-expected)))
    checks = {'expected_original_pivot': source_error <= .0001,
              'world_pivot_unchanged_at_90_degrees': pivot_shift <= .000001,
              'world_bounds_center_preserved_at_90_degrees': center_shift <= .00001,
              'before_spin_axis_x': bool(np.allclose(before['spin_axis'], [1, 0, 0], atol=.00001, rtol=0)),
              'after_spin_axis_x': bool(np.allclose(after['spin_axis'], [1, 0, 0], atol=.00001, rtol=0))}
    return {'name': obj.name, 'pass': all(checks.values()), 'checks': checks,
            'expected_origin_blender': expected, 'before': before, 'after_90_degrees': after,
            'world_pivot_shift_max_m': pivot_shift, 'world_bounds_center_shift_max_m': center_shift,
            'source_pivot_error_max_m': source_error}


def main():
    before_hash = hashes()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(MODEL))
    bpy.context.view_layer.update()
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    records = [component_records(o) for o in meshes]
    components = [c for r in records for c in r['components']]
    closed = [c for c in components if c['closed']]
    open_components = [c for c in components if not c['closed']]
    wheels = []
    for name, expected in EXPECTED.items():
        found = [o for o in meshes if o.name == name]
        wheels.append(spin_record(found[0], expected) if len(found) == 1 else {'name': name, 'pass': False, 'reason': 'Expected exactly one independent mesh object'})
    after_hash = hashes()
    checks = {'at_least_one_closed_component': bool(closed),
              'all_closed_components_positive_signed_volume': all(c['positive_closed_volume'] for c in closed),
              'no_edges_with_more_than_two_faces': not any(c['nonmanifold_edges_gt2'] for c in components),
              'closed_components_consistent_face_winding': not any(c['inconsistent_winding_shared_edges'] for c in closed),
              'four_independent_wheels_spin_about_original_axis_and_center': len(wheels) == 4 and all(w['pass'] for w in wheels),
              'protected_source_hashes_unchanged': before_hash == after_hash}
    result = {'pass': all(checks.values()), 'checks': checks, 'blender_version': bpy.app.version_string,
              'model': str(MODEL), 'source_hashes_before': before_hash, 'source_hashes_after': after_hash,
              'mesh_objects': len(meshes), 'total_components': len(components), 'closed_components': len(closed),
              'open_components': len(open_components), 'minimum_closed_signed_volume_m3': min((c['signed_volume_m3'] for c in closed), default=None),
              'component_topology': records, 'open_component_evidence': open_components, 'wheel_spin_90': wheels,
              'method': 'Import GLB; virtually reconnect UV-split coordinates at 1e-6m in temporary BMesh only. Face-edge connected components; signed tetrahedral volume for fully closed components without changing normals. Rotate each wheel by local X 90 degrees in memory; compare world origins/bounds centers/axes. SHA-256 before and after all four source files.',
              'limitations': 'Static Blender geometry test. Open decal/surface planes have no enclosed signed-volume requirement and their orientation is listed, not semantically approved. Does not run Unity wheel animation, physics or import.'}
    (HERE/'check_closed_volume_wheel_spin.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({k: result[k] for k in ['pass', 'checks', 'total_components', 'closed_components', 'open_components', 'minimum_closed_signed_volume_m3']}, ensure_ascii=False))
    if not result['pass']:
        raise RuntimeError('Static geometry audit failed; see review JSON')


main()
