"""Blender background read-only FBX re-import audit; does not save the scene.

Run: blender -b -P tools/asset-gen/check_hill_house_fbx.py --
It checks actual imported meshes, UVs, triangles, units and comparison with the
independent GLB audit. Texture RGB/Point and provenance are covered by GLB audit;
FBX has no reliable portable Point sampler setting, so Unity import is still QA.
"""
from pathlib import Path
import argparse
import hashlib
import json
import sys

import bpy
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--model', type=Path, default=ROOT / 'art/models/hill_house_v01/hill_house_v01.fbx')
parser.add_argument('--reference', type=Path, default=ROOT / 'art/_review/2026-10-23-hillhouse/check-hillhouse.json')
parser.add_argument('--out', type=Path, default=ROOT / 'art/_review/2026-10-23-hillhouse/check-hillhouse-fbx.json')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])

try:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(args.model), use_anim=False)
    ref = json.loads(args.reference.read_text(encoding='utf-8'))
    points, scales, violations = [], [], []
    count = 0
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH':
            continue
        mesh = obj.data
        mesh.calc_loop_triangles()
        uv = mesh.uv_layers.active
        if not uv:
            raise ValueError('Missing UV on ' + obj.name)
        world = np.array([tuple(obj.matrix_world @ vertex.co) for vertex in mesh.vertices])
        # Blender imports FBX back to Z-up metres; compare against GLB Y-up.
        yup = world[:, [0, 2, 1]].copy()
        yup[:, 2] *= -1
        for triangle in mesh.loop_triangles:
            count += 1
            p = yup[list(triangle.vertices)]
            points.extend(p)
            pixels = np.array([tuple(uv.data[i].uv) for i in triangle.loops]) * 512
            edge = np.column_stack([p[1] - p[0], p[2] - p[0]])
            area = np.linalg.norm(np.cross(edge[:, 0], edge[:, 1]))
            if area < 1e-12:
                violations.append({'triangle': count, 'reason': 'Degenerate imported geometry'})
                continue
            _, basis = np.linalg.qr(edge, mode='reduced')
            singular = np.linalg.svd(np.column_stack([pixels[1] - pixels[0], pixels[2] - pixels[0]]) @ np.linalg.inv(basis), compute_uv=False)
            scales.append([float(min(singular)), float(max(singular))])
            # Blender UV origin is bottom-left, matching the original slot grid.
            low, high = pixels.min(axis=0), pixels.max(axis=0)
            candidates = []
            for slot in range(51, 59):
                lo = np.array([(slot % 8) * 64, (slot // 8) * 64])
                if np.all(low >= lo - .001) and np.all(high <= lo + 64 + .001):
                    candidates.append(slot)
            if len(candidates) != 1:
                violations.append({'triangle': count, 'reason': 'Imported UV crosses or misses allowed cell'})
    if not points or not scales:
        raise ValueError('No imported, textured triangle geometry')
    bounds = np.array(points)
    lo, hi = bounds.min(axis=0), bounds.max(axis=0)
    density_min = min(s[0] for s in scales)
    density_max = max(s[1] for s in scales)
    checks = {
        'triangle_budget': 0 < count <= 2500,
        'tris_match_glb': count == ref['tris'],
        'density_both_principal_axes': len(scales) == count and density_min >= 28.8 - .0001 and density_max <= 35.2 + .0001,
        'valid_geometry_and_permitted_uv_cells': not violations,
        'dimensions_match_glb': bool(np.allclose(hi - lo, ref['dimensions_m'], atol=.0001)),
        'bounds_match_glb': bool(np.allclose(lo, ref['bounds_min_m'], atol=.0001) and np.allclose(hi, ref['bounds_max_m'], atol=.0001)),
        'pivot_on_floor': abs(float(lo[1])) <= .0001,
    }
    material_evidence = []
    for material in bpy.data.materials:
        if not material.use_nodes:
            material_evidence.append({'name': material.name, 'uses_nodes': False})
            continue
        shader = next((n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if not shader:
            material_evidence.append({'name': material.name, 'principled_shader': False})
            continue
        base = shader.inputs['Base Color']
        direct = base.links[0].from_node if base.is_linked else None
        # The FBX importer can link the texture directly and silently lose a
        # source Blender multiply node. A socket default does not tint a link.
        inferred_factor = [1, 1, 1, 1] if direct and direct.type == 'TEX_IMAGE' else (list(base.default_value) if not direct else None)
        expected = {'unlit_glass': [.12, .19, .21, 1], 'plant': [.42, .6, .20, 1]}.get(material.name)
        emission = shader.inputs['Emission Color']
        strength = shader.inputs['Emission Strength']
        material_evidence.append({
            'name': material.name,
            'base_color_linked': base.is_linked,
            'base_color_source_node': direct.type if direct else None,
            'base_color_unlinked_socket_default': list(base.default_value),
            'effective_base_color_factor_if_simple_graph': inferred_factor,
            'expected_glb_base_color_factor': expected,
            'stored_base_color_matches_expected': bool(np.allclose(base.default_value, expected, atol=.00001)) if expected else None,
            'factor_matches_expected': bool(np.allclose(inferred_factor, expected, atol=.00001)) if expected and inferred_factor else None,
            'emission_color': list(emission.default_value),
            'emission_strength': float(strength.default_value),
            'emission_linked': emission.is_linked or strength.is_linked,
            'image_paths': [n.image.filepath for n in material.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image],
        })
    material_handoff = []
    for entry in material_evidence:
        if entry.get('expected_glb_base_color_factor') is None:
            continue
        if not entry['image_paths'] or not entry['base_color_linked']:
            material_handoff.append(entry['name'] + ': atlas connection absent after FBX import; restore HillHouseAtlas base texture')
        if entry['stored_base_color_matches_expected'] is False:
            material_handoff.append(entry['name'] + ': stored FBX material color differs from GLB; restore baseColorFactor')
        elif entry['factor_matches_expected'] is False:
            material_handoff.append(entry['name'] + ': atlas and tint data are preserved, but Blender imports a direct image link that bypasses the stored tint; confirm texture-times-tint in Unity importer/material')
    tint_entries = [m for m in material_evidence if m.get('expected_glb_base_color_factor') is not None]
    result = {
        'pass': all(checks.values()), 'pass_scope': 'Geometry, UV, density and units only; material handoff issues below are not waived.', 'checks': checks,
        'tris': count, 'density_min': density_min, 'density_max': density_max,
        'dimensions_m': (hi - lo).tolist(), 'bounds_min_m': lo.tolist(), 'bounds_max_m': hi.tolist(),
        'violations': violations,
        'imported_materials': material_evidence,
        'material_handoff_issues': material_handoff,
        'material_handoff_complete': not material_handoff,
        'fbx_tint_and_atlas_data_preserved': all(m['stored_base_color_matches_expected'] and m['image_paths'] and m['base_color_linked'] for m in tint_entries),
        'blender_imported_shader_tints_match_glb': all(m['factor_matches_expected'] for m in tint_entries),
        'fbx_sha256': hashlib.sha256(args.model.read_bytes()).hexdigest(),
        'reference_glb_sha256': ref['model_sha256'],
        'blender_version': bpy.app.version_string,
        'method': 'Fresh factory-empty Blender scene; actual FBX importer; mesh loop triangles, transformed positions and loop UV Jacobian. Compare with independently decoded GLB bounds and tris.',
        'limitations': 'Blender import only. Unity/FBX material texture Point settings still require programming/QA import; no Unity editor was run.',
    }
except Exception as exc:
    result = {'pass': False, 'error': f'{type(exc).__name__}: {exc}'}
args.out.parent.mkdir(parents=True, exist_ok=True)
args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(result, ensure_ascii=False))
if not result['pass']:
    raise RuntimeError('FBX audit failed; see ' + str(args.out))
