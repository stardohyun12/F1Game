"""Read-only GLB tint/emission preservation check for the final hill house."""
from pathlib import Path
import hashlib
import json

import numpy as np

from check_asset import accessor, node_matrix, read_glb

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / 'art/models/hill_house_v01/hill_house_v01.glb'
OUT = ROOT / 'art/_review/2026-10-23-hillhouse/check-hillhouse-materials.json'
g, blob = read_glb(MODEL)
checks = {}
records = []
emissive_indices = []
for index, material in enumerate(g['materials']):
    name = material.get('name', '')
    pbr = material.get('pbrMetallicRoughness', {})
    factor = pbr.get('baseColorFactor', [1, 1, 1, 1])
    emission = material.get('emissiveFactor', [0, 0, 0])
    if max(emission) > 0:
        emissive_indices.append(index)
    expected = {'unlit_glass': [.12, .19, .21, 1], 'plant': [.42, .6, .20, 1]}.get(name)
    if expected:
        checks[name + '_tint_preserved'] = bool(np.allclose(factor, expected, atol=.000001))
        checks[name + '_atlas_connected'] = 'baseColorTexture' in pbr
    records.append({'name': name, 'baseColorFactor': factor, 'baseColorTexture': pbr.get('baseColorTexture'), 'emissiveFactor': emission})
checks['required_tint_materials_exist'] = all(any(m.get('name') == name for m in g['materials']) for name in ['unlit_glass', 'plant'])
checks['one_emissive_material'] = len(emissive_indices) == 1
emissive_faces = []


def walk(index, parent):
    node = g['nodes'][index]
    world = parent @ node_matrix(node)
    if 'mesh' in node:
        for primitive in g['meshes'][node['mesh']]['primitives']:
            if primitive.get('material') not in emissive_indices:
                continue
            positions = accessor(g, blob, primitive['attributes']['POSITION']).astype(float)
            positions = (np.column_stack([positions, np.ones(len(positions))]) @ world.T)[:, :3]
            ids = accessor(g, blob, primitive['indices']).ravel() if 'indices' in primitive else np.arange(len(positions))
            for tri in ids.reshape(-1, 3):
                emissive_faces.append([tuple(np.round(p, 5)) for p in positions[tri]])
    for child in node.get('children', []):
        walk(child, world)


for index in g['scenes'][g.get('scene', 0)]['nodes']:
    walk(index, np.eye(4))
graph = {}
for tri in emissive_faces:
    for point in tri:
        graph.setdefault(point, set()).update(tri)
components = []
unseen = set(graph)
while unseen:
    stack = [next(iter(unseen))]
    component = set()
    while stack:
        point = stack.pop()
        if point in component:
            continue
        component.add(point)
        stack.extend(graph[point] - component)
    unseen.difference_update(component)
    positions = np.array(list(component))
    components.append({'welded_vertices': len(component), 'bounds_min_m': positions.min(axis=0).tolist(), 'bounds_max_m': positions.max(axis=0).tolist()})
checks['one_connected_emissive_region'] = len(components) == 1
result = {
    'pass': all(checks.values()), 'checks': checks, 'materials': records,
    'emissive_triangle_count': len(emissive_faces), 'emissive_connected_components': components,
    'model_sha256': hashlib.sha256(MODEL.read_bytes()).hexdigest(),
    'method': 'Read exported GLB material factors, texture references and emission. Count connected emissive triangles after world-position welding at 0.00001m; one component is geometric evidence for one glowing-window region.',
    'limitations': 'GLB metadata and geometry verification only, not Unity rendering. FBX material losses are recorded separately in check-hillhouse-fbx.json.',
}
OUT.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(result, ensure_ascii=False))
raise SystemExit(0 if result['pass'] else 1)
